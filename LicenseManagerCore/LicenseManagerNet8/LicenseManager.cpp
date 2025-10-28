#include "LicenseManager.h"
#include "KeyStrings.h"


using namespace System;

using namespace System::Net;
using namespace System::Net::Sockets;
using namespace System::Security::Cryptography;
using namespace System::Text;
using namespace ProgramLicenseManager;

String^ LicenseHelper::LICENSE_FILE() {
    return System::IO::Path::Combine(System::IO::Directory::GetCurrentDirectory(), "license.dat");
}

void LicenseHelper::EnsureLicenseDirectoryExists() {
    // No folder creation needed as we use the current directory.
}

bool LicenseHelper::CreateLicense(DateTime expirationDate) {
    try {
        EnsureLicenseDirectoryExists();
        String^ licenseData = expirationDate.ToString("yyyy-MM-dd HH:mm:ss");
        String^ encrypted = EncryptString(licenseData);
        System::IO::File::WriteAllText(LICENSE_FILE(), encrypted);
        Console::WriteLine("✓ License file created: {0}", LICENSE_FILE());
        return true;
    }
    catch (UnauthorizedAccessException^) {
        Console::WriteLine("❌ Permission error: {0}", LICENSE_FILE());
        Console::WriteLine("Run the program as an administrator.");
        return false;
    }
    catch (Exception^ ex) {
        Console::WriteLine("License creation failed: {0}", ex->Message);
        return false;
    }
}

bool LicenseHelper::CheckLicense() {
    try {
        if (!System::IO::File::Exists(LICENSE_FILE())) {
            Console::WriteLine("License file not found: {0}", LICENSE_FILE());
            return false;
        }

        String^ encrypted = System::IO::File::ReadAllText(LICENSE_FILE());
        String^ decrypted = DecryptString(encrypted);
        DateTime expirationDate = DateTime::Parse(decrypted);

        DateTime internetTime = GetInternetTime();

        if (internetTime > expirationDate) {
            Console::WriteLine("License has expired. Expiration date: {0:yyyy-MM-dd}", expirationDate);
            return false;
        }

        TimeSpan remaining = expirationDate - internetTime;
        Console::WriteLine("License valid - remaining time: {0} days", remaining.Days);
        return true;
    }
    catch (Exception^ ex) {
        Console::WriteLine("License check failed: {0}", ex->Message);
        return false;
    }
}

String^ LicenseHelper::GetDateTime() {
    try {
        if (!System::IO::File::Exists(LICENSE_FILE())) {
            Console::WriteLine("License file not found: {0}", LICENSE_FILE());
            return "License file not found";
        }

        String^ encrypted = System::IO::File::ReadAllText(LICENSE_FILE());
        String^ decrypted = DecryptString(encrypted);
        DateTime expirationDate = DateTime::Parse(decrypted);

        DateTime internetTime = GetInternetTime();

        if (internetTime > expirationDate) {
            Console::WriteLine("License has expired. Expiration date: {0:yyyy-MM-dd}", expirationDate);
            return "License has expired";
        }

        TimeSpan remaining = expirationDate - internetTime;
        Console::WriteLine("License valid - remaining time: {0} days", remaining.Days);
        return String::Format("License valid - remaining time: {0} days", remaining.Days);
    }
    catch (Exception^ ex) {
        Console::WriteLine("License check failed: {0}", ex->Message);
        return "Cannot verify license";
    }
}

Nullable<DateTime> LicenseHelper::GetLicenseInfo() {
    try {
        if (!System::IO::File::Exists(LICENSE_FILE())) {
            return Nullable<DateTime>();
        }

        String^ encrypted = System::IO::File::ReadAllText(LICENSE_FILE());
        String^ decrypted = DecryptString(encrypted);
        return DateTime::Parse(decrypted);
    }
    catch (Exception^) {
        return Nullable<DateTime>();
    }
}

String^ LicenseHelper::GetLicenseFilePath() {
    return LICENSE_FILE();
}

