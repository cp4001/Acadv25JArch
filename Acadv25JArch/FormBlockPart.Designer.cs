namespace Acadv25JArch
{
    partial class FormBlockPart
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            btnAllBlocks = new Button();
            dgvBlock = new DataGridView();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            dgvRoom = new DataGridView();
            btnRooms = new Button();
            tabPage3 = new TabPage();
            tabPage4 = new TabPage();
            dgvWindow = new DataGridView();
            btnWindows = new Button();
            dgvDoor = new DataGridView();
            btnDoors = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvBlock).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRoom).BeginInit();
            tabPage3.SuspendLayout();
            tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWindow).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvDoor).BeginInit();
            SuspendLayout();
            // 
            // btnAllBlocks
            // 
            btnAllBlocks.Location = new Point(23, 27);
            btnAllBlocks.Name = "btnAllBlocks";
            btnAllBlocks.Size = new Size(112, 34);
            btnAllBlocks.TabIndex = 0;
            btnAllBlocks.Text = "All Blocks";
            btnAllBlocks.UseVisualStyleBackColor = true;
            btnAllBlocks.Click += btnAllBlocks_Click;
            // 
            // dgvBlock
            // 
            dgvBlock.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("맑은 고딕", 14F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            dgvBlock.DefaultCellStyle = dataGridViewCellStyle1;
            dgvBlock.Location = new Point(90, 108);
            dgvBlock.Name = "dgvBlock";
            dgvBlock.RowHeadersWidth = 62;
            dgvBlock.RowTemplate.Height = 50;
            dgvBlock.Size = new Size(1164, 428);
            dgvBlock.TabIndex = 1;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1376, 690);
            tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(dgvBlock);
            tabPage1.Controls.Add(btnAllBlocks);
            tabPage1.Location = new Point(4, 34);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1368, 652);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Blocks";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(dgvRoom);
            tabPage2.Controls.Add(btnRooms);
            tabPage2.Location = new Point(4, 34);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1368, 652);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Rooms";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // dgvRoom
            // 
            dgvRoom.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("맑은 고딕", 14F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dgvRoom.DefaultCellStyle = dataGridViewCellStyle2;
            dgvRoom.Location = new Point(68, 93);
            dgvRoom.Name = "dgvRoom";
            dgvRoom.RowHeadersWidth = 62;
            dgvRoom.RowTemplate.Height = 50;
            dgvRoom.Size = new Size(1164, 428);
            dgvRoom.TabIndex = 2;
            // 
            // btnRooms
            // 
            btnRooms.Location = new Point(33, 31);
            btnRooms.Name = "btnRooms";
            btnRooms.Size = new Size(112, 34);
            btnRooms.TabIndex = 1;
            btnRooms.Text = "All Rooms";
            btnRooms.UseVisualStyleBackColor = true;
            btnRooms.Click += btnRooms_Click;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(dgvWindow);
            tabPage3.Controls.Add(btnWindows);
            tabPage3.Location = new Point(4, 34);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(1368, 652);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Windows";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(dgvDoor);
            tabPage4.Controls.Add(btnDoors);
            tabPage4.Location = new Point(4, 34);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(1368, 652);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Doors";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // dgvWindow
            // 
            dgvWindow.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = SystemColors.Window;
            dataGridViewCellStyle3.Font = new Font("맑은 고딕", 14F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle3.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            dgvWindow.DefaultCellStyle = dataGridViewCellStyle3;
            dgvWindow.Location = new Point(87, 112);
            dgvWindow.Name = "dgvWindow";
            dgvWindow.RowHeadersWidth = 62;
            dgvWindow.RowTemplate.Height = 50;
            dgvWindow.Size = new Size(1164, 428);
            dgvWindow.TabIndex = 4;
            // 
            // btnWindows
            // 
            btnWindows.Location = new Point(52, 50);
            btnWindows.Name = "btnWindows";
            btnWindows.Size = new Size(180, 34);
            btnWindows.TabIndex = 3;
            btnWindows.Text = "All Windows";
            btnWindows.UseVisualStyleBackColor = true;
            // 
            // dgvDoor
            // 
            dgvDoor.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = SystemColors.Window;
            dataGridViewCellStyle4.Font = new Font("맑은 고딕", 14F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle4.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            dgvDoor.DefaultCellStyle = dataGridViewCellStyle4;
            dgvDoor.Location = new Point(105, 112);
            dgvDoor.Name = "dgvDoor";
            dgvDoor.RowHeadersWidth = 62;
            dgvDoor.RowTemplate.Height = 50;
            dgvDoor.Size = new Size(1164, 428);
            dgvDoor.TabIndex = 6;
            // 
            // btnDoors
            // 
            btnDoors.Location = new Point(70, 50);
            btnDoors.Name = "btnDoors";
            btnDoors.Size = new Size(180, 34);
            btnDoors.TabIndex = 5;
            btnDoors.Text = "All Doors";
            btnDoors.UseVisualStyleBackColor = true;
            // 
            // FormBlockPart
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1376, 690);
            Controls.Add(tabControl1);
            Name = "FormBlockPart";
            Text = "BlocksForm";
            ((System.ComponentModel.ISupportInitialize)dgvBlock).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvRoom).EndInit();
            tabPage3.ResumeLayout(false);
            tabPage4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWindow).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvDoor).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button btnAllBlocks;
        private DataGridView dgvBlock;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private DataGridView dgvRoom;
        private Button btnRooms;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private DataGridView dgvWindow;
        private Button btnWindows;
        private DataGridView dgvDoor;
        private Button btnDoors;
    }
}