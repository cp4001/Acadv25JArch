// C++/CLI에서 Vercel 서버와 통신하는 예제
// LicenseManagerNet8 프로젝트에 통합 가능

#pragma once

using namespace System;
using namespace System::Net::Http;
using namespace System::Text;
using namespace System::Text::Json;
using namespace System::Threading::Tasks;
using namespace System::Management;
using namespace System::Security::Cryptography;

namespace VercelLicenseClient {

    public ref class LicenseClient
    {
    private:
        static HttpClient^ client = gcnew HttpClient();
        
        // ⚠️ 실제 배포된 Vercel URL로 변경하세요!
        static String^ API_URL = "https://your-project.vercel.app/api/check-license";

    public:
        /// <summary>
        /// 서버에서 암호화 키를 받아옵니다
        /// </summary>
        static Task<String^>^ GetEncryptionKeyFromServerAsync(String^ machineId)
        {
            return Task::Run(gcnew Func<String^>(
                [machineId]() -> String^ {
                    try {
                        // JSON 요청 생성
                        String^ jsonRequest = String::Format("{{\"id\":\"{0}\"}}", machineId);
                        auto content = gcnew StringContent(jsonRequest, Encoding::UTF8, "application/json");

                        // HTTP POST 요청
                        auto response = client->PostAsync(API_URL, content)->Result;
                        String^ responseJson = response->Content->ReadAsStringAsync()->Result;

                        if (response->IsSuccessStatusCode) {
                            // JSON 파싱 (간단한 방법)
                            if (responseJson->Contains("\"success\":true") && 
                                responseJson->Contains("\"valid\":true")) {
                                
                                // "key" 값 추출
                                int keyIndex = responseJson->IndexOf("\"key\":\"");
                                if (keyIndex != -1) {
                                    int startIndex = keyIndex + 7; // "key":" 길이
                                    int endIndex = responseJson->IndexOf("\"", startIndex);
                                    String^ key = responseJson->Substring(startIndex, endIndex - startIndex);
                                    
                                    Console::WriteLine("✓ License valid");
                                    return key;
                                }
                            }
                            
                            throw gcnew Exception("Invalid license response");
                        }
                        else {
                            throw gcnew Exception("License check failed: " + responseJson);
                        }
                    }
                    catch (Exception^ ex) {
                        throw gcnew Exception("License verification failed: " + ex->Message);
                    }
                }
            ));
        }

        /// <summary>
        /// 머신 고유 ID 생성 (CPU + 마더보드)
        /// </summary>
        static String^ GetMachineId()
        {
            try {
                String^ cpuId = GetCpuId();
                String^ motherboardSerial = GetMotherboardSerial();
                
                // SHA256 해시 생성
                String^ combined = String::Format("{0}-{1}", cpuId, motherboardSerial);
                array<Byte>^ bytes = Encoding::UTF8->GetBytes(combined);
                array<Byte>^ hash = SHA256::Create()->ComputeHash(bytes);
                
                // 처음 8바이트만 사용
                String^ hashString = BitConverter::ToString(hash, 0, 8)->Replace("-", "");
                return String::Format("MACHINE-{0}", hashString);
            }
            catch (Exception^ ex) {
                Console::WriteLine("Warning: Could not generate hardware-based ID: {0}", ex->Message);
                return String::Format("MACHINE-{0}", Environment::MachineName);
            }
        }

    private:
        static String^ GetCpuId()
        {
            try {
                ManagementObjectSearcher^ searcher = gcnew ManagementObjectSearcher(
                    "SELECT ProcessorId FROM Win32_Processor");
                
                for each (ManagementObject^ obj in searcher->Get()) {
                    Object^ procId = obj["ProcessorId"];
                    if (procId != nullptr) {
                        return procId->ToString();
                    }
                }
            }
            catch (Exception^) { }
            return "UNKNOWN";
        }

        static String^ GetMotherboardSerial()
        {
            try {
                ManagementObjectSearcher^ searcher = gcnew ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_BaseBoard");
                
                for each (ManagementObject^ obj in searcher->Get()) {
                    Object^ serial = obj["SerialNumber"];
                    if (serial != nullptr) {
                        return serial->ToString();
                    }
                }
            }
            catch (Exception^) { }
            return "UNKNOWN";
        }
    };
}


// ========================================
// 사용 예제
// ========================================

/*

// KeyStrings.h 수정 예제

#pragma once

using namespace System;
using namespace VercelLicenseClient;

namespace ProgramLicenseManager {

    public ref class KeyStrings abstract sealed
    {
    public:
        static String^ GetEncryptionKey() {
            try {
                // 1. 머신 ID 생성
                String^ machineId = LicenseClient::GetMachineId();
                Console::WriteLine("Machine ID: {0}", machineId);
                
                // 2. 서버에서 키 받기
                String^ key = LicenseClient::GetEncryptionKeyFromServerAsync(machineId)->Result;
                return key;
            }
            catch (Exception^ ex) {
                Console::WriteLine("Failed to get key from server: {0}", ex->Message);
                throw;
            }
        }
        
        static initonly array<String^>^ NTP_SERVERS = {
            "time.google.com",
            "time.windows.com",
            "pool.ntp.org",
            "time.nist.gov"
        };
    };
}

*/


// ========================================
// 프로젝트에 추가할 참조
// ========================================

/*

프로젝트 속성 → 공용 속성 → 참조 추가:
1. System.Net.Http
2. System.Management
3. System.Text.Json

또는 .vcxproj에 추가:

<ItemGroup>
  <Reference Include="System.Net.Http" />
  <Reference Include="System.Management" />
  <Reference Include="System.Text.Json" />
</ItemGroup>

*/
