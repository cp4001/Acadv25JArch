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
            Col1 = new DataGridViewTextBoxColumn();
            Column1 = new DataGridViewImageColumn();
            Column2 = new DataGridViewTextBoxColumn();
            Column3 = new DataGridViewTextBoxColumn();
            Column4 = new DataGridViewTextBoxColumn();
            Column5 = new DataGridViewTextBoxColumn();
            Column6 = new DataGridViewTextBoxColumn();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            dgvRoom = new DataGridView();
            btnRooms = new Button();
            tabPage3 = new TabPage();
            dgvWindow = new DataGridView();
            btnWindows = new Button();
            tabPage4 = new TabPage();
            dgvDoor = new DataGridView();
            btnDoors = new Button();
            Col_Index = new DataGridViewTextBoxColumn();
            Col_Name = new DataGridViewTextBoxColumn();
            Col_CeilingHeight = new DataGridViewTextBoxColumn();
            Col_FloorHeight = new DataGridViewTextBoxColumn();
            Col_Roofarea = new DataGridViewTextBoxColumn();
            Col_FloorArea = new DataGridViewTextBoxColumn();
            Col_Volumn = new DataGridViewTextBoxColumn();
            ColWallText = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dgvBlock).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRoom).BeginInit();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWindow).BeginInit();
            tabPage4.SuspendLayout();
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
            dgvBlock.Columns.AddRange(new DataGridViewColumn[] { Col1, Column1, Column2, Column3, Column4, Column5, Column6 });
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
            dgvBlock.Size = new Size(1386, 428);
            dgvBlock.TabIndex = 1;
            // 
            // Col1
            // 
            Col1.DataPropertyName = "Index";
            Col1.HeaderText = "Index";
            Col1.MinimumWidth = 8;
            Col1.Name = "Col1";
            Col1.Width = 80;
            // 
            // Column1
            // 
            Column1.DataPropertyName = "Img";
            Column1.HeaderText = "Img";
            Column1.MinimumWidth = 8;
            Column1.Name = "Column1";
            Column1.Width = 80;
            // 
            // Column2
            // 
            Column2.DataPropertyName = "Name";
            Column2.HeaderText = "Name";
            Column2.MinimumWidth = 8;
            Column2.Name = "Column2";
            Column2.Width = 500;
            // 
            // Column3
            // 
            Column3.DataPropertyName = "Count";
            Column3.HeaderText = "Count";
            Column3.MinimumWidth = 8;
            Column3.Name = "Column3";
            Column3.Width = 80;
            // 
            // Column4
            // 
            Column4.DataPropertyName = "PartName";
            Column4.HeaderText = "PartName";
            Column4.MinimumWidth = 8;
            Column4.Name = "Column4";
            Column4.Width = 150;
            // 
            // Column5
            // 
            Column5.DataPropertyName = "Type";
            Column5.HeaderText = "Type";
            Column5.MinimumWidth = 8;
            Column5.Name = "Column5";
            Column5.Width = 150;
            // 
            // Column6
            // 
            Column6.DataPropertyName = "SymbolType";
            Column6.HeaderText = "Symbol";
            Column6.MinimumWidth = 8;
            Column6.Name = "Column6";
            Column6.Width = 150;
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
            tabControl1.Size = new Size(2131, 690);
            tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(dgvBlock);
            tabPage1.Controls.Add(btnAllBlocks);
            tabPage1.Location = new Point(4, 34);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(2123, 652);
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
            tabPage2.Size = new Size(2123, 652);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Rooms";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // dgvRoom
            // 
            dgvRoom.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRoom.Columns.AddRange(new DataGridViewColumn[] { Col_Index, Col_Name, Col_CeilingHeight, Col_FloorHeight, Col_Roofarea, Col_FloorArea, Col_Volumn, ColWallText });
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
            dgvRoom.Size = new Size(2021, 428);
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
            tabPage3.Size = new Size(2123, 652);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Windows";
            tabPage3.UseVisualStyleBackColor = true;
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
            // tabPage4
            // 
            tabPage4.Controls.Add(dgvDoor);
            tabPage4.Controls.Add(btnDoors);
            tabPage4.Location = new Point(4, 34);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(2123, 652);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Doors";
            tabPage4.UseVisualStyleBackColor = true;
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
            // Col_Index
            // 
            Col_Index.DataPropertyName = "Index";
            Col_Index.HeaderText = "Index";
            Col_Index.MinimumWidth = 8;
            Col_Index.Name = "Col_Index";
            Col_Index.Width = 80;
            // 
            // Col_Name
            // 
            Col_Name.DataPropertyName = "Name";
            Col_Name.HeaderText = "Name";
            Col_Name.MinimumWidth = 8;
            Col_Name.Name = "Col_Name";
            Col_Name.Width = 150;
            // 
            // Col_CeilingHeight
            // 
            Col_CeilingHeight.DataPropertyName = "CeilingHeight";
            Col_CeilingHeight.HeaderText = "천정고";
            Col_CeilingHeight.MinimumWidth = 8;
            Col_CeilingHeight.Name = "Col_CeilingHeight";
            // 
            // Col_FloorHeight
            // 
            Col_FloorHeight.DataPropertyName = "FloorHeight";
            Col_FloorHeight.HeaderText = "층고";
            Col_FloorHeight.MinimumWidth = 8;
            Col_FloorHeight.Name = "Col_FloorHeight";
            // 
            // Col_Roofarea
            // 
            Col_Roofarea.DataPropertyName = "RoofArea";
            Col_Roofarea.HeaderText = "지붕면적";
            Col_Roofarea.MinimumWidth = 8;
            Col_Roofarea.Name = "Col_Roofarea";
            // 
            // Col_FloorArea
            // 
            Col_FloorArea.DataPropertyName = "FloorArea";
            Col_FloorArea.HeaderText = "바닥면적";
            Col_FloorArea.MinimumWidth = 8;
            Col_FloorArea.Name = "Col_FloorArea";
            // 
            // Col_Volumn
            // 
            Col_Volumn.DataPropertyName = "Volumn";
            Col_Volumn.HeaderText = "체적";
            Col_Volumn.MinimumWidth = 8;
            Col_Volumn.Name = "Col_Volumn";
            // 
            // ColWallText
            // 
            ColWallText.DataPropertyName = "WallText";
            ColWallText.HeaderText = "벽면정보";
            ColWallText.MinimumWidth = 8;
            ColWallText.Name = "ColWallText";
            ColWallText.Width = 800;
            // 
            // FormBlockPart
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2131, 690);
            Controls.Add(tabControl1);
            Name = "FormBlockPart";
            Text = "BlocksForm";
            ((System.ComponentModel.ISupportInitialize)dgvBlock).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvRoom).EndInit();
            tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWindow).EndInit();
            tabPage4.ResumeLayout(false);
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
        private DataGridViewTextBoxColumn Col1;
        private DataGridViewImageColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private DataGridViewTextBoxColumn Column5;
        private DataGridViewTextBoxColumn Column6;
        private DataGridViewTextBoxColumn Col_Index;
        private DataGridViewTextBoxColumn Col_Name;
        private DataGridViewTextBoxColumn Col_CeilingHeight;
        private DataGridViewTextBoxColumn Col_FloorHeight;
        private DataGridViewTextBoxColumn Col_Roofarea;
        private DataGridViewTextBoxColumn Col_FloorArea;
        private DataGridViewTextBoxColumn Col_Volumn;
        private DataGridViewTextBoxColumn ColWallText;
    }
}