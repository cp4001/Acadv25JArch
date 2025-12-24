namespace WinformDiaSel
{
    /// <summary>
    /// 위생기구 타입 정보
    /// </summary>
    public class FixtureType
    {
        public string Name { get; set; }
        public double UnitFactor { get; set; }
        public int MinPipeSize { get; set; }
        public bool IsFlushValve { get; set; }

        public FixtureType(string name, double factor, int minSize, bool isFV = false)
        {
            Name = name;
            UnitFactor = factor;
            MinPipeSize = minSize;
            IsFlushValve = isFV;
        }
    }
}
