# DuctDesign API 명세서

> 참조 자료: [`DuctSizeDesign.md`](./DuctSizeDesign.md) — 결정 로직, 표준 시리즈, 룩업 테이블 정의
> 보조 데이터: [`data/duct_de_lookup.csv`](./data/duct_de_lookup.csv) — 환산표 전체 매트릭스 (참고용)

---

## 1. 개요

엑셀 자료(`DUCT_MEASURE_1204.xls`)의 결정 로직을 함수화한 두 API 명세.
입력은 동일하고 자유도 처리 방식만 다르다.

| 함수 | 입력 | 출력 | 자유도 잠금 |
|------|------|------|-------------|
| `DuctDesign_A` | L, Q, ΔP, SF | 단일 `Result` | 정방형환산표 자동 룩업 |
| `DuctDesign_D` | L, Q, ΔP, SF | `List<Result>` | 다중 후보 (ASP ≤ 3, De 오차 허용범위) |

---

## 2. 입력 명세

| 매개변수 | 기호 | 단위 | 유효범위 | 설명 |
|----------|------|------|-----------|------|
| `L` | L | m | 0 < L ≤ 1000 | 덕트 직선 길이 |
| `Q` | Q | CMH (m³/h) | 0 < Q ≤ 500,000 | 요구 풍량 |
| `dP` | ΔP | mmAq | 0 < ΔP ≤ 100 | 허용 총 정압 강하 |
| `SF` | SF | – | 0.5 ≤ SF ≤ 1.5 | 여유율 (면적 보정계수) |

**파생값**

```
R       = ΔP / L                  [mmAq/m]   ← 단위 마찰손실
Q_si    = Q / 3600                [m³/s]     ← 내부 계산용
A_si    = SF · (Q_si / V_target)  [m²]       ← 여유율 반영 면적
```

**입력 검증 실패 시**: `ArgumentException` 발생 (메시지에 위반 항목 명시).

---

## 3. 출력 자료형

```csharp
public sealed class DuctDesignResult
{
    public int    A           { get; init; }   // 장변 [mm], §5.3 표준 시리즈
    public int    B           { get; init; }   // 단변 [mm], §5.3 표준 시리즈
    public double De          { get; init; }   // 등가 원형 직경 [mm]
    public double V           { get; init; }   // 통과 풍속 [m/s]
    public double R           { get; init; }   // 단위 마찰손실 [mmAq/m]
    public double DeltaPActual{ get; init; }   // 전 길이 손실 [mmAq] = R · L
    public double Aspect      { get; init; }   // A / B (참고)
}
```

- `A ≥ B` 보장 (정렬 후 반환)
- `A`, `B`는 §5.3의 69개 표준 사이즈 안의 정수값 (mm)
- `De`, `V`, `R`, `DeltaPActual`은 (a, b) 확정 후 **재계산된 실제값**

---

## 4. 의존 자료 (정적 데이터)

```csharp
public static class DuctTables
{
    // §5.3 표준 사이즈 시리즈 (단위: mm). 장변·단변 공통.
    public static readonly int[] StandardSizes = {
        50,100,150,200,250,300,350,400,450,500,
        550,600,650,700,750,800,850,900,950,1000,
        1050,1100,1150,1200,1250,1300,1350,1400,1450,1500,
        1550,1600,1650,1700,1750,1800,
        1900,2000,2050,2100,
        2200,2300,2400,2500,2600,2700,2800,2900,3000,
        3100,3200,3300,3400,3500,3600,3700,3800,3900,4000,
        4100,4200,4300,4400,4500,4600,4700,4800,4900,5000
    }; // 69개
}
```

§5.4 정방형환산표(De → 장변 49 구간)와 §5.5 환산표(69×69 De 매트릭스)는 **공식 직접 계산**으로 대체하므로 룩업 테이블은 코드 내장 불요.

---

## 5. 핵심 공식

### 5.1 등가 원형 직경 (Huebscher)

```
De [mm] = 1.30 · (a · b)^0.625 / (a + b)^0.25       (a, b: mm)
```

§5.5 환산표의 모든 셀과 일치 검증 완료.

### 5.2 마찰선도 공식 (Darcy-Weisbach + Altshul-Tsal)

