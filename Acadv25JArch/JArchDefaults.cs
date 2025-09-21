using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acadv25JArch
{
    public static class Jdf // JArch Defaults
    {
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


        }
    }
}
