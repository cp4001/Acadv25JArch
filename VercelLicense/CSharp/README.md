# 🔐 Eleclicense C# Projects

3개의 C# 프로젝트로 구성된 라이선스 관리 시스템

---

## 📁 프로젝트 구조

```
CSharp/
├── LicenseGenerator/          # 라이선스 파일 생성기
│   ├── Program.cs
│   └── LicenseGenerator.csproj
├── LicenseCheckLibrary/       # 라이선스 검증 라이브러리
│   ├── LicenseChecker.cs
│   └── LicenseCheckLibrary.csproj
└── TestApp/                   # 테스트 애플리케이션
    ├── Program.cs
    └── TestApp.csproj
```

---

## 🎯 각 프로젝트 설명

### 1️⃣ LicenseGenerator (라이선스 생성기)
**용도:** Eleclicense.dat 파일 생성  
**배포:** 관리자만 사용 (고객에게 배포 X)

**특징:**
- ✅ 서버 통신 없음
- ✅ 암호화 키 하드코딩
- ✅ ID와 만료일 입력받아 암호화된 파일 생성

**사용법:**
```bash
cd LicenseGenerator
dotnet run
```

**입력:**
- Machine ID (예: MACHINE-TEST-123)
- End Date (예: 2025-12-31)
- Output Path (선택사항)

**출력:**
- `Eleclicense.dat` (암호화된 라이선스 파일)

---

### 2️⃣ LicenseCheckLibrary (검증 라이브러리)
**용도:** 앱에서 참조하는 클래스 라이브러리  
**배포:** 앱과 함께 배포

**특징:**
- ✅ Vercel 서버에서 ID로 암호화 키 가져오기
- ✅ Eleclicense.dat 파일 복호화
- ✅ 인터넷 시간으로 만료일 검증
- ✅ ID 불일치 체크

**사용법:**
```csharp
using LicenseCheckLibrary;

var result = await LicenseChecker.CheckLicenseAsync("MACHINE-TEST-123");

if (result.IsValid)
{
    Console.WriteLine($"✅ Valid - {result.RemainingDays} days left");
}
else
{
    Console.WriteLine($"❌ Invalid: {result.Message}");
}
```

---

### 3️⃣ TestApp (테스트 앱)
**용도:** 라이브러리 테스트용

**사용법:**
```bash
cd TestApp
dotnet run
```

---

## 🚀 빌드 및 실행

### 전체 솔루션 빌드
```bash
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense\CSharp

# 각 프로젝트 빌드
dotnet build LicenseGenerator
dotnet build LicenseCheckLibrary
dotnet build TestApp
```

### 실행
```bash
# 1. 라이선스 생성
cd LicenseGenerator
dotnet run

# 2. 테스트
cd ..\TestApp
dotnet run
```

---

## 📝 사용 시나리오

### 시나리오 1: 고객에게 라이선스 발급

**관리자 측:**
```bash
# 1. LicenseGenerator 실행
cd LicenseGenerator
dotnet run

# 2. 입력
Enter Machine ID: MACHINE-CUSTOMER-001
Enter End Date: 2025-12-31
Output path: C:\Licenses

# 3. 결과
✅ License file created successfully!
   Location: C:\Licenses\Eleclicense.dat
```

**고객에게 전달:**
- `Eleclicense.dat` 파일만 전달
- 앱 실행 폴더에 복사하도록 안내

---

### 시나리오 2: 앱에서 라이선스 검증

**앱 코드:**
```csharp
using LicenseCheckLibrary;

public class MyApp
{
    public async Task<bool> CheckLicense()
    {
        string machineId = GetMachineId(); // 앱에서 머신 ID 생성
        
        var result = await LicenseChecker.CheckLicenseAsync(machineId);
        
        if (!result.IsValid)
        {
            MessageBox.Show($"License Error: {result.Message}");
            Application.Exit();
            return false;
        }
        
        return true;
    }
}
```

---

## 🔐 보안 흐름

```
┌─────────────────┐
│ LicenseGenerator│
│  (관리자 전용)   │
└────────┬────────┘
         │ ID + EndDate 입력
         │ Key="YourSecretKey123" (하드코딩)
         ↓
   ┌─────────────┐
   │ AES 암호화  │
   └─────┬───────┘
         │
         ↓
  [Eleclicense.dat]  ← 고객에게 전달
         │
         ↓
┌────────────────────────────────────┐
│        고객 앱 실행                 │
├────────────────────────────────────┤
│ 1. LicenseChecker.CheckLicenseAsync│
│    ↓                               │
│ 2. Vercel 서버에 ID 전송          │
│    ← Key 받음                      │
│    ↓                               │
│ 3. Eleclicense.dat 복호화         │
│    ↓                               │
│ 4. ID 검증                         │
│    ↓                               │
│ 5. 인터넷 시간 + EndDate 검증     │
│    ↓                               │
│ 6. ✅ 또는 ❌                      │
└────────────────────────────────────┘
```

---

## ⚙️ 설정

### LicenseGenerator에서 키 변경
`Program.cs` 파일:
```csharp
private const string ENCRYPTION_KEY = "YourSecretKey123"; // 여기 수정
```

### LicenseCheckLibrary에서 API URL 변경
`LicenseChecker.cs` 파일:
```csharp
private const string VERCEL_API_URL = "https://elec-license.vercel.app/api/check-license";
```

---

## 📦 배포 가이드

### 관리자 도구 (내부용)
- **LicenseGenerator.exe** - 배포 X

### 고객 앱
- **앱.exe** + **LicenseCheckLibrary.dll** - 배포 O
- **Eleclicense.dat** - 고객별로 생성해서 전달

---

## 🐛 문제 해결

### "License file not found"
→ Eleclicense.dat 파일이 앱과 같은 폴더에 있는지 확인

### "Server connection failed"
→ 인터넷 연결 확인, Vercel 서버 상태 확인

### "License ID mismatch"
→ 다른 머신의 라이선스 파일을 사용 중

### "License has expired"
→ 만료일이 지남, 새 라이선스 파일 발급 필요

---

## 💡 팁

### 머신 ID 생성 예제
```csharp
public static string GetMachineId()
{
    // 옵션 1: 컴퓨터 이름
    return $"MACHINE-{Environment.MachineName}";
    
    // 옵션 2: 하드웨어 기반 (더 안전)
    // CPU ID, MAC 주소 등 조합
}
```

### 라이선스 파일 경로 지정
```csharp
// 현재 디렉토리
var result = await LicenseChecker.CheckLicenseAsync(machineId);

// 특정 경로
var result = await LicenseChecker.CheckLicenseAsync(
    machineId, 
    @"C:\MyApp\Licenses\Eleclicense.dat"
);
```

---

## 📄 라이선스

MIT License

---

**제작일:** 2025-11-04  
**버전:** 1.0.0
