# 🖥️ License Admin App

Vercel + Neon 라이선스 관리 Windows 애플리케이션

---

## 📋 개요

이 애플리케이션은 Vercel에 배포된 라이선스 서버의 관리자용 클라이언트입니다.

### 주요 기능
- ✅ 라이선스 목록 조회
- ✅ 새 라이선스 등록 (Machine ID, Product, Username, 만료일)
- ✅ 라이선스 수정
- ✅ 라이선스 삭제
- ✅ 검색 기능 (ID, 제품, 사용자명)
- ✅ CSV 내보내기

---

## 🔧 설정

### API 서버 정보

`VercelApiClient.cs` 파일에서 다음 정보를 수정하세요:

```csharp
private const string BASE_URL = "https://elec-license.vercel.app";
private const string ADMIN_KEY = "super-secret-admin-key-change-me-12345";
```

**현재 설정:**
- **API URL:** https://elec-license.vercel.app
- **Admin Key:** super-secret-admin-key-change-me-12345

⚠️ **보안:** 실제 운영 시 ADMIN_KEY를 반드시 변경하세요!

---

## 🚀 빌드 및 실행

### 필요 환경
- Visual Studio 2022 이상
- .NET 8.0 SDK
- Windows 10/11

### 빌드 방법

#### 방법 1: Visual Studio 사용
1. `LicenseAdminApp.csproj` 파일을 Visual Studio로 열기
2. `빌드` → `솔루션 빌드` (Ctrl+Shift+B)
3. `디버그` → `디버깅 시작` (F5)

#### 방법 2: 명령줄 사용
```cmd
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense\CSharp\LicenseAdminApp
dotnet build
dotnet run
```

---

## 📊 데이터 필드

### 라이선스 정보
| 필드 | 타입 | 필수 | 설명 |
|------|------|------|------|
| Machine ID | string | ✅ | 머신 고유 식별자 (최소 5자) |
| Product | string | ❌ | 제품명 |
| Username | string | ❌ | 사용자명 |
| Expiry Date | DateTime? | ❌ | 만료일 (미설정 시 무제한) |

### 표시 정보
- **상태**: 유효 여부 (✅ Valid / ❌ Invalid)
- **등록일**: 라이선스 등록 시각
- **수정일**: 마지막 수정 시각
- **남은 기간**: 만료까지 남은 일수

---

## 🎯 사용법

### 1. 라이선스 추가
1. **Add** 버튼 클릭
2. 정보 입력:
   - Machine ID (필수, 최소 5자)
   - Product (선택)
   - Username (선택)
   - Expiry Date (선택, "No Expiry" 체크박스 사용 가능)
3. **OK** 클릭

### 2. 라이선스 수정
1. 목록에서 수정할 라이선스 선택
2. **Edit** 버튼 클릭
3. 정보 수정
4. **OK** 클릭

**주의:** Machine ID가 변경되면 기존 라이선스가 삭제되고 새로 등록됩니다.

### 3. 라이선스 삭제
1. 목록에서 삭제할 라이선스 선택
2. **Delete** 버튼 클릭
3. 확인 대화상자에서 **Yes** 클릭

### 4. 검색
- 검색창에 Machine ID, 제품명, 또는 사용자명 입력
- 실시간으로 필터링됨

### 5. CSV 내보내기
1. **Export** 버튼 클릭
2. 저장 위치 선택
3. 저장 완료

---

## 🔐 보안 고려사항

### 1. Admin Key 보호
- ✅ 소스 코드에 하드코딩하지 않기
- ✅ 환경 변수 또는 설정 파일 사용 권장
- ✅ Git에 키 커밋하지 않기

### 2. HTTPS 사용
- ✅ API 서버는 반드시 HTTPS 사용
- ✅ 현재: https://elec-license.vercel.app

### 3. 접근 제어
- ✅ 관리자만 이 앱을 사용해야 함
- ✅ 네트워크 접근 제한 고려

---

## 📡 API 엔드포인트

### 1. 목록 조회
**POST** `/api/list-ids`
```json
{
  "adminKey": "your-admin-key"
}
```

### 2. 라이선스 등록
**POST** `/api/register-id`
```json
{
  "adminKey": "your-admin-key",
  "id": "MACHINE-001",
  "product": "MyApp",
  "username": "John Doe",
  "expiresAt": "2025-12-31"
}
```

### 3. 라이선스 삭제
**POST** `/api/delete-id`
```json
{
  "adminKey": "your-admin-key",
  "id": "MACHINE-001"
}
```

---

## 🐛 문제 해결

### "Failed to list licenses" 오류
- API URL 확인: `VercelApiClient.cs`의 `BASE_URL`
- 인터넷 연결 확인
- Vercel 서버 상태 확인

### "Invalid admin key" 오류
- `ADMIN_KEY`가 Vercel 환경 변수와 일치하는지 확인
- Vercel Dashboard → Settings → Environment Variables

### "ID already exists" 오류
- 동일한 Machine ID가 이미 등록되어 있음
- 다른 ID 사용하거나 기존 라이선스 삭제 후 재등록

### 빌드 오류
```cmd
# 클린 빌드
dotnet clean
dotnet build
```

---

## 📁 파일 구조

```
LicenseAdminApp/
├── MainForm.cs              # 메인 폼
├── MainForm.Designer.cs     # 메인 폼 디자이너
├── AddEditDialog.cs         # 추가/수정 대화상자
├── AddEditDialog.Designer.cs# 대화상자 디자이너
├── VercelApiClient.cs       # API 클라이언트
├── Program.cs               # 진입점
└── README.md                # 이 파일
```

---

## 🔄 업데이트 이력

### v2.0 (2025-11-05)
- ✅ Product (제품명) 필드 추가
- ✅ Username (사용자명) 필드 추가
- ✅ 검색 기능에 Product, Username 포함
- ✅ CSV 내보내기에 새 필드 추가
- ✅ UI 레이아웃 개선

### v1.0 (이전)
- ✅ 기본 라이선스 관리 기능
- ✅ Machine ID, 만료일 관리

---

## 📞 지원

문제가 발생하면:
1. **Vercel 로그** 확인: https://vercel.com/dashboard
2. **Neon 상태** 확인: https://console.neon.tech
3. **FINAL-SETUP.md** 참조

---

## 📄 라이선스

MIT License

---

**개발:** License Admin App  
**API 서버:** https://elec-license.vercel.app  
**최종 업데이트:** 2025-11-05
