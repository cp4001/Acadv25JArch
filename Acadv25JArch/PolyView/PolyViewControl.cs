using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch.PolyView
{
    /// <summary>
    /// PolyView 관리 팔레트의 내용. TreeView 단일 구성.
    /// 사양: PolyViewManager.md
    /// </summary>
    public partial class PolyViewControl : UserControl
    {
        private PolyViewGroup _root = new();
        private Dictionary<string, PolyViewItem> _items = new(StringComparer.OrdinalIgnoreCase);
        private bool _building;

        private class NodeTag
        {
            public PolyViewGroup? Group;    // 그룹/Root 노드
            public PolyViewItem? Item;      // 정상 PolyView 노드
            public string? Handle;          // PolyView 노드 (누락 포함)
            public bool IsRoot;
            public bool IsMissing => Group == null && Item == null;
        }

        public PolyViewControl()
        {
            InitializeComponent();

            tsbRefresh.Click += (s, e) => RefreshTree();
            tsbNewGroup.Click += (s, e) => NewGroupUnderSelection();
            tsbPurge.Click += (s, e) => PurgeMissing();
            tsbExpand.Click += (s, e) => treeView.ExpandAll();
            tsbCollapse.Click += (s, e) => CollapseToRoot();

            treeView.NodeMouseClick += TreeView_NodeMouseClick;
            treeView.BeforeLabelEdit += TreeView_BeforeLabelEdit;
            treeView.AfterLabelEdit += TreeView_AfterLabelEdit;
            treeView.ItemDrag += TreeView_ItemDrag;
            treeView.DragEnter += TreeView_DragEnter;
            treeView.DragOver += TreeView_DragOver;
            treeView.DragDrop += TreeView_DragDrop;
        }

        #region 트리 구성

        public void ClearTree()
        {
            treeView.Nodes.Clear();
            _root = new PolyViewGroup();
            _items = new Dictionary<string, PolyViewItem>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>도면을 다시 읽어 트리를 만든다. 펼침 상태는 보존.</summary>
        public void RefreshTree()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ClearTree();
                treeView.Nodes.Add(new TreeNode("열린 도면 없음"));
                return;
            }

            var expanded = CollectExpandedPaths();
            string? selectedPath = PathOf(treeView.SelectedNode);

            try
            {
                using (doc.LockDocument())
                {
                    _root = PolyViewStore.BuildTree(doc.Database, out _items);
                }
            }
            catch (System.Exception ex)
            {
                ClearTree();
                treeView.Nodes.Add(new TreeNode("읽기 오류: " + ex.Message));
                return;
            }

            _building = true;
            treeView.BeginUpdate();
            try
            {
                treeView.Nodes.Clear();

                var rootNode = new TreeNode(System.IO.Path.GetFileName(doc.Name))
                {
                    Tag = new NodeTag { Group = _root, IsRoot = true }
                };
                treeView.Nodes.Add(rootNode);
                FillGroup(rootNode, _root);

                RestoreExpanded(expanded);
                rootNode.Expand();
                SelectByPath(selectedPath);
            }
            finally
            {
                treeView.EndUpdate();
                _building = false;
            }
        }

        private void FillGroup(TreeNode parentNode, PolyViewGroup group)
        {
            // 그룹 먼저, 그 다음 PolyView 항목 (각각 저장된 순서 유지)
            foreach (var g in group.Groups)
            {
                var node = new TreeNode($"{g.Name}  ({PolyViewStore.CountItems(g)})")
                {
                    Tag = new NodeTag { Group = g }
                };
                parentNode.Nodes.Add(node);
                FillGroup(node, g);
            }

            foreach (var h in group.Handles)
            {
                _items.TryGetValue(h, out PolyViewItem? item);

                TreeNode node;
                if (item != null)
                {
                    node = new TreeNode(DisplayText(item))
                    {
                        Tag = new NodeTag { Item = item, Handle = h }
                    };
                }
                else
                {
                    node = new TreeNode($"(누락)  {h}")
                    {
                        Tag = new NodeTag { Handle = h },
                        ForeColor = System.Drawing.SystemColors.GrayText
                    };
                }
                parentNode.Nodes.Add(node);
            }
        }

        /// 노드 표시 텍스트: "이름 - 내용" (내용이 없으면 이름만). 줄바꿈은 공백으로 눕힌다.
        private static string DisplayText(PolyViewItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Memo)) return item.Name;
            string memo = item.Memo.Replace("\r\n", " ").Replace("\n", " ").Trim();
            return $"{item.Name} - {memo}";
        }

        private HashSet<string> CollectExpandedPaths()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode n in nodes)
                {
                    if (n.IsExpanded)
                    {
                        string? p = PathOf(n);
                        if (p != null) set.Add(p);
                    }
                    Walk(n.Nodes);
                }
            }
            Walk(treeView.Nodes);
            return set;
        }

        private void RestoreExpanded(HashSet<string> expanded)
        {
            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode n in nodes)
                {
                    string? p = PathOf(n);
                    if (p != null && expanded.Contains(p)) n.Expand();
                    Walk(n.Nodes);
                }
            }
            Walk(treeView.Nodes);
        }

        /// 노드의 식별 경로. 그룹은 이름 경로, 항목은 Handle 로 식별한다.
        private string? PathOf(TreeNode? node)
        {
            if (node?.Tag is not NodeTag tag) return null;
            if (tag.IsRoot) return "\\";
            if (tag.Group != null)
            {
                string? parent = PathOf(node.Parent);
                return parent == null ? null : parent + "\\G:" + tag.Group.Name;
            }
            return "H:" + tag.Handle;
        }

        private void SelectByPath(string? path)
        {
            if (path == null) return;
            TreeNode? found = null;
            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode n in nodes)
                {
                    if (found != null) return;
                    if (PathOf(n) == path) { found = n; return; }
                    Walk(n.Nodes);
                }
            }
            Walk(treeView.Nodes);
            if (found != null) treeView.SelectedNode = found;
        }

        private void CollapseToRoot()
        {
            treeView.CollapseAll();
            if (treeView.Nodes.Count > 0) treeView.Nodes[0].Expand();
        }

        #endregion

        #region 클릭 / 줌 / 컨텍스트 메뉴

        private void TreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (_building) return;
            if (!e.Node.Bounds.Contains(e.Location)) return;   // +/- 클릭은 무시

            treeView.SelectedNode = e.Node;

            if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Node, e.Location);
                return;
            }

            if (e.Button == MouseButtons.Left && e.Node.Tag is NodeTag tag && tag.Item != null)
                ZoomTo(tag.Item);
        }

        private void ZoomTo(PolyViewItem item)
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            if (doc.Database.TileMode == false)
            {
                MessageBox.Show("모델 공간에서 실행하세요.", "PolyView",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (doc.LockDocument())
                {
                    doc.Editor.ZoomToEntities(new[] { item.Id }, 1.1);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("확대에 실패했습니다: " + ex.Message, "PolyView",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SelectInDrawing(PolyViewItem item)
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            try
            {
                using (doc.LockDocument())
                {
                    doc.Editor.SetImpliedSelection(new[] { item.Id });
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("선택에 실패했습니다: " + ex.Message, "PolyView",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowContextMenu(TreeNode node, System.Drawing.Point location)
        {
            if (node.Tag is not NodeTag tag) return;

            var menu = new ContextMenuStrip();

            if (tag.Group != null)
            {
                menu.Items.Add("새 그룹", null, (s, e) => NewGroup(tag.Group, node));
                if (!tag.IsRoot)
                {
                    menu.Items.Add("그룹 이름 변경", null, (s, e) => node.BeginEdit());
                    menu.Items.Add("그룹 삭제", null, (s, e) => DeleteGroup(tag.Group));
                }
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("모두 펼치기", null, (s, e) => node.ExpandAll());
                menu.Items.Add("모두 접기", null, (s, e) => node.Collapse());
            }
            else if (tag.Item != null)
            {
                menu.Items.Add("속성 편집", null, (s, e) => EditProperties(tag.Item));
                menu.Items.Add("도면에서 선택", null, (s, e) => SelectInDrawing(tag.Item));
                menu.Items.Add("확대", null, (s, e) => ZoomTo(tag.Item));
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("PolyView 삭제", null, (s, e) => DeleteItem(tag.Item));
            }
            else if (tag.Handle != null)
            {
                menu.Items.Add("이 항목 삭제 (누락)", null, (s, e) => DeleteMissing(tag.Handle));
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("새로고침", null, (s, e) => RefreshTree());
            menu.Items.Add("누락 정리", null, (s, e) => PurgeMissing());

            menu.Show(treeView, location);
        }

        #endregion

        #region 그룹 조작

        private void NewGroupUnderSelection()
        {
            TreeNode? node = treeView.SelectedNode;
            if (node?.Tag is NodeTag tag && tag.Group != null) NewGroup(tag.Group, node);
            else if (treeView.Nodes.Count > 0 && treeView.Nodes[0].Tag is NodeTag rootTag && rootTag.Group != null)
                NewGroup(rootTag.Group, treeView.Nodes[0]);
        }

        private void NewGroup(PolyViewGroup parent, TreeNode parentNode)
        {
            string name = "새 그룹";
            int n = 2;
            while (PolyViewStore.SiblingGroupNameExists(parent, name, null))
                name = $"새 그룹 ({n++})";

            parent.Groups.Add(new PolyViewGroup { Name = name });
            if (!SaveTree()) return;

            RefreshTree();

            // 새로 만든 그룹을 찾아 바로 이름 편집 상태로 둔다
            TreeNode? created = FindGroupNode(treeView.Nodes, name);
            if (created != null)
            {
                treeView.SelectedNode = created;
                created.BeginEdit();
            }
        }

        private TreeNode? FindGroupNode(TreeNodeCollection nodes, string name)
        {
            foreach (TreeNode n in nodes)
            {
                if (n.Tag is NodeTag t && t.Group != null && !t.IsRoot &&
                    string.Equals(t.Group.Name, name, StringComparison.Ordinal))
                    return n;
                var found = FindGroupNode(n.Nodes, name);
                if (found != null) return found;
            }
            return null;
        }

        private void DeleteGroup(PolyViewGroup group)
        {
            PolyViewGroup? parent = PolyViewStore.FindParent(_root, group);
            if (parent == null) return;   // Root 는 삭제 불가

            bool hasChildren = group.Groups.Count > 0 || group.Handles.Count > 0;
            if (hasChildren)
            {
                var r = MessageBox.Show(
                    $"'{group.Name}' 그룹을 삭제합니다.\n" +
                    "하위 PolyView 와 하위 그룹은 최상위로 이동합니다. 계속할까요?",
                    "그룹 삭제", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (r != DialogResult.OK) return;
            }

            parent.Groups.Remove(group);
            _root.Handles.AddRange(group.Handles);
            _root.Groups.AddRange(group.Groups);

            if (SaveTree()) RefreshTree();
        }

        #endregion

        #region 항목 조작

        private void EditProperties(PolyViewItem item)
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            using (var form = new PolyViewEditForm(item.Name, item.Memo))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                string name = form.PolyViewName;
                string memo = form.Memo;

                if (PolyViewStore.NameExists(_items, name, item.Handle))
                {
                    MessageBox.Show($"'{name}' 은(는) 이미 사용 중인 이름입니다.", "PolyView",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!WriteXData(doc, item.Id, name, memo)) return;
            }
            RefreshTree();
        }

        private bool WriteXData(Document doc, ObjectId id, string name, string memo)
        {
            try
            {
                using (doc.LockDocument())
                {
                    if (!PolyViewStore.WriteXData(doc.Database, id, name, memo))
                    {
                        MessageBox.Show("XData 기록에 실패했습니다. 라이선스 상태를 확인하세요.",
                            "PolyView", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("기록에 실패했습니다: " + ex.Message, "PolyView",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        /// PolyView 등록 해제: Poly 의 XData 를 지우고 트리에서 뺀다. Poly 자체는 도면에 남는다.
        /// XData 를 먼저 지워야 한다 — 남아 있으면 다음 새로고침에서 미편입으로 Root 에 다시 붙는다.
        private void DeleteItem(PolyViewItem item)
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var r = MessageBox.Show(
                $"'{item.Name}' PolyView 를 삭제합니다.\n" +
                "도면의 Poly 는 남고 PolyView 등록(이름/내용)만 해제됩니다. 계속할까요?",
                "PolyView 삭제", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (r != DialogResult.OK) return;

            try
            {
                using (doc.LockDocument())
                {
                    PolyViewStore.RemoveXData(doc.Database, item.Id);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("삭제에 실패했습니다: " + ex.Message, "PolyView",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PolyViewStore.RemoveHandle(_root, item.Handle);
            if (SaveTree()) RefreshTree();
        }

        private void DeleteMissing(string handle)
        {
            PolyViewStore.RemoveHandle(_root, handle);
            if (SaveTree()) RefreshTree();
        }

        private void PurgeMissing()
        {
            int n = CountMissing(_root);
            if (n == 0)
            {
                MessageBox.Show("정리할 누락 항목이 없습니다.", "PolyView",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var r = MessageBox.Show($"누락 항목 {n}개를 트리에서 제거합니다. 계속할까요?",
                "누락 정리", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (r != DialogResult.OK) return;

            PolyViewStore.PurgeMissing(_root, _items);
            if (SaveTree()) RefreshTree();
        }

        private int CountMissing(PolyViewGroup g)
        {
            int n = 0;
            foreach (var h in g.Handles) if (!_items.ContainsKey(h)) n++;
            foreach (var child in g.Groups) n += CountMissing(child);
            return n;
        }

        #endregion

        #region 이름 편집

        private void TreeView_BeforeLabelEdit(object? sender, NodeLabelEditEventArgs e)
        {
            if (e.Node?.Tag is not NodeTag tag) { e.CancelEdit = true; return; }

            // Root 와 누락 항목은 이름을 바꿀 수 없다
            if (tag.IsRoot || tag.IsMissing) { e.CancelEdit = true; return; }

            // PolyView 는 이름/내용을 함께 고쳐야 하므로 인라인 대신 속성 대화상자로 보낸다
            if (tag.Item != null)
            {
                e.CancelEdit = true;
                PolyViewItem item = tag.Item;
                BeginInvoke(() => EditProperties(item));
                return;
            }

            // 그룹은 표시 텍스트에 개수가 붙어 있으므로 편집은 원래 이름으로 시작
            if (tag.Group != null) e.Node.Text = tag.Group.Name;
        }

        private void TreeView_AfterLabelEdit(object? sender, NodeLabelEditEventArgs e)
        {
            // 표시 텍스트는 RefreshTree 가 다시 만든다. 여기서는 항상 취소하고 모델만 고친다.
            e.CancelEdit = true;

            if (e.Node?.Tag is not NodeTag tag) { BeginInvoke(RefreshTree); return; }
            if (e.Label == null) { BeginInvoke(RefreshTree); return; }   // 사용자가 취소

            string label = e.Label.Trim();
            if (label.Length == 0) { BeginInvoke(RefreshTree); return; }

            if (tag.Group != null)
            {
                PolyViewGroup? parent = PolyViewStore.FindParent(_root, tag.Group);
                if (parent != null && PolyViewStore.SiblingGroupNameExists(parent, label, tag.Group))
                {
                    MessageBox.Show($"같은 위치에 '{label}' 그룹이 이미 있습니다.", "PolyView",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    BeginInvoke(RefreshTree);
                    return;
                }
                tag.Group.Name = label;
                SaveTree();
                BeginInvoke(RefreshTree);
            }
            // PolyView 는 BeforeLabelEdit 에서 대화상자로 보내므로 여기 오지 않는다.
        }

        #endregion

        #region 드래그앤드롭

        private void TreeView_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node && node.Tag is NodeTag tag && !tag.IsRoot)
                DoDragDrop(node, DragDropEffects.Move);
        }

        private void TreeView_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void TreeView_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = CanDrop(e, out _, out _) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void TreeView_DragDrop(object? sender, DragEventArgs e)
        {
            if (!CanDrop(e, out NodeTag? src, out PolyViewGroup? target)) return;
            if (src == null || target == null) return;

            if (src.Group != null)
            {
                if (PolyViewStore.SiblingGroupNameExists(target, src.Group.Name, src.Group))
                {
                    MessageBox.Show($"대상 위치에 '{src.Group.Name}' 그룹이 이미 있습니다.", "PolyView",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                PolyViewGroup? parent = PolyViewStore.FindParent(_root, src.Group);
                if (parent == null) return;
                parent.Groups.Remove(src.Group);
                target.Groups.Add(src.Group);
            }
            else if (src.Handle != null)
            {
                PolyViewStore.RemoveHandle(_root, src.Handle);
                target.Handles.Add(src.Handle);
            }
            else return;

            if (SaveTree()) RefreshTree();
        }

        /// 드롭 가능 여부 판정. 대상은 그룹 또는 Root 여야 하고, 그룹을 자기 자손으로는 못 옮긴다.
        private bool CanDrop(DragEventArgs e, out NodeTag? src, out PolyViewGroup? target)
        {
            src = null;
            target = null;

            if (e.Data?.GetData(typeof(TreeNode)) is not TreeNode srcNode) return false;
            if (srcNode.Tag is not NodeTag srcTag || srcTag.IsRoot) return false;

            System.Drawing.Point pt = treeView.PointToClient(new System.Drawing.Point(e.X, e.Y));
            TreeNode? targetNode = treeView.GetNodeAt(pt);
            if (targetNode?.Tag is not NodeTag targetTag) return false;
            if (targetTag.Group == null) return false;                 // 항목 위에는 드롭 불가
            if (ReferenceEquals(srcNode, targetNode)) return false;

            if (srcTag.Group != null && PolyViewStore.IsSelfOrDescendant(srcTag.Group, targetTag.Group))
                return false;                                           // 자기 자신/자손으로 이동 금지

            src = srcTag;
            target = targetTag.Group;
            return true;
        }

        #endregion

        private bool SaveTree()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return false;
            try
            {
                using (doc.LockDocument())
                {
                    PolyViewStore.SaveTree(doc.Database, _root);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("트리 저장에 실패했습니다: " + ex.Message, "PolyView",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}
