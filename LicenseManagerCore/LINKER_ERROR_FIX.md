# C++/CLI .NET Core 링커 오류 해결 가이드

## 🔴 오류 증상
```
확인할 수 없는 외부 기호 _DllMainCRTStartup
확인할 수 없는 외부 기호 __security_init_cookie
확인할 수 없는 외부 기호 _fltused
```

## 🔧 해결 방법

### 방법 1: 프로젝트 다시 빌드 (권장)

1. **솔루션 정리**
   ```
   빌드 > 솔루션 정리
   ```

2. **중간 파일 삭제**
   - `LicenseManagerNet8/Debug` 폴더 삭제
   - `LicenseManagerNet8/Release` 폴더 삭제
   - `LicenseManagerNet8/x64` 폴더 삭제

3. **Visual Studio 재시작**

4. **솔루션 다시 빌드**
   ```
   빌드 > 솔루션 다시 빌드 (Ctrl + Shift + B)
   ```

### 방법 2: 수동으로 빌드 폴더 정리

Windows 탐색기에서:
```
C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore\LicenseManagerNet8\
```
다음 폴더들을 삭제:
- `Debug`
- `Release`  
- `x64`
- `.vs` (숨김 폴더)

그 다음 Visual Studio에서 솔루션을 다시 빌드하세요.

### 방법 3: 명령줄에서 빌드

Visual Studio 개발자 명령 프롬프트를 관리자 권한으로 실행:

```cmd
cd C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore

REM 정리
msbuild LicenseManagerNet8.sln /t:Clean /p:Configuration=Debug /p:Platform=x64

REM 다시 빌드
msbuild LicenseManagerNet8.sln /t:Rebuild /p:Configuration=Debug /p:Platform=x64
```

## 🎯 수정된 프로젝트 설정

### Debug 구성
```xml
<RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
<AdditionalDependencies>msvcrtd.lib;ucrtd.lib;vcruntime.lib</AdditionalDependencies>
```

### Release 구성
```xml
<RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
<AdditionalDependencies>msvcrt.lib;ucrt.lib;vcruntime.lib</AdditionalDependencies>
```

## ⚠️ 여전히 문제가 발생하는 경우

### 체크리스트

1. **Visual Studio 2022가 최신 버전인가?**
   - 도움말 > 업데이트 확인

2. **C++/CLI 구성요소가 설치되어 있나?**
   - Visual Studio Installer 실행
   - 수정 버튼 클릭
   - 개별 구성요소 탭
   - "C++/CLI support for v143 build tools (.NET Core)" 확인

3. **.NET 8 SDK가 설치되어 있나?**
   ```cmd
   dotnet --list-sdks
   ```
   8.0.x 버전이 표시되어야 합니다.

4. **플랫폼이 x64로 설정되어 있나?**
   - 상단 도구 모음에서 "x64" 선택 확인

## 🔄 대안: 프로젝트 재생성

위 방법이 모두 실패하면, 다음 단계를 시도하세요:

### 1단계: 백업
- `LicenseManager.cpp`, `LicenseManager.h`, `KeyStrings.h` 파일 백업

### 2단계: Visual Studio에서 새 프로젝트 생성
1. 파일 > 새로 만들기 > 프로젝트
2. "C++ CLR 클래스 라이브러리(.NET)" 템플릿 선택
3. .NET 버전을 .NET 8.0으로 선택
4. 프로젝트 생성

### 3단계: 파일 복사
- 백업한 소스 파일을 새 프로젝트에 추가

### 4단계: 프로젝트 설정 조정
- 플랫폼: x64만 남기고 삭제
- C++ 언어 표준: C++20

## 💡 팁

### Visual Studio 캐시 정리
```cmd
rd /s /q "%LocalAppData%\Microsoft\VisualStudio\17.0_xxxxx\ComponentModelCache"
```
(xxxxx는 실제 인스턴스 ID)

### NuGet 캐시 정리
```cmd
dotnet nuget locals all --clear
```

## 📞 추가 지원이 필요한 경우

다음 정보를 확인하세요:
1. Visual Studio 버전: 도움말 > Microsoft Visual Studio 정보
2. .NET SDK 버전: `dotnet --version`
3. Windows 버전: `winver`

이 정보가 문제 해결에 도움이 될 수 있습니다.
