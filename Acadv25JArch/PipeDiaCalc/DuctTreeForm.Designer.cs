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
            treeView = new TreeView();
            panel = new Panel();
            btnExpand = new Button();
            btnCollapse = new Button();
            btnApply = new Button();
            btnSelect = new Button();
            btnClose = new Button();
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
            // treeView
            //
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.Dock = DockStyle.Fill;
            treeView.Font = new Font("Consolas", 9.5F);
            treeView.FullRowSelect = true;
            treeView.Location = new Point(0, 28);
            treeView.Name = "treeView";
            treeView.Size = new Size(860, 516);
            treeView.TabIndex = 1;
            //
            // panel
            //
            panel.Controls.Add(btnExpand);
            panel.Controls.Add(btnCollapse);
            panel.Controls.Add(btnApply);
            panel.Controls.Add(btnSelect);
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
            // btnClose
            //
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Location = new Point(770, 10);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(80, 34);
            btnClose.TabIndex = 4;
            btnClose.Text = "닫기";
            btnClose.Click += btnClose_Click;
            //
            // DuctTreeForm
            //
            ClientSize = new Size(860, 600);
            Controls.Add(treeView);
            Controls.Add(lblStats);
            Controls.Add(panel);
            Font = new Font("맑은 고딕", 9F);
            MinimumSize = new Size(560, 420);
            Name = "DuctTreeForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Duct Tree 분석";
            panel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.Label    lblStats;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.Panel    panel;
        private System.Windows.Forms.Button   btnExpand;
        private System.Windows.Forms.Button   btnCollapse;
        private System.Windows.Forms.Button   btnApply;
        private System.Windows.Forms.Button   btnSelect;
        private System.Windows.Forms.Button   btnClose;
    }
}
