namespace JArchLicenseAdmin;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _client?.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        grid = new DataGridView();
        pnlEdit = new Panel();
        lblComId = new Label();
        txtComId = new TextBox();
        lblExpDate = new Label();
        dtpExpDate = new DateTimePicker();
        lblUserName = new Label();
        txtUserName = new TextBox();
        lblCompName = new Label();
        txtCompName = new TextBox();
        lblPartName = new Label();
        txtPartName = new TextBox();
        lblRegDate = new Label();
        lblRegDateValue = new Label();
        btnNew = new Button();
        btnCreate = new Button();
        btnUpdate = new Button();
        btnDelete = new Button();
        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();
        pnlTop = new Panel();
        lblSearch = new Label();
        txtSearch = new TextBox();
        btnRefresh = new Button();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        pnlEdit.SuspendLayout();
        statusStrip.SuspendLayout();
        pnlTop.SuspendLayout();
        SuspendLayout();
        //
        // grid
        //
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.Dock = DockStyle.Fill;
        grid.EditMode = DataGridViewEditMode.EditProgrammatically;
        grid.MultiSelect = false;
        grid.Name = "grid";
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.TabIndex = 1;
        //
        // pnlEdit
        //
        pnlEdit.Controls.Add(lblComId);
        pnlEdit.Controls.Add(txtComId);
        pnlEdit.Controls.Add(lblExpDate);
        pnlEdit.Controls.Add(dtpExpDate);
        pnlEdit.Controls.Add(lblUserName);
        pnlEdit.Controls.Add(txtUserName);
        pnlEdit.Controls.Add(lblCompName);
        pnlEdit.Controls.Add(txtCompName);
        pnlEdit.Controls.Add(lblPartName);
        pnlEdit.Controls.Add(txtPartName);
        pnlEdit.Controls.Add(lblRegDate);
        pnlEdit.Controls.Add(lblRegDateValue);
        pnlEdit.Controls.Add(btnNew);
        pnlEdit.Controls.Add(btnCreate);
        pnlEdit.Controls.Add(btnUpdate);
        pnlEdit.Controls.Add(btnDelete);
        pnlEdit.Dock = DockStyle.Bottom;
        pnlEdit.Name = "pnlEdit";
        pnlEdit.Padding = new Padding(0, 8, 0, 8);
        pnlEdit.Size = new Size(980, 168);
        pnlEdit.TabIndex = 2;
        //
        // lblComId
        //
        lblComId.AutoSize = true;
        lblComId.Location = new Point(14, 16);
        lblComId.Name = "lblComId";
        lblComId.Size = new Size(85, 15);
        lblComId.Text = "라이선스 ID *";
        //
        // txtComId
        //
        txtComId.Location = new Point(112, 12);
        txtComId.Name = "txtComId";
        txtComId.Size = new Size(190, 23);
        txtComId.TabIndex = 10;
        //
        // lblExpDate
        //
        lblExpDate.AutoSize = true;
        lblExpDate.Location = new Point(330, 16);
        lblExpDate.Name = "lblExpDate";
        lblExpDate.Size = new Size(65, 15);
        lblExpDate.Text = "사용 기한 *";
        //
        // dtpExpDate
        //
        dtpExpDate.Format = DateTimePickerFormat.Custom;
        dtpExpDate.CustomFormat = "yyyy-MM-dd";
        dtpExpDate.Location = new Point(408, 12);
        dtpExpDate.Name = "dtpExpDate";
        dtpExpDate.Size = new Size(150, 23);
        dtpExpDate.TabIndex = 11;
        //
        // lblUserName
        //
        lblUserName.AutoSize = true;
        lblUserName.Location = new Point(14, 52);
        lblUserName.Name = "lblUserName";
        lblUserName.Size = new Size(46, 15);
        lblUserName.Text = "사용자";
        //
        // txtUserName
        //
        txtUserName.Location = new Point(112, 48);
        txtUserName.Name = "txtUserName";
        txtUserName.Size = new Size(190, 23);
        txtUserName.TabIndex = 12;
        //
        // lblCompName
        //
        lblCompName.AutoSize = true;
        lblCompName.Location = new Point(330, 52);
        lblCompName.Name = "lblCompName";
        lblCompName.Size = new Size(46, 15);
        lblCompName.Text = "회사명";
        //
        // txtCompName
        //
        txtCompName.Location = new Point(408, 48);
        txtCompName.Name = "txtCompName";
        txtCompName.Size = new Size(190, 23);
        txtCompName.TabIndex = 13;
        //
        // lblPartName
        //
        lblPartName.AutoSize = true;
        lblPartName.Location = new Point(14, 88);
        lblPartName.Name = "lblPartName";
        lblPartName.Size = new Size(46, 15);
        lblPartName.Text = "부서명";
        //
        // txtPartName
        //
        txtPartName.Location = new Point(112, 84);
        txtPartName.Name = "txtPartName";
        txtPartName.Size = new Size(190, 23);
        txtPartName.TabIndex = 14;
        //
        // lblRegDate
        //
        lblRegDate.AutoSize = true;
        lblRegDate.Location = new Point(330, 88);
        lblRegDate.Name = "lblRegDate";
        lblRegDate.Size = new Size(46, 15);
        lblRegDate.Text = "등록일";
        //
        // lblRegDateValue
        //
        lblRegDateValue.AutoSize = true;
        lblRegDateValue.ForeColor = SystemColors.GrayText;
        lblRegDateValue.Location = new Point(408, 88);
        lblRegDateValue.Name = "lblRegDateValue";
        lblRegDateValue.Size = new Size(140, 15);
        lblRegDateValue.Text = "(추가 시 서버가 채움)";
        //
        // btnNew
        //
        btnNew.Location = new Point(112, 124);
        btnNew.Name = "btnNew";
        btnNew.Size = new Size(110, 30);
        btnNew.TabIndex = 20;
        btnNew.Text = "새 입력";
        btnNew.UseVisualStyleBackColor = true;
        btnNew.Click += btnNew_Click;
        //
        // btnCreate
        //
        btnCreate.Location = new Point(232, 124);
        btnCreate.Name = "btnCreate";
        btnCreate.Size = new Size(110, 30);
        btnCreate.TabIndex = 21;
        btnCreate.Text = "추가";
        btnCreate.UseVisualStyleBackColor = true;
        btnCreate.Click += btnCreate_Click;
        //
        // btnUpdate
        //
        btnUpdate.Location = new Point(352, 124);
        btnUpdate.Name = "btnUpdate";
        btnUpdate.Size = new Size(110, 30);
        btnUpdate.TabIndex = 22;
        btnUpdate.Text = "수정";
        btnUpdate.UseVisualStyleBackColor = true;
        btnUpdate.Click += btnUpdate_Click;
        //
        // btnDelete
        //
        btnDelete.Location = new Point(472, 124);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(110, 30);
        btnDelete.TabIndex = 23;
        btnDelete.Text = "삭제";
        btnDelete.UseVisualStyleBackColor = true;
        btnDelete.Click += btnDelete_Click;
        //
        // statusStrip
        //
        statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(980, 22);
        statusStrip.TabIndex = 3;
        //
        // lblStatus
        //
        lblStatus.Name = "lblStatus";
        lblStatus.Text = "준비";
        //
        // pnlTop
        //
        pnlTop.Controls.Add(lblSearch);
        pnlTop.Controls.Add(txtSearch);
        pnlTop.Controls.Add(btnRefresh);
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Name = "pnlTop";
        pnlTop.Size = new Size(980, 46);
        pnlTop.TabIndex = 0;
        //
        // lblSearch
        //
        lblSearch.AutoSize = true;
        lblSearch.Location = new Point(14, 14);
        lblSearch.Name = "lblSearch";
        lblSearch.Size = new Size(34, 15);
        lblSearch.Text = "검색";
        //
        // txtSearch
        //
        txtSearch.Location = new Point(60, 10);
        txtSearch.Name = "txtSearch";
        txtSearch.PlaceholderText = "라이선스 ID / 사용자 / 회사명 / 부서명";
        txtSearch.Size = new Size(320, 23);
        txtSearch.TabIndex = 1;
        txtSearch.TextChanged += txtSearch_TextChanged;
        //
        // btnRefresh
        //
        btnRefresh.Location = new Point(396, 9);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(110, 26);
        btnRefresh.TabIndex = 2;
        btnRefresh.Text = "새로 고침";
        btnRefresh.UseVisualStyleBackColor = true;
        btnRefresh.Click += btnRefresh_Click;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(980, 620);
        MinimumSize = new Size(760, 480);
        // 도킹은 z-order 역순으로 처리된다. grid(Fill) 을 먼저 넣어야
        // 나머지가 가장자리를 차지한 뒤 남는 공간을 grid 가 채운다.
        Controls.Add(grid);
        Controls.Add(pnlEdit);
        Controls.Add(statusStrip);
        Controls.Add(pnlTop);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "JArch 라이선스 관리";
        Load += MainForm_Load;
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        pnlEdit.ResumeLayout(false);
        pnlEdit.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        pnlTop.ResumeLayout(false);
        pnlTop.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private DataGridView grid;
    private Panel pnlEdit;
    private Label lblComId;
    private TextBox txtComId;
    private Label lblExpDate;
    private DateTimePicker dtpExpDate;
    private Label lblUserName;
    private TextBox txtUserName;
    private Label lblCompName;
    private TextBox txtCompName;
    private Label lblPartName;
    private TextBox txtPartName;
    private Label lblRegDate;
    private Label lblRegDateValue;
    private Button btnNew;
    private Button btnCreate;
    private Button btnUpdate;
    private Button btnDelete;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;
    private Panel pnlTop;
    private Label lblSearch;
    private TextBox txtSearch;
    private Button btnRefresh;
}
