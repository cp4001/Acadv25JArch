# 백로그

`/devlog new` 로 착수하면 해당 행을 지운다.

| 날짜 | 내용 | 출처 |
|---|---|---|
| 2026-10-28 | (필요 시) `Insert_UpDn`/`_All` 교차 판정을 XY 투영(`IntersectWith(..., Plane, ...)`)으로 — Z 미세 오차 무시, 삽입 Z=0 | F-003 테스트 (현재는 `ZZERO` 로 대응) |
| 2026-10-29 | `Insert_Duct_Size` 재실행 시 기존 사이즈 Text 중복 검사/갱신 (Text XData `DuctSize` 로 식별) | F-004 |
| 2026-10-29 | (보류 — 현 상태 유지 결정) 라이선스 우회 XData 기록 경로 정리: `XdataCopy`(`jCadExtention.cs:242` — `Duct_C1/C1E/C2/C2E/EE` 분할선·`SCL`·`DuctOutLine` 버튼), `.XData =` 직접 대입(`Duct_E` 중심 Arc, `To_Duct/To_Pipe/To_Group`, `ArchOverrule`, `ChangeXdataName` 등). 막을 땐 `JXdata.SetXdata` 경로로 바꾸거나 명령 입구 `MyPlugin.IsLicenseValid`. `Duct_tag` 는 XData 없어 만료와 무관 | 대화 (보안 점검) |
