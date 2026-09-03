// JArchSbLicense.dll 검증용 콘솔 프로그램.
//
//   test_client.exe            → 서버에 심어 둔 TEST-* 로 회귀 검증
//   test_client.exe ABC-1234   → 임의 comID 조회 (종료코드 0=유효, 1=차단)

#include "JArchSbLicense.h"

#include <stdio.h>
#include <string.h>

namespace
{
    struct Case
    {
        const char* comID;
        int         expected;
        const char* note;
    };

    // prd.md §5.1 반환 규칙. 데이터는 public.licenses 에 등록돼 있다.
    const Case CASES[] = {
        { "TEST-VALID",   1, "exp_date = 오늘+30" },
        { "TEST-TODAY",   1, "exp_date = 오늘 (당일은 사용 가능)" },
        { "TEST-EXPIRED", 0, "exp_date = 오늘-1" },
        { "NO-SUCH-ID",   0, "미등록" },
        { "",             0, "빈 문자열 (요청 안 보냄)" },
    };
}

int main(int argc, char** argv)
{
    if (argc > 1)
    {
        int r = CheckLicenseOnline(argv[1]);
        printf("%s -> %s\n", argv[1], r ? "true (사용 가능)" : "false (차단)");
        return r ? 0 : 1;
    }

    printf("JArchSbLicense 회귀 검증\n");
    printf("------------------------------------------------------------\n");

    int failed = 0;
    for (size_t i = 0; i < sizeof(CASES) / sizeof(CASES[0]); ++i)
    {
        const Case& c = CASES[i];
        int got = CheckLicenseOnline(c.comID);
        int ok  = (got == c.expected);
        if (!ok)
            ++failed;

        printf("[%s] %-14s 기대=%-5s 실제=%-5s  %s\n",
               ok ? "OK  " : "FAIL",
               c.comID[0] ? c.comID : "(빈값)",
               c.expected ? "true" : "false",
               got ? "true" : "false",
               c.note);
    }

    printf("------------------------------------------------------------\n");
    if (failed == 0)
        printf("전부 통과\n");
    else
        printf("%d건 실패 — 서버 상태(ACTIVE_HEALTHY)와 네트워크를 확인할 것\n", failed);

    return failed == 0 ? 0 : 1;
}
