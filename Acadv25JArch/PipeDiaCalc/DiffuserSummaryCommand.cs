using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    /// <summary>
    /// Diffuser_Summary — Room Poly(XData "Room")를 다중 선택하면 Poly 안의 디퓨저 블럭
    /// (Insert_Diffuser / Insert_Diffuser_Bypoly 가 XData "Diffuser" 등을 기록한 것)을
    /// 실(Room) × 계통(SA/RA/EA/OA) × 사이즈별로 집계해 "덕트기구일람표" 를 Line + Text 로 그린다.
    /// 실명 = Poly 안의 RoomText(XData "RoomText") 문자열, 실 풍량 = Poly XData "CFM"(To_RoomCFM, 있을 때만).
    /// 풍량 = 한 대당 CMH, 전체풍량 = 같은 행 디퓨저 CMH 합계.
    /// </summary>
    public class DiffuserSummaryCommand
    {
        private record Row(string Sys, string Type, string Size, string ND, double Cmh, int Count, double Total);
        private record RoomSum(string Name, string? RoomCmh, List<Row> Rows);

        // Type → (형식, 비고). 표에 없는 Type 은 (Type, "") 로 표시
        private static readonly Dictionary<string, (string Form, string Note)> TypeInfo = new()
        {
            ["RPD"] = ("원형디퓨져", "PAN TYPE"),
            ["SPD"] = ("각형디퓨져", "PAN TYPE"),
            ["RAD"] = ("원형디퓨져", "ANEMO TYPE"),
            ["SAD"] = ("각형디퓨져", "ANEMO TYPE"),
        };

        // 표 치수 (도면 단위) — 모두 TextH 기준
        private const double TextH = 250.0;
        private const double RowH = 500.0;
        private const double HeadH = 600.0;
        private const double TitleH = 350.0;
        private static readonly double[] ColW = { 2700, 1050, 1100, 900, 1450, 1200, 1800, 1600, 2200 };
        private static readonly string[] Heads =
            { "실    명", "구 분", "풍 량", "수 량", "전체풍량", "N.D(%%c)", "형    식", "규    격", "비    고" };

        // 색상 (ACI) — 첨부 일람표 양식
        private const short ColTitle = 2;    // 제목 Yellow
        private const short ColBorder = 3;   // 상·하 외곽선, 제목 밑줄 Green
        private const short ColHeadLine = 2; // 헤더 아래선 Yellow
        private const short ColGrid = 1;     // 내부 격자 Red
        private const short ColText = 7;     // 본문 White
        private const short ColRoomCmh = 4;  // 실 풍량 Cyan

        [CommandMethod("Diffuser_Summary", CommandFlags.UsePickSet)]
        public void Cmd_DiffuserSummary()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1. 디퓨저 블럭 / RoomText 를 도면 전체에서 수집 (Editor 호출 → Transaction 밖)
            PromptSelectionResult bsr = ed.SelectAll(JSelFilter.MakeFilterTypesRegs("INSERT", "Diffuser"));
            if (bsr.Status != PromptStatus.OK || bsr.Value.Count == 0)
            {
                ed.WriteMessage("\n[오류] XData \"Diffuser\" 를 가진 디퓨저 블럭이 도면에 없습니다.");
                return;
            }
            ObjectId[] blockIds = bsr.Value.GetObjectIds();

            PromptSelectionResult rsr = ed.SelectAll(JSelFilter.MakeFilterTypesRegs("TEXT", "RoomText"));
            ObjectId[] roomTextIds = rsr.Status == PromptStatus.OK ? rsr.Value.GetObjectIds() : Array.Empty<ObjectId>();

            // 2. Poly 별 집계 — 값만 복사해 둔다
            var rooms = new List<RoomSum>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Polyline> polys = JEntityFunc.GetEntityByTpye<Polyline>(
                    "Room Poly 를 선택하세요",
                    JSelFilter.MakeFilterTypesRegs("LWPOLYLINE", "Room"));
                if (polys == null || polys.Count == 0) return;

                foreach (Polyline pl in polys)
                {
                    RoomSum? rs = CollectRoom(tr, pl, blockIds, roomTextIds, ed);
                    if (rs != null) rooms.Add(rs);
                }
                tr.Commit();
            }

            if (rooms.Count == 0)
            {
                ed.WriteMessage("\n[알림] 선택한 Poly 안에 디퓨저가 없습니다.");
                return;
            }
            rooms = rooms.OrderBy(r => r.Name, StringComparer.Ordinal).ToList();

            // 3. 제목(공조기 명칭) / 삽입점
            var pso = new PromptStringOptions("\n공조기 명칭 (예: AHU-MP-01, Enter=생략): ");
            pso.AllowSpaces = false;
            PromptResult psr = ed.GetString(pso);
            if (psr.Status != PromptStatus.OK) return;
            string ahu = psr.StringResult.Trim();
            string title = "덕 트 기 구 일 람 표" + (ahu.Length > 0 ? $" ({ahu})" : "");

            PromptPointResult ppr = ed.GetPoint("\n일람표 좌상단 점을 지정하세요: ");
            if (ppr.Status != PromptStatus.OK) return;

            // 4. 그리기
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                DrawTable(tr, space, db, ppr.Value, title, rooms);
                tr.Commit();
            }

            ed.WriteMessage($"\n덕트기구일람표 작성 완료 — 실 {rooms.Count}개, 행 {rooms.Sum(r => r.Rows.Count)}개.");
        }

        /// <summary>Poly 하나의 실명 / 실 풍량 / 디퓨저 행 목록. 디퓨저가 없으면 null.</summary>
        private static RoomSum? CollectRoom(Transaction tr, Polyline pl,
                                            ObjectId[] blockIds, ObjectId[] roomTextIds, Editor ed)
        {
            // 실명: Poly 안의 RoomText 중 가장 위의 것
            string? name = roomTextIds
                .Select(id => tr.GetObject(id, OpenMode.ForRead) as DBText)
                .Where(t => t != null && DiffuserInsertCommand.IsInsidePoly(pl, t.Position))
                .OrderByDescending(t => t!.Position.Y)
                .Select(t => t!.TextString)
                .FirstOrDefault();
            if (name == null)
            {
                name = $"Room({pl.Handle})";
                ed.WriteMessage($"\n[알림] Poly(핸들 {pl.Handle}) 안에 RoomText 가 없어 '{name}' 으로 표시합니다.");
            }

            // 실 풍량: Poly XData "CFM" (To_RoomCFM)
            string? roomCmh = null;
            if (double.TryParse(JXdata.GetXdata(pl, "CFM"), NumberStyles.Any, CultureInfo.InvariantCulture, out double cfm) && cfm > 0)
                roomCmh = $"{cfm:#,##0} CMH";

            // 디퓨저: Poly 안의 블럭을 (계통, Type, Size, ND, 한 대당 CMH) 로 묶는다
            var items = new List<(string Sys, string Type, string Size, string ND, string CmhStr, double Cmh)>();
            foreach (ObjectId id in blockIds)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not BlockReference br) continue;
                if (!DiffuserInsertCommand.IsInsidePoly(pl, br.Position)) continue;

                string cmhStr = JXdata.GetXdata(br, "CMH") ?? "";
                if (!double.TryParse(cmhStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double cmh))
                {
                    ed.WriteMessage($"\n[건너뜀] 블럭(핸들 {br.Handle}) — CMH '{cmhStr}' 를 읽을 수 없습니다.");
                    continue;
                }
                items.Add((JXdata.GetXdata(br, "SystemType") ?? "",
                           JXdata.GetXdata(br, "Diffuser") ?? "",
                           JXdata.GetXdata(br, "Size") ?? "",
                           JXdata.GetXdata(br, "ND") ?? "",
                           cmhStr, cmh));
            }

            if (items.Count == 0)
            {
                ed.WriteMessage($"\n[알림] '{name}' 안에 디퓨저가 없어 제외합니다.");
                return null;
            }

            int SysOrder(string s)
            {
                int i = Array.IndexOf(DiffuserInsertCommand.SystemTypes, s);
                return i < 0 ? int.MaxValue : i;
            }

            List<Row> rows = items
                .GroupBy(i => (i.Sys, i.Type, i.Size, i.ND, i.CmhStr))
                .Select(g => new Row(g.Key.Sys, g.Key.Type, g.Key.Size, g.Key.ND,
                                     g.First().Cmh, g.Count(), g.Sum(i => i.Cmh)))
                .OrderBy(r => SysOrder(r.Sys)).ThenBy(r => r.Type).ThenByDescending(r => r.Cmh)
                .ToList();

            return new RoomSum(name, roomCmh, rows);
        }

        private static void DrawTable(Transaction tr, BlockTableRecord space, Database db,
                                      Point3d p0, string title, List<RoomSum> rooms)
        {
            double x0 = p0.X, y0 = p0.Y, z = p0.Z;
            double width = ColW.Sum();
            int totalRows = rooms.Sum(r => r.Rows.Count);
            double yBot = y0 - HeadH - RowH * totalRows;

            // 열 경계 X (xs[0] = 좌측 끝, xs[n] = 우측 끝)
            var xs = new double[ColW.Length + 1];
            xs[0] = x0;
            for (int c = 0; c < ColW.Length; c++) xs[c + 1] = xs[c] + ColW[c];
            double ColCenter(int c) => (xs[c] + xs[c + 1]) * 0.5;

            // 제목 + 밑줄 (밑줄 길이는 제목 폭에 맞춘다)
            double underY = y0 + RowH * 0.35;
            DBText t = AddText(tr, space, db, title, new Point3d(x0 + TextH * 0.4, underY + TextH * 0.4, z),
                               TitleH, ColTitle, false);
            Extents3d te = t.GeometricExtents;
            AddLine(tr, space, new Point3d(x0, underY, z),
                    new Point3d(Math.Max(te.MaxPoint.X + TextH * 0.4, x0 + ColW[0]), underY, z), ColBorder);

            // 외곽선 / 헤더선
            AddLine(tr, space, new Point3d(x0, y0, z), new Point3d(x0 + width, y0, z), ColBorder);
            AddLine(tr, space, new Point3d(x0, y0 - HeadH, z), new Point3d(x0 + width, y0 - HeadH, z), ColHeadLine);
            AddLine(tr, space, new Point3d(x0, yBot, z), new Point3d(x0 + width, yBot, z), ColBorder);

            // 내부 세로선 (좌·우 바깥선은 없음)
            for (int c = 1; c < ColW.Length; c++)
                AddLine(tr, space, new Point3d(xs[c], y0, z), new Point3d(xs[c], yBot, z), ColGrid);

            // 헤더
            for (int c = 0; c < Heads.Length; c++)
                AddText(tr, space, db, Heads[c], new Point3d(ColCenter(c), y0 - HeadH * 0.5, z), TextH, ColText);

            // 본문
            double y = y0 - HeadH;
            for (int ri = 0; ri < rooms.Count; ri++)
            {
                RoomSum room = rooms[ri];
                double groupH = RowH * room.Rows.Count;
                double midY = y - groupH * 0.5;

                // 실명 (여러 행 병합) + 실 풍량
                if (room.RoomCmh == null)
                {
                    AddText(tr, space, db, room.Name, new Point3d(ColCenter(0), midY, z), TextH, ColText);
                }
                else
                {
                    AddText(tr, space, db, room.Name, new Point3d(ColCenter(0), midY + TextH * 0.4, z), TextH, ColText);
                    AddText(tr, space, db, room.RoomCmh, new Point3d(ColCenter(0), midY - TextH * 0.75, z), TextH * 0.7, ColRoomCmh);
                }

                for (int k = 0; k < room.Rows.Count; k++)
                {
                    Row r = room.Rows[k];
                    double cy = y - RowH * (k + 0.5);
                    (string form, string note) = TypeInfo.TryGetValue(r.Type, out var ti) ? ti : (r.Type, "");
                    string[] cells =
                    {
                        r.Sys,
                        r.Cmh.ToString("#,##0"),
                        r.Count.ToString(),
                        r.Total.ToString("#,##0"),
                        r.ND,
                        form,
                        FormatSize(r.Size),
                        note,
                    };
                    for (int c = 0; c < cells.Length; c++)
                        AddText(tr, space, db, cells[c], new Point3d(ColCenter(c + 1), cy, z), TextH, ColText);

                    // 같은 실 안의 행 구분선 — 실명 열은 병합이라 제외
                    if (k < room.Rows.Count - 1)
                        AddLine(tr, space, new Point3d(xs[1], y - RowH * (k + 1), z),
                                new Point3d(x0 + width, y - RowH * (k + 1), z), ColGrid);
                }

                y -= groupH;
                // 실 사이 구분선 (마지막 실 아래는 하단 외곽선)
                if (ri < rooms.Count - 1)
                    AddLine(tr, space, new Point3d(x0, y, z), new Point3d(x0 + width, y, z), ColGrid);
            }
        }

        /// <summary>"600A" → "%%c600"(원형), "300x300" 등 각형은 그대로.</summary>
        private static string FormatSize(string size)
        {
            if (size.EndsWith("A", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(size[..^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int d))
                return "%%c" + d;
            return size;
        }

        private static DBText AddText(Transaction tr, BlockTableRecord space, Database db,
                                      string s, Point3d at, double h, short color, bool center = true)
        {
            var t = new DBText
            {
                TextString = s,
                Height = h,
                Position = at,
                ColorIndex = color,
                TextStyleId = db.Textstyle,
            };
            if (center)
            {
                t.HorizontalMode = TextHorizontalMode.TextCenter;
                t.VerticalMode = TextVerticalMode.TextVerticalMid;
                t.AlignmentPoint = at;
            }
            space.AppendEntity(t);
            tr.AddNewlyCreatedDBObject(t, true);
            if (center) t.AdjustAlignment(db);
            return t;
        }

        private static void AddLine(Transaction tr, BlockTableRecord space, Point3d a, Point3d b, short color)
        {
            var ln = new Line(a, b) { ColorIndex = color };
            space.AppendEntity(ln);
            tr.AddNewlyCreatedDBObject(ln, true);
        }
    }
}
