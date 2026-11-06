using AcadFunction;
using Autodesk.AutoCAD.EditorInput;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AcadDocument = Autodesk.AutoCAD.ApplicationServices.Document;

namespace PipeLoad2
{
    /// <summary>Duct_ToRevit 폼. 픽셀 배치 대신 TableLayoutPanel 로 짠다 (한글 라벨 + DPI 배율 대비).</summary>
    internal class DuctToRevitForm : Form
    {
        private const string DefaultBaseHeight = "2800";

        private readonly AcadDocument _doc;
        private readonly Label _lblProject = NewLabel("Revit 연결 중...");
        private readonly Label _lblSelection = NewLabel("선택된 덕트 없음");
        private readonly Button _btnReload = NewButton("다시 불러오기");
        private readonly Button _btnSelect = NewButton("Duct 선택");
        private readonly Button _btnCreate = NewButton("Revit 에 생성");
        private readonly DataGridView _grid = new();
        private readonly TextBox _txtResult = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };

        private readonly DataGridViewTextBoxColumn _colGroup = new() { HeaderText = "그룹", ReadOnly = true };
        private readonly DataGridViewTextBoxColumn _colCount = new() { HeaderText = "선 수", ReadOnly = true };
        private readonly DataGridViewComboBoxColumn _colSystem = NewCombo("시스템");
        private readonly DataGridViewComboBoxColumn _colType = NewCombo("덕트 타입");
        private readonly DataGridViewComboBoxColumn _colLevel = NewCombo("레벨");
        private readonly DataGridViewTextBoxColumn _colHeight = new() { HeaderText = "기준 높이(mm)" };
        private readonly DataGridViewComboBoxColumn _colAlign = new() { HeaderText = "정렬" };

        private RevitProject? _project;
        private DuctSelection? _selection;

        public DuctToRevitForm(AcadDocument doc)
        {
            _doc = doc;

            Text = "Duct → Revit";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(960, 560);

            _colAlign.Items.AddRange("Bottom", "Middle", "Top");
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Columns.AddRange(_colGroup, _colCount, _colSystem, _colType, _colLevel, _colHeight, _colAlign);
            _grid.DataError += (s, e) => e.ThrowException = false; // 다시 불러오기로 콤보 목록이 바뀔 때

            var top = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            top.Controls.AddRange(new Control[] { _btnReload, _lblProject });

            var middle = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            middle.Controls.AddRange(new Control[] { _btnSelect, _lblSelection });

            var bottom = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            bottom.Controls.Add(_btnCreate);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            layout.Controls.Add(top, 0, 0);
            layout.Controls.Add(middle, 0, 1);
            layout.Controls.Add(_grid, 0, 2);
            layout.Controls.Add(bottom, 0, 3);
            layout.Controls.Add(_txtResult, 0, 4);
            Controls.Add(layout);

            Load += async (s, e) => await LoadProjectAsync();
            _btnReload.Click += async (s, e) => await LoadProjectAsync();
            _btnSelect.Click += (s, e) => SelectDucts();
            _btnCreate.Click += async (s, e) => await CreateDuctsAsync();
            UpdateButtons();
        }

        private async Task LoadProjectAsync()
        {
            _project = null;
            _btnReload.Enabled = false;
            UpdateButtons();
            _lblProject.Text = "Revit 연결 중...";
            try
            {
                RevitProject project = await DuctToRevitClient.GetProjectAsync();
                if (project.Systems.Count == 0 || project.DuctTypes.Count == 0 || project.Levels.Count == 0)
                    throw new InvalidOperationException($"'{project.Document}' 에 덕트 시스템·사각 덕트 타입·레벨이 모두 있어야 합니다.");

                Bind(_colSystem, project.Systems);
                Bind(_colType, project.DuctTypes);
                Bind(_colLevel, project.Levels);
                _project = project;
                _lblProject.Text = "Revit 프로젝트: " + project.Document;
                RebuildRows();
            }
            catch (Exception ex)
            {
                _lblProject.Text = ex.Message;
            }
            _btnReload.Enabled = true;
            UpdateButtons();
        }

