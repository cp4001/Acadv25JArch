namespace DuctSizeApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        grpInput = new GroupBox();
        lblL = new Label();
        txtL = new TextBox();
        lblLUnit = new Label();
        lblQ = new Label();
        txtQ = new TextBox();
        lblQUnit = new Label();
        lblDp = new Label();
        txtDp = new TextBox();
        lblDpUnit = new Label();
        lblSf = new Label();
        txtSf = new TextBox();
        lblSfUnit = new Label();
        lblEps = new Label();
        txtEps = new TextBox();
        lblEpsUnit = new Label();
        lblTol = new Label();
        txtTol = new TextBox();
        lblTolUnit = new Label();
        lblAspMax = new Label();
        txtAspMax = new TextBox();
        lblAspMaxUnit = new Label();
        btnCalc = new Button();
        grpOptA = new GroupBox();
        lblOptA = new Label();
        grpOptD = new GroupBox();
        gridD = new DataGridView();
        colA = new DataGridViewTextBoxColumn();
        colB = new DataGridViewTextBoxColumn();
        colDe = new DataGridViewTextBoxColumn();
        colV = new DataGridViewTextBoxColumn();
        colR = new DataGridViewTextBoxColumn();
        colDP = new DataGridViewTextBoxColumn();
        colASP = new DataGridViewTextBoxColumn();
        grpInput.SuspendLayout();
        grpOptA.SuspendLayout();
        grpOptD.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridD).BeginInit();
        SuspendLayout();
        // 
        // grpInput
        // 
        grpInput.Controls.Add(lblL);
        grpInput.Controls.Add(txtL);
        grpInput.Controls.Add(lblLUnit);
        grpInput.Controls.Add(lblQ);
        grpInput.Controls.Add(txtQ);
        grpInput.Controls.Add(lblQUnit);
        grpInput.Controls.Add(lblDp);
        grpInput.Controls.Add(txtDp);
        grpInput.Controls.Add(lblDpUnit);
        grpInput.Controls.Add(lblSf);
        grpInput.Controls.Add(txtSf);
        grpInput.Controls.Add(lblSfUnit);
        grpInput.Controls.Add(lblEps);
        grpInput.Controls.Add(txtEps);
        grpInput.Controls.Add(lblEpsUnit);
        grpInput.Controls.Add(lblTol);
        grpInput.Controls.Add(txtTol);
        grpInput.Controls.Add(lblTolUnit);
        grpInput.Controls.Add(lblAspMax);
        grpInput.Controls.Add(txtAspMax);
        grpInput.Controls.Add(lblAspMaxUnit);
        grpInput.Location = new Point(17, 20);
        grpInput.Margin = new Padding(4, 5, 4, 5);
        grpInput.Name = "grpInput";
        grpInput.Padding = new Padding(4, 5, 4, 5);
        grpInput.Size = new Size(1223, 217);
        grpInput.TabIndex = 0;
        grpInput.TabStop = false;
        grpInput.Text = "입력";
        // 
        // lblL
        // 
        lblL.Location = new Point(23, 47);
        lblL.Margin = new Padding(4, 0, 4, 0);
        lblL.Name = "lblL";
        lblL.Size = new Size(214, 33);
        lblL.TabIndex = 0;
        lblL.Text = "덕트 길이 L";
        lblL.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtL
        // 
        txtL.Location = new Point(244, 43);
        txtL.Margin = new Padding(4, 5, 4, 5);
        txtL.Name = "txtL";
        txtL.Size = new Size(155, 31);
        txtL.TabIndex = 1;
        // 
        // lblLUnit
        // 
        lblLUnit.ForeColor = Color.DimGray;
        lblLUnit.Location = new Point(409, 47);
        lblLUnit.Margin = new Padding(4, 0, 4, 0);
        lblLUnit.Name = "lblLUnit";
        lblLUnit.Size = new Size(186, 33);
        lblLUnit.TabIndex = 2;
        lblLUnit.Text = "m";
        lblLUnit.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblQ
        // 
        lblQ.Location = new Point(23, 93);
        lblQ.Margin = new Padding(4, 0, 4, 0);
        lblQ.Name = "lblQ";
        lblQ.Size = new Size(214, 33);
        lblQ.TabIndex = 3;
        lblQ.Text = "요구 풍량 Q";
        lblQ.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtQ
        // 
        txtQ.Location = new Point(244, 90);
        txtQ.Margin = new Padding(4, 5, 4, 5);
        txtQ.Name = "txtQ";
        txtQ.Size = new Size(155, 31);
        txtQ.TabIndex = 4;
        // 
        // lblQUnit
        // 
        lblQUnit.ForeColor = Color.DimGray;
        lblQUnit.Location = new Point(409, 93);
        lblQUnit.Margin = new Padding(4, 0, 4, 0);
        lblQUnit.Name = "lblQUnit";
        lblQUnit.Size = new Size(186, 33);
        lblQUnit.TabIndex = 5;
        lblQUnit.Text = "CMH (m³/h)";
        lblQUnit.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblDp
        // 
        lblDp.Location = new Point(23, 140);
        lblDp.Margin = new Padding(4, 0, 4, 0);
        lblDp.Name = "lblDp";
        lblDp.Size = new Size(214, 33);
        lblDp.TabIndex = 6;
        lblDp.Text = "허용 손실율 (p/L)";
        lblDp.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtDp
        // 
        txtDp.Location = new Point(244, 137);
        txtDp.Margin = new Padding(4, 5, 4, 5);
        txtDp.Name = "txtDp";
        txtDp.Size = new Size(155, 31);
        txtDp.TabIndex = 7;
        // 
        // lblDpUnit
        // 
        lblDpUnit.ForeColor = Color.DimGray;
        lblDpUnit.Location = new Point(409, 140);
        lblDpUnit.Margin = new Padding(4, 0, 4, 0);
        lblDpUnit.Name = "lblDpUnit";
        lblDpUnit.Size = new Size(186, 33);
        lblDpUnit.TabIndex = 8;
        lblDpUnit.Text = "mmAq/m";
        lblDpUnit.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblSf
        // 
        lblSf.Location = new Point(23, 187);
        lblSf.Margin = new Padding(4, 0, 4, 0);
        lblSf.Name = "lblSf";
        lblSf.Size = new Size(214, 33);
        lblSf.TabIndex = 9;
        lblSf.Text = "여유율 SF";
        lblSf.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtSf
        // 
        txtSf.Location = new Point(244, 183);
        txtSf.Margin = new Padding(4, 5, 4, 5);
        txtSf.Name = "txtSf";
        txtSf.Size = new Size(155, 31);
        txtSf.TabIndex = 10;
        // 
        // lblSfUnit
        // 
        lblSfUnit.ForeColor = Color.DimGray;
        lblSfUnit.Location = new Point(409, 187);
        lblSfUnit.Margin = new Padding(4, 0, 4, 0);
        lblSfUnit.Name = "lblSfUnit";
        lblSfUnit.Size = new Size(186, 33);
        lblSfUnit.TabIndex = 11;
        lblSfUnit.Text = "(0.5~1.5)";
        lblSfUnit.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblEps
        // 
        lblEps.Location = new Point(609, 47);
        lblEps.Margin = new Padding(4, 0, 4, 0);
        lblEps.Name = "lblEps";
        lblEps.Size = new Size(214, 33);
        lblEps.TabIndex = 12;
        lblEps.Text = "조도 ε";
        lblEps.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtEps
        // 
        txtEps.Location = new Point(830, 43);
        txtEps.Margin = new Padding(4, 5, 4, 5);
        txtEps.Name = "txtEps";
        txtEps.Size = new Size(155, 31);
        txtEps.TabIndex = 13;
        // 
        // lblEpsUnit
        // 
        lblEpsUnit.ForeColor = Color.DimGray;
        lblEpsUnit.Location = new Point(994, 47);
        lblEpsUnit.Margin = new Padding(4, 0, 4, 0);
        lblEpsUnit.Name = "lblEpsUnit";
        lblEpsUnit.Size = new Size(214, 33);
        lblEpsUnit.TabIndex = 14;
        lblEpsUnit.Text = "mm (기본 0.09)";
        lblEpsUnit.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblTol
        // 
        lblTol.Location = new Point(609, 93);
        lblTol.Margin = new Padding(4, 0, 4, 0);
        lblTol.Name = "lblTol";
        lblTol.Size = new Size(214, 33);
        lblTol.TabIndex = 15;
        lblTol.Text = "De 허용오차 (옵션 D)";
        lblTol.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtTol
        // 
        txtTol.Location = new Point(830, 90);
        txtTol.Margin = new Padding(4, 5, 4, 5);
        txtTol.Name = "txtTol";
        txtTol.Size = new Size(155, 31);
        txtTol.TabIndex = 16;
        // 
        // lblTolUnit
        // 
        lblTolUnit.ForeColor = Color.DimGray;
        lblTolUnit.Location = new Point(994, 93);
        lblTolUnit.Margin = new Padding(4, 0, 4, 0);
        lblTolUnit.Name = "lblTolUnit";
        lblTolUnit.Size = new Size(214, 33);
        lblTolUnit.TabIndex = 17;
        lblTolUnit.Text = "(0.01~0.5)";
        lblTolUnit.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblAspMax
        // 
        lblAspMax.Location = new Point(609, 140);
        lblAspMax.Margin = new Padding(4, 0, 4, 0);
        lblAspMax.Name = "lblAspMax";
        lblAspMax.Size = new Size(214, 33);
        lblAspMax.TabIndex = 18;
        lblAspMax.Text = "ASP 상한 (옵션 D)";
        lblAspMax.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtAspMax
        // 
        txtAspMax.Location = new Point(830, 137);
        txtAspMax.Margin = new Padding(4, 5, 4, 5);
        txtAspMax.Name = "txtAspMax";
        txtAspMax.Size = new Size(155, 31);
        txtAspMax.TabIndex = 19;
        // 
        // lblAspMaxUnit
        // 
        lblAspMaxUnit.ForeColor = Color.DimGray;
        lblAspMaxUnit.Location = new Point(994, 140);
        lblAspMaxUnit.Margin = new Padding(4, 0, 4, 0);
        lblAspMaxUnit.Name = "lblAspMaxUnit";
        lblAspMaxUnit.Size = new Size(214, 33);
        lblAspMaxUnit.TabIndex = 20;
        lblAspMaxUnit.Text = "(1~10)";
        lblAspMaxUnit.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnCalc
        // 
        btnCalc.Location = new Point(1008, 47);
        btnCalc.Margin = new Padding(4, 5, 4, 5);
        btnCalc.Name = "btnCalc";
        btnCalc.Size = new Size(171, 53);
        btnCalc.TabIndex = 21;
        btnCalc.Text = "계산";
        btnCalc.UseVisualStyleBackColor = true;
        btnCalc.Click += btnCalc_Click;
        // 
        // grpOptA
        // 
        grpOptA.Controls.Add(btnCalc);
        grpOptA.Controls.Add(lblOptA);
        grpOptA.Location = new Point(17, 253);
        grpOptA.Margin = new Padding(4, 5, 4, 5);
        grpOptA.Name = "grpOptA";
        grpOptA.Padding = new Padding(4, 5, 4, 5);
        grpOptA.Size = new Size(1223, 150);
        grpOptA.TabIndex = 1;
        grpOptA.TabStop = false;
        grpOptA.Text = "옵션 A — 단일 결과 (정방형환산표 자동)";
        // 
        // lblOptA
        // 
        lblOptA.Font = new Font("Consolas", 10F);
        lblOptA.Location = new Point(23, 47);
        lblOptA.Margin = new Padding(4, 0, 4, 0);
        lblOptA.Name = "lblOptA";
        lblOptA.Size = new Size(1171, 83);
        lblOptA.TabIndex = 0;
        lblOptA.Text = "(계산 버튼을 누르세요)";
        // 
        // grpOptD
        // 
        grpOptD.Controls.Add(gridD);
        grpOptD.Location = new Point(17, 420);
        grpOptD.Margin = new Padding(4, 5, 4, 5);
        grpOptD.Name = "grpOptD";
        grpOptD.Padding = new Padding(4, 5, 4, 5);
        grpOptD.Size = new Size(1223, 627);
        grpOptD.TabIndex = 2;
        grpOptD.TabStop = false;
        grpOptD.Text = "옵션 D — 다중 후보 (ASP 작은 순)";
        // 
        // gridD
        // 
        gridD.AllowUserToAddRows = false;
        gridD.AllowUserToDeleteRows = false;
        gridD.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = SystemColors.Control;
        dataGridViewCellStyle1.Font = new Font("맑은 고딕", 9F);
        dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        gridD.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        gridD.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridD.Columns.AddRange(new DataGridViewColumn[] { colA, colB, colDe, colV, colR, colDP, colASP });
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = SystemColors.Window;
        dataGridViewCellStyle2.Font = new Font("맑은 고딕", 9F);
        dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        gridD.DefaultCellStyle = dataGridViewCellStyle2;
        gridD.Location = new Point(17, 40);
        gridD.Margin = new Padding(4, 5, 4, 5);
        gridD.Name = "gridD";
        gridD.ReadOnly = true;
        gridD.RowHeadersVisible = false;
        gridD.RowHeadersWidth = 62;
        gridD.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridD.Size = new Size(1189, 567);
        gridD.TabIndex = 0;
        // 
        // colA
        // 
        colA.HeaderText = "장변 A (mm)";
        colA.MinimumWidth = 8;
        colA.Name = "colA";
        colA.ReadOnly = true;
        // 
        // colB
        // 
        colB.HeaderText = "단변 B (mm)";
        colB.MinimumWidth = 8;
        colB.Name = "colB";
        colB.ReadOnly = true;
        // 
        // colDe
        // 
        colDe.HeaderText = "De (mm)";
        colDe.MinimumWidth = 8;
        colDe.Name = "colDe";
        colDe.ReadOnly = true;
        // 
        // colV
        // 
        colV.HeaderText = "V (m/s)";
        colV.MinimumWidth = 8;
        colV.Name = "colV";
        colV.ReadOnly = true;
        // 
        // colR
        // 
        colR.HeaderText = "R (mmAq/m)";
        colR.MinimumWidth = 8;
        colR.Name = "colR";
        colR.ReadOnly = true;
        // 
        // colDP
        // 
        colDP.HeaderText = "ΔP_actual (mmAq)";
        colDP.MinimumWidth = 8;
        colDP.Name = "colDP";
        colDP.ReadOnly = true;
        // 
        // colASP
        // 
        colASP.HeaderText = "ASP";
        colASP.MinimumWidth = 8;
        colASP.Name = "colASP";
        colASP.ReadOnly = true;
        // 
        // MainForm
        // 
        AcceptButton = btnCalc;
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1257, 1067);
        Controls.Add(grpOptD);
        Controls.Add(grpOptA);
        Controls.Add(grpInput);
        Font = new Font("맑은 고딕", 9F);
        Margin = new Padding(4, 5, 4, 5);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "덕트 사이즈 결정 (DuctSize)";
        grpInput.ResumeLayout(false);
        grpInput.PerformLayout();
        grpOptA.ResumeLayout(false);
        grpOptD.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridD).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.GroupBox grpInput;
    private System.Windows.Forms.Label lblL;
    private System.Windows.Forms.TextBox txtL;
    private System.Windows.Forms.Label lblLUnit;
    private System.Windows.Forms.Label lblQ;
    private System.Windows.Forms.TextBox txtQ;
    private System.Windows.Forms.Label lblQUnit;
    private System.Windows.Forms.Label lblDp;
    private System.Windows.Forms.TextBox txtDp;
    private System.Windows.Forms.Label lblDpUnit;
    private System.Windows.Forms.Label lblSf;
    private System.Windows.Forms.TextBox txtSf;
    private System.Windows.Forms.Label lblSfUnit;
    private System.Windows.Forms.Label lblEps;
    private System.Windows.Forms.TextBox txtEps;
    private System.Windows.Forms.Label lblEpsUnit;
    private System.Windows.Forms.Label lblTol;
    private System.Windows.Forms.TextBox txtTol;
    private System.Windows.Forms.Label lblTolUnit;
    private System.Windows.Forms.Label lblAspMax;
    private System.Windows.Forms.TextBox txtAspMax;
    private System.Windows.Forms.Label lblAspMaxUnit;
    private System.Windows.Forms.Button btnCalc;
    private System.Windows.Forms.GroupBox grpOptA;
    private System.Windows.Forms.Label lblOptA;
    private System.Windows.Forms.GroupBox grpOptD;
    private System.Windows.Forms.DataGridView gridD;
    private System.Windows.Forms.DataGridViewTextBoxColumn colA;
    private System.Windows.Forms.DataGridViewTextBoxColumn colB;
    private System.Windows.Forms.DataGridViewTextBoxColumn colDe;
    private System.Windows.Forms.DataGridViewTextBoxColumn colV;
    private System.Windows.Forms.DataGridViewTextBoxColumn colR;
    private System.Windows.Forms.DataGridViewTextBoxColumn colDP;
    private System.Windows.Forms.DataGridViewTextBoxColumn colASP;
}
