---
tags:
  - AutoCAD
  - Revit
---

# Duct_ToRevit 명령어

> 파일: `PipeDiaCalc/DuctToRevitCommand.cs` · `DuctToRevitForm.cs` · `DuctToRevitClient.cs`
> 수신측: JRevit `JRevit/Duct/DuctBridge.cs` · `DuctBuilder.cs`
> 클래스: `PipeLoad2.DuctToRevitCommand.Cmd_DuctToRevit`
> 최종 업데이트: 2026-11-06 (신규, F-007)

> **상태: 빌드만 통과. 실제 Revit·AutoCAD 연동은 아직 시험하지 않았다.** 덕트·엘보 생성, 높이 파라미터, 폼 동작은 미확인.

---

## 1. 개요

DuctTree 도면의 덕트 중심선을 골라 **열려 있는 Revit 프로젝트에 사각 덕트와 엘보를 만든다.** 시스템·덕트 타입·레벨은 Revit 프로젝트에서 읽어 와 XData `Group` 별로 고른다. 도면은 수정하지 않는다.

| 항목 | 값 |
|---|---|
| AutoCAD | 2025, `Duct_ToRevit` 명령 |
| Revit | 2024 영문판, JRevit 설치 필요 |
| 통신 | `http://localhost:47891/` (같은 PC 안에서만) |
| 단위 | mm 도면 전제. XY 좌표를 그대로 Revit 좌표로 쓴다 |

---

## 2. 사용 순서

| 단계 | 동작 |
|---|---|
| ① | Revit 을 켜고 프로젝트를 연다 |
| ② | AutoCAD 에서 `Duct_ToRevit` 실행 → 폼이 프로젝트 이름과 목록을 불러온다 |
| ③ | `Duct 선택` → 덕트 Line 과 엘보 Arc 선택 |
| ④ | 그룹별로 시스템·덕트 타입·레벨·기준 높이·정렬 확인 |
| ⑤ | `Revit 에 생성` → 결과와 실패 항목이 폼 하단에 표시된다 |

Revit 에서 프로젝트를 바꿨으면 `다시 불러오기` 를 누른다. 생성에 성공하면 선택이 비워진다 (같은 선택을 다시 보내 덕트가 두 벌 생기는 것을 막는다).

---

## 3. 폼 항목

| 열 | 내용 | 기본값 |
|---|---|---|
| 그룹 | XData `Group`. 없으면 `미지정` | - |
| 선 수 | 그룹의 Line 수 | - |
| 시스템 | Revit 덕트 시스템 타입 | 프로젝트 첫 항목 |
| 덕트 타입 | Revit 사각 덕트 타입 | 프로젝트 첫 항목 |
| 레벨 | Revit 레벨 (높이순) | 가장 낮은 레벨 |
| 기준 높이(mm) | 정렬 기준 높이 | 2800 |
| 정렬 | Bottom · Middle · Top | Bottom |

**첫 항목이 맞는 값이라는 보장은 없다.** 생성 전에 확인할 것.

---

## 4. 선택 대상

| 대상 | 조건 | 쓰는 값 |
|---|---|---|
| Line | XData `Tree` + `a` + `b` | 시작·끝점, 폭 `a`, 높이 `b`, 그룹 `Group` |
| Arc | XData `Tree` 또는 `a` | 시작·끝점 (엘보 위치) |

- `a`, `b` 가 없거나 숫자로 시작하지 않는 Line 은 제외하고 폼에 개수를 표시한다 (부하 0 인 Line 등).
- `600x400` 처럼 적힌 값은 앞 숫자를 쓴다.
- `Tree` 가 없는 Line 은 `a` 가 있어도 무시한다.
- `Group` 은 `To_Group` 명령으로 넣는다.

---

## 5. Revit 생성 순서

전체가 Transaction 하나(`AutoCAD Ducts`)라 Revit Undo 한 번으로 취소된다.

| 단계 | 동작 |
|---|---|
| ① | 폼을 연 프로젝트와 활성 프로젝트가 같은지 확인. 다르면 중단 |
| ② | 레벨 높이에 덕트 생성, 폭·높이 지정. 공유 파라미터 `S07_Group`, `S09_D-Size` 가 있으면 기록 |
| ③ | Arc 양 끝에서 평면 거리 20mm 안의 열린 커넥터 2개를 찾아 엘보 생성 |
| ④ | 그룹의 정렬·기준 높이 적용 |

| 정렬 | Vertical Justification | 기준 높이를 넣는 파라미터 |
|---|---|---|
| Bottom | 1 | `Lower End Bottom Elevation` |
| Middle | 0 | `Middle Elevation` |
| Top | 2 | `Upper End Top Elevation` |

- 파라미터 이름은 **영문판 기준**이다. 다른 언어판에서는 높이 설정이 실패로 표시된다.
- 일부 덕트·엘보가 실패해도 나머지는 생성된다. 덕트가 하나도 안 만들어지면 전체를 되돌린다.

---

## 6. 통신

모든 요청에 `X-JRevit-Client` 헤더가 필요하다 (웹 페이지가 보내는 단순 요청 차단). 응답은 `{ ok, error, data }`.

| 요청 | 내용 |
|---|---|
| `GET /status` | 활성 문서 이름, Revit 버전 |
| `GET /project` | 레벨·시스템·사각 덕트 타입 목록 |
| `POST /ducts` | 덕트·엘보 생성 |

DTO 는 `DuctToRevitClient.cs` 와 JRevit `DuctBuilder.cs` 가 짝이다. 형식을 바꾸면 양쪽을 같이 고친다.

---

## 7. 주의

| 항목 | 내용 |
|---|---|
| 엘보 | Arc 가 있는 코너에만 생긴다. Arc 없는 직각 코너는 만들지 않는다 |
| 미지원 | 원형 덕트, 티·크로스, 댐퍼, 패밀리 배치 |
| Revit 사용 중 | 대화상자나 명령이 열려 있으면 10초 뒤 오류로 답하고 요청을 취소한다. 끝낸 뒤 다시 누른다 |
| Revit 두 개 | 먼저 연 쪽이 포트를 갖는다. 나중에 연 Revit 은 받지 않는다 |
| 라이선스 | JRevit 사용기간만 검사한다. AutoCAD 명령 자체에는 검사가 없다 |
| 설치 | NETLOAD 로는 시험할 수 없다. 번들이 먼저 로드되고 언로드되지 않으므로, 명령이 들어간 설치본을 재설치해야 한다 |