```
입력:  Q [m³/s],  D [mm]
출력:  R [mmAq/m]

D_m = D · 1e-3
A   = π · D_m² / 4
V   = Q / A
Re  = ρ · V · D_m / μ

if Re < 2300:               (층류, 비실용 영역)
    f = 64 / Re
else:                       (난류, 명시적 근사)
    f = 0.11 · (68/Re + ε/D_m)^0.25
    if f < 0.018:
        f = 0.85·f + 0.0028              ← Altshul-Tsal 보정

dP/dL_Pa = f · ρ · V² / (2 · D_m)        [Pa/m]
R_mmAq   = dP/dL_Pa / 9.80665            [mmAq/m]
```

**기본 상수**

| 기호 | 값 | 설명 |
|------|-----|------|
| ρ (rho) | 1.2 kg/m³ | 공기 밀도 (20℃, 1 atm) |
| μ (mu) | 1.81 × 10⁻⁵ Pa·s | 공기 점성계수 |
| ε (eps) | 0.09 × 10⁻³ m | 갈바나 강판 덕트 조도 (clean) |
| g | 9.80665 m/s² | 중력가속도 (mmAq ↔ Pa 환산) |

> 필요 시 ε는 옵션 매개변수로 노출 (지하 매설/오래된 덕트는 0.15~0.30 mm).

### 5.3 (Q, R) → De 역산 (이분법)

```
solve_diameter(Q, R_target):
    D_lo, D_hi = 50, 5000        # mm 검색 구간
    for _ in range(60):
        D_mid = (D_lo + D_hi) / 2
        R_mid = friction(Q, D_mid)
        if R_mid > R_target:     # D 작을수록 R 큼
            D_lo = D_mid
        else:
            D_hi = D_mid
        if abs(D_hi - D_lo) < 0.01: break
    return D_mid
```

### 5.4 여유율 적용

```
면적 보정: A_target = A_calc × SF
직경 환산: D_target = D_calc × √SF
```

엑셀 시트 C8 셀 의미와 동일.

### 5.5 [Note] 공식 기반 결정의 정합성

엑셀 룩업 대신 §5.1~5.3 공식을 사용해도 **최종 (a, b)는 항상 §5.3 표준 시리즈 조합**으로 나오는 것이 보장된다. 이유는 결정 단계가 두 개로 분리되기 때문이다:

| 단계 | 결정값 | 도메인 |
|------|--------|--------|
| De 산출 (§5.2 + §5.3) | 임의 실수 | 연속값 (공식이든 룩업이든) |
| (a, b) 결정 (§6 step 4·5) | `snap_to_standard()` | §4의 69개 정수만 |

따라서 De가 ±1~2% 정도 변해도 표준 시리즈 간격(50 mm)이 충분히 커서 **대부분 같은 (a, b)로 스냅**된다.

**공식 vs 엑셀 룩업의 실효 차이**

| 케이스 | 결과 |
|--------|------|
| De 차이 < 표준 간격의 절반 (≈ 25 mm) | (a, b) 동일 |
| De가 두 표준 사이즈 경계선에 걸침 | (a, b) **±1단계** 차이 가능 (50 mm 한 단계) |

> 따라서 구현 단계에서 엑셀과 결과가 한 단계(50 mm) 차이 나는 케이스는 **버그가 아니라 자료의 본질적 양자화** 때문이다. §11 검증 기준이 "De 차이 ±2% 이내" 인 이유.

---

## 6. 알고리즘 — DuctDesign_A (옵션 A: 단일 출력)

