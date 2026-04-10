using System;
using System.Drawing;
using System.Windows.Forms;

namespace PipeLoad2
{
    public partial class LineTreeForm : Form
    {
        public LineTreeForm(LineTreeBuilder.LineNode rootNode,
                            int totalNodes, int leafCount, string layer)
        {
            InitializeComponent();

            this.Text = $"Line Tree 분석 — 레이어: {layer}";

            int midCount = totalNodes - leafCount - 1;
            lblStats.Text =
                $"레이어: {layer}  |  총 노드: {totalNodes}  |  " +
                $"Root: 1  Mid: {midCount}  Leaf: {leafCount}  |  " +
                $"총 부하: {rootNode.Load:F2}  |  총 Leaf: {rootNode.LeafCount}";

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
    }
}
