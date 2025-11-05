using namespace System;

namespace ProgramLicenseManager {

    public ref class LicenseHelper
    {
    public:
        static bool CreateLicense(String^ id, DateTime expirationDate);
        static bool CheckLicense(String^ id);
        static String^ GetDateTime();
        static Nullable<DateTime> GetLicenseInfo();
        static String^ GetLicenseFilePath();

    private:
        static String^ LICENSE_FILE();
        static void EnsureLicenseDirectoryExists();
        static DateTime GetInternetTime();
        static unsigned int SwapEndianness(unsigned __int64 x);
        static String^ EncryptString(String^ text);
        static String^ DecryptString(String^ cipherText);
    };
}
