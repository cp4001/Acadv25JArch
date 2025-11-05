# 🎯 빠른 시작 가이드 (Neon PostgreSQL)

Vercel + Neon으로 5분 안에 라이선스 서버 배포!

---

## ⚡ 4단계로 시작하기

### 1️⃣ 설치
```bash
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense
npm install
```

### 2️⃣ 배포
```bash
vercel login
vercel --prod
```

### 3️⃣ Neon 연결
1. Vercel 대시보드 → Storage → Connect Store → **Neon**
2. Neon 계정 연동 (무료)
3. 자동으로 환경 변수 설정 ✅

### 4️⃣ 데이터베이스 초기화
```powershell
$body = @{
    adminKey = "your-admin-key"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-project.vercel.app/api/init-db" `
    -Method POST -Body $body -ContentType "application/json"
```

완료! 🎉

---

## 📝 환경 변수 설정

Vercel → Settings → Environment Variables:

```
ENCRYPTION_KEY=YourSecretKey123
ADMIN_KEY=super-secret-admin-key-change-me
```

---

## 📝 첫 라이선스 등록

```powershell
$body = @{
    adminKey = "your-admin-key"
    id = "MACHINE-TEST-123"
    expiresAt = "2025-12-31"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-project.vercel.app/api/register-id" `
    -Method POST -Body $body -ContentType "application/json"
```

---

## 💻 C# 클라이언트 사용

```csharp
// 1. client-examples/CSharpClient.cs 복사
// 2. API_URL 수정
// 3. 사용:

string machineId = LicenseHelper.GetMachineId();
string key = await LicenseHelper.GetEncryptionKeyFromServer(machineId);
```

---

## 🆚 왜 Neon?

| 항목 | Vercel KV | Neon PostgreSQL |
|------|-----------|-----------------|
| 무료 저장소 | 256MB | **3GB** ✅ |
| 무료 요청 | 10만/월 | **무제한** ✅ |
| SQL 지원 | ❌ | ✅ |
| 복잡한 쿼리 | ❌ | ✅ |
| 관계형 DB | ❌ | ✅ |

**Neon이 더 좋습니다!**

---

## 📚 자세한 가이드

- [README.md](README.md) - API 문서
- [DEPLOYMENT.md](DEPLOYMENT.md) - 배포 가이드
- [client-examples/](client-examples/) - 클라이언트 예제

---

## 🐛 문제 해결

**테이블 없음 오류:**
```powershell
# /api/init-db 호출
$body = @{ adminKey = "your-key" } | ConvertTo-Json
Invoke-RestMethod -Uri "https://your-app.vercel.app/api/init-db" -Method POST -Body $body -ContentType "application/json"
```

**환경 변수 안 보임:**
```bash
vercel env pull
```

---

## ✅ 체크리스트

- [ ] npm install 완료
- [ ] vercel --prod 배포 완료
- [ ] Neon 연결 완료
- [ ] /api/init-db 호출 완료
- [ ] 환경 변수 설정 완료
- [ ] 테스트 ID 등록 완료
- [ ] 클라이언트 코드 통합 완료

---

**즐거운 개발 되세요!** 🚀
