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
//   사용기한 판정 : Supabase 의 check_license_status RPC 가 전담한다.
//                   이 PC 의 고유 ID(JArchSbLicense 의 GetMachineId 와 동일
//                   알고리즘)를 보내고 'NONE' / 'YYYY-MM-DD|Y' / 'YYYY-MM-DD|N'
//                   을 받는다. 판정이 서버에서 이뤄지므로 로컬 시계를 돌려도
//                   무의미하다. 조회 실패는 전부 차단이다 - fail-closed.
//                   현재 시각은 같은 응답의 Date 헤더에서 함께 얻는다.
//

#include <rxregsvc.h>
#include <aced.h>
#include <dbmain.h>
#include <dbents.h>
#include <adslib.h>
#include <tchar.h>

#include <windows.h>
#include <winhttp.h>
#include <bcrypt.h>
#include <stdio.h>
#include <stdlib.h>
#include <string>
#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "bcrypt.lib")

#define JARCH_API extern "C" __declspec(dllexport)

// ---------------------------------------------------------------------------
// 사용기한은 Supabase 가 갖고 있다. 이 파일에 날짜 상수는 없다.
// ---------------------------------------------------------------------------
namespace {

    const wchar_t* kSbHost       = L"bvgpukvuygluxternzig.supabase.co";
    const wchar_t* kPathStatus   = L"/rest/v1/rpc/check_license_status";
    const wchar_t* kPathRegister = L"/rest/v1/rpc/register_license";
    const DWORD    kTimeoutMs    = 3000;   // AutoCAD 가 오래 멈추지 않게

    // anon 키. 노출 전제 키이며 RLS 로 보호된다. 이 키로 할 수 있는 것은
    // check_license_status / register_license RPC 호출뿐이고, 테이블 직접
    // 조회는 SELECT 정책이 없어 항상 빈 배열이다.
    // service_role 키는 절대 여기에 넣지 않는다.
    const char* kAnonKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSI"
        "sInJlZiI6ImJ2Z3B1a3Z1eWdsdXh0ZXJuemlnIiwicm9sZSI6ImFub24iLCJ"
        "pYXQiOjE3ODgyNTQwNDgsImV4cCI6MjEwMzgzMDA0OH0.U_FGwTTmeDovDAl"
        "MIQf-sFtid6HLuekMt72hiOmaj2g";

    // -----------------------------------------------------------------------
    // 컴퓨터 고유 ID - JArchSbLicense.dll 의 GetMachineId 와 완전히 같은 값을
    // 만들어야 한다. 알고리즘을 한 글자라도 바꾸면 등록된 모든 PC 가 미등록이
    // 된다. 규칙: 대문자 MachineGuid + '|' + 대문자 SMBIOS UUID 를 SHA-256 하고
    // 대문자 hex 앞 20자를 4자씩 끊는다. UUID 불량 기종은 'M-' 접두어.
    // -----------------------------------------------------------------------
    bool readMachineGuid(std::string& out)
    {
        wchar_t wbuf[64];
        DWORD cb = sizeof(wbuf);
        if (RegGetValueW(HKEY_LOCAL_MACHINE,
                         L"SOFTWARE\\Microsoft\\Cryptography",
                         L"MachineGuid",
                         RRF_RT_REG_SZ | RRF_SUBKEY_WOW6464KEY,
                         NULL, wbuf, &cb) != ERROR_SUCCESS)
            return false;

        std::string s;
        for (const wchar_t* p = wbuf; *p; ++p) {
            if (*p > 0x7F)
                return false;
            s += (char)((*p >= L'a' && *p <= L'z') ? *p - 32 : *p);
        }
        if (s.empty())
            return false;
        out = s;
        return true;
    }

