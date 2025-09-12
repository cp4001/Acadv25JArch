using Autodesk.AutoCAD.DatabaseServices;
using Maroquio;
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

            if (blockparts == null)
            {
                MessageBox.Show(" 대상을 선택 하세요");
                return;
            }
            SortableBindingList <BlockPart>
                      drbl = new SortableBindingList<BlockPart>(blockparts);
            bs.DataSource = drbl;

            dgvBlock.DataSource = bs;
        }
    }


}
