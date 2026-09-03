#include "JArchSbLicense.h"

#include <windows.h>
#include <winhttp.h>
#include <stdio.h>
#include <string>

#include <bcrypt.h>

#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "bcrypt.lib")
#pragma comment(lib, "advapi32.lib")   // RegGetValueW

namespace
{
    const wchar_t* SUPABASE_HOST = L"bvgpukvuygluxternzig.supabase.co";
    const wchar_t* RPC_PATH      = L"/rest/v1/rpc/check_license";

    // anon 키. 노출 전제 키이며 RLS 로 보호된다 (prd.md §3.2).
    // public.licenses 는 정책이 0건이라 직접 조회는 항상 빈 배열이고,
    // 이 키로 할 수 있는 것은 check_license RPC 호출 하나뿐이다.
    // service_role / sb_secret_ 키는 절대 여기에 넣지 않는다.
    const char* SUPABASE_ANON_KEY =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSI"
        "sInJlZiI6ImJ2Z3B1a3Z1eWdsdXh0ZXJuemlnIiwicm9sZSI6ImFub24iLCJ"
        "pYXQiOjE3ODgyNTQwNDgsImV4cCI6MjEwMzgzMDA0OH0.U_FGwTTmeDovDAl"
        "MIQf-sFtid6HLuekMt72hiOmaj2g";

    const DWORD TIMEOUT_MS = 5000;   // 연결 5초 / 읽기 5초 (prd.md §5.1)

    // comID 를 JSON 문자열 리터럴 안에 넣을 수 있게 이스케이프한다.
    std::string JsonEscape(const char* s)
    {
        std::string out;
        for (const unsigned char* p = (const unsigned char*)s; *p; ++p)
        {
            switch (*p)
            {
            case '"':  out += "\\\""; break;
            case '\\': out += "\\\\"; break;
            case '\b': out += "\\b";  break;
            case '\f': out += "\\f";  break;
            case '\n': out += "\\n";  break;
            case '\r': out += "\\r";  break;
            case '\t': out += "\\t";  break;
            default:
                if (*p < 0x20)
                {
                    char buf[7];
                    sprintf_s(buf, sizeof(buf), "\\u%04x", *p);
                    out += buf;
                }
                else
                {
                    out += (char)*p;
                }
            }
        }
        return out;
    }

    // 앞뒤 공백/개행 제거. PostgREST 스칼라 응답에 개행이 붙어 와도 되게.
    std::string Trim(const std::string& s)
    {
        size_t b = s.find_first_not_of(" \t\r\n");
        if (b == std::string::npos)
            return std::string();
        size_t e = s.find_last_not_of(" \t\r\n");
        return s.substr(b, e - b + 1);
    }

    // ── 컴퓨터 고유 ID ────────────────────────────────────────────────
    //
    // MachineGuid 와 SMBIOS UUID 를 이어 붙여 SHA-256 으로 해싱한다.
    // 둘은 약점이 서로 반대다. MachineGuid 는 OS 재설치로 바뀌지만 하드웨어를
    // 바꿔도 그대로고, SMBIOS UUID 는 메인보드를 바꾸면 달라지지만 OS 를 밀어도
    // 그대로다. 섞으면 한쪽이 부실한 기종에서도 유일성이 남는다.
    //
    // ⚠ 아래 입력 규칙은 한 번 정하면 바꿀 수 없다. 바꾸는 순간 이미 등록된
    //   모든 PC 의 ID 가 달라져 전부 재등록해야 한다. 확정 사항:
    //     대소문자   두 값 모두 대문자
    //     구분자     '|' 한 글자
    //     GUID 형식  하이픈 포함 원문 그대로
    //     해시       SHA-256, 출력은 대문자 hex 64자 중 앞 20자
    //     포맷       4자씩 끊어 하이픈으로 연결
    const int MACHINE_ID_HEX_CHARS = 20;

    // HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid — OS 설치 때 생성된다.
    // RRF_SUBKEY_WOW6464KEY 를 명시한다. x64 빌드에서는 기본값과 같지만,
    // 32비트로 잘못 빌드하면 WOW6432Node 를 보게 되어 "값 없음" 으로 오진한다.
    bool ReadMachineGuid(std::string& out)
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
        for (const wchar_t* p = wbuf; *p; ++p)
        {
            if (*p > 0x7F)              // GUID 는 ASCII 다. 아니면 신뢰하지 않는다
                return false;
            s += (char)((*p >= 'a' && *p <= 'z') ? *p - 32 : *p);
        }
        if (s.empty())
            return false;

