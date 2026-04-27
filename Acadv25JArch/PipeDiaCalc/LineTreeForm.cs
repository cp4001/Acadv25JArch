using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PipeLoad2
{
    public partial class LineTreeForm : Form
    {
        private readonly LineTreeBuilder.LineNode _rootNode;
        private readonly Database                 _db;
        private readonly LineTreeBuilder          _builder = new();

        public LineTreeForm(LineTreeBuilder.LineNode rootNode,
                            int totalNodes, int leafCount,
                            string layer, Database db,
                            LineTreeBuilder.CalcMode mode = LineTreeBuilder.CalcMode.Supply)
        {
            InitializeComponent();

            _rootNode = rootNode;
            _db       = db;

            string modeStr = mode == LineTreeBuilder.CalcMode.Return ? "환탕" : "급수/급탕";
            this.Text = $"Line Tree 분석 [{modeStr}] — 레이어: {layer}";

            int midCount = totalNodes - leafCount - 1;
            string loadLabel = mode == LineTreeBuilder.CalcMode.Return ? "총 누적체적" : "총 부하";
            lblStats.Text =
                $"[{modeStr}]  레이어: {layer}  |  총 노드: {totalNodes}  |  " +
                $"Root: 1  Mid: {midCount}  Leaf: {leafCount}  |  " +
                $"{loadLabel}: {rootNode.Load:F2}  |  총 Leaf: {rootNode.LeafCount}";

            treeView.BeginUpdate();
            treeView.Nodes.Add(CreateTreeNode(rootNode));
            treeView.EndUpdate();
            treeView.ExpandAll();
        }

        private TreeNode CreateTreeNode(LineTreeBuilder.LineNode node)
        {
            string symbol = node.Type switch
            {
                LineTreeBuilder.NodeType.Root => "●",
                LineTreeBuilder.NodeType.Mid  => "◆",
                LineTreeBuilder.NodeType.Leaf => "■",
                _ => "○"
            };
            string loadInfo = node.Type == LineTreeBuilder.NodeType.Leaf
                ? $"부하={node.Load:F2}"
                : $"누적부하={node.Load:F2}  Leaf={node.LeafCount}";
            string label =
                $"{symbol} [{node.Handle}]  Lv={node.Level}  {loadInfo}  관경={node.Diameter}mm";

            var tvNode = new TreeNode(label)
            {
                ForeColor = node.Type switch
                {
                    LineTreeBuilder.NodeType.Root => Color.Red,
                    LineTreeBuilder.NodeType.Mid  => Color.Blue,
                    LineTreeBuilder.NodeType.Leaf => Color.Green,
                    _ => Color.Black
                }
            };
            foreach (var child in node.Children)
                tvNode.Nodes.Add(CreateTreeNode(child));
            return tvNode;
        }

        private void btnExpand_Click(object sender, EventArgs e)   => treeView.ExpandAll();
        private void btnCollapse_Click(object sender, EventArgs e) => treeView.CollapseAll();
        private void btnClose_Click(object sender, EventArgs e)    => this.Close();

        private void btnApply_Click(object sender, EventArgs e)
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application
                              .DocumentManager.MdiActiveDocument;
                using (doc.LockDocument())
                {
                    _builder.ApplyDiameters(_rootNode, _db);
                }
                MessageBox.Show("관경이 XData \"DD\" 에 저장되었습니다.",
                    "적용 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
