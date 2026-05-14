---
tags: [project, hvac, duct, ductsize, jarchitecture]
created: 2026-05-08
status: 구현 완료 (v1)
---

# DuctSize — 덕트 사이즈 결정 도구

## 한 줄 요약

엑셀 자료(`DUCT_MEASURE_1204.xls`, 오종열 2009)의 덕트 사이즈 결정 로직을 분석하여, **(L, Q, R, SF) 4개 입력으로 사각 덕트 a×b를 산출**하는 C# WinForm 도구로 구현했다.

## 출처

- 원본 엑셀: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\DuctSize\DUCT_MEASURE_1204.xls`
- 설명서: `DUCT_MEASURE_1204_설명서.txt` (작성자 오종열, REV.0911 / 2009.11.17)
- 근거 도서: 도서출판 한미「덕트 설계시공편」, 지문당「건축설비설계메뉴얼 - 공기조화설비설계」

## 산출물

| 산출물 | 위치 | 용도 |
|--------|------|------|
| [[DuctSizeDesign]] | 본 vault | 엑셀 결정 로직 분석 + 표준 시리즈 + 룩업 테이블 정의 |
| [[DuctDesignAPI]] | 본 vault | C# 함수 명세 (OptionA / OptionD) |
| [[DuctSizeApp-README]] | 본 vault | WinForm 앱 빌드/실행 가이드 |
| 코드 | `Acadv25JArch/DuctSize/DuctSizeApp/` | C# .NET 8 WinForm 솔루션 |
| 룩업 CSV | `Acadv25JArch/DuctSize/data/duct_de_lookup.csv` | 환산표 69×69 매트릭스 추출본 |

## 핵심 결정 사항

### 1. 5가지 결정 시나리오 (엑셀 원본)

엑셀 시트는 (Q, D, V, R, △p) 5개 변수 중 일부 입력 → 나머지 산출 + 사각덕트 변환.
- 시나리오 1: Q + R → 사각 (가장 일반적)
- 시나리오 2: D + V → Q
- 시나리오 3: Q + D 검토
- 시나리오 4: Q + V → 사각
- 시나리오 5: 기존 (a₀, b₀) → 새 (a₁, b₁) (등가 De 보존)

자세한 분석: [[DuctSizeDesign#3 5가지 결정 시나리오]]

### 2. 표준 사이즈 시리즈 (69개)

장변·단변은 **임의값이 아니라 표준 시리즈에서만 선택**:
- 50 ~ 1000 mm (50 mm 간격) - 20개
- 1050 ~ 1800 mm (50 mm 간격) - 16개
- 1900 ~ 2100 mm (혼재) - 4개
- 2200 ~ 5000 mm (100 mm 간격) - 29개

자세히: [[DuctSizeDesign#5 3 표준 사이즈 시리즈 장변·단변 가용 치수]]

### 3. 룩업 테이블 두 종류

| 시트 | 매핑 | 크기 | 특징 |
|------|------|------|------|
| 환산표 | (장변, 단변) → De | 69 × 69 | 대칭, Huebscher 식과 일치 |
| 정방형환산표 | De → 표준 장변 | 49 구간 | De 단조 매핑, 3498 mm까지만 수록 |

전체 매트릭스 데이터: `data/duct_de_lookup.csv` (33 KB)

### 4. (Q, R) → De 산출 = 공식 직접 계산 채택

엑셀 룩업 대신 **Darcy-Weisbach + Altshul-Tsal 공식** 사용:
- 장점: 코드 내장 데이터 불필요, 모든 입력 범위 커버
- 단점: 경계 케이스에서 엑셀과 ±1단계 차이 가능 (자료의 본질적 양자화)
- 검증 기준: De 차이 ±2% 이내 합격

자세히: [[DuctDesignAPI#5 5 Note 공식 기반 결정의 정합성]]

### 5. 자유도 1 → 두 함수로 분리

(Q, R, SF) → De는 임의 실수 1차원 결정이지만, 같은 De를 만족하는 (a, b) 표준 조합은 다수 존재 (자유도 1 남음). 이를 두 가지 방식으로 잠금:

| 함수 | 자유도 잠금 방식 | 출력 |
|------|------------------|------|
| `DuctDesign.OptionA` | 정사각 가정으로 a 추정 → ASP > 3이면 한 단계 ↑ | 단일 (a, b, De, V, R, ΔP) |
| `DuctDesign.OptionD` | De 오차 ≤ tol & ASP ≤ asp_max 통과 후보 모두 | List, ASP 작은 순 정렬 |

명세: [[DuctDesignAPI]]

## 작업 흐름 (시간순)

1. **자료 분석** — 엑셀 12개 시트 추출 (xlrd로 cp949 → UTF-8 변환), 설명서 해석
2. **로직 추출** — 5가지 시나리오 + 종합 결정 플로우 정리 → [[DuctSizeDesign]]
3. **데이터 표화**
   - 표준 사이즈 시리즈 69개 전체 표
   - De → 장변 매핑 49 구간 전체 표
   - 환산표 발췌(16×16) + 전체 CSV 추출
4. **자유도 토론** — ASP 임의값 vs 표준 유한집합 명확화
5. **API 설계** — [[DuctDesignAPI]] 작성 (옵션 A/D 두 함수)
6. **C# 구현**
   - 솔루션: `DuctSizeApp.sln`
   - 핵심: `DuctDesign.cs` (Tables, Math, Design, Result)
   - UI: WinForm `MainForm` (Designer 파일 분리, VS 디자이너 호환)
   - 검증: `Tests/SmokeTest.cs` 콘솔 러너
7. **사용자 피드백 반영**
   - 입력 의미 변경: ΔP [mmAq] → R [mmAq/m] (단위 길이당 손실율)
   - Form Designer 호환 구조 (partial class + .Designer.cs + .resx)

## 빌드 / 실행

```pwsh
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\DuctSize
dotnet build DuctSizeApp.sln -c Release
.\DuctSizeApp\bin\Release\net8.0-windows\DuctSizeApp.exe
```

또는 Visual Studio에서 `DuctSizeApp.sln` 더블클릭 → F5.

검증 케이스: (L=30, Q=13000 CMH, R=0.08 mmAq/m, SF=1.0)
- 옵션 A → `700 × 700` (정사각, ASP 1.00)
- 옵션 D → 22개 후보 (ASP 1.00 ~ 3.00)
- 엑셀 시나리오 1 결과 `1050 × 500`은 옵션 D 후보에 ASP 2.10 위치로 포함됨

## 알아둘 것 (구현·운용 시)

- 엑셀 옵션 A 결과(예 `1050 × 500`)는 사용자가 단변=500을 추가 입력한 케이스. 본 도구의 옵션 A는 한 변 지정 없이 자동으로 가장 정사각에 가까운 형상을 선택 → `700 × 700`. 형상을 강제하려면 옵션 D 표에서 직접 선택.
- 정방형환산표는 De ≤ 3498 mm까지만. 그 이상은 Huebscher 공식 직접 산출 (구현 시 자동 처리).
- §5.3 표준 시리즈에는 2050 mm가 있으나 정방형환산표 룩업에는 없음 → De 룩업 결과 장변은 §5.3의 부분집합.
- 여유율 SF는 면적 보정 (`A_design = A_calc × SF`). 1.0 미만이면 사이즈 축소 → R 재검증 필수.

## 관련 프로젝트

- [[../JArchitecture/INDEX|JArchitecture]] — AutoCAD .NET 플러그인. 향후 본 로직을 AutoCAD 명령으로 노출 가능 (현재는 별도 WinForm 도구).

## 참조

- 엑셀 시트별 상세: [[DuctSizeDesign#2 시트 구조 DUCT MEASURE]]
- 검증 기준값 (권장 풍속, 단위 마찰손실 추장치): [[DuctSizeDesign#5 검증 기준값 DUCT_DATA 시트 요약]]
- 마찰선도 공식 상수: [[DuctDesignAPI#5 2 마찰선도 공식 Darcy-Weisbach + Altshul-Tsal]]
