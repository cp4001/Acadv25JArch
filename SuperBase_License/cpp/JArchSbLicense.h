#pragma once

#ifdef JARCHSBLICENSE_EXPORTS
#define JARCHSBLICENSE_API __declspec(dllexport)
#else
#define JARCHSBLICENSE_API __declspec(dllimport)
#endif

extern "C"
{
    // Supabase 에 comID 의 사용 기한 유효성을 조회한다.
    //   1 = 사용 가능 (exp_date >= 오늘, Asia/Seoul)
    //   0 = 미등록 / 기간 초과 / HTTP 오류 / 네트워크 오류
    // 판정은 전부 서버(public.check_license)가 하고 여기서는 boolean 만 받는다.
    JARCHSBLICENSE_API int CheckLicenseOnline(const char* comID);
}
