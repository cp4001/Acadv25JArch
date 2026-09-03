#pragma once

#ifdef JARCHSBLICENSE_EXPORTS
#define JARCHSBLICENSE_API __declspec(dllexport)
#else
#define JARCHSBLICENSE_API __declspec(dllimport)
#endif

// GetMachineId 가 요구하는 최소 버퍼 크기 (NUL 포함).
#define JARCH_MACHINE_ID_BUFSIZE 32

extern "C"
{
    // Supabase 에 comID 의 사용 기한 유효성을 조회한다.
    //   1 = 사용 가능 (exp_date >= 오늘, Asia/Seoul)
    //   0 = 미등록 / 기간 초과 / HTTP 오류 / 네트워크 오류
    // 판정은 전부 서버(public.check_license)가 하고 여기서는 boolean 만 받는다.
    JARCHSBLICENSE_API int CheckLicenseOnline(const char* comID);

    // 이 PC 의 고유 ID 를 만들어 buf 에 NUL 종단 ASCII 로 기록한다.
    // MachineGuid + SMBIOS UUID 를 SHA-256 해싱해 앞 20자리를 4자씩 끊은 형식이다.
    //   정상          XXXX-XXXX-XXXX-XXXX-XXXX     (24자)
    //   UUID 불량     M-XXXX-XXXX-XXXX-XXXX-XXXX   (26자, MachineGuid 단독)
    // 이 값을 그대로 comID 로 써서 CheckLicenseOnline 에 넘긴다.
    //   1 = 성공 / 0 = 실패 (buf 는 빈 문자열)
    // buf 는 JARCH_MACHINE_ID_BUFSIZE 이상이어야 한다.
    JARCHSBLICENSE_API int GetMachineId(char* buf, int bufSize);
}
