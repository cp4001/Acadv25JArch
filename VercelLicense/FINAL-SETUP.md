# 🎉 Vercel + Neon License Server 최종 구성 문서

프로젝트 설정 및 배포 완료 - 2025년 11월 5일

---

## 📊 프로젝트 구성 요약

### Vercel 배포
- **프로젝트명:** elec-license
- **프로젝트 ID:** prj_zpP2zqdeMZ2ZcdcuiiQDrjIoP8fN
- **배포 URL:** https://elec-license.vercel.app
- **배포 상태:** ✅ 운영 중

### Neon 데이터베이스
- **프로젝트명:** ElecLicense
- **리전:** AWS Singapore (ap-southeast-1)
- **엔드포인트:** ep-delicate-field-a1sam349
- **데이터베이스:** neondb
- **테이블:** jlicense

---

## 🗄️ 데이터베이스 구조

### jlicense 테이블

```sql
CREATE TABLE jlicense (
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
- `id`: 자동 증가 고유 ID (Primary Key)
- `machine_id`: 머신/클라이언트 고유 식별자 (필수, 유니크)
- `product`: 제품명 (선택사항)
- `username`: 사용자명 (선택사항)
- `valid`: 라이선스 유효 여부 (기본값: true)
- `registered_at`: 등록 일시 (자동)
- `expires_at`: 만료 날짜 (선택사항)
- `updated_at`: 마지막 수정 일시 (자동)

**인덱스:**
- PRIMARY KEY: `id`
- UNIQUE: `machine_id`
- INDEX: `machine_id`, `valid`, `product`

**중요 제약조건:**
- ✅ `machine_id`는 UNIQUE - 중복 등록 시 에러 발생
- ✅ API에서 중복 체크 구현됨

---

## 🔌 연결 정보

### PostgreSQL 연결 문자열
```
postgresql://neondb_owner:npg_8g6HskzYuhGJ@ep-delicate-field-a1sam349-pooler.ap-southeast-1.aws.neon.tech/neondb?sslmode=require
```

### Vercel 환경 변수
```
POSTGRES_URL=postgresql://...@.../neondb?sslmode=require
POSTGRES_PRISMA_URL=postgresql://...@.../neondb?pgbouncer=true&connect_timeout=15&sslmode=require
POSTGRES_URL_NON_POOLING=postgresql://...@.../neondb?sslmode=require
ENCRYPTION_KEY=YourSecretKey123
ADMIN_KEY=super-secret-admin-key-change-me-12345
```

---

## 📡 API 엔드포인트

### 1. 데이터베이스 초기화 (관리자)
**POST** `https://elec-license.vercel.app/api/init-db`

```json
{
  "adminKey": "super-secret-admin-key-change-me-12345"
}
```

### 2. 라이선스 등록 (관리자)
**POST** `https://elec-license.vercel.app/api/register-id`

```json
{
  "adminKey": "super-secret-admin-key-change-me-12345",
  "id": "MACHINE-ABC-123",
  "product": "MyApplication",
  "username": "John Doe",
  "expiresAt": "2025-12-31"
}
```

**중복 등록 시:**
```json
{
  "success": false,
  "error": "ID already exists",
  "details": "This machine ID is already registered"
}
```

### 3. 라이선스 목록 조회 (관리자)
**POST** `https://elec-license.vercel.app/api/list-ids`

```json
{
  "adminKey": "super-secret-admin-key-change-me-12345"
}
```

### 4. 라이선스 확인 (클라이언트)
**POST** `https://elec-license.vercel.app/api/check-license`

```json
{
  "id": "MACHINE-ABC-123"
}
```

**성공 응답:**
```json
{
  "success": true,
  "valid": true,
  "key": "YourSecretKey123",
  "expiresAt": "2025-12-31",
  "registeredAt": "2025-11-05T12:11:36Z"
}
```

### 5. 라이선스 삭제 (관리자)
**POST** `https://elec-license.vercel.app/api/delete-id`

