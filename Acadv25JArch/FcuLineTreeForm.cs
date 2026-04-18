using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Drawing;
using System.Windows.Forms;
using Node     = PipeLoad2.FcuLineTreeBuilder.FcuNode;
using NodeType = PipeLoad2.FcuLineTreeBuilder.FcuNodeType;
using CalcMode = PipeLoad2.FcuLineTreeBuilder.CalcMode;

namespace PipeLoad2
{
    public partial class FcuLineTreeForm : Form
    {
        private readonly Node                 _rootNode;
        private readonly Database             _db;
        private readonly FcuLineTreeBuilder   _builder = new();
        private readonly string               _layer;
        private int                           _head;
        private CalcMode                      _mode;

        public FcuLineTreeForm(Node rootNode, string layer, int head, Database db,
                               CalcMode mode = CalcMode.Supply)
        {
            InitializeComponent();

            _rootNode = rootNode;
            _db       = db;
            _layer    = layer;
            _head     = head;
            _mode     = mode;

            foreach (int h in FcuLineTreeBuilder.HeadOptions)
                cmbHead.Items.Add(h.ToString());
            cmbHead.SelectedItem = head.ToString();

            cmbMode.Items.Add("Supply (공급/H-W)");
            cmbMode.Items.Add("Drain (배수/수량)");
            cmbMode.SelectedIndex = mode == CalcMode.Drain ? 1 : 0;

            UpdateHeadEnabled();
            this.Text = $"FCU Line Tree 분석 — 레이어: {layer}";
            Recalculate();
        }

        private void UpdateHeadEnabled()
        {
            cmbHead.Enabled = _mode == CalcMode.Supply;
        }

        private void Recalculate()
        {
            _builder.CalculateDiameters(_rootNode, _head, _mode);

            int totalNodes = _builder.CountNodes(_rootNode);
            int lineNodes  = _builder.CountLineNodes(_rootNode);
            int blockNodes = _builder.CountBlockNodes(_rootNode);
            int leafLines  = _builder.CountLeafLines(_rootNode);
            int midLines   = lineNodes - leafLines - 1;

            string modeTag = _mode == CalcMode.Drain ? "배수" : "공급";
            string calcTag = _mode == CalcMode.Drain
                ? $"FCU={_rootNode.LeafCount}대 → Root 관경: {_rootNode.Diameter}"
                : $"수두 {_head} mmAq/m | 총 유량 {_rootNode.Load:F1} LPM | Root 관경: {_rootNode.Diameter}" +
                  (_rootNode.Velocity > FcuLineTreeBuilder.V_WARN
                      ? $" ⚠ V={_rootNode.Velocity:F2} m/s"
                      : $" V={_rootNode.Velocity:F2} m/s");

            lblStats.Text =
                $"[{modeTag}] {_layer} | 총 {totalNodes} (Root=1, Mid={midCountClamp(midLines)}, " +
                $"Leaf={leafLines}, FCU={blockNodes}) | {calcTag}";

            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            treeView.Nodes.Add(CreateTreeNode(_rootNode));
            treeView.EndUpdate();
            treeView.ExpandAll();
        }

        private static int midCountClamp(int v) => v < 0 ? 0 : v;

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
                label = $"{symbol} [{node.Handle}]  FCU={node.BlockName}  LPM={node.Load:F1}";
            }
            else if (_mode == CalcMode.Drain)
            {
                label =
                    $"{symbol} [{node.Handle}]  Lv={node.Level}  FCU={node.LeafCount}대  " +
                    $"관경={node.Diameter}  (참고 유량={node.Load:F1} LPM)";
            }
            else
            {
                string warn = node.Velocity > FcuLineTreeBuilder.V_WARN ? " ⚠" : "";
                label =
                    $"{symbol} [{node.Handle}]  Lv={node.Level}  유량={node.Load:F1} LPM  " +
                    $"관경={node.Diameter}  Vmax={node.MaxFlow:F1}  V={node.Velocity:F2} m/s{warn}  " +
                    $"FCU={node.LeafCount}";
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
                }
            };

            if (_mode == CalcMode.Supply &&
                node.Velocity > FcuLineTreeBuilder.V_WARN &&
                node.Type != NodeType.Block)
                tv.BackColor = Color.FromArgb(255, 240, 220);

            foreach (var c in node.Children) tv.Nodes.Add(CreateTreeNode(c));
            return tv;
        }

        private void cmbHead_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbHead.SelectedItem?.ToString(), out int h))
            {
                _head = h;
                if (_mode == CalcMode.Supply) Recalculate();
            }
        }

        private void cmbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mode = cmbMode.SelectedIndex == 1 ? CalcMode.Drain : CalcMode.Supply;
            UpdateHeadEnabled();
            Recalculate();
        }

        private void btnRecalc_Click(object sender, EventArgs e)   => Recalculate();
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
                string modeTag = _mode == CalcMode.Drain ? "배수" : "공급";
                MessageBox.Show($"[{modeTag}] 관경이 XData \"Dia\"에 저장되었습니다.",
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
