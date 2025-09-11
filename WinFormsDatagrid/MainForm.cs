using System.ComponentModel;

namespace WinFormsDatagrid
{
    // Person 클래스 정의
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
            this.Text = "DataGridView Person 샘플";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // DataGridView 설정
            dataGridView1 = new DataGridView();
            dataGridView1.Location = new Point(12, 12);
            dataGridView1.Size = new Size(760, 400);
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowTemplate.Height = 80; // 이미지 표시를 위한 행 높이 설정

            // 버튼들 설정
            btnLoadSample = new Button();
            btnLoadSample.Text = "샘플 데이터 로드";
            btnLoadSample.Location = new Point(12, 430);
            btnLoadSample.Size = new Size(120, 30);
            btnLoadSample.Click += BtnLoadSample_Click;

            btnAddPerson = new Button();
            btnAddPerson.Text = "사람 추가";
            btnAddPerson.Location = new Point(150, 430);
            btnAddPerson.Size = new Size(100, 30);
            btnAddPerson.Click += BtnAddPerson_Click;

            btnDeletePerson = new Button();
            btnDeletePerson.Text = "선택 삭제";
            btnDeletePerson.Location = new Point(270, 430);
            btnDeletePerson.Size = new Size(100, 30);
            btnDeletePerson.Click += BtnDeletePerson_Click;

            // 컨트롤들을 폼에 추가
            this.Controls.Add(dataGridView1);
            this.Controls.Add(btnLoadSample);
            this.Controls.Add(btnAddPerson);
            this.Controls.Add(btnDeletePerson);
        }

        private void InitializeData()
        {
            // BindingList 초기화
            personList = new BindingList<Person>();

            // DataGridView에 바인딩
            dataGridView1.DataSource = personList;

            // 컬럼 설정
            SetupColumns();
        }

        private void SetupColumns()
        {
            if (dataGridView1.Columns.Count > 0)
            {
                // Photo 컬럼을 이미지 컬럼으로 설정
                if (dataGridView1.Columns["Photo"] != null)
                {
                    var photoColumn = dataGridView1.Columns["Photo"];
                    photoColumn.HeaderText = "사진";
                    photoColumn.Width = 100;
                    photoColumn.DefaultCellStyle.NullValue = null;
                }

                // Name 컬럼 설정
                if (dataGridView1.Columns["Name"] != null)
                {
                    dataGridView1.Columns["Name"].HeaderText = "이름";
                    dataGridView1.Columns["Name"].Width = 150;
                }

                // Age 컬럼 설정
                if (dataGridView1.Columns["Age"] != null)
                {
                    dataGridView1.Columns["Age"].HeaderText = "나이";
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

            // 샘플 이미지 생성 (간단한 컬러 박스)
            var sampleImages = new Image[]
            {
                CreateSampleImage(Color.LightBlue, "A"),
                CreateSampleImage(Color.LightGreen, "B"),
                CreateSampleImage(Color.LightCoral, "C"),
                CreateSampleImage(Color.LightYellow, "D")
            };

            // 샘플 데이터 추가
            personList.Add(new Person { Photo = sampleImages[0], Name = "김철수", Age = 25 });
            personList.Add(new Person { Photo = sampleImages[1], Name = "이영희", Age = 30 });
            personList.Add(new Person { Photo = sampleImages[2], Name = "박민수", Age = 28 });
            personList.Add(new Person { Photo = sampleImages[3], Name = "최수진", Age = 35 });

            // 컬럼 설정 재적용
            SetupColumns();
        }

        private Image CreateSampleImage(Color backgroundColor, string text)
        {
            var bitmap = new Bitmap(60, 60);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // 배경 채우기
                graphics.FillRectangle(new SolidBrush(backgroundColor), 0, 0, 60, 60);

                // 테두리 그리기
                graphics.DrawRectangle(Pens.Black, 0, 0, 59, 59);

                // 텍스트 그리기
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
                MessageBox.Show("삭제할 항목을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 이미지 리소스 정리
                foreach (var person in personList)
                {
                    person.Photo?.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

    // 사람 추가 폼
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
            this.Text = "사람 추가";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 이름 라벨 및 텍스트박스
            var lblName = new Label();
            lblName.Text = "이름:";
            lblName.Location = new Point(20, 20);
            lblName.Size = new Size(50, 23);

            txtName = new TextBox();
            txtName.Location = new Point(80, 20);
            txtName.Size = new Size(200, 23);

            // 나이 라벨 및 숫자입력
            var lblAge = new Label();
            lblAge.Text = "나이:";
            lblAge.Location = new Point(20, 60);
            lblAge.Size = new Size(50, 23);

            numAge = new NumericUpDown();
            numAge.Location = new Point(80, 60);
            numAge.Size = new Size(100, 23);
            numAge.Minimum = 0;
            numAge.Maximum = 150;
            numAge.Value = 20;

            // 사진 라벨 및 PictureBox
            var lblPhoto = new Label();
            lblPhoto.Text = "사진:";
            lblPhoto.Location = new Point(20, 100);
            lblPhoto.Size = new Size(50, 23);

            pictureBox = new PictureBox();
            pictureBox.Location = new Point(80, 100);
            pictureBox.Size = new Size(100, 100);
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            btnSelectImage = new Button();
            btnSelectImage.Text = "이미지 선택";
            btnSelectImage.Location = new Point(200, 130);
            btnSelectImage.Size = new Size(100, 30);
            btnSelectImage.Click += BtnSelectImage_Click;

            // 확인/취소 버튼
            btnOK = new Button();
            btnOK.Text = "확인";
            btnOK.Location = new Point(200, 220);
            btnOK.Size = new Size(80, 30);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button();
            btnCancel.Text = "취소";
            btnCancel.Location = new Point(290, 220);
            btnCancel.Size = new Size(80, 30);
            btnCancel.DialogResult = DialogResult.Cancel;

            // 컨트롤들 추가
            this.Controls.AddRange(new Control[] {
                lblName, txtName, lblAge, numAge, lblPhoto, pictureBox,
                btnSelectImage, btnOK, btnCancel
            });
        }

        private void BtnSelectImage_Click(object? sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Title = "이미지 파일 선택";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        pictureBox.Image = Image.FromFile(openFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"이미지를 로드할 수 없습니다: {ex.Message}", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("이름을 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    // 프로그램 진입점
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
