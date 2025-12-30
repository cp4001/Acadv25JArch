namespace WinformDiaSel
{
    partial class PipeSizingForm
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
            panelTop = new System.Windows.Forms.Panel();
            btnCalculate = new System.Windows.Forms.Button();
            lblMainSize = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            lblTotalEffective = new System.Windows.Forms.Label();
            lblFvEffective = new System.Windows.Forms.Label();
            lblFvRate = new System.Windows.Forms.Label();
            lblFvLoad = new System.Windows.Forms.Label();
            lblGenEffective = new System.Windows.Forms.Label();
            lblGenRate = new System.Windows.Forms.Label();
            lblGenLoad = new System.Windows.Forms.Label();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // panelTop
            // 
            panelTop.BackColor = System.Drawing.SystemColors.Control;
            panelTop.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            panelTop.Controls.Add(btnCalculate);
            panelTop.Controls.Add(lblMainSize);
            panelTop.Controls.Add(label8);
            panelTop.Controls.Add(lblTotalEffective);
            panelTop.Controls.Add(lblFvEffective);
            panelTop.Controls.Add(lblFvRate);
            panelTop.Controls.Add(lblFvLoad);
            panelTop.Controls.Add(lblGenEffective);
            panelTop.Controls.Add(lblGenRate);
            panelTop.Controls.Add(lblGenLoad);
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Location = new System.Drawing.Point(0, 0);
            panelTop.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            panelTop.Name = "panelTop";
            panelTop.Size = new System.Drawing.Size(1287, 161);
            panelTop.TabIndex = 0;
            // 
            // btnCalculate
            // 
            btnCalculate.Anchor = System.Windows.Forms.AnchorStyles.None;
            btnCalculate.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            btnCalculate.Location = new System.Drawing.Point(691, 84);
            btnCalculate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCalculate.Name = "btnCalculate";
            btnCalculate.Size = new System.Drawing.Size(171, 44);
            btnCalculate.TabIndex = 0;
            btnCalculate.Text = "계산";
            btnCalculate.UseVisualStyleBackColor = true;
            btnCalculate.Click += btnCalculate_Click;
            // 
            // lblMainSize
            // 
            lblMainSize.AutoSize = true;
            lblMainSize.Font = new System.Drawing.Font("맑은 고딕", 24F, System.Drawing.FontStyle.Bold);
            lblMainSize.ForeColor = System.Drawing.Color.Red;
            lblMainSize.Location = new System.Drawing.Point(990, 69);
            lblMainSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblMainSize.Name = "lblMainSize";
            lblMainSize.Size = new System.Drawing.Size(136, 65);
            lblMainSize.TabIndex = 8;
            lblMainSize.Text = "- - A";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            label8.Location = new System.Drawing.Point(959, 28);
            label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(195, 38);
            label8.TabIndex = 7;
            label8.Text = "최종 결정관경";
            // 
            // lblTotalEffective
            // 
            lblTotalEffective.AutoSize = true;
            lblTotalEffective.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            lblTotalEffective.Location = new System.Drawing.Point(691, 26);
            lblTotalEffective.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblTotalEffective.Name = "lblTotalEffective";
            lblTotalEffective.Size = new System.Drawing.Size(193, 30);
            lblTotalEffective.TabIndex = 6;
            lblTotalEffective.Text = "최종 유효부하: - -";
            // 
            // lblFvEffective
            // 
            lblFvEffective.AutoSize = true;
            lblFvEffective.Font = new System.Drawing.Font("맑은 고딕", 10F);
            lblFvEffective.Location = new System.Drawing.Point(335, 100);
            lblFvEffective.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblFvEffective.Name = "lblFvEffective";
            lblFvEffective.Size = new System.Drawing.Size(193, 28);
            lblFvEffective.TabIndex = 5;
            lblFvEffective.Text = "대변기 유효부하: - -";
            // 
            // lblFvRate
            // 
            lblFvRate.AutoSize = true;
            lblFvRate.Font = new System.Drawing.Font("맑은 고딕", 10F);
            lblFvRate.Location = new System.Drawing.Point(335, 63);
            lblFvRate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblFvRate.Name = "lblFvRate";
            lblFvRate.Size = new System.Drawing.Size(173, 28);
            lblFvRate.TabIndex = 4;
            lblFvRate.Text = "대변기 동시율: - -";
            // 
            // lblFvLoad
            // 
            lblFvLoad.AutoSize = true;
            lblFvLoad.Font = new System.Drawing.Font("맑은 고딕", 10F);
            lblFvLoad.Location = new System.Drawing.Point(335, 28);
            lblFvLoad.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblFvLoad.Name = "lblFvLoad";
            lblFvLoad.Size = new System.Drawing.Size(174, 28);
            lblFvLoad.TabIndex = 3;
            lblFvLoad.Text = "대변기 부하   : - -";
            // 
            // lblGenEffective
            // 
            lblGenEffective.AutoSize = true;
            lblGenEffective.Font = new System.Drawing.Font("맑은 고딕", 10F);
            lblGenEffective.Location = new System.Drawing.Point(29, 100);
            lblGenEffective.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblGenEffective.Name = "lblGenEffective";
            lblGenEffective.Size = new System.Drawing.Size(213, 28);
            lblGenEffective.TabIndex = 2;
            lblGenEffective.Text = "일반기구 유효부하: - -";
            // 
            // lblGenRate
            // 
            lblGenRate.AutoSize = true;
            lblGenRate.Font = new System.Drawing.Font("맑은 고딕", 10F);
            lblGenRate.Location = new System.Drawing.Point(29, 63);
            lblGenRate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblGenRate.Name = "lblGenRate";
            lblGenRate.Size = new System.Drawing.Size(193, 28);
            lblGenRate.TabIndex = 1;
            lblGenRate.Text = "일반기구 동시율: - -";
            // 
            // lblGenLoad
            // 
            lblGenLoad.AutoSize = true;
            lblGenLoad.Font = new System.Drawing.Font("맑은 고딕", 10F);
            lblGenLoad.Location = new System.Drawing.Point(29, 28);
            lblGenLoad.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblGenLoad.Name = "lblGenLoad";
            lblGenLoad.Size = new System.Drawing.Size(194, 28);
            lblGenLoad.TabIndex = 0;
            lblGenLoad.Text = "일반기구  부하  : - -";
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            dataGridView1.Location = new System.Drawing.Point(0, 161);
            dataGridView1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.RowTemplate.Height = 25;
            dataGridView1.Size = new System.Drawing.Size(1287, 460);
            dataGridView1.TabIndex = 1;
            // 
            // PipeSizingForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1287, 621);
            Controls.Add(dataGridView1);
            Controls.Add(panelTop);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "PipeSizingForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "급수 관경 결정 계산";
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnCalculate;
        private System.Windows.Forms.Label lblGenLoad;
        private System.Windows.Forms.Label lblGenRate;
        private System.Windows.Forms.Label lblGenEffective;
        private System.Windows.Forms.Label lblFvLoad;
        private System.Windows.Forms.Label lblFvRate;
        private System.Windows.Forms.Label lblFvEffective;
        private System.Windows.Forms.Label lblTotalEffective;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label lblMainSize;
    }
}
