using System.ComponentModel;

namespace DataGridViewPersonSample
{
    public partial class MainForm : Form
    {
        private BindingList<Person> personList;

        // Person 클래스 정의
        public class Person
        {
            public Image? Photo { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }



        ///

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

        private void btnAddPerson_Click(object sender, EventArgs e)
        {
            // 새 Person 추가
            var newPerson = new Person
            {
                Photo = CreateSampleImage(Color.LightGray, "?"),
                Name = "새로운 사람",
                Age = 20
            };
            personList.Add(newPerson);
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

    }
}
