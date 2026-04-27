using System;

namespace Acadv25JArch.PipeDiaCalc
{
    /// <summary>
    /// 현재 열린 DWG의 기본 상수값을 보관하는 static 클래스.
    /// 도면 Open 시 DwgDefaultLoader가 NOD에서 값을 읽어 여기에 채워 넣는다.
    /// 새로운 상수는 이 클래스에 필드로 추가하고
    /// AinitCommand.WriteAllDefaults / DwgDefaultLoader.LoadAllDefaults 에도
    /// 같은 키 이름으로 추가하면 된다.
    /// </summary>
    public static class DwgDefault
    {
        // 기본값 (NOD에 기록이 없을 때 사용)
        public static string DiaNoteHeight = "100";

        // 추후 추가될 상수들...
        // public static string XxxValue = "default";
    }
}
