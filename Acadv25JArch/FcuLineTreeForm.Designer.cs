namespace PipeLoad2
{
    partial class FcuLineTreeForm
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
            topPanel = new Panel();
            lblHead = new Label();
            cmbHead = new ComboBox();
            btnRecalc = new Button();
            lblMode = new Label();
            cmbMode = new ComboBox();
            treeView = new TreeView();
            panel = new Panel();
            btnExpand = new Button();
            btnCollapse = new Button();
            btnApply = new Button();
            btnClose = new Button();
            topPanel.SuspendLayout();
            panel.SuspendLayout();
            SuspendLayout();
            // 
            // lblStats
            // 
            lblStats.BackColor = Color.FromArgb(240, 240, 240);
            lblStats.Dock = DockStyle.Top;
            lblStats.Font = new Font("맑은 고딕", 8.5F);
            lblStats.Location = new Point(0, 53);
            lblStats.Name = "lblStats";
            lblStats.Padding = new Padding(6, 0, 0, 0);
            lblStats.Size = new Size(860, 10);
            lblStats.TabIndex = 1;
            lblStats.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // topPanel
            // 
            topPanel.BackColor = Color.FromArgb(224, 232, 244);
            topPanel.Controls.Add(lblHead);
            topPanel.Controls.Add(cmbHead);
            topPanel.Controls.Add(btnRecalc);
            topPanel.Controls.Add(lblMode);
            topPanel.Controls.Add(cmbMode);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(860, 53);
            topPanel.TabIndex = 0;
            // 
            // lblHead
            // 
            lblHead.AutoSize = true;
            lblHead.Font = new Font("맑은 고딕", 9F);
            lblHead.Location = new Point(12, 13);
            lblHead.Name = "lblHead";
            lblHead.Size = new Size(146, 25);
            lblHead.TabIndex = 0;
            lblHead.Text = "수두 (mmAq/m):";
            // 
            // cmbHead
            // 
            cmbHead.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHead.Font = new Font("맑은 고딕", 9F);
            cmbHead.Location = new Point(175, 10);
            cmbHead.Name = "cmbHead";
            cmbHead.Size = new Size(94, 33);
            cmbHead.TabIndex = 0;
            cmbHead.SelectedIndexChanged += cmbHead_SelectedIndexChanged;
            // 
            // btnRecalc
            // 
            btnRecalc.Location = new Point(308, 8);
            btnRecalc.Name = "btnRecalc";
            btnRecalc.Size = new Size(90, 34);
            btnRecalc.TabIndex = 1;
            btnRecalc.Text = "재계산";
            btnRecalc.UseVisualStyleBackColor = true;
            btnRecalc.Click += btnRecalc_Click;
            //
            // lblMode
            //
            lblMode.AutoSize = true;
            lblMode.Font = new Font("맑은 고딕", 9F);
            lblMode.Location = new Point(420, 13);
            lblMode.Name = "lblMode";
            lblMode.Text = "모드:";
            //
            // cmbMode
            //
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.Font = new Font("맑은 고딕", 9F);
            cmbMode.Location = new Point(470, 10);
            cmbMode.Name = "cmbMode";
            cmbMode.Size = new Size(180, 33);
            cmbMode.TabIndex = 2;
            cmbMode.SelectedIndexChanged += cmbMode_SelectedIndexChanged;
            //
            // treeView
            // 
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.Dock = DockStyle.Fill;
            treeView.Font = new Font("Consolas", 9.5F);
            treeView.FullRowSelect = true;
            treeView.Location = new Point(0, 63);
            treeView.Name = "treeView";
            treeView.Size = new Size(860, 481);
            treeView.TabIndex = 2;
            // 
            // panel
            // 
            panel.Controls.Add(btnExpand);
            panel.Controls.Add(btnCollapse);
            panel.Controls.Add(btnApply);
            panel.Controls.Add(btnClose);
            panel.Dock = DockStyle.Bottom;
            panel.Location = new Point(0, 544);
            panel.Name = "panel";
            panel.Size = new Size(860, 56);
            panel.TabIndex = 3;
            // 
            // btnExpand
            // 
            btnExpand.Location = new Point(6, 6);
            btnExpand.Name = "btnExpand";
            btnExpand.Size = new Size(100, 38);
            btnExpand.TabIndex = 0;
            btnExpand.Text = "전체 펼치기";
            btnExpand.Click += btnExpand_Click;
            // 
            // btnCollapse
            // 
            btnCollapse.Location = new Point(112, 6);
            btnCollapse.Name = "btnCollapse";
            btnCollapse.Size = new Size(100, 38);
            btnCollapse.TabIndex = 1;
            btnCollapse.Text = "전체 접기";
            btnCollapse.Click += btnCollapse_Click;
            // 
            // btnApply
            // 
            btnApply.BackColor = Color.FromArgb(0, 120, 215);
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.ForeColor = Color.White;
            btnApply.Location = new Point(227, 6);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(110, 38);
            btnApply.TabIndex = 2;
            btnApply.Text = "관경 적용 (Dia)";
            btnApply.UseVisualStyleBackColor = false;
            btnApply.Click += btnApply_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Location = new Point(666, 10);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(80, 34);
            btnClose.TabIndex = 3;
            btnClose.Text = "닫기";
            btnClose.Click += btnClose_Click;
            // 
            // FcuLineTreeForm
            // 
            ClientSize = new Size(860, 600);
            Controls.Add(treeView);
            Controls.Add(lblStats);
            Controls.Add(topPanel);
            Controls.Add(panel);
            Font = new Font("맑은 고딕", 9F);
            MinimumSize = new Size(560, 420);
            Name = "FcuLineTreeForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FCU Line Tree 분석";
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            panel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.Label    lblStats;
        private System.Windows.Forms.Panel    topPanel;
        private System.Windows.Forms.Label    lblHead;
        private System.Windows.Forms.ComboBox cmbHead;
        private System.Windows.Forms.Button   btnRecalc;
        private System.Windows.Forms.Label    lblMode;
        private System.Windows.Forms.ComboBox cmbMode;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.Panel    panel;
        private System.Windows.Forms.Button   btnExpand;
        private System.Windows.Forms.Button   btnCollapse;
        private System.Windows.Forms.Button   btnApply;
        private System.Windows.Forms.Button   btnClose;
    }
}
