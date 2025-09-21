using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            public const string Block = "!Arch_Block"; // Window Door 
        }

        public static class Cmd
        {
            public const string mmdl = "mmdl"; //Cmd_mmdl_GroupLinesBySlopeAndMiddleLine()
            public const string mdl  = "mdl";   //Cmd_mdl_GroupLinesBySlopeAndMiddleLine800()

        }
    }
}
