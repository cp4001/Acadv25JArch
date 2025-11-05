# 📘 Visual Studio에서 LicenseAdminApp 편집하기

## 🎨 WinForms Designer 사용 가이드

### 1️⃣ 프로젝트 열기

#### 방법 1: 솔루션 파일로 열기 (추천)
```
1. Eleclicense.sln 파일을 더블클릭
2. Visual Studio가 자동으로 실행됨
3. 솔루션 탐색기에서 LicenseAdminApp 확인
```

#### 방법 2: Visual Studio에서 직접 열기
```
1. Visual Studio 2022 실행
2. 파일 → 열기 → 프로젝트/솔루션
3. Eleclicense.sln 선택
```

---

## 🖼️ MainForm 디자이너 편집

### Form 열기
```
솔루션 탐색기에서:
LicenseAdminApp
  └── MainForm.cs (더블클릭 또는 우클릭 → 디자이너 보기)
```

### 편집 가능한 컨트롤들

#### 📊 **ToolStrip (상단 도구 모음)**
```
컨트롤 이름: toolStrip1
위치: 폼 상단

버튼들:
- btnAdd (추가)
- btnEdit (수정)  
- btnDelete (삭제)
- btnRefresh (새로고침)
- btnExport (CSV 내보내기)

편집 방법:
1. toolStrip1 클릭
2. 속성 창에서 Items 클릭
3. 컬렉션 편집기에서 버튼 추가/삭제/수정
```

#### 🔍 **검색 패널**
```
컨트롤 이름: panel1
포함 컨트롤:
- label1 (검색: 라벨)
- txtSearch (검색 텍스트박스)

편집 방법:
1. panel1 클릭
2. 속성 창에서 크기, 색상 등 변경
3. 내부 컨트롤 개별 편집 가능
```

#### 📋 **DataGridView (데이터 테이블)**
```
컨트롤 이름: dataGridView1
위치: 폼 중앙 (Dock=Fill)

편집 방법:
1. dataGridView1 클릭
2. 속성 창에서:
   - Columns: 컬럼 추가/삭제
   - BackgroundColor: 배경색 변경
   - Font: 폰트 변경
   - RowTemplate.Height: 행 높이 조정
```

#### 📊 **StatusStrip (하단 상태 표시줄)**
```
컨트롤 이름: statusStrip1
포함 항목:
- lblStatus (상태 메시지)
- lblCount (라이선스 개수)

편집 방법:
1. statusStrip1 클릭
2. 속성 창에서 Items 클릭
3. 라벨 추가/삭제/수정
```

---

## 🔧 AddEditDialog 디자이너 편집

### Dialog 열기
```
솔루션 탐색기에서:
LicenseAdminApp
  └── AddEditDialog.cs (더블클릭)
```

### 편집 가능한 컨트롤들

#### 📝 **입력 컨트롤**
```
1. txtMachineId (TextBox)
   - Machine ID 입력란
   - PlaceholderText 변경 가능

2. dtpExpiryDate (DateTimePicker)
   - 만료일 선택
   - Format 속성으로 날짜 형식 변경

3. chkNoExpiry (CheckBox)
   - 만료일 없음 체크박스
   - Text 속성으로 라벨 변경
```

#### 🔘 **버튼**
```
1. btnOK (확인 버튼)
2. btnCancel (취소 버튼)

편집:
- Text: 버튼 텍스트 변경
- Size: 버튼 크기 조정
- Location: 위치 이동
```

---

## 🎨 UI 커스터마이징 가이드

### 색상 테마 변경

#### 다크 모드 적용 예시
```csharp
// MainForm.cs의 MainForm_Load 메서드에 추가

private async void MainForm_Load(object sender, EventArgs e)
{
    // 기존 코드...
    
    // 다크 모드 적용
    this.BackColor = Color.FromArgb(32, 32, 32);
    panel1.BackColor = Color.FromArgb(45, 45, 45);
    dataGridView1.BackgroundColor = Color.FromArgb(32, 32, 32);
    dataGridView1.ForeColor = Color.White;
    dataGridView1.GridColor = Color.FromArgb(64, 64, 64);
    
    await LoadLicensesAsync();
}
```

### 폰트 변경

#### 전역 폰트 설정
```
디자이너에서:
1. 폼 선택 (MainForm 또는 AddEditDialog)
2. 속성 → Font → 맑은 고딕, 10pt
3. 모든 컨트롤에 자동 적용됨
```

### 컨트롤 크기 조정

#### DataGridView 행 높이
```
디자이너에서:
1. dataGridView1 선택
2. 속성 → RowTemplate → Height
3. 값 입력 (기본: 25, 권장: 30-35)
```

#### 버튼 크기
```
디자이너에서:
1. 버튼 선택
2. 속성 → Size → Width, Height 조정
또는:
3. 마우스로 직접 드래그하여 크기 조정
```

---

## 🔨 실전 커스터마이징 예제

### 예제 1: 로고 추가하기

```
1. 솔루션 탐색기에서 MainForm.cs 디자이너 열기
2. 도구 상자에서 PictureBox 드래그
3. 속성 설정:
   - Name: picLogo
   - SizeMode: StretchImage
   - Dock: None
   - Location: 상단 원하는 위치
4. 속성 → Image → 로컬 리소스 선택하여 이미지 로드
```

### 예제 2: 필터 콤보박스 추가

```
1. panel1에 ComboBox 추가
2. 속성 설정:
   - Name: cmbFilter
   - DropDownStyle: DropDownList
   - Items 추가:
     * 전체
     * 유효
     * 무효
     * 만료 임박
3. MainForm.cs 코드에서 이벤트 추가:
```

