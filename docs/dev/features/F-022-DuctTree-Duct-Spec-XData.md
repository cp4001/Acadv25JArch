# F-022 DUCTTREE Apply — XData `Duct`·`Spec` 추가

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-23 / 2026-11-24 |
| 커밋 | |

## 요청 (원문 그대로)

> Ducttree outline 작성후 Xdata 저장 기록을 조사해줘

> (Duct Line XData 덤프: Tree=Mid · Total_CMH=10056 · a=800 · b=550 · Disp=800x550[10056] · JLicense=JJH)
> 이내용이 Duct Line에 기록된 Xdata 인데
> (Duct=D · Spec=500*400)
> 를 추가해서 작성하게  해줘

## 범위

- 포함: `DuctTreeBuilder.ApplyRecursive` 에서 `a`/`b` 를 쓸 때 `Duct`="D", `Spec`="{a}*{b}" 함께 기록 (`Load > 0` 이고 Mode D 결과가 있을 때만 — `a`/`b` 와 같은 조건)
- 형식 근거: BricsCAD `AutoCADFunction_22_6_28\Duct\DuctSolution.cs:7976` (`Duct`="D", `Spec`=`$"{Spec1}*{Spec2}"`)
- Outline 분할 조각은 `XdataCopy` 로 전부 승계되므로 별도 작업 없음
- 포함 (추가 요청 "outline Dcut에서 생성된 Arc에도 동일한 정보를 기록하게 수정해줘", 2026-11-24): Duct_E 엘보 중심선 Arc — `a` 만 복사 → `XdataCopy` 전체 승계. 노란 외곽 Arc(`Duct_OutLine`)는 XData 없음 그대로
- 포함 (추가 요청 "Duct_Edit 도 Spec 같이 고쳐줘"): `Duct_Edit`(F-020) a·b 수정 시 `Duct`="D"·`Spec`="{a}*{b}" 함께 기록

## 계획

1. (완료) `ApplyTotalCmh` RegApp `Duct`·`Spec` 등록, `ApplyRecursive` 기록 추가 → 검증: Release 빌드 오류 0
2. (완료) `Duct_Edit` 기록 단계에 `Duct`·`Spec` 추가 → 검증: Release 빌드 오류 0
3. (설치파일 `JArchitecture_Setup_11_24.exe` 생성·설치 프로그램 실행 — 실행 결과 기록 없음) 설치파일로 `DUCTTREE` Apply 후 Line XData 덤프 → 검증: `Duct`=D · `Spec`={a}*{b}

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/DuctTreeBuilder.cs` | `ApplyTotalCmh`: `CheckRegName("Duct")`·`("Spec")` · `ApplyRecursive`: `Disp` 다음에 `Duct`·`Spec` 기록 |
| `Acadv25JArch/PipeDiaCalc/DuctTreeCommand.cs` | `Duct_Edit`: RegApp `Duct`·`Spec` 등록, a·b 다음에 `Duct`="D"·`Spec`="{a}*{b}" 기록 |
| `Acadv25JArch/PipeDiaCalc/DuctElbowCommand.cs` | `AddCenterArc`: `GetXDataForApplication("a")` → `source.XdataCopy(arc)` |

## 검증 결과

- [x] 빌드 (Release, 오류 0)
- [x] 설치파일 `JArchitecture_Setup_11_24.exe` 생성 · 설치 프로그램 실행
- [ ] AutoCAD 실행 확인 — 사용자 결과 보고 없이 마감 (Apply·`Duct_Edit`·엘보 Arc XData 미확인)

## 후속 · 남은 일

- AutoCAD 에서 XData 확인 (위 미확인 항목)