```json
{
  "adminKey": "super-secret-admin-key-change-me-12345",
  "id": "MACHINE-ABC-123"
}
```

---

## 💻 PowerShell 스크립트 사용법

### 프로젝트 경로
```
C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense
```

### 배포 스크립트
```powershell
.\deploy-vercel.ps1
```

### DB 관리 스크립트

#### 초기화
```powershell
.\neon-db-query.ps1 -Init -AdminKey "super-secret-admin-key-change-me-12345"
```

#### 라이선스 등록
```powershell
.\neon-db-query.ps1 -Register `
    -Id "MACHINE-001" `
    -Product "MyApp" `
    -Username "John Doe" `
    -ExpiresAt "2025-12-31" `
    -AdminKey "super-secret-admin-key-change-me-12345"
```

#### 목록 조회
```powershell
.\neon-db-query.ps1 -List -AdminKey "super-secret-admin-key-change-me-12345"
```

#### 라이선스 확인
```powershell
.\neon-db-query.ps1 -Check -Id "MACHINE-001"
```

#### 라이선스 삭제
```powershell
.\neon-db-query.ps1 -Delete -Id "MACHINE-001" -AdminKey "super-secret-admin-key-change-me-12345"
```

---

## 🧪 테스트 결과

### ✅ 완료된 테스트

#### 1. 데이터베이스 초기화
- ✅ jlicense 테이블 생성
- ✅ 인덱스 생성

#### 2. 라이선스 등록
- ✅ 샘플 데이터 3개 등록 성공
  - TEST-MACHINE-001 (ElecApp, John Doe)
  - TEST-MACHINE-002 (PowerSuite, Jane Smith)
  - TEST-MACHINE-003 (AutoTool, Mike Johnson)

#### 3. 중복 방지
- ✅ 동일 ID 재등록 시 에러 발생 확인
- ✅ "ID already exists" 메시지 정상 출력

#### 4. 목록 조회
- ✅ 3개 라이선스 정상 조회
- ✅ product, username 필드 정상 표시

---

## 🎯 주요 기능

### 1. 라이선스 관리
- ✅ 고유 ID 기반 라이선스 등록
- ✅ 제품명, 사용자명 추가 정보 관리
- ✅ 만료일 설정
- ✅ 중복 방지 (DB 레벨 + API 레벨)

### 2. 보안
- ✅ 관리자 키 인증 (ADMIN_KEY)
- ✅ 암호화 키 배포 (ENCRYPTION_KEY)
- ✅ CORS 설정
- ✅ PostgreSQL SSL 연결

### 3. 클라이언트 검증
- ✅ 머신 ID로 라이선스 확인
- ✅ 만료일 자동 체크
- ✅ 유효성 검증

---

## 📁 프로젝트 파일 구조

```
VercelLicense/
├── api/
│   ├── init-db.js          # DB 초기화
│   ├── register-id.js      # 라이선스 등록 (중복 방지)
│   ├── list-ids.js         # 목록 조회
│   ├── check-license.js    # 라이선스 확인
│   └── delete-id.js        # 라이선스 삭제
├── client-examples/
│   └── CSharpClient.cs     # C# 클라이언트 예제
├── test-scripts/
│   └── test-api.ps1        # API 테스트 스크립트
├── deploy-vercel.ps1       # Vercel 배포 스크립트
├── neon-db-query.ps1       # Neon DB 관리 스크립트
├── deployment-url.txt      # 배포 URL
├── .vercelignore           # Vercel 배포 제외 파일
├── .gitignore              # Git 제외 파일
├── package.json            # 의존성
├── vercel.json             # Vercel 설정
├── README.md               # 프로젝트 문서
├── VercelNeon.md           # 사용 가이드
└── FINAL-SETUP.md          # 최종 구성 문서 (이 파일)
```

---

## 🔐 보안 권장사항

### 1. 관리자 키 변경
현재 기본값에서 변경:
```
ADMIN_KEY=super-secret-admin-key-change-me-12345
```

→ 강력한 키로 변경 권장 (최소 32자)

### 2. 암호화 키 변경
```
ENCRYPTION_KEY=YourSecretKey123
```

→ 복잡한 키로 변경

### 3. .env 파일 보호
- ✅ .gitignore에 .env 추가됨
- ✅ Git에 업로드되지 않음

### 4. API 접근 제한
필요 시 Vercel에서 IP 제한 설정 가능

---

## 📈 리소스 사용량

### Neon (무료 플랜)
- **스토리지:** 30.92 MB / 3 GB
- **활성 시간:** 사용량 확인 필요 / 100시간/월
- **현재 상태:** ✅ 정상

### Vercel (무료 플랜)
- **Compute:** 0.35 CU-hrs
- **Storage:** 0.15 GB
- **Network:** 0 GB
- **현재 상태:** ✅ 정상

---

## 🚀 운영 가이드

### 일상 작업

#### 새 라이선스 등록
```powershell
.\neon-db-query.ps1 -Register `
    -Id "CLIENT-ID" `
    -Product "ProductName" `
    -Username "UserName" `
    -ExpiresAt "2026-12-31" `
    -AdminKey "your-key"
```