    bool readSmbiosUuid(std::string& out)
    {
        const DWORD RSMB = 'RSMB';
        UINT size = GetSystemFirmwareTable(RSMB, 0, NULL, 0);
        if (size < 8)
            return false;

        std::string raw(size, '\0');
        if (GetSystemFirmwareTable(RSMB, 0, &raw[0], size) != size)
            return false;

        // RawSMBIOSData 헤더 8바이트 뒤부터가 구조체 테이블이다.
        const BYTE* base = (const BYTE*)raw.data();
        DWORD tableLen = *(const DWORD*)(base + 4);
        if (tableLen > size - 8)
            tableLen = size - 8;

        const BYTE* t   = base + 8;
        const BYTE* end = t + tableLen;

        while (t + 4 <= end) {
            const BYTE type = t[0];
            const BYTE len  = t[1];
            if (len < 4 || t + len > end)
                break;

            if (type == 1 && len >= 0x19) {       // UUID 는 offset 0x08, 16바이트
                const BYTE* u = t + 0x08;
                bool allZero = true, allFF = true;
                for (int i = 0; i < 16; ++i) {
                    if (u[i] != 0x00) allZero = false;
                    if (u[i] != 0xFF) allFF   = false;
                }
                if (allZero || allFF)             // OEM 미설정 -> 폴백
                    return false;

                char b[40];
                sprintf_s(b, sizeof(b),
                          "%02X%02X%02X%02X-%02X%02X-%02X%02X-%02X%02X-%02X%02X%02X%02X%02X%02X",
                          u[3], u[2], u[1], u[0], u[5], u[4], u[7], u[6],
                          u[8], u[9], u[10], u[11], u[12], u[13], u[14], u[15]);
                out = b;
                return true;
            }

            // 형식 영역 뒤의 문자열 집합은 이중 NUL 로 끝난다.
            const BYTE* s = t + len;
            while (s + 1 < end && !(s[0] == 0 && s[1] == 0))
                ++s;
            t = s + 2;
        }
        return false;
    }

    bool sha256Hex(const std::string& in, std::string& out)
    {
        BCRYPT_ALG_HANDLE alg = NULL;
        if (!BCRYPT_SUCCESS(BCryptOpenAlgorithmProvider(&alg, BCRYPT_SHA256_ALGORITHM, NULL, 0)))
            return false;

        BYTE hash[32];
        NTSTATUS st = BCryptHash(alg, NULL, 0, (PUCHAR)in.data(), (ULONG)in.size(),
                                 hash, sizeof(hash));
        BCryptCloseAlgorithmProvider(alg, 0);
        if (!BCRYPT_SUCCESS(st))
            return false;

        out.clear();
        for (int i = 0; i < 32; ++i) {
            char b[3];
            sprintf_s(b, sizeof(b), "%02X", hash[i]);
            out += b;
        }
        return true;
    }

    bool makeMachineId(std::string& out)
    {
        std::string guid;
        if (!readMachineGuid(guid))     // 없으면 추측해서 만들지 않는다
            return false;

        std::string uuid;
        const bool hasUuid = readSmbiosUuid(uuid);

        std::string hex;
        if (!sha256Hex(hasUuid ? (guid + "|" + uuid) : guid, hex))
            return false;

        out = hasUuid ? "" : "M-";
        for (int i = 0; i < 20; ++i) {
            if (i > 0 && i % 4 == 0)
                out += '-';
            out += hex[i];
        }
        return true;
    }

    // -----------------------------------------------------------------------
    // JSON 문자열 리터럴용 이스케이프. 사용자명/회사명에 한글·따옴표가 섞여도
    // 본문이 깨지지 않게 한다. 입력은 UTF-8 바이트열이어야 한다.
    // -----------------------------------------------------------------------
    std::string jsonEscape(const std::string& s)
    {
        std::string out;
        for (size_t i = 0; i < s.size(); ++i) {
            const unsigned char c = (unsigned char)s[i];
            switch (c) {
            case '"':  out += "\\\""; break;
            case '\\': out += "\\\\"; break;
            case '\b': out += "\\b";  break;
            case '\f': out += "\\f";  break;
            case '\n': out += "\\n";  break;
            case '\r': out += "\\r";  break;
            case '\t': out += "\\t";  break;
            default:
                if (c < 0x20) {
                    char b[7];
                    sprintf_s(b, sizeof(b), "\\u%04x", c);
                    out += b;
                } else {
                    out += (char)c;
                }
            }
        }
        return out;
    }

