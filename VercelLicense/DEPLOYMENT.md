# 🚀 Neon PostgreSQL License Server 배포 가이드

## 📋 배포 순서

### 1단계: 프로젝트 준비
```bash
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense

# 의존성 재설치 (Neon용으로 변경됨)
npm install
```

---

### 2단계: Vercel CLI 설치 및 로그인
```bash
# Vercel CLI 설치 (이미 설치했으면 생략)
npm install -g vercel

# 로그인
vercel login
```

---

### 3단계: Neon PostgreSQL 연결

#### Vercel 대시보드에서:
1. https://vercel.com/dashboard 접속
2. 프로젝트 생성 (또는 기존 프로젝트 선택)
3. **Storage** 탭 클릭
4. **Connect Store** 버튼
5. **Neon** 선택
6. **Continue with Neon** 클릭
7. Neon 계정 연동 (무료, GitHub 계정으로 가능)
8. 데이터베이스 이름 입력
9. **Create** 클릭

✅ 자동으로 환경 변수가 설정됩니다!

---

### 4단계: 환경 변수 설정

#### Vercel 대시보드에서:
1. 프로젝트 → Settings → Environment Variables
2. 다음 변수 추가:

```
ENCRYPTION_KEY=YourSecretKey123
ADMIN_KEY=super-secret-admin-key-change-me-12345
```

**중요:** ADMIN_KEY는 강력한 랜덤 문자열로 변경하세요!

---

### 5단계: 프로젝트 배포
```bash
# 첫 배포
vercel

# 질문에 답변:
# Set up and deploy? Y
# Which scope? (계정 선택)
# Link to existing project? N (처음) / Y (이미 있으면)
# What's your project's name? vercel-license-server
# In which directory is your code located? ./

# 프로덕션 배포
vercel --prod
```

배포 완료! URL 표시:
```
✅ Production: https://your-project.vercel.app
```

---

### 6단계: 데이터베이스 초기화 (중요!)

배포 후 **반드시** 테이블 생성:

#### PowerShell:
```powershell
$url = "https://your-project.vercel.app/api/init-db"  # ⚠️ 실제 URL로 변경
$body = @{
    adminKey = "super-secret-admin-key-change-me-12345"  # ⚠️ 실제 키로 변경
} | ConvertTo-Json

Invoke-RestMethod -Uri $url -Method POST -Body $body -ContentType "application/json"
```

#### cURL:
```bash
curl -X POST https://your-project.vercel.app/api/init-db \
  -H "Content-Type: application/json" \
  -d '{"adminKey": "super-secret-admin-key-change-me-12345"}'
```

성공 응답:
```json
{
  "success": true,
  "message": "Database initialized successfully",
  "table": "licenses"
}
```

---

### 7단계: 테스트 ID 등록

```powershell
$body = @{
    adminKey = "super-secret-admin-key-change-me-12345"
    id = "MACHINE-TEST-123"
    expiresAt = "2025-12-31"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-project.vercel.app/api/register-id" `
    -Method POST -Body $body -ContentType "application/json"
```

---

### 8단계: 라이선스 확인 테스트

```powershell
$body = @{
    id = "MACHINE-TEST-123"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-project.vercel.app/api/check-license" `
    -Method POST -Body $body -ContentType "application/json"
```

성공 응답:
```json
{
  "success": true,
  "valid": true,
  "key": "YourSecretKey123",
  "expiresAt": "2025-12-31"
}
```

---

## 🔧 업데이트 배포

코드 수정 후:
```bash
vercel --prod
```

---

## 🗄️ Neon 대시보드 접근

데이터베이스 직접 관리:
1. https://console.neon.tech 접속
2. 프로젝트 선택
3. **SQL Editor** 에서 직접 쿼리 실행

예시 쿼리:
```sql
-- 모든 라이선스 조회
SELECT * FROM licenses;

-- 특정 ID 확인
SELECT * FROM licenses WHERE machine_id = 'MACHINE-TEST-123';

-- 만료된 라이선스 확인
SELECT * FROM licenses WHERE expires_at < CURRENT_DATE;
```

---

## 🐛 문제 해결

### "relation licenses does not exist"
→ `/api/init-db` API 호출 안 했음. 6단계 실행

### "POSTGRES_URL is not defined"
→ Vercel Storage에서 Neon 재연결

### 환경 변수 적용 안 됨
```bash
vercel env pull  # 로컬에서 확인
vercel --prod    # 재배포
```

### 테이블 재생성 필요
Neon SQL Editor에서:
```sql
DROP TABLE IF EXISTS licenses;
```
그리고 `/api/init-db` 재호출

---

## 📊 모니터링

### Vercel 대시보드:
- **Analytics** - API 호출 수, 응답 시간
- **Logs** - 실시간 에러 로그

### Neon 대시보드:
- **Monitoring** - 데이터베이스 성능
- **Branches** - 데이터베이스 브랜치 관리

---

## 💰 비용 확인

### Neon 무료 플랜 한도:
- 스토리지: 3GB
- 활성 시간: 100시간/월 (자동 일시 정지)
- 충분히 소규모 사용 가능

### 초과 시:
- Pro 플랜: $19/월 (무제한)

---

## 🔐 보안 체크리스트

- [x] Neon 데이터베이스 연결 완료
- [x] 데이터베이스 초기화 완료
- [ ] ADMIN_KEY를 강력한 랜덤 값으로 변경
- [ ] ENCRYPTION_KEY를 고유한 값으로 변경
- [ ] 클라이언트 코드에 API URL 업데이트
- [ ] 테스트 ID로 확인 완료

---

## 🎉 완료!

배포된 URL: `https://your-project.vercel.app`

이제 C# 클라이언트 코드에서 이 URL을 사용하세요!

다음 단계: `client-examples/CSharpClient.cs` 확인