```csharp
private void cmbFilter_SelectedIndexChanged(object sender, EventArgs e)
{
    var filter = cmbFilter.SelectedItem?.ToString();
    
    if (filter == "전체" || string.IsNullOrEmpty(filter))
    {
        bindingSource.DataSource = allLicenses;
    }
    else if (filter == "유효")
    {
        bindingSource.DataSource = allLicenses.Where(l => l.Valid).ToList();
    }
    else if (filter == "무효")
    {
        bindingSource.DataSource = allLicenses.Where(l => !l.Valid).ToList();
    }
    else if (filter == "만료 임박")
    {
        bindingSource.DataSource = allLicenses
            .Where(l => l.DaysRemaining.HasValue && l.DaysRemaining <= 30)
            .ToList();
    }
    
    bindingSource.ResetBindings(false);
}
```

### 예제 3: 상태 표시등 추가

```
1. statusStrip1에 ToolStripStatusLabel 추가
2. 속성:
   - Name: lblServerStatus
   - Text: ● 서버 연결됨
   - ForeColor: Green
3. MainForm.cs에서 서버 상태 체크:
```

```csharp
private async Task CheckServerStatus()
{
    try
    {
        await VercelApiClient.ListAllLicensesAsync();
        lblServerStatus.Text = "● 서버 연결됨";
        lblServerStatus.ForeColor = Color.Green;
    }
    catch
    {
        lblServerStatus.Text = "● 서버 연결 끊김";
        lblServerStatus.ForeColor = Color.Red;
    }
}
```

### 예제 4: DataGridView 컬럼 색상 구분

MainForm.cs의 `ConfigureDataGridColumns()` 메서드 수정:

```csharp
private void ConfigureDataGridColumns()
{
    // 기존 코드...
    
    // 상태 컬럼 색상 설정
    if (dataGridView1.Columns["Status"] != null)
    {
        dataGridView1.Columns["Status"].DefaultCellStyle.Font = 
            new Font(dataGridView1.Font, FontStyle.Bold);
    }
    
    // 데이터 바인딩 후 셀 색상 변경 이벤트
    dataGridView1.CellFormatting += (s, e) =>
    {
        if (e.ColumnIndex == dataGridView1.Columns["Status"].Index)
        {
            if (e.Value?.ToString() == "✅ Valid")
            {
                e.CellStyle.ForeColor = Color.Green;
            }
            else if (e.Value?.ToString() == "❌ Invalid")
            {
                e.CellStyle.ForeColor = Color.Red;
            }
        }
    };
}
```

---

## 🛠️ 디버깅 및 테스트

### F5로 실행하여 테스트
```
1. 수정 완료 후 저장 (Ctrl+S)
2. F5 키로 디버그 실행
3. 변경 사항 확인
4. 문제가 있으면 다시 디자이너로 돌아가 수정
```

### 중단점 설정하여 디버깅
```
1. 코드 라인 왼쪽 여백 클릭 (빨간 점 생김)
2. F5로 실행
3. 해당 라인에서 멈춤
4. F10으로 한 줄씩 실행하며 확인
```

---

## 📋 자주 사용하는 단축키

| 단축키 | 기능 |
|--------|------|
| **F5** | 디버그 실행 |
| **Shift+F5** | 디버그 중지 |
| **F7** | 코드 보기 |
| **Shift+F7** | 디자이너 보기 |
| **F10** | 한 줄씩 실행 |
| **Ctrl+Space** | IntelliSense 표시 |
| **Ctrl+K, Ctrl+D** | 코드 정렬 |
| **Ctrl+S** | 저장 |
| **Ctrl+Shift+B** | 빌드 |

---

## 🎯 권장 커스터마이징

### 초보자용
1. ✅ 색상 테마 변경
2. ✅ 버튼 텍스트 수정
3. ✅ 폰트 크기 조정
4. ✅ 폼 크기 조정

### 중급자용
5. ✅ 새로운 컨트롤 추가
6. ✅ 필터 기능 추가
7. ✅ 아이콘 변경
8. ✅ 상태 표시 개선

### 고급자용
9. ✅ 다중 탭 인터페이스
10. ✅ 차트/그래프 추가
11. ✅ 드래그 앤 드롭 기능
12. ✅ 커스텀 렌더링

---

## 💡 프로 팁

### 1. 디자이너가 안 열릴 때
```
1. 프로젝트 클린 (빌드 → 솔루션 정리)
2. 프로젝트 리빌드 (빌드 → 솔루션 다시 빌드)
3. Visual Studio 재시작
```

### 2. 컨트롤이 안 보일 때
```
속성 → Visible이 False로 되어있는지 확인
```

### 3. 레이아웃 꼬였을 때
```
Ctrl+Z로 되돌리기
또는
편집 → 실행 취소
```

### 4. 코드와 디자이너 동기화
```
항상 저장(Ctrl+S) 후 F5로 실행하여 확인
```

---

## 📚 더 배우기

### Visual Studio 공식 문서
- [WinForms 디자이너 사용법](https://docs.microsoft.com/ko-kr/dotnet/desktop/winforms/controls/)
- [컨트롤 레이아웃](https://docs.microsoft.com/ko-kr/dotnet/desktop/winforms/controls/layout)

### 추천 YouTube 채널
- IAmTimCorey
- Programming with Mosh (영어)

---

**Happy Designing! 🎨**

궁금한 점이 있으면 README.md를 참고하세요!