    // wide -> UTF-8. NULL/빈 문자열은 빈 문자열로.
    std::string toUtf8(const wchar_t* w)
    {
        if (w == NULL || w[0] == L'\0')
            return std::string();
        int n = WideCharToMultiByte(CP_UTF8, 0, w, -1, NULL, 0, NULL, NULL);
        if (n <= 1)
            return std::string();
        std::string s((size_t)(n - 1), '\0');
        WideCharToMultiByte(CP_UTF8, 0, w, -1, &s[0], n, NULL, NULL);
        return s;
    }

    // -----------------------------------------------------------------------
    // Supabase RPC 호출. 응답 본문(스칼라 JSON 문자열)과, 있으면 응답 Date
    // 헤더의 UTC 시각을 함께 돌려준다.
    // TLS 로 접속하므로 hosts 로 가짜 서버를 물려도 핸드셰이크에서 실패한다.
    // -----------------------------------------------------------------------
    bool supabaseRpc(const wchar_t* path, const std::string& body,
                     std::string& respOut, FILETIME* nowOut)
    {
        bool ok = false;
        respOut.clear();

        HINTERNET hS = WinHttpOpen(L"JArchXData",
                                   WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY,
                                   WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
        if (!hS)
            return false;
        WinHttpSetTimeouts(hS, kTimeoutMs, kTimeoutMs, kTimeoutMs, kTimeoutMs);

        HINTERNET hC = WinHttpConnect(hS, kSbHost, INTERNET_DEFAULT_HTTPS_PORT, 0);
        if (hC) {
            HINTERNET hR = WinHttpOpenRequest(hC, L"POST", path, NULL,
                                              WINHTTP_NO_REFERER,
                                              WINHTTP_DEFAULT_ACCEPT_TYPES,
                                              WINHTTP_FLAG_SECURE);
            if (hR) {
                const std::string  key(kAnonKey);
                const std::wstring wkey(key.begin(), key.end());   // 키는 ASCII
                const std::wstring headers =
                    L"apikey: " + wkey + L"\r\n" +
                    L"Authorization: Bearer " + wkey + L"\r\n" +
                    L"Content-Type: application/json\r\n";

                if (WinHttpSendRequest(hR, headers.c_str(), (DWORD)headers.length(),
                                       (LPVOID)body.c_str(), (DWORD)body.length(),
                                       (DWORD)body.length(), 0)
                    && WinHttpReceiveResponse(hR, NULL))
                {
                    DWORD status = 0, sz = sizeof(status);
                    if (WinHttpQueryHeaders(hR,
                            WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
                            WINHTTP_HEADER_NAME_BY_INDEX, &status, &sz,
                            WINHTTP_NO_HEADER_INDEX) && status == 200)
                    {
                        // 현재 시각도 같은 응답에서 얻는다(별도 왕복이 필요없다).
                        if (nowOut) {
                            SYSTEMTIME st = {};
                            DWORD dsz = sizeof(st);
                            if (WinHttpQueryHeaders(hR,
                                    WINHTTP_QUERY_DATE | WINHTTP_QUERY_FLAG_SYSTEMTIME,
                                    WINHTTP_HEADER_NAME_BY_INDEX, &st, &dsz,
                                    WINHTTP_NO_HEADER_INDEX))
                                SystemTimeToFileTime(&st, nowOut);
                        }

                        ok = true;
                        for (;;) {
                            DWORD avail = 0;
                            if (!WinHttpQueryDataAvailable(hR, &avail)) { ok = false; break; }
                            if (avail == 0)
                                break;
                            // 스칼라 문자열이 정상 응답이다. 그보다 크면 응답으로 보지 않는다.
                            if (respOut.size() + avail > 256) { ok = false; break; }
                            char buf[256];
                            DWORD read = 0;
                            if (!WinHttpReadData(hR, buf, avail, &read)) { ok = false; break; }
                            if (read == 0)
                                break;
                            respOut.append(buf, read);
                        }
                    }
                }
                WinHttpCloseHandle(hR);
            }
            WinHttpCloseHandle(hC);
        }
        WinHttpCloseHandle(hS);
        return ok;
    }

    // 응답은 JSON 스칼라 문자열이라 따옴표에 싸여 온다. 벗겨서 알맹이만.
    std::string unquote(const std::string& s)
    {
        size_t b = s.find_first_not_of(" \t\r\n");
        if (b == std::string::npos)
            return std::string();
        size_t e = s.find_last_not_of(" \t\r\n");
        std::string t = s.substr(b, e - b + 1);
        if (t.size() >= 2 && t[0] == '"' && t[t.size() - 1] == '"')
            t = t.substr(1, t.size() - 2);
        return t;
    }

    // "YYYY-MM-DD" -> UTC 자정 FILETIME
    bool parseDate(const std::string& s, FILETIME& ftOut)
    {
        if (s.size() < 10 || s[4] != '-' || s[7] != '-')
            return false;
        SYSTEMTIME st = {};
        st.wYear  = (WORD)atoi(s.substr(0, 4).c_str());
        st.wMonth = (WORD)atoi(s.substr(5, 2).c_str());
        st.wDay   = (WORD)atoi(s.substr(8, 2).c_str());
        if (st.wYear < 1970 || st.wMonth < 1 || st.wMonth > 12 ||
            st.wDay < 1 || st.wDay > 31)
            return false;
        return SystemTimeToFileTime(&st, &ftOut) != FALSE;
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

    // 세션당 한 번만 확정하는 라이선스 상태.
    // 판정 결과는 프로세스가 떠 있는 동안 바뀌지 않으므로, 최초 1회만 서버에
    // 묻고 이후 모든 호출(Set 포함)은 이 값을 재사용한다.
    // (보통은 C# 이 로드 직후 JArchGetLicenseInfo 를 부르는 시점에 확정된다.)
    //
    // 상태값: 0 = 사용가능 / 1 = 만료 / 2 = 미등록 / 3 = 조회실패
    bool        g_resolved = false;
    int         g_status   = 3;    // 확정 전 기본값은 차단이다 (fail-closed)
    FILETIME    g_now      = {};   // 확정된 현재 시각(UTC)
    FILETIME    g_expiry   = {};   // 서버가 준 만료일(UTC 자정)
    bool        g_fromNet  = false;
    std::string g_comId;           // 이 PC 의 고유 ID

    void ensureResolved()
    {
        if (g_resolved)
            return;
        g_resolved = true;                   // 실패해도 매번 재시도하지 않는다

        GetSystemTimeAsFileTime(&g_now);     // 서버 응답을 못 받았을 때의 표시용

        if (!makeMachineId(g_comId)) {
            g_status = 3;                    // ID 를 못 만들면 조회 자체가 불가
            return;
        }

        std::string resp;
        FILETIME    srvNow = {};
        const std::string body =
            std::string("{\"p_com_id\":\"") + jsonEscape(g_comId) + "\"}";

        if (!supabaseRpc(kPathStatus, body, resp, &srvNow)) {
            g_status = 3;                    // 네트워크/서버 오류 -> 차단
            return;
        }

        g_fromNet = true;
        if (srvNow.dwHighDateTime != 0 || srvNow.dwLowDateTime != 0)
            g_now = srvNow;

        // 롤백 방어: 지금까지 본 최신 시각보다 과거면 그 값으로 끌어올린다.
        FILETIME hw;
        if (readHighWater(hw) && CompareFileTime(&g_now, &hw) < 0)
            g_now = hw;
        if (!readHighWater(hw) || CompareFileTime(&g_now, &hw) > 0)
            writeHighWater(g_now);

        // 'NONE' 또는 'YYYY-MM-DD|Y' / 'YYYY-MM-DD|N'
        const std::string s = unquote(resp);
        if (s == "NONE") {
            g_status = 2;                    // 등록된 사용자가 아니다
        } else if (s.size() >= 12 && s[10] == '|' && parseDate(s, g_expiry)) {
            g_status = (s[11] == 'Y') ? 0 : 1;
        } else {
            g_status = 3;                    // 알 수 없는 응답 -> 차단
        }
    }

    // 사용할 수 없으면 true. 만료·미등록·조회실패가 전부 여기 걸린다.
    bool isBlocked()
    {
        ensureResolved();
        return g_status != 0;
    }

    // -----------------------------------------------------------------------
    // 이미 열려 있는(kForWrite) 엔티티에 Xdata 를 기록하는 공통 루틴.
    // 여는 것도 닫는 것도 하지 않는다 - 소유권은 호출자에게 있다.
    // -----------------------------------------------------------------------
    Acad::ErrorStatus writeXData(AcDbEntity* pEnt,
                                 const wchar_t* regName,
                                 const wchar_t* value)
    {
        // 항상 함께 기록하는 라이선스 표식.
        static const wchar_t* kLicReg = L"JLicense";
        static const wchar_t* kLicVal = L"JJH";

        // RegApp 테이블 등록: 없으면 등록하고 이미 있으면 그냥 넘어간다.
        // 호출자가 넘긴 이름과 라이선스 이름을 모두 확인/등록한다.
        acdbRegApp(regName);
        acdbRegApp(kLicReg);

        // 1) 호출자가 요청한 Xdata (1001 . regName) (1000 . value)
        Acad::ErrorStatus es;
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

        return es;
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

    AcDbObjectId id((AcDbStub*)objIdPtr);

    AcDbEntity* pEnt = NULL;
    Acad::ErrorStatus es = acdbOpenObject(pEnt, id, AcDb::kForWrite);
    if (es != Acad::eOk)
        return es;

    es = writeXData(pEnt, regName, value);

    pEnt->close();

    return es;
}

// ---------------------------------------------------------------------------
// 이미 열려 있는 엔티티에 직접 기록하는 변형.
//
//   pEntPtr : C# 의 DBObject.UnmanagedObject (= AcDbEntity*)
//
// .NET Transaction 안에서 GetObject(ForWrite)/UpgradeOpen 으로 이미 열어 둔
// 객체는 acdbOpenObject 로 다시 열 수 없다(eWasOpenForWrite). 그런 호출자를
// 위해 열기/닫기 없이 setXData 만 수행한다. 열기 상태 보장은 호출자 책임.
// DB 에 아직 추가되지 않은(ObjectId 가 Null 인) 신규 엔티티에도 쓸 수 있다.
// ---------------------------------------------------------------------------
JARCH_API int __cdecl JArchXDataSetEnt(void* pEntPtr,
                                       const wchar_t* regName,
                                       const wchar_t* value)
{
    if (pEntPtr == NULL)
        return Acad::eNullObjectPointer;
    if (regName == NULL || regName[0] == L'\0' || value == NULL)
        return Acad::eInvalidInput;

    // 사용기한 경과 시: 아무것도 기록하지 않고 성공처럼 조용히 반환.
    if (isBlocked())
        return Acad::eOk;

    return writeXData((AcDbEntity*)pEntPtr, regName, value);
}

// ---------------------------------------------------------------------------
// 라이선스 정보 조회. 최초 호출 시 서버에 한 번 묻고 확정하며, 이후 Set 호출은
// 이 확정값을 재사용하므로 다시 네트워크를 타지 않는다.
//
//   nowUtc       : 확정된 현재 시각(UTC).  NULL 허용
//   endUtc       : 서버가 준 만료일(UTC). 미등록/조회실패면 0.  NULL 허용
//   fromInternet : 서버 응답을 실제로 받았으면 1.  NULL 허용
//   반환값       : 0 = 사용가능, 1 = 만료, 2 = 미등록, 3 = 조회실패
// ---------------------------------------------------------------------------
JARCH_API int __cdecl JArchGetLicenseInfo(SYSTEMTIME* nowUtc,
                                          SYSTEMTIME* endUtc,
                                          int* fromInternet)
{
    ensureResolved();

    if (nowUtc)
        FileTimeToSystemTime(&g_now, nowUtc);

    if (endUtc) {
        if (g_status == 0 || g_status == 1)
            FileTimeToSystemTime(&g_expiry, endUtc);
        else
            ZeroMemory(endUtc, sizeof(*endUtc));   // 보여줄 만료일이 없다
    }

    if (fromInternet)
        *fromInternet = g_fromNet ? 1 : 0;

    return g_status;
}

// ---------------------------------------------------------------------------
// 이 PC 의 고유 ID 를 buf 에 NUL 종단 wide 문자열로 기록한다.
// JArchSbLicense.dll 의 GetMachineId 와 같은 값이다.
//   cch    : buf 의 문자 수. 32 이상이어야 한다
//   반환값 : 1 = 성공, 0 = 실패(buf 는 빈 문자열)
// ---------------------------------------------------------------------------
JARCH_API int __cdecl JArchGetMachineId(wchar_t* buf, int cch)
{
    if (buf == NULL || cch < 32)
        return 0;
    buf[0] = L'\0';

    std::string id;
    if (!makeMachineId(id))
        return 0;

    // ID 는 ASCII(hex + 하이픈)라 단순 확장으로 충분하다.
    int i = 0;
    for (; i < (int)id.size() && i < cch - 1; ++i)
        buf[i] = (wchar_t)(unsigned char)id[i];
    buf[i] = L'\0';
    return 1;
}

// ---------------------------------------------------------------------------
// 자가 등록. 체험 기간 만료일은 서버가 정한다(클라이언트가 지정할 수 없다).
//
//   userName/compName/partName : 비워도 된다(NULL 허용)
//   outExp / cch : 성공 시 "YYYY-MM-DD" 를 받는다. NULL 허용
//   반환값       : 0 = 등록됨, 1 = 이미 등록된 PC, 2 = ID 형식 오류,
//                  3 = 통신 실패
//
// 등록에 성공하면 확정 상태를 무효화해 다음 조회에서 다시 묻게 한다.
// ---------------------------------------------------------------------------
JARCH_API int __cdecl JArchRegisterLicense(const wchar_t* userName,
                                           const wchar_t* compName,
                                           const wchar_t* partName,
                                           wchar_t* outExp,
                                           int cch)
{
    if (outExp != NULL && cch > 0)
        outExp[0] = L'\0';

    std::string id;
    if (!makeMachineId(id))
        return 3;

    const std::string body =
        std::string("{\"p_com_id\":\"")    + jsonEscape(id)                  +
        "\",\"p_user_name\":\"" + jsonEscape(toUtf8(userName)) +
        "\",\"p_comp_name\":\"" + jsonEscape(toUtf8(compName)) +
        "\",\"p_part_name\":\"" + jsonEscape(toUtf8(partName)) + "\"}";

    std::string resp;
    if (!supabaseRpc(kPathRegister, body, resp, NULL))
        return 3;

    const std::string s = unquote(resp);

    if (s == "DUP")
        return 1;
    if (s == "BAD")
        return 2;

    // "OK|YYYY-MM-DD"
    if (s.size() >= 13 && s.compare(0, 3, "OK|") == 0) {
        const std::string d = s.substr(3);
        if (outExp != NULL && cch > (int)d.size()) {
            int i = 0;
            for (; i < (int)d.size(); ++i)
                outExp[i] = (wchar_t)(unsigned char)d[i];
            outExp[i] = L'\0';
        }
        g_resolved = false;      // 다음 조회에서 다시 묻는다
        return 0;
    }

    return 3;                    // 알 수 없는 응답
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
