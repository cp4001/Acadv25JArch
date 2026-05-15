using DuctSizing.Core;

namespace DuctSizing1;

public partial class MainForm : Form
{
    private const int SimulMaxCombos = 10;

    private List<RectCombination> _combos = new();
    private string? _sortColumn;
    private bool _sortAscending = true;

    public MainForm()
    {
        InitializeComponent();
        WireUp();
        LoadStandardSizes();
    }

    private void WireUp()
    {
        btnCalc.Click += OnCalcClick;
        rbModeA.CheckedChanged += OnModeChanged;
        rbModeB.CheckedChanged += OnModeChanged;
        dgvCombos.ColumnHeaderMouseClick += OnColumnHeaderClick;
        btnSimul.Click += OnSimulClick;
        OnModeChanged(this, EventArgs.Empty); // 초기 컬럼 가시성 동기화
    }

    private void LoadStandardSizes()
    {
        foreach (var v in StandardSizes.Default) cmbShortSide.Items.Add(v);
        var idx = cmbShortSide.Items.IndexOf(500);
        cmbShortSide.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void OnModeChanged(object? sender, EventArgs e)
    {
        cmbShortSide.Enabled = rbModeA.Checked;
        colDeEq.Visible = rbModeA.Checked; // Mode B 선택 시 De′ 컬럼 숨김
    }

    private void OnCalcClick(object? sender, EventArgs e)
    {
        var q = (double)numQ.Value;
        var ductType = (DuctType)cmbDuctType.SelectedIndex;
        var alpha = (double)numAlpha.Value;
        var aspectMax = (double)numAspect.Value;

        DuctSizingResult result;
        if (rbModeA.Checked)
        {
            var b = (int)cmbShortSide.SelectedItem!;
            result = DuctSizingCalculator.ModeA(q, ductType, alpha, b);
        }
        else
        {
            result = DuctSizingCalculator.ModeB(q, ductType, alpha, aspectMax);
        }

        lblDe.Text = $"등가직경 De = {result.De:0} mm";
        lblD.Text = $"원형 D = {result.D} mm";
        lblA.Text = $"단면적 A = {result.Aux.Area:0.000} m²";
        lblV.Text = $"풍속 V = {result.Aux.Velocity:0.000} m/s";
        lblDp.Text = $"동압 Δp = {result.Aux.DynamicPressure:0.000} mmAq";
        lblRp.Text = $"실저항 R′ = {result.Aux.ActualResistance:0.000} mmAq/m";

        _combos = result.Combinations.ToList();
        _sortColumn = null;
        BindCombos();
    }

    private void OnColumnHeaderClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0) return;
        var prop = dgvCombos.Columns[e.ColumnIndex].DataPropertyName;
        if (string.IsNullOrEmpty(prop)) return;

        if (_sortColumn == prop) _sortAscending = !_sortAscending;
        else { _sortColumn = prop; _sortAscending = true; }

        BindCombos();
    }

    private void OnSimulClick(object? sender, EventArgs e)
    {
        var alpha = (double)numAlpha.Value;
        var aspectMax = (double)numAspect.Value;

        Cursor = Cursors.WaitCursor;
        treeSimul.BeginUpdate();
        try
        {
            treeSimul.Nodes.Clear();
            for (int q = 100; q <= 24_000; q += 100)
            {
                var qNode = treeSimul.Nodes.Add($"Q = {q:N0} CMH");
                AddTypeNode(qNode, q, DuctType.Suction, "흡입", alpha, aspectMax);
                AddTypeNode(qNode, q, DuctType.Discharge, "토출", alpha, aspectMax);
            }
        }
        finally
        {
            treeSimul.EndUpdate();
            Cursor = Cursors.Default;
        }
    }

    private static void AddTypeNode(TreeNode parent, int q, DuctType type, string label, double alpha, double aspectMax)
    {
        var r = DuctResistance.Of(type);
        var result = DuctSizingCalculator.ModeB(q, type, alpha, aspectMax);
        var header = $"{label} (R={r:0.00}) — De={result.De:0} mm, 원형 D={result.D} mm";
        var typeNode = parent.Nodes.Add(header);

        if (result.Combinations.Count == 0)
        {
            typeNode.Nodes.Add("(조건 만족 조합 없음)");
            return;
        }

        foreach (var c in result.Combinations.Take(SimulMaxCombos))
        {
            typeNode.Nodes.Add($"{c.B} × {c.A} mm | De′={c.DeEq:0} | a/b={c.Aspect:0.00} | 면적={c.Area:0.000} m²");
        }
        if (result.Combinations.Count > SimulMaxCombos)
        {
            typeNode.Nodes.Add($"… 외 {result.Combinations.Count - SimulMaxCombos}개 (상위 {SimulMaxCombos}만 표시)");
        }
    }

    private void BindCombos()
    {
        if (_sortColumn != null)
        {
            _combos.Sort((x, y) =>
            {
                int cmp = _sortColumn switch
                {
                    "B" => x.B.CompareTo(y.B),
                    "A" => x.A.CompareTo(y.A),
                    "DeEq" => x.DeEq.CompareTo(y.DeEq),
                    "Aspect" => x.Aspect.CompareTo(y.Aspect),
                    "Area" => x.Area.CompareTo(y.Area),
                    _ => 0,
                };
                return _sortAscending ? cmp : -cmp;
            });
        }

        dgvCombos.DataSource = null;
        dgvCombos.DataSource = _combos;

        foreach (DataGridViewColumn c in dgvCombos.Columns)
        {
            c.HeaderCell.SortGlyphDirection = c.DataPropertyName == _sortColumn
                ? (_sortAscending ? SortOrder.Ascending : SortOrder.Descending)
                : SortOrder.None;
        }
    }
}
