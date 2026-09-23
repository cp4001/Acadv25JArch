using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    /// <summary>
    /// Insert_Diffuser — 룸 풍량 / 디퓨저 Type / 개수를 입력받아
    /// 한 대당 풍량(룸 풍량 ÷ 개수) 이상인 최소 표준풍량 행을 디퓨저 선정표에서 고르고,
    /// 선택 지점부터 수평(+X)으로 블럭을 개수만큼 배치한다 (블럭 간 순간격 = ND × 2).
    /// 각 블럭에 XData "Diffuser"(=Type) / "SystemType"(SA/RA/EA/OA) / "Type" / "Size" / "ND"
    /// + "CMH" / "Disp"(한 대당 풍량, CMH 명령과 동일 패턴) 를 문자열로 기록.
    /// 블럭 이름은 "JArch_" + Type + "_ST" (JArch_RPD_ST 등)이고, 정의는 매 실행마다
    /// 참조 도면(Blocks\JArch_Blocks.dwg)에서 가져와 덮어쓴다. XData 값은 Type("RPD") 그대로 사용.
    /// 블럭 속성(Attribute) "SystemType" 에도 선택한 계통을 기록한다.
    /// </summary>
    public class DiffuserInsertCommand
    {
        private record DiffuserSpec(string Type, string Size, int ND, int MinCmh, int StdCmh, int MaxCmh);

        // 디퓨저 선정표 (TYPE / SIZE / ND / 최소풍량 / 표준풍량 / 최대풍량, CMH)
        // 2026-09-23 갱신: RPD 650A/375 삭제, RAD 650A/350 삭제,
        //                  SAD 250x250/125 값 변경(150/225/450 → 140/200/380). 36행 → 32행.
        private static readonly DiffuserSpec[] Table =
        {
            new("RPD", "270A", 125,  140,  200,  380),
            new("RPD", "320A", 150,  205,  300,  550),
            new("RPD", "420A", 200,  360,  550,  970),
            new("RPD", "480A", 250,  555,  850, 1480),
            new("RPD", "550A", 300,  850, 1300, 2160),
            new("RPD", "600A", 350, 1100, 1800, 2930),
            new("RPD", "650A", 400, 1410, 2200, 3765),

            new("SPD", "300x300", 125,  140,  200,  380),
            new("SPD", "300x300", 150,  205,  300,  550),
            new("SPD", "300x300", 200,  360,  550,  970),
            new("SPD", "410x410", 250,  555,  850, 1480),
            new("SPD", "460x460", 300,  850, 1300, 2160),
            new("SPD", "620x620", 350, 1100, 1800, 2930),
            new("SPD", "620x620", 375, 1250, 2000, 3300),

            new("RAD", "175A", 100,  100,  150,  300),
            new("RAD", "230A", 100,  150,  225,  350),
            new("RAD", "270A", 125,  180,  270,  420),
            new("RAD", "320A", 150,  200,  300,  600),
            new("RAD", "420A", 200,  400,  600, 1200),
            new("RAD", "480A", 250,  600,  900, 1800),
            new("RAD", "550A", 300,  800, 1200, 2400),
            new("RAD", "650A", 400, 1200, 1800, 3600),
            new("RAD", "820A", 450, 1350, 2000, 4000),
            new("RAD", "820A", 500, 1500, 2500, 5000),

            new("SAD", "250x250", 100,  100,  150,  300),
            new("SAD", "250x250", 125,  140,  200,  380),
            new("SAD", "300x300", 150,  200,  300,  600),
            new("SAD", "300x300", 200,  400,  600, 1200),
            new("SAD", "410x410", 250,  600,  900, 1800),
            new("SAD", "460x460", 300,  800, 1200, 2400),
            new("SAD", "620x620", 350, 1100, 1800, 2930),
            new("SAD", "620x620", 375, 1250, 2000, 3300),
        };

        private static readonly string[] Types = { "RPD", "SPD", "RAD", "SAD" };

        // 급기(Supply) / 환기(Return) / 배기(Exhaust) / 외기(Outdoor Air)
        private static readonly string[] SystemTypes = { "SA", "RA", "EA", "OA" };

        private const string BlockPrefix = "JArch_";
        private const string BlockSuffix = "_ST";      // SystemType 속성을 가진 블럭
        private const string SystemTypeTag = "SystemType";
        private const double RowGap = 800.0;          // Bypoly: Spec Text 항목 간 세로 간격
        private const double RowStartLeft = 600.0;    // Bypoly: Poly 센터에서 왼쪽(-X)으로 이 거리부터 배치 시작

        /// <summary>Spec Text "2400,RPD,RA,3" 을 파싱한 결과.</summary>
        private record SpecText(double Cfm, string Type, string SystemType, int Count);

        // Diffuser_Spec 지정 성공 시 Text 에 적용하는 표시 스타일
        private const short SpecColorIndex = 41;      // ACI
        private const double SpecObliqueDeg = 5.0;    // 기울기(도)

        [CommandMethod("Insert_Diffuser")]
        public void Cmd_InsertDiffuser()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1. 룸 풍량
            var pdo = new PromptDoubleOptions("\n룸 필요 풍량(CMH)을 입력하세요: ");
            pdo.AllowNegative = false;
            pdo.AllowZero = false;
            pdo.AllowNone = false;
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK) return;
            double roomCmh = pdr.Value;

            // 2. Type
            var pko = new PromptKeywordOptions("\n디퓨저 Type 을 선택하세요");
            foreach (var t in Types) pko.Keywords.Add(t);
            pko.Keywords.Default = Types[0];
            pko.AllowNone = true;
            PromptResult pkr = ed.GetKeywords(pko);
            if (pkr.Status != PromptStatus.OK) return;
            string type = pkr.StringResult;

            // 3. SystemType (SA/RA/EA/OA)
            var psko = new PromptKeywordOptions("\n계통(SystemType)을 선택하세요");
            foreach (var t in SystemTypes) psko.Keywords.Add(t);
            psko.Keywords.Default = SystemTypes[0];
            psko.AllowNone = true;
            PromptResult pskr = ed.GetKeywords(psko);
            if (pskr.Status != PromptStatus.OK) return;
            string systemType = pskr.StringResult;

            // 4. 개수
            var pio = new PromptIntegerOptions("\n디퓨저 개수를 입력하세요: ");
            pio.LowerLimit = 1;
            pio.DefaultValue = 1;
            pio.UseDefaultValue = true;
            PromptIntegerResult pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK) return;
            int count = pir.Value;

            // 5. 선정 — 한 대당 풍량 이상인 최소 표준풍량 행
            double perUnit = roomCmh / count;
            DiffuserSpec spec = SelectSpec(type, perUnit, out bool exceeded);
            if (exceeded)
                ed.WriteMessage($"\n[경고] 한 대당 {perUnit:0.#} CMH 는 {type} 최대 표준풍량({spec.StdCmh})을 초과합니다. 최대 사이즈로 배치합니다.");
            ed.WriteMessage($"\n선정: {systemType} {spec.Type} {spec.Size} ND{spec.ND} (표준 {spec.StdCmh} CMH, 한 대당 {perUnit:0.#} CMH × {count}개)");

            // 6. 배치 기준점
            PromptPointResult ppr = ed.GetPoint("\n배치 시작점을 지정하세요: ");
            if (ppr.Status != PromptStatus.OK) return;
            Point3d basePt = ppr.Value;

            // 7. 참조 도면에서 블럭 정의를 가져온다(기존 정의는 덮어씀). Transaction 밖에서 수행.
            string blockName = BlockPrefix + type + BlockSuffix;
            if (!JArchBlockLibrary.Import(db, ed, blockName)) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId btrId = bt[blockName];
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                tr.ChecRegNames(db, "Diffuser,SystemType,Type,Size,ND,CMH,Disp");

                PlaceRow(tr, space, btrId, basePt, spec, systemType, count,
                         perUnit.ToString("0.##"), blockName, ed);

                tr.Commit();
            }

            ed.WriteMessage($"\n{blockName} 블럭 {count}개 배치 완료 ({systemType}).");
        }

        /// <summary>
        /// 블럭 정의의 AttributeDefinition 들로 AttributeReference 를 만들어 br 에 붙이고,
        /// Tag 가 "SystemType" 인 속성에는 선택한 계통을 넣는다. 기록한 SystemType 속성 개수를 반환.
        /// 프로그램으로 BlockReference 를 만들면 속성이 자동 생성되지 않으므로 직접 붙여야 한다.
        /// </summary>
        private static int AppendAttributes(Transaction tr, BlockReference br, ObjectId btrId, string systemType)
        {
            int stCount = 0;
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            foreach (ObjectId id in btr)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition ad) continue;
                if (ad.Constant) continue;   // 상수 속성은 정의에 포함되어 참조를 만들지 않는다

                var ar = new AttributeReference();
                ar.SetAttributeFromBlock(ad, br.BlockTransform);
                if (ad.Tag.Equals(SystemTypeTag, StringComparison.OrdinalIgnoreCase))
                {
                    ar.TextString = systemType;
                    stCount++;
                }
                br.AttributeCollection.AppendAttribute(ar);
                tr.AddNewlyCreatedDBObject(ar, true);
            }
            return stCount;
        }

        /// <summary>
        /// Diffuser_Spec — "2400,RPD,RA,3" 형식의 Text 를 선택하면 형식을 검증한 뒤
        /// XData RegApp "Diffuser" 에 Type(RPD/SPD/RAD/SAD)을 기록한다.
        /// 필드 순서: CFM(숫자) , Type , SystemType(SA/RA/EA/OA) , 개수(양의 정수).
        /// 지정 성공한 Text 는 색상 41 / 기울기 5° 로 바꿔 지정 여부를 눈으로 구분한다.
        /// 형식이 맞지 않는 Text 는 이유를 출력하고 건너뛴다(나머지는 계속 처리).
        /// </summary>
        [CommandMethod("Diffuser_Spec", CommandFlags.UsePickSet)]
        public void Cmd_DiffuserSpec()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<DBText> targets = JEntityFunc.GetEntityByTpye<DBText>(
                    "디퓨저 Spec Text 를 선택하세요 (예: 2400,RPD,RA,3)",
                    JSelFilter.MakeFilterTypes("TEXT"));
                if (targets == null || targets.Count == 0) return;

                tr.ChecRegNames(db, "Diffuser");

                int okCount = 0;
                int errCount = 0;
                foreach (DBText txt in targets)
                {
                    if (!TryParseSpec(txt.TextString, out SpecText? sp, out string reason))
                    {
                        ed.WriteMessage($"\n[건너뜀] \"{txt.TextString}\" — {reason}");
                        errCount++;
                        continue;
                    }

                    var layer = (LayerTableRecord)tr.GetObject(txt.LayerId, OpenMode.ForRead);
                    if (layer.IsLocked)
                    {
                        ed.WriteMessage($"\n[건너뜀] \"{txt.TextString}\" — 레이어 '{layer.Name}' 가 잠겨 있습니다.");
                        errCount++;
                        continue;
                    }

                    txt.UpgradeOpen();
                    JXdata.SetXdata(txt, "Diffuser", sp!.Type);
                    txt.ColorIndex = SpecColorIndex;
                    txt.Oblique = SpecObliqueDeg * Math.PI / 180.0;
                    okCount++;
                }

                tr.Commit();

                ed.WriteMessage($"\n{okCount}건 기록 완료" + (errCount > 0 ? $", {errCount}건 건너뜀." : "."));
            }
        }

        /// <summary>
        /// "2400,RPD,RA,3" 을 검증해 CFM / Type / SystemType / 개수를 돌려준다.
        /// Type / SystemType 은 대소문자를 구분하지 않고 표준 표기(대문자)로 정규화한다.
        /// </summary>
        private static bool TryParseSpec(string text, out SpecText? spec, out string reason)
        {
            spec = null;
            reason = "";

            if (string.IsNullOrWhiteSpace(text))
            {
                reason = "빈 Text";
                return false;
            }

            string[] f = text.Split(',');
            if (f.Length != 4)
            {
                reason = $"필드가 {f.Length}개 (CFM,Type,SystemType,개수 = 4개 필요)";
                return false;
            }

            string cfmStr = f[0].Trim();
            string typeStr = f[1].Trim();
            string sysStr = f[2].Trim();
            string cntStr = f[3].Trim();

            if (!double.TryParse(cfmStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double cfm) || cfm <= 0)
            {
                reason = $"CFM '{cfmStr}' 은 양수가 아닙니다";
                return false;
            }

            string? matchedType = Types.FirstOrDefault(t => t.Equals(typeStr, StringComparison.OrdinalIgnoreCase));
            if (matchedType == null)
            {
                reason = $"Type '{typeStr}' 는 [{string.Join("/", Types)}] 중 하나여야 합니다";
                return false;
            }

            if (!SystemTypes.Any(t => t.Equals(sysStr, StringComparison.OrdinalIgnoreCase)))
            {
                reason = $"SystemType '{sysStr}' 는 [{string.Join("/", SystemTypes)}] 중 하나여야 합니다";
                return false;
            }

            if (!int.TryParse(cntStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cnt) || cnt < 1)
            {
                reason = $"개수 '{cntStr}' 는 1 이상의 정수여야 합니다";
                return false;
            }

            string? matchedSys = SystemTypes.First(t => t.Equals(sysStr, StringComparison.OrdinalIgnoreCase));
            spec = new SpecText(cfm, matchedType, matchedSys, cnt);
            return true;
        }

        /// <summary>한 대당 풍량 이상인 최소 표준풍량 행. 최대치를 넘으면 최대 행 + exceeded=true.</summary>
        private static DiffuserSpec SelectSpec(string type, double perUnit, out bool exceeded)
        {
            var candidates = Table.Where(s => s.Type == type).OrderBy(s => s.StdCmh).ToList();
            DiffuserSpec? hit = candidates.FirstOrDefault(s => s.StdCmh >= perUnit);
            exceeded = hit == null;
            return hit ?? candidates.Last();
        }

        /// <summary>
        /// basePt 부터 +X 방향으로 count 개를 배치한다(순간격 = ND × 2).
        /// 블럭 속성 + XData 7건 기록까지 한 곳에서 처리 — Insert_Diffuser / Insert_Diffuser_Bypoly 공용.
        /// </summary>
        private static void PlaceRow(Transaction tr, BlockTableRecord space, ObjectId btrId,
                                     Point3d basePt, DiffuserSpec spec, string systemType,
                                     int count, string cmhStr, string blockName, Editor ed)
        {
            double spacing = spec.ND * 2.0;   // 블럭 간 순간격(외곽선 사이 거리)
            double pitch = 0;                 // 첫 블럭 폭 + spacing (첫 블럭 삽입 후 결정)
            for (int i = 0; i < count; i++)
            {
                var br = new BlockReference(basePt + Vector3d.XAxis * (pitch * i), btrId);
                space.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                if (i == 0)
                {
                    // 속성을 붙이기 전에 계산 — 속성 텍스트가 폭에 끼지 않도록
                    Extents3d ext = br.GeometricExtents;
                    pitch = (ext.MaxPoint.X - ext.MinPoint.X) + spacing;
                }

                // 블럭 속성 생성 + SystemType 기록
                int stSet = AppendAttributes(tr, br, btrId, systemType);
                if (i == 0 && stSet == 0)
                    ed.WriteMessage($"\n[경고] \'{blockName}\' 에 \'{SystemTypeTag}\' 속성이 없어 계통을 기록하지 못했습니다.");

                JXdata.SetXdata(br, "Diffuser", spec.Type);
                JXdata.SetXdata(br, "SystemType", systemType);
                JXdata.SetXdata(br, "Type", spec.Type);
                JXdata.SetXdata(br, "Size", spec.Size);
                JXdata.SetXdata(br, "ND", spec.ND.ToString());
                JXdata.SetXdata(br, "CMH", cmhStr);
                JXdata.SetXdata(br, "Disp", cmhStr);
            }
        }

        /// <summary>Poly 의 GeometricExtents 중심.</summary>
        private static Point3d PolyCenter(Polyline pl)
        {
            Extents3d ext = pl.GeometricExtents;
            return new Point3d((ext.MinPoint.X + ext.MaxPoint.X) * 0.5,
                               (ext.MinPoint.Y + ext.MaxPoint.Y) * 0.5,
                               (ext.MinPoint.Z + ext.MaxPoint.Z) * 0.5);
        }

        /// <summary>
        /// 점이 Poly 내부인지 판정 (XY 평면 ray casting).
        /// ⚠️ bulge(원호) 구간은 정점 사이 직선(현)으로 근사한다.
        /// jCadExtention 의 Polyline.Contains 확장은 bbox 검사일 뿐이라 L 자형 룸에서 오판하므로 쓰지 않는다.
        /// </summary>
        private static bool IsInsidePoly(Polyline pl, Point3d p)
        {
            int n = pl.NumberOfVertices;
            if (n < 3) return false;

            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Point2d a = pl.GetPoint2dAt(i);
                Point2d b = pl.GetPoint2dAt(j);
                if ((a.Y > p.Y) != (b.Y > p.Y) &&
                    p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>
        /// Insert_Diffuser_Bypoly — XData "Room" 을 가진 Poly 를 선택하면 그 안의
        /// Spec Text(XData "Diffuser", Diffuser_Spec 으로 지정한 것)를 찾아
        /// "CFM,Type,SystemType,개수" 를 읽고 Poly 센터부터 배치한다.
        /// Text 가 여러 개면 항목마다 800 아래로 내려가며 한 줄씩 배치한다.
        /// 각 줄의 시작점은 Poly 센터에서 왼쪽으로 600 떨어진 지점.
        /// Poly 는 여러 개를 한 번에 선택할 수 있다(GetSelection → Poly 별로 반복).
        /// </summary>
        [CommandMethod("Insert_Diffuser_Bypoly", CommandFlags.UsePickSet)]
        public void Cmd_InsertDiffuserByPoly()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1. Diffuser XData 를 가진 TEXT 를 도면 전체에서 수집 (Editor 호출 → Transaction 밖)
            PromptSelectionResult tsr = ed.SelectAll(JSelFilter.MakeFilterTypesRegs("TEXT", "Diffuser"));
            if (tsr.Status != PromptStatus.OK || tsr.Value.Count == 0)
            {
                ed.WriteMessage("\n[오류] XData \"Diffuser\" 를 가진 Text 가 도면에 없습니다. Diffuser_Spec 으로 먼저 지정하세요.");
                return;
            }
            ObjectId[] textIds = tsr.Value.GetObjectIds();

            // 2. 계획 수집 — Poly 별로 내부 Spec Text 를 찾아 (배치 원점, Spec) 목록을 만든다
            var plans = new List<(Point3d Origin, SpecText Spec)>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Polyline> polys = JEntityFunc.GetEntityByTpye<Polyline>(
                    "Room Poly 를 선택하세요",
                    JSelFilter.MakeFilterTypesRegs("LWPOLYLINE", "Room"));
                if (polys == null || polys.Count == 0) return;

                foreach (Polyline pl in polys)
                {
                    Point3d center = PolyCenter(pl);
                    var found = new List<(Point3d Pos, SpecText Spec)>();

                    foreach (ObjectId id in textIds)
                    {
                        if (tr.GetObject(id, OpenMode.ForRead) is not DBText txt) continue;
                        if (!IsInsidePoly(pl, txt.Position)) continue;

                        if (!TryParseSpec(txt.TextString, out SpecText? sp, out string reason))
                        {
                            ed.WriteMessage($"\n[건너뜀] \"{txt.TextString}\" — {reason}");
                            continue;
                        }
                        found.Add((txt.Position, sp!));
                    }

                    if (found.Count == 0)
                    {
                        ed.WriteMessage($"\n[알림] Poly(핸들 {pl.Handle}) 안에 유효한 Spec Text 가 없습니다.");
                        continue;
                    }

                    // 위 → 아래, 같은 높이면 왼쪽 → 오른쪽 순서로 줄을 배정
                    var ordered = found.OrderByDescending(t => t.Pos.Y).ThenBy(t => t.Pos.X).ToList();
                    for (int k = 0; k < ordered.Count; k++)
                    {
                        // 센터에서 왼쪽으로 RowStartLeft, 항목마다 아래로 RowGap
                        Point3d origin = center
                                       - Vector3d.XAxis * RowStartLeft
                                       - Vector3d.YAxis * (RowGap * k);
                        plans.Add((origin, ordered[k].Spec));
                    }
                }

                tr.Commit();
            }

            if (plans.Count == 0) return;

            // 3. 필요한 블럭 정의를 참조 도면에서 가져온다 (Transaction 밖, Type 별 1회)
            foreach (string name in plans.Select(p => BlockPrefix + p.Spec.Type + BlockSuffix).Distinct())
                if (!JArchBlockLibrary.Import(db, ed, name)) return;

            // 4. 배치
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                tr.ChecRegNames(db, "Diffuser,SystemType,Type,Size,ND,CMH,Disp");

                foreach (var (origin, sp) in plans)
                {
                    double perUnit = sp.Cfm / sp.Count;
                    DiffuserSpec spec = SelectSpec(sp.Type, perUnit, out bool exceeded);
                    string blockName = BlockPrefix + sp.Type + BlockSuffix;

                    if (exceeded)
                        ed.WriteMessage($"\n[경고] 한 대당 {perUnit:0.#} CMH 는 {sp.Type} 최대 표준풍량({spec.StdCmh})을 초과합니다. 최대 사이즈로 배치합니다.");
                    ed.WriteMessage($"\n선정: {sp.SystemType} {spec.Type} {spec.Size} ND{spec.ND} (표준 {spec.StdCmh} CMH, 한 대당 {perUnit:0.#} CMH × {sp.Count}개)");

                    PlaceRow(tr, space, bt[blockName], origin, spec, sp.SystemType, sp.Count,
                             perUnit.ToString("0.##"), blockName, ed);
                }

                tr.Commit();
            }

            ed.WriteMessage($"\n{plans.Count}개 항목 배치 완료.");
        }
    }
}
