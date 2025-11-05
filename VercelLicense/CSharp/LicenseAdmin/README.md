# 🔐 License Admin Panel

Vercel License Server를 위한 Windows 관리자 패널

## 📋 기능

- ✅ 라이선스 조회 (Machine ID, 유효성, 등록/만료/수정 날짜)
- ➕ 라이선스 추가
- ✏️ 라이선스 수정
- 🗑️ 라이선스 삭제
- 🔍 Machine ID 검색
- 📄 CSV 파일로 내보내기
- 🔄 자동/수동 새로고침

## 🚀 빌드 및 실행

### 필수 요구사항
- .NET 8.0 SDK
- Windows 10 이상

### 빌드 방법

1. **패키지 복원**
```bash
dotnet restore
```

2. **빌드**
```bash
dotnet build
```

3. **실행**
```bash
dotnet run
```

또는 Visual Studio에서 F5를 눌러 실행

## ⚙️ 설정

### App.config 설정

프로젝트 루트의 `App.config` 파일에서 다음 값을 수정하세요:

```xml
<appSettings>
    <!-- 실제 Admin Key로 변경하세요 -->
    <add key="AdminKey" value="your-actual-admin-key-here" />
    
    <!-- API 서버 URL (필요시 변경) -->
    <add key="ApiBaseUrl" value="https://elec-license.vercel.app" />
</appSettings>
```

⚠️ **중요**: `AdminKey`를 실제 Vercel 서버의 Admin Key로 변경해야 합니다!

## 📖 사용법

### 라이선스 추가
1. **➕ Add** 버튼 클릭
2. Machine ID 입력
3. 만료일 설정 (또는 "No Expiry" 체크)
4. **OK** 클릭

### 라이선스 수정
1. 수정할 라이선스 선택
2. **✏️ Edit** 버튼 클릭
3. 정보 수정
4. **OK** 클릭

### 라이선스 삭제
1. 삭제할 라이선스 선택
2. **🗑️ Delete** 버튼 클릭
3. 확인 다이얼로그에서 **Yes** 클릭

### 검색
- 우측 상단 검색창에 Machine ID를 입력하면 실시간으로 필터링됩니다

### CSV 내보내기
1. **📄 Export** 버튼 클릭
2. 저장 위치 선택
3. 파일명 입력 (기본값: licenses_YYYYMMDD.csv)

## 🏗️ 프로젝트 구조

```
LicenseAdmin/
├── MainForm.cs                    # 메인 폼 로직
├── MainForm.Designer.cs           # 메인 폼 UI 디자인
├── MainForm.resx                  # 메인 폼 리소스
├── AddEditDialog.cs               # 추가/수정 다이얼로그 로직
├── AddEditDialog.Designer.cs      # 다이얼로그 UI 디자인
├── VercelApiClient.cs             # API 클라이언트
├── App.config                     # 설정 파일
├── LicenseAdmin.csproj           # 프로젝트 파일
└── README.md                      # 이 파일
```

## 🔒 보안 주의사항

1. **Admin Key 보호**
   - App.config 파일을 버전 관리에 커밋하지 마세요
   - .gitignore에 App.config 추가 권장

2. **HTTPS 사용**
   - 반드시 HTTPS URL 사용
   - 인증서 검증 활성화

3. **접근 제한**
   - 관리자만 접근 가능하도록 설정
   - 프로그램 실행 권한 제한

## 🐛 문제 해결

### "AdminKey not configured" 오류
- App.config 파일에 AdminKey가 설정되었는지 확인하세요

### "Network error" 오류
- 인터넷 연결 확인
- 방화벽 설정 확인
- API URL이 올바른지 확인

### "Request timeout" 오류
- 네트워크 속도 확인
- 서버 상태 확인

## 📝 라이선스

이 프로젝트는 내부 사용을 위한 관리 도구입니다.

## 📞 지원

문제가 발생하면 개발팀에 문의하세요.
