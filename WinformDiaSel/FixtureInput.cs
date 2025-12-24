namespace WinformDiaSel
{
    /// <summary>
    /// 기구 입력 데이터
    /// </summary>
    public class FixtureInput
    {
        public FixtureType Fixture { get; set; }
        public int Quantity { get; set; }
        public double TotalLoadUnits => Fixture.UnitFactor * Quantity;
    }
}
