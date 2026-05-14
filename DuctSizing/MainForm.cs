namespace DuctSizing;

public partial class MainForm : Form
{
    private const double DeUpperRatio = 1.05; // De_eq 허용 상한 = De × 1.05 (+5%)

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
    }

    private void OnCalcClick(object? sender, EventArgs e)
    {
        var q = (double)numQ.Value;
        var r = (double)numR.Value;
        var alpha = (double)numAlpha.Value;
        var aspectMax = (double)numAspect.Value;

        var de = EquivalentDiameter.Calculate(q, r, alpha);
        var d = StandardSizes.NextAtLeast(de);
        var aux = AuxiliaryCalculator.Calculate(q, de);

        lblDe.Text = $"등가직경 De = {de:0} mm";
        lblD.Text = $"원형 D = {d} mm";
        lblA.Text = $"단면적 A = {aux.Area:0.000} m²";
        lblV.Text = $"풍속 V = {aux.Velocity:0.000} m/s";
        lblDp.Text = $"동압 Δp = {aux.DynamicPressure:0.000} mmAq";
        lblRp.Text = $"실저항 R′ = {aux.ActualResistance:0.000} mmAq/m";

        var deMax = de * DeUpperRatio;

        if (rbModeA.Checked)
        {
            var b = (int)cmbShortSide.SelectedItem!;
            _combos = RectDuctSelector.FindLongSidesInRange(de, deMax, b, StandardSizes.Default)
                .Select(a => new RectCombination
                {
                    B = b,
                    A = a,
                    DeEq = HuebscherFormula.CalculateRounded(b, a),
                    Aspect = (double)a / b,
                    Area = (double)b * a / 1_000_000.0,
                })
                .ToList();
        }
        else
        {
            _combos = RectCombinationFinder.FindAll(de, deMax, aspectMax, StandardSizes.Default);
        }

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