#### 상태 확인
```powershell
.\neon-db-query.ps1 -List -AdminKey "your-key"
```

#### 라이선스 삭제
```powershell
.\neon-db-query.ps1 -Delete -Id "CLIENT-ID" -AdminKey "your-key"
```

### 백업

Neon Console에서:
1. ElecLicense 프로젝트 선택
2. **Branches** 탭
3. **Create Branch** - 백업용 브랜치 생성

### 모니터링

#### Vercel
- https://vercel.com/dashboard
- 로그 확인: Functions → Logs

#### Neon
- https://console.neon.tech
- 모니터링: Monitoring 탭

---

## 🐛 문제 해결

### "ID already exists" 에러
→ 정상 동작 (중복 방지)
→ 다른 ID 사용 또는 기존 ID 삭제 후 재등록

### "Invalid admin key" 에러
→ Vercel 환경 변수의 ADMIN_KEY 확인
→ 스크립트 실행 시 동일한 키 사용

### "database does not exist" 에러
→ POSTGRES_URL 환경 변수 확인
→ `/neondb` 경로 확인

### 테이블이 안 보임
→ Neon Console에서 올바른 프로젝트/데이터베이스 선택 확인
→ ElecLicense 프로젝트 → neondb 데이터베이스

---

## 📞 추가 리소스

- **Vercel 문서:** https://vercel.com/docs
- **Neon 문서:** https://neon.tech/docs
- **Vercel 대시보드:** https://vercel.com/dashboard
- **Neon Console:** https://console.neon.tech

---

## ✅ 최종 체크리스트

### 배포
- [✅] Vercel 프로젝트 생성 (elec-license)
- [✅] Neon DB 연결 (ElecLicense)
- [✅] 환경 변수 설정
- [✅] 프로덕션 배포

### 데이터베이스
- [✅] jlicense 테이블 생성
- [✅] 인덱스 생성
- [✅] 샘플 데이터 등록 (3개)
- [✅] 중복 방지 확인

### 코드
- [✅] API 파일 수정 (5개)
- [✅] 스크립트 작성 (2개)
- [✅] 문서 작성 (3개)
- [✅] .gitignore, .vercelignore 설정

### 테스트
- [✅] 초기화 테스트
- [✅] 등록 테스트
- [✅] 조회 테스트
- [✅] 중복 방지 테스트

---

## 🎊 완료!

**모든 설정이 완료되었습니다.**

이제 라이선스 서버를 운영할 수 있습니다.

---

**문서 작성일:** 2025년 11월 5일  
**프로젝트 경로:** `C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense`  
**배포 URL:** https://elec-license.vercel.app
