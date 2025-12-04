using Autodesk.AutoCAD.DatabaseServices;
using ClosedXML.Excel;

//using Maroquio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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
            // Excel 파일 경로
            string filePath = "C:\\Jarch25\\load_Calc_org.xlsm";

            var roomparts = RoomPart.GetAllRoomParts();



            // 워크북 열기
            using (var workbook = new XLWorkbook(filePath))
            {
                foreach (var room in roomparts)
                {
                    Console.WriteLine($"Room Name: {room.Name}, Floor Area: {room.FloorArea}");

                    // Sheet1 가져오기
                    var worksheet = workbook.Worksheet("부하계산");

                    // 외창  S11에 문자열 방위각  입력
                    foreach (var ww in room.GetWalls())
                    {
                        int windex = 11;
                        //방위각
                        worksheet.Cell($"S{windex.ToString()}").Value = "NW";
                        // 면적   
                        worksheet.Cell($"T{windex.ToString()}").FormulaA1 = "NW";
                    }



                    // 1~50행 범위 선택 (전체 열)
                    var sourceRange = worksheet.Range("1:50");

                    // 51행 위치로 복사
                    sourceRange.CopyTo(worksheet.Cell(51, 1));

                }



                // 저장 (같은 파일 또는 다른 이름)
                workbook.SaveAs(filePath);
                // workbook.SaveAs("파일1.xlsm"); // 다른 이름으로 저장 시

                Console.WriteLine("복사 완료!");
            }


        }

        private void FormBlockPart_Load(object sender, EventArgs e)
        {

        }
    }


}
