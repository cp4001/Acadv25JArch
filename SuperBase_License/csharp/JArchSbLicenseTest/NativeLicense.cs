using System.Runtime.InteropServices;

namespace JArchSbLicenseTest;

/// <summary>
/// JArchSbLicense.dll (cpp/) 래퍼.
///
/// ★ AutoCAD 플러그인에 붙일 때는 이 파일만 그대로 복사하면 된다.
///   JArchSbLicense.dll 을 플러그인 DLL 과 같은 폴더에 두기만 하면 동작한다.
/// </summary>
public static class NativeLicense
{
    private const string Dll = "JArchSbLicense.dll";

    // const char* 는 UTF-8 바이트로 넘겨야 한다.
    // CharSet.Ansi 로 두면 시스템 ANSI 코드페이지(이 PC 는 CP949)로 변환되어,
    // comID 에 한글이 섞이면 서버로 가는 JSON 본문이 깨진 UTF-8 이 된다.
    // LPUTF8Str 은 .NET Core 3.0+ 에서 쓸 수 있다.
    //
    // x64 에서는 호출 규약이 하나뿐이라 Cdecl 지정은 사실상 무시되지만,
    // 32비트로 잘못 빌드했을 때 바로 티가 나도록 명시해 둔다.
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern int CheckLicenseOnline([MarshalAs(UnmanagedType.LPUTF8Str)] string comID);

    /// <summary>
    /// comID 의 사용 기한이 유효한지 서버에 묻는다.
    /// 미등록·기간초과·HTTP 오류·네트워크 오류는 전부 false (fail-closed).
    /// </summary>
    public static bool IsValid(string comID) => CheckLicenseOnline(comID) != 0;
}