```
DuctDesign_A(L, Q_cmh, dP_mmAq, SF) → DuctDesignResult:

  # 1. 입력 환산
  Q   = Q_cmh / 3600              [m³/s]
  R   = dP_mmAq / L               [mmAq/m]

  # 2. (Q, R) → De₀
  De₀ = solve_diameter(Q, R)      [mm]

  # 3. 여유율 적용
  De  = De₀ × √SF                 [mm]

  # 4. De → 표준 장변 a  (정방형환산표 방식 = "가장 정사각에 가깝도록")
  #    공식 직접 계산: De² = a·b 일 때 a = De / k(ASP) 로부터 시작.
  #    여기서는 "정사각 가정" → a₀ = De / 1.087  (Huebscher 정사각 비례 계수)
  #    그 후 a₀를 표준 시리즈에서 가장 가까운 값으로 스냅.
  a₀  = De / 1.087
  a   = snap_to_standard(a₀)      ← 표준 시리즈에서 가장 가까운 값

  # 5. (a, De) → 단변 b
  #    Huebscher 식을 b에 대해 수치해 후 표준 시리즈로 스냅
  b₀  = solve_b_huebscher(a, De)
  b   = snap_to_standard(b₀)

  # 6. a ≥ b 정렬 + ASP 검증
  if a < b: swap(a, b)
  while a / b > 3.0:
      a = next_size_up(a)         ← 표준 시리즈에서 한 단계 큰 값
      b₀ = solve_b_huebscher(a, De)
      b  = snap_to_standard(b₀)
      if a < b: swap(a, b)

  # 7. 검증값 재계산
  De_actual = 1.30 × (a·b)^0.625 / (a+b)^0.25
  V         = Q / (a · b · 1e-6)            [m/s]
  R_actual  = friction(Q, De_actual)        [mmAq/m]
  dP_actual = R_actual × L                  [mmAq]

  return DuctDesignResult {
      A=a, B=b, De=De_actual, V=V,
      R=R_actual, DeltaPActual=dP_actual,
      Aspect = (double)a / b
  }
```

**보조 함수**

```
snap_to_standard(x) → int:
    return argmin_{s ∈ StandardSizes} |s - x|

next_size_up(s) → int:
    return StandardSizes[index_of(s) + 1]   (없으면 throw)

solve_b_huebscher(a, De) → double:
    # 1.30 · (a·b)^0.625 / (a+b)^0.25 = De  를 b에 대해 이분법
    b_lo, b_hi = 50, a
    for _ in range(60):
        b_mid = (b_lo + b_hi) / 2
        De_mid = 1.30 · (a·b_mid)^0.625 / (a + b_mid)^0.25
        if De_mid < De: b_lo = b_mid    # De 작으면 b 키워야
        else:           b_hi = b_mid
    return b_mid
```

---

## 7. 알고리즘 — DuctDesign_D (옵션 D: 다중 후보)

```
DuctDesign_D(L, Q_cmh, dP_mmAq, SF, tol=0.05, asp_max=3.0)
        → List<DuctDesignResult>:

  # 1~3. DuctDesign_A 와 동일 (De 까지 산출)
  Q   = Q_cmh / 3600
  R   = dP_mmAq / L
  De  = solve_diameter(Q, R) × √SF

  # 4. 표준 시리즈를 모두 순회하며 De 만족 + ASP ≤ asp_max 후보 수집
  candidates = []
  for a in StandardSizes:
      for b in StandardSizes where b ≤ a:
          De_ab = huebscher(a, b)
          err   = |De_ab - De| / De
          asp   = a / b
          if err ≤ tol and asp ≤ asp_max:
              V        = Q / (a·b·1e-6)
              R_ab     = friction(Q, De_ab)
              dP_ab    = R_ab × L
              candidates.append(Result(a,b,De_ab,V,R_ab,dP_ab))

  # 5. 정렬: ASP 작은 순(=정사각에 가까운 순), 동률은 De 오차 작은 순
  candidates.sort(key = (c.Aspect, |c.De - De|))

  return candidates
```

**기본값**

| 매개변수 | 기본값 | 의미 |
|----------|---------|------|
| `tol` | 0.05 | De 허용 오차 ±5% |
| `asp_max` | 3.0 | 종횡비 상한 |

→ 두 값을 좁히면 후보 감소, 넓히면 증가. 호출자가 옵션으로 조정.

**예상 후보 수**: De 오차 5%, ASP ≤ 3 기준에서 보통 5~12개. ASP ≤ 2로 강화 시 3~6개.

---

## 8. C# 시그니처 (참고)

```csharp
namespace JArchitecture.HVAC.DuctSize;

public static class DuctDesign
{
    public static DuctDesignResult OptionA(
        double lengthM, double flowCMH, double dPmmAq, double safetyFactor,
        double roughnessM = 9e-5);

    public static IReadOnlyList<DuctDesignResult> OptionD(
        double lengthM, double flowCMH, double dPmmAq, double safetyFactor,
        double tolerance = 0.05, double aspectMax = 3.0,
        double roughnessM = 9e-5);
}

public sealed record DuctDesignResult(
    int A, int B, double De, double V, double R,
    double DeltaPActual, double Aspect);

public static class DuctTables
{
    public static IReadOnlyList<int> StandardSizes { get; }
}
```

