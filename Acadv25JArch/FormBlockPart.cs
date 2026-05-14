using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using CADExtension;
using OfficeOpenXml;
//using Maroquio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace Acadv25JArch
{
    public partial class FormBlockPart : Form
    {
        public static List<ObjectId> selids = new List<ObjectId>();                 // 선택된 Blocks
        public static List<BlockPart> blockparts = new List<BlockPart>();        // Group화된 BlockPart
        public static BindingSource bs = new BindingSource();
        public static BindingSource bs_room = new BindingSource();
        public static BindingSource bs_window = new BindingSource();          // WindowPart 용
        public static BindingSource bs_door = new BindingSource();              // DoorPart 용
        public static BindingSource bsNumDia = new BindingSource();

        //-- Temp Graphic View 
        private DBObjectCollection m_mrkers = new DBObjectCollection();
        private IntegerCollection intColl = new IntegerCollection();


        // 어셈블리 리졸브 핸들러 등록
        static FormBlockPart()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        // 어셈블리 리졸브 핸들러
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // 어셈블리 이름 파싱
            string assemblyName = new AssemblyName(args.Name).Name;

            // DLL 경로
            string dllPath = Path.Combine(@"C:\Jarch25", assemblyName + ".dll");

            if (File.Exists(dllPath))
            {
                return Assembly.LoadFrom(dllPath);
            }

            return null;
        }

        public FormBlockPart()
        {
            InitializeComponent();
            // EPPlus 8 이상에서는 이렇게 설정
            // EPPlus 8 라이선스 설정 - 개인 비상업용
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("junhoi");
        }

        private void btnAllBlocks_Click(object sender, EventArgs e)
        {
            //var entids = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
            selids.Clear();
            //blockparts = BlockPart.GetSelectedBlockParts();
            blockparts = BlockPart.GetAllBlockParts();
            //// Img  가 없는것은 제외   
            //blockparts = blockparts.Where(x => x.Img != null).ToList();
            if (blockparts == null)
            {
                MessageBox.Show(" 대상을 선택 하세요");
                return;
            }
            SortableBindingList<BlockPart>
                      drbl = new SortableBindingList<BlockPart>(blockparts);
            bs.DataSource = drbl;
            dgvBlock.DataSource = bs;
        }

        private void btnRooms_Click(object sender, EventArgs e)
        {
            var roomparts = RoomPart.GetAllRoomParts();
            if (roomparts == null)
            {
                MessageBox.Show(" room  이 없습니다.");
                return;
            }
            SortableBindingList<RoomPart> drbl = new SortableBindingList<RoomPart>(roomparts);
            bs_room.DataSource = drbl;
            dgvRoom.DataSource = bs_room;
        }

        private void btnExcel_Click(object sender, EventArgs e)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                string fullPath = doc.Database.Filename;

                if (string.IsNullOrEmpty(fullPath)) return;
                 
                // 핵심: 확장자를 .dwg에서 .xlsm으로 변경
                string excelPath = Path.ChangeExtension(fullPath, ".xlsm");


                //// EPPlus 라이선스 설정 (이 줄 추가!)
                //ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                // 현재 실행 중인 DLL의 위치
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dllFolder = System.IO.Path.GetDirectoryName(dllPath);

                // 같은 폴더에 있는 Excel 파일
                string filePath = System.IO.Path.Combine(dllFolder, "Excel", "load_eng.xlsm");

                //// Excel 파일 경로
                //string filePath = @"C:\Jarch25\load_eng.xlsm"; ;// @"C:\Jarch25\load_Calc_org.xlsm";

                // 파일 존재 확인
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"파일이 없습니다: {filePath}");
                    return;
                }

                //copy 원본파일 excelPath 로 복사
                File.Copy(filePath, excelPath, true);


                var roomparts = RoomPart.GetAllRoomParts();
                if (roomparts == null || roomparts.Count == 0)
                {
                    MessageBox.Show("roomparts가 없습니다.");
                    return;
                }

                // 워크북 열기
                FileInfo fileInfo = new FileInfo(excelPath);
                using (var package = new ExcelPackage(fileInfo))
                {
                    // 시트 존재 확인
                    var worksheet = package.Workbook.Worksheets["Load(w)"]; //부하계산
                    if (worksheet == null)
                    {
                        MessageBox.Show("'부하계산' 시트가 없습니다.");
                        return;
                    }

                    // int rdex = 50; // room 별로 50 Row 생성 
                    int rindex = 0; // room 시작 행
                                    //room 별로  원본 1~50 행 복사 

                    // 1~50행을 51~100행으로 복사
                    // EPPlus는 전체 행 범위를 직접 복사
                    for (int roomcnt = 1; roomcnt <= roomparts.Count - 1; roomcnt++)
                    {
                        for (int row = 1; row <= 50; row++)
                        {
                            int targetRow = row + roomcnt * 50; // 51~100행

                            // 각 행의 사용된 열 범위 복사
                            var sourceRow = worksheet.Cells[row, 1, row, worksheet.Dimension.End.Column];
                            var targetCell = worksheet.Cells[targetRow, 1];

                            // 값, 수식, 서식 모두 복사
                            sourceRow.Copy(targetCell);
                        }

                    }


                    foreach (var room in roomparts)
                    {
                        Console.WriteLine($"Room Name: {room.Name}, Floor Area: {room.FloorArea}");

                        var walls = room.GetWalls();
                        var walls1 = walls.OrderBy(x => x.StartsWith("P")).ToList(); // P로 시작하지 않는 Wall  앞으로 배치
                        var windows = room.GetWindows();
                        var doors = room.GetDoors();
                        //var windows1= windows.OrderBy(x => x.StartsWith("P")).ToList(); // P로 시작하지 않는 Wall  앞으로 배치 


                        int rdex = 2 + rindex * 50; // room 번호, 층 이름 ,층고,실명,천정고
                                                    // 실번호
                        worksheet.Cells[$"T{rdex}"].Value = room.RoomIndex;

                        // 층 이름: 1층 2층
                        worksheet.Cells[$"T{rdex + 1}"].Value = room.Floor;

                        // 층 고: FloorHeight (double)
                        worksheet.Cells[$"T{rdex + 2}"].Value = room.FloorHeight; // double 직접 할당

                        // 실 명:   
                        worksheet.Cells[$"T{rdex + 3}"].Value = room.Name;

                        // 실 면적 :   
                        worksheet.Cells[$"T{rdex + 4}"].Value = room.FloorArea;

                        // 천정고: (double)
                        worksheet.Cells[$"T{rdex + 5}"].Value = room.CeilingHeight; // double 직접 할당

                        //if (walls == null || walls.Count == 0) continue;
                        int windex = 11 + rindex * 50;
                        foreach (var ww in windows) // 외창 처리 -- windows 는 내창과 외창이 섞여있음  
                        {
                            if (ww.Contains("P")) continue; // 내창은 패스  
                            // 쉼표(',')를 기준으로 자르기
                            string[] window = ww.Split(':');

                            //항목
                            worksheet.Cells[$"S{windex}"].Value = "Window";// "NW";"외  창"
                            // 방위각
                            worksheet.Cells[$"T{windex}"].Value = window[0];// "NW";
                            // 면적 (수식)
                            worksheet.Cells[$"U{windex}"].Formula = window[1];// "3*3";
                            windex++;
                        }

                        // 벽처리  외벽외창 + 도어 + 외벽+내벽    
                        int waldex = 19 + rindex * 50;
                        //외벽에 걸친 외창 처리
                        foreach (var ww in windows) // 외창 + 내창 처리 
                        {
                            // 쉼표(',')를 기준으로 자르기
                            string[] window = ww.Split(':');
                            //// 방위각
                            //worksheet.Cells[$"S{waldex}"].Value = "외  창";// "NW";
                            //if(ww.StartsWith("P")) worksheet.Cells[$"S{waldex}"].Value = "내  창";

                            //항목
                            worksheet.Cells[$"W{waldex}"].Value = window[0].Contains("P") ? "Window" : "Window"; //"외벽";   
                            worksheet.Cells[$"X{waldex}"].Value = window[0];// "NW";
                            worksheet.Cells[$"Y{waldex}"].Formula = window[1];// "3*3// 면적 (수식)
                            waldex++;
                        }
                        // 걸친 Door 처리
                        foreach (var dr in doors) // 외창 + 내창 처리 
                        {
                            // 쉼표(',')를 기준으로 자르기
                            string[] door = dr.Split(':');
                            //// 방위각
                            //worksheet.Cells[$"S{waldex}"].Value = "외  창";// "NW";
                            //if(ww.StartsWith("P")) worksheet.Cells[$"S{waldex}"].Value = "내  창";

                            //항목
                            worksheet.Cells[$"W{waldex}"].Value = door[0].Contains("P") ? "Door" : "Door"; //"외벽";   //"내벽도어" : "외벽도어"
                            worksheet.Cells[$"X{waldex}"].Value = door[0];// "NW";
                            worksheet.Cells[$"Y{waldex}"].Formula = door[1];// "3*3// 면적 (수식)
                            waldex++;
                        }
                        foreach (var wal in walls1)
                        {
                            //Wall 처리 
                            // 쉼표(':')를 기준으로 자르기
                            string[] wall = wal.Split(':');
                            //항목
                            worksheet.Cells[$"W{waldex}"].Value = wall[0].Contains("P") ? "Wall" : "Wall"; //"외벽";   "내   벽" : "외   벽"
                            // 방위각
                            worksheet.Cells[$"X{waldex}"].Value = wall[0];// "NW" Or P
                            // 면적 (수식)
                            worksheet.Cells[$"Y{waldex}"].Formula = wall[1];// "3*3";
                            waldex++;
                        }

                        rindex++; // 다음 room 으로 
                    }

                    // 저장
                    // 3. FileInfo 객체 생성 (EPPlus는 string 경로 대신 FileInfo를 요구함)
                    //FileInfo excelFile = new FileInfo(excelPath);
                    //package.SaveAs(excelFile);
                    package.Save();
                    MessageBox.Show($"{excelPath} 저장 완료!");
                    ed.WriteMessage($"\n{excelPath} \n저장 완료!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}");

            }
        }

        private void FormBlockPart_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1800, 750);
        }

        private void btnWindows_Click(object sender, EventArgs e)
        {
            var windowparts = WindowPart.GetAllBlockParts();
            if (windowparts == null)
            {
                MessageBox.Show(" Windows가   없습니다.");
                return;
            }
            SortableBindingList<WindowPart> drbl = new SortableBindingList<WindowPart>(windowparts);
            bs_window.DataSource = drbl;
            dgvWindow.DataSource = bs_window;
        }

        private void btnDoors_Click(object sender, EventArgs e)
        {
            var doorparts = DoorPart.GetAllBlockParts();
            if (doorparts == null)
            {
                MessageBox.Show(" Windows가   없습니다.");
                return;
            }
            SortableBindingList<DoorPart> drbl = new SortableBindingList<DoorPart>(doorparts);
            bs_door.DataSource = drbl;
            dgvDoor.DataSource = bs_door;
        }

        private void dgvRoom_KeyDown(object sender, KeyEventArgs e) // Return 입력시 Action 
        {
            e.Handled = true; // 엔터시 row 가 다음으로 가는 것을 방지 
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ClearMarkers();

            DataGridView dgv = sender as DataGridView;
            if (dgv != null)
            {

                RoomPart selectedRoom = (RoomPart)dgvRoom.CurrentRow.DataBoundItem;


                if (selectedRoom == null) return;

                //DBText dt = newObjId.GetObject(OpenMode.ForWrite, false) as DBText;
                //Line dLine = newObjId.GetObject(OpenMode.ForWrite, false) as Line;
                //Arc dArc = newObjId.GetObject(OpenMode.ForWrite, false) as Arc;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {

                    //Entity ent = newObjId.GetObject(OpenMode.ForWrite, false) as Entity;
                    Entity ent = selectedRoom.GetPoly();

                    //dt.ColorIndex = 41;
                    //string[] Xpos = row.Cells["MidP"].Value.ToString().Split('.');
                    // Update the display and display an alert message
                    //doc.Editor.Regen();

                    //Point3d ctrPt = new Point3d(Double.Parse(Xpos[0]), Double.Parse(Xpos[1]), 0);
                    Point3d ctrPt = new Point3d();
                    Point3d MarkerCtrpt = new Point3d();


                    if (ent != null)
                    {
                        ctrPt = ent.GetEntiyGeoCenter();
                        MarkerCtrpt = ctrPt;
                    }

                    ZoomExtensionMethods.ZoomCenter(ed, ctrPt, 1);

                    //doc.CommandWillStart += new CommandEventHandler(doc_CommandWillStart);

                    //Entity Temp Graphics Marker
                    DBText acTExt = new DBText();
                    //MarkerCtrpt = new Point3d(ctrPt.X + dt.Height / 2, ctrPt.Y, 0.0);

                    //acTExt.Position = MarkerCtrpt; //new Point3d(ctrPt.X , ctrPt.Y, 0.0);
                    //acTExt.Height = 300;
                    //acTExt.TextString = "O";
                    ////acTExt.LineWeight = LineWeight.LineWeight040;
                    //acTExt.ColorIndex = 131;

                    Circle circ = new Circle(ctrPt, Vector3d.ZAxis, 1000);
                    circ.ColorIndex = 210;
                    circ.LineWeight = LineWeight.LineWeight040;
                    //circ.LineWeight = LineWeight.LineWeight030;
                    TransientManager.CurrentTransientManager.AddTransient(circ, TransientDrawingMode.DirectTopmost, 128, intColl);

                    //m_mrkers.Add(acTExt);
                    m_mrkers.Add(circ);
                    tr.Commit();
                }

            }
        }

        private void ClearMarkers()
        {
            if (m_mrkers.Count >= 1)
            {
                TransientManager.CurrentTransientManager.EraseTransient(m_mrkers[0], intColl);
                m_mrkers[0].Dispose();
                m_mrkers.Clear();
            }

        }

        private void btnExcel1_Click(object sender, EventArgs e)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                string fullPath = doc.Database.Filename;

                if (string.IsNullOrEmpty(fullPath)) return;

                // 확장자를 .dwg에서 .xlsx로 변경
                string excelPath = Path.ChangeExtension(fullPath, ".xlsx");

                //// 템플릿 파일 경로
                //string templatePath = @"C:\Jarch25\Excel\LoadCalcRow.xlsx";

                //// 파일 존재 확인
                //if (!File.Exists(templatePath))
                //{
                //    MessageBox.Show($"템플릿 파일이 없습니다: {templatePath}");
                //    return;
                //}

                // 현재 실행 중인 DLL의 위치
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dllFolder = System.IO.Path.GetDirectoryName(dllPath);

                // 같은 폴더에 있는 Excel 파일
                string templatePath = System.IO.Path.Combine(dllFolder, "Excel", "LoadCalcRow.xlsx");

                //// Excel 파일 경로
                //string filePath = @"C:\Jarch25\load_eng.xlsm"; ;// @"C:\Jarch25\load_Calc_org.xlsm";

                // 파일 존재 확인
                if (!File.Exists(templatePath))
                {
                    MessageBox.Show($"파일이 없습니다: {templatePath}");
                    return;
                }



                // 템플릿 파일을 excelPath로 복사
                File.Copy(templatePath, excelPath, true);

                var roomparts = RoomPart.GetAllRoomParts();
                if (roomparts == null || roomparts.Count == 0)
                {
                    MessageBox.Show("roomparts가 없습니다.");
                    return;
                }

                // 워크북 열기
                FileInfo fileInfo = new FileInfo(excelPath);
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // 첫 번째 시트

                    int currentRow = 4; // 헤더가 1-3행이므로 4행부터 데이터 입력

                    foreach (var room in roomparts)
                    {
                        Console.WriteLine($"Room Name: {room.Name}, Floor Area: {room.FloorArea}");

                        var walls = room.GetWalls();
                        var walls1 = walls.OrderBy(x => x.StartsWith("P")).ToList();
                        var windows = room.GetWindows();
                        var doors = room.GetDoors();

                        // === Basic Data (A-F 컬럼) ===
                        int col = 1;

                        // A: Room Number (실번호)
                        worksheet.Cells[currentRow, col++].Value = room.RoomIndex;

                        // B: Floor (층 이름)
                        worksheet.Cells[currentRow, col++].Value = room.Floor;

                        // C: Floor Height (층고)
                        worksheet.Cells[currentRow, col++].Value = room.FloorHeight;

                        // D: Room Name (실명)
                        worksheet.Cells[currentRow, col++].Value = room.Name;

                        // E: Ceiling Height (천정고)
                        worksheet.Cells[currentRow, col++].Value = room.CeilingHeight;

                        // F: Area (면적)
                        worksheet.Cells[currentRow, col++].Value = room.FloorArea;

                        // G: Roof Load
                        worksheet.Cells[currentRow, col++].Value = "";

                        // H: Floor Load
                        worksheet.Cells[currentRow, col++].Value = "";

                        // === Glass 1-5 (I-R 컬럼, 외창만) ===> Glass 1-10  i-AB
                        var outerWindows = windows.Where(w => !w.Contains("P")).ToList();
                        for (int i = 0; i < 10; i++)
                        {
                            if (i < outerWindows.Count)
                            {
                                string[] window = outerWindows[i].Split(':');

                                // Direction
                                worksheet.Cells[currentRow, col++].Value = window[0];

                                // Area (수식)
                                worksheet.Cells[currentRow, col++].Formula = window[1];
                            }
                            else
                            {
                                col += 2; // 빈 칸
                            }
                        }

                        // === Wall 1-5 (S-AB 컬럼, 외벽만) ===> Wall 1-5 (AC-AV 컬럼, 외벽만)
                        var outerWalls = walls1.Where(w => !w.StartsWith("P")).Take(5).ToList();
                        for (int i = 0; i < 10; i++)
                        {
                            if (i < outerWalls.Count)
                            {
                                string[] wall = outerWalls[i].Split(':');

                                // Direction
                                worksheet.Cells[currentRow, col++].Value = wall[0];

                                // Area (수식)
                                worksheet.Cells[currentRow, col++].Formula = wall[1];
                            }
                            else
                            {
                                col += 2; // 빈 칸
                            }
                        }

                        // === Partition 1-5 (AC-AL 컬럼, 내벽 + 내창) ===>  Partition 1-5 (AW-BF 컬럼, 내벽 + 내창)
                        var innerWalls = walls1.Where(w => w.StartsWith("P")).ToList();
                        var innerWindows = windows.Where(w => w.Contains("P")).ToList();
                        var partitions = new List<string>();
                        partitions.AddRange(innerWalls);
                        partitions.AddRange(innerWindows);

                        for (int i = 0; i < 5; i++)
                        {
                            if (i < partitions.Count)
                            {
                                string[] partition = partitions[i].Split(':');

                                // Type (Partition 또는 Window)
                                string type = partition[0].Contains("P") ? "Partition" : "Window";
                                worksheet.Cells[currentRow, col++].Value = type;

                                // Area (수식)
                                worksheet.Cells[currentRow, col++].Formula = partition[1];
                            }
                            else
                            {
                                col += 2; // 빈 칸
                            }
                        }

                        // === Door 1-2 (AM-AP 컬럼) ===  Door 1-2 (BG-BJ 컬럼
                        for (int i = 0; i < 2; i++)
                        {
                            if (i < doors.Count)
                            {
                                string[] door = doors[i].Split(':');

                                // Direction
                                worksheet.Cells[currentRow, col++].Value = door[0];

                                // Area (수식)
                                worksheet.Cells[currentRow, col++].Formula = door[1];
                            }
                            else
                            {
                                col += 2; // 빈 칸
                            }
                        }

                        currentRow++; // 다음 행으로
                    }

                    // 저장
                    package.Save();
                    MessageBox.Show($"{excelPath} 저장 완료!");
                    ed.WriteMessage($"\n{excelPath} \n저장 완료!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        // RTS Input JSON 출력 (RTS_Input_Jason.md 사양)
        // - Polyline 정점 → CCW from SW, m 단위, 빌딩 SW 원점 정규화
        // - 각 변(edge)별 OutWall 검사 + Window/Door 블록 면적 누적
        // - CAD 비파생 항목(location/climate/U/glazing/schedule)은 spec 예시 기본값
        private void btnRtsJson_Click(object sender, EventArgs e)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;
                string fullPath = doc.Database.Filename;
                if (string.IsNullOrEmpty(fullPath)) return;

                string jsonPath = Path.Combine(
                    Path.GetDirectoryName(fullPath),
                    Path.GetFileNameWithoutExtension(fullPath) + "_RTS.json");

                var roomparts = RoomPart.GetAllRoomParts();
                if (roomparts == null || roomparts.Count == 0)
                {
                    MessageBox.Show("roomparts 가 없습니다.");
                    return;
                }

                const double scale = 0.001; // AutoCAD mm → m

                // 빌딩 BBox 최소(mm) — 모든 룸의 정점 SW
                double minX_mm = double.MaxValue, minY_mm = double.MaxValue;
                foreach (var rp in roomparts)
                {
                    var poly = rp.GetPoly();
                    if (poly == null) continue;
                    for (int i = 0; i < poly.NumberOfVertices; i++)
                    {
                        var p = poly.GetPoint2dAt(i);
                        if (p.X < minX_mm) minX_mm = p.X;
                        if (p.Y < minY_mm) minY_mm = p.Y;
                    }
                }
                double minX_m = minX_mm * scale;
                double minY_m = minY_mm * scale;

                var rooms = new List<object>();

                using (DocumentLock docLock = doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var rp in roomparts)
                    {
                        var poly = rp.GetPoly();
                        if (poly == null) continue;
                        int n = poly.NumberOfVertices;
                        if (n < 3) continue;

                        // 1. 정점 (m, 원점 정규화)
                        var verts = new List<(double x, double y)>();
                        for (int i = 0; i < n; i++)
                        {
                            var p = poly.GetPoint2dAt(i);
                            verts.Add((p.X * scale - minX_m, p.Y * scale - minY_m));
                        }

                        // 2. 변별 메타 (edge i: vert[i] → vert[(i+1)%n] in 원본 순서)
                        var lines = poly.GetLines();
                        var edgeMeta = new List<RtsEdgeMeta>();
                        for (int i = 0; i < n; i++)
                            edgeMeta.Add(BuildRtsEdgeMeta(lines[i], rp.FloorHeight));

                        // 3. CCW 정규화
                        if (RtsSignedArea(verts) < 0)
                        {
                            verts.Reverse();
                            // 정점 reverse 시 새 edge[i]는 원래 edge[(2n-2-i) mod n] 의 역방향
                            var rotated = new List<RtsEdgeMeta>(n);
                            for (int i = 0; i < n; i++)
                                rotated.Add(edgeMeta[(2 * n - 2 - i) % n]);
                            edgeMeta = rotated;
                        }

                        // 4. SW 부터 시작 (Y 최소, 동률시 X 최소)
                        int swIdx = 0;
                        for (int i = 1; i < n; i++)
                        {
                            if (verts[i].y < verts[swIdx].y - 1e-9 ||
                                (Math.Abs(verts[i].y - verts[swIdx].y) < 1e-9 && verts[i].x < verts[swIdx].x))
                                swIdx = i;
                        }
                        if (swIdx > 0)
                        {
                            verts = verts.Skip(swIdx).Concat(verts.Take(swIdx)).ToList();
                            edgeMeta = edgeMeta.Skip(swIdx).Concat(edgeMeta.Take(swIdx)).ToList();
                        }

                        // 5. surfaces
                        var surfaces = new List<object>();
                        for (int i = 0; i < n; i++)
                        {
                            var (vx0, vy0) = verts[i];
                            var (vx1, vy1) = verts[(i + 1) % n];
                            double dx = vx1 - vx0;
                            double dy = vy1 - vy0;
                            double length_m = Math.Sqrt(dx * dx + dy * dy);
                            if (length_m < 1e-6) continue;

                            // CCW 외향 법선 = (dy, -dx). 남=0, 동=-90, 서=+90, 북=±180
                            double nx = dy, ny = -dx;
                            double az = Math.Atan2(-nx, -ny) * 180.0 / Math.PI;

                            var meta = edgeMeta[i];
                            double edgeArea = length_m * rp.FloorHeight;
                            double winArea = meta.WindowAreas.Sum();
                            double doorArea = meta.DoorAreas.Sum();
                            double opaqueArea = Math.Max(edgeArea - winArea - doorArea, 0.0);

                            if (meta.IsOuterWall)
                            {
                                if (opaqueArea > 0.01)
                                {
                                    surfaces.Add(new
                                    {
                                        type = "ExteriorWall",
                                        name = $"외벽 #{i}",
                                        wall_cts_id = 33,
                                        area_m2 = Math.Round(opaqueArea, 2),
                                        azimuth_deg = Math.Round(az, 1),
                                        tilt_deg = 90,
                                        color = "Dark",
                                        surroundings = "Vertical",
                                        edge_index = i
                                    });
                                }
                                foreach (var wa in meta.WindowAreas)
                                {
                                    surfaces.Add(new
                                    {
                                        type = "Window",
                                        name = $"창 #{i}",
                                        glazing_id = "5d",
                                        u_si = 3.18,
                                        area_m2 = Math.Round(wa, 2),
                                        azimuth_deg = Math.Round(az, 1),
                                        tilt_deg = 90,
                                        edge_index = i,
                                        interior_shading = new
                                        {
                                            type = "VenetianBlinds",
                                            iac_0 = 0.74,
                                            iac_60 = 0.65,
                                            iac_diff = 0.79
                                        }
                                    });
                                }
                                foreach (var da in meta.DoorAreas)
                                {
                                    surfaces.Add(new
                                    {
                                        type = "ExteriorWall",
                                        name = $"외부도어 #{i}",
                                        wall_cts_id = 33,
                                        area_m2 = Math.Round(da, 2),
                                        azimuth_deg = Math.Round(az, 1),
                                        tilt_deg = 90,
                                        color = "Dark",
                                        surroundings = "Vertical",
                                        edge_index = i
                                    });
                                }
                            }
                            else
                            {
                                surfaces.Add(new
                                {
                                    type = "InteriorPartition",
                                    name = $"내벽 #{i}",
                                    u_si = 1.7,
                                    area_m2 = Math.Round(edgeArea, 2),
                                    adjacent_temp_C = 26.0,
                                    edge_index = i
                                });
                            }
                        }

                        double floorAreaM2 = 0;
                        double.TryParse(rp.FloorArea, out floorAreaM2);

                        rooms.Add(new
                        {
                            id = string.IsNullOrEmpty(rp.RoomIndex) ? $"R{rp.Index:D3}" : rp.RoomIndex,
                            name = string.IsNullOrEmpty(rp.Name) ? $"Room {rp.Index}" : rp.Name,
                            floor_area_m2 = Math.Round(floorAreaM2, 2),
                            ceiling_height_m = rp.CeilingHeight,
                            construction_class = "Medium",
                            has_carpet = false,
                            glass_pct = 50,
                            is_interior_zone = false,
                            position = new
                            {
                                vertices = verts.Select(v => new
                                {
                                    x_m = Math.Round(v.x, 3),
                                    y_m = Math.Round(v.y, 3)
                                }).ToArray()
                            },
                            surfaces = surfaces,
                            occupancy = new
                            {
                                max_persons = 2,
                                sensible_W_per_person = 75,
                                latent_W_per_person = 55,
                                schedule_24h = new double[] { 0,0,0,0,0,0,0, 1,1,1,1, 0.5,1,1,1,1,1, 0,0,0,0,0,0,0 }
                            },
                            lighting = new
                            {
                                type = "Pendant_Fluorescent",
                                total_W = 200,
                                use_factor = 1.0,
                                special_allowance = 0.85,
                                schedule_24h = new double[] { 0,0,0,0,0,0, 1,1,1,1,1,1,1,1,1,1,1,1, 0,0,0,0,0,0 }
                            },
                            equipment = new
                            {
                                type = "OfficeEquipment",
                                total_W = 250,
                                radiant_fraction = 0.20,
                                schedule_24h = new double[] { 0,0,0,0,0,0,0, 1,1,1,1, 0.5,1,1,1,1,1, 0,0,0,0,0,0,0 }
                            },
                            infiltration = new { ach_cooling = 0.2, ach_heating = 1.0 },
                            return_plenum_fraction = 0
                        });
                    }
                    tr.Commit();
                }

                // 6. 전체 JSON
                var root = new
                {
                    project = new
                    {
                        name = Path.GetFileNameWithoutExtension(fullPath),
                        designer = "AutoCAD plugin",
                        date = DateTime.Now.ToString("yyyy-MM-dd")
                    },
                    location = new
                    {
                        city = "Seoul",
                        latitude_deg = 37.5,
                        longitude_deg = 127.0,
                        elevation_m = 38,
                        lsm_deg = 135.0,
                        ground_reflectivity = 0.20
                    },
                    climate = new
                    {
                        design_db_C = 31.4,
                        design_wb_C = 25.7,
                        daily_range_C = 8.0,
                        tau_b_monthly = new Dictionary<string, double> { ["7"] = 0.510 },
                        tau_d_monthly = new Dictionary<string, double> { ["7"] = 2.080 }
                    },
                    indoor_design = new
                    {
                        cooling_db_C = 26.0,
                        cooling_rh_pct = 50,
                        heating_db_C = 20.0
                    },
                    rooms = rooms
                };

                var opts = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = System.Text.Json.JsonSerializer.Serialize(root, opts);
                File.WriteAllText(jsonPath, json, new System.Text.UTF8Encoding(false));

                MessageBox.Show($"{jsonPath} 저장 완료!");
                ed.WriteMessage($"\n{jsonPath} \n저장 완료!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        private class RtsEdgeMeta
        {
            public bool IsOuterWall;
            public List<double> WindowAreas = new List<double>();
            public List<double> DoorAreas = new List<double>();
        }

        private static double RtsSignedArea(List<(double x, double y)> verts)
        {
            double s = 0; int n = verts.Count;
            for (int i = 0; i < n; i++)
            {
                var a = verts[i];
                var b = verts[(i + 1) % n];
                s += a.x * b.y - b.x * a.y;
            }
            return s * 0.5;
        }

        private RtsEdgeMeta BuildRtsEdgeMeta(Line line, double floorHeight)
        {
            var meta = new RtsEdgeMeta();
            // OutWall 레이어 검사 (RoomPart.GetWallText 와 동일 패턴)
            var midPt = line.GetPointAtDist(line.Length / 2.0);
            var outs = SelSet.GetEntitys(midPt,
                JSelFilter.MakeFilterTypesLayer("LINE,LWPOLYLINE", Jdf.Layer.OutWall), 400);
            meta.IsOuterWall = outs != null && outs.Count > 0;

            // Window/Door 블록
            var brs = SelSet.GetEntitys(line, JSelFilter.MakeFilterTypesRegs("INSERT", "Door,Window"))?
                .OfType<Entity>().Select(x => x as BlockReference).ToList();
            if (brs == null) return meta;

            foreach (var br in brs)
            {
                if (br == null) continue;
                var brpoly = br.GetPoly1();
                double blockLen_mm = line.GetDistFromPolyIntersect(brpoly);
                if (blockLen_mm <= 0) continue;
                double blockLen_m = blockLen_mm / 1000.0;

                var bhStr = JXdata.GetXdata(br, "Height");
                double bh = 0;
                double.TryParse(bhStr, out bh);
                if (bh <= 0) bh = floorHeight;
                double area = blockLen_m * bh;

                if (JXdata.GetXdata(br, "Window") != null)
                    meta.WindowAreas.Add(area);
                else if (JXdata.GetXdata(br, "Door") != null)
                    meta.DoorAreas.Add(area);
            }
            return meta;
        }
    }
}
