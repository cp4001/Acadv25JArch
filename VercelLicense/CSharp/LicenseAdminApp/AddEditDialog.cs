namespace LicenseAdminApp
{
    /// <summary>
    /// 라이선스 추가/수정 대화상자
    /// </summary>
    public partial class AddEditDialog : Form
    {
        public string MachineId => txtMachineId.Text.Trim();
        public DateTime? ExpiryDate => chkNoExpiry.Checked ? null : dtpExpiryDate.Value.Date;

        public AddEditDialog() : this(null, null)
        {
        }

        public AddEditDialog(string? machineId, DateTime? expiryDate)
        {
            InitializeComponent();

            // 편집 모드
            if (!string.IsNullOrEmpty(machineId))
            {
                this.Text = "라이선스 수정";
                txtMachineId.Text = machineId;
                
                if (expiryDate.HasValue)
                {
                    dtpExpiryDate.Value = expiryDate.Value;
                    chkNoExpiry.Checked = false;
                }
                else
                {
                    chkNoExpiry.Checked = true;
                }
            }
            else
            {
                // 추가 모드
                this.Text = "라이선스 추가";
                dtpExpiryDate.Value = DateTime.Now.AddYears(1);
                chkNoExpiry.Checked = false;
            }
        }

        private void chkNoExpiry_CheckedChanged(object sender, EventArgs e)
        {
            dtpExpiryDate.Enabled = !chkNoExpiry.Checked;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // 유효성 검사
            if (string.IsNullOrWhiteSpace(txtMachineId.Text))
            {
                MessageBox.Show("Machine ID를 입력해주세요.", 
                    "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMachineId.Focus();
                return;
            }

            // Machine ID 길이 체크 (최소 5자)
            if (txtMachineId.Text.Trim().Length < 5)
            {
                MessageBox.Show("Machine ID는 최소 5자 이상이어야 합니다.", 
                    "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMachineId.Focus();
                return;
            }

            // 만료일 검사 (과거 날짜 체크)
            if (!chkNoExpiry.Checked && dtpExpiryDate.Value.Date < DateTime.Now.Date)
            {
                var result = MessageBox.Show(
                    "만료일이 과거입니다. 계속하시겠습니까?", 
                    "확인", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.No)
                {
                    dtpExpiryDate.Focus();
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