        out = s;
        return true;
    }

    // SMBIOS Type 1 (System Information) 구조체의 UUID.
    // 앞 세 필드를 리틀엔디언으로 읽어 표준 GUID 형식으로 만든다 — SMBIOS 2.6
    // 이후 규정이며, WMI Win32_ComputerSystemProduct.UUID 와 같은 값이 나온다.
    // (관리자가 PowerShell 로 대조할 수 있어야 디버깅이 된다.)
    bool ReadSmbiosUuid(std::string& out)
    {
        const DWORD RSMB = 'RSMB';

        UINT size = GetSystemFirmwareTable(RSMB, 0, NULL, 0);
        if (size < 8)
            return false;

        std::string raw(size, '\0');
        if (GetSystemFirmwareTable(RSMB, 0, &raw[0], size) != size)
            return false;

        // RawSMBIOSData 헤더 8바이트(Used20CallingMethod, Major, Minor,
        // DmiRevision, Length) 뒤부터가 구조체 테이블이다.
        const BYTE* base = (const BYTE*)raw.data();
        DWORD tableLen = *(const DWORD*)(base + 4);
        if (tableLen > size - 8)
            tableLen = size - 8;

        const BYTE* t   = base + 8;
        const BYTE* end = t + tableLen;

        while (t + 4 <= end)
        {
            const BYTE type = t[0];
            const BYTE len  = t[1];
            if (len < 4 || t + len > end)
                break;

            if (type == 1 && len >= 0x19)       // UUID 는 offset 0x08, 16바이트
            {
                const BYTE* u = t + 0x08;

                bool allZero = true, allFF = true;
                for (int i = 0; i < 16; ++i)
                {
                    if (u[i] != 0x00) allZero = false;
                    if (u[i] != 0xFF) allFF   = false;
                }
                if (allZero || allFF)           // OEM 미설정 → 폴백
                    return false;

                char b[40];
                sprintf_s(b, sizeof(b),
                          "%02X%02X%02X%02X-%02X%02X-%02X%02X-%02X%02X-%02X%02X%02X%02X%02X%02X",
                          u[3], u[2], u[1], u[0],
                          u[5], u[4],
                          u[7], u[6],
                          u[8], u[9],
                          u[10], u[11], u[12], u[13], u[14], u[15]);
                out = b;
                return true;
            }

            // 형식 영역 뒤에 문자열 집합이 붙고 이중 NUL 로 끝난다.
            const BYTE* s = t + len;
            while (s + 1 < end && !(s[0] == 0 && s[1] == 0))
                ++s;
            t = s + 2;
        }
        return false;
    }

    bool Sha256Hex(const std::string& in, std::string& out)
    {
        BCRYPT_ALG_HANDLE alg = NULL;
        if (!BCRYPT_SUCCESS(BCryptOpenAlgorithmProvider(&alg, BCRYPT_SHA256_ALGORITHM, NULL, 0)))
            return false;

        BYTE hash[32];
        NTSTATUS st = BCryptHash(alg, NULL, 0,
                                 (PUCHAR)in.data(), (ULONG)in.size(),
                                 hash, sizeof(hash));
        BCryptCloseAlgorithmProvider(alg, 0);
        if (!BCRYPT_SUCCESS(st))
            return false;

        out.clear();
        for (int i = 0; i < 32; ++i)
        {
            char b[3];
            sprintf_s(b, sizeof(b), "%02X", hash[i]);
            out += b;
        }
        return true;
    }
}

