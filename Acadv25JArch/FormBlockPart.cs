using Autodesk.AutoCAD.DatabaseServices;
using ClosedXML.Excel;

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
                using (var workbook = new XLWorkbook(filePath))
                {
                    // 시트 존재 확인
                    if (!workbook.Worksheets.Contains("부하계산"))
                    {
                        MessageBox.Show("'부하계산' 시트가 없습니다.");
                        return;
                    }

                    var worksheet = workbook.Worksheet("부하계산"); 

                    foreach (var room in roomparts)
                    {
                        Console.WriteLine($"Room Name: {room.Name}, Floor Area: {room.FloorArea}");

                        var walls = room.GetWalls();
                        if (walls == null || walls.Count == 0) continue;

                        foreach (var ww in walls)
                        {
                            int windex = 11;
                            //방위각
                            worksheet.Cell($"S{windex}").Value = "NW";
                            // 면적   
                            worksheet.Cell($"T{windex}").FormulaA1 = "3*3";
                        }

                        // 1~50행 범위 선택 (전체 열)
                        var sourceRange = worksheet.Range("1:50");
                        // 51행 위치로 복사
                        sourceRange.CopyTo(worksheet.Cell(51, 1));
                    }

                    // 저장
                    workbook.SaveAs(filePath);
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
