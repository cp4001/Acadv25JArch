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

        [CommandMethod(Jdf.Cmd.ShowBlockForm)]
        public void Cmd_ShowBlockForm()
        {
            FormBlockPart  form = new FormBlockPart();
            //Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(form);
            //form.ShowDialog();
        }
    }
}
