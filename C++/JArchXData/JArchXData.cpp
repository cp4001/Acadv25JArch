//
// JArchXData.arx - AutoCAD 2025 (ObjectARX 2025)
//
// C# 에서 P/Invoke 로 호출하는 범용 Xdata 쓰기 함수 + 사용기한 보안.
//
//   int JArchXDataSet(void* objIdPtr, const wchar_t* regName, const wchar_t* value)
//
//   - objIdPtr : C# 의 ObjectId.OldIdPtr (= AcDbStub*)
//   - 사용기한이 지났으면 아무것도 기록하지 않고 eOk 로 조용히 반환한다.
//     (C# 쪽에서는 예외/에러가 없으므로 그냥 지나가고, 결과물의 Xdata 만 비어 있게 된다.)
//
//   시각 판정 : 인터넷(HTTPS 응답 Date 헤더) 시각을 우선 사용하고,
//               연결 실패 시 로컬 시계로 폴백하되 레지스트리에 숨겨둔
//               최신 관측 시각(high-water) 아래로는 내려가지 못하게 한다.
//

#include <rxregsvc.h>
#include <aced.h>
#include <dbmain.h>
#include <dbents.h>
#include <adslib.h>
#include <tchar.h>

#include <windows.h>
#include <winhttp.h>
#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "advapi32.lib")

#define JARCH_API extern "C" __declspec(dllexport)

// ---------------------------------------------------------------------------
// 사용기한: 2027-04-01 (UTC 자정). 정적 분석(strings)에 노출되지 않도록 XOR 은닉.
// ---------------------------------------------------------------------------
namespace {

    const unsigned int kMask = 0x5A3C7E11u;   // 위장용 상수
    const unsigned int kEncY = 2027u ^ kMask; // 연
    const unsigned int kEncM = 4u    ^ kMask; // 월
    const unsigned int kEncD = 1u    ^ kMask; // 일

    void expiryFileTime(FILETIME& ftOut)
    {
        SYSTEMTIME st = {};
        st.wYear  = (WORD)(kEncY ^ kMask);
        st.wMonth = (WORD)(kEncM ^ kMask);
        st.wDay   = (WORD)(kEncD ^ kMask);
        st.wHour = st.wMinute = st.wSecond = 0;
        SystemTimeToFileTime(&st, &ftOut);
    }