DateTime LicenseHelper::GetInternetTime() {
    for each (String^ ntpServer in KeyStrings::NTP_SERVERS) {
        try {
            array<Byte>^ ntpData = gcnew array<Byte>(48);
            ntpData[0] = 0x1B;

            array<IPAddress^>^ addresses = Dns::GetHostEntry(ntpServer)->AddressList;
            IPEndPoint^ ipEndPoint = gcnew IPEndPoint(addresses[0], 123);

            Socket^ socket = gcnew Socket(AddressFamily::InterNetwork, SocketType::Dgram, ProtocolType::Udp);
            socket->ReceiveTimeout = 3000;
            socket->Connect(ipEndPoint);
            socket->Send(ntpData);
            socket->Receive(ntpData);
            socket->Close();

            const int serverReplyTime = 40;
            unsigned __int64 intPart = BitConverter::ToUInt32(ntpData, serverReplyTime);
            unsigned __int64 fractPart = BitConverter::ToUInt32(ntpData, serverReplyTime + 4);

            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            unsigned __int64 milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000LL);
            DateTime networkDateTime = (DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind::Utc)).AddMilliseconds((long long)milliseconds);

            return networkDateTime.ToLocalTime();
        }
        catch (Exception^ ex) {
            Console::WriteLine("NTP server {0} connection failed: {1}", ntpServer, ex->Message);
            continue;
        }
    }
    throw gcnew Exception("All NTP servers failed to connect. Check your internet connection.");
}

unsigned int LicenseHelper::SwapEndianness(unsigned __int64 x) {
    return (unsigned int)(((x & 0x000000ff) << 24) +
        ((x & 0x0000ff00) << 8) +
        ((x & 0x00ff0000) >> 8) +
        ((x & 0xff000000) >> 24));
}

String^ LicenseHelper::EncryptString(String^ text) {
    array<Byte>^ keyBytes = SHA256::Create()->ComputeHash(Encoding::UTF8->GetBytes(KeyStrings::ENCRYPTION_KEY));
    array<Byte>^ iv = gcnew array<Byte>(16);
    Array::Copy(keyBytes, iv, 16);

    Aes^ aesAlg = Aes::Create();
    aesAlg->Key = keyBytes;
    aesAlg->IV = iv;

    ICryptoTransform^ encryptor = aesAlg->CreateEncryptor(aesAlg->Key, aesAlg->IV);

    System::IO::MemoryStream^ msEncrypt = gcnew System::IO::MemoryStream();
    CryptoStream^ csEncrypt = gcnew CryptoStream(msEncrypt, encryptor, CryptoStreamMode::Write);
    // StreamWriter 생성자 오버로드 모호성 해결: bufferSize와 leaveOpen 매개변수 명시
    System::IO::StreamWriter^ swEncrypt = gcnew System::IO::StreamWriter(csEncrypt, Encoding::UTF8, 1024, true);

    swEncrypt->Write(text);
    swEncrypt->Flush();
    swEncrypt->Close();
    csEncrypt->Close();
    msEncrypt->Close();

    return Convert::ToBase64String(msEncrypt->ToArray());
}

String^ LicenseHelper::DecryptString(String^ cipherText) {
    array<Byte>^ keyBytes = SHA256::Create()->ComputeHash(Encoding::UTF8->GetBytes(KeyStrings::ENCRYPTION_KEY));
    array<Byte>^ iv = gcnew array<Byte>(16);
    Array::Copy(keyBytes, iv, 16);

    Aes^ aesAlg = Aes::Create();
    aesAlg->Key = keyBytes;
    aesAlg->IV = iv;

    ICryptoTransform^ decryptor = aesAlg->CreateDecryptor(aesAlg->Key, aesAlg->IV);

    System::IO::MemoryStream^ msDecrypt = gcnew System::IO::MemoryStream(Convert::FromBase64String(cipherText));
    CryptoStream^ csDecrypt = gcnew CryptoStream(msDecrypt, decryptor, CryptoStreamMode::Read);
    // StreamReader 생성자 오버로드 모호성 해결: detectEncodingFromByteOrderMarks, bufferSize, leaveOpen 매개변수 명시
    System::IO::StreamReader^ srDecrypt = gcnew System::IO::StreamReader(csDecrypt, Encoding::UTF8, false, 1024, true);

    String^ result = srDecrypt->ReadToEnd();

    srDecrypt->Close();
    csDecrypt->Close();
    msDecrypt->Close();

    return result;
}
