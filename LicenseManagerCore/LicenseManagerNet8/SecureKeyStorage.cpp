#pragma unmanaged  // 이 부분은 네이티브 C++로 컴파일됨

#include <string>
#include <vector>

// 네이티브 C++ 함수 - 디컴파일 어려움
namespace NativeCore {
    
    // XOR 기반 간단한 난독화 (실제로는 더 복잡하게)
    static void DecryptKey(char* output, size_t size) {
        // 난독화된 키 (컴파일 시점에 XOR 처리됨)
        unsigned char encryptedKey[] = {
            0x79 ^ 0xAA, 0x6F ^ 0xAA, 0x75 ^ 0xAA, 0x72 ^ 0xAA,  // "Your"
            0x53 ^ 0xAA, 0x65 ^ 0xAA, 0x63 ^ 0xAA, 0x72 ^ 0xAA,  // "Secr"
            0x65 ^ 0xAA, 0x74 ^ 0xAA, 0x4B ^ 0xAA, 0x65 ^ 0xAA,  // "etKe"
            0x79 ^ 0xAA, 0x31 ^ 0xAA, 0x32 ^ 0xAA, 0x33 ^ 0xAA,  // "y123"
            0x00
        };
        
        // 런타임에 복호화
        for (size_t i = 0; i < size - 1 && encryptedKey[i] != 0; i++) {
            output[i] = encryptedKey[i] ^ 0xAA;
        }
        output[size - 1] = '\0';
    }
    
    // 더 복잡한 난독화: 여러 레이어
    static void GetEncryptionKey(char* buffer, size_t bufferSize) {
        // 1단계: 기본 복호화
        DecryptKey(buffer, bufferSize);
        
        // 2단계: 추가 변환 (선택사항)
        // 환경 변수, 레지스트리, 하드웨어 ID 등과 조합 가능
    }
    
    // C 스타일 함수로 export (C++/CLI에서 호출 가능)
    extern "C" {
        __declspec(dllexport) void GetSecureKey(char* buffer, size_t size) {
            GetEncryptionKey(buffer, size);
        }
    }
}

#pragma managed  // 다시 관리 코드로 전환
