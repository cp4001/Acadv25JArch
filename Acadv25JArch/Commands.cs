using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acadv25JArch
{
    public class Commands
    {

        [CommandMethod("Show_BlockForm")]
        public void ShowMyForm()
        {
            FormBlockPart  form = new FormBlockPart();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);
            //form.ShowDialog();
        }
    }
}
