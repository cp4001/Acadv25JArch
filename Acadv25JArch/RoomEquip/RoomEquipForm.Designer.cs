namespace PipeLoad2
{
    partial class RoomEquipForm
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
            tabMain = new TabControl();
            tabRoom = new TabPage();
            dgvRoom = new DataGridView();
            colNo = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            colHeat = new DataGridViewTextBoxColumn();
            colCool = new DataGridViewTextBoxColumn();
            colRoomQ = new DataGridViewTextBoxColumn();
            colEtc = new DataGridViewTextBoxColumn();
            pnlRoomTop = new Panel();
            btnSave = new Button();
            tabEquip = new TabPage();
            dgvEquip = new DataGridView();
            colEqRoom = new DataGridViewTextBoxColumn();
            colEqKind = new DataGridViewTextBoxColumn();
            colEqType = new DataGridViewTextBoxColumn();
            colEqSys = new DataGridViewTextBoxColumn();
            colEqCfm = new DataGridViewTextBoxColumn();
            colEqCount = new DataGridViewTextBoxColumn();
            pnlEquipTop = new Panel();
            lblEqRoom = new Label();
            cmbEqRoom = new ComboBox();
            lblEqKind = new Label();
            txtEqKind = new TextBox();
            lblEqType = new Label();
            cmbEqType = new ComboBox();
            lblEqSys = new Label();
            cmbEqSys = new ComboBox();
            lblEqCfm = new Label();
            txtEqCfm = new TextBox();
            lblEqCount = new Label();
            numEqCount = new NumericUpDown();
            btnEqAdd = new Button();
            tabMain.SuspendLayout();
            tabRoom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRoom).BeginInit();
            pnlRoomTop.SuspendLayout();
            tabEquip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvEquip).BeginInit();
            pnlEquipTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numEqCount).BeginInit();
            SuspendLayout();
            // 
            // tabMain
            // 
            tabMain.Controls.Add(tabRoom);
            tabMain.Controls.Add(tabEquip);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Location = new Point(0, 0);
            tabMain.Margin = new Padding(4, 5, 4, 5);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(1120, 768);
            tabMain.TabIndex = 0;
            // 
            // tabRoom
            // 
            tabRoom.Controls.Add(dgvRoom);
            tabRoom.Controls.Add(pnlRoomTop);
            tabRoom.Location = new Point(4, 34);
            tabRoom.Margin = new Padding(4, 5, 4, 5);
            tabRoom.Name = "tabRoom";
            tabRoom.Padding = new Padding(4, 5, 4, 5);
            tabRoom.Size = new Size(1112, 730);
            tabRoom.TabIndex = 0;
            tabRoom.Text = "실";
            tabRoom.UseVisualStyleBackColor = true;
            // 
            // dgvRoom
            // 
            dgvRoom.AllowUserToAddRows = false;
            dgvRoom.AllowUserToDeleteRows = false;
            dgvRoom.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRoom.Columns.AddRange(new DataGridViewColumn[] { colNo, colName, colHeat, colCool, colRoomQ, colEtc });
            dgvRoom.Dock = DockStyle.Fill;
            dgvRoom.Location = new Point(4, 65);
            dgvRoom.Margin = new Padding(4, 5, 4, 5);
            dgvRoom.Name = "dgvRoom";
            dgvRoom.RowHeadersVisible = false;
            dgvRoom.RowHeadersWidth = 62;
            dgvRoom.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvRoom.Size = new Size(1104, 660);
            dgvRoom.TabIndex = 0;
            // 
            // colNo
            // 
            colNo.HeaderText = "Room No.";
            colNo.MinimumWidth = 8;
            colNo.Name = "colNo";
            colNo.ReadOnly = true;
            colNo.Width = 80;
            // 
            // colName
            // 
            colName.HeaderText = "실명";
            colName.MinimumWidth = 8;
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.Width = 180;
            // 
            // colHeat
            // 
            colHeat.HeaderText = "난방부하";
            colHeat.MinimumWidth = 8;
            colHeat.Name = "colHeat";
            colHeat.Width = 110;
            // 
            // colCool
            // 
            colCool.HeaderText = "냉방부하";
            colCool.MinimumWidth = 8;
            colCool.Name = "colCool";
            colCool.Width = 110;
            // 
            // colRoomQ
            // 
            colRoomQ.HeaderText = "총풍량";
            colRoomQ.MinimumWidth = 8;
            colRoomQ.Name = "colRoomQ";
            colRoomQ.Width = 110;
            // 
            // colEtc
            // 
            colEtc.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colEtc.HeaderText = "비고";
            colEtc.MinimumWidth = 8;
            colEtc.Name = "colEtc";
            // 
            // pnlRoomTop
            // 
            pnlRoomTop.Controls.Add(btnSave);
            pnlRoomTop.Dock = DockStyle.Top;
            pnlRoomTop.Location = new Point(4, 5);
            pnlRoomTop.Margin = new Padding(4, 5, 4, 5);
            pnlRoomTop.Name = "pnlRoomTop";
            pnlRoomTop.Size = new Size(1104, 60);
            pnlRoomTop.TabIndex = 1;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(909, 10);
            btnSave.Margin = new Padding(4, 5, 4, 5);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(129, 45);
            btnSave.TabIndex = 0;
            btnSave.Text = "저장";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // tabEquip
            //
            tabEquip.Controls.Add(dgvEquip);
            tabEquip.Controls.Add(pnlEquipTop);
            tabEquip.Location = new Point(4, 34);
            tabEquip.Margin = new Padding(4, 5, 4, 5);
            tabEquip.Name = "tabEquip";
            tabEquip.Padding = new Padding(4, 5, 4, 5);
            tabEquip.Size = new Size(1112, 730);
            tabEquip.TabIndex = 1;
            tabEquip.Text = "장비";
            tabEquip.UseVisualStyleBackColor = true;
            //
            // pnlEquipTop
            //
            pnlEquipTop.Controls.Add(lblEqRoom);
            pnlEquipTop.Controls.Add(cmbEqRoom);
            pnlEquipTop.Controls.Add(lblEqKind);
            pnlEquipTop.Controls.Add(txtEqKind);
            pnlEquipTop.Controls.Add(lblEqType);
            pnlEquipTop.Controls.Add(cmbEqType);
            pnlEquipTop.Controls.Add(lblEqSys);
            pnlEquipTop.Controls.Add(cmbEqSys);
            pnlEquipTop.Controls.Add(lblEqCfm);
            pnlEquipTop.Controls.Add(txtEqCfm);
            pnlEquipTop.Controls.Add(lblEqCount);
            pnlEquipTop.Controls.Add(numEqCount);
            pnlEquipTop.Controls.Add(btnEqAdd);
            pnlEquipTop.Dock = DockStyle.Top;
            pnlEquipTop.Location = new Point(4, 5);
            pnlEquipTop.Margin = new Padding(4, 5, 4, 5);
            pnlEquipTop.Name = "pnlEquipTop";
            pnlEquipTop.Size = new Size(1104, 100);
            pnlEquipTop.TabIndex = 1;
            //
            // lblEqRoom
            //
            lblEqRoom.AutoSize = true;
            lblEqRoom.Location = new Point(10, 15);
            lblEqRoom.Name = "lblEqRoom";
            lblEqRoom.Text = "실명";
            //
            // cmbEqRoom
            //
            cmbEqRoom.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEqRoom.Location = new Point(10, 50);
            cmbEqRoom.Name = "cmbEqRoom";
            cmbEqRoom.Size = new Size(220, 33);
            cmbEqRoom.TabIndex = 0;
            //
            // lblEqKind
            //
            lblEqKind.AutoSize = true;
            lblEqKind.Location = new Point(245, 15);
            lblEqKind.Name = "lblEqKind";
            lblEqKind.Text = "장비 종류";
            //
            // txtEqKind
            //
            txtEqKind.Location = new Point(245, 50);
            txtEqKind.Name = "txtEqKind";
            txtEqKind.ReadOnly = true;
            txtEqKind.Size = new Size(110, 31);
            txtEqKind.TabStop = false;
            txtEqKind.Text = "디퓨져";
            //
            // lblEqType
            //
            lblEqType.AutoSize = true;
            lblEqType.Location = new Point(370, 15);
            lblEqType.Name = "lblEqType";
            lblEqType.Text = "설비 타입";
            //
            // cmbEqType
            //
            cmbEqType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEqType.Location = new Point(370, 50);
            cmbEqType.Name = "cmbEqType";
            cmbEqType.Size = new Size(110, 33);
            cmbEqType.TabIndex = 1;
            //
            // lblEqSys
            //
            lblEqSys.AutoSize = true;
            lblEqSys.Location = new Point(495, 15);
            lblEqSys.Name = "lblEqSys";
            lblEqSys.Text = "시스템 타입";
            //
            // cmbEqSys
            //
            cmbEqSys.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEqSys.Location = new Point(495, 50);
            cmbEqSys.Name = "cmbEqSys";
            cmbEqSys.Size = new Size(110, 33);
            cmbEqSys.TabIndex = 2;
            //
            // lblEqCfm
            //
            lblEqCfm.AutoSize = true;
            lblEqCfm.Location = new Point(620, 15);
            lblEqCfm.Name = "lblEqCfm";
            lblEqCfm.Text = "풍량";
            //
            // txtEqCfm
            //
            txtEqCfm.Location = new Point(620, 50);
            txtEqCfm.Name = "txtEqCfm";
            txtEqCfm.Size = new Size(120, 31);
            txtEqCfm.TabIndex = 3;
            //
            // lblEqCount
            //
            lblEqCount.AutoSize = true;
            lblEqCount.Location = new Point(755, 15);
            lblEqCount.Name = "lblEqCount";
            lblEqCount.Text = "개수";
            //
            // numEqCount
            //
            numEqCount.Location = new Point(755, 50);
            numEqCount.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            numEqCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numEqCount.Name = "numEqCount";
            numEqCount.Size = new Size(90, 31);
            numEqCount.TabIndex = 4;
            numEqCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // btnEqAdd
            //
            btnEqAdd.Location = new Point(909, 42);
            btnEqAdd.Margin = new Padding(4, 5, 4, 5);
            btnEqAdd.Name = "btnEqAdd";
            btnEqAdd.Size = new Size(129, 45);
            btnEqAdd.TabIndex = 5;
            btnEqAdd.Text = "+ADD";
            btnEqAdd.UseVisualStyleBackColor = true;
            btnEqAdd.Click += btnEqAdd_Click;
            //
            // dgvEquip
            //
            dgvEquip.AllowUserToAddRows = false;
            dgvEquip.AllowUserToDeleteRows = false;
            dgvEquip.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvEquip.Columns.AddRange(new DataGridViewColumn[] { colEqRoom, colEqKind, colEqType, colEqSys, colEqCfm, colEqCount });
            dgvEquip.Dock = DockStyle.Fill;
            dgvEquip.Location = new Point(4, 105);
            dgvEquip.Margin = new Padding(4, 5, 4, 5);
            dgvEquip.Name = "dgvEquip";
            dgvEquip.ReadOnly = true;
            dgvEquip.RowHeadersVisible = false;
            dgvEquip.RowHeadersWidth = 62;
            dgvEquip.Size = new Size(1104, 620);
            dgvEquip.TabIndex = 0;
            //
            // colEqRoom
            //
            colEqRoom.HeaderText = "실명";
            colEqRoom.MinimumWidth = 8;
            colEqRoom.Name = "colEqRoom";
            colEqRoom.Width = 180;
            //
            // colEqKind
            //
            colEqKind.HeaderText = "장비 종류";
            colEqKind.MinimumWidth = 8;
            colEqKind.Name = "colEqKind";
            colEqKind.Width = 110;
            //
            // colEqType
            //
            colEqType.HeaderText = "설비 타입";
            colEqType.MinimumWidth = 8;
            colEqType.Name = "colEqType";
            colEqType.Width = 110;
            //
            // colEqSys
            //
            colEqSys.HeaderText = "시스템 타입";
            colEqSys.MinimumWidth = 8;
            colEqSys.Name = "colEqSys";
            colEqSys.Width = 120;
            //
            // colEqCfm
            //
            colEqCfm.HeaderText = "풍량";
            colEqCfm.MinimumWidth = 8;
            colEqCfm.Name = "colEqCfm";
            colEqCfm.Width = 110;
            //
            // colEqCount
            //
            colEqCount.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colEqCount.HeaderText = "개수";
            colEqCount.MinimumWidth = 8;
            colEqCount.Name = "colEqCount";
            //
            // RoomEquipForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1120, 768);
            Controls.Add(tabMain);
            Margin = new Padding(4, 5, 4, 5);
            Name = "RoomEquipForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "실 / 장비";
            tabMain.ResumeLayout(false);
            tabRoom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvRoom).EndInit();
            pnlRoomTop.ResumeLayout(false);
            tabEquip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvEquip).EndInit();
            pnlEquipTop.ResumeLayout(false);
            pnlEquipTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numEqCount).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabRoom;
        private System.Windows.Forms.TabPage tabEquip;
        private System.Windows.Forms.Panel pnlRoomTop;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.DataGridView dgvRoom;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHeat;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCool;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRoomQ;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEtc;
        private System.Windows.Forms.Panel pnlEquipTop;
        private System.Windows.Forms.Label lblEqRoom;
        private System.Windows.Forms.ComboBox cmbEqRoom;
        private System.Windows.Forms.Label lblEqKind;
        private System.Windows.Forms.TextBox txtEqKind;
        private System.Windows.Forms.Label lblEqType;
        private System.Windows.Forms.ComboBox cmbEqType;
        private System.Windows.Forms.Label lblEqSys;
        private System.Windows.Forms.ComboBox cmbEqSys;
        private System.Windows.Forms.Label lblEqCfm;
        private System.Windows.Forms.TextBox txtEqCfm;
        private System.Windows.Forms.Label lblEqCount;
        private System.Windows.Forms.NumericUpDown numEqCount;
        private System.Windows.Forms.Button btnEqAdd;
        private System.Windows.Forms.DataGridView dgvEquip;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEqRoom;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEqKind;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEqType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEqSys;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEqCfm;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEqCount;
    }
}
