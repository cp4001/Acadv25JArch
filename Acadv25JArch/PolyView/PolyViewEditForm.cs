using System.Windows.Forms;

namespace Acadv25JArch.PolyView
{
    /// <summary>PolyView 이름/내용 편집 모달.</summary>
    public partial class PolyViewEditForm : Form
    {
        public string PolyViewName => txtName.Text.Trim();
        public string Memo => txtMemo.Text.Trim();

        public PolyViewEditForm(string name, string memo)
        {
            InitializeComponent();

            txtName.MaxLength = PolyViewStore.MaxNameLen;
            txtMemo.MaxLength = PolyViewStore.MaxMemoLen;
            txtName.Text = name;
            txtMemo.Text = memo;

            UpdateCount();
            txtMemo.TextChanged += (s, e) => UpdateCount();
            btnOk.Click += BtnOk_Click;
        }

        private void UpdateCount()
        {
            lblCount.Text = $"{txtMemo.TextLength} / {PolyViewStore.MaxMemoLen}";
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (PolyViewName.Length == 0)
            {
                MessageBox.Show("이름은 비워 둘 수 없습니다.", "PolyView",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                txtName.Focus();
            }
        }
    }
}
