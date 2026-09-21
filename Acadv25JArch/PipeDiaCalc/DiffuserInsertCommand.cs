using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    /// <summary>
    /// Insert_Diffuser — 룸 풍량 / 디퓨저 Type / 개수를 입력받아
    /// 한 대당 풍량(룸 풍량 ÷ 개수) 이상인 최소 표준풍량 행을 디퓨저 선정표에서 고르고,
    /// 선택 지점부터 수평(+X)으로 블럭을 개수만큼 배치한다 (블럭 간 순간격 = ND × 2).
    /// 각 블럭에 XData "Diffuser"(=Type) / "Type" / "Size" / "ND" + "CMH" / "Disp"(한 대당 풍량, CMH 명령과 동일 패턴) 를 문자열로 기록.
    /// 블럭 이름은 "JArch_" + Type (JArch_RPD 등)이고, 정의는 매 실행마다
    /// 참조 도면(Blocks\JArch_Blocks.dwg)에서 가져와 덮어쓴다. XData 값은 Type("RPD") 그대로 사용.
    /// </summary>
    public class DiffuserInsertCommand
    {
        private record DiffuserSpec(string Type, string Size, int ND, int MinCmh, int StdCmh, int MaxCmh);

        // 디퓨저 선정표 (TYPE / SIZE / ND / 최소풍량 / 표준풍량 / 최대풍량, CMH)
        private static readonly DiffuserSpec[] Table =
        {
            new("RPD", "270A", 125,  140,  200,  380),
            new("RPD", "320A", 150,  205,  300,  550),
            new("RPD", "420A", 200,  360,  550,  970),
            new("RPD", "480A", 250,  555,  850, 1480),
            new("RPD", "550A", 300,  850, 1300, 2160),
            new("RPD", "600A", 350, 1100, 1800, 2930),
            new("RPD", "650A", 375, 1200, 2100, 3450),
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
            new("RAD", "650A", 350, 1000, 1500, 3000),
            new("RAD", "650A", 400, 1200, 1800, 3600),
            new("RAD", "820A", 450, 1350, 2000, 4000),
            new("RAD", "820A", 500, 1500, 2500, 5000),

            new("SAD", "250x250", 100,  100,  150,  300),
            new("SAD", "250x250", 125,  150,  225,  450),
            new("SAD", "300x300", 150,  200,  300,  600),
            new("SAD", "300x300", 200,  400,  600, 1200),
            new("SAD", "410x410", 250,  600,  900, 1800),
            new("SAD", "460x460", 300,  800, 1200, 2400),
            new("SAD", "620x620", 350, 1100, 1800, 2930),
            new("SAD", "620x620", 375, 1250, 2000, 3300),
        };

        private static readonly string[] Types = { "RPD", "SPD", "RAD", "SAD" };

        private const string BlockPrefix = "JArch_";

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

            // 3. 개수
            var pio = new PromptIntegerOptions("\n디퓨저 개수를 입력하세요: ");
            pio.LowerLimit = 1;
            pio.DefaultValue = 1;
            pio.UseDefaultValue = true;
            PromptIntegerResult pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK) return;
            int count = pir.Value;

            // 4. 선정 — 한 대당 풍량 이상인 최소 표준풍량 행
            double perUnit = roomCmh / count;
            var candidates = Table.Where(s => s.Type == type).OrderBy(s => s.StdCmh).ToList();
            DiffuserSpec? spec = candidates.FirstOrDefault(s => s.StdCmh >= perUnit);
            if (spec == null)
            {
                spec = candidates.Last();
                ed.WriteMessage($"\n[경고] 한 대당 {perUnit:0.#} CMH 는 {type} 최대 표준풍량({spec.StdCmh})을 초과합니다. 최대 사이즈로 배치합니다.");
            }
            ed.WriteMessage($"\n선정: {spec.Type} {spec.Size} ND{spec.ND} (표준 {spec.StdCmh} CMH, 한 대당 {perUnit:0.#} CMH × {count}개)");

            // 5. 배치 기준점
            PromptPointResult ppr = ed.GetPoint("\n배치 시작점을 지정하세요: ");
            if (ppr.Status != PromptStatus.OK) return;
            Point3d basePt = ppr.Value;

            // 6. 참조 도면에서 블럭 정의를 가져온다(기존 정의는 덮어씀). Transaction 밖에서 수행.
            string blockName = BlockPrefix + type;
            if (!JArchBlockLibrary.Import(db, ed, blockName)) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId btrId = bt[blockName];
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                tr.ChecRegNames(db, "Diffuser,Type,Size,ND,CMH,Disp");

                double spacing = spec.ND * 2.0;   // 블럭 간 순간격(외곽선 사이 거리)
                string cmhStr = perUnit.ToString("0.##");
                double pitch = 0;   // 첫 블럭 폭 + spacing (첫 블럭 삽입 후 결정)
                for (int i = 0; i < count; i++)
                {
                    var br = new BlockReference(basePt + Vector3d.XAxis * (pitch * i), btrId);
                    space.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);

                    if (i == 0)
                    {
                        Extents3d ext = br.GeometricExtents;
                        pitch = (ext.MaxPoint.X - ext.MinPoint.X) + spacing;
                    }

                    JXdata.SetXdata(br, "Diffuser", spec.Type);
                    JXdata.SetXdata(br, "Type", spec.Type);
                    JXdata.SetXdata(br, "Size", spec.Size);
                    JXdata.SetXdata(br, "ND", spec.ND.ToString());
                    JXdata.SetXdata(br, "CMH", cmhStr);
                    JXdata.SetXdata(br, "Disp", cmhStr);
                }

                tr.Commit();
            }

            ed.WriteMessage($"\n{blockName} 블럭 {count}개 배치 완료.");
        }
    }
}
