using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;

using CADExtension;

using OpenStudio;
using Point3d = Autodesk.AutoCAD.Geometry.Point3d;

namespace Acadv25JArch
{
    public class OpenStudioGeometryConverter
    {
        /// <summary>
        /// AutoCAD Polyline을 OpenStudio Space로 변환 (바닥, 벽, 천정 생성)
        /// </summary>
        public static Space CreateSpaceFromPolyline(
            Polyline polyline,
            double floorHeight,
            Model model,
            string spaceName = "Space")
        {
            // Space 생성
            var space = new Space(model);
            space.setName(spaceName);

            // Polyline의 정점들을 추출
            List<Point3d> vertices = GetPolylineVertices(polyline);

            // *** 중요: 반시계방향으로 정점 순서 보장 ***
            vertices = EnsureCounterClockwise(vertices);

            // 1. 바닥 생성 (Floor)
            CreateFloorSurface(vertices, model, space);

            // 2. 벽 생성 (Walls)
            CreateWallSurfaces(vertices, floorHeight, model, space);

            // 3. 천정 생성 (Ceiling)
            CreateCeilingSurface(vertices, floorHeight, model, space);

            return space;
        }

        /// <summary>
        /// Polyline의 정점들을 추출
        /// </summary>
        private static List<Point3d> GetPolylineVertices(Polyline polyline)
        {
            List<Point3d> vertices = new List<Point3d>();

            int numVertices = polyline.NumberOfVertices;

            for (int i = 0; i < numVertices; i++)
            {
                Point2d pt2d = polyline.GetPoint2dAt(i);
                vertices.Add(new Point3d(pt2d.X, pt2d.Y, 0));
            }

            return vertices;
        }

        /// <summary>
        /// 다각형이 시계방향인지 확인 (Shoelace Formula 사용)
        /// </summary>
        /// <param name="vertices">정점 리스트</param>
        /// <returns>양수: 반시계방향(CCW), 음수: 시계방향(CW)</returns>
        private static double CalculateSignedArea(List<Point3d> vertices)
        {
            double area = 0.0;
            int n = vertices.Count;

            for (int i = 0; i < n; i++)
            {
                Point3d p1 = vertices[i];
                Point3d p2 = vertices[(i + 1) % n];

                // Shoelace formula: (x1*y2 - x2*y1)
                area += (p1.X * p2.Y - p2.X * p1.Y);
            }

            return area / 2.0;
        }

        /// <summary>
        /// 정점들이 반시계방향(CCW)이 되도록 보장
        /// </summary>
        /// <param name="vertices">원본 정점 리스트</param>
        /// <returns>반시계방향으로 정렬된 정점 리스트</returns>
        private static List<Point3d> EnsureCounterClockwise(List<Point3d> vertices)
        {
            double signedArea = CalculateSignedArea(vertices);

            // 음수면 시계방향이므로 역순으로 변경
            if (signedArea < 0)
            {
                vertices.Reverse();
                System.Diagnostics.Debug.WriteLine("Polyline 방향을 시계방향에서 반시계방향으로 변경했습니다.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Polyline이 이미 반시계방향입니다.");
            }

            return vertices;
        }

        /// <summary>
        /// 바닥 Surface 생성
        /// </summary>
        private static void CreateFloorSurface(
            List<Point3d> vertices,
            Model model,
            Space space)
        {
            var osVertices = new Point3dVector();

            // *** 중요: 바닥의 법선은 아래쪽(-Z)을 향해야 함 ***
            // vertices는 위에서 봤을 때 CCW이므로, 법선이 위쪽(+Z)을 향함
            // 따라서 역순(CW)으로 배치하여 법선이 아래쪽(-Z)을 향하도록 함
            for (int i = vertices.Count - 1; i >= 0; i--)
            {
                var vertex = vertices[i];
                osVertices.Add(new OpenStudio.Point3d(vertex.X, vertex.Y, 0.0));
            }

            var floor = new OpenStudio.Surface(osVertices, model);
            floor.setName($"{space.nameString()}_Floor");
            floor.setSurfaceType("Floor");
            floor.setSpace(space);
        }

