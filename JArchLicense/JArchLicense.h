#pragma once

#ifdef JARCHLICENSE_EXPORTS
#define JARCHLICENSE_API __declspec(dllexport)
#else
#define JARCHLICENSE_API __declspec(dllimport)
#endif

extern "C"
{
    JARCHLICENSE_API int CheckLicense();
    JARCHLICENSE_API const char* GetExpirationDate();
}
