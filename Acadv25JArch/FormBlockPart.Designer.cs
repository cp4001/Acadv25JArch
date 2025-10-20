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
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            btnAllBlocks = new Button();
            dgvBlock = new DataGridView();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            btnRooms = new Button();
            dgvRoom = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)dgvBlock).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRoom).BeginInit();
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
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = SystemColors.Window;
            dataGridViewCellStyle3.Font = new Font("맑은 고딕", 14F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle3.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            dgvBlock.DefaultCellStyle = dataGridViewCellStyle3;
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
            // dgvRoom
            // 
            dgvRoom.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = SystemColors.Window;
            dataGridViewCellStyle4.Font = new Font("맑은 고딕", 14F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle4.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            dgvRoom.DefaultCellStyle = dataGridViewCellStyle4;
            dgvRoom.Location = new Point(68, 93);
            dgvRoom.Name = "dgvRoom";
            dgvRoom.RowHeadersWidth = 62;
            dgvRoom.RowTemplate.Height = 50;
            dgvRoom.Size = new Size(1164, 428);
            dgvRoom.TabIndex = 2;
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
    }
}