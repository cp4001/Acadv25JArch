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
    /// TreeView에 표시할 Line Node 데이터 모델
    /// </summary>
    public class PipeLineTreeNodeViewModel
    {
        public string DisplayText { get; set; }
        public Brush NodeColor { get; set; }
        public Brush TextColor { get; set; }
        public FontWeight FontWeight { get; set; }
        public ObservableCollection<PipeLineTreeNodeViewModel> Children { get; set; }
        public PipeLineNode LineNodeData { get; set; }

        public PipeLineTreeNodeViewModel()
        {
            Children = new ObservableCollection<PipeLineTreeNodeViewModel>();
            FontWeight = FontWeights.Normal;
        }
    }

    /// <summary>
    /// PipeTreeViewV2.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PipeTreeViewV2 : UserControl
    {
        private PipeLineGraph pipeLineGraph;
        private PipeLineNode rootLineNode;

        public PipeTreeViewV2()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Graph 데이터로 TreeView 초기화
        /// </summary>
        public void LoadPipeGraph(PipeLineGraph graph, PipeLineNode root)
        {
            pipeLineGraph = graph;
            rootLineNode = root;

            // 통계 정보 업데이트
            txtStatistics.Text = $"총 Line: {graph.Nodes.Count}개\n" +
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

            if (rootLineNode == null || pipeLineGraph == null)
                return;

            // 모든 Node 방문 플래그 초기화
            foreach (var node in pipeLineGraph.Nodes)
            {
                node.IsVisited = false;
            }

            // Root ViewModel 생성
            var rootViewModel = CreateTreeNodeViewModel(rootLineNode, true);

            // 재귀적으로 하위 Node 추가
            BuildTreeRecursive(rootLineNode, rootViewModel, null);

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
        /// ViewModel 생성 (PipeLineNode → PipeLineTreeNodeViewModel)
        /// </summary>
        private PipeLineTreeNodeViewModel CreateTreeNodeViewModel(PipeLineNode lineNode, bool isRoot, bool isLeafInTree = false)
        {
            var viewModel = new PipeLineTreeNodeViewModel
            {
                LineNodeData = lineNode
            };

            // 텍스트 구성
            string nodeText = $"Line[{lineNode.LineHandle}] - 부하: {lineNode.TotalLoad:F2} - 연결: {lineNode.ConnectedNodes.Count}개";

            // 디버그: 연결 개수 표시
            System.Diagnostics.Debug.WriteLine($"CreateTreeNodeViewModel: Line[{lineNode.LineHandle}], ConnectedCount={lineNode.ConnectedNodes.Count}, IsLeaf={isLeafInTree}");

            // Leaf인 경우 Xdata 표시
            if (isLeafInTree && lineNode.LoadValue > 0)
            {
                nodeText += $" [Xdata: {lineNode.LoadValue:F2}]";
            }

            viewModel.DisplayText = nodeText;

            // 색상 및 스타일 설정
            if (isRoot)
            {
                // Root Line - 빨강
                viewModel.NodeColor = Brushes.Red;
                viewModel.TextColor = Brushes.Red;
                viewModel.FontWeight = FontWeights.Bold;
            }
            else if (isLeafInTree)
            {
                // Leaf Line - 초록
                viewModel.NodeColor = Brushes.Green;
                viewModel.TextColor = Brushes.Green;
                viewModel.FontWeight = FontWeights.Normal;
                System.Diagnostics.Debug.WriteLine($"  -> LEAF 설정! 초록색");
            }
            else
            {
                // 중간 Line - 파랑
                viewModel.NodeColor = Brushes.Blue;
                viewModel.TextColor = Brushes.Blue;
                viewModel.FontWeight = FontWeights.Normal;
            }

            return viewModel;
        }

        /// <summary>
        /// 재귀적으로 Tree 구조 생성
        /// </summary>
        private void BuildTreeRecursive(PipeLineNode currentNode, PipeLineTreeNodeViewModel currentViewModel, PipeLineNode parentNode)
        {
            currentNode.IsVisited = true;

            // 연결된 모든 Line Node 탐색
            var childViewModels = new List<PipeLineTreeNodeViewModel>();

            foreach (var nextNode in currentNode.ConnectedNodes)
            {
                // 부모로 역행하지 않음
                if (nextNode == parentNode)
                    continue;

                // 이미 방문한 Node는 스킵 (순환 방지)
                if (nextNode.IsVisited)
                    continue;

                // 물리적 Leaf 여부 판단 (한쪽 끝점만 연결)
                bool isPhysicalLeaf = nextNode.IsLeaf();

                // 자식 ViewModel 생성 (물리적 Leaf 사용)
                var childViewModel = CreateTreeNodeViewModel(nextNode, false, isPhysicalLeaf);
                childViewModels.Add(childViewModel);

                // 재귀적으로 하위 탐색
                BuildTreeRecursive(nextNode, childViewModel, currentNode);
            }

            // 부하 순으로 정렬 (내림차순)
            childViewModels.Sort((a, b) => b.LineNodeData.TotalLoad.CompareTo(a.LineNodeData.TotalLoad));

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
            if (e.NewValue is PipeLineTreeNodeViewModel selectedViewModel)
            {
                var node = selectedViewModel.LineNodeData;
                // 선택된 Line Node 정보를 표시
                txtStatistics.Text = $"선택된 Line[{node.LineHandle}] - 부하: {node.TotalLoad:F2} - 연결: {node.ConnectedNodes.Count}개";
            }
        }

        /// <summary>
        /// TreeView 더블클릭 이벤트
        /// </summary>
        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem is PipeLineTreeNodeViewModel selectedViewModel)
            {
                var node = selectedViewModel.LineNodeData;

                string details = $"Line Handle: {node.LineHandle}\n" +
                               $"총 부하: {node.TotalLoad:F2}\n" +
                               $"연결 수: {node.ConnectedNodes.Count}\n" +
                               $"타입: {(node.IsLeaf() ? "Leaf" : node == rootLineNode ? "Root" : "중간 Line")}";

                if (node.IsLeaf())
                {
                    details += $"\nXdata 부하: {node.LoadValue:F2}";
                }

                MessageBox.Show(details, "Line 상세 정보", MessageBoxButton.OK, MessageBoxImage.Information);
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
