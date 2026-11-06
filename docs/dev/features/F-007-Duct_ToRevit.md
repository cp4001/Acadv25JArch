# F-007 AutoCAD → Revit 덕트 생성 (Duct_ToRevit)

| 항목 | 값 |
|---|---|
| 상태 | 진행중 |
| 규모 | M |
| 요청일 / 완료일 | 2026-11-06 / |
| 커밋 | `e7fd96a` · JRevit `c98857d` (240716New) |
| 문서 | `Acadv25JArch\PipeDiaCalc\DuctToRevit.md` · Confluence `10387458` |

## 요청 (원문 그대로)

> AutoCAD에서 Duct  작성 명령을 하는 거니까.  현재 OPen 된  Dwg에서 사용자가 명령어를 실행하면  Jrevit Duct Dwg  같은 Winform이 생기고 Dwg  선택대신  Duct  선택 버턴이 있어서 사용자가  Duct Line 을 선택하게 한다.  기타 Level    같은 정보는 현재 Revit에서 Open된 프로젝트에서 가져와서 선택 가능하게 한다.

> autocad 에서 생성된 Duct line은  DuctTree에서 2 Line으로 생성된 도면이도   덕트폭은 Xdata a 높이는 b에 있어.
> DuctLine 선택은 XData Tree 속성으로 필터링하면 될것 같어.

> AutoCad에 Group  명령을 아직 작성하지 않았지만  XData Group  이 생길 예정이야.

## 명확화

| 질문 | 답 |
|---|---|
| 대상 Revit | 2024 영문판 (JRevit 수신기) |
| 선택 대상 | LINE + XData `Tree`, 폭 `a`, 높이 `b`. 엘보용 ARC + XData `a` |
| 그룹 | XData `Group` 별 설정. 없으면 "미지정" 한 그룹 |
| 기존 Duct DWG 폼 | 건드리지 않음. 검증용 샘플 DWG 없이 진행 |

## 범위

- 포함: 사각 덕트 생성, Arc 위치 엘보, 그룹별 시스템·타입·레벨·높이·정렬
- 제외: 원형 덕트, 티·크로스, 댐퍼, 패밀리 배치, 코너 자동 엘보, 리본 버튼

## 계획

1. [x] JRevit `DuctBuilder` + `DuctBridge` (localhost HTTP 수신기) → 검증: Debug · Debug2024 빌드
2. [x] AutoCAD `Duct_ToRevit` 명령 + 폼 + 클라이언트 → 검증: Debug 빌드, 크기 파싱 체크
3. [ ] 실제 연동 → 검증: Revit 2024 + AutoCAD 2025 에서 선 선택 → 덕트·엘보 생성 (사용자 확인)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch\PipeDiaCalc\DuctToRevit.md` | 명령 문서 |
| `Acadv25JArch\PipeDiaCalc\DuctToRevitCommand.cs` | 명령, 선택 → 전송 데이터 변환 |
| `Acadv25JArch\PipeDiaCalc\DuctToRevitForm.cs` | 폼 (그룹별 설정) |
| `Acadv25JArch\PipeDiaCalc\DuctToRevitClient.cs` | DTO, HTTP 클라이언트, 크기 파싱 |
| JRevit `Duct\DuctBuilder.cs` | 폼 의존 없는 덕트·엘보 생성 |
| JRevit `Duct\DuctBridge.cs` | localhost:47891 수신기, ExternalEvent 전달 |
| JRevit `App.cs`, `JRevit.csproj` | 수신기 시작·종료, 파일 등록 |

## 검증 결과

- [x] 빌드: JRevit Debug · Debug2024, Acadv25JArch Debug (오류 0)
- [x] 크기 파싱 체크 8건 통과, 비관리자 권한 HttpListener 시작 확인
- [ ] 실제 실행 확인 (미실행)

## 후속 · 남은 일

- 실제 연동 시험은 설치본 재설치 후 진행한다. 설치된 번들 DLL(2026-10-29)에는 이 명령이 없고, NETLOAD 는 무효
- 시험 통과 후 JRevit `240716New` → `master` 병합, 이 저장소 `F-007` 브랜치 → `master` 병합
