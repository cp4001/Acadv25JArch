using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Acadv25JArch
{
    public static class Jdf // JArch Defaults
    {
        public static class  Color
        {
            public const int ByBlock = 0;
            public const int Red = 1;       // 빨강
            public const int Yellow = 2;    // 노랑
            public const int Green = 3;     // 초록
            public const int Cyan = 4;      // 청록
            public const int Blue = 5;      // 파랑
            public const int Magenta = 6;   // 자홍
            public const int WhiteBlack = 7; // 흰색/검정 - 배경에 따라
        }
        public static class LineGroup
        {
            public const string Default = "Default";
        }
        public static class Layer
        {
            public const string Wall = "!Arch_Wall";
            public const string Room = "!Arch_Room";
            public const string RoomPoly = "!Arch_RoomPoly";
            public const string Block = "!Arch_Block"; // Window Door 
            public const string OutWall  = "!Arch_OutWall"; // 외벽지정 layer
        }

        public static class Cmd
        {
            public const string mmdl = "mmdl";                                           //Cmd_mmdl_GroupLinesBySlopeAndMiddleLine()
            public const string mdl  = "mdl";                                            //Cmd_mdl_GroupLinesBySlopeAndMiddleLine800()
            public const string 선택라인방위각 = "LineDir";                        //Cmd_LineDir_AnalyzeLineDirection()
            public const string AnalyzePolyLineDirection = "PolyDir";                    //Cmd_PolyDir_LinesDirection()

            public const string ShowBlockForm = "ShowBlockForm";                         //Cmd_ShowBlockForm()

            public const string ChangeBlockEntityLayer = "Block_Entity_layer";           //Cmd_ChangeBlockEntityLayer()
            public const string ChangeBlockAllEntityColor = "Block_Entity_ALL_color";    //Cmd_ChangeAllBlockEntityLayersToZeroWithColor()   
                                //도면내부의 전체 블럭에 대하여  Block 내부 Entity 의 Layer 를 0 으로 변경하고 Color 를 ByBlock 으로 변경

            public const string RoomLayOutWork = "aWork";                                   //Cmd_aWork_Area_Work()

            public const string 벽라인정리 = "Wall_Line_Arrange";                   //Cmd_Wall_Line_Arrange()

            public const string 선택폴리룸지정 = "To_RoomPoly";                    //Cmd_Poly_Set_RoomPoly()
            public const string 선택폴리룸계산 = "Room_Poly_Calc";                         //Cmd_RoomPoly_Calc()

            public const string 룸폴리전체계산 = "Room_Poly_ALL_Calc";                        //Cmd_RoomPoly_All_Calc()
            public const string 룸텍스트제거 = "Room_text_delete";                  //Cmd_Room_texts_Delete()

            public const string 벽센터라인선택폴리만들기 = "Wall_Cen_LINES2POLY";         // Cmd_Wall_LinesTo_ConvertClosedPolyline Cmd_LinesTo_ConvertClosedPolyline()
            public const string 라인선택안목폴리만들기 = "Net_Dim";         // Cmd_LinesTo_ConvertClosedPolyline()
            public const string 벽라인제거 = "Wall_Line_delete";      // Cmd_Wall_Lines_Delete()

            public const string 선택블럭창지정     = "To_Window";      // Cmd_Blocks_To_Window() 
            public const string 선택블럭문지정     = "To_Door";        // Cmd_Blocks_To_Door()
            public const string 선택블럭기둥지정   = "To_Column";      // Cmd_Blocks_To_Column()
            public const string 선택개체구분지정   = "To_Symbol";      // Cmd_Entitys_To_Symbol()
        }
    }
}
