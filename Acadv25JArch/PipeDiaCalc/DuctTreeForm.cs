using Autodesk.AutoCAD.DatabaseServices;
using DuctSizing.Core;
using System;
using System.Drawing;
using System.Windows.Forms;
using Node     = PipeLoad2.DuctTreeBuilder.DuctNode;
using NodeType = PipeLoad2.DuctTreeBuilder.DuctNodeType;

namespace PipeLoad2
{
    public partial class DuctTreeForm : Form
    {
        private readonly Node             _rootNode;
        private readonly Database         _db;
        private readonly DuctTreeBuilder  _builder = new();
        private readonly string           _layer;

        public DuctTreeForm(Node rootNode, string layer, Database db)
        {
            InitializeComponent();

            _rootNode = rootNode;
            _db       = db;
            _layer    = layer;

            // Designer 재생성 시 Checked / NumericUpDown 값이 누락될 수 있어 코드에서 재보장
            if (!rbSupply.Checked && !rbReturn.Checked) rbSupply.Checked = true;
            if (numBMin.Value < numBMin.Minimum) numBMin.Value = 200;
            if (numBMax.Value < numBMax.Minimum) numBMax.Value = 500;

            this.Text = $"Duct Tree 분석 — 레이어: {layer}";
            RefreshView();
        }

        private DuctType SelectedDuctType =>
            rbReturn.Checked ? DuctType.Return : DuctType.Supply;

        private void RefreshView()
        {
            int lineNodes  = _builder.CountLineNodes(_rootNode);
            int blockNodes = _builder.CountBlockNodes(_rootNode);
            int leafLines  = CountLeafLines(_rootNode);
            int midLines   = lineNodes - leafLines - 1;

            lblStats.Text =
                $"[Duct] {_layer} | Line {lineNodes} (Root=1, Mid={midCountClamp(midLines)}, Leaf={leafLines}) | " +
                $"Diffuser={blockNodes} | Root 총 풍량: {_rootNode.Load:F1} CMH";

            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            treeView.Nodes.Add(CreateTreeNode(_rootNode));
            treeView.EndUpdate();
            treeView.ExpandAll();
        }

        private static int midCountClamp(int v) => v < 0 ? 0 : v;

        private int CountLeafLines(Node node)
        {
            int n = (node.Type == NodeType.Leaf) ? 1 : 0;
            foreach (var c in node.Children) n += CountLeafLines(c);
            return n;
        }

        private TreeNode CreateTreeNode(Node node)
        {
            string symbol = node.Type switch
            {
                NodeType.Root  => "●",
                NodeType.Mid   => "◆",
                NodeType.Leaf  => "■",
                NodeType.Block => "▣",
                _              => "○"
            };

            string label;
            if (node.Type == NodeType.Block)
            {
                label = $"{symbol} [{node.Handle}]  Diffuser={node.BlockName}  CMH={node.Load:F1}";
            }
            else
            {
                label =
                    $"{symbol} [{node.Handle}]  Lv={node.Level}  누적={node.Load:F1} CMH  " +
                    $"Diffuser={node.LeafCount}";
            }

            var tv = new TreeNode(label)
            {
                ForeColor = node.Type switch
                {
                    NodeType.Root  => Color.Red,
                    NodeType.Mid   => Color.Blue,
                    NodeType.Leaf  => Color.Green,
                    NodeType.Block => Color.Purple,
                    _              => Color.Black
                },
                Tag = node.Handle
            };

            foreach (var c in node.Children) tv.Nodes.Add(CreateTreeNode(c));
            return tv;
        }

        private void btnExpand_Click(object sender, EventArgs e)   => treeView.ExpandAll();
        private void btnCollapse_Click(object sender, EventArgs e) => treeView.CollapseAll();
        private void btnClose_Click(object sender, EventArgs e)    => this.Close();

        private void btnSelect_Click(object sender, EventArgs e)
        {
            try
            {
                var sel = treeView.SelectedNode;
                if (sel == null || sel.Tag is not string handleStr || string.IsNullOrEmpty(handleStr))
                {
                    MessageBox.Show("Tree 에서 노드를 먼저 선택하세요.", "Select",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var doc = Autodesk.AutoCAD.ApplicationServices.Application
                              .DocumentManager.MdiActiveDocument;
                var ed  = doc.Editor;

                var handle = new Handle(Convert.ToInt64(handleStr, 16));
                if (!_db.TryGetObjectId(handle, out ObjectId objId) || objId.IsNull)
                {
                    MessageBox.Show($"Handle {handleStr} 엔티티를 찾을 수 없습니다.",
                        "Select", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ed.SetImpliedSelection(new[] { objId });
                doc.Window.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            try
            {
                var ductType = SelectedDuctType;
                int bMin = (int)numBMin.Value;
                int bMax = (int)numBMax.Value;
                if (bMin > bMax)
                {
                    MessageBox.Show("b 최소 값이 최대 값보다 큽니다.", "입력 오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var doc = Autodesk.AutoCAD.ApplicationServices.Application
                              .DocumentManager.MdiActiveDocument;
                using (doc.LockDocument())
                {
                    _builder.ApplyTotalCmh(_rootNode, _db, ductType, bMin, bMax);
                }
                MessageBox.Show(
                    $"[{ductType}] b={bMin}..{bMax} — Total_CMH / Tree / a / b / Disp XData 저장됨.",
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
