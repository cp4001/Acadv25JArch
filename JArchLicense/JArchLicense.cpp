#include "JArchLicense.h"
#include <windows.h>

static const int EXP_YEAR = 2026;
static const int EXP_MONTH = 4;
static const int EXP_DAY = 1;

static char g_expirationStr[16] = {0};

extern "C"
{
    JARCHLICENSE_API int CheckLicense()
    {
        SYSTEMTIME st;
        GetLocalTime(&st);

        if (st.wYear > EXP_YEAR)
            return 0;
        if (st.wYear == EXP_YEAR && st.wMonth > EXP_MONTH)
            return 0;
        if (st.wYear == EXP_YEAR && st.wMonth == EXP_MONTH && st.wDay > EXP_DAY)
            return 0;

        return 1;
    }

    JARCHLICENSE_API const char* GetExpirationDate()
    {
        wsprintfA(g_expirationStr, "%04d-%02d-%02d", EXP_YEAR, EXP_MONTH, EXP_DAY);
        return g_expirationStr;
    }
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    return TRUE;
}
