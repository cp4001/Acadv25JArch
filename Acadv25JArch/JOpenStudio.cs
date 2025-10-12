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
        /// <param name="polyline">AutoCAD Polyline (바닥 평면)</param>
        /// <param name="floorHeight">층고 높이 (미터)</param>
        /// <param name="model">OpenStudio Model 객체</param>
        /// <param name="spaceName">생성될 Space 이름</param>
        /// <returns>생성된 OpenStudio Space</returns>
        public static Space CreateSpaceFromPolyline(
            Polyline polyline,
            double floorHeight,
            Model model,
            string spaceName = "Space")
        {
            // Space 생성
            var space = new OpenStudio.Space(model);
            space.setName(spaceName);

            // Polyline의 정점들을 추출
            List<Point3d> vertices = GetPolylineVertices(polyline);

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
        private static List<Autodesk.AutoCAD.Geometry.Point3d> GetPolylineVertices(Polyline polyline)
        {
            List<Point3d> vertices = new List<Point3d>();

            int numVertices = polyline.NumberOfVertices;

            // Polyline이 닫혀있지 않으면 마지막 점 추가
            int count = polyline.Closed ? numVertices : numVertices;

            for (int i = 0; i < count; i++)
            {
                Point2d pt2d = polyline.GetPoint2dAt(i);
                // AutoCAD 좌표를 미터 단위로 변환 (필요시)
                // 여기서는 이미 미터 단위라고 가정
                vertices.Add(new Point3d(pt2d.X, pt2d.Y, 0));
            }

            return vertices;
        }

        /// <summary>
        /// 바닥 Surface 생성
        /// </summary>
        private static void CreateFloorSurface(
            List<Point3d> vertices,
            Model model,
            OpenStudio.Space space)
        {
            // OpenStudio Point3dVector 생성
            var osVertices = new OpenStudio.Point3dVector();

            // 바닥은 위에서 봤을 때 시계 반대 방향 (CCW)
            foreach (var vertex in vertices)
            {
                osVertices.Add(new OpenStudio.Point3d(vertex.X, vertex.Y, 0.0));
            }

            // Floor Surface 생성
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
            OpenStudio.Model model,
            OpenStudio.Space space)
        {
            int numWalls = vertices.Count;

            for (int i = 0; i < numWalls; i++)
            {
                // 현재 점과 다음 점
                Point3d p1 = vertices[i];
                Point3d p2 = vertices[(i + 1) % numWalls];

                // 벽 정점 생성 (밖에서 봤을 때 시계 반대 방향)
                // 하단 왼쪽 -> 하단 오른쪽 -> 상단 오른쪽 -> 상단 왼쪽
                var wallVertices = new OpenStudio.Point3dVector();
                wallVertices.Add(new OpenStudio.Point3d(p1.X, p1.Y, 0.0));           // 하단 왼쪽
                wallVertices.Add(new OpenStudio.Point3d(p2.X, p2.Y, 0.0));           // 하단 오른쪽
                wallVertices.Add(new OpenStudio.Point3d(p2.X, p2.Y, floorHeight));   // 상단 오른쪽
                wallVertices.Add(new OpenStudio.Point3d(p1.X, p1.Y, floorHeight));   // 상단 왼쪽

                // Wall Surface 생성
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
            OpenStudio.Model model,
            OpenStudio.Space space)
        {
            // OpenStudio Point3dVector 생성
            var osVertices = new OpenStudio.Point3dVector();

            // 천정은 아래에서 봤을 때 시계 반대 방향 (위에서 보면 시계 방향)
            // 따라서 역순으로 정점 추가
            for (int i = vertices.Count - 1; i >= 0; i--)
            {
                var vertex = vertices[i];
                osVertices.Add(new OpenStudio.Point3d(vertex.X, vertex.Y, floorHeight));
            }

            // Ceiling Surface 생성
            var ceiling = new OpenStudio.Surface(osVertices, model);
            ceiling.setName($"{space.nameString()}_Ceiling");
            ceiling.setSurfaceType("RoofCeiling");
            ceiling.setSpace(space);
        }

        /// <summary>
        /// 창문을 벽에 추가하는 헬퍼 함수
        /// </summary>
        public static OpenStudio.SubSurface CreateWindow(
            OpenStudio.Surface wall,
            double x1, double y1, double z1,
            double x2, double y2, double z2,
            double x3, double y3, double z3,
            double x4, double y4, double z4,
            OpenStudio.Model model,
            string windowName = "Window")
        {
            var windowVertices = new OpenStudio.Point3dVector();
            windowVertices.Add(new OpenStudio.Point3d(x1, y1, z1));
            windowVertices.Add(new OpenStudio.Point3d(x2, y2, z2));
            windowVertices.Add(new OpenStudio.Point3d(x3, y3, z3));
            windowVertices.Add(new OpenStudio.Point3d(x4, y4, z4));

            var window = new OpenStudio.SubSurface(windowVertices, model);
            window.setName(windowName);
            window.setSubSurfaceType("FixedWindow");
            window.setSurface(wall);

            return window;
        }
    }

    public class AutoCADToOpenStudioCommand
    {
        [CommandMethod("CreateOpenStudioSpace")]
        public void CreateOpenStudioSpace()
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
