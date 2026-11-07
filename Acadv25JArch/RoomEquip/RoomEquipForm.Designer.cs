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
            tabMain = new System.Windows.Forms.TabControl();
            tabRoom = new System.Windows.Forms.TabPage();
            dgvRoom = new System.Windows.Forms.DataGridView();
            colNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHeat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCool = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colRoomQ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEtc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            tabEquip = new System.Windows.Forms.TabPage();
            pnlRoomTop = new System.Windows.Forms.Panel();
            btnSave = new System.Windows.Forms.Button();
            tabMain.SuspendLayout();
            tabRoom.SuspendLayout();
            pnlRoomTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRoom).BeginInit();
            SuspendLayout();
            //
            // tabMain
            //
            tabMain.Controls.Add(tabRoom);
            tabMain.Controls.Add(tabEquip);
            tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            tabMain.Location = new System.Drawing.Point(0, 0);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new System.Drawing.Size(784, 461);
            tabMain.TabIndex = 0;
            //
            // tabRoom
            //
            tabRoom.Controls.Add(dgvRoom);
            tabRoom.Controls.Add(pnlRoomTop);
            tabRoom.Location = new System.Drawing.Point(4, 24);
            tabRoom.Name = "tabRoom";
            tabRoom.Padding = new System.Windows.Forms.Padding(3);
            tabRoom.Size = new System.Drawing.Size(776, 433);
            tabRoom.TabIndex = 0;
            tabRoom.Text = "실";
            tabRoom.UseVisualStyleBackColor = true;
            //
            // dgvRoom
            //
            // pnlRoomTop
            pnlRoomTop.Controls.Add(btnSave);
            pnlRoomTop.Dock = System.Windows.Forms.DockStyle.Top;
            pnlRoomTop.Location = new System.Drawing.Point(3, 3);
            pnlRoomTop.Name = "pnlRoomTop";
            pnlRoomTop.Size = new System.Drawing.Size(770, 36);
            pnlRoomTop.TabIndex = 1;
            // btnSave
            btnSave.Location = new System.Drawing.Point(3, 5);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(90, 27);
            btnSave.TabIndex = 0;
            btnSave.Text = "저장";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // dgvRoom
            dgvRoom.AllowUserToAddRows = false;
            dgvRoom.AllowUserToDeleteRows = false;
            dgvRoom.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRoom.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colNo, colName, colHeat, colCool, colRoomQ, colEtc });
            dgvRoom.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvRoom.Location = new System.Drawing.Point(3, 3);
            dgvRoom.Name = "dgvRoom";
            dgvRoom.RowHeadersVisible = false;
            dgvRoom.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            dgvRoom.Size = new System.Drawing.Size(770, 391);
            dgvRoom.TabIndex = 0;
            //
            // colNo
            //
            colNo.HeaderText = "Room No.";
            colNo.Name = "colNo";
            colNo.ReadOnly = true;
            colNo.Width = 80;
            //
            // colName
            //
            colName.HeaderText = "실명";
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.Width = 180;
            //
            // colHeat
            //
            colHeat.HeaderText = "난방부하";
            colHeat.Name = "colHeat";
            colHeat.Width = 110;
            //
            // colCool
            //
            colCool.HeaderText = "냉방부하";
            colCool.Name = "colCool";
            colCool.Width = 110;
            //
            // colRoomQ
            //
            colRoomQ.HeaderText = "총풍량";
            colRoomQ.Name = "colRoomQ";
            colRoomQ.Width = 110;
            //
            // colEtc
            //
            colEtc.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            colEtc.HeaderText = "비고";
            colEtc.Name = "colEtc";
            //
            // tabEquip
            //
            tabEquip.Location = new System.Drawing.Point(4, 24);
            tabEquip.Name = "tabEquip";
            tabEquip.Padding = new System.Windows.Forms.Padding(3);
            tabEquip.Size = new System.Drawing.Size(776, 433);
            tabEquip.TabIndex = 1;
            tabEquip.Text = "장비";
            tabEquip.UseVisualStyleBackColor = true;
            //
            // RoomEquipForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(784, 461);
            Controls.Add(tabMain);
            Name = "RoomEquipForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "실 / 장비";
            tabMain.ResumeLayout(false);
            tabRoom.ResumeLayout(false);
            pnlRoomTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvRoom).EndInit();
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
    }
}
