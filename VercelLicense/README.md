# 🔐 Vercel License Server (Neon PostgreSQL)

Neon PostgreSQL을 사용한 라이선스 관리 서버

---

## 📋 목차
1. [설치 및 배포](#설치-및-배포)
2. [API 엔드포인트](#api-엔드포인트)
3. [C# 클라이언트 사용법](#c-클라이언트-사용법)

---

## 🚀 설치 및 배포

### 1단계: Neon PostgreSQL 연결
1. [Vercel 대시보드](https://vercel.com/dashboard) 접속
2. 프로젝트 선택
3. **Storage** 탭 클릭
4. **Connect Store** → **Neon** 선택
5. Neon 계정 연동 (무료)
6. 데이터베이스 생성 완료

자동으로 환경 변수가 설정됩니다:
```
POSTGRES_URL
POSTGRES_PRISMA_URL
POSTGRES_URL_NON_POOLING
```

### 2단계: 환경 변수 추가
Vercel 대시보드 → Settings → Environment Variables:

```
ENCRYPTION_KEY=YourSecretKey123
ADMIN_KEY=super-secret-admin-key-change-me
```

### 3단계: 프로젝트 배포
```bash
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense

# 의존성 설치
npm install

# Vercel 로그인
vercel login

# 배포
vercel --prod
```

### 4단계: 데이터베이스 초기화
배포 후 테이블 생성:

```powershell
$body = @{
    adminKey = "super-secret-admin-key-change-me"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-project.vercel.app/api/init-db" `
    -Method POST -Body $body -ContentType "application/json"
```

응답:
```json
{
  "success": true,
  "message": "Database initialized successfully"
}
```

---

## 📡 API 엔드포인트

### 1. 데이터베이스 초기화 (최초 1회)
**POST** `/api/init-db`

**요청:**
```json
{
  "adminKey": "your-admin-key"
}
```

### 2. 라이선스 확인 (클라이언트용)
**POST** `/api/check-license`

**요청:**
```json
{
  "id": "MACHINE-ABC-123"
}
```

**성공 응답 (200):**
```json
{
  "success": true,
  "valid": true,
  "key": "YourSecretKey123",
  "expiresAt": "2025-12-31",
  "registeredAt": "2025-01-01T00:00:00Z"
}
```

### 3. ID 등록 (관리자 전용)
**POST** `/api/register-id`

**요청:**
```json
{
  "adminKey": "your-admin-key",
  "id": "MACHINE-ABC-123",
  "product": "MyApplication",
  "username": "John Doe",
  "expiresAt": "2025-12-31"
}
```

**참고:** `product`, `username`, `expiresAt`는 선택사항입니다.

### 4. ID 삭제 (관리자 전용)
**POST** `/api/delete-id`

**요청:**
```json
{
  "adminKey": "your-admin-key",
  "id": "MACHINE-ABC-123"
}
```

### 5. 모든 ID 조회 (관리자 전용)
**POST** `/api/list-ids`

**요청:**
```json
{
  "adminKey": "your-admin-key"
}
```

**응답:**
```json
{
  "success": true,
  "count": 2,
  "licenses": [
    {
      "id": "MACHINE-ABC-123",
      "product": "MyApplication",
      "username": "John Doe",
      "valid": true,
      "registered_at": "2025-01-01T00:00:00Z",
      "expires_at": "2025-12-31",
      "updated_at": "2025-01-01T00:00:00Z"
    }
  ]
}
```

---

## 💻 C# 클라이언트 사용법

`client-examples/CSharpClient.cs` 참고

```csharp
// 머신 ID 생성
string machineId = LicenseHelper.GetMachineId();

// 서버에서 키 받기
string key = await LicenseHelper.GetEncryptionKeyFromServer(machineId);
```

---

## 🗄️ 데이터베이스 구조

### licenses 테이블
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
- `machine_id`: 머신/클라이언트 고유 식별자 (필수, 유니크)
- `product`: 제품명 (선택사항)
- `username`: 사용자명 (선택사항)
- `valid`: 라이선스 유효 여부
- `registered_at`: 등록 일시
- `expires_at`: 만료 날짜 (선택사항)
- `updated_at`: 마지막 수정 일시

---

## 🔧 관리 명령어

### ID 등록
```powershell
# 기본 등록
$body = @{
    adminKey = "your-admin-key"
    id = "MACHINE-ABC-123"
    expiresAt = "2025-12-31"
} | ConvertTo-Json

# 제품명과 사용자명 포함 등록
$body = @{
    adminKey = "your-admin-key"
    id = "MACHINE-ABC-123"
    product = "MyApplication"
    username = "John Doe"
    expiresAt = "2025-12-31"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-project.vercel.app/api/register-id" `
    -Method POST -Body $body -ContentType "application/json"
```

### ID 조회
```powershell
$body = @{
    adminKey = "your-admin-key"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-project.vercel.app/api/list-ids" `
    -Method POST -Body $body -ContentType "application/json"
```

---

## 💰 비용

### Neon 무료 플랜
- 저장소: 3GB
- 활성 시간: 100시간/월
- 충분히 소규모 라이선스 관리 가능

### Vercel 무료 플랜
- Serverless Functions: 100GB-시간/월
- 10만 요청/월

---

## 🆚 KV vs Neon 비교

| 기능 | Vercel KV | Neon PostgreSQL |
|------|-----------|-----------------|
| 타입 | Redis | PostgreSQL |
| 무료 저장소 | 256MB | 3GB |
| 무료 요청 | 10만/월 | 무제한 |
| 쿼리 복잡도 | 간단 | 복잡 가능 |
| 관계형 | ❌ | ✅ |
| SQL 지원 | ❌ | ✅ |

**Neon 장점:**
- ✅ 더 많은 저장 공간
- ✅ 복잡한 쿼리 가능
- ✅ 관계형 데이터 관리
- ✅ 백업 자동화

---

## 🐛 문제 해결

### "POSTGRES_URL not found"
→ Vercel Storage에서 Neon 연결 확인

### 테이블이 없다는 오류
→ `/api/init-db` 호출

### 연결 타임아웃
→ Neon 대시보드에서 데이터베이스 상태 확인

---

## 📄 라이선스

MIT License


