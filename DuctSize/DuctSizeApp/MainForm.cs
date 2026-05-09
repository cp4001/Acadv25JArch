using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace DuctSizeApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        SetDefaults();
    }

    private void SetDefaults()
    {
        txtL.Text = "30";
        txtQ.Text = "13000";
        txtDp.Text = "0.08";
        txtSf.Text = "1.00";
        txtEps.Text = "0.09";
        txtTol.Text = "0.05";
        txtAspMax.Text = "3.0";
    }

    private void btnCalc_Click(object? sender, EventArgs e)
    {
        try
        {
            double L = ParseDouble(txtL.Text, "덕트 길이 L");
            double Q = ParseDouble(txtQ.Text, "요구 풍량 Q");
            double R = ParseDouble(txtDp.Text, "허용 손실율 (p/L)");
            double SF = ParseDouble(txtSf.Text, "여유율 SF");
            double epsMm = ParseDouble(txtEps.Text, "조도 ε");
            double tol = ParseDouble(txtTol.Text, "De 허용오차");
            double aspMax = ParseDouble(txtAspMax.Text, "ASP 상한");

            double epsM = epsMm * 1e-3;

            var rA = DuctDesign.OptionA(L, Q, R, SF, epsM);
            ShowOptionA(rA);

            var rD = DuctDesign.OptionD(L, Q, R, SF, tol, aspMax, epsM);
            ShowOptionD(rD);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowOptionA(DuctDesignResult r)
    {
        var ci = CultureInfo.InvariantCulture;
        lblOptA.Text =
            $"A × B = {r.A} × {r.B} mm     De = {r.De.ToString("F1", ci)} mm     ASP = {r.Aspect.ToString("F2", ci)}\r\n" +
            $"V = {r.V.ToString("F2", ci)} m/s     R = {r.R.ToString("F4", ci)} mmAq/m     ΔP_actual = {r.DeltaPActual.ToString("F2", ci)} mmAq";
    }

    private void ShowOptionD(IReadOnlyList<DuctDesignResult> list)
    {
        gridD.Rows.Clear();
        var ci = CultureInfo.InvariantCulture;
        foreach (var r in list)
        {
            gridD.Rows.Add(
                r.A,
                r.B,
                r.De.ToString("F1", ci),
                r.V.ToString("F2", ci),
                r.R.ToString("F4", ci),
                r.DeltaPActual.ToString("F2", ci),
                r.Aspect.ToString("F2", ci));
        }

        if (list.Count == 0)
            MessageBox.Show(this, "조건을 만족하는 후보가 없습니다. tol/asp_max를 완화하세요.",
                "옵션 D", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static double ParseDouble(string text, string fieldName)
    {
        if (!double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) &&
            !double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out v))
            throw new FormatException($"입력값을 인식할 수 없습니다: {fieldName} = '{text}'");
        return v;
    }
}
