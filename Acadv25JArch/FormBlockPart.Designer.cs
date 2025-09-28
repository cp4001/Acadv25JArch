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
            btnAllBlocks = new Button();
            dgvBlock = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)dgvBlock).BeginInit();
            SuspendLayout();
            // 
            // btnAllBlocks
            // 
            btnAllBlocks.Location = new Point(67, 45);
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
            dgvBlock.Location = new Point(163, 120);
            dgvBlock.Name = "dgvBlock";
            dgvBlock.RowHeadersWidth = 62;
            dgvBlock.RowTemplate.Height = 50;
            dgvBlock.Size = new Size(1084, 464);
            dgvBlock.TabIndex = 1;
            // 
            // FormBlockPart
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1376, 690);
            Controls.Add(dgvBlock);
            Controls.Add(btnAllBlocks);
            Name = "FormBlockPart";
            Text = "BlocksForm";
            ((System.ComponentModel.ISupportInitialize)dgvBlock).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button btnAllBlocks;
        private DataGridView dgvBlock;
    }
}