using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
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
        public static List<ObjectId> selids = new List<ObjectId>();            // 선택된 Blocks
        public static List<BlockPart> blockparts = new List<BlockPart>();      // Group화된 BlockPart
        public static BindingSource bs = new BindingSource();
        public static BindingSource bs_room = new BindingSource();
        public static BindingSource bsNumDia = new BindingSource();

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
                string fullPath = doc.Database.Filename;

                if (string.IsNullOrEmpty(fullPath)) return;

                // 핵심: 확장자를 .dwg에서 .xlsm으로 변경
                string excelPath = Path.ChangeExtension(fullPath, ".xlsm");


                //// EPPlus 라이선스 설정 (이 줄 추가!)
                //ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                // Excel 파일 경로
                string filePath = "C:\\Jarch25\\load_Calc_org.xlsm";

                // 파일 존재 확인
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"파일이 없습니다: {filePath}");
                    return;
                }

                var roomparts = RoomPart.GetAllRoomParts();
                if (roomparts == null || roomparts.Count == 0)
                {
                    MessageBox.Show("roomparts가 없습니다.");
                    return;
                }

                // 워크북 열기
                FileInfo fileInfo = new FileInfo(filePath);
                using (var package = new ExcelPackage(fileInfo))
                {
                    // 시트 존재 확인
                    var worksheet = package.Workbook.Worksheets["부하계산"];
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
                    for (int roomcnt = 1; roomcnt <= roomparts.Count-1; roomcnt++)
                    {
                        for (int row = 1; row <= 50; row++)
                        {
                            int targetRow = row + roomcnt*50; // 51~100행

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
                        var windows = room.GetWindows();
                        windows = windows.OrderBy(x => x.StartsWith("P")).ToList(); // P로 시작하지 않는 Wall  앞으로 배치 

                        //if (walls == null || walls.Count == 0) continue;
                        int windex = 11 + rindex * 50;
                        foreach (var ww in windows) // 외창 처리 
                        {
                            // 쉼표(',')를 기준으로 자르기
                            string[] window = ww.Split(':');

                            //항목
                            worksheet.Cells[$"S{windex}"].Value = "외  창";// "NW";
                            // 방위각
                            worksheet.Cells[$"T{windex}"].Value = window[0];// "NW";
                            // 면적 (수식)
                            worksheet.Cells[$"U{windex}"].Formula = window[1];// "3*3";
                            windex++;
                        }




                        // 벽처리  외벽외창 + 외벽+내벽    
                        int waldex = 19 + rindex * 50;
                        //외벽에 걸친 외창 처리
                        foreach (var ww in windows) // 외창 처리 
                        {
                            // 쉼표(',')를 기준으로 자르기
                            string[] window = ww.Split(':');
                            // 방위각
                            worksheet.Cells[$"S{waldex}"].Value = "외  창";// "NW";
                            worksheet.Cells[$"T{waldex}"].Value = window[0];// "NW";
                            worksheet.Cells[$"U{waldex}"].Formula = window[1];// "3*3// 면적 (수식)
                            waldex++;
                        }
                        foreach (var wal in walls) 
                        {
                            //Wall 처리 
                            // 쉼표(':')를 기준으로 자르기
                            string[] wall = wal.Split(':');
                            //항목
                            worksheet.Cells[$"S{waldex}"].Value = wall[0].Contains("P") ? "내   벽" : "외   벽"; //"외벽";   
                            // 방위각
                            worksheet.Cells[$"T{waldex}"].Value = wall[0];// "NW" Or P
                            // 면적 (수식)
                            worksheet.Cells[$"U{waldex}"].Formula = wall[1];// "3*3";
                            waldex++;
                        }

                        rindex++; // 다음 room 으로 
                    }

                    // 저장
                    // 3. FileInfo 객체 생성 (EPPlus는 string 경로 대신 FileInfo를 요구함)
                    FileInfo excelFile = new FileInfo(excelPath);
                    package.SaveAs(excelFile);
                    MessageBox.Show("완료!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        private void FormBlockPart_Load(object sender, EventArgs e)
        {
        }
    }


}
