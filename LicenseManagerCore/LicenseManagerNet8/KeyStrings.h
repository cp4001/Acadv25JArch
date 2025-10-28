#pragma once

using namespace System;

namespace ProgramLicenseManager {

    public ref class KeyStrings abstract sealed // static class
    {
    public:
        static initonly String^ ENCRYPTION_KEY = "YourSecretKey123";
        static initonly array<String^>^ NTP_SERVERS = {
            "time.google.com",
            "time.windows.com",
            "pool.ntp.org",
            "time.nist.gov"
        };
    };
}
