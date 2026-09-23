namespace Acadv25JArch.PolyView
{
    // 주의: 이 Designer 코드는 손으로 작성했다. VS Designer 로 열어 재생성하면
    //       ToolStrip 버튼 설정이 누락될 수 있다. 수정은 이 파일을 직접 편집할 것.
    partial class PolyViewControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbRefresh;
        private System.Windows.Forms.ToolStripButton tsbNewGroup;
        private System.Windows.Forms.ToolStripButton tsbPurge;
        private System.Windows.Forms.ToolStripSeparator tsSep1;
        private System.Windows.Forms.ToolStripButton tsbExpand;
        private System.Windows.Forms.ToolStripButton tsbCollapse;
        private System.Windows.Forms.TreeView treeView;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsbNewGroup = new System.Windows.Forms.ToolStripButton();
            this.tsbPurge = new System.Windows.Forms.ToolStripButton();
            this.tsSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExpand = new System.Windows.Forms.ToolStripButton();
            this.tsbCollapse = new System.Windows.Forms.ToolStripButton();
            this.treeView = new System.Windows.Forms.TreeView();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();

            // toolStrip
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbRefresh,
                this.tsbNewGroup,
                this.tsbPurge,
                this.tsSep1,
                this.tsbExpand,
                this.tsbCollapse});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(300, 25);
            this.toolStrip.TabIndex = 0;

            // tsbRefresh
            this.tsbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRefresh.Name = "tsbRefresh";
            this.tsbRefresh.Text = "새로고침";
            this.tsbRefresh.ToolTipText = "도면을 다시 읽어 트리를 갱신";

            // tsbNewGroup
            this.tsbNewGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbNewGroup.Name = "tsbNewGroup";
            this.tsbNewGroup.Text = "새 그룹";
            this.tsbNewGroup.ToolTipText = "선택한 그룹 아래에 새 그룹 생성";

            // tsbPurge
            this.tsbPurge.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbPurge.Name = "tsbPurge";
            this.tsbPurge.Text = "누락 정리";
            this.tsbPurge.ToolTipText = "도면에서 사라진 항목을 트리에서 제거";

            // tsbExpand
            this.tsbExpand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExpand.Name = "tsbExpand";
            this.tsbExpand.Text = "펼치기";

            // tsbCollapse
            this.tsbCollapse.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCollapse.Name = "tsbCollapse";
            this.tsbCollapse.Text = "접기";

            // treeView
            this.treeView.AllowDrop = true;
            this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView.HideSelection = false;
            this.treeView.LabelEdit = true;
            this.treeView.Location = new System.Drawing.Point(0, 25);
            this.treeView.Name = "treeView";
            this.treeView.ShowNodeToolTips = true;
            this.treeView.Size = new System.Drawing.Size(300, 475);
            this.treeView.TabIndex = 1;

            // PolyViewControl
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.toolStrip);
            this.Name = "PolyViewControl";
            this.Size = new System.Drawing.Size(300, 500);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
