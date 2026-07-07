namespace PipeLoad2
{
    partial class DuctTreeForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblStats = new Label();
            pnlMode = new Panel();
            rbSupply = new RadioButton();
            rbReturn = new RadioButton();
            lblBMin = new Label();
            numBMin = new NumericUpDown();
            lblBMax = new Label();
            numBMax = new NumericUpDown();
            treeView = new TreeView();
            panel = new Panel();
            btnExpand = new Button();
            btnCollapse = new Button();
            btnApply = new Button();
            btnSelect = new Button();
            btnDuctOutline = new Button();
            btnClose = new Button();
            pnlMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numBMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numBMax).BeginInit();
            panel.SuspendLayout();
            SuspendLayout();
            // 
            // lblStats
            // 
            lblStats.BackColor = Color.FromArgb(240, 240, 240);
            lblStats.Dock = DockStyle.Top;
            lblStats.Font = new Font("맑은 고딕", 8.5F);
            lblStats.Location = new Point(0, 0);
            lblStats.Name = "lblStats";
            lblStats.Padding = new Padding(6, 0, 0, 0);
            lblStats.Size = new Size(860, 28);
            lblStats.TabIndex = 0;
            lblStats.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pnlMode
            // 
            pnlMode.Controls.Add(rbSupply);
            pnlMode.Controls.Add(rbReturn);
            pnlMode.Controls.Add(lblBMin);
            pnlMode.Controls.Add(numBMin);
            pnlMode.Controls.Add(lblBMax);
            pnlMode.Controls.Add(numBMax);
            pnlMode.Dock = DockStyle.Top;
            pnlMode.Location = new Point(0, 28);
            pnlMode.Name = "pnlMode";
            pnlMode.Padding = new Padding(6, 4, 6, 4);
            pnlMode.Size = new Size(860, 45);
            pnlMode.TabIndex = 3;
            // 
            // rbSupply
            // 
            rbSupply.AutoSize = true;
            rbSupply.Checked = true;
            rbSupply.Location = new Point(10, 6);
            rbSupply.Name = "rbSupply";
            rbSupply.Size = new Size(92, 29);
            rbSupply.TabIndex = 0;
            rbSupply.TabStop = true;
            rbSupply.Text = "Supply";
            rbSupply.UseVisualStyleBackColor = true;
            // 
            // rbReturn
            // 
            rbReturn.AutoSize = true;
            rbReturn.Location = new Point(108, 7);
            rbReturn.Name = "rbReturn";
            rbReturn.Size = new Size(90, 29);
            rbReturn.TabIndex = 1;
            rbReturn.Text = "Return";
            rbReturn.UseVisualStyleBackColor = true;
            // 
            // lblBMin
            // 
            lblBMin.AutoSize = true;
            lblBMin.Location = new Point(232, 11);
            lblBMin.Name = "lblBMin";
            lblBMin.Size = new Size(69, 25);
            lblBMin.TabIndex = 2;
            lblBMin.Text = "b 최소:";
            // 
            // numBMin
            // 
            numBMin.Increment = new decimal(new int[] { 50, 0, 0, 0 });
            numBMin.Location = new Point(307, 8);
            numBMin.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            numBMin.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numBMin.Name = "numBMin";
            numBMin.Size = new Size(70, 31);
            numBMin.TabIndex = 3;
            numBMin.Value = new decimal(new int[] { 200, 0, 0, 0 });
            // 
            // lblBMax
            // 
            lblBMax.AutoSize = true;
            lblBMax.Location = new Point(389, 10);
            lblBMax.Name = "lblBMax";
            lblBMax.Size = new Size(69, 25);
            lblBMax.TabIndex = 4;
            lblBMax.Text = "b 최대:";
            // 
            // numBMax
            // 
            numBMax.Increment = new decimal(new int[] { 50, 0, 0, 0 });
            numBMax.Location = new Point(464, 7);
            numBMax.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            numBMax.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numBMax.Name = "numBMax";
            numBMax.Size = new Size(70, 31);
            numBMax.TabIndex = 5;
            numBMax.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // treeView
            // 
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.Dock = DockStyle.Fill;
            treeView.Font = new Font("Consolas", 9.5F);
            treeView.FullRowSelect = true;
            treeView.Location = new Point(0, 73);
            treeView.Name = "treeView";
            treeView.Size = new Size(860, 471);
            treeView.TabIndex = 1;
            // 
            // panel
            // 
            panel.Controls.Add(btnExpand);
            panel.Controls.Add(btnCollapse);
            panel.Controls.Add(btnApply);
            panel.Controls.Add(btnSelect);
            panel.Controls.Add(btnDuctOutline);
            panel.Controls.Add(btnClose);
            panel.Dock = DockStyle.Bottom;
            panel.Location = new Point(0, 544);
            panel.Name = "panel";
            panel.Size = new Size(860, 56);
            panel.TabIndex = 2;
            // 
            // btnExpand
            // 
            btnExpand.Location = new Point(6, 6);
            btnExpand.Name = "btnExpand";
            btnExpand.Size = new Size(100, 38);
            btnExpand.TabIndex = 0;
            btnExpand.Text = "전체";
            btnExpand.Click += btnExpand_Click;
            // 
            // btnCollapse
            // 
            btnCollapse.Location = new Point(112, 6);
            btnCollapse.Name = "btnCollapse";
            btnCollapse.Size = new Size(100, 38);
            btnCollapse.TabIndex = 1;
            btnCollapse.Text = "접기";
            btnCollapse.Click += btnCollapse_Click;
            // 
            // btnApply
            // 
            btnApply.BackColor = Color.FromArgb(0, 120, 215);
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.ForeColor = Color.White;
            btnApply.Location = new Point(227, 6);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(150, 38);
            btnApply.TabIndex = 2;
            btnApply.Text = "누적 적용 (Total_CMH)";
            btnApply.UseVisualStyleBackColor = false;
            btnApply.Click += btnApply_Click;
            // 
            // btnSelect
            // 
            btnSelect.BackColor = Color.FromArgb(40, 167, 69);
            btnSelect.FlatStyle = FlatStyle.Flat;
            btnSelect.ForeColor = Color.White;
            btnSelect.Location = new Point(383, 6);
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new Size(100, 38);
            btnSelect.TabIndex = 3;
            btnSelect.Text = "Select";
            btnSelect.UseVisualStyleBackColor = false;
            btnSelect.Click += btnSelect_Click;
            //
            // btnDuctOutline
            //
            btnDuctOutline.BackColor = Color.FromArgb(108, 117, 125);
            btnDuctOutline.FlatStyle = FlatStyle.Flat;
            btnDuctOutline.ForeColor = Color.White;
            btnDuctOutline.Location = new Point(489, 6);
            btnDuctOutline.Name = "btnDuctOutline";
            btnDuctOutline.Size = new Size(150, 38);
            btnDuctOutline.TabIndex = 4;
            btnDuctOutline.Text = "DuctOutLine 생성";
            btnDuctOutline.UseVisualStyleBackColor = false;
            btnDuctOutline.Click += btnDuctOutline_Click;
            //
            // btnClose
            //
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Location = new Point(770, 10);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(80, 34);
            btnClose.TabIndex = 5;
            btnClose.Text = "닫기";
            btnClose.Click += btnClose_Click;
            // 
            // DuctTreeForm
            // 
            ClientSize = new Size(860, 600);
            Controls.Add(treeView);
            Controls.Add(pnlMode);
            Controls.Add(lblStats);
            Controls.Add(panel);
            Font = new Font("맑은 고딕", 9F);
            MinimumSize = new Size(560, 420);
            Name = "DuctTreeForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Duct Tree 분석";
            pnlMode.ResumeLayout(false);
            pnlMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numBMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numBMax).EndInit();
            panel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.Label         lblStats;
        private System.Windows.Forms.Panel         pnlMode;
        private System.Windows.Forms.RadioButton   rbSupply;
        private System.Windows.Forms.RadioButton   rbReturn;
        private System.Windows.Forms.Label         lblBMin;
        private System.Windows.Forms.NumericUpDown numBMin;
        private System.Windows.Forms.Label         lblBMax;
        private System.Windows.Forms.NumericUpDown numBMax;
        private System.Windows.Forms.TreeView    treeView;
        private System.Windows.Forms.Panel       panel;
        private System.Windows.Forms.Button      btnExpand;
        private System.Windows.Forms.Button      btnCollapse;
        private System.Windows.Forms.Button      btnApply;
        private System.Windows.Forms.Button      btnSelect;
        private System.Windows.Forms.Button      btnDuctOutline;
        private System.Windows.Forms.Button      btnClose;
    }
}
