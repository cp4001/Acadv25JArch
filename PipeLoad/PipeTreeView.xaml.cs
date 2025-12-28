using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PipeLoadAnalysis
{
    /// <summary>
    /// TreeView에 표시할 Node 데이터 모델
    /// </summary>
    public class PipeTreeNodeViewModel
    {
        public string DisplayText { get; set; }
        public Brush NodeColor { get; set; }
        public Brush TextColor { get; set; }
        public FontWeight FontWeight { get; set; }
        public ObservableCollection<PipeTreeNodeViewModel> Children { get; set; }
        public PipeNode PipeNodeData { get; set; }

        public PipeTreeNodeViewModel()
        {
            Children = new ObservableCollection<PipeTreeNodeViewModel>();
            FontWeight = FontWeights.Normal;
        }
    }

    /// <summary>
    /// PipeTreeView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PipeTreeView : UserControl
    {
        private PipeGraph pipeGraph;
        private PipeNode rootNode;

        public PipeTreeView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Graph 데이터로 TreeView 초기화
        /// </summary>
        public void LoadPipeGraph(PipeGraph graph, PipeNode root)
        {
            pipeGraph = graph;
            rootNode = root;

            // 통계 정보 업데이트
            txtStatistics.Text = $"총 Node: {graph.Nodes.Count}개 | 총 Edge: {graph.Edges.Count}개\n" +
                               $"Root 부하: {root.TotalLoad:F2}";

            // TreeView 구성
            BuildTreeView();
        }

        /// <summary>
        /// TreeView 구조 생성
        /// </summary>
        private void BuildTreeView()
        {
            treeView.Items.Clear();

            if (rootNode == null || pipeGraph == null)
                return;

            // 모든 Node 방문 플래그 초기화
            foreach (var node in pipeGraph.Nodes)
            {
                node.IsVisited = false;
            }

            // Root ViewModel 생성
            var rootViewModel = CreateTreeNodeViewModel(rootNode, null, true);

            // 재귀적으로 하위 Node 추가
            BuildTreeRecursive(rootNode, rootViewModel, null);

            // TreeView에 추가
            treeView.Items.Add(rootViewModel);

            // 첫 번째 레벨까지 펼치기
            if (treeView.Items.Count > 0)
            {
                var item = treeView.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
                if (item != null)
                {
                    item.IsExpanded = true;
                }
            }
        }

        /// <summary>
        /// ViewModel 생성 (PipeNode → PipeTreeNodeViewModel)
        /// </summary>
        private PipeTreeNodeViewModel CreateTreeNodeViewModel(PipeNode pipeNode, PipeEdge fromEdge, bool isRoot)
        {
            var viewModel = new PipeTreeNodeViewModel
            {
                PipeNodeData = pipeNode
            };

            // 텍스트 구성
            string nodeText = $"Node[{pipeNode.NodeId}] - 부하: {pipeNode.TotalLoad:F2} - 연결: {pipeNode.ConnectedEdges.Count}개";

            // Leaf인 경우 Xdata 표시
            if (pipeNode.IsLeaf() && fromEdge != null && fromEdge.LoadValue > 0)
            {
                nodeText += $" [Xdata: {fromEdge.LoadValue:F2}]";
            }

            viewModel.DisplayText = nodeText;

            // 색상 및 스타일 설정
            if (isRoot)
            {
                // Root Node - 빨강
                viewModel.NodeColor = Brushes.Red;
                viewModel.TextColor = Brushes.Red;
                viewModel.FontWeight = FontWeights.Bold;
            }
            else if (pipeNode.IsLeaf())
            {
                // Leaf Node - 초록
                viewModel.NodeColor = Brushes.Green;
                viewModel.TextColor = Brushes.Green;
                viewModel.FontWeight = FontWeights.Normal;
            }
            else
            {
                // 중간 Node - 파랑
                viewModel.NodeColor = Brushes.Blue;
                viewModel.TextColor = Brushes.Blue;
                viewModel.FontWeight = FontWeights.Normal;
            }

            return viewModel;
        }

        /// <summary>
        /// 재귀적으로 Tree 구조 생성
        /// </summary>
        private void BuildTreeRecursive(PipeNode currentNode, PipeTreeNodeViewModel currentViewModel, PipeNode parentNode)
        {
            currentNode.IsVisited = true;

            // 연결된 모든 Edge 탐색
            var childViewModels = new List<PipeTreeNodeViewModel>();

            foreach (var edge in currentNode.ConnectedEdges)
            {
                var nextNode = edge.GetOtherNode(currentNode);

                // 부모로 역행하지 않음
                if (nextNode == parentNode || nextNode == null)
                    continue;

                // 이미 방문한 Node는 스킵 (순환 방지)
                if (nextNode.IsVisited)
                    continue;

                // 자식 ViewModel 생성
                var childViewModel = CreateTreeNodeViewModel(nextNode, edge, false);
                childViewModels.Add(childViewModel);

                // Leaf가 아니면 재귀 호출
                if (!nextNode.IsLeaf())
                {
                    BuildTreeRecursive(nextNode, childViewModel, currentNode);
                }
                else
                {
                    nextNode.IsVisited = true;
                }
            }

            // 부하 순으로 정렬 (내림차순)
            childViewModels.Sort((a, b) => b.PipeNodeData.TotalLoad.CompareTo(a.PipeNodeData.TotalLoad));

            // 정렬된 순서로 Children에 추가
            foreach (var child in childViewModels)
            {
                currentViewModel.Children.Add(child);
            }
        }

        /// <summary>
        /// TreeView 선택 이벤트
        /// </summary>
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is PipeTreeNodeViewModel selectedViewModel)
            {
                var node = selectedViewModel.PipeNodeData;
                // 선택된 Node 정보를 표시 (확장 가능)
                txtStatistics.Text = $"선택된 Node[{node.NodeId}] - 부하: {node.TotalLoad:F2} - 연결: {node.ConnectedEdges.Count}개\n" +
                                   $"위치: ({node.Position.X:F1}, {node.Position.Y:F1})";
            }
        }

        /// <summary>
        /// TreeView 더블클릭 이벤트
        /// </summary>
        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem is PipeTreeNodeViewModel selectedViewModel)
            {
                var node = selectedViewModel.PipeNodeData;

                string details = $"Node ID: {node.NodeId}\n" +
                               $"위치: ({node.Position.X:F2}, {node.Position.Y:F2}, {node.Position.Z:F2})\n" +
                               $"총 부하: {node.TotalLoad:F2}\n" +
                               $"연결 수: {node.ConnectedEdges.Count}\n" +
                               $"타입: {(node.IsLeaf() ? "Leaf" : node == rootNode ? "Root" : "중간 Node")}";

                MessageBox.Show(details, "Node 상세 정보", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 모든 Node 펼치기
        /// </summary>
        public void ExpandAll()
        {
            ExpandAllNodes(treeView.Items);
        }

        /// <summary>
        /// 재귀적으로 모든 TreeViewItem 펼치기
        /// </summary>
        private void ExpandAllNodes(ItemCollection items)
        {
            foreach (var item in items)
            {
                var treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null)
                {
                    treeViewItem.IsExpanded = true;
                    ExpandAllNodes(treeViewItem.Items);
                }
            }
        }

        /// <summary>
        /// 모든 Node 접기
        /// </summary>
        public void CollapseAll()
        {
            CollapseAllNodes(treeView.Items);
        }

        /// <summary>
        /// 재귀적으로 모든 TreeViewItem 접기
        /// </summary>
        private void CollapseAllNodes(ItemCollection items)
        {
            foreach (var item in items)
            {
                var treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null)
                {
                    treeViewItem.IsExpanded = false;
                    CollapseAllNodes(treeViewItem.Items);
                }
            }
        }
    }
}
