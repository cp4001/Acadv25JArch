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
            lblStats    = new System.Windows.Forms.Label();
            treeView    = new System.Windows.Forms.TreeView();
            panel       = new System.Windows.Forms.Panel();
            btnExpand   = new System.Windows.Forms.Button();
            btnCollapse = new System.Windows.Forms.Button();
            btnApply    = new System.Windows.Forms.Button();
            btnClose    = new System.Windows.Forms.Button();
            panel.SuspendLayout();
            SuspendLayout();

            // lblStats
            lblStats.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            lblStats.Dock      = System.Windows.Forms.DockStyle.Top;
            lblStats.Font      = new System.Drawing.Font("맑은 고딕", 8.5F);
            lblStats.Height    = 28;
            lblStats.Name      = "lblStats";
            lblStats.Padding   = new System.Windows.Forms.Padding(6, 0, 0, 0);
            lblStats.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // treeView
            treeView.BorderStyle   = System.Windows.Forms.BorderStyle.FixedSingle;
            treeView.Dock          = System.Windows.Forms.DockStyle.Fill;
            treeView.Font          = new System.Drawing.Font("Consolas", 9.5F);
            treeView.FullRowSelect = true;
            treeView.ShowLines     = true;
            treeView.ShowPlusMinus = true;
            treeView.Name          = "treeView";

            // panel
            panel.Dock   = System.Windows.Forms.DockStyle.Bottom;
            panel.Height = 44;
            panel.Name   = "panel";

            // btnExpand
            btnExpand.Location = new System.Drawing.Point(6, 6);
            btnExpand.Size     = new System.Drawing.Size(100, 30);
            btnExpand.Name     = "btnExpand";
            btnExpand.Text     = "전체 펼치기";
            btnExpand.Click   += btnExpand_Click;

            // btnCollapse
            btnCollapse.Location = new System.Drawing.Point(112, 6);
            btnCollapse.Size     = new System.Drawing.Size(100, 30);
            btnCollapse.Name     = "btnCollapse";
            btnCollapse.Text     = "전체 접기";
            btnCollapse.Click   += btnCollapse_Click;

            // btnApply
            btnApply.Location  = new System.Drawing.Point(224, 6);
            btnApply.Size      = new System.Drawing.Size(100, 30);
            btnApply.Name      = "btnApply";
            btnApply.Text      = "관경 적용";
            btnApply.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnApply.ForeColor = System.Drawing.Color.White;
            btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnApply.Click    += btnApply_Click;

            // btnClose
            btnClose.Anchor   = System.Windows.Forms.AnchorStyles.Top
                               | System.Windows.Forms.AnchorStyles.Right;
            btnClose.Size     = new System.Drawing.Size(80, 30);
            btnClose.Top      = 6;
            btnClose.Name     = "btnClose";
            btnClose.Text     = "닫기";
            btnClose.Click   += btnClose_Click;

            // panel
            panel.Controls.Add(btnExpand);
            panel.Controls.Add(btnCollapse);
            panel.Controls.Add(btnApply);
            panel.Controls.Add(btnClose);

            // LineTreeForm
            ClientSize     = new System.Drawing.Size(800, 600);
            MinimumSize    = new System.Drawing.Size(500, 400);
            StartPosition  = System.Windows.Forms.FormStartPosition.CenterScreen;
            Font           = new System.Drawing.Font("맑은 고딕", 9F);
            Name           = "LineTreeForm";
            Text           = "Line Tree 분석";

            Controls.Add(treeView);
            Controls.Add(lblStats);
            Controls.Add(panel);

            panel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.Label    lblStats;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.Panel    panel;
        private System.Windows.Forms.Button   btnExpand;
        private System.Windows.Forms.Button   btnCollapse;
        private System.Windows.Forms.Button   btnApply;
        private System.Windows.Forms.Button   btnClose;
    }
}
