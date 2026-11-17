# F-019 ss1 — 화면 밖 Base 처리 + Editor 선택 Transaction 밖으로

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-17 / 2026-11-17 |
| 커밋 | |

## 요청 (원문 그대로)

> 1번과 2번 수정해줘

(직전 `ss1` 분석의 1번: `SelectCrossingPolygon` 이 뷰포트 기준 → 화면 밖 Base 는 Target 0건, 2번: Editor 선택이 Transaction 안)

## 범위

- 포함: Target 후보를 `SelectAll` 로 Transaction 밖에서 1회 수집, Base 별 접촉 판정을 기하 계산으로 대체
- 제외: XData·레이어 조건(분석 3·4번), Arc/Poly 지원(9번), 분할·스냅·관통 제외 규칙

## 계획

1. (완료) `Util_Command.cs` `ss1` — `SelectAll(sfTarget)` 를 Transaction 전으로, `SelectCrossingPolygon` 폴리곤 → bbox 선필터 + `LinesTouch` → 검증: 빌드
2. (미확인) 화면 밖까지 이어진 Base·축소 화면에서 `ss1` → 검증: 화면 안과 같은 분할·스냅 결과

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/Util_Command.cs` | `ss1`: Base 선택 직후 `ed.SelectAll(sfTarget)` (Transaction 밖), Target 을 `(id, Line, Extents)` 목록으로 보관. Base 루프의 폴리곤·`SelectCrossingPolygon` 제거 → 같은 `OwnerId`·`ExtentsNear`(CSS 헬퍼 재사용) + 신규 `LinesTouch`(구간 교차 또는 끝점~상대 구간 거리 ≤ `SEARCH_WIDTH`). `TryExtents` 재사용 |
| `Util_Command/SS1.md` | v2.1 — §4.1 흐름, §8 이력 |

## 판정 차이 (구 폴리곤 대비)

- 구: Base 양옆 ±1.0 직사각형과 겹치는 선. 신: Base 선분에서 1.0 이내 (끝 쪽은 반원 모양으로 1.0 더 넓음)
- 넓어진 끝 영역 후보도 뒤 단계의 "교차점이 Base 구간 위" 조건을 그대로 통과해야 하므로 결과 차이는 Base 끝 1.0 바깥에서 비스듬히 닿는 극단 경우뿐

## 검증 결과

- [x] 빌드 (Release, 오류 0) · 설치파일 `JArchitecture_Setup_11_17.exe` 생성
- [ ] 테스트 / 실제 실행 확인 — 설치파일 실행까지 함, 실행 결과는 기록 안 됨 (사용자 done 요청)

## 후속 · 남은 일

- 분석 3번(Target XData 조건 — CSS 와 기준 다름)·4번(레이어 조건 없음)·9번(Arc/Poly) 미결정
- 매우 큰 도면: Base M × Target N bbox 비교 속도
