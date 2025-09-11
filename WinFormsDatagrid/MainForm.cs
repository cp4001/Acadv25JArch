using System.ComponentModel;

namespace WinFormsDatagrid
{
    // Person Ŭ���� ����
    public class Person
    {
        public Image? Photo { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public partial class MainForm : Form
    {
        private DataGridView dataGridView1;
        private Button btnAddPerson;
        private Button btnLoadSample;
        private Button btnDeletePerson;
        private BindingList<Person> personList;

        public MainForm()
        {
            InitializeComponent();
            InitializeData();
        }

        private void InitializeComponent()
        {
            this.Text = "DataGridView Person ����";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // DataGridView ����
            dataGridView1 = new DataGridView();
            dataGridView1.Location = new Point(12, 12);
            dataGridView1.Size = new Size(760, 400);
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowTemplate.Height = 80; // �̹��� ǥ�ø� ���� �� ���� ����

            // ��ư�� ����
            btnLoadSample = new Button();
            btnLoadSample.Text = "���� ������ �ε�";
            btnLoadSample.Location = new Point(12, 430);
            btnLoadSample.Size = new Size(120, 30);
            btnLoadSample.Click += BtnLoadSample_Click;

            btnAddPerson = new Button();
            btnAddPerson.Text = "��� �߰�";
            btnAddPerson.Location = new Point(150, 430);
            btnAddPerson.Size = new Size(100, 30);
            btnAddPerson.Click += BtnAddPerson_Click;

            btnDeletePerson = new Button();
            btnDeletePerson.Text = "���� ����";
            btnDeletePerson.Location = new Point(270, 430);
            btnDeletePerson.Size = new Size(100, 30);
            btnDeletePerson.Click += BtnDeletePerson_Click;

            // ��Ʈ�ѵ��� ���� �߰�
            this.Controls.Add(dataGridView1);
            this.Controls.Add(btnLoadSample);
            this.Controls.Add(btnAddPerson);
            this.Controls.Add(btnDeletePerson);
        }

        private void InitializeData()
        {
            // BindingList �ʱ�ȭ
            personList = new BindingList<Person>();

            // DataGridView�� ���ε�
            dataGridView1.DataSource = personList;

            // �÷� ����
            SetupColumns();
        }

        private void SetupColumns()
        {
            if (dataGridView1.Columns.Count > 0)
            {
                // Photo �÷��� �̹��� �÷����� ����
                if (dataGridView1.Columns["Photo"] != null)
                {
                    var photoColumn = dataGridView1.Columns["Photo"];
                    photoColumn.HeaderText = "����";
                    photoColumn.Width = 100;
                    photoColumn.DefaultCellStyle.NullValue = null;
                }

                // Name �÷� ����
                if (dataGridView1.Columns["Name"] != null)
                {
                    dataGridView1.Columns["Name"].HeaderText = "�̸�";
                    dataGridView1.Columns["Name"].Width = 150;
                }

                // Age �÷� ����
                if (dataGridView1.Columns["Age"] != null)
                {
                    dataGridView1.Columns["Age"].HeaderText = "����";
                    dataGridView1.Columns["Age"].Width = 80;
                }
            }
        }

        private void BtnLoadSample_Click(object? sender, EventArgs e)
        {
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            personList.Clear();

            // ���� �̹��� ���� (������ �÷� �ڽ�)
            var sampleImages = new Image[]
            {
                CreateSampleImage(Color.LightBlue, "A"),
                CreateSampleImage(Color.LightGreen, "B"),
                CreateSampleImage(Color.LightCoral, "C"),
                CreateSampleImage(Color.LightYellow, "D")
            };

            // ���� ������ �߰�
            personList.Add(new Person { Photo = sampleImages[0], Name = "��ö��", Age = 25 });
            personList.Add(new Person { Photo = sampleImages[1], Name = "�̿���", Age = 30 });
            personList.Add(new Person { Photo = sampleImages[2], Name = "�ڹμ�", Age = 28 });
            personList.Add(new Person { Photo = sampleImages[3], Name = "�ּ���", Age = 35 });

            // �÷� ���� ������
            SetupColumns();
        }

        private Image CreateSampleImage(Color backgroundColor, string text)
        {
            var bitmap = new Bitmap(60, 60);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // ��� ä���
                graphics.FillRectangle(new SolidBrush(backgroundColor), 0, 0, 60, 60);

                // �׵θ� �׸���
                graphics.DrawRectangle(Pens.Black, 0, 0, 59, 59);

                // �ؽ�Ʈ �׸���
                using (var font = new Font("Arial", 20, FontStyle.Bold))
                {
                    var textSize = graphics.MeasureString(text, font);
                    var x = (60 - textSize.Width) / 2;
                    var y = (60 - textSize.Height) / 2;
                    graphics.DrawString(text, font, Brushes.Black, x, y);
                }
            }
            return bitmap;
        }

        private void BtnAddPerson_Click(object? sender, EventArgs e)
        {
            using (var addForm = new AddPersonForm())
            {
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    personList.Add(addForm.NewPerson);
                }
            }
        }

