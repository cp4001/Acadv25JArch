# 🔄 License Admin App 업데이트 가이드

**버전:** v2.0  
**날짜:** 2025-11-05  
**기준:** FINAL-SETUP.md

---

## ✅ 완료된 수정 사항

### 1. 새로운 필드 추가
- ✅ **Product** (제품명) 필드
- ✅ **Username** (사용자명) 필드

### 2. API 클라이언트 업데이트
**파일:** `VercelApiClient.cs`
- ✅ `LicenseInfo` 클래스에 `Product`, `Username` 속성 추가
- ✅ `RegisterLicenseAsync()` 메서드에 product, username 파라미터 추가
- ✅ `UpdateLicenseAsync()` 메서드에 product, username 파라미터 추가

### 3. UI 업데이트
**파일:** `AddEditDialog.Designer.cs`, `AddEditDialog.cs`
- ✅ Product 입력 텍스트박스 추가
- ✅ Username 입력 텍스트박스 추가
- ✅ 다이얼로그 크기 조정 (260px → 390px)
- ✅ 생성자에 product, username 파라미터 추가

### 4. 메인 폼 업데이트
**파일:** `MainForm.cs`
- ✅ DataGridView에 "제품", "사용자" 컬럼 추가
- ✅ 검색 기능에 Product, Username 포함
- ✅ CSV 내보내기에 새 필드 추가
- ✅ 추가/수정 시 새 필드 전달

---

## 🎯 변경 전후 비교

### 라이선스 등록 (Before)
```csharp
await VercelApiClient.RegisterLicenseAsync(
    dialog.MachineId, 
    dialog.ExpiryDate);
```

### 라이선스 등록 (After)
```csharp
await VercelApiClient.RegisterLicenseAsync(
    dialog.MachineId,
    dialog.Product,      // ⭐ 신규
    dialog.Username,     // ⭐ 신규
    dialog.ExpiryDate);
```

---

## 🖥️ UI 변경 사항

### AddEditDialog (이전)
```
┌─────────────────────────┐
│ Machine ID:             │
│ [텍스트박스]             │
│                         │
│ Expiry Date:            │
│ [날짜선택]               │
│ □ No Expiry             │
│                         │
│        [OK] [Cancel]    │
└─────────────────────────┘
높이: 260px
```

### AddEditDialog (현재)
```
┌─────────────────────────┐
│ Machine ID:             │
│ [텍스트박스]             │
│                         │
│ Product:            ⭐  │
│ [텍스트박스]        ⭐  │
│                         │
│ Username:           ⭐  │
│ [텍스트박스]        ⭐  │
│                         │
│ Expiry Date:            │
│ [날짜선택]               │
│ □ No Expiry             │
│                         │
│        [OK] [Cancel]    │
└─────────────────────────┘
높이: 390px
```

---

## 📊 DataGridView 컬럼 변경

### 이전 컬럼
1. Machine ID
2. 상태
3. 만료일
4. 남은 기간
5. 등록일
6. 수정일

### 현재 컬럼
1. Machine ID
2. **제품** ⭐
3. **사용자** ⭐
4. 상태
5. 만료일
6. 남은 기간
7. 등록일
8. 수정일

---

## 🔧 빌드 및 테스트

### 1. 빌드
```cmd
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense\CSharp\LicenseAdminApp
dotnet build
```

### 2. 실행
```cmd
dotnet run
```

또는 Visual Studio에서 F5

### 3. 테스트 시나리오

#### 테스트 1: 라이선스 추가
1. **Add** 버튼 클릭
2. 입력:
   - Machine ID: `TEST-APP-001`
   - Product: `MyApplication`
   - Username: `Test User`
   - Expiry Date: `2025-12-31`
3. **OK** 클릭
4. 성공 메시지 확인

#### 테스트 2: 목록 확인
1. 추가된 라이선스 확인
2. "제품", "사용자" 컬럼에 데이터 표시 확인

#### 테스트 3: 검색
1. 검색창에 "MyApplication" 입력
2. 필터링 확인

#### 테스트 4: CSV 내보내기
1. **Export** 버튼 클릭
2. CSV 파일 저장
3. 파일 열어서 Product, Username 컬럼 확인

---

## ⚠️ 주의사항

### API 서버와의 호환성
- ✅ API 서버가 product, username 필드를 지원해야 함
- ✅ FINAL-SETUP.md 기준으로 이미 지원됨
- ✅ 테이블: `jlicense` (Neon DB)

### 기존 데이터
- ✅ 기존 라이선스는 Product, Username이 NULL로 표시됨
- ✅ 수정 시 값을 채울 수 있음
- ✅ NULL 값은 "-"로 표시

---

## 🐛 알려진 문제

### 없음
현재 알려진 문제 없음

---

## 📝 다음 업데이트 계획

### 향후 추가 기능
- [ ] 라이선스 일괄 등록 (CSV 가져오기)
- [ ] 만료 임박 라이선스 알림
- [ ] 통계 대시보드
- [ ] 라이선스 활성화 이력

---

## 📞 문의

문제 발생 시:
1. FINAL-SETUP.md 참조
2. Vercel 로그 확인
3. Neon Console 확인

---

**업데이트 완료일:** 2025-11-05  
**버전:** v2.0  
**호환 API:** https://elec-license.vercel.app
