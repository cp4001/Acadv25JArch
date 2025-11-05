# 🚀 Vercel + Neon 프로젝트 배포 및 사용 가이드

이 문서는 Vercel License Server 프로젝트의 배포부터 DB 관리까지 전체 과정을 설명합니다.

---

## 📋 목차
1. [사전 준비](#사전-준비)
2. [Vercel 배포](#vercel-배포)
3. [Neon DB 연결](#neon-db-연결)
4. [환경 변수 설정](#환경-변수-설정)
5. [DB 초기화 및 관리](#db-초기화-및-관리)
6. [문제 해결](#문제-해결)

---

## 🔧 사전 준비

### 1. 필수 도구 설치

```powershell
# Node.js 설치 확인
node --version

# Vercel CLI 설치
npm install -g vercel

# 설치 확인
vercel --version
```

### 2. 계정 준비
- Vercel 계정 (https://vercel.com)
- Neon 계정 (자동 생성됨)

---

## 🚀 Vercel 배포

### 자동 배포 스크립트 사용

프로젝트 폴더에서 PowerShell 실행:

```powershell
# 프로젝트 디렉토리로 이동
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense

# 배포 스크립트 실행
.\deploy-vercel.ps1
```

**스크립트가 자동으로 수행하는 작업:**
1. ✅ Vercel CLI 설치 확인
2. ✅ npm 의존성 설치
3. ✅ Vercel 로그인 (필요시)
4. ✅ 프로덕션 배포
5. ✅ 배포 URL 저장 (`deployment-url.txt`)

### 수동 배포 (선택)

```powershell
# 로그인
vercel login

# 프로덕션 배포
vercel --prod

# 개발 서버 실행
vercel dev
```

### 배포 확인

배포 성공 시 출력:
```
✓ 배포 성공!
배포 URL: https://your-project.vercel.app
URL이 deployment-url.txt에 저장되었습니다
```

---

## 🗄️ Neon DB 연결

### 1. Vercel 대시보드 접속
1. https://vercel.com/dashboard 접속
2. 배포한 프로젝트 선택

### 2. Storage 연결
1. **Storage** 탭 클릭
2. **Connect Store** 버튼 클릭
3. **Neon** 선택
4. **Continue** → Neon 계정 자동 생성
5. 데이터베이스 이름 입력 (기본값 사용 가능)
6. **Connect** 클릭

### 3. 자동 생성되는 환경 변수
```
POSTGRES_URL=postgresql://...
POSTGRES_PRISMA_URL=postgresql://...
POSTGRES_URL_NON_POOLING=postgresql://...
```

**주의:** 이 변수들은 자동으로 Vercel 프로젝트에 추가됩니다.

---

## ⚙️ 환경 변수 설정

### Vercel 대시보드에서 설정

1. 프로젝트 → **Settings** → **Environment Variables**
2. 다음 변수 추가:

| 변수명 | 값 예시 | 설명 |
|--------|---------|------|
| `ENCRYPTION_KEY` | `YourSecretKey123` | 클라이언트에게 전달할 암호화 키 |
| `ADMIN_KEY` | `super-secret-admin-key-12345` | 관리자 인증 키 |

### Production/Preview/Development 선택
- **Production**: 체크 (필수)
- Preview: 선택 사항
- Development: 선택 사항

### 저장 후 재배포
환경 변수 변경 시 자동으로 재배포됩니다.

---

## 💾 DB 초기화 및 관리

### 1. 데이터베이스 초기화 (최초 1회)

```powershell
# 스크립트로 초기화
.\neon-db-query.ps1 -Init -AdminKey "super-secret-admin-key-12345"
```

**생성되는 테이블:**
```sql
CREATE TABLE licenses (
    id SERIAL PRIMARY KEY,
    machine_id VARCHAR(255) UNIQUE NOT NULL,
    product VARCHAR(100),
    username VARCHAR(100),
    valid BOOLEAN DEFAULT true,
    registered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at DATE,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**필드 설명:**
- `id`: 자동 증가 고유 ID
- `machine_id`: 머신 ID (필수, 유니크)
- `product`: 제품명 (선택)
- `username`: 사용자명 (선택)
- `valid`: 유효 여부
- `registered_at`: 등록일시
- `expires_at`: 만료날짜 (선택)
- `updated_at`: 수정일시

**초기화 결과:**
```
✓ 데이터베이스 초기화 성공
{
  "success": true,
  "message": "Database initialized successfully"
}
```

---

### 2. 라이선스 ID 등록

```powershell
# 기본 등록
.\neon-db-query.ps1 -Register `
    -Id "MACHINE-ABC-123" `
    -ExpiresAt "2025-12-31" `
    -AdminKey "super-secret-admin-key-12345"

# 제품명과 사용자명 포함 등록
.\neon-db-query.ps1 -Register `
    -Id "MACHINE-ABC-123" `
    -Product "MyApplication" `
    -Username "John Doe" `
    -ExpiresAt "2025-12-31" `
    -AdminKey "super-secret-admin-key-12345"
```

**결과:**
```
✓ 등록 성공
{
  "success": true,
  "message": "ID registered successfully",
  "machineId": "MACHINE-ABC-123"
}
```

---

### 3. 등록된 라이선스 조회

```powershell
# 전체 목록 조회
.\neon-db-query.ps1 -List -AdminKey "super-secret-admin-key-12345"
```

**결과:**
```
✓ 조회 성공 (총 2개)

등록된 라이선스:
  [✓ 유효] ID: MACHINE-ABC-123
      제품: MyApplication
      사용자: John Doe
      등록일: 2025-01-01T00:00:00Z
      만료일: 2025-12-31

  [✓ 유효] ID: MACHINE-XYZ-789
      제품: AnotherApp
      사용자: Jane Smith
      등록일: 2025-01-15T10:30:00Z
      만료일: 2026-01-31
```

---

### 4. 라이선스 확인 (클라이언트용)

```powershell
# 특정 ID 유효성 검증
.\neon-db-query.ps1 -Check -Id "MACHINE-ABC-123"
```

**유효한 경우:**
```
✓ 유효한 라이선스
  암호화 키: YourSecretKey123
  만료일: 2025-12-31
  등록일: 2025-01-01T00:00:00Z
```

**무효한 경우:**
```
✗ 확인 실패: License not found
```

---

### 5. 라이선스 삭제

```powershell
# 특정 ID 삭제
.\neon-db-query.ps1 -Delete `
    -Id "MACHINE-ABC-123" `
    -AdminKey "super-secret-admin-key-12345"
```

**결과:**
```
✓ 삭제 성공
{
  "success": true,
  "message": "ID deleted successfully"
}
```

---

## 📊 스크립트 옵션 정리

### deploy-vercel.ps1

| 옵션 | 설명 |
|------|------|
| `-SkipLogin` | Vercel 로그인 건너뛰기 |
| `-Dev` | 개발 서버 모드로 실행 |

**사용 예:**
```powershell
.\deploy-vercel.ps1 -SkipLogin
.\deploy-vercel.ps1 -Dev
```

---

### neon-db-query.ps1

| 옵션 | 필수 파라미터 | 설명 |
|------|---------------|------|
| `-Init` | `-AdminKey` | DB 초기화 |
| `-List` | `-AdminKey` | 전체 목록 조회 |
| `-Register` | `-AdminKey`, `-Id` | 새 라이선스 등록 |
| `-Delete` | `-AdminKey`, `-Id` | 라이선스 삭제 |
| `-Check` | `-Id` | 라이선스 확인 |

**공통 옵션:**
- `-Url`: Vercel URL (자동 로드 가능)
- `-AdminKey`: 관리자 키 (자동 로드 가능)
- `-Product`: 제품명 (등록 시 선택)
- `-Username`: 사용자명 (등록 시 선택)
- `-ExpiresAt`: 만료날짜 (등록 시 선택, 기본값: 2025-12-31)

**URL 자동 로드:**
- `deployment-url.txt` 파일이 있으면 자동으로 URL 읽기

**사용 예:**
```powershell
# URL 자동 로드
.\neon-db-query.ps1 -List -AdminKey "your-key"

# URL 수동 지정
.\neon-db-query.ps1 -List `
    -Url "https://your-project.vercel.app" `
    -AdminKey "your-key"
```

---

## 🔄 전체 워크플로우

### 초기 설정 (1회)

```powershell
# 1. 배포
.\deploy-vercel.ps1

# 2. Vercel 대시보드에서:
#    - Neon DB 연결
#    - 환경 변수 설정 (ENCRYPTION_KEY, ADMIN_KEY)

# 3. DB 초기화
.\neon-db-query.ps1 -Init -AdminKey "your-admin-key"
```

### 일상 작업

```powershell
# 라이선스 등록 (제품명과 사용자명 포함)
.\neon-db-query.ps1 -Register -Id "NEW-MACHINE" -Product "MyApp" -Username "John" -ExpiresAt "2025-12-31" -AdminKey "your-key"

# 목록 확인
.\neon-db-query.ps1 -List -AdminKey "your-key"

# 특정 라이선스 확인
.\neon-db-query.ps1 -Check -Id "NEW-MACHINE"

# 라이선스 삭제
.\neon-db-query.ps1 -Delete -Id "OLD-MACHINE" -AdminKey "your-key"
```

---

## 🐛 문제 해결

### 1. "Vercel CLI가 설치되지 않았습니다"

```powershell
# 전역으로 설치
npm install -g vercel

# 확인
vercel --version
```

---

### 2. "POSTGRES_URL not found"

**원인:** Neon DB가 연결되지 않음

**해결:**
1. Vercel 대시보드 → Storage → Connect Store
2. Neon 선택 후 연결
3. 자동으로 환경 변수 생성됨

---

### 3. "테이블이 없다는 오류"

```powershell
# DB 초기화 실행
.\neon-db-query.ps1 -Init -AdminKey "your-admin-key"
```

---

### 4. "Invalid admin key"

**원인:** ADMIN_KEY 환경 변수와 스크립트 파라미터 불일치

**확인:**
1. Vercel 대시보드 → Settings → Environment Variables
2. `ADMIN_KEY` 값 확인
3. 스크립트 실행 시 동일한 값 사용

---

### 5. "deployment-url.txt를 찾을 수 없습니다"

**해결 1:** 배포 스크립트 재실행
```powershell
.\deploy-vercel.ps1
```

**해결 2:** 수동으로 URL 지정
```powershell
.\neon-db-query.ps1 -List -Url "https://your-project.vercel.app" -AdminKey "your-key"
```

---

### 6. Neon DB 접속 문제

**확인 사항:**
1. Neon 대시보드 (https://console.neon.tech) 접속
2. 프로젝트 상태 확인
3. "Active" 상태인지 확인
4. 무료 플랜 제한 확인:
   - 저장소: 3GB
   - 활성 시간: 100시간/월

---

## 💡 팁과 모범 사례

### 1. 보안
- ✅ `.env` 파일을 `.gitignore`에 추가
- ✅ ADMIN_KEY는 복잡하게 설정 (최소 20자)
- ✅ 정기적으로 만료된 라이선스 정리

### 2. 성능
- Neon의 "Scale to zero" 기능으로 비용 절감
- 자주 사용하지 않으면 자동으로 sleep 상태

### 3. 백업
- Neon 대시보드에서 자동 백업 활성화
- 중요한 변경 전에 스냅샷 생성

### 4. 모니터링
```powershell
# 정기적으로 라이선스 상태 확인
.\neon-db-query.ps1 -List -AdminKey "your-key"
```

---

## 📞 추가 리소스

- **Vercel 문서:** https://vercel.com/docs
- **Neon 문서:** https://neon.tech/docs
- **프로젝트 README:** `README.md`
- **배포 가이드:** `DEPLOYMENT.md`
- **빠른 시작:** `QUICKSTART.md`

---

## 📝 변경 이력

| 날짜 | 버전 | 변경 사항 |
|------|------|-----------|
| 2025-01-XX | 1.0 | 초기 문서 작성 |

---

## ✅ 체크리스트

### 초기 설정
- [ ] Node.js 설치
- [ ] Vercel CLI 설치
- [ ] Vercel 계정 생성
- [ ] 프로젝트 배포
- [ ] Neon DB 연결
- [ ] 환경 변수 설정
- [ ] DB 초기화

### 일상 관리
- [ ] 새 라이선스 등록
- [ ] 만료 라이선스 확인
- [ ] 불필요한 라이선스 삭제
- [ ] 정기 백업 확인

---

**문서 작성일:** 2025-01-XX  
**프로젝트 경로:** `C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense`
