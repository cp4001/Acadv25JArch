using System;
using System.Windows.Forms;

namespace LicenseAdmin
{
    public partial class AddEditDialog : Form
    {
        public string MachineId { get; private set; } = "";
        public DateTime? ExpiryDate { get; private set; }

        // 생성자 - 신규 추가
        public AddEditDialog()
        {
            InitializeComponent();
            this.Text = "Add License";
            dateTimePicker1.Value = DateTime.Now.AddYears(1);
        }

        // 생성자 - 수정
        public AddEditDialog(string machineId, DateTime? expiresAt) : this()
        {
            this.Text = "Edit License";
            txtMachineId.Text = machineId;
            
            if (expiresAt.HasValue)
            {
                dateTimePicker1.Value = expiresAt.Value;
                chkNoExpiry.Checked = false;
            }
            else
            {
                chkNoExpiry.Checked = true;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // 입력 검증
            if (string.IsNullOrWhiteSpace(txtMachineId.Text))
            {
                MessageBox.Show("Please enter a Machine ID.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMachineId.Focus();
                return;
            }

            MachineId = txtMachineId.Text.Trim();
            ExpiryDate = chkNoExpiry.Checked ? null : dateTimePicker1.Value.Date;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void chkNoExpiry_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePicker1.Enabled = !chkNoExpiry.Checked;
        }
    }
}
