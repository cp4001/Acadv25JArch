namespace WinformDiaSel
{
    /// <summary>
    /// 계산 결과 데이터
    /// </summary>
    public class CalcResult
    {
        public double GenLoadSum { get; set; }
        public double GenRate { get; set; }
        public double GenEffective { get; set; }
        public double FvLoadSum { get; set; }
        public double FvRate { get; set; }
        public double FvEffective { get; set; }
        public int FvQtySum { get; set; }
        public double TotalEffective { get; set; }
        public int MainSize { get; set; }
    }
}
