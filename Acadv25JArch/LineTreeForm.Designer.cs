namespace PipeLoad2
{
    partial class LineTreeForm
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
            lblStats.Size = new Size(700, 28);
            lblStats.TabIndex = 1;
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
            treeView.Size = new Size(700, 554);
            treeView.TabIndex = 0;
            // 
            // panel
            // 
            panel.Controls.Add(btnExpand);
            panel.Controls.Add(btnCollapse);
            panel.Controls.Add(btnClose);
            panel.Dock = DockStyle.Bottom;
            panel.Location = new Point(0, 582);
            panel.Name = "panel";
            panel.Size = new Size(700, 38);
            panel.TabIndex = 2;
            // 
            // btnExpand
            // 
            btnExpand.Location = new Point(6, 6);
            btnExpand.Name = "btnExpand";
            btnExpand.Size = new Size(100, 32);
            btnExpand.TabIndex = 0;
            btnExpand.Text = "전체 펼치기";
            btnExpand.Click += btnExpand_Click;
            // 
            // btnCollapse
            // 
            btnCollapse.Location = new Point(112, 6);
            btnCollapse.Name = "btnCollapse";
            btnCollapse.Size = new Size(100, 32);
            btnCollapse.TabIndex = 1;
            btnCollapse.Text = "전체 접기";
            btnCollapse.Click += btnCollapse_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Location = new Point(500, 6);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(80, 32);
            btnClose.TabIndex = 2;
            btnClose.Text = "닫기";
            btnClose.Click += btnClose_Click;
            // 
            // LineTreeForm
            // 
            ClientSize = new Size(700, 620);
            Controls.Add(treeView);
            Controls.Add(lblStats);
            Controls.Add(panel);
            Font = new Font("맑은 고딕", 9F);
            MinimumSize = new Size(500, 400);
            Name = "LineTreeForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Line Tree 분석";
            panel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.Label    lblStats;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.Panel    panel;
        private System.Windows.Forms.Button   btnExpand;
        private System.Windows.Forms.Button   btnCollapse;
        private System.Windows.Forms.Button   btnClose;
    }
}
