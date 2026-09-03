using System.ComponentModel;

namespace JArchLicenseAdmin;

public partial class MainForm : Form
{
    private readonly SupabaseLicenseClient _client;
    private readonly BindingList<License> _view = new();
    private List<License> _all = new();

    public MainForm(SupabaseLicenseClient client)
    {
        _client = client;
        InitializeComponent();

        // 그리드 컬럼은 Designer 가 아니라 여기서 만든다.
        // VS Designer 를 열었다 저장하면 코드로 추가한 컬럼을 통째로 날려먹는 전례가 있다.
        BuildGridColumns();

        grid.AutoGenerateColumns = false;
        grid.DataSource = _view;
        grid.SelectionChanged += grid_SelectionChanged;
    }

    private void BuildGridColumns()
    {
        grid.Columns.Clear();
        AddColumn(nameof(License.ComId),    "라이선스 ID", 160);
        AddColumn(nameof(License.ExpDate),  "사용 기한",   110);
        AddColumn(nameof(License.RegDate),  "등록일",      110);
        AddColumn(nameof(License.UserName), "사용자",      120);
        AddColumn(nameof(License.CompName), "회사명",      160);
        AddColumn(nameof(License.PartName), "부서명",      120);

        // 마지막 컬럼이 남는 폭을 먹게 해서 오른쪽에 빈 회색 영역이 남지 않게 한다.
        grid.Columns[^1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    private void AddColumn(string property, string header, int width)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = property,
            DataPropertyName = property,
            HeaderText = header,
            Width = width,
            SortMode = DataGridViewColumnSortMode.Automatic,
        });
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        dtpExpDate.Value = DateTime.Today.AddYears(1);
        await ReloadAsync();
    }

    private async void btnRefresh_Click(object? sender, EventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            SetBusy(true, "조회 중...");
            _all = await _client.ListAsync();
            ApplyFilter();
            SetStatus($"{_all.Count}건 조회됨 — {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception ex)
        {
            ShowError("조회", ex);
            SetStatus("조회 실패");
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private void txtSearch_TextChanged(object? sender, EventArgs e) => ApplyFilter();

    /// <summary>
    /// 서버가 아니라 이미 받아 온 목록에서 거른다. 라이선스 건수는 많지 않고,
    /// PostgREST 의 or=(...) 필터는 값에 쉼표·괄호가 섞이면 깨지기 쉽다.
    /// </summary>
    private void ApplyFilter()
    {
        string q = txtSearch.Text.Trim();

        _view.RaiseListChangedEvents = false;
        _view.Clear();
        foreach (License row in _all)
        {
            if (q.Length == 0 || Matches(row, q))
                _view.Add(row);
        }
        _view.RaiseListChangedEvents = true;
        _view.ResetBindings();
    }

    private static bool Matches(License row, string q)
        => Contains(row.ComId, q) || Contains(row.UserName, q)
        || Contains(row.CompName, q) || Contains(row.PartName, q);

    private static bool Contains(string? value, string q)
        => value != null && value.Contains(q, StringComparison.OrdinalIgnoreCase);

    private void grid_SelectionChanged(object? sender, EventArgs e)
    {
        if (grid.CurrentRow?.DataBoundItem is not License row)
            return;

        txtComId.Text = row.ComId;
        txtUserName.Text = row.UserName ?? "";
        txtCompName.Text = row.CompName ?? "";
        txtPartName.Text = row.PartName ?? "";
        lblRegDateValue.Text = row.RegDate ?? "-";

        if (DateTime.TryParse(row.ExpDate, out DateTime exp))
            dtpExpDate.Value = exp;
    }

    private void btnNew_Click(object? sender, EventArgs e)
    {
        grid.ClearSelection();
        txtComId.Clear();
        txtUserName.Clear();
        txtCompName.Clear();
        txtPartName.Clear();
        dtpExpDate.Value = DateTime.Today.AddYears(1);
        lblRegDateValue.Text = "(추가 시 서버가 채움)";
        txtComId.Focus();
        SetStatus("새 입력");
    }

    private async void btnCreate_Click(object? sender, EventArgs e)
    {
        if (!TryReadForm(out License row))
            return;

        try
        {
            SetBusy(true, "추가 중...");
            await _client.CreateAsync(row);
            await ReloadAsync();
            SelectRow(row.ComId);
            SetStatus($"'{row.ComId}' 추가됨");
        }
        catch (Exception ex)
        {
            ShowError("추가", ex);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private async void btnUpdate_Click(object? sender, EventArgs e)
    {
        if (!TryReadForm(out License row))
            return;

        try
        {
            SetBusy(true, "수정 중...");
            await _client.UpdateAsync(row.ComId, row);
            await ReloadAsync();
            SelectRow(row.ComId);
            SetStatus($"'{row.ComId}' 수정됨");
        }
        catch (Exception ex)
        {
            ShowError("수정", ex);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private async void btnDelete_Click(object? sender, EventArgs e)
    {
        string comId = txtComId.Text.Trim();
        if (comId.Length == 0)
        {
            MessageBox.Show(this, "삭제할 라이선스 ID 를 선택하거나 입력하세요.",
                "삭제", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult answer = MessageBox.Show(this,
            $"'{comId}' 를 삭제합니다.\r\n되돌릴 수 없습니다. 계속할까요?",
            "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
            return;

        try
        {
            SetBusy(true, "삭제 중...");
            await _client.DeleteAsync(comId);
            await ReloadAsync();
            SetStatus($"'{comId}' 삭제됨");
        }
        catch (Exception ex)
        {
            ShowError("삭제", ex);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private bool TryReadForm(out License row)
    {
        row = new License
        {
            ComId = txtComId.Text.Trim(),
            ExpDate = dtpExpDate.Value.ToString("yyyy-MM-dd"),
            UserName = txtUserName.Text,
            CompName = txtCompName.Text,
            PartName = txtPartName.Text,
        };

        if (row.ComId.Length == 0)
        {
            MessageBox.Show(this, "라이선스 ID 는 반드시 입력해야 합니다.",
                "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtComId.Focus();
            return false;
        }

        return true;
    }

    private void SelectRow(string comId)
    {
        foreach (DataGridViewRow gridRow in grid.Rows)
        {
            if (gridRow.DataBoundItem is License row && row.ComId == comId)
            {
                gridRow.Selected = true;
                grid.CurrentCell = gridRow.Cells[0];
                return;
            }
        }
    }

    private void SetBusy(bool busy, string? message)
    {
        pnlEdit.Enabled = !busy;
        pnlTop.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        if (message != null)
            SetStatus(message);
    }

    private void SetStatus(string message) => lblStatus.Text = message;

    private void ShowError(string what, Exception ex)
        => MessageBox.Show(this, ex.Message, $"{what} 실패",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
}
