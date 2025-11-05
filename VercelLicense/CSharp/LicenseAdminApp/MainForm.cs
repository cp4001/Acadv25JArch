namespace LicenseAdminApp
{
    /// <summary>
    /// 메인 폼 - 라이선스 관리
    /// </summary>
    public partial class MainForm : Form
    {
        private List<VercelApiClient.LicenseInfo> allLicenses = new();
        private BindingSource bindingSource = new();

        public MainForm()
        {
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // DataGridView 설정
            dataGridView1.DataSource = bindingSource;
            dataGridView1.AutoGenerateColumns = true;
            
            // 자동 로드
            await LoadLicensesAsync();
        }

        private async Task LoadLicensesAsync()
        {
            try
            {
                SetStatus("Loading...");
                btnRefresh.Enabled = false;

                allLicenses = await VercelApiClient.ListAllLicensesAsync();
                
                // BindingSource에 데이터 설정
                bindingSource.DataSource = allLicenses;
                
                // 컬럼 설정
                ConfigureDataGridColumns();

                SetStatus("Ready");
                lblCount.Text = $"{allLicenses.Count} licenses";
            }
            catch (Exception ex)
            {
                SetStatus("Error!");
                MessageBox.Show($"라이선스 목록을 불러오는데 실패했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private void ConfigureDataGridColumns()
        {
            // 먼저 모든 컬럼 확인 (디버깅용)
            System.Diagnostics.Debug.WriteLine("=== Available Columns ===");
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                System.Diagnostics.Debug.WriteLine($"Column: {col.Name}");
            }

            // 원본 속성 컬럼 숨기기 (표시용 속성만 보이게)
            var columnsToHide = new[] { 
                "RegisteredAt", "ExpiresAt", "UpdatedAt", 
                "Valid", "DaysRemaining", 
                "Product", "Username"  // 원본 속성 숨김
            };
            
            foreach (var colName in columnsToHide)
            {
                if (dataGridView1.Columns[colName] != null)
                {
                    dataGridView1.Columns[colName].Visible = false;
                }
            }

            // 컬럼 순서 및 헤더 설정
            int colIndex = 0;
            
            if (dataGridView1.Columns["Id"] != null)
            {
                dataGridView1.Columns["Id"].DisplayIndex = colIndex++;
                dataGridView1.Columns["Id"].HeaderText = "Machine ID";
                dataGridView1.Columns["Id"].Width = 200;
            }

            if (dataGridView1.Columns["ProductName"] != null)
            {
                dataGridView1.Columns["ProductName"].DisplayIndex = colIndex++;
                dataGridView1.Columns["ProductName"].HeaderText = "제품";
                dataGridView1.Columns["ProductName"].Width = 120;
            }

            if (dataGridView1.Columns["UserName"] != null)
            {
                dataGridView1.Columns["UserName"].DisplayIndex = colIndex++;
                dataGridView1.Columns["UserName"].HeaderText = "사용자";
                dataGridView1.Columns["UserName"].Width = 120;
            }

            if (dataGridView1.Columns["Status"] != null)
            {
                dataGridView1.Columns["Status"].DisplayIndex = colIndex++;
                dataGridView1.Columns["Status"].HeaderText = "상태";
                dataGridView1.Columns["Status"].Width = 100;
            }

            if (dataGridView1.Columns["ExpiryDate"] != null)
            {
                dataGridView1.Columns["ExpiryDate"].DisplayIndex = colIndex++;
                dataGridView1.Columns["ExpiryDate"].HeaderText = "만료일";
                dataGridView1.Columns["ExpiryDate"].Width = 120;
            }

            if (dataGridView1.Columns["DaysLeft"] != null)
            {
                dataGridView1.Columns["DaysLeft"].DisplayIndex = colIndex++;
                dataGridView1.Columns["DaysLeft"].HeaderText = "남은 기간";
                dataGridView1.Columns["DaysLeft"].Width = 100;
            }

            if (dataGridView1.Columns["RegisteredDate"] != null)
            {
                dataGridView1.Columns["RegisteredDate"].DisplayIndex = colIndex++;
                dataGridView1.Columns["RegisteredDate"].HeaderText = "등록일";
                dataGridView1.Columns["RegisteredDate"].Width = 150;
            }

            if (dataGridView1.Columns["UpdatedDate"] != null)
            {
                dataGridView1.Columns["UpdatedDate"].DisplayIndex = colIndex++;
                dataGridView1.Columns["UpdatedDate"].HeaderText = "수정일";
                dataGridView1.Columns["UpdatedDate"].Width = 150;
            }

            // 자동 크기 조정 비활성화 (고정 너비 사용)
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadLicensesAsync();
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new AddEditDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetStatus("추가 중...");
                        
                        await VercelApiClient.RegisterLicenseAsync(
                            dialog.MachineId,
                            dialog.Product,
                            dialog.Username,
                            dialog.ExpiryDate);
                        
                        MessageBox.Show("라이선스가 추가되었습니다!", 
                            "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        await LoadLicensesAsync();
                    }
                    catch (Exception ex)
                    {
                        SetStatus("Error!");
                        MessageBox.Show($"라이선스 추가 실패:\n{ex.Message}", 
                            "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("수정할 라이선스를 선택해주세요.", 
                    "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedLicense = (VercelApiClient.LicenseInfo)dataGridView1.SelectedRows[0].DataBoundItem;

            using (var dialog = new AddEditDialog(
                selectedLicense.Id, 
                selectedLicense.Product,
                selectedLicense.Username,
                selectedLicense.ExpiresAt))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetStatus("수정 중...");
                        
                        await VercelApiClient.UpdateLicenseAsync(
                            selectedLicense.Id,
                            dialog.MachineId,
                            dialog.Product,
                            dialog.Username,
                            dialog.ExpiryDate);
                        
                        MessageBox.Show("라이선스가 수정되었습니다!", 
                            "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        await LoadLicensesAsync();
                    }
                    catch (Exception ex)
                    {
                        SetStatus("Error!");
                        MessageBox.Show($"라이선스 수정 실패:\n{ex.Message}", 
                            "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("삭제할 라이선스를 선택해주세요.", 
                    "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedLicense = (VercelApiClient.LicenseInfo)dataGridView1.SelectedRows[0].DataBoundItem;

            var result = MessageBox.Show(
                $"정말로 이 라이선스를 삭제하시겠습니까?\n\nMachine ID: {selectedLicense.Id}", 
                "삭제 확인", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    SetStatus("삭제 중...");
                    
                    await VercelApiClient.DeleteLicenseAsync(selectedLicense.Id);
                    
                    MessageBox.Show("라이선스가 삭제되었습니다!", 
                        "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    await LoadLicensesAsync();
                }
                catch (Exception ex)
                {
                    SetStatus("Error!");
                    MessageBox.Show($"라이선스 삭제 실패:\n{ex.Message}", 
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                bindingSource.DataSource = allLicenses;
            }
            else
            {
                var filtered = allLicenses.Where(l => 
                    l.Id.ToLower().Contains(searchText) ||
                    (l.Product != null && l.Product.ToLower().Contains(searchText)) ||
                    (l.Username != null && l.Username.ToLower().Contains(searchText))).ToList();
                
                bindingSource.DataSource = filtered;
            }
            
            bindingSource.ResetBindings(false);
            lblCount.Text = $"{((List<VercelApiClient.LicenseInfo>)bindingSource.DataSource).Count} / {allLicenses.Count} licenses";
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*";
                    saveFileDialog.FileName = $"licenses_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var lines = new List<string>
                        {
                            "Machine ID,Product,Username,Status,Registered,Expires,Days Left,Updated"
                        };

                        foreach (var license in allLicenses)
                        {
                            lines.Add($"\"{license.Id}\"," +
                                     $"\"{license.ProductName}\"," +
                                     $"\"{license.UserName}\"," +
                                     $"\"{license.Status}\"," +
                                     $"\"{license.RegisteredDate}\"," +
                                     $"\"{license.ExpiryDate}\"," +
                                     $"\"{license.DaysLeft}\"," +
                                     $"\"{license.UpdatedDate}\"");
                        }

                        File.WriteAllLines(saveFileDialog.FileName, lines, System.Text.Encoding.UTF8);
                        
                        MessageBox.Show($"{allLicenses.Count}개의 라이선스를 내보냈습니다:\n{saveFileDialog.FileName}", 
                            "내보내기 완료", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"내보내기 실패:\n{ex.Message}", 
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetStatus(string message)
        {
            lblStatus.Text = message;
            statusStrip1.Refresh();
        }
    }
}
