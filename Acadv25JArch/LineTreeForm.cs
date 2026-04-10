using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Font = System.Drawing.Font;

namespace PipeLoad2
{
    /// <summary>
    /// LineTree WinForms 표시 커맨드
    /// </summary>
    public class LineTreeFormCommand
    {
        [CommandMethod("LINETREE_FORM")]
        public void Cmd_LineTreeForm()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1. Line 선택
                TypedValue[] filterList = [new TypedValue((int)DxfCode.Start, "LINE")];
                var filter = new SelectionFilter(filterList);
                var opts = new PromptSelectionOptions { MessageForAdding = "\nLine들을 선택하세요: " };
                var psr = ed.GetSelection(opts, filter);
                if (psr.Status != PromptStatus.OK) return;
                var selectedIds = psr.Value.GetObjectIds().ToList();

                // 2. Root Line 선택
                var peo = new PromptEntityOptions("\nRoot Line을 선택하세요: ");
                peo.SetRejectMessage("\nLine만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Line), true);
                var per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                // 3. Transaction - ForRead만 사용
                using var tr = db.TransactionManager.StartTransaction();

                var rootLine = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
                if (rootLine == null) { tr.Commit(); return; }

                var allLines = selectedIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(l => l != null && l.Layer == rootLine.Layer)
                    .ToList();

                ed.WriteMessage($"\n레이어 [{rootLine.Layer}] 필터: {allLines.Count}개 Line");

                // 4. Tree 구성
                var builder = new LineTreeBuilder();
                var connections = builder.BuildConnectionGraph(allLines);
                var rootNode = builder.BuildTreeStructureBFS(rootLine, allLines, connections);
                builder.CalculateLoads(rootNode);

                int totalNodes = builder.CountNodes(rootNode);
                int leafCount = builder.CountLeafNodes(rootNode);

                tr.Commit();

                // 5. Form 표시 (AutoCAD UI 스레드)
                Application.ShowModelessDialog(
                    new LineTreeForm(rootNode, totalNodes, leafCount, rootLine.Layer));
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// LineTree WinForms Form
    /// </summary>
    public class LineTreeForm : Form
    {
        private TreeView _treeView;
        private Label _lblStats;
        private Button _btnClose;
        private Button _btnExpand;
        private Button _btnCollapse;

        public LineTreeForm(LineTreeBuilder.LineNode rootNode, int totalNodes, int leafCount, string layer)
        {
            InitializeComponents();
            this.Text = $"Line Tree 분석 — 레이어: {layer}";

            // 통계 표시
            int midCount = totalNodes - leafCount - 1;
            _lblStats.Text =
                $"레이어: {layer}  |  총 노드: {totalNodes}  |  " +
                $"Root: 1  Mid: {midCount}  Leaf: {leafCount}  |  " +
                $"총 부하: {rootNode.Load:F2}  |  총 Leaf: {rootNode.LeafCount}";

            // TreeView 구성
            _treeView.BeginUpdate();
            var rootTvNode = CreateTreeNode(rootNode);
            _treeView.Nodes.Add(rootTvNode);
            _treeView.EndUpdate();
            _treeView.ExpandAll();
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

            string label = $"{symbol} [{node.Handle}]  Lv={node.Level}  {loadInfo}";

            var tvNode = new TreeNode(label);

            // 색상
            tvNode.ForeColor = node.Type switch
            {
                LineTreeBuilder.NodeType.Root => Color.Red,
                LineTreeBuilder.NodeType.Mid  => Color.Blue,
                LineTreeBuilder.NodeType.Leaf => Color.Green,
                _ => Color.Black
            };

            foreach (var child in node.Children)
                tvNode.Nodes.Add(CreateTreeNode(child));

            return tvNode;
        }

        private void InitializeComponents()
        {
            this.Size = new Size(700, 600);
            this.MinimumSize = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("맑은 고딕", 9f);

            // 통계 라벨
            _lblStats = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(6, 0, 0, 0),
                Font = new Font("맑은 고딕", 8.5f)
            };

            // TreeView
            _treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5f),
                ShowLines = true,
                ShowPlusMinus = true,
                FullRowSelect = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 버튼 패널
            var panel = new Panel { Dock = DockStyle.Bottom, Height = 38 };

            _btnExpand = new Button { Text = "전체 펼치기", Width = 100, Left = 6, Top = 6, Height = 26 };
            _btnCollapse = new Button { Text = "전체 접기", Width = 100, Left = 112, Top = 6, Height = 26 };
            _btnClose = new Button { Text = "닫기", Width = 80, Height = 26, Top = 6 };
            _btnClose.Left = panel.Width - _btnClose.Width - 10;
            _btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;

            _btnExpand.Click   += (s, e) => _treeView.ExpandAll();
            _btnCollapse.Click += (s, e) => _treeView.CollapseAll();
            _btnClose.Click    += (s, e) => this.Close();

            panel.Controls.AddRange(new Control[] { _btnExpand, _btnCollapse, _btnClose });

            this.Controls.Add(_treeView);
            this.Controls.Add(_lblStats);
            this.Controls.Add(panel);
        }
    }
}