        private void BtnDeletePerson_Click(object? sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                var selectedIndex = dataGridView1.SelectedRows[0].Index;
                if (selectedIndex >= 0 && selectedIndex < personList.Count)
                {
                    personList.RemoveAt(selectedIndex);
                }
            }
            else
            {
                MessageBox.Show("������ �׸��� �������ּ���.", "�˸�", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // �̹��� ���ҽ� ����
                foreach (var person in personList)
                {
                    person.Photo?.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

    // ��� �߰� ��
    public partial class AddPersonForm : Form
    {
        private TextBox txtName;
        private NumericUpDown numAge;
        private PictureBox pictureBox;
        private Button btnSelectImage;
        private Button btnOK;
        private Button btnCancel;

        public Person NewPerson { get; private set; } = new Person();

        public AddPersonForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "��� �߰�";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // �̸� �� �� �ؽ�Ʈ�ڽ�
            var lblName = new Label();
            lblName.Text = "�̸�:";
            lblName.Location = new Point(20, 20);
            lblName.Size = new Size(50, 23);

            txtName = new TextBox();
            txtName.Location = new Point(80, 20);
            txtName.Size = new Size(200, 23);

            // ���� �� �� �����Է�
            var lblAge = new Label();
            lblAge.Text = "����:";
            lblAge.Location = new Point(20, 60);
            lblAge.Size = new Size(50, 23);

            numAge = new NumericUpDown();
            numAge.Location = new Point(80, 60);
            numAge.Size = new Size(100, 23);
            numAge.Minimum = 0;
            numAge.Maximum = 150;
            numAge.Value = 20;

            // ���� �� �� PictureBox
            var lblPhoto = new Label();
            lblPhoto.Text = "����:";
            lblPhoto.Location = new Point(20, 100);
            lblPhoto.Size = new Size(50, 23);

            pictureBox = new PictureBox();
            pictureBox.Location = new Point(80, 100);
            pictureBox.Size = new Size(100, 100);
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            btnSelectImage = new Button();
            btnSelectImage.Text = "�̹��� ����";
            btnSelectImage.Location = new Point(200, 130);
            btnSelectImage.Size = new Size(100, 30);
            btnSelectImage.Click += BtnSelectImage_Click;

            // Ȯ��/��� ��ư
            btnOK = new Button();
            btnOK.Text = "Ȯ��";
            btnOK.Location = new Point(200, 220);
            btnOK.Size = new Size(80, 30);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button();
            btnCancel.Text = "���";
            btnCancel.Location = new Point(290, 220);
            btnCancel.Size = new Size(80, 30);
            btnCancel.DialogResult = DialogResult.Cancel;

            // ��Ʈ�ѵ� �߰�
            this.Controls.AddRange(new Control[] {
                lblName, txtName, lblAge, numAge, lblPhoto, pictureBox,
                btnSelectImage, btnOK, btnCancel
            });
        }

        private void BtnSelectImage_Click(object? sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "�̹��� ����|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Title = "�̹��� ���� ����";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        pictureBox.Image = Image.FromFile(openFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"�̹����� �ε��� �� �����ϴ�: {ex.Message}", "����",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("�̸��� �Է����ּ���.", "�Է� ����", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            NewPerson = new Person
            {
                Name = txtName.Text.Trim(),
                Age = (int)numAge.Value,
                Photo = pictureBox.Image?.Clone() as Image
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // ���α׷� ������
    public class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
