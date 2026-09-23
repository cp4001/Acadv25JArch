using AcadFunction;
using Autodesk.AutoCAD.DatabaseServices;
using CADExtension;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Acadv25JArch.PolyView
{
    /// <summary>
    /// PolyView 데이터 접근 계층.
    ///  - 이름/내용의 정본 : Poly 엔티티 XData ("PolyView" / "PolyViewMemo")
    ///  - 그룹 트리 구조   : 도면 NOD 사전 "JARCH_POLYVIEW" 의 Xrecord "TreeJson"
    /// 자세한 사양은 PolyViewManager.md 참조.
    /// </summary>
    public static class PolyViewStore
    {
        public const string RegAppName = "PolyView";
        public const string RegAppMemo = "PolyViewMemo";

        public const string DictName = "JARCH_POLYVIEW";
        public const string KeyTreeJson = "TreeJson";

        public const int MaxNameLen = 60;
        public const int MaxMemoLen = 100;

        /// Xrecord 문자열 1개당 담을 문자 수. 한글 3바이트 기준으로도 여유가 있는 값.
        private const int ChunkLen = 200;

        private static readonly JsonSerializerOptions JsonOpt = new()
        {
            // 한글이 \uXXXX 로 부풀지 않도록 (도면 내부 저장이라 HTML 이스케이프 불필요)
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        #region 도면 스캔 (XData)

        /// <summary>
        /// 모델 공간에서 PolyView XData 를 가진 닫힌 Polyline 을 모두 수집한다.
        /// 키는 Handle 문자열.
        /// </summary>
        public static Dictionary<string, PolyViewItem> ScanModelSpace(Database db)
        {
            var map = new Dictionary<string, PolyViewItem>(StringComparer.OrdinalIgnoreCase);
            if (db == null) return map;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (!id.IsValid || id.IsErased) continue;

                    Polyline? pl = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                    if (pl == null) continue;

                    string? name = JXdata.GetXdata(pl, RegAppName);
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    string h = pl.Handle.ToString();
                    map[h] = new PolyViewItem
                    {
                        Handle = h,
                        Name = name,
                        Memo = Unescape(JXdata.GetXdata(pl, RegAppMemo) ?? ""),
                        Id = id
                    };
                }
                tr.Commit();
            }
            return map;
        }

        /// <summary>
        /// Poly 에 PolyView 이름/내용을 기록한다. RegApp 선등록 포함.
        /// 기록 후 되읽어 검증하며, 값이 비면 false (라이선스 만료 시 arx 가 조용히 생략).
        /// </summary>
        public static bool WriteXData(Database db, ObjectId id, string name, string memo)
        {
            if (db == null || id.IsNull) return false;

            bool ok;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                tr.ChecRegNames(db, RegAppName + "," + RegAppMemo);

                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                JXdata.SetXdata(ent, RegAppName, name);
                JXdata.SetXdata(ent, RegAppMemo, Escape(memo ?? ""));

                // 되읽기 검증 — arx 가 만료로 기록을 생략했는지 확인
                ok = !string.IsNullOrEmpty(JXdata.GetXdata(ent, RegAppName));

                tr.Commit();
            }
            return ok;
        }

        /// <summary>도면 전체에서 같은 이름이 이미 쓰이는지 검사 (대소문자 무시).</summary>
        public static bool NameExists(Dictionary<string, PolyViewItem> items, string name, string? exceptHandle)
        {
            foreach (var kv in items)
            {
                if (exceptHandle != null && string.Equals(kv.Key, exceptHandle, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(kv.Value.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // XData 문자열에 줄바꿈을 그대로 넣지 않고 \n 두 글자로 이스케이프한다.
        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\r\n", "\\n").Replace("\n", "\\n");

        private static string Unescape(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    if (s[i + 1] == 'n') { sb.Append('\n'); i++; continue; }
                    if (s[i + 1] == '\\') { sb.Append('\\'); i++; continue; }
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        #endregion

        #region 그룹 트리 (NOD)

        private class GroupDto
        {
            public string? n { get; set; }
            public List<GroupDto>? g { get; set; }
            public List<string>? i { get; set; }
        }

        private class TreeDto
        {
            public int v { get; set; } = 1;
            public GroupDto? root { get; set; }
        }

        /// <summary>NOD 에서 그룹 트리를 읽는다. 사전이 없으면 빈 Root 를 돌려준다.</summary>
        public static PolyViewGroup LoadTree(Database db)
        {
            string json = ReadTreeJson(db);
            if (string.IsNullOrWhiteSpace(json)) return new PolyViewGroup();

            try
            {
                TreeDto? dto = JsonSerializer.Deserialize<TreeDto>(json, JsonOpt);
                if (dto?.root == null) return new PolyViewGroup();
                return ToGroup(dto.root);
            }
            catch
            {
                // 손상된 JSON 은 구조를 버리고 전부 Root 직속으로 되돌린다(데이터는 XData 에 남아 있음)
                return new PolyViewGroup();
            }
        }

        /// <summary>그룹 트리를 NOD 에 저장한다. 호출 측이 문서 잠금을 보유해야 한다.</summary>
        public static void SaveTree(Database db, PolyViewGroup root)
        {
            if (db == null || root == null) return;

            var dto = new TreeDto { v = 1, root = ToDto(root) };
            string json = JsonSerializer.Serialize(dto, JsonOpt);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);

                DBDictionary dict;
                if (nod.Contains(DictName))
                {
                    dict = (DBDictionary)tr.GetObject(nod.GetAt(DictName), OpenMode.ForWrite);
                }
                else
                {
                    dict = new DBDictionary();
                    nod.SetAt(DictName, dict);
                    tr.AddNewlyCreatedDBObject(dict, true);
                }

                Xrecord xrec;
                if (dict.Contains(KeyTreeJson))
                {
                    xrec = (Xrecord)tr.GetObject(dict.GetAt(KeyTreeJson), OpenMode.ForWrite);
                }
                else
                {
                    xrec = new Xrecord();
                    dict.SetAt(KeyTreeJson, xrec);
                    tr.AddNewlyCreatedDBObject(xrec, true);
                }

                var tvs = new List<TypedValue>();
                foreach (string chunk in Split(json, ChunkLen))
                    tvs.Add(new TypedValue((int)DxfCode.Text, chunk));
                xrec.Data = new ResultBuffer(tvs.ToArray());

                tr.Commit();
            }
        }

        private static string ReadTreeJson(Database db)
        {
            if (db == null) return "";

            var sb = new StringBuilder();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (nod.Contains(DictName))
                {
                    DBDictionary dict = (DBDictionary)tr.GetObject(nod.GetAt(DictName), OpenMode.ForRead);
                    if (dict.Contains(KeyTreeJson))
                    {
                        Xrecord xrec = (Xrecord)tr.GetObject(dict.GetAt(KeyTreeJson), OpenMode.ForRead);
                        ResultBuffer rb = xrec.Data;
                        if (rb != null)
                        {
                            foreach (TypedValue tv in rb)
                            {
                                if (tv.TypeCode == (short)DxfCode.Text && tv.Value != null)
                                    sb.Append(tv.Value.ToString());
                            }
                        }
                    }
                }
                tr.Commit();
            }
            return sb.ToString();
        }

        /// 문자열을 길이 max 로 자른다. 서로게이트 쌍은 나누지 않는다.
        private static IEnumerable<string> Split(string s, int max)
        {
            int pos = 0;
            while (pos < s.Length)
            {
                int len = Math.Min(max, s.Length - pos);
                if (pos + len < s.Length && char.IsHighSurrogate(s[pos + len - 1])) len--;
                if (len <= 0) len = 1;
                yield return s.Substring(pos, len);
                pos += len;
            }
        }

        private static PolyViewGroup ToGroup(GroupDto dto)
        {
            var g = new PolyViewGroup { Name = dto.n ?? "" };
            if (dto.i != null) g.Handles.AddRange(dto.i);
            if (dto.g != null)
                foreach (var child in dto.g) g.Groups.Add(ToGroup(child));
            return g;
        }

        private static GroupDto ToDto(PolyViewGroup g)
        {
            var dto = new GroupDto
            {
                n = string.IsNullOrEmpty(g.Name) ? null : g.Name,
                i = g.Handles.Count > 0 ? new List<string>(g.Handles) : null,
                g = null
            };
            if (g.Groups.Count > 0)
            {
                dto.g = new List<GroupDto>();
                foreach (var child in g.Groups) dto.g.Add(ToDto(child));
            }
            return dto;
        }

        #endregion

        #region 트리 재구성

        /// <summary>
        /// 도면 스캔 결과와 NOD 트리를 맞춰 Root 를 만든다.
        ///  - 트리에만 있고 도면에 없는 항목 : 자리를 유지한 채 "누락" 으로 남긴다
        ///  - 도면에만 있고 트리에 없는 항목 : Root 직속 끝에 편입하고 NOD 에 반영
        /// 호출 측이 문서 잠금을 보유해야 한다(편입 시 저장이 일어남).
        /// </summary>
        public static PolyViewGroup BuildTree(Database db, out Dictionary<string, PolyViewItem> items)
        {
            items = ScanModelSpace(db);
            PolyViewGroup root = LoadTree(db);

            var inTree = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            RemoveDuplicates(root, inTree);

            var unfiled = new List<string>();
            foreach (var h in items.Keys)
                if (!inTree.Contains(h)) unfiled.Add(h);

            if (unfiled.Count > 0)
            {
                root.Handles.AddRange(unfiled);
                SaveTree(db, root);
            }
            return root;
        }

        /// 같은 Handle 이 트리 여러 곳에 있으면 처음 것만 남긴다(수집 집합도 함께 채운다).
        private static void RemoveDuplicates(PolyViewGroup g, HashSet<string> seen)
        {
            for (int i = g.Handles.Count - 1; i >= 0; i--)
            {
                if (!seen.Add(g.Handles[i])) g.Handles.RemoveAt(i);
            }
            foreach (var child in g.Groups) RemoveDuplicates(child, seen);
        }

        /// <summary>트리에서 도면에 없는 Handle 을 모두 제거하고 제거 개수를 돌려준다.</summary>
        public static int PurgeMissing(PolyViewGroup g, Dictionary<string, PolyViewItem> items)
        {
            int n = 0;
            for (int i = g.Handles.Count - 1; i >= 0; i--)
            {
                if (!items.ContainsKey(g.Handles[i])) { g.Handles.RemoveAt(i); n++; }
            }
            foreach (var child in g.Groups) n += PurgeMissing(child, items);
            return n;
        }

        /// <summary>트리에서 Handle 하나를 제거한다.</summary>
        public static bool RemoveHandle(PolyViewGroup g, string handle)
        {
            for (int i = 0; i < g.Handles.Count; i++)
            {
                if (string.Equals(g.Handles[i], handle, StringComparison.OrdinalIgnoreCase))
                {
                    g.Handles.RemoveAt(i);
                    return true;
                }
            }
            foreach (var child in g.Groups)
                if (RemoveHandle(child, handle)) return true;
            return false;
        }

        /// <summary>target 그룹의 부모를 찾는다. Root 자신이면 null.</summary>
        public static PolyViewGroup? FindParent(PolyViewGroup root, PolyViewGroup target)
        {
            foreach (var child in root.Groups)
            {
                if (ReferenceEquals(child, target)) return root;
                var found = FindParent(child, target);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>candidate 가 g 자신이거나 g 의 자손이면 true (그룹을 자기 안으로 넣는 것 차단).</summary>
        public static bool IsSelfOrDescendant(PolyViewGroup g, PolyViewGroup candidate)
        {
            if (ReferenceEquals(g, candidate)) return true;
            foreach (var child in g.Groups)
                if (IsSelfOrDescendant(child, candidate)) return true;
            return false;
        }

        /// <summary>형제 중 같은 이름의 그룹이 있는지 검사.</summary>
        public static bool SiblingGroupNameExists(PolyViewGroup parent, string name, PolyViewGroup? except)
        {
            foreach (var child in parent.Groups)
            {
                if (ReferenceEquals(child, except)) continue;
                if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>그룹에 속한 PolyView 총 개수(하위 그룹 포함).</summary>
        public static int CountItems(PolyViewGroup g)
        {
            int n = g.Handles.Count;
            foreach (var child in g.Groups) n += CountItems(child);
            return n;
        }

        #endregion
    }
}
