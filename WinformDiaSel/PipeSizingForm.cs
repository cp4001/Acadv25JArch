using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinformDiaSel
{
    public partial class PipeSizingForm : Form
    {
        private PipeSizingEngine _engine;
        private List<FixtureType> _fixtures;

        public PipeSizingForm()
        {
            InitializeComponent();
            InitializeData();
            SetupGrid();
        }

        private void InitializeData()
        {
            _engine = new PipeSizingEngine();
            
            // 기본 기구 목록 초기화
            _fixtures = new List<FixtureType>
            {
                new FixtureType("대변기", 4.9, 25, true),  // 세정밸브
                new FixtureType("소변기", 1.0, 15),
                new FixtureType("세면기", 1.0, 15),
                new FixtureType("청소씽크", 2.6, 20),
                new FixtureType("샤워", 1.0, 15),
                new FixtureType("욕조", 2.6, 20),
                new FixtureType("주방씽크", 1.0, 15)
            };
        }

        private void SetupGrid()
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.RowTemplate.Height = 35; // 행 높이 증가

            // 컬럼 헤더 중앙 정렬
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // 컬럼 설정
            dataGridView1.Columns.Clear();
            
            // 위생기구명
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFixtureName",
                HeaderText = "위생기구",
                Width = 180,
                ReadOnly = true
            });

            // 접속관경
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMinPipeSize",
                HeaderText = "접속관경",
                Width = 150,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            // 15A 환산
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colUnitFactor",
                HeaderText = "15A환산",
                Width = 150,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            // 수량 (편집 가능)
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colQuantity",
                HeaderText = "수량",
                Width = 120,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            // 합계
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTotal",
                HeaderText = "합계",
                Width = 150,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            // 동시사용율
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSimulRate",
                HeaderText = "동시사용율",
                Width = 180,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            // 동시사용율값
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colEffective",
                HeaderText = "동시사용율값",
                Width = 195,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            // 데이터 행 추가
            foreach (var fixture in _fixtures)
            {
                int rowIndex = dataGridView1.Rows.Add();
                var row = dataGridView1.Rows[rowIndex];
                
                row.Cells["colFixtureName"].Value = fixture.Name;
                row.Cells["colMinPipeSize"].Value = fixture.MinPipeSize;
                row.Cells["colUnitFactor"].Value = fixture.UnitFactor;
                row.Cells["colQuantity"].Value = 0;
                row.Cells["colTotal"].Value = "0.0";
                row.Cells["colSimulRate"].Value = "";
                row.Cells["colEffective"].Value = "";
                row.Tag = fixture; // 기구 정보 저장
            }

            // 셀 편집 이벤트
            dataGridView1.CellValueChanged += DataGridView1_CellValueChanged;
            dataGridView1.CellEndEdit += DataGridView1_CellEndEdit;
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "colQuantity")
            {
                UpdateRowTotal(e.RowIndex);
            }
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == "colQuantity")
            {
                UpdateRowTotal(e.RowIndex);
            }
        }

        private void UpdateRowTotal(int rowIndex)
        {
            try
            {
                var row = dataGridView1.Rows[rowIndex];
                var qtyCell = row.Cells["colQuantity"].Value;
                int qty = 0;

                if (qtyCell != null && int.TryParse(qtyCell.ToString(), out qty) && qty >= 0)
                {
                    var unitFactor = Convert.ToDouble(row.Cells["colUnitFactor"].Value);
                    double total = qty * unitFactor;
                    row.Cells["colTotal"].Value = total.ToString("F1");
                }
                else
                {
                    row.Cells["colQuantity"].Value = 0;
                    row.Cells["colTotal"].Value = "0.0";
                }
            }
            catch
            {
                dataGridView1.Rows[rowIndex].Cells["colQuantity"].Value = 0;
                dataGridView1.Rows[rowIndex].Cells["colTotal"].Value = "0.0";
            }
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            try
            {
                // 입력 데이터 수집
                var inputs = new List<FixtureInput>();
                
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var fixture = row.Tag as FixtureType;
                    if (fixture == null) continue;

                    int qty = 0;
                    var qtyValue = row.Cells["colQuantity"].Value;
                    if (qtyValue != null)
                    {
                        int.TryParse(qtyValue.ToString(), out qty);
                    }

                    inputs.Add(new FixtureInput
                    {
                        Fixture = fixture,
                        Quantity = qty
                    });
                }

                // 계산 수행
                var result = _engine.Calculate(inputs);

                // 그리드에 동시사용율 업데이트
                UpdateGridSimultaneity(inputs, result);

                // 결과 표시
                lblGenLoad.Text =       $"일반기구   부 하: {result.GenLoadSum:F1}";
                lblGenRate.Text =        $"일반기구 동시율: {result.GenRate:F2}";
                lblGenEffective.Text =  $"일반기구 유효부하: {result.GenEffective:F2}";
                
                lblFvLoad.Text =       $"대변기   부 하: {result.FvLoadSum:F1}";
                lblFvRate.Text =        $"대변기 동시율: {result.FvRate:F2} "; //(수량: {result.FvQtySum}개)
                lblFvEffective.Text =  $"대변기 유효부하: {result.FvEffective:F2}";
                
                lblTotalEffective.Text = $"최종 유효부하: {result.TotalEffective:F2}";
                lblMainSize.Text = $"{result.MainSize} A";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"계산 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateGridSimultaneity(List<FixtureInput> inputs, CalcResult result)
        {
            for (int i = 0; i < dataGridView1.Rows.Count && i < inputs.Count; i++)
            {
                var row = dataGridView1.Rows[i];
                var input = inputs[i];

                if (input.Quantity == 0)
                {
                    row.Cells["colSimulRate"].Value = "";
                    row.Cells["colEffective"].Value = "";
                    continue;
                }

                double rate;
                double effective;

                if (input.Fixture.IsFlushValve)
                {
                    rate = result.FvRate;
                    effective = input.TotalLoadUnits * rate;
                }
                else
                {
                    rate = _engine.GetGeneralSimultaneity(input.TotalLoadUnits);
                    effective = input.TotalLoadUnits * rate;
                }

                row.Cells["colSimulRate"].Value = rate.ToString("F2");
                row.Cells["colEffective"].Value = effective.ToString("F2");
            }
        }
    }
}
