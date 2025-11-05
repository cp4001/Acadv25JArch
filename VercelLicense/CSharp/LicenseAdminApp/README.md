# 🔐 License Admin App

Vercel 서버의 라이선스를 관리하는 CRUD WinForms 애플리케이션

## 📋 주요 기능

### ✅ CRUD 작업
- **조회 (Read)**: 모든 라이선스 목록 표시
- **추가 (Create)**: 새 라이선스 등록
- **수정 (Update)**: 기존 라이선스 수정
- **삭제 (Delete)**: 라이선스 삭제

### 🔍 추가 기능
- **실시간 검색**: Machine ID로 빠른 검색
- **새로고침**: 최신 데이터 불러오기
- **CSV 내보내기**: 전체 데이터 CSV 파일로 저장
- **상태 표시**: 유효/무효 상태 및 남은 기간 표시

## 🖥️ 화면 구성

```
┌─────────────────────────────────────────────────────┐
│ [추가] [수정] [삭제] | [새로고침] | [CSV 내보내기]    │
├─────────────────────────────────────────────────────┤
│ 검색: [_________________________]                    │
├─────────────────────────────────────────────────────┤
│  Machine ID       │ 상태  │ 만료일    │ 남은기간   │
│  MACHINE-001      │ ✅    │ 2025-12-31│ 423 days  │
│  MACHINE-002      │ ❌    │ 2024-01-01│ 0 days    │
│  MACHINE-003      │ ✅    │ No Expiry │ ∞         │
├─────────────────────────────────────────────────────┤
│ Ready                               3 licenses       │
└─────────────────────────────────────────────────────┘
```

## 🚀 실행 방법

### 1. 빌드
```bash
cd LicenseAdminApp
dotnet build
```

### 2. 실행
```bash
dotnet run
```

또는 Visual Studio에서 직접 실행 (F5)

## ⚙️ 설정

### Admin Key 변경
`VercelApiClient.cs` 파일:
```csharp
private const string ADMIN_KEY = "your-actual-admin-key-here";
```

### API URL 변경
`VercelApiClient.cs` 파일:
```csharp
private const string BASE_URL = "https://your-vercel-url.vercel.app";
```

## 📝 사용 방법

### 라이선스 추가
1. **[추가]** 버튼 클릭
2. Machine ID 입력 (예: MACHINE-CUSTOMER-001)
3. 만료일 선택 또는 "만료일 없음" 체크
4. **[확인]** 클릭

### 라이선스 수정
1. 수정할 라이선스 행 선택
2. **[수정]** 버튼 클릭
3. 정보 변경
4. **[확인]** 클릭

### 라이선스 삭제
1. 삭제할 라이선스 행 선택
2. **[삭제]** 버튼 클릭
3. 확인 메시지에서 **[예]** 클릭

### 검색
- 검색 입력란에 Machine ID 일부 입력
- 실시간으로 필터링됨

### CSV 내보내기
1. **[CSV 내보내기]** 버튼 클릭
2. 저장 위치 선택
3. 파일명 입력 (기본: licenses_날짜시간.csv)

## 🔐 보안

### Admin Key
- Vercel 서버의 Admin Key가 필요합니다
- 기본값: `super-secret-admin-key-change-me-12345`
- ⚠️ 실제 운영 환경에서는 반드시 변경하세요!

### 권한
- 이 앱을 실행하려면 Admin Key가 필요합니다
- Admin Key 없이는 어떤 작업도 수행할 수 없습니다

## 📊 데이터 표시

### 컬럼 정보
| 컬럼 | 설명 |
|------|------|
| **Machine ID** | 라이선스 고유 식별자 |
| **상태** | ✅ 유효 / ❌ 무효 |
| **만료일** | 라이선스 만료 날짜 (또는 "No Expiry") |
| **남은 기간** | 만료까지 남은 일수 (또는 "∞") |
| **등록일** | 최초 등록 날짜 및 시간 |
| **수정일** | 마지막 수정 날짜 및 시간 |

## 🐛 문제 해결

### "Server error" 메시지
→ Admin Key가 잘못되었거나 서버에 연결할 수 없음

### "Failed to list licenses"
→ 인터넷 연결 확인 또는 Vercel 서버 상태 확인

### 라이선스 추가 실패
→ 동일한 Machine ID가 이미 존재할 수 있음

## 💡 팁

### 영구 라이선스
- "만료일 없음" 체크박스를 선택하면 영구 라이선스로 등록됩니다
- 데이터베이스에는 만료일이 `null`로 저장됩니다

### 일괄 작업
- CSV로 내보낸 후 엑셀에서 편집
- 필요시 스크립트로 일괄 등록 가능

### Machine ID 규칙
- 최소 5자 이상 권장
- 예시: `MACHINE-COMPANY-001`, `CUSTOMER-A-PC-001`
- 고유성이 보장되어야 함

## 📦 배포

### 릴리스 빌드
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### 실행 파일 위치
```
bin/Release/net8.0-windows/win-x64/publish/LicenseAdminApp.exe
```

### 배포 시 주의사항
- ⚠️ Admin Key를 먼저 변경하세요
- 이 앱은 관리자 전용입니다
- 고객에게 배포하면 안 됩니다!

## 🔗 관련 프로젝트

- **LicenseGenerator**: 라이선스 파일 생성기
- **LicenseCheckLibrary**: 라이선스 검증 라이브러리
- **TestApp**: 테스트 애플리케이션

## 📄 라이선스

MIT License

---

**제작일**: 2025-11-05  
**버전**: 1.0.0
