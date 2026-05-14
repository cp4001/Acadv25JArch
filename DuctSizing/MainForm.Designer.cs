#nullable enable
namespace DuctSizing;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.grpInput = new GroupBox();
        this.lblQ = new Label();
        this.numQ = new NumericUpDown();
        this.lblR = new Label();
        this.numR = new NumericUpDown();
        this.lblAlpha = new Label();
        this.numAlpha = new NumericUpDown();
        this.lblAspect = new Label();
        this.numAspect = new NumericUpDown();

        this.grpMode = new GroupBox();
        this.rbModeA = new RadioButton();
        this.rbModeB = new RadioButton();
        this.lblShortSide = new Label();
        this.cmbShortSide = new ComboBox();

        this.btnCalc = new Button();

        this.grpResult = new GroupBox();
        this.lblDe = new Label();
        this.lblD = new Label();
        this.lblA = new Label();
        this.lblV = new Label();
        this.lblDp = new Label();
        this.lblRp = new Label();

        this.dgvCombos = new DataGridView();
        this.colB = new DataGridViewTextBoxColumn();
        this.colA = new DataGridViewTextBoxColumn();
        this.colDeEq = new DataGridViewTextBoxColumn();
        this.colAspect = new DataGridViewTextBoxColumn();
        this.colArea = new DataGridViewTextBoxColumn();

        ((System.ComponentModel.ISupportInitialize)this.numQ).BeginInit();
        ((System.ComponentModel.ISupportInitialize)this.numR).BeginInit();
        ((System.ComponentModel.ISupportInitialize)this.numAlpha).BeginInit();
        ((System.ComponentModel.ISupportInitialize)this.numAspect).BeginInit();
        ((System.ComponentModel.ISupportInitialize)this.dgvCombos).BeginInit();
        this.grpInput.SuspendLayout();
        this.grpMode.SuspendLayout();
        this.grpResult.SuspendLayout();
        this.SuspendLayout();

        // grpInput
        this.grpInput.Text = "입력";
        this.grpInput.Location = new Point(12, 12);
        this.grpInput.Size = new Size(732, 100);
        this.grpInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.grpInput.Controls.Add(this.lblQ);
        this.grpInput.Controls.Add(this.numQ);
        this.grpInput.Controls.Add(this.lblR);
        this.grpInput.Controls.Add(this.numR);
        this.grpInput.Controls.Add(this.lblAlpha);
        this.grpInput.Controls.Add(this.numAlpha);
        this.grpInput.Controls.Add(this.lblAspect);
        this.grpInput.Controls.Add(this.numAspect);

        // lblQ / numQ
        this.lblQ.Text = "풍량 Q [CMH]";
        this.lblQ.Location = new Point(16, 30);
        this.lblQ.AutoSize = true;
        this.numQ.Location = new Point(170, 26);
        this.numQ.Size = new Size(150, 23);
        this.numQ.Minimum = 1;
        this.numQ.Maximum = 999_999;
        this.numQ.Increment = 100;
        this.numQ.DecimalPlaces = 0;
        this.numQ.ThousandsSeparator = true;
        this.numQ.Value = 13_000;
        this.numQ.TextAlign = HorizontalAlignment.Right;

        // lblR / numR
        this.lblR.Text = "마찰손실 R [mmAq/m]";
        this.lblR.Location = new Point(16, 64);
        this.lblR.AutoSize = true;
        this.numR.Location = new Point(170, 60);
        this.numR.Size = new Size(150, 23);
        this.numR.Minimum = 0.01M;
        this.numR.Maximum = 10M;
        this.numR.Increment = 0.01M;
        this.numR.DecimalPlaces = 2;
        this.numR.Value = 0.08M;
        this.numR.TextAlign = HorizontalAlignment.Right;

        // lblAlpha / numAlpha
        this.lblAlpha.Text = "여유율 α";
        this.lblAlpha.Location = new Point(380, 30);
        this.lblAlpha.AutoSize = true;
        this.numAlpha.Location = new Point(530, 26);
        this.numAlpha.Size = new Size(150, 23);
        this.numAlpha.Minimum = 1.00M;
        this.numAlpha.Maximum = 2.00M;
        this.numAlpha.Increment = 0.01M;
        this.numAlpha.DecimalPlaces = 2;
        this.numAlpha.Value = 1.01M;
        this.numAlpha.TextAlign = HorizontalAlignment.Right;

        // lblAspect / numAspect
        this.lblAspect.Text = "아스펙트비 한계";
        this.lblAspect.Location = new Point(380, 64);
        this.lblAspect.AutoSize = true;
        this.numAspect.Location = new Point(530, 60);
        this.numAspect.Size = new Size(150, 23);
        this.numAspect.Minimum = 1.0M;
        this.numAspect.Maximum = 10.0M;
        this.numAspect.Increment = 0.1M;
        this.numAspect.DecimalPlaces = 1;
        this.numAspect.Value = 3.1M;
        this.numAspect.TextAlign = HorizontalAlignment.Right;

        // grpMode
        this.grpMode.Text = "모드";
        this.grpMode.Location = new Point(12, 124);
        this.grpMode.Size = new Size(732, 60);
        this.grpMode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.grpMode.Controls.Add(this.rbModeA);
        this.grpMode.Controls.Add(this.lblShortSide);
        this.grpMode.Controls.Add(this.cmbShortSide);
        this.grpMode.Controls.Add(this.rbModeB);

        // rbModeA
        this.rbModeA.Text = "Mode A: 단변 지정";
        this.rbModeA.Location = new Point(16, 26);
        this.rbModeA.AutoSize = true;
        this.rbModeA.Checked = false;

        // lblShortSide / cmbShortSide
        this.lblShortSide.Text = "단변 b [mm]";
        this.lblShortSide.Location = new Point(186, 28);
        this.lblShortSide.AutoSize = true;
        this.cmbShortSide.Location = new Point(280, 24);
        this.cmbShortSide.Size = new Size(120, 23);
        this.cmbShortSide.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbShortSide.Enabled = false;

        // rbModeB
        this.rbModeB.Text = "Mode B: 모든 조합 탐색";
        this.rbModeB.Location = new Point(440, 26);
        this.rbModeB.AutoSize = true;
        this.rbModeB.Checked = true;

        // btnCalc
        this.btnCalc.Text = "계산";
        this.btnCalc.Location = new Point(12, 196);
        this.btnCalc.Size = new Size(732, 36);
        this.btnCalc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.btnCalc.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

        // grpResult
        this.grpResult.Text = "계산 결과";
        this.grpResult.Location = new Point(12, 244);
        this.grpResult.Size = new Size(732, 100);
        this.grpResult.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.grpResult.Controls.Add(this.lblDe);
        this.grpResult.Controls.Add(this.lblD);
        this.grpResult.Controls.Add(this.lblA);
        this.grpResult.Controls.Add(this.lblV);
        this.grpResult.Controls.Add(this.lblDp);
        this.grpResult.Controls.Add(this.lblRp);

        // lblDe
        this.lblDe.Text = "등가직경 De = —";
        this.lblDe.Location = new Point(16, 26);
        this.lblDe.AutoSize = true;
        this.lblDe.Font = new Font("Segoe UI", 11F, FontStyle.Bold);

        // lblD
        this.lblD.Text = "원형 D = —";
        this.lblD.Location = new Point(300, 26);
        this.lblD.AutoSize = true;
        this.lblD.Font = new Font("Segoe UI", 11F, FontStyle.Bold);

        // lblA / lblV / lblDp / lblRp
        this.lblA.Text = "단면적 A = —";
        this.lblA.Location = new Point(16, 64);
        this.lblA.AutoSize = true;

        this.lblV.Text = "풍속 V = —";
        this.lblV.Location = new Point(196, 64);
        this.lblV.AutoSize = true;

        this.lblDp.Text = "동압 Δp = —";
        this.lblDp.Location = new Point(376, 64);
        this.lblDp.AutoSize = true;

        this.lblRp.Text = "실저항 R′ = —";
        this.lblRp.Location = new Point(556, 64);
        this.lblRp.AutoSize = true;

        // dgvCombos
        this.dgvCombos.Location = new Point(12, 356);
        this.dgvCombos.Size = new Size(732, 256);
        this.dgvCombos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.dgvCombos.AutoGenerateColumns = false;
        this.dgvCombos.AllowUserToAddRows = false;
        this.dgvCombos.AllowUserToDeleteRows = false;
        this.dgvCombos.ReadOnly = true;
        this.dgvCombos.RowHeadersVisible = false;
        this.dgvCombos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvCombos.MultiSelect = false;
        this.dgvCombos.BackgroundColor = SystemColors.Window;
        this.dgvCombos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvCombos.Columns.Add(this.colB);
        this.dgvCombos.Columns.Add(this.colA);
        this.dgvCombos.Columns.Add(this.colDeEq);
        this.dgvCombos.Columns.Add(this.colAspect);
        this.dgvCombos.Columns.Add(this.colArea);

        this.colB.HeaderText = "단변 b [mm]";
        this.colB.DataPropertyName = "B";
        this.colB.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        this.colB.FillWeight = 100;
        this.colB.DefaultCellStyle = new DataGridViewCellStyle { Format = "0", Alignment = DataGridViewContentAlignment.MiddleRight };

        this.colA.HeaderText = "장변 a [mm]";
        this.colA.DataPropertyName = "A";
        this.colA.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        this.colA.FillWeight = 100;
        this.colA.DefaultCellStyle = new DataGridViewCellStyle { Format = "0", Alignment = DataGridViewContentAlignment.MiddleRight };

        this.colDeEq.HeaderText = "De′ [mm]";
        this.colDeEq.DataPropertyName = "DeEq";
        this.colDeEq.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        this.colDeEq.FillWeight = 90;
        this.colDeEq.DefaultCellStyle = new DataGridViewCellStyle { Format = "0", Alignment = DataGridViewContentAlignment.MiddleRight };

        this.colAspect.HeaderText = "a/b";
        this.colAspect.DataPropertyName = "Aspect";
        this.colAspect.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        this.colAspect.FillWeight = 70;
        this.colAspect.DefaultCellStyle = new DataGridViewCellStyle { Format = "0.00", Alignment = DataGridViewContentAlignment.MiddleRight };

        this.colArea.HeaderText = "면적 [m²]";
        this.colArea.DataPropertyName = "Area";
        this.colArea.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        this.colArea.FillWeight = 90;
        this.colArea.DefaultCellStyle = new DataGridViewCellStyle { Format = "0.000", Alignment = DataGridViewContentAlignment.MiddleRight };

        // MainForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(756, 624);
        this.MinimumSize = new Size(620, 520);
        this.Controls.Add(this.grpInput);
        this.Controls.Add(this.grpMode);
        this.Controls.Add(this.btnCalc);
        this.Controls.Add(this.grpResult);
        this.Controls.Add(this.dgvCombos);
        this.Text = "덕트 사이즈 계산";
        this.StartPosition = FormStartPosition.CenterScreen;

        ((System.ComponentModel.ISupportInitialize)this.numQ).EndInit();
        ((System.ComponentModel.ISupportInitialize)this.numR).EndInit();
        ((System.ComponentModel.ISupportInitialize)this.numAlpha).EndInit();
        ((System.ComponentModel.ISupportInitialize)this.numAspect).EndInit();
        ((System.ComponentModel.ISupportInitialize)this.dgvCombos).EndInit();
        this.grpInput.ResumeLayout(false);
        this.grpInput.PerformLayout();
        this.grpMode.ResumeLayout(false);
        this.grpMode.PerformLayout();
        this.grpResult.ResumeLayout(false);
        this.grpResult.PerformLayout();
        this.ResumeLayout(false);
    }

    private GroupBox grpInput = null!;
    private Label lblQ = null!;
    private NumericUpDown numQ = null!;
    private Label lblR = null!;
    private NumericUpDown numR = null!;
    private Label lblAlpha = null!;
    private NumericUpDown numAlpha = null!;
    private Label lblAspect = null!;
    private NumericUpDown numAspect = null!;

    private GroupBox grpMode = null!;
    private RadioButton rbModeA = null!;
    private RadioButton rbModeB = null!;
    private Label lblShortSide = null!;
    private ComboBox cmbShortSide = null!;

    private Button btnCalc = null!;

    private GroupBox grpResult = null!;
    private Label lblDe = null!;
    private Label lblD = null!;
    private Label lblA = null!;
    private Label lblV = null!;
    private Label lblDp = null!;
    private Label lblRp = null!;

    private DataGridView dgvCombos = null!;
    private DataGridViewTextBoxColumn colB = null!;
    private DataGridViewTextBoxColumn colA = null!;
    private DataGridViewTextBoxColumn colDeEq = null!;
    private DataGridViewTextBoxColumn colAspect = null!;
    private DataGridViewTextBoxColumn colArea = null!;
}
