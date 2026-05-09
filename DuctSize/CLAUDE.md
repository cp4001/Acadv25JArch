# DuctSize

엑셀 자료(`DUCT_MEASURE_1204.xls`) 기반 덕트 사이즈 결정 로직을 분석·문서화하고 C# WinForm 도구로 구현한 프로젝트.

## Obsidian
- 저장 위치: `D:\Dropbox\Obsidian\Projects\DuctSize\`
- 진입점: `INDEX.md` (프로젝트 개요 + 다른 노트 위키링크)
- 동기화 노트: `DuctSizeDesign.md`, `DuctDesignAPI.md`, `DuctSizeApp-README.md` (이 폴더 산출물의 사본)

## QMD 키워드
- 덕트, 덕트사이즈, DuctSize, DUCT_MEASURE, 풍량, 풍속, 정압, 마찰손실
- 등가원형, Huebscher, 환산표, 정방형환산표, 아스펙트비, 종횡비
- HVAC, 공조, 사각덕트, 원형덕트
- DuctDesign, OptionA, OptionD

## Build
```pwsh
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\DuctSize
dotnet build DuctSizeApp.sln -c Release
.\DuctSizeApp\bin\Release\net8.0-windows\DuctSizeApp.exe
```

또는 `DuctSizeApp.sln`을 Visual Studio에서 열어 F5.

검증 (콘솔):
```pwsh
cd DuctSizeApp\Tests
dotnet run --project SmokeTest.csproj -c Release
```

## Conventions
- 입력 명세: (L [m], Q [CMH], R [mmAq/m], SF) — R은 **단위 길이당 마찰손실** (총 ΔP 아님)
- 출력: `DuctDesignResult` (A, B, De, V, R, DeltaPActual, Aspect)
- 표준 사이즈: §5.3의 69개 정수 (50 ~ 5000 mm)
- ASP 검증: a/b ≤ 3 (옵션 A는 자동 보정, 옵션 D는 필터)

## 핵심 파일

| 파일 | 역할 |
|------|------|
| `DuctSizeDesign.md` | 엑셀 결정 로직 분석서 (5가지 시나리오, 표준 시리즈, 룩업 테이블) |
| `DuctDesignAPI.md` | C# 함수 명세 (OptionA/OptionD, 마찰선도 공식) |
| `data/duct_de_lookup.csv` | 환산표 69×69 De 매트릭스 추출본 |
| `DuctSizeApp.sln` | Visual Studio 솔루션 |
| `DuctSizeApp/DuctDesign.cs` | 핵심 라이브러리 (Tables, Math, Design, Result) |
| `DuctSizeApp/MainForm.cs` (+ `.Designer.cs`, `.resx`) | WinForm UI |
| `DuctSizeApp/Tests/SmokeTest.cs` | 콘솔 검증 러너 |

## 기타
- Form Designer 편집 가능 구조 (partial class). 컨트롤 명명: `txt`, `lbl`, `btn`, `grid`, `grp` + camelCase
- SmokeTest는 솔루션에서 제외됨 (이름 충돌 회피). CLI에서만 실행.
- 엑셀 룩업 대신 마찰선도 공식 직접 계산 채택 (Darcy-Weisbach + Altshul-Tsal). 경계 케이스에서 엑셀과 ±1단계 차이 가능.
