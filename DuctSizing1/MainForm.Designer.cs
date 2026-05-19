#nullable enable
namespace DuctSizing1;

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
        grpInput = new GroupBox();
        lblQ = new Label();
        numQ = new NumericUpDown();
        lblDuctType = new Label();
        cmbDuctType = new ComboBox();
        lblAlpha = new Label();
        numAlpha = new NumericUpDown();
        lblAspect = new Label();
        numAspect = new NumericUpDown();
        grpMode = new GroupBox();
        rbModeA = new RadioButton();
        lblShortSide = new Label();
        cmbShortSide = new ComboBox();
        rbModeB = new RadioButton();
        btnCalc = new Button();
        grpResult = new GroupBox();
        lblDe = new Label();
        lblD = new Label();
        lblA = new Label();
        lblV = new Label();
        lblDp = new Label();
        lblRp = new Label();
        dgvCombos = new DataGridView();
        tabMain = new TabControl();
        tabSingle = new TabPage();
        tabSimul = new TabPage();
        lblSimulInfo = new Label();
        btnSimul = new Button();
        treeSimul = new TreeView();
        tabModeD = new TabPage();
        lblModeDInfo = new Label();
        lblBMin = new Label();
        numBMin = new NumericUpDown();
        lblBMax = new Label();
        numBMax = new NumericUpDown();
        btnModeD = new Button();
        dgvModeD = new DataGridView();
        grpInput.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numQ).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numAlpha).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numAspect).BeginInit();
        grpMode.SuspendLayout();
        grpResult.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvCombos).BeginInit();
        tabMain.SuspendLayout();
        tabSingle.SuspendLayout();
        tabSimul.SuspendLayout();
        tabModeD.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numBMin).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numBMax).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvModeD).BeginInit();
        SuspendLayout();
        // 
        // grpInput
        // 
        grpInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpInput.Controls.Add(lblQ);
        grpInput.Controls.Add(numQ);
        grpInput.Controls.Add(lblDuctType);
        grpInput.Controls.Add(cmbDuctType);
        grpInput.Controls.Add(lblAlpha);
        grpInput.Controls.Add(numAlpha);
        grpInput.Controls.Add(lblAspect);
        grpInput.Controls.Add(numAspect);
        grpInput.Location = new Point(17, 20);
        grpInput.Margin = new Padding(4, 5, 4, 5);
        grpInput.Name = "grpInput";
        grpInput.Padding = new Padding(4, 5, 4, 5);
        grpInput.Size = new Size(2046, 167);
        grpInput.TabIndex = 0;
        grpInput.TabStop = false;
        grpInput.Text = "입력";
        // 
        // lblQ
        // 
        lblQ.AutoSize = true;
        lblQ.Location = new Point(23, 50);
        lblQ.Margin = new Padding(4, 0, 4, 0);
        lblQ.Name = "lblQ";
        lblQ.Size = new Size(122, 25);
        lblQ.TabIndex = 0;
        lblQ.Text = "풍량 Q [CMH]";
        // 
        // numQ
        // 
        numQ.Increment = new decimal(new int[] { 100, 0, 0, 0 });
        numQ.Location = new Point(243, 43);
        numQ.Margin = new Padding(4, 5, 4, 5);
        numQ.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
        numQ.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numQ.Name = "numQ";
        numQ.Size = new Size(214, 31);
        numQ.TabIndex = 1;
        numQ.TextAlign = HorizontalAlignment.Right;
        numQ.ThousandsSeparator = true;
        numQ.Value = new decimal(new int[] { 13000, 0, 0, 0 });
        // 
        // lblDuctType
        // 
        lblDuctType.AutoSize = true;
        lblDuctType.Location = new Point(23, 107);
        lblDuctType.Margin = new Padding(4, 0, 4, 0);
        lblDuctType.Name = "lblDuctType";
        lblDuctType.Size = new Size(89, 25);
        lblDuctType.TabIndex = 2;
        lblDuctType.Text = "덕트 구분";
        // 
        // cmbDuctType
        // 
        cmbDuctType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbDuctType.Items.AddRange(new object[] { "Return (R=0.08)", "Supply (R=0.10)" });
        cmbDuctType.Location = new Point(243, 100);
        cmbDuctType.Margin = new Padding(4, 5, 4, 5);
        cmbDuctType.Name = "cmbDuctType";
        cmbDuctType.Size = new Size(213, 33);
        cmbDuctType.TabIndex = 3;
        // 
        // lblAlpha
        // 
        lblAlpha.AutoSize = true;
        lblAlpha.Location = new Point(543, 50);
        lblAlpha.Margin = new Padding(4, 0, 4, 0);
        lblAlpha.Name = "lblAlpha";
        lblAlpha.Size = new Size(82, 25);
        lblAlpha.TabIndex = 4;
        lblAlpha.Text = "여유율 α";
        // 
        // numAlpha
        // 
        numAlpha.DecimalPlaces = 2;
        numAlpha.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        numAlpha.Location = new Point(757, 43);
        numAlpha.Margin = new Padding(4, 5, 4, 5);
        numAlpha.Maximum = new decimal(new int[] { 200, 0, 0, 131072 });
        numAlpha.Minimum = new decimal(new int[] { 100, 0, 0, 131072 });
        numAlpha.Name = "numAlpha";
        numAlpha.Size = new Size(214, 31);
        numAlpha.TabIndex = 5;
        numAlpha.TextAlign = HorizontalAlignment.Right;
        numAlpha.Value = new decimal(new int[] { 101, 0, 0, 131072 });
        // 
        // lblAspect
        // 
        lblAspect.AutoSize = true;
        lblAspect.Location = new Point(543, 107);
        lblAspect.Margin = new Padding(4, 0, 4, 0);
        lblAspect.Name = "lblAspect";
        lblAspect.Size = new Size(143, 25);
        lblAspect.TabIndex = 6;
        lblAspect.Text = "아스펙트비 한계";
        // 
        // numAspect
        // 
        numAspect.DecimalPlaces = 1;
        numAspect.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        numAspect.Location = new Point(757, 100);
        numAspect.Margin = new Padding(4, 5, 4, 5);
        numAspect.Maximum = new decimal(new int[] { 100, 0, 0, 65536 });
        numAspect.Minimum = new decimal(new int[] { 10, 0, 0, 65536 });
        numAspect.Name = "numAspect";
        numAspect.Size = new Size(214, 31);
        numAspect.TabIndex = 7;
        numAspect.TextAlign = HorizontalAlignment.Right;
        numAspect.Value = new decimal(new int[] { 31, 0, 0, 65536 });
        // 
        // grpMode
        // 
        grpMode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpMode.Controls.Add(rbModeA);
        grpMode.Controls.Add(lblShortSide);
        grpMode.Controls.Add(cmbShortSide);
        grpMode.Controls.Add(rbModeB);
        grpMode.Location = new Point(17, 207);
        grpMode.Margin = new Padding(4, 5, 4, 5);
        grpMode.Name = "grpMode";
        grpMode.Padding = new Padding(4, 5, 4, 5);
        grpMode.Size = new Size(2046, 100);
        grpMode.TabIndex = 1;
        grpMode.TabStop = false;
        grpMode.Text = "모드";
        // 
        // rbModeA
        // 
        rbModeA.AutoSize = true;
        rbModeA.Location = new Point(23, 43);
        rbModeA.Margin = new Padding(4, 5, 4, 5);
        rbModeA.Name = "rbModeA";
        rbModeA.Size = new Size(187, 29);
        rbModeA.TabIndex = 0;
        rbModeA.Text = "Mode A: 단변 지정";
        // 
        // lblShortSide
        // 
        lblShortSide.AutoSize = true;
        lblShortSide.Location = new Point(266, 47);
        lblShortSide.Margin = new Padding(4, 0, 4, 0);
        lblShortSide.Name = "lblShortSide";
        lblShortSide.Size = new Size(111, 25);
        lblShortSide.TabIndex = 1;
        lblShortSide.Text = "단변 b [mm]";
        // 
        // cmbShortSide
        // 
        cmbShortSide.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbShortSide.Enabled = false;
        cmbShortSide.Location = new Point(400, 40);
        cmbShortSide.Margin = new Padding(4, 5, 4, 5);
        cmbShortSide.Name = "cmbShortSide";
        cmbShortSide.Size = new Size(170, 33);
        cmbShortSide.TabIndex = 2;
        // 
        // rbModeB
        // 
        rbModeB.AutoSize = true;
        rbModeB.Checked = true;
        rbModeB.Location = new Point(629, 43);
        rbModeB.Margin = new Padding(4, 5, 4, 5);
        rbModeB.Name = "rbModeB";
        rbModeB.Size = new Size(226, 29);
        rbModeB.TabIndex = 3;
        rbModeB.TabStop = true;
        rbModeB.Text = "Mode B: 모든 조합 탐색";
        // 
        // btnCalc
        // 
        btnCalc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnCalc.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnCalc.Location = new Point(17, 327);
        btnCalc.Margin = new Padding(4, 5, 4, 5);
        btnCalc.Name = "btnCalc";
        btnCalc.Size = new Size(2046, 60);
        btnCalc.TabIndex = 2;
        btnCalc.Text = "계산";
        // 
        // grpResult
        // 
        grpResult.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpResult.Controls.Add(lblDe);
        grpResult.Controls.Add(lblD);
        grpResult.Controls.Add(lblA);
        grpResult.Controls.Add(lblV);
        grpResult.Controls.Add(lblDp);
        grpResult.Controls.Add(lblRp);
        grpResult.Location = new Point(17, 407);
        grpResult.Margin = new Padding(4, 5, 4, 5);
        grpResult.Name = "grpResult";
        grpResult.Padding = new Padding(4, 5, 4, 5);
        grpResult.Size = new Size(2046, 167);
        grpResult.TabIndex = 3;
        grpResult.TabStop = false;
        grpResult.Text = "계산 결과";
        // 
        // lblDe
        // 
        lblDe.AutoSize = true;
        lblDe.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblDe.Location = new Point(23, 43);
        lblDe.Margin = new Padding(4, 0, 4, 0);
        lblDe.Name = "lblDe";
        lblDe.Size = new Size(185, 30);
        lblDe.TabIndex = 0;
        lblDe.Text = "등가직경 De = —";
        // 
        // lblD
        // 
        lblD.AutoSize = true;
        lblD.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblD.Location = new Point(429, 43);
        lblD.Margin = new Padding(4, 0, 4, 0);
        lblD.Name = "lblD";
        lblD.Size = new Size(129, 30);
        lblD.TabIndex = 1;
        lblD.Text = "원형 D = —";
        // 
        // lblA
        // 
        lblA.AutoSize = true;
        lblA.Location = new Point(23, 107);
        lblA.Margin = new Padding(4, 0, 4, 0);
        lblA.Name = "lblA";
        lblA.Size = new Size(123, 25);
        lblA.TabIndex = 2;
        lblA.Text = "단면적 A = —";
        // 
        // lblV
        // 
        lblV.AutoSize = true;
        lblV.Location = new Point(280, 107);
        lblV.Margin = new Padding(4, 0, 4, 0);
        lblV.Name = "lblV";
        lblV.Size = new Size(104, 25);
        lblV.TabIndex = 3;
        lblV.Text = "풍속 V = —";
        // 
        // lblDp
        // 
        lblDp.AutoSize = true;
        lblDp.Location = new Point(537, 107);
        lblDp.Margin = new Padding(4, 0, 4, 0);
        lblDp.Name = "lblDp";
        lblDp.Size = new Size(116, 25);
        lblDp.TabIndex = 4;
        lblDp.Text = "동압 Δp = —";
        // 
        // lblRp
        // 
        lblRp.AutoSize = true;
        lblRp.Location = new Point(794, 107);
        lblRp.Margin = new Padding(4, 0, 4, 0);
        lblRp.Name = "lblRp";
        lblRp.Size = new Size(126, 25);
        lblRp.TabIndex = 5;
        lblRp.Text = "실저항 R′ = —";
        // 
        // dgvCombos
        // 
        dgvCombos.AllowUserToAddRows = false;
        dgvCombos.AllowUserToDeleteRows = false;
        dgvCombos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvCombos.BackgroundColor = SystemColors.Window;
        dgvCombos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvCombos.Location = new Point(17, 594);
        dgvCombos.Margin = new Padding(4, 5, 4, 5);
        dgvCombos.MultiSelect = false;
        dgvCombos.Name = "dgvCombos";
        dgvCombos.ReadOnly = true;
        dgvCombos.RowHeadersVisible = false;
        dgvCombos.RowHeadersWidth = 62;
        dgvCombos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCombos.Size = new Size(2046, 1627);
        dgvCombos.TabIndex = 4;
        // 
        // tabMain
        // 
        tabMain.Controls.Add(tabSingle);
        tabMain.Controls.Add(tabSimul);
        tabMain.Controls.Add(tabModeD);
        tabMain.Dock = DockStyle.Fill;
        tabMain.Location = new Point(0, 0);
        tabMain.Margin = new Padding(4, 5, 4, 5);
        tabMain.Name = "tabMain";
        tabMain.SelectedIndex = 0;
        tabMain.Size = new Size(1286, 1367);
        tabMain.TabIndex = 0;
        // 
        // tabSingle
        // 
        tabSingle.Controls.Add(grpInput);
        tabSingle.Controls.Add(grpMode);
        tabSingle.Controls.Add(btnCalc);
        tabSingle.Controls.Add(grpResult);
        tabSingle.Controls.Add(dgvCombos);
        tabSingle.Location = new Point(4, 34);
        tabSingle.Margin = new Padding(4, 5, 4, 5);
        tabSingle.Name = "tabSingle";
        tabSingle.Padding = new Padding(4, 5, 4, 5);
        tabSingle.Size = new Size(1278, 1329);
        tabSingle.TabIndex = 0;
        tabSingle.Text = "단건";
        tabSingle.UseVisualStyleBackColor = true;
        // 
        // tabSimul
        // 
        tabSimul.Controls.Add(lblSimulInfo);
        tabSimul.Controls.Add(btnSimul);
        tabSimul.Controls.Add(treeSimul);
        tabSimul.Location = new Point(4, 34);
        tabSimul.Margin = new Padding(4, 5, 4, 5);
        tabSimul.Name = "tabSimul";
        tabSimul.Padding = new Padding(4, 5, 4, 5);
        tabSimul.Size = new Size(278, 129);
        tabSimul.TabIndex = 1;
        tabSimul.Text = "Simul";
        tabSimul.UseVisualStyleBackColor = true;
        // 
        // lblSimulInfo
        // 
        lblSimulInfo.AutoSize = true;
        lblSimulInfo.Location = new Point(17, 20);
        lblSimulInfo.Margin = new Padding(4, 0, 4, 0);
        lblSimulInfo.Name = "lblSimulInfo";
        lblSimulInfo.Size = new Size(766, 25);
        lblSimulInfo.TabIndex = 0;
        lblSimulInfo.Text = "단건 탭의 여유율 α·아스펙트비 한계 사용. Q 100~24,000 CMH (100 간격), Return·Supply 양쪽 산출.";
        // 
        // btnSimul
        // 
        btnSimul.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnSimul.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnSimul.Location = new Point(17, 63);
        btnSimul.Margin = new Padding(4, 5, 4, 5);
        btnSimul.Name = "btnSimul";
        btnSimul.Size = new Size(1040, 60);
        btnSimul.TabIndex = 1;
        btnSimul.Text = "Simulation 실행";
        // 
        // treeSimul
        // 
        treeSimul.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        treeSimul.HideSelection = false;
        treeSimul.Location = new Point(17, 143);
        treeSimul.Margin = new Padding(4, 5, 4, 5);
        treeSimul.Name = "treeSimul";
        treeSimul.Size = new Size(1038, 881);
        treeSimul.TabIndex = 2;
        // 
        // tabModeD
        // 
        tabModeD.Controls.Add(lblModeDInfo);
        tabModeD.Controls.Add(lblBMin);
        tabModeD.Controls.Add(numBMin);
        tabModeD.Controls.Add(lblBMax);
        tabModeD.Controls.Add(numBMax);
        tabModeD.Controls.Add(btnModeD);
        tabModeD.Controls.Add(dgvModeD);
        tabModeD.Location = new Point(4, 34);
        tabModeD.Margin = new Padding(4, 5, 4, 5);
        tabModeD.Name = "tabModeD";
        tabModeD.Padding = new Padding(4, 5, 4, 5);
        tabModeD.Size = new Size(278, 129);
        tabModeD.TabIndex = 2;
        tabModeD.Text = "Mode D";
        tabModeD.UseVisualStyleBackColor = true;
        // 
        // lblModeDInfo
        // 
        lblModeDInfo.AutoSize = true;
        lblModeDInfo.Location = new Point(17, 20);
        lblModeDInfo.Margin = new Padding(4, 0, 4, 0);
        lblModeDInfo.Name = "lblModeDInfo";
        lblModeDInfo.Size = new Size(807, 25);
        lblModeDInfo.TabIndex = 0;
        lblModeDInfo.Text = "단건 탭의 여유율 α 사용 (aspectMax=1.5 고정). Q 100~20,000 CMH (100 간격), Return·Supply 양쪽 산출.";
        // 
        // lblBMin
        // 
        lblBMin.AutoSize = true;
        lblBMin.Location = new Point(17, 73);
        lblBMin.Margin = new Padding(4, 0, 4, 0);
        lblBMin.Name = "lblBMin";
        lblBMin.Size = new Size(136, 25);
        lblBMin.TabIndex = 1;
        lblBMin.Text = "단변 최소 [mm]";
        // 
        // numBMin
        // 
        numBMin.Increment = new decimal(new int[] { 50, 0, 0, 0 });
        numBMin.Location = new Point(171, 67);
        numBMin.Margin = new Padding(4, 5, 4, 5);
        numBMin.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
        numBMin.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
        numBMin.Name = "numBMin";
        numBMin.Size = new Size(129, 31);
        numBMin.TabIndex = 2;
        numBMin.TextAlign = HorizontalAlignment.Right;
        numBMin.Value = new decimal(new int[] { 200, 0, 0, 0 });
        // 
        // lblBMax
        // 
        lblBMax.AutoSize = true;
        lblBMax.Location = new Point(329, 73);
        lblBMax.Margin = new Padding(4, 0, 4, 0);
        lblBMax.Name = "lblBMax";
        lblBMax.Size = new Size(136, 25);
        lblBMax.TabIndex = 3;
        lblBMax.Text = "단변 최대 [mm]";
        // 
        // numBMax
        // 
        numBMax.Increment = new decimal(new int[] { 50, 0, 0, 0 });
        numBMax.Location = new Point(483, 67);
        numBMax.Margin = new Padding(4, 5, 4, 5);
        numBMax.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
        numBMax.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
        numBMax.Name = "numBMax";
        numBMax.Size = new Size(129, 31);
        numBMax.TabIndex = 4;
        numBMax.TextAlign = HorizontalAlignment.Right;
        numBMax.Value = new decimal(new int[] { 500, 0, 0, 0 });
        // 
        // btnModeD
        // 
        btnModeD.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnModeD.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnModeD.Location = new Point(17, 127);
        btnModeD.Margin = new Padding(4, 5, 4, 5);
        btnModeD.Name = "btnModeD";
        btnModeD.Size = new Size(1040, 60);
        btnModeD.TabIndex = 5;
        btnModeD.Text = "Mode D 시뮬레이션 실행";
        // 
        // dgvModeD
        // 
        dgvModeD.AllowUserToAddRows = false;
        dgvModeD.AllowUserToDeleteRows = false;
        dgvModeD.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvModeD.BackgroundColor = SystemColors.Window;
        dgvModeD.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvModeD.Location = new Point(17, 207);
        dgvModeD.Margin = new Padding(4, 5, 4, 5);
        dgvModeD.MultiSelect = false;
        dgvModeD.Name = "dgvModeD";
        dgvModeD.ReadOnly = true;
        dgvModeD.RowHeadersVisible = false;
        dgvModeD.RowHeadersWidth = 62;
        dgvModeD.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvModeD.Size = new Size(1040, 820);
        dgvModeD.TabIndex = 6;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1286, 1367);
        Controls.Add(tabMain);
        Margin = new Padding(4, 5, 4, 5);
        MinimumSize = new Size(1105, 1163);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "덕트 사이즈 계산";
        grpInput.ResumeLayout(false);
        grpInput.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numQ).EndInit();
        ((System.ComponentModel.ISupportInitialize)numAlpha).EndInit();
        ((System.ComponentModel.ISupportInitialize)numAspect).EndInit();
        grpMode.ResumeLayout(false);
        grpMode.PerformLayout();
        grpResult.ResumeLayout(false);
        grpResult.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dgvCombos).EndInit();
        tabMain.ResumeLayout(false);
        tabSingle.ResumeLayout(false);
        tabSimul.ResumeLayout(false);
        tabSimul.PerformLayout();
        tabModeD.ResumeLayout(false);
        tabModeD.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numBMin).EndInit();
        ((System.ComponentModel.ISupportInitialize)numBMax).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvModeD).EndInit();
        ResumeLayout(false);
    }

    private GroupBox grpInput = null!;
    private Label lblQ = null!;
    private NumericUpDown numQ = null!;
    private Label lblDuctType = null!;
    private ComboBox cmbDuctType = null!;
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

    private TabControl tabMain = null!;
    private TabPage tabSingle = null!;
    private TabPage tabSimul = null!;
    private Label lblSimulInfo = null!;
    private Button btnSimul = null!;
    private TreeView treeSimul = null!;

    private TabPage tabModeD = null!;
    private Label lblModeDInfo = null!;
    private Label lblBMin = null!;
    private NumericUpDown numBMin = null!;
    private Label lblBMax = null!;
    private NumericUpDown numBMax = null!;
    private Button btnModeD = null!;
    private DataGridView dgvModeD = null!;
    private DataGridViewTextBoxColumn colMdQ = null!;
    private DataGridViewTextBoxColumn colMdSucB = null!;
    private DataGridViewTextBoxColumn colMdSucA = null!;
    private DataGridViewTextBoxColumn colMdDisB = null!;
    private DataGridViewTextBoxColumn colMdDisA = null!;
}