extern "C"
{
    JARCHSBLICENSE_API int CheckLicenseOnline(const char* comID)
    {
        if (comID == NULL || *comID == '\0')
            return 0;

        HINTERNET hSession = NULL;
        HINTERNET hConnect = NULL;
        HINTERNET hRequest = NULL;
        int result = 0;                 // 어떤 경로로 실패해도 차단이 기본값

        const std::string body = std::string("{\"p_com_id\":\"") + JsonEscape(comID) + "\"}";

        // 헤더는 wide 문자열이어야 한다. anon 키는 ASCII 라 단순 확장으로 충분.
        const std::string key(SUPABASE_ANON_KEY);
        const std::wstring wkey(key.begin(), key.end());
        const std::wstring headers =
            L"apikey: " + wkey + L"\r\n" +
            L"Authorization: Bearer " + wkey + L"\r\n" +
            L"Content-Type: application/json\r\n";

        hSession = WinHttpOpen(L"JArchSbLicense/1.0",
                               WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY,
                               WINHTTP_NO_PROXY_NAME,
                               WINHTTP_NO_PROXY_BYPASS,
                               0);
        if (hSession == NULL)
            goto cleanup;

        // 해석 / 연결 / 전송 / 수신
        WinHttpSetTimeouts(hSession, TIMEOUT_MS, TIMEOUT_MS, TIMEOUT_MS, TIMEOUT_MS);

        hConnect = WinHttpConnect(hSession, SUPABASE_HOST, INTERNET_DEFAULT_HTTPS_PORT, 0);
        if (hConnect == NULL)
            goto cleanup;

        hRequest = WinHttpOpenRequest(hConnect, L"POST", RPC_PATH,
                                      NULL, WINHTTP_NO_REFERER,
                                      WINHTTP_DEFAULT_ACCEPT_TYPES,
                                      WINHTTP_FLAG_SECURE);
        if (hRequest == NULL)
            goto cleanup;

        if (!WinHttpSendRequest(hRequest,
                                headers.c_str(), (DWORD)headers.length(),
                                (LPVOID)body.c_str(), (DWORD)body.length(),
                                (DWORD)body.length(), 0))
            goto cleanup;

        if (!WinHttpReceiveResponse(hRequest, NULL))
            goto cleanup;

        {
            DWORD status = 0;
            DWORD statusSize = sizeof(status);
            if (!WinHttpQueryHeaders(hRequest,
                                     WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
                                     WINHTTP_HEADER_NAME_BY_INDEX,
                                     &status, &statusSize, WINHTTP_NO_HEADER_INDEX))
                goto cleanup;

            if (status != 200)          // 서버 오류 → 차단
                goto cleanup;

            std::string response;
            for (;;)
            {
                DWORD avail = 0;
                if (!WinHttpQueryDataAvailable(hRequest, &avail))
                    goto cleanup;
                if (avail == 0)
                    break;

                // 스칼라 boolean 이 정상 응답이다. 그보다 크면 응답이 아니므로 차단.
                if (response.size() + avail > 64)
                    goto cleanup;

                char buf[64];
                DWORD read = 0;
                if (!WinHttpReadData(hRequest, buf, avail, &read))
                    goto cleanup;
                if (read == 0)
                    break;

                response.append(buf, read);
            }

            // 서버는 true / false 만 돌려준다. true 가 아니면 전부 차단.
            result = (Trim(response) == "true") ? 1 : 0;
        }

    cleanup:
        if (hRequest) WinHttpCloseHandle(hRequest);
        if (hConnect) WinHttpCloseHandle(hConnect);
        if (hSession) WinHttpCloseHandle(hSession);
        return result;
    }

    JARCHSBLICENSE_API int GetMachineId(char* buf, int bufSize)
    {
        if (buf == NULL || bufSize < JARCH_MACHINE_ID_BUFSIZE)
            return 0;
        buf[0] = '\0';

        // MachineGuid 가 없으면 ID 를 만들 수 없다. 추측해서 만들지 않는다.
        std::string guid;
        if (!ReadMachineGuid(guid))
            return 0;

        std::string uuid;
        const bool hasUuid = ReadSmbiosUuid(uuid);

        std::string hex;
        if (!Sha256Hex(hasUuid ? (guid + "|" + uuid) : guid, hex))
            return 0;

        // UUID 를 못 읽은 기종은 접두어 M- 로 구분한다. 같은 PC 가 두 방식으로
        // 서로 다른 ID 를 갖게 되는 것을 관리자가 알아볼 수 있어야 한다.
        std::string id = hasUuid ? "" : "M-";
        for (int i = 0; i < MACHINE_ID_HEX_CHARS; ++i)
        {
            if (i > 0 && i % 4 == 0)
                id += '-';
            id += hex[i];
        }

        if ((int)id.size() + 1 > bufSize)
            return 0;

        memcpy(buf, id.c_str(), id.size() + 1);
        return 1;
    }
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    return TRUE;
}
