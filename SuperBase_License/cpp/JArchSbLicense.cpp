#include "JArchSbLicense.h"

#include <windows.h>
#include <winhttp.h>
#include <stdio.h>
#include <string>

#pragma comment(lib, "winhttp.lib")

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
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    return TRUE;
}
