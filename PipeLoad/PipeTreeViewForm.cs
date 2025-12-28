using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PipeLoadAnalysis
{
    /// <summary>
    /// 배관 네트워크 Tree 구조를 표시하는 WinForms Form
    /// </summary>
    public class PipeTreeViewForm : Form
    {
        private TreeView treeView;
        private PipeGraph pipeGraph;
        private PipeNode rootNode;
        private Label lblTitle;
        private Panel topPanel;

        public PipeTreeViewForm(PipeGraph graph, PipeNode root)
        {
            pipeGraph = graph;
            rootNode = root;
            InitializeComponents();
            BuildTreeView();
        }

        /// <summary>
        /// Form 및 컨트롤 초기화
        /// </summary>
        private void InitializeComponents()
        {
            // Form 설정
            this.Text = "배관 네트워크 Tree 구조";
            this.Size = new Size(600, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = true;
            this.MaximizeBox = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // 상단 정보 패널
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10)
            };

            // 제목 라벨
            lblTitle = new Label
            {
                Text = "배관 네트워크 분석 결과",
                Font = new Font("맑은 고딕", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            // 통계 정보 라벨
            var lblStats = new Label
            {
                Text = $"총 Node: {pipeGraph.Nodes.Count}개 | 총 Edge: {pipeGraph.Edges.Count}개\n" +
                       $"Root 부하: {rootNode.TotalLoad:F2}",
                Font = new Font("맑은 고딕", 9),
                AutoSize = true,
                Location = new Point(10, 40)
            };

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblStats);

            // TreeView 설정
            treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 9),
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                FullRowSelect = true,
                HideSelection = false
            };

            // TreeView 이벤트 핸들러
            treeView.AfterSelect += TreeView_AfterSelect;
            treeView.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;

            // 범례 패널
            var legendPanel = CreateLegendPanel();

            // Form에 컨트롤 추가
            this.Controls.Add(treeView);
            this.Controls.Add(legendPanel);
            this.Controls.Add(topPanel);
        }

        /// <summary>
        /// 범례 패널 생성
        /// </summary>
        private Panel CreateLegendPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(10)
            };

            var lblLegend = new Label
            {
                Text = "■ Root Node   ■ 중간 Node   ■ Leaf Node",
                Font = new Font("맑은 고딕", 9),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(10, 20)
            };

            // 색상 박스들
            var rootBox = new Label
            {
                BackColor = Color.Red,
                Size = new Size(15, 15),
                Location = new Point(10, 20)
            };

            var middleBox = new Label
            {
                BackColor = Color.Blue,
                Size = new Size(15, 15),
                Location = new Point(100, 20)
            };

            var leafBox = new Label
            {
                BackColor = Color.Green,
                Size = new Size(15, 15),
                Location = new Point(190, 20)
            };

            panel.Controls.Add(rootBox);
            panel.Controls.Add(middleBox);
            panel.Controls.Add(leafBox);

            return panel;
        }

        /// <summary>
        /// TreeView 구조 생성
        /// </summary>
        private void BuildTreeView()
        {
            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            // Root Node부터 시작
            var rootTreeNode = CreateTreeNode(rootNode, null);
            treeView.Nodes.Add(rootTreeNode);

            // 모든 Node를 방문하지 않은 상태로 초기화
            foreach (var node in pipeGraph.Nodes)
            {
                node.IsVisited = false;
            }

            // 재귀적으로 하위 Node 추가
            BuildTreeRecursive(rootNode, rootTreeNode, null);

            // Root Node 펼치기
            rootTreeNode.Expand();

            treeView.EndUpdate();
        }

        /// <summary>
        /// TreeNode 생성 (PipeNode 정보 기반)
        /// </summary>
        private TreeNode CreateTreeNode(PipeNode pipeNode, PipeEdge fromEdge)
        {
            // Node 텍스트 구성
            string nodeText = $"Node[{pipeNode.NodeId}] - 부하: {pipeNode.TotalLoad:F2} - 연결: {pipeNode.ConnectedEdges.Count}개";

            // 위치 정보 추가
            nodeText += $" ({pipeNode.Position.X:F1}, {pipeNode.Position.Y:F1})";

            var treeNode = new TreeNode(nodeText);

            // Tag에 PipeNode 저장 (나중에 사용 가능)
            treeNode.Tag = pipeNode;

            // Node 타입에 따라 색상 구분
            if (pipeNode == rootNode)
            {
                // Root Node - 빨강
                treeNode.ForeColor = Color.Red;
                treeNode.NodeFont = new Font(treeView.Font, FontStyle.Bold);
            }
            else if (pipeNode.IsLeaf())
            {
                // Leaf Node - 초록
                treeNode.ForeColor = Color.Green;
                
                // Leaf의 부하값 표시
                if (fromEdge != null && fromEdge.LoadValue > 0)
                {
                    treeNode.Text += $" [Xdata: {fromEdge.LoadValue:F2}]";
                }
            }
            else
            {
                // 중간 Node - 파랑
                treeNode.ForeColor = Color.Blue;
            }

            return treeNode;
        }

        /// <summary>
        /// 재귀적으로 TreeNode 구조 생성
        /// </summary>
        private void BuildTreeRecursive(PipeNode currentNode, TreeNode currentTreeNode, PipeNode parentNode)
        {
            currentNode.IsVisited = true;

            // 연결된 모든 Edge 탐색
            foreach (var edge in currentNode.ConnectedEdges)
            {
                var nextNode = edge.GetOtherNode(currentNode);

                // 부모로 역행하지 않음
                if (nextNode == parentNode || nextNode == null)
                    continue;

                // 이미 방문한 Node는 스킵 (순환 방지)
                if (nextNode.IsVisited)
                    continue;

                // 자식 TreeNode 생성
                var childTreeNode = CreateTreeNode(nextNode, edge);
                currentTreeNode.Nodes.Add(childTreeNode);

                // Leaf가 아니면 재귀 호출
                if (!nextNode.IsLeaf())
                {
                    BuildTreeRecursive(nextNode, childTreeNode, currentNode);
                }
                else
                {
                    nextNode.IsVisited = true;
                }
            }

            // 자식 Node들을 부하 순으로 정렬
            SortChildNodes(currentTreeNode);
        }

        /// <summary>
        /// 자식 TreeNode들을 부하값 기준으로 정렬
        /// </summary>
        private void SortChildNodes(TreeNode parentNode)
        {
            if (parentNode.Nodes.Count <= 1)
                return;

            // TreeNode 배열로 변환
            var nodes = new TreeNode[parentNode.Nodes.Count];
            parentNode.Nodes.CopyTo(nodes, 0);

            // 부하값 기준 정렬 (내림차순)
            Array.Sort(nodes, (a, b) =>
            {
                var pipeNodeA = a.Tag as PipeNode;
                var pipeNodeB = b.Tag as PipeNode;

                if (pipeNodeA == null || pipeNodeB == null)
                    return 0;

                return pipeNodeB.TotalLoad.CompareTo(pipeNodeA.TotalLoad);
            });

            // 정렬된 순서로 다시 추가
            parentNode.Nodes.Clear();
            parentNode.Nodes.AddRange(nodes);
        }

        /// <summary>
        /// TreeView Node 선택 이벤트
        /// </summary>
        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is PipeNode selectedNode)
            {
                // 선택된 Node 정보를 상태 표시줄에 표시 (확장 가능)
                this.Text = $"배관 네트워크 Tree 구조 - Node[{selectedNode.NodeId}] 선택됨";
            }
        }

        /// <summary>
        /// TreeView Node 더블클릭 이벤트
        /// </summary>
        private void TreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // 더블클릭 시 해당 Node의 상세 정보 표시
            if (e.Node.Tag is PipeNode selectedNode)
            {
                string details = $"Node ID: {selectedNode.NodeId}\n" +
                               $"위치: ({selectedNode.Position.X:F2}, {selectedNode.Position.Y:F2}, {selectedNode.Position.Z:F2})\n" +
                               $"총 부하: {selectedNode.TotalLoad:F2}\n" +
                               $"연결 수: {selectedNode.ConnectedEdges.Count}\n" +
                               $"타입: {(selectedNode.IsLeaf() ? "Leaf" : selectedNode == rootNode ? "Root" : "중간 Node")}";

                MessageBox.Show(details, "Node 상세 정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 모든 Node 펼치기 메서드 (공개)
        /// </summary>
        public void ExpandAll()
        {
            treeView.ExpandAll();
        }

        /// <summary>
        /// 모든 Node 접기 메서드 (공개)
        /// </summary>
        public void CollapseAll()
        {
            treeView.CollapseAll();
        }

        /// <summary>
        /// 특정 깊이까지만 펼치기
        /// </summary>
        public void ExpandToLevel(int level)
        {
            treeView.CollapseAll();
            ExpandNodesRecursive(treeView.Nodes, 0, level);
        }

        /// <summary>
        /// 재귀적으로 특정 레벨까지 펼치기
        /// </summary>
        private void ExpandNodesRecursive(TreeNodeCollection nodes, int currentLevel, int targetLevel)
        {
            if (currentLevel >= targetLevel)
                return;

            foreach (TreeNode node in nodes)
            {
                node.Expand();
                if (node.Nodes.Count > 0)
                {
                    ExpandNodesRecursive(node.Nodes, currentLevel + 1, targetLevel);
                }
            }
        }
    }
}