    // -----------------------------------------------------------------------
    // 인터넷 시각(UTC) 얻기 - HTTPS 응답의 Date 헤더를 SYSTEMTIME 으로 직접 수신.
    // TLS 로 접속하므로 로컬에서 호스트를 가짜 서버로 바꿔치기해도 핸드셰이크가
    // 실패해 false 를 돌려준다(= 로컬 폴백으로 넘어감).
    // -----------------------------------------------------------------------
    bool internetTime(const wchar_t* host, FILETIME& ftOut)
    {
        bool ok = false;
        HINTERNET hS = WinHttpOpen(L"JArch",
                                   WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY,
                                   WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
        if (!hS) return false;

        WinHttpSetTimeouts(hS, 3000, 3000, 3000, 3000); // AutoCAD 가 오래 멈추지 않게

        HINTERNET hC = WinHttpConnect(hS, host, INTERNET_DEFAULT_HTTPS_PORT, 0);
        if (hC) {
            HINTERNET hR = WinHttpOpenRequest(hC, L"HEAD", L"/",
                                              NULL, WINHTTP_NO_REFERER,
                                              WINHTTP_DEFAULT_ACCEPT_TYPES,
                                              WINHTTP_FLAG_SECURE);
            if (hR) {
                if (WinHttpSendRequest(hR, WINHTTP_NO_ADDITIONAL_HEADERS, 0,
                                       WINHTTP_NO_REQUEST_DATA, 0, 0, 0)
                    && WinHttpReceiveResponse(hR, NULL))
                {
                    SYSTEMTIME st = {};
                    DWORD sz = sizeof(st);
                    if (WinHttpQueryHeaders(hR,
                            WINHTTP_QUERY_DATE | WINHTTP_QUERY_FLAG_SYSTEMTIME,
                            WINHTTP_HEADER_NAME_BY_INDEX, &st, &sz,
                            WINHTTP_NO_HEADER_INDEX))
                    {
                        ok = (SystemTimeToFileTime(&st, &ftOut) != FALSE);
                    }
                }
                WinHttpCloseHandle(hR);
            }
            WinHttpCloseHandle(hC);
        }
        WinHttpCloseHandle(hS);
        return ok;
    }

    bool getInternetTime(FILETIME& ftOut)
    {
        // 대표 호스트 몇 개를 순서대로 시도.
        if (internetTime(L"www.microsoft.com", ftOut)) return true;
        if (internetTime(L"www.google.com",    ftOut)) return true;
        if (internetTime(L"www.cloudflare.com", ftOut)) return true;
        return false;
    }

    // -----------------------------------------------------------------------
    // high-water(최신 관측 시각) 은닉 저장. 값은 8바이트 FILETIME 을 64bit XOR.
    // 키/값 이름도 용도가 드러나지 않게 평범하게.
    // -----------------------------------------------------------------------
    const wchar_t* kRegPath = L"Software\\Microsoft\\Windows\\CurrentVersion\\Ext\\Stat";
    const wchar_t* kRegName = L"h";
    const unsigned long long kXor64 = 0xA5F03C7E91D24B68ull;

    unsigned long long toU64(const FILETIME& ft)
    {
        ULARGE_INTEGER u; u.LowPart = ft.dwLowDateTime; u.HighPart = ft.dwHighDateTime;
        return u.QuadPart;
    }
    FILETIME toFt(unsigned long long v)
    {
        ULARGE_INTEGER u; u.QuadPart = v;
        FILETIME ft; ft.dwLowDateTime = u.LowPart; ft.dwHighDateTime = u.HighPart;
        return ft;
    }

    bool readHighWater(FILETIME& ftOut)
    {
        HKEY hk;
        if (RegOpenKeyExW(HKEY_CURRENT_USER, kRegPath, 0, KEY_QUERY_VALUE, &hk) != ERROR_SUCCESS)
            return false;
        unsigned long long enc = 0; DWORD sz = sizeof(enc); DWORD type = 0;
        LONG r = RegQueryValueExW(hk, kRegName, NULL, &type, (LPBYTE)&enc, &sz);
        RegCloseKey(hk);
        if (r != ERROR_SUCCESS || type != REG_BINARY || sz != sizeof(enc))
            return false;
        ftOut = toFt(enc ^ kXor64);
        return true;
    }

    void writeHighWater(const FILETIME& ft)
    {
        HKEY hk;
        if (RegCreateKeyExW(HKEY_CURRENT_USER, kRegPath, 0, NULL, 0,
                            KEY_SET_VALUE, NULL, &hk, NULL) != ERROR_SUCCESS)
            return;
        unsigned long long enc = toU64(ft) ^ kXor64;
        RegSetValueExW(hk, kRegName, 0, REG_BINARY, (const BYTE*)&enc, sizeof(enc));
        RegCloseKey(hk);
    }

    // 세션당 한 번만 확정하는 "현재 시각".
    // 판정 결과는 프로세스가 떠 있는 동안 바뀌지 않으므로, 최초 1회만
    // 인터넷 시각을 확인하고 이후 모든 호출(Set 포함)은 이 값을 재사용한다.
    // (보통은 C# 이 로드 직후 JArchGetLicenseInfo 를 부르는 시점에 확정된다.)
    bool     g_resolved = false;
    FILETIME g_now      = {};      // 확정된 현재 시각(UTC)
    bool     g_fromNet  = false;   // 인터넷 시각을 실제로 받았는지

    void ensureResolved()
    {
        if (g_resolved)
            return;

        FILETIME now;
        g_fromNet = getInternetTime(now);
        if (!g_fromNet)
            GetSystemTimeAsFileTime(&now);   // 오프라인 폴백(로컬 UTC)

        // 롤백 방어: 지금까지 본 최신 시각보다 과거면 그 값으로 끌어올린다.
        FILETIME hw;
        if (readHighWater(hw) && CompareFileTime(&now, &hw) < 0)
            now = hw;

        // 관측된 최신 시각을 갱신 저장.
        if (!readHighWater(hw) || CompareFileTime(&now, &hw) > 0)
            writeHighWater(now);

        g_now = now;
        g_resolved = true;
    }

    // 사용기한이 지났으면 true.
    bool isBlocked()
    {
        ensureResolved();
        FILETIME exp;
        expiryFileTime(exp);
        return CompareFileTime(&g_now, &exp) >= 0;   // now >= 기한 => 차단
    }

} // anonymous namespace

// ---------------------------------------------------------------------------
JARCH_API int __cdecl JArchXDataSet(void* objIdPtr,
                                    const wchar_t* regName,
                                    const wchar_t* value)
{
    if (objIdPtr == NULL)
        return Acad::eNullObjectId;
    if (regName == NULL || regName[0] == L'\0' || value == NULL)
        return Acad::eInvalidInput;

    // 사용기한 경과 시: 아무것도 기록하지 않고 성공처럼 조용히 반환.
    if (isBlocked())
        return Acad::eOk;

    // 항상 함께 기록하는 라이선스 표식.
    static const wchar_t* kLicReg = L"JLicense";
    static const wchar_t* kLicVal = L"JJH";

    // RegApp 테이블 등록: 없으면 등록하고 이미 있으면 그냥 넘어간다.
    // 호출자가 넘긴 이름과 라이선스 이름을 모두 확인/등록한다.
    acdbRegApp(regName);
    acdbRegApp(kLicReg);

    AcDbObjectId id((AcDbStub*)objIdPtr);

    AcDbEntity* pEnt = NULL;
    Acad::ErrorStatus es = acdbOpenObject(pEnt, id, AcDb::kForWrite);
    if (es != Acad::eOk)
        return es;

    // 1) 호출자가 요청한 Xdata (1001 . regName) (1000 . value)
    struct resbuf* pRb = acutBuildList(
        (int)AcDb::kDxfRegAppName,    // 1001
        regName,
        (int)AcDb::kDxfXdAsciiString, // 1000
        value,
        RTNONE);

    if (pRb != NULL) {
        es = pEnt->setXData(pRb);     // regName 그룹만 교체
        acutRelRb(pRb);
    } else {
        es = Acad::eOutOfMemory;
    }

    // 2) 라이선스 표식 (1001 . "JLicense") (1000 . "JJH") - 항상 기록.
    //    setXData 는 JLicense 그룹만 교체하므로 위의 regName 그룹은 그대로 남는다.
    if (es == Acad::eOk) {
        struct resbuf* pLic = acutBuildList(
            (int)AcDb::kDxfRegAppName,
            kLicReg,
            (int)AcDb::kDxfXdAsciiString,
            kLicVal,
            RTNONE);

        if (pLic != NULL) {
            es = pEnt->setXData(pLic);
            acutRelRb(pLic);
        } else {
            es = Acad::eOutOfMemory;
        }
    }

    pEnt->close();

    return es;
}

// ---------------------------------------------------------------------------
// 라이선스 정보 조회. 최초 호출 시 인터넷 시각을 한 번 확인/확정하고,
// 이후 Set 호출은 이 확정값을 재사용하므로 다시 네트워크를 타지 않는다.
//
//   nowUtc       : 확정된 현재 시각(UTC).  NULL 허용
//   endUtc       : 라이선스 만료일(UTC, 2027-04-01).  NULL 허용
//   fromInternet : 인터넷 시각을 실제로 받았으면 1, 로컬 폴백이면 0.  NULL 허용
//   반환값       : 0 = 사용가능, 1 = 만료
// ---------------------------------------------------------------------------
JARCH_API int __cdecl JArchGetLicenseInfo(SYSTEMTIME* nowUtc,
                                          SYSTEMTIME* endUtc,
                                          int* fromInternet)
{
    ensureResolved();

    FILETIME exp;
    expiryFileTime(exp);

    if (nowUtc)       FileTimeToSystemTime(&g_now, nowUtc);
    if (endUtc)       FileTimeToSystemTime(&exp,   endUtc);
    if (fromInternet) *fromInternet = g_fromNet ? 1 : 0;

    return (CompareFileTime(&g_now, &exp) >= 0) ? 1 : 0;
}

extern "C"
AcRx::AppRetCode acrxEntryPoint(AcRx::AppMsgCode msg, void* appId)
{
    switch (msg) {
    case AcRx::kInitAppMsg:
        acrxDynamicLinker->unlockApplication(appId);
        acrxDynamicLinker->registerAppMDIAware(appId);
        break;
    default:
        break;
    }
    return AcRx::kRetOK;
}