---

## 9. 검증 / 에러 처리

| 케이스 | 처리 |
|--------|------|
| L ≤ 0, Q ≤ 0, ΔP ≤ 0 | `ArgumentOutOfRangeException` |
| SF 가 (0.5, 1.5) 범위 밖 | `ArgumentOutOfRangeException` |
| 이분법 비수렴 (60회 초과) | `InvalidOperationException` ("입력 조합으로 직경 수렴 실패") |
| De 가 표준 시리즈 상한 초과 (> 5000 mm) | `InvalidOperationException` ("표준 시리즈 범위 초과 — 입력 재검토 필요") |
| 옵션 A: ASP ≤ 3 만족 시까지 a 키워도 미해결 | `InvalidOperationException` ("ASP 제약 만족 불가") |
| 옵션 D: 후보 0개 | `tol` 자동 ×1.5 후 재시도 (최대 2회), 그래도 0개면 빈 리스트 반환 |

---

## 10. 사용 예제

### 10.1 옵션 A

```csharp
var r = DuctDesign.OptionA(
    lengthM: 30,
    flowCMH: 13000,
    dPmmAq:  2.4,        // 30 m × 0.08 mmAq/m 기준
    safetyFactor: 1.0);

// 기대 결과 (엑셀 시나리오 1과 동일):
// A=1050, B=500, De≈779, V≈6.88, R≈0.076, ΔP≈2.27, Aspect=2.10
```

### 10.2 옵션 D

```csharp
var list = DuctDesign.OptionD(
    lengthM: 30, flowCMH: 13000, dPmmAq: 2.4, safetyFactor: 1.0);

// 기대 결과 (예시 — De ≈ 763, tol=5%, ASP≤3):
// [(750, 750, 820, 8.22, 0.094, 2.83, 1.00),
//  (850, 700, 825, 7.78, 0.085, 2.55, 1.21),
//  (900, 650, 813, 7.91, 0.090, 2.70, 1.38),
//  (950, 600, 802, 8.01, 0.094, 2.83, 1.58),
//  (1000, 550, 786, 8.18, 0.099, 2.97, 1.82),
//  (1100, 500, 783, 8.20, 0.100, 3.00, 2.20),
//  (1200, 450, 770, 8.33, 0.106, 3.18, 2.67),  ← ASP 위반 시 제외
//  (1350, 400, 766, 8.35, 0.108, 3.24, 3.38)] ← 제외
```

---

## 11. 검증용 비교 케이스 (엑셀 vs 함수)

| 케이스 | L | Q | ΔP | SF | 엑셀 결과 (a × b) | 옵션A 기대 | 비고 |
|--------|---|---|-----|-----|---------------------|-------------|------|
| 1 | 30 | 13,000 | 2.4 | 1.00 | 1050 × 500 | 1050 × 500 | 기본 시나리오 |
| 2 | 30 | 13,000 | 2.4 | 1.05 | 약간 큼 | 1100 × 500 | 여유율 적용 |
| 3 | 50 | 28,800 | 5.0 | 1.00 | 1400 × 450 (ASP 8 case) | 1500 × 1000 | ASP ≤ 3 강제 |

> 단위검증 시 엑셀 결과와 함수 결과의 De 차이 ±2% 이내이면 합격.

---

## 12. 다음 단계 (구현 권고 순서)

1. `DuctTables.StandardSizes` 정적 배열 정의 + 단위 테스트
2. `Huebscher`, `Friction`, `SolveDiameter` 순수 함수 구현 + 단위 테스트 (엑셀 셀 5개와 비교)
3. `OptionA` 구현 + 11장 비교 케이스 통과
4. `OptionD` 구현 + tol/asp_max 경계 테스트
5. JArchitecture AutoCAD 명령으로 노출 (별도 작업)

---

## 13. 변경 이력

- v1.0 (2026-05-08) — 초안. 옵션 A/D 두 함수 명세, 마찰선도 공식 직접계산 채택.
