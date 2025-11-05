# 🚀 LicenseAdminApp 빠른 시작 가이드

## ⚡ 5분 안에 시작하기

### 1️⃣ 프로젝트 열기
```bash
# Visual Studio로 열기
Eleclicense.sln 더블클릭

# 또는 VS Code로
code .
```

### 2️⃣ Admin Key 설정
`LicenseAdminApp/VercelApiClient.cs` 파일을 열고:
```csharp
private const string ADMIN_KEY = "your-actual-admin-key"; // 여기를 실제 키로 변경
```

### 3️⃣ 빌드 및 실행
**Visual Studio 사용:**
- LicenseAdminApp을 시작 프로젝트로 설정
- F5 키 눌러 실행

**명령줄 사용:**
```bash
cd LicenseAdminApp
dotnet build
dotnet run
```

### 4️⃣ 사용해보기

#### 📝 라이선스 추가
1. **[추가]** 버튼 클릭
2. Machine ID: `TEST-MACHINE-001`
3. 만료일: 1년 후 선택
4. **[확인]** 클릭

#### 🔍 검색
검색창에 `TEST` 입력하면 실시간 필터링

#### ✏️ 수정
1. 테이블에서 행 선택
2. **[수정]** 버튼 클릭
3. 정보 변경 후 **[확인]**

#### 🗑️ 삭제
1. 테이블에서 행 선택
2. **[삭제]** 버튼 클릭
3. 확인 대화상자에서 **[예]**

#### 💾 CSV 내보내기
1. **[CSV 내보내기]** 버튼 클릭
2. 저장 위치 선택
3. 엑셀에서 열기

---

## ✅ 예상 결과

### 앱 실행 시:
```
┌─────────────────────────────────────────┐
│ Eleclicense 관리자 - License Manager   │
├─────────────────────────────────────────┤
│ [추가] [수정] [삭제] [새로고침] [CSV]  │
├─────────────────────────────────────────┤
│ 검색: [                            ]    │
├─────────────────────────────────────────┤
│  Machine ID    │ 상태 │ 만료일  │ 남은일│
│  TEST-001      │ ✅   │2025-12-31│ 423  │
├─────────────────────────────────────────┤
│ Ready                      1 licenses    │
└─────────────────────────────────────────┘
```

### 라이선스 추가 성공:
```
✅ 라이선스가 추가되었습니다!
```

### 라이선스 삭제 확인:
```
❓ 정말로 이 라이선스를 삭제하시겠습니까?
   Machine ID: TEST-MACHINE-001
   
   [예]  [아니오]
```

---

## 🎯 다음 단계

### 실제 운영 환경 설정
1. **Admin Key 변경**
   - Vercel 서버에서 실제 키 확인
   - `VercelApiClient.cs`에 적용

2. **빌드 및 배포**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```

3. **실행 파일 위치**
   ```
   bin/Release/net8.0-windows/win-x64/publish/
   LicenseAdminApp.exe
   ```

### 팀원에게 공유
- 실행 파일만 복사해서 전달
- Admin Key는 안전하게 관리
- 사용 방법 공유

---

## 🔧 문제 해결

### Q: "Server error" 메시지가 나와요
**A:** Admin Key를 확인하세요
```csharp
// VercelApiClient.cs
private const string ADMIN_KEY = "올바른-키-입력";
```

### Q: 라이선스 목록이 안 보여요
**A:** 
1. 인터넷 연결 확인
2. Vercel 서버 상태 확인
3. [새로고침] 버튼 클릭

### Q: "Failed to list licenses" 오류
**A:** 
- Vercel 서버 URL 확인
- 방화벽 설정 확인
- 서버 상태 확인

---

## 💡 유용한 팁

### 영구 라이선스 만들기
라이선스 추가 시 "만료일 없음" 체크 → 영구 라이선스!

### 일괄 관리
CSV로 내보내기 → 엑셀에서 편집 → 필요시 스크립트로 재등록

### Machine ID 규칙
```
✅ 좋은 예시:
- COMPANY-DEPT-001
- CUSTOMER-A-PC-001
- MACHINE-OFFICE-DESKTOP-01

❌ 피해야 할 예시:
- test (너무 짧음)
- 123 (숫자만)
- 한글이름 (추적 어려움)
```

---

## 📚 더 알아보기

- 📖 [상세 README](README.md) - 전체 기능 설명
- 🔗 [Vercel 서버 설정](../README.md) - API 서버 정보
- 🛠️ [LicenseGenerator](../LicenseGenerator/README.md) - 파일 생성기

---

**Happy Managing! 🎉**
