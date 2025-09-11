using System.ComponentModel;

namespace DataGridViewPersonSample
{
    public partial class MainForm : Form
    {
        private BindingList<Person> personList;

        // Person Ŭ���� ����
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

        private void btnAddPerson_Click(object sender, EventArgs e)
        {
            // �� Person �߰�
            var newPerson = new Person
            {
                Photo = CreateSampleImage(Color.LightGray, "?"),
                Name = "���ο� ���",
                Age = 20
            };
            personList.Add(newPerson);
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

    }
}
