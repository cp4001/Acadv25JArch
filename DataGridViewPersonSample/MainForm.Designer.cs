namespace DataGridViewPersonSample
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Button btnDeletePerson;
            dataGridView1 = new DataGridView();
            btnAddPerson = new Button();
            btnLoadSample = new Button();
            btnDeletePerson = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // btnDeletePerson
            // 
            btnDeletePerson.Location = new Point(824, 542);
            btnDeletePerson.Name = "btnDeletePerson";
            btnDeletePerson.Size = new Size(171, 34);
            btnDeletePerson.TabIndex = 3;
            btnDeletePerson.Text = "Delete Person";
            btnDeletePerson.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(288, 102);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new Size(707, 403);
            dataGridView1.TabIndex = 0;
            // 
            // btnAddPerson
            // 
            btnAddPerson.Location = new Point(288, 542);
            btnAddPerson.Name = "btnAddPerson";
            btnAddPerson.Size = new Size(208, 34);
            btnAddPerson.TabIndex = 1;
            btnAddPerson.Text = "Add Person";
            btnAddPerson.UseVisualStyleBackColor = true;
            btnAddPerson.Click += btnAddPerson_Click;
            // 
            // btnLoadSample
            // 
            btnLoadSample.Location = new Point(557, 542);
            btnLoadSample.Name = "btnLoadSample";
            btnLoadSample.Size = new Size(164, 34);
            btnLoadSample.TabIndex = 2;
            btnLoadSample.Text = "Load Sample";
            btnLoadSample.UseVisualStyleBackColor = true;
          
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1215, 688);
            Controls.Add(btnDeletePerson);
            Controls.Add(btnLoadSample);
            Controls.Add(btnAddPerson);
            Controls.Add(dataGridView1);
            Name = "MainForm";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dataGridView1;
        private Button btnAddPerson;
        private Button btnLoadSample;
    }
}
