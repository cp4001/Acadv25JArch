using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using FlowDirection = System.Windows.Forms.FlowDirection;
using Image = System.Drawing.Image;

namespace LayerManager
{
    /// <summary>
    /// AutoCAD용 레이어 관리 팔레트 (DataGridView 방식) - .NET 8.0 / AutoCAD 2025 API 기준
    /// </summary>
    public class LayerManagerPalette
    {
        private static PaletteSet? _paletteSet;
        private static LayerManagerControl? _layerControl;

        /// <summary>
        /// 레이어 관리 팔레트 표시/숨김 토글
        /// </summary>
        [CommandMethod("LAYER_MANAGER")]
        public static void Cmd_ShowLayerManager()
        {
            try
            {
                if (_paletteSet == null)
                {
                    // PaletteSet 생성 - 크기 증가
                    _paletteSet = new PaletteSet("도면층 관리", new Guid("12345678-1234-1234-1234-123456789ABC"))
                    {
                        Size = new Size(800, 600), // 650 -> 800으로 증가
                        MinimumSize = new Size(700, 300), // 580 -> 700으로 증가
                        DockEnabled = DockSides.Left | DockSides.Right,
                        Style = PaletteSetStyles.ShowPropertiesMenu |
                               PaletteSetStyles.ShowAutoHideButton |
                               PaletteSetStyles.ShowCloseButton
                    };

                    // 사용자 컨트롤 생성 및 추가
                    _layerControl = new LayerManagerControl();
                    _paletteSet.Add("레이어 관리", _layerControl);

                    // 이벤트 핸들러 등록
                    _paletteSet.StateChanged += PaletteSet_StateChanged;
                }

                // 팔레트 표시
                _paletteSet.Visible = !_paletteSet.Visible;
            }
            catch (System.Exception ex)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                var ed = doc?.Editor;
                ed?.WriteMessage($"\n팔레트 생성 오류: {ex.Message}");
            }
        }

        private static void PaletteSet_StateChanged(object? sender, PaletteSetStateEventArgs e)
        {
            // 팔레트가 표시될 때 레이어 정보 새로고침
            if (e.NewState == StateEventIndex.Show && _layerControl != null)
            {
                _layerControl.RefreshLayers();
            }
        }
    }

    /// <summary>
    /// 레이어 관리를 위한 사용자 컨트롤 (DataGridView 기반)
    /// </summary>
    public partial class LayerManagerControl : UserControl
    {
        private DataGridView _dataGridView = null!;
        private ToolStrip _toolStrip = null!;
        private ContextMenuStrip _contextMenu = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private ImageList _imageList = null!;
        private BindingList<LayerGridData> _layerDataList = null!;
        private List<LayerGridData> _allLayerData = null!; // 전체 레이어 데이터 보관
        private ToolStripTextBox _filterTextBox = null!;

        // 레이어 상태 이미지 인덱스
        private const int LAYER_ON = 0;
        private const int LAYER_OFF = 1;
        private const int LAYER_FROZEN = 2;
        private const int LAYER_LOCKED = 3;
        private const int LAYER_CURRENT = 4;

        public LayerManagerControl()
        {
            InitializeComponent();
            SetupEventHandlers();
            RefreshLayers();
        }

        /// <summary>
        /// 컨트롤 초기화
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();

            // 이미지 리스트 설정
            SetupImageList();

            // 데이터 리스트 초기화
            _layerDataList = new BindingList<LayerGridData>();
            _allLayerData = new List<LayerGridData>();

            // 툴스트립 생성
            SetupToolStrip();

            // DataGridView 생성
            SetupDataGridView();

            // 상태바 생성
            SetupStatusStrip();

            // 컨텍스트 메뉴 생성
            SetupContextMenu();

            // 레이아웃 설정 - 크기 증가
            Size = new Size(780, 580); // 630 -> 780으로 증가
            Controls.Add(_dataGridView);
            Controls.Add(_statusStrip);
            Controls.Add(_toolStrip);

            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// 이미지 리스트 설정
        /// </summary>
        private void SetupImageList()
        {
            _imageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };

            // 레이어 상태별 아이콘 생성
            _imageList.Images.Add(CreateLayerIcon(System.Drawing.Color.Green));      // ON
            _imageList.Images.Add(CreateLayerIcon(System.Drawing.Color.Red));        // OFF
            _imageList.Images.Add(CreateLayerIcon(System.Drawing.Color.Blue));       // FROZEN
            _imageList.Images.Add(CreateLayerIcon(System.Drawing.Color.Orange));     // LOCKED
            _imageList.Images.Add(CreateLayerIcon(System.Drawing.Color.Gold));       // CURRENT
        }

        /// <summary>
        /// 레이어 상태별 아이콘 생성
        /// </summary>
        private Bitmap CreateLayerIcon(System.Drawing.Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(System.Drawing.Color.Transparent);
                using (var brush = new SolidBrush(color))
                using (var pen = new Pen(System.Drawing.Color.Black, 1))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                    g.DrawEllipse(pen, 2, 2, 12, 12);
                }
            }
            return bitmap;
        }

        /// <summary>
        /// 툴스트립 설정
        /// </summary>
        private void SetupToolStrip()
        {
            _toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                ImageScalingSize = new Size(16, 16)
            };

            // 새 레이어 버튼
            var newLayerBtn = new ToolStripButton("새 레이어", null, OnNewLayer)
            {
                ToolTipText = "새 레이어 생성",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            // 레이어 삭제 버튼
            var deleteLayerBtn = new ToolStripButton("삭제", null, OnDeleteLayer)
            {
                ToolTipText = "선택된 레이어 삭제",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            // 현재 레이어 설정 버튼
            var setCurrentBtn = new ToolStripButton("현재 설정", null, OnSetCurrent)
            {
                ToolTipText = "선택된 레이어를 현재 레이어로 설정",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            // 구분자
            var separator1 = new ToolStripSeparator();

            // 필터 레이블
            var filterLabel = new ToolStripLabel("필터:");

            // 필터 텍스트박스
            _filterTextBox = new ToolStripTextBox
            {
                Size = new Size(120, 23),
                ToolTipText = "레이어명에 포함될 텍스트를 입력하세요"
            };

            // 필터 적용 버튼
            var applyFilterBtn = new ToolStripButton("적용", null, OnApplyFilter)
            {
                ToolTipText = "필터 적용",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };

            // 필터 해제 버튼
            var clearFilterBtn = new ToolStripButton("해제", null, OnClearFilter)
            {
                ToolTipText = "필터 해제",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };

            // 구분자
            var separator2 = new ToolStripSeparator();

            // 새로고침 버튼
            var refreshBtn = new ToolStripButton("새로고침", null, OnRefresh)
            {
                ToolTipText = "레이어 목록 새로고침",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            _toolStrip.Items.AddRange([
                newLayerBtn, deleteLayerBtn, setCurrentBtn, separator1,
                filterLabel, _filterTextBox, applyFilterBtn, clearFilterBtn, separator2,
                refreshBtn
            ]);

            // Enter 키로 필터 적용
            _filterTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    OnApplyFilter(s, e);
                }
            };
        }

        /// <summary>
        /// DataGridView 설정
        /// </summary>
        private void SetupDataGridView()
        {
            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 28, // 헤더 높이 증가
                RowTemplate = { Height = 26 }, // 행 높이 증가
                BackgroundColor = SystemColors.Window,
                GridColor = SystemColors.ControlLight,
                // 글자 크기 조정
                Font = new System.Drawing.Font("맑은 고딕", 9F, FontStyle.Regular)
            };

            // 컬럼 정의
            SetupColumns();

            // 데이터 바인딩
            _dataGridView.DataSource = _layerDataList;
        }

        /// <summary>
        /// DataGridView 컬럼 설정
        /// </summary>
        private void SetupColumns()
        {
            // 상태 아이콘 컬럼
            var statusColumn = new DataGridViewImageColumn
            {
                Name = "StatusIcon",
                HeaderText = "",
                DataPropertyName = "StatusIcon",
                Width = 35, // 폭 증가
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            // 레이어명 컬럼 - 폭 대폭 증가
            var nameColumn = new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "레이어명",
                DataPropertyName = "Name",
                Width = 280, // 180 -> 280으로 대폭 증가
                MinimumWidth = 200 // 120 -> 200으로 증가
            };

            // 색상 컬럼
            var colorColumn = new DataGridViewTextBoxColumn
            {
                Name = "ColorName",
                HeaderText = "색상",
                DataPropertyName = "ColorName",
                Width = 80, // 70 -> 80으로 증가
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            // 끄기 컬럼
            var offColumn = new DataGridViewCheckBoxColumn
            {
                Name = "IsOff",
                HeaderText = "끄기",
                DataPropertyName = "IsOff",
                Width = 50, // 45 -> 50으로 증가
                TrueValue = true,
                FalseValue = false
            };

            // 동결 컬럼
            var frozenColumn = new DataGridViewCheckBoxColumn
            {
                Name = "IsFrozen",
                HeaderText = "동결",
                DataPropertyName = "IsFrozen",
                Width = 50, // 45 -> 50으로 증가
                TrueValue = true,
                FalseValue = false
            };

            // 잠금 컬럼
            var lockedColumn = new DataGridViewCheckBoxColumn
            {
                Name = "IsLocked",
                HeaderText = "잠금",
                DataPropertyName = "IsLocked",
                Width = 50, // 45 -> 50으로 증가
                TrueValue = true,
                FalseValue = false
            };

            // 출력가능 컬럼
            var plottableColumn = new DataGridViewCheckBoxColumn
            {
                Name = "IsPlottable",
                HeaderText = "출력",
                DataPropertyName = "IsPlottable",
                Width = 50, // 45 -> 50으로 증가
                TrueValue = true,
                FalseValue = false
            };

            // 설명 컬럼
            var descriptionColumn = new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "설명",
                DataPropertyName = "Description",
                Width = 120, // 80 -> 120으로 증가
                MinimumWidth = 80
            };

            _dataGridView.Columns.AddRange([
                statusColumn, nameColumn, colorColumn,
                offColumn, frozenColumn, lockedColumn, plottableColumn,
                descriptionColumn
            ]);
        }

        /// <summary>
        /// 상태바 설정
        /// </summary>
        private void SetupStatusStrip()
        {
            _statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom
            };

            _statusLabel = new ToolStripStatusLabel("준비됨")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _statusStrip.Items.Add(_statusLabel);
        }

        /// <summary>
        /// 컨텍스트 메뉴 설정
        /// </summary>
        private void SetupContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            var items = new ToolStripItem[]
            {
                new ToolStripMenuItem("새 레이어", null, OnNewLayer),
                new ToolStripSeparator(),
                new ToolStripMenuItem("현재 레이어로 설정", null, OnSetCurrent),
                new ToolStripSeparator(),
                new ToolStripMenuItem("레이어 켜기", null, (s, e) => SetLayerProperty("IsOff", false)),
                new ToolStripMenuItem("레이어 끄기", null, (s, e) => SetLayerProperty("IsOff", true)),
                new ToolStripSeparator(),
                new ToolStripMenuItem("레이어 동결", null, (s, e) => SetLayerProperty("IsFrozen", true)),
                new ToolStripMenuItem("레이어 해동", null, (s, e) => SetLayerProperty("IsFrozen", false)),
                new ToolStripSeparator(),
                new ToolStripMenuItem("레이어 잠금", null, (s, e) => SetLayerProperty("IsLocked", true)),
                new ToolStripMenuItem("레이어 해제", null, (s, e) => SetLayerProperty("IsLocked", false)),
                new ToolStripSeparator(),
                new ToolStripMenuItem("레이어 속성...", null, OnLayerProperties),
                new ToolStripSeparator(),
                new ToolStripMenuItem("레이어 삭제", null, OnDeleteLayer)
            };

            _contextMenu.Items.AddRange(items);
            _dataGridView.ContextMenuStrip = _contextMenu;
        }

        /// <summary>
        /// 이벤트 핸들러 설정
        /// </summary>
        private void SetupEventHandlers()
        {
            // 문서 변경 감지
            Application.DocumentManager.DocumentActivated += (s, e) =>
            {
                BeginInvoke(RefreshLayers);
            };

            // DataGridView 이벤트들 - 수정된 부분
            _dataGridView.CellValueChanged += OnCellValueChanged;
            _dataGridView.CurrentCellDirtyStateChanged += OnCurrentCellDirtyStateChanged; // 체크박스용 추가
            _dataGridView.CellDoubleClick += OnCellDoubleClick;
            _dataGridView.SelectionChanged += OnSelectionChanged;
            _dataGridView.CellFormatting += OnCellFormatting;
            _dataGridView.DataError += OnDataError;
        }

        /// <summary>
        /// 레이어 목록 새로고침
        /// </summary>
        public void RefreshLayers()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                UpdateStatus("문서가 활성화되지 않음");
                _layerDataList.Clear();
                _allLayerData.Clear();
                return;
            }

            try
            {
                var layerData = new List<LayerGridData>();

                using var tr = doc.TransactionManager.StartTransaction();
                if (tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) is LayerTable layerTable)
                {
                    var currentLayerId = doc.Database.Clayer;

                    foreach (ObjectId layerId in layerTable)
                    {
                        if (tr.GetObject(layerId, OpenMode.ForRead) is LayerTableRecord layer)
                        {
                            var isCurrent = layerId == currentLayerId;
                            var data = new LayerGridData
                            {
                                LayerId = layerId,
                                Name = layer.Name,
                                ColorName = GetColorName(layer.Color),
                                ColorIndex = layer.Color.ColorIndex,
                                IsOff = layer.IsOff,
                                IsFrozen = layer.IsFrozen,
                                IsLocked = layer.IsLocked,
                                IsPlottable = layer.IsPlottable,
                                IsCurrent = isCurrent,
                                Description = layer.Description,
                                StatusIcon = GetLayerStatusIcon(layer, isCurrent)
                            };

                            layerData.Add(data);
                        }
                    }
                }

                tr.Commit();

                // 전체 데이터 보관 (정렬된 상태로)
                _allLayerData = layerData.OrderBy(l => l.IsCurrent ? 0 : 1)
                                        .ThenBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                                        .ToList();

                // 필터 적용된 데이터 표시
                ApplyCurrentFilter();

                UpdateStatus($"전체 레이어 {_allLayerData.Count}개 로드됨");
            }
            catch (System.Exception ex)
            {
                var ed = doc.Editor;
                ed?.WriteMessage($"\n레이어 새로고침 오류: {ex.Message}");
                UpdateStatus($"오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 필터 적용
        /// </summary>
        private void ApplyCurrentFilter()
        {
            var filterText = _filterTextBox?.Text?.Trim();

            _layerDataList.Clear();

            if (string.IsNullOrWhiteSpace(filterText))
            {
                // 필터가 없으면 전체 표시
                foreach (var layer in _allLayerData)
                {
                    _layerDataList.Add(layer);
                }
            }
            else
            {
                // 필터가 있으면 해당하는 레이어만 표시
                var filteredData = _allLayerData.Where(l =>
                    l.Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                foreach (var layer in filteredData)
                {
                    _layerDataList.Add(layer);
                }

                UpdateStatus($"필터 적용됨: '{filterText}' - {_layerDataList.Count}개 레이어 표시 (전체: {_allLayerData.Count}개)");
            }
        }

        /// <summary>
        /// 색상명 가져오기
        /// </summary>
        private string GetColorName(Color color)
        {
            var colorNames = new Dictionary<short, string>
            {
                [1] = "빨강",
                [2] = "노랑",
                [3] = "녹색",
                [4] = "청록",
                [5] = "파랑",
                [6] = "자홍",
                [7] = "검정/흰색",
                [8] = "회색",
                [9] = "밝은회색",
                [10] = "진한빨강",
                [11] = "분홍",
                [12] = "갈색"
            };

            return colorNames.TryGetValue(color.ColorIndex, out var name) ? name : $"ACI-{color.ColorIndex}";
        }

        /// <summary>
        /// 레이어 상태 아이콘 가져오기
        /// </summary>
        private Image GetLayerStatusIcon(LayerTableRecord layer, bool isCurrent)
        {
            int index;
            if (isCurrent) index = LAYER_CURRENT;
            else if (layer.IsOff) index = LAYER_OFF;
            else if (layer.IsFrozen) index = LAYER_FROZEN;
            else if (layer.IsLocked) index = LAYER_LOCKED;
            else index = LAYER_ON;

            return _imageList.Images[index];
        }

        /// <summary>
        /// 상태바 업데이트
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = message;
            }
        }

        #region 필터 관련 이벤트 핸들러

        private void OnApplyFilter(object? sender, EventArgs e)
        {
            ApplyCurrentFilter();
        }

        private void OnClearFilter(object? sender, EventArgs e)
        {
            _filterTextBox.Text = "";
            ApplyCurrentFilter();
            UpdateStatus($"필터 해제됨 - 전체 {_allLayerData.Count}개 레이어 표시");
        }

        #endregion

        #region DataGridView 이벤트 핸들러

        private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var layerData = _layerDataList[e.RowIndex];
            var columnName = _dataGridView.Columns[e.ColumnIndex].Name;

            // 체크박스 컬럼들 처리
            if (columnName is "IsOff" or "IsFrozen" or "IsLocked" or "IsPlottable")
            {
                UpdateLayerProperty(layerData.LayerId, columnName, _dataGridView[e.ColumnIndex, e.RowIndex].Value);
            }
            // 레이어명 변경 처리
            else if (columnName == "Name")
            {
                var newName = _dataGridView[e.ColumnIndex, e.RowIndex].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    UpdateLayerName(layerData.LayerId, newName);
                }
            }
            // 설명 변경 처리
            else if (columnName == "Description")
            {
                var newDescription = _dataGridView[e.ColumnIndex, e.RowIndex].Value?.ToString() ?? "";
                UpdateLayerDescription(layerData.LayerId, newDescription);
            }
        }

        // 체크박스 변경 즉시 감지를 위한 추가 이벤트 핸들러
        private void OnCurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (_dataGridView.IsCurrentCellDirty)
            {
                // 체크박스 컬럼인 경우 즉시 커밋
                var currentColumn = _dataGridView.CurrentCell?.ColumnIndex ?? -1;
                if (currentColumn >= 0)
                {
                    var columnName = _dataGridView.Columns[currentColumn].Name;
                    if (columnName is "IsOff" or "IsFrozen" or "IsLocked" or "IsPlottable")
                    {
                        _dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    }
                }
            }
        }

        private void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var layerData = _layerDataList[e.RowIndex];
                SetCurrentLayer(layerData.LayerId);
            }
        }

        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            if (_dataGridView.SelectedRows.Count > 0)
            {
                var selectedRow = _dataGridView.SelectedRows[0];
                if (selectedRow.DataBoundItem is LayerGridData layerData)
                {
                    UpdateStatus($"선택된 레이어: {layerData.Name}");
                }
            }
        }

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _layerDataList.Count)
            {
                var layerData = _layerDataList[e.RowIndex];

                // 현재 레이어는 굵게 표시
                if (layerData.IsCurrent)
                {
                    e.CellStyle.Font = new System.Drawing.Font(_dataGridView.DefaultCellStyle.Font, FontStyle.Bold);
                    e.CellStyle.BackColor = SystemColors.Info;
                }

                // 색상 컬럼의 텍스트를 해당 색상으로 표시
                if (_dataGridView.Columns[e.ColumnIndex].Name == "ColorName")
                {
                    e.CellStyle.ForeColor = ConvertToSystemColor(layerData.ColorIndex);
                }
            }
        }

        private void OnDataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            UpdateStatus($"데이터 오류: {e.Exception?.Message ?? "알 수 없는 오류"}");
            e.Cancel = true;
        }

        #endregion

        #region 툴바 이벤트 핸들러

        private void OnNewLayer(object? sender, EventArgs e)
        {
            CreateNewLayer();
        }

        private void OnDeleteLayer(object? sender, EventArgs e)
        {
            DeleteSelectedLayer();
        }

        private void OnSetCurrent(object? sender, EventArgs e)
        {
            SetSelectedLayerAsCurrent();
        }

        private void OnRefresh(object? sender, EventArgs e)
        {
            RefreshLayers();
        }

        private void OnLayerProperties(object? sender, EventArgs e)
        {
            ShowLayerProperties();
        }

        #endregion

        #region 레이어 관리 기능

        /// <summary>
        /// 새 레이어 생성
        /// </summary>
        private void CreateNewLayer()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                var dialog = new LayerNameDialog();
                if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.LayerName))
                {
                    return;
                }

                var layerName = dialog.LayerName.Trim();

                // Document Lock 추가
                using (var docLock = doc.LockDocument())
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    if (tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) is LayerTable layerTable)
                    {
                        if (layerTable.Has(layerName))
                        {
                            MessageBox.Show("같은 이름의 레이어가 이미 존재합니다.", "오류",
                                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            tr.Commit();
                            return;
                        }

                        layerTable.UpgradeOpen();

                        var newLayer = new LayerTableRecord
                        {
                            Name = layerName,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, 7), // White
                            LinetypeObjectId = doc.Database.ContinuousLinetype
                        };

                        var layerId = layerTable.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);

                        tr.Commit();

                        RefreshLayers();

                        var ed = doc.Editor;
                        ed.WriteMessage($"\n새 레이어 '{layerName}'이 생성되었습니다.");
                        UpdateStatus($"레이어 '{layerName}' 생성됨");
                    }
                }
            }
            catch (System.Exception ex)
            {
                var ed = doc.Editor;
                ed?.WriteMessage($"\n레이어 생성 오류: {ex.Message}");
                UpdateStatus($"레이어 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택된 레이어 삭제
        /// </summary>
        private void DeleteSelectedLayer()
        {
            if (_dataGridView.SelectedRows.Count == 0) return;

            var selectedRow = _dataGridView.SelectedRows[0];
            if (selectedRow.DataBoundItem is not LayerGridData layerData) return;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Document Lock 추가
                using (var docLock = doc.LockDocument())
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    if (tr.GetObject(layerData.LayerId, OpenMode.ForRead) is LayerTableRecord layer)
                    {
                        if (layer.Name == "0" || layer.Name.ToUpper() == "DEFPOINTS")
                        {
                            MessageBox.Show("기본 레이어는 삭제할 수 없습니다.", "오류",
                                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            tr.Commit();
                            return;
                        }

                        if (layerData.IsCurrent)
                        {
                            MessageBox.Show("현재 레이어는 삭제할 수 없습니다.", "오류",
                                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            tr.Commit();
                            return;
                        }

                        var result = MessageBox.Show(
                            $"레이어 '{layer.Name}'을 삭제하시겠습니까?",
                            "레이어 삭제",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            layer.UpgradeOpen();
                            layer.Erase();
                            tr.Commit();

                            RefreshLayers();

                            var ed = doc.Editor;
                            ed.WriteMessage($"\n레이어 '{layer.Name}'이 삭제되었습니다.");
                            UpdateStatus($"레이어 '{layer.Name}' 삭제됨");
                        }
                        else
                        {
                            tr.Commit();
                        }
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                var ed = doc.Editor;
                ed?.WriteMessage($"\n레이어 삭제 오류: {ex.Message}");
                UpdateStatus($"삭제 오류: {ex.Message}");

                //if (ex.ErrorStatus == ErrorStatus.HasAssociatedObjects)
                //{
                //    MessageBox.Show("이 레이어는 객체가 포함되어 있어 삭제할 수 없습니다.", "오류",
                //                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    UpdateStatus("삭제 실패: 레이어에 객체가 있음");
                //}
            }
        }

        /// <summary>
        /// 선택된 레이어를 현재 레이어로 설정
        /// </summary>
        private void SetSelectedLayerAsCurrent()
        {
            if (_dataGridView.SelectedRows.Count == 0) return;

            var selectedRow = _dataGridView.SelectedRows[0];
            if (selectedRow.DataBoundItem is LayerGridData layerData)
            {
                SetCurrentLayer(layerData.LayerId);
            }
        }

        /// <summary>
        /// 현재 레이어로 설정
        /// </summary>
        private void SetCurrentLayer(ObjectId layerId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Document Lock 추가
                using (var docLock = doc.LockDocument())
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    if (tr.GetObject(layerId, OpenMode.ForRead) is LayerTableRecord layer)
                    {
                        doc.Database.Clayer = layerId;
                        tr.Commit();

                        var ed = doc.Editor;
                        ed.WriteMessage($"\n현재 레이어가 '{layer.Name}'으로 설정되었습니다.");
                        UpdateStatus($"현재 레이어: {layer.Name}");

                        RefreshLayers();
                    }
                }
            }
            catch (System.Exception ex)
            {
                var ed = doc.Editor;
                ed?.WriteMessage($"\n레이어 설정 오류: {ex.Message}");
                UpdateStatus($"레이어 설정 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 레이어 속성 업데이트
        /// </summary>
        private void UpdateLayerProperty(ObjectId layerId, string propertyName, object? value)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Document Lock 추가
                using (var docLock = doc.LockDocument())
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    if (tr.GetObject(layerId, OpenMode.ForWrite) is LayerTableRecord layer)
                    {
                        switch (propertyName)
                        {
                            case "IsOff":
                                layer.IsOff = (bool)(value ?? false);
                                break;
                            case "IsFrozen":
                                layer.IsFrozen = (bool)(value ?? false);
                                break;
                            case "IsLocked":
                                layer.IsLocked = (bool)(value ?? false);
                                break;
                            case "IsPlottable":
                                layer.IsPlottable = (bool)(value ?? false);
                                break;
                        }

                        tr.Commit();

                        // 전체 데이터와 표시 데이터 모두 업데이트
                        var allLayerData = _allLayerData.FirstOrDefault(l => l.LayerId == layerId);
                        var displayLayerData = _layerDataList.FirstOrDefault(l => l.LayerId == layerId);

                        if (allLayerData != null)
                        {
                            allLayerData.StatusIcon = GetLayerStatusIcon(layer, allLayerData.IsCurrent);

                            // 속성 값도 업데이트
                            switch (propertyName)
                            {
                                case "IsOff": allLayerData.IsOff = layer.IsOff; break;
                                case "IsFrozen": allLayerData.IsFrozen = layer.IsFrozen; break;
                                case "IsLocked": allLayerData.IsLocked = layer.IsLocked; break;
                                case "IsPlottable": allLayerData.IsPlottable = layer.IsPlottable; break;
                            }
                        }

                        if (displayLayerData != null)
                        {
                            displayLayerData.StatusIcon = GetLayerStatusIcon(layer, displayLayerData.IsCurrent);
                            var index = _layerDataList.IndexOf(displayLayerData);
                            if (index >= 0)
                            {
                                _dataGridView.InvalidateRow(index);
                            }
                        }

                        var ed = doc.Editor;
                        ed.WriteMessage($"\n레이어 '{layer.Name}' 속성이 변경되었습니다.");
                        UpdateStatus($"레이어 '{layer.Name}' 속성 변경됨");
                    }
                }
            }
            catch (System.Exception ex)
            {
                var ed = doc.Editor;
                ed?.WriteMessage($"\n레이어 속성 변경 오류: {ex.Message}");
                UpdateStatus($"속성 변경 오류: {ex.Message}");
                RefreshLayers(); // 오류 발생 시 전체 새로고침
            }
        }

        /// <summary>
        /// 레이어명 변경
        /// </summary>
        private void UpdateLayerName(ObjectId layerId, string newName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Document Lock 추가
                using (var docLock = doc.LockDocument())
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    if (tr.GetObject(layerId, OpenMode.ForWrite) is LayerTableRecord layer)
                    {
                        if (layer.Name == "0")
                        {
                            MessageBox.Show("기본 레이어의 이름은 변경할 수 없습니다.", "오류",
                                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            RefreshLayers();
                            tr.Commit();
                            return;
                        }

                        var oldName = layer.Name;
                        layer.Name = newName;
                        tr.Commit();

                        var ed = doc.Editor;
                        ed.WriteMessage($"\n레이어 이름이 '{oldName}'에서 '{newName}'으로 변경되었습니다.");
                        UpdateStatus($"레이어 이름 변경: '{oldName}' → '{newName}'");

                        // 이름 변경 후 새로고침
                        RefreshLayers();
                    }
                }
            }
            catch (System.Exception ex)
            {
                var ed = doc.Editor;
                ed?.WriteMessage($"\n레이어 이름 변경 오류: {ex.Message}");
                UpdateStatus($"이름 변경 오류: {ex.Message}");
                RefreshLayers(); // 오류 발생 시 원래 이름으로 복구
            }
        }

        /// <summary>
        /// 레이어 설명 변경
        /// </summary>
        private void UpdateLayerDescription(ObjectId layerId, string newDescription)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Document Lock 추가
                using (var docLock = doc.LockDocument())
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    if (tr.GetObject(layerId, OpenMode.ForWrite) is LayerTableRecord layer)
                    {
                        layer.Description = newDescription;
                        tr.Commit();

                        // 전체 데이터와 표시 데이터의 설명도 업데이트
                        var allLayerData = _allLayerData.FirstOrDefault(l => l.LayerId == layerId);
                        if (allLayerData != null)
                        {
                            allLayerData.Description = newDescription;
                        }

                        var ed = doc.Editor;
                        ed.WriteMessage($"\n레이어 '{layer.Name}' 설명이 변경되었습니다.");
                        UpdateStatus($"레이어 '{layer.Name}' 설명 변경됨");
                    }
                }
            }
            catch (System.Exception ex)
            {
                var ed = doc.Editor;
                ed?.WriteMessage($"\n레이어 설명 변경 오류: {ex.Message}");
                UpdateStatus($"설명 변경 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 레이어 속성 설정 (컨텍스트 메뉴용)
        /// </summary>
        private void SetLayerProperty(string propertyName, bool value)
        {
            if (_dataGridView.SelectedRows.Count == 0) return;

            var selectedRow = _dataGridView.SelectedRows[0];
            if (selectedRow.DataBoundItem is LayerGridData layerData)
            {
                UpdateLayerProperty(layerData.LayerId, propertyName, value);
            }
        }

        /// <summary>
        /// 레이어 속성 대화상자 표시
        /// </summary>
        private void ShowLayerProperties()
        {
            if (_dataGridView.SelectedRows.Count == 0) return;

            var selectedRow = _dataGridView.SelectedRows[0];
            if (selectedRow.DataBoundItem is not LayerGridData layerData) return;

            try
            {
                var dialog = new LayerPropertiesDialog(layerData.LayerId);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RefreshLayers();
                    UpdateStatus("레이어 속성 변경됨");
                }
            }
            catch (System.Exception ex)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                var ed = doc?.Editor;
                ed?.WriteMessage($"\n레이어 속성 대화상자 오류: {ex.Message}");
                UpdateStatus($"속성 대화상자 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// AutoCAD Color Index를 System.Drawing.Color로 변환
        /// </summary>
        private System.Drawing.Color ConvertToSystemColor(short colorIndex)
        {
            try
            {
                var colorMap = new Dictionary<short, System.Drawing.Color>
                {
                    [1] = System.Drawing.Color.Red,
                    [2] = System.Drawing.Color.Yellow,
                    [3] = System.Drawing.Color.Green,
                    [4] = System.Drawing.Color.Cyan,
                    [5] = System.Drawing.Color.Blue,
                    [6] = System.Drawing.Color.Magenta,
                    [7] = System.Drawing.Color.Black,
                    [8] = System.Drawing.Color.DarkGray,
                    [9] = System.Drawing.Color.LightGray,
                    [10] = System.Drawing.Color.DarkRed,
                    [11] = System.Drawing.Color.Pink,
                    [12] = System.Drawing.Color.Brown
                };

                return colorMap.TryGetValue(colorIndex, out var color) ? color : System.Drawing.Color.Black;
            }
            catch
            {
                return System.Drawing.Color.Black;
            }
        }

        #endregion
    }

    /// <summary>
    /// DataGridView용 레이어 데이터 클래스
    /// </summary>
    public class LayerGridData
    {
        public ObjectId LayerId { get; set; }
        public string Name { get; set; } = "";
        public string ColorName { get; set; } = "";
        public short ColorIndex { get; set; }
        public bool IsOff { get; set; }
        public bool IsFrozen { get; set; }
        public bool IsLocked { get; set; }
        public bool IsPlottable { get; set; }
        public bool IsCurrent { get; set; }
        public string Description { get; set; } = "";
        public Image StatusIcon { get; set; } = null!;
    }

    /// <summary>
    /// 레이어명 입력 대화상자
    /// </summary>
    public partial class LayerNameDialog : Form
    {
        private TextBox _nameTextBox = null!;

        public string LayerName => _nameTextBox.Text;

        public LayerNameDialog()
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Text = "새 레이어";
            Size = new Size(300, 120);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var label = new Label
            {
                Text = "레이어 이름:",
                Location = new Point(10, 15),
                Size = new Size(80, 20)
            };

            _nameTextBox = new TextBox
            {
                Location = new Point(100, 12),
                Size = new Size(170, 20),
                Text = $"Layer{DateTime.Now.Ticks % 1000}"
            };

            var okBtn = new Button
            {
                Text = "확인",
                Location = new Point(115, 50),
                Size = new Size(75, 25),
                DialogResult = DialogResult.OK
            };

            var cancelBtn = new Button
            {
                Text = "취소",
                Location = new Point(195, 50),
                Size = new Size(75, 25),
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange([label, _nameTextBox, okBtn, cancelBtn]);

            AcceptButton = okBtn;
            CancelButton = cancelBtn;

            _nameTextBox.SelectAll();
            _nameTextBox.Focus();
        }
    }

    /// <summary>
    /// 레이어 속성 대화상자
    /// </summary>
    public partial class LayerPropertiesDialog : Form
    {
        private readonly ObjectId _layerId;
        private TextBox _nameTextBox = null!;
        private TextBox _descriptionTextBox = null!;
        private ComboBox _colorComboBox = null!;
        private CheckBox _isOffCheckBox = null!;
        private CheckBox _isFrozenCheckBox = null!;
        private CheckBox _isLockedCheckBox = null!;
        private CheckBox _isPlottableCheckBox = null!;

        public LayerPropertiesDialog(ObjectId layerId)
        {
            _layerId = layerId;
            InitializeDialog();
            LoadLayerProperties();
        }

        private void InitializeDialog()
        {
            Text = "레이어 속성";
            Size = new Size(400, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(10)
            };

            // 레이어명
            panel.Controls.Add(new Label { Text = "이름:", Anchor = AnchorStyles.Left }, 0, 0);
            _nameTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            panel.Controls.Add(_nameTextBox, 1, 0);

            // 설명
            panel.Controls.Add(new Label { Text = "설명:", Anchor = AnchorStyles.Left }, 0, 1);
            _descriptionTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            panel.Controls.Add(_descriptionTextBox, 1, 1);

            // 색상
            panel.Controls.Add(new Label { Text = "색상:", Anchor = AnchorStyles.Left }, 0, 2);
            _colorComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            SetupColorComboBox();
            panel.Controls.Add(_colorComboBox, 1, 2);

            // 체크박스들
            _isOffCheckBox = new CheckBox { Text = "끄기" };
            panel.Controls.Add(_isOffCheckBox, 1, 3);

            _isFrozenCheckBox = new CheckBox { Text = "동결" };
            panel.Controls.Add(_isFrozenCheckBox, 1, 4);

            _isLockedCheckBox = new CheckBox { Text = "잠금" };
            panel.Controls.Add(_isLockedCheckBox, 1, 5);

            _isPlottableCheckBox = new CheckBox { Text = "출력 가능" };
            panel.Controls.Add(_isPlottableCheckBox, 1, 6);

            // 버튼들
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 35
            };

            var cancelBtn = new Button { Text = "취소", DialogResult = DialogResult.Cancel };
            var okBtn = new Button { Text = "확인", DialogResult = DialogResult.OK };
            okBtn.Click += OnOkClick;

            buttonPanel.Controls.Add(cancelBtn);
            buttonPanel.Controls.Add(okBtn);

            Controls.Add(panel);
            Controls.Add(buttonPanel);
        }

        private void SetupColorComboBox()
        {
            var colors = new Dictionary<int, string>
            {
                [1] = "빨강",
                [2] = "노랑",
                [3] = "녹색",
                [4] = "청록",
                [5] = "파랑",
                [6] = "자홍",
                [7] = "검정/흰색",
                [8] = "진한회색",
                [9] = "밝은회색",
                [10] = "진한빨강",
                [11] = "분홍",
                [12] = "갈색"
            };

            _colorComboBox.DisplayMember = "Value";
            _colorComboBox.ValueMember = "Key";
            _colorComboBox.DataSource = colors.ToArray();
        }

        private void LoadLayerProperties()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                using var tr = doc.TransactionManager.StartTransaction();
                if (tr.GetObject(_layerId, OpenMode.ForRead) is LayerTableRecord layer)
                {
                    _nameTextBox.Text = layer.Name;
                    _descriptionTextBox.Text = layer.Description;
                    _colorComboBox.SelectedValue = (int)layer.Color.ColorIndex;
                    _isOffCheckBox.Checked = layer.IsOff;
                    _isFrozenCheckBox.Checked = layer.IsFrozen;
                    _isLockedCheckBox.Checked = layer.IsLocked;
                    _isPlottableCheckBox.Checked = layer.IsPlottable;
                }
                tr.Commit();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"레이어 속성 로드 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Document Lock 추가
                using (var docLock = doc.LockDocument())
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    if (tr.GetObject(_layerId, OpenMode.ForWrite) is LayerTableRecord layer)
                    {
                        // 레이어명은 기본 레이어가 아닐 때만 변경 허용
                        if (layer.Name != "0" && !string.IsNullOrWhiteSpace(_nameTextBox.Text))
                        {
                            layer.Name = _nameTextBox.Text.Trim();
                        }

                        layer.Description = _descriptionTextBox.Text.Trim();

                        if (_colorComboBox.SelectedValue is int colorIndex)
                        {
                            layer.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);
                        }

                        layer.IsOff = _isOffCheckBox.Checked;
                        layer.IsFrozen = _isFrozenCheckBox.Checked;
                        layer.IsLocked = _isLockedCheckBox.Checked;
                        layer.IsPlottable = _isPlottableCheckBox.Checked;

                        tr.Commit();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"레이어 속성 저장 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
    }
}