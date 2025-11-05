namespace LicenseAdmin
{
    partial class AddEditDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblMachineId = new System.Windows.Forms.Label();
            this.txtMachineId = new System.Windows.Forms.TextBox();
            this.lblExpiryDate = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkNoExpiry = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lblMachineId
            // 
            this.lblMachineId.AutoSize = true;
            this.lblMachineId.Location = new System.Drawing.Point(20, 30);
            this.lblMachineId.Name = "lblMachineId";
            this.lblMachineId.Size = new System.Drawing.Size(91, 20);
            this.lblMachineId.TabIndex = 0;
            this.lblMachineId.Text = "Machine ID:";
            // 
            // txtMachineId
            // 
            this.txtMachineId.Location = new System.Drawing.Point(20, 53);
            this.txtMachineId.Name = "txtMachineId";
            this.txtMachineId.Size = new System.Drawing.Size(360, 27);
            this.txtMachineId.TabIndex = 1;
            // 
            // lblExpiryDate
            // 
            this.lblExpiryDate.AutoSize = true;
            this.lblExpiryDate.Location = new System.Drawing.Point(20, 100);
            this.lblExpiryDate.Name = "lblExpiryDate";
            this.lblExpiryDate.Size = new System.Drawing.Size(91, 20);
            this.lblExpiryDate.TabIndex = 2;
            this.lblExpiryDate.Text = "Expiry Date:";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Location = new System.Drawing.Point(20, 123);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(250, 27);
            this.dateTimePicker1.TabIndex = 3;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(150, 200);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 35);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(256, 200);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // chkNoExpiry
            // 
            this.chkNoExpiry.AutoSize = true;
            this.chkNoExpiry.Location = new System.Drawing.Point(20, 160);
            this.chkNoExpiry.Name = "chkNoExpiry";
            this.chkNoExpiry.Size = new System.Drawing.Size(98, 24);
            this.chkNoExpiry.TabIndex = 6;
            this.chkNoExpiry.Text = "No Expiry";
            this.chkNoExpiry.UseVisualStyleBackColor = true;
            this.chkNoExpiry.CheckedChanged += new System.EventHandler(this.chkNoExpiry_CheckedChanged);
            // 
            // AddEditDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(400, 260);
            this.Controls.Add(this.chkNoExpiry);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.dateTimePicker1);
            this.Controls.Add(this.lblExpiryDate);
            this.Controls.Add(this.txtMachineId);
            this.Controls.Add(this.lblMachineId);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddEditDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add/Edit License";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label lblMachineId;
        private TextBox txtMachineId;
        private Label lblExpiryDate;
        private DateTimePicker dateTimePicker1;
        private Button btnOK;
        private Button btnCancel;
        private CheckBox chkNoExpiry;
    }
}
