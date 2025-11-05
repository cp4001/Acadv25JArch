using System.Data;

namespace LicenseAdmin
{
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
                
                //// 컬럼 헤더 이름 설정
                //if (dataGridView1.Columns.Count > 0)
                //{
                //    dataGridView1.Columns["Id"].HeaderText = "Machine ID";
                //    dataGridView1.Columns["Valid"].HeaderText = "Valid";
                //    dataGridView1.Columns["RegisteredAt"].HeaderText = "Registered";
                //    dataGridView1.Columns["ExpiresAt"].HeaderText = "Expires";
                //    dataGridView1.Columns["UpdatedAt"].HeaderText = "Updated";
                    
                //    // 날짜 형식 지정
                //    dataGridView1.Columns["RegisteredAt"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                //    dataGridView1.Columns["ExpiresAt"].DefaultCellStyle.Format = "yyyy-MM-dd";
                //    dataGridView1.Columns["UpdatedAt"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                //}

                SetStatus($"Loaded {allLicenses.Count} licenses");
            }
            catch (Exception ex)
            {
                SetStatus("Error!");
                MessageBox.Show($"Failed to load licenses:\n{ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
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
                        SetStatus("Adding...");
                        
                        await VercelApiClient.RegisterLicenseAsync(
                            dialog.MachineId, 
                            dialog.ExpiryDate);
                        
                        MessageBox.Show("License added successfully!", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        await LoadLicensesAsync();
                    }
                    catch (Exception ex)
                    {
                        SetStatus("Error!");
                        MessageBox.Show($"Failed to add license:\n{ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a license to edit.", "Warning", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedLicense = (VercelApiClient.LicenseInfo)dataGridView1.SelectedRows[0].DataBoundItem;

            using (var dialog = new AddEditDialog(selectedLicense.Id, selectedLicense.ExpiresAt))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetStatus("Updating...");
                        
                        await VercelApiClient.UpdateLicenseAsync(
                            selectedLicense.Id,
                            dialog.MachineId,
                            dialog.ExpiryDate);
                        
                        MessageBox.Show("License updated successfully!", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        await LoadLicensesAsync();
                    }
                    catch (Exception ex)
                    {
                        SetStatus("Error!");
                        MessageBox.Show($"Failed to update license:\n{ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a license to delete.", "Warning", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedLicense = (VercelApiClient.LicenseInfo)dataGridView1.SelectedRows[0].DataBoundItem;

            var result = MessageBox.Show(
                $"Are you sure you want to delete this license?\n\nMachine ID: {selectedLicense.Id}", 
                "Confirm Delete", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    SetStatus("Deleting...");
                    
                    await VercelApiClient.DeleteLicenseAsync(selectedLicense.Id);
                    
                    MessageBox.Show("License deleted successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    await LoadLicensesAsync();
                }
                catch (Exception ex)
                {
                    SetStatus("Error!");
                    MessageBox.Show($"Failed to delete license:\n{ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                bindingSource.DataSource = allLicenses;
            }
            else
            {
                var filtered = allLicenses.Where(l => 
                    l.Id.ToLower().Contains(searchText)).ToList();
                
                bindingSource.DataSource = filtered;
            }
            
            bindingSource.ResetBindings(false);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    saveFileDialog.FileName = $"licenses_{DateTime.Now:yyyyMMdd}.csv";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var lines = new List<string>
                        {
                            "Machine ID,Valid,Registered,Expires,Updated"
                        };

                        foreach (var license in allLicenses)
                        {
                            lines.Add($"\"{license.Id}\",{license.Valid}," +
                                     $"{license.RegisteredAt:yyyy-MM-dd HH:mm}," +
                                     $"{license.ExpiresAt:yyyy-MM-dd}," +
                                     $"{license.UpdatedAt:yyyy-MM-dd HH:mm}");
                        }

                        File.WriteAllLines(saveFileDialog.FileName, lines);
                        
                        MessageBox.Show($"Exported {allLicenses.Count} licenses to:\n{saveFileDialog.FileName}", 
                            "Export Complete", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export:\n{ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetStatus(string message)
        {
            lblStatus.Text = message;
            lblStatus.Refresh();
        }
    }
}
