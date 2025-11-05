# 🚀 빠른 시작 가이드

## ⚡ 5분 안에 시작하기

### 1️⃣ 솔루션 열기
```bash
# Visual Studio로 열기
Eleclicense.sln

# 또는 VS Code로
code .
```

### 2️⃣ 빌드
```bash
dotnet build
```

### 3️⃣ 라이선스 파일 생성
```bash
cd LicenseGenerator
dotnet run

# 입력 예시:
# Machine ID: MACHINE-TEST-123
# End Date: 2025-12-31
# Output path: (Enter)
```

### 4️⃣ 테스트
```bash
cd ..\TestApp
dotnet run

# 입력:
# Machine ID: MACHINE-TEST-123
# License file path: (Enter)
```

---

## ✅ 예상 결과

### 라이선스 생성 성공:
```
✅ License file created successfully!
   Location: C:\...\Eleclicense.dat
   Machine ID: MACHINE-TEST-123
   Expires: 2025-12-31
```

### 라이선스 검증 성공:
```
✅ LICENSE VALID
   Machine ID: MACHINE-TEST-123
   Expiry Date: 2025-12-31
   Remaining Days: 423 days
   Message: License valid - 423 days remaining
```

---

## 🎯 다음 단계

1. **앱에 통합:**
   ```csharp
   using LicenseCheckLibrary;
   
   var result = await LicenseChecker.CheckLicenseAsync("YOUR-MACHINE-ID");
   if (!result.IsValid) Application.Exit();
   ```

2. **머신 ID 커스터마이징:**
   - 컴퓨터 이름
   - 하드웨어 ID
   - 고유 코드

3. **배포:**
   - 앱 + LicenseCheckLibrary.dll
   - 고객별 Eleclicense.dat 생성

---

**자세한 내용:** [README.md](README.md)