        private void SelectDucts()
        {
            Editor ed = _doc.Editor;
            using (EditorUserInteraction interaction = ed.StartUserInteraction(Handle))
            {
                var options = new PromptSelectionOptions { MessageForAdding = "\nRevit 으로 보낼 덕트 Line 과 엘보 Arc 를 선택하세요: " };
                PromptSelectionResult result = ed.GetSelection(options, JSelFilter.MakeFilterTypesRegs("LINE,ARC", "Tree,a"));
                interaction.End();
                if (result.Status != PromptStatus.OK) return;

                _selection = DuctSelection.Read(_doc.Database, result.Value.GetObjectIds());
            }

            _lblSelection.Text = $"덕트 {_selection.Lines.Count}개, 엘보 {_selection.Arcs.Count}개" +
                (_selection.NoSizeCount > 0 ? $" (크기 a·b 없는 Line {_selection.NoSizeCount}개 제외)" : "");
            RebuildRows();
            UpdateButtons();
        }

        /// <summary>그룹마다 한 줄. 새 값은 프로젝트의 첫 항목으로 채운다.</summary>
        private void RebuildRows()
        {
            _grid.Rows.Clear();
            if (_project == null || _selection == null) return;

            foreach (var group in _selection.Lines.GroupBy(x => x.Group).OrderBy(x => x.Key))
            {
                _grid.Rows.Add(group.Key, group.Count(),
                    _project.Systems[0].Id, _project.DuctTypes[0].Id, _project.Levels[0].Id,
                    DefaultBaseHeight, "Bottom");
            }
        }

        private async Task CreateDuctsAsync()
        {
            if (_project == null || _selection == null) return;

            var groups = new List<RevitDuctGroup>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                string name = (string)row.Cells[_colGroup.Index].Value;
                if (!double.TryParse(Convert.ToString(row.Cells[_colHeight.Index].Value), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double baseHeight))
                {
                    _txtResult.Text = $"그룹 '{name}' 의 기준 높이가 숫자가 아닙니다.";
                    return;
                }

                groups.Add(new RevitDuctGroup
                {
                    Name = name,
                    SystemTypeId = (int)row.Cells[_colSystem.Index].Value,
                    DuctTypeId = (int)row.Cells[_colType.Index].Value,
                    LevelId = (int)row.Cells[_colLevel.Index].Value,
                    BaseHeight = baseHeight,
                    Align = (string)row.Cells[_colAlign.Index].Value
                });
            }

            var request = new RevitDuctRequest
            {
                Document = _project.Document,
                Groups = groups,
                Lines = _selection.Lines,
                Arcs = _selection.Arcs
            };

            _btnCreate.Enabled = false; // 중복 생성 방지
            _txtResult.Text = "생성 중...";
            try
            {
                RevitDuctResult result = await DuctToRevitClient.CreateDuctsAsync(request);
                var lines = new List<string> { $"덕트 {result.Created}개, 엘보 {result.Elbows}개 생성" };
                if (result.Failures.Count > 0)
                {
                    lines.Add($"실패 {result.Failures.Count}건:");
                    lines.AddRange(result.Failures);
                }
                _txtResult.Text = string.Join(Environment.NewLine, lines);

                // 같은 선택을 다시 보내면 덕트가 두 벌 생긴다.
                if (result.Created > 0)
                {
                    _selection = null;
                    _lblSelection.Text = "선택된 덕트 없음";
                    RebuildRows();
                }
            }
            catch (Exception ex)
            {
                _txtResult.Text = "생성 실패: " + ex.Message;
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _btnCreate.Enabled = _project != null && _selection != null && _selection.Lines.Count > 0;
        }

        private static void Bind(DataGridViewComboBoxColumn column, List<RevitItem> items)
        {
            column.DataSource = items;
            column.DisplayMember = nameof(RevitItem.Name);
            column.ValueMember = nameof(RevitItem.Id);
        }

        private static DataGridViewComboBoxColumn NewCombo(string header) => new() { HeaderText = header };

        private static Label NewLabel(string text) =>
            new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(8, 8, 0, 0) };

        private static Button NewButton(string text) => new() { Text = text, AutoSize = true };
    }
}
