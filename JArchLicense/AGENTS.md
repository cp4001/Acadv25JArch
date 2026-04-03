<!-- Generated: 2026-02-16 | Updated: 2026-02-16 -->

# JArchLicense

## Purpose
Windows DLL 라이선스 검증 모듈. JArchitecture 소프트웨어의 라이선스 만료 여부를 체크하는 네이티브 C++ DLL을 제공합니다.

## Key Files

| File | Description |
|------|-------------|
| `JArchLicense.h` | DLL API 헤더 파일 - `CheckLicense()`, `GetExpirationDate()` 함수 선언 |
| `JArchLicense.cpp` | 구현 파일 - 시스템 날짜와 만료일 비교 로직 |
| `JArchLicense.def` | 모듈 정의 파일 - DLL Export 함수 명시 |
| `JArchLicense.sln` | Visual Studio 2022 솔루션 파일 |
| `JArchLicense.vcxproj` | Visual Studio 프로젝트 파일 |
| `build.bat` | 명령줄 빌드 스크립트 (MSVC cl.exe 사용) |

## Build Outputs

| File | Description |
|------|-------------|
| `JArchLicense.dll` | 빌드된 Dynamic Link Library |
| `JArchLicense.lib` | Import 라이브러리 (정적 링크용) |

## For AI Agents

### Working In This Directory

- 만료일 변경 시 `JArchLicense.cpp`의 `EXP_YEAR`, `EXP_MONTH`, `EXP_DAY` 상수 수정
- DLL API 추가/변경 시 `.h`, `.cpp`, `.def` 파일 모두 동기화 필요
- `extern "C"` 블록 유지 - C++ name mangling 방지

### Build Requirements

**Visual Studio 사용:**
1. `JArchLicense.sln` 열기
2. Release | x64 구성 선택
3. 빌드 (Ctrl+Shift+B)

**명령줄 사용:**
1. Visual Studio Developer Command Prompt 실행
2. `build.bat` 실행

### API Reference

```c
// 라이선스 유효성 검사
// 반환: 1 (유효), 0 (만료)
int CheckLicense();

// 만료일 문자열 반환
// 반환: "YYYY-MM-DD" 형식 문자열
const char* GetExpirationDate();
```

### Common Patterns

- Windows API `SYSTEMTIME` 구조체로 시스템 날짜 획득
- `wsprintfA`로 ANSI 문자열 포맷팅
- 정적 버퍼 `g_expirationStr`로 문자열 반환 (thread-safe 아님)

## Dependencies

### External

- Windows SDK (`windows.h`)
- `user32.lib` - Windows User API

### Toolchain

- MSVC v143 (Visual Studio 2022)
- Windows 10 SDK

## Project Configuration

| Setting | Value |
|---------|-------|
| Configuration Type | DynamicLibrary (.dll) |
| Character Set | Unicode |
| Platforms | Win32, x64 |
| Configurations | Debug, Release |
| Preprocessor | `JARCHLICENSE_EXPORTS` |

<!-- MANUAL: -->
