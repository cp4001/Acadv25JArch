#pragma once

// 네이티브 C++ 함수 선언
extern "C" {
    __declspec(dllexport) void GetSecureKey(char* buffer, size_t size);
}