        /// <summary>
        /// 벽 Surface들 생성
        /// </summary>
        private static void CreateWallSurfaces(
            List<Point3d> vertices,
            double floorHeight,
            Model model,
            Space space)
        {
            int numWalls = vertices.Count;

            for (int i = 0; i < numWalls; i++)
            {
                Point3d p1 = vertices[i];
                Point3d p2 = vertices[(i + 1) % numWalls];

                var wallVertices = new Point3dVector();

                // *** 중요: 벽의 법선은 방 밖을 향해야 함 ***
                // 바닥이 위에서 봤을 때 CCW라면, p1->p2 방향으로 바닥 둘레를 따라감
                // 이 벽을 "방 밖에서" 봤을 때 CCW가 되도록 정점 배치:
                // 하단 왼쪽(p1,0) -> 하단 오른쪽(p2,0) -> 상단 오른쪽(p2,h) -> 상단 왼쪽(p1,h)
                // 결과: 법선이 방 밖을 향함 ✓

                wallVertices.Add(new OpenStudio.Point3d(p1.X, p1.Y, 0.0));
                wallVertices.Add(new OpenStudio.Point3d(p2.X, p2.Y, 0.0));
                wallVertices.Add(new OpenStudio.Point3d(p2.X, p2.Y, floorHeight));
                wallVertices.Add(new OpenStudio.Point3d(p1.X, p1.Y, floorHeight));

                var wall = new OpenStudio.Surface(wallVertices, model);
                wall.setName($"{space.nameString()}_Wall_{i + 1}");
                wall.setSurfaceType("Wall");
                wall.setSpace(space);
            }
        }

        /// <summary>
        /// 천정 Surface 생성
        /// </summary>
        private static void CreateCeilingSurface(
            List<Point3d> vertices,
            double floorHeight,
            Model model,
            Space space)
        {
            var osVertices = new Point3dVector();

            // *** 중요: 천정의 법선은 위쪽(+Z)을 향해야 함 ***
            // vertices는 위에서 봤을 때 CCW이므로, 
            // 그대로 사용하면 법선이 위쪽(+Z)을 향함 ✓
            foreach (var vertex in vertices)
            {
                osVertices.Add(new OpenStudio.Point3d(vertex.X, vertex.Y, floorHeight));
            }

            var ceiling = new OpenStudio.Surface(osVertices, model);
            ceiling.setName($"{space.nameString()}_Ceiling");
            ceiling.setSurfaceType("RoofCeiling");
            ceiling.setSpace(space);
        }
    }

    public class AutoCADToOpenStudioCommand
    {
        [CommandMethod("CreateOpenStudioSpace")]
        public void Cmd_CreateOpenStudioSpace()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Polyline 선택
            PromptEntityOptions peo = new PromptEntityOptions("\n바닥 평면 Polyline을 선택하세요: ");
            peo.SetRejectMessage("\nPolyline만 선택 가능합니다.");
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            // 층고 입력
            PromptDoubleOptions pdo = new PromptDoubleOptions("\n층고를 입력하세요 (미터): ");
            pdo.DefaultValue = 3.0;
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK) return;

            double floorHeight = pdr.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline polyline = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;

                // OpenStudio Model 생성
                var osModel = new OpenStudio.Model();

                // Space 생성
                var space = OpenStudioGeometryConverter.CreateSpaceFromPolyline(
                    polyline,
                    floorHeight,
                    osModel,
                    "Room_01"
                );

                // Thermal Zone 할당 (선택사항)
                var thermalZone = new OpenStudio.ThermalZone(osModel);
                thermalZone.setName("Zone_01");
                space.setThermalZone(thermalZone);

                // 모델 저장
                string savePath = @"C:\Temp\building_model.osm";
                OpenStudio.Path osPath = OpenStudio.OpenStudioUtilitiesCore.toPath(savePath);
                osModel.save(osPath, true);

                ed.WriteMessage($"\nOpenStudio 모델이 생성되었습니다: {savePath}");
                ed.WriteMessage($"\n- 바닥: 1개");
                ed.WriteMessage($"\n- 벽: {polyline.NumberOfVertices}개");
                ed.WriteMessage($"\n- 천정: 1개");

                tr.Commit();
            }
        }
    }

}
