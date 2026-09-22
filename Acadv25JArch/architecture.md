# JArchitecture - AutoCAD 플러그인 명령어 정리

## Overview
AutoCAD 2025용 건축 설계 자동화 플러그인 (C# .NET)
라이선스: `JArchLicense.dll` 기반 만료일 체크

---

## 1. 건축 벽/라인 관련

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `Wall_Line_Arrange` | LineGrouping.cs | 벽 라인 정리 (기울기별 그룹핑) |
| `Wall_Line_delete` | LineGrouping.cs | 벽 라인 제거 |
| `Wall_Cen_LINES2POLY` | JPolyLine.cs | 벽 센터라인 → 폐합 폴리라인 |
| `Net_Dim` | JPolyLine.cs | 라인 선택 → 안목 폴리라인 |
| `c_visibleline_to_Poly` | JPolyLine.cs | 보이는 라인 → 폴리라인 변환 |
| `mmdl` | LineGrouping.cs | 기울기별 라인 그룹핑 + 중심선 생성 |
| `mdl` | LineGrouping.cs | 기울기별 라인 그룹핑 (800mm) + 중심선 |
| `Group_Lines` | LineGrouping.cs | 라인 그룹핑 |
| `Group_Lines11` | LineGrouping.cs | 라인 그룹핑 (변형) |
| `GROUPLINES` | LineGrouping.cs | 라인 그룹핑 |
| `GROUPLINES_CUSTOM` | LineGrouping.cs | 커스텀 라인 그룹핑 |
| `GROUPLINES_STATS` | LineGrouping.cs | 라인 그룹 통계 |
| `CreateMiddleLine` | LineGrouping.cs | 중심선 생성 |
| `TEST_COLINEAR` | LineGrouping.cs | 동일선상 테스트 |
| `line_proj` | LineGrouping.cs | 라인 투영 |
| `Highlight_Projectable_Lines` | LineGrouping.cs | 투영 가능 라인 하이라이트 |
| `CHKLINEPROJ` | LineGrouping.cs | 라인 투영 체크 |
| `Show_SelectionBox` | LineGrouping.cs | 선택 박스 표시 |
| `RestoreLineColors` | LineGrouping.cs | 라인 색상 복원 |
| `c_SPLITLINES` | LineSplitter.cs | 라인 분할 |
| `FindIntersections` | LineSplitter.cs | 교차점 찾기 |
| `ExtendToIntersections` | LineSplitter.cs | 교차점까지 연장 |
| `LineExtend2Block` | PipeDiaCalc/LineTreeBuilder.cs | Line+Block 선택, Line을 가장 가까운 Block BBox 경계까지 연장 |

---

## 2. 실(Room) / 면적 계산

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `To_RoomPoly` | RoomCalc.cs | 선택 폴리 → 룸 폴리 지정 |
| `Room_Poly_Calc` | RoomCalc.cs | 선택 룸 폴리 면적 계산 |
| `Room_Poly_ALL_Calc` | RoomCalc.cs | 전체 룸 폴리 면적 계산 |
| `Room_text_delete` | RoomCalc.cs | 룸 텍스트 제거 |
| `To_RoomText` | RoomCalc.cs | 선택 TEXT → 룸 텍스트 지정 (XData `Arch=RoomText` / `RoomText` / `Disp=__`). 2026-09-19 RegApp 오타 `Archi`→`Arch` 수정 — `ArchOverrule`·`Room_text_delete` 필터와 동일 규약 |
| `To_RoomCFM` | RoomCalc.cs | 선택 **닫힌** 폴리에 풍량 XData `"CFM"` 기록 (`PromptDoubleOptions` 입력값을 문자열로, `To_CeilingHeight` 패턴). `TTG` 가 폴리 중앙에 Cyan `"{값} CFM"` 으로 표시 (2026-09-19 신규) |
| `To_CeilingHeight` | RoomCalc.cs | 천장고 지정 |
| `To_FloorHeight` | RoomCalc.cs | 바닥높이 지정 |
| `To_Floor` | RoomCalc.cs | 바닥 지정 |
| `HH` | RoomCalc.cs | 높이 지정 |
| `OutWall` | RoomCalc.cs | 외벽 지정 |
| `room_Dir_Cus` | RoomCalc.cs | 룸 방위 커스텀 분석 |
| `ANALYZE_DIRECTION_DETAIL` | RoomCalc.cs | 방위 상세 분석 |
| `Set_North_Vector` | RoomCalc.cs | 북쪽 벡터 설정 |
| `CONVEXHULL` | RoomCalc.cs | Convex Hull 생성 |
| `a_Work` | AreaCalc.cs | 면적 작업 |
| `aWork` | AreaCalc.cs | 면적 레이아웃 작업 |
| `a_Work_Disp` | AreaCalc.cs | 면적 작업 결과 표시 |
| `ceil_height` | ArchZone.cs | 닫힌 폴리 내부 객체 선택 |

---

## 3. 라인 방향 분석

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `LineDir` | RoomCalc.cs | 선택 라인 방위각 분석 |
| `LineDir1` | RoomCalc.cs | 라인 방위각 분석 (변형) |
| `PolyDir` | RoomCalc.cs | 폴리라인 방향 분석 |
| `ANGLE2LINE` | Commands.cs | 두 선 사이의 내각 계산 |

---

## 4. 블럭(Block) 관련

### 블럭 지정/변환

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `To_Window` | RoomCalc.cs | 선택 블럭 → 창 지정 |
| `To_Door` | RoomCalc.cs | 선택 블럭 → 문 지정 |
| `To_Column` | RoomCalc.cs | 선택 블럭 → 기둥 지정 |
| `To_Symbol` | RoomCalc.cs | 선택 개체 → 구분 심볼 지정 |
| `ShowBlockForm` | Commands.cs | 블럭 파트 폼(UI) 표시 |
| `Block_Entity_layer` | BlockClass.cs | 블럭 내부 Entity 레이어 변경 |
| `Block_Entity_ALL_color` | BlockClass.cs | 전체 블럭 → Layer 0, Color ByBlock |
| `Insert_Block` | BlockClass.cs | 블럭 삽입 (현재 UCS) |
| `K2` | BlockClass.cs | 블럭 교체 |

### 블럭 조회/카운트

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `BB_Count` | BlockClass.cs | 블럭 테이블 카운트 |
| `BB_Count1` | BlockClass.cs | 선택 블럭 카운트 |
| `LISTATT` | BlockClass.cs | 블럭 속성(Attribute) 리스트 |
| `BLOCK_INFO` | BlockClass.cs | 블럭 정보 조회 |
| `CBT` | BlockClass.cs | 블럭 처리 |

### 블럭 Explode

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `BB_Explode` | BlockClass.cs | 블럭 테이블에서 Explode |
| `EXPLODE_BLOCK_ACTUAL` | BlockClass.cs | 블럭 실제 Explode |
| `EXPLODE_ALL_BLOCKS` | BlockClass.cs | 전체 블럭 Explode |
| `EXPLODE_Sel_BLOCKS` | BlockClass.cs | 선택 블럭 Explode |

### 블럭 Entity 복사

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `COPY_BLOCK_ENTITY` | BlockClass.cs | 블럭 내 가까운 Entity 복사 |
| `COPY_BLOCK_ENTITY_CUSTOM` | BlockClass.cs | 블럭 내 Entity 복사 (커스텀 반경) |
| `FIND_NESTE_DCURVES` | BlockClass.cs | 중첩 블럭 내 Curve 찾기 |
| `CLEAR_HIGHLIGHT` | BlockClass.cs | 하이라이트 제거 |
| `CREATE_XCLIP_POLYLINES` | BlockClass.cs | XClip 폴리라인 생성 |

### 블럭 이미지 추출

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `EXTRACT_BLOCK_ICON` | BlockClass.cs | 선택 블럭 아이콘 추출 |
| `ANALYZE_BLOCK_ICON` | BlockClass.cs | 선택 블럭 아이콘 분석 |
| `EXTRACT_ALL_BLOCKICONS` | BlockClass.cs | 전체 블럭 아이콘 추출 |
| `ANALYZE_PREVIEW_ICONS` | BlockClass.cs | 미리보기 아이콘 분석 |
| `EXTRACT_ONLY_PREVIEW_ICONS` | BlockClass.cs | 미리보기 아이콘만 추출 |
| `SAVE_BLOCK_IMAGE` | BlockClass.cs | 블럭 이미지 저장 |
| `SAVE_ALL_BLOCKS_IMAGE` | BlockClass.cs | 전체 블럭 이미지 저장 |
| `SAVE_BLOCK_IMAGE_WITHSIZE` | BlockClass.cs | 블럭 이미지 크기 지정 저장 |

### 블럭 Boundary

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `CREATEBLOCKBOUNDARY` | BlockBoundary.cs | 블럭 바운더리 생성 |
| `CREATEBLOCKBOUNDARYADVANCED` | BlockBoundary.cs | 블럭 바운더리 생성 (고급) |
| `CREATEBLOCKBOUNDARYEXACT` | BlockBoundary.cs | 블럭 바운더리 정밀 생성 |
| `GETBLOCKBOUNDARY` | BlockBoundary.cs | 블럭 바운더리 조회 |

---

## 5. 경계 추적 / 폴리라인

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `TB` | BoundayClosedToPoly.cs | 경계 추적 → 폴리라인 변환 |
| `TB1` | BoundayClosedToPoly.cs | 룸 경계 추적 |
| `TB_rr` | BoundayClosedToPoly.cs | 룸 텍스트 기반 경계 추적 |
| `CloneToAreaCalc` | BoundayClosedToPoly.cs | 면적 계산용 복제 |
| `CloneHelp` | BoundayClosedToPoly.cs | 복제 도움말 |
| `CloneStatus` | BoundayClosedToPoly.cs | 복제 상태 확인 |
| `TraceInnerBoundary` | InnerBoundaryTracer.cs | 내부 경계 추적 |
| `CLEARTRACED` | InnerBoundaryTracer.cs | 추적 결과 제거 |
| `FindMinimumAngleLine` | ClockwiseLineSelector.cs | 최소 각도 라인 찾기 |
| `POLYDIFF` | PolyProcess.cs | 폴리라인 차이 연산 |
| `REGIONTOPOLY` | PolyProcess.cs | Region → 폴리라인 변환 |
| `POLYSUBTRACT` | PolylineBoolean.cs | 폴리라인 Boolean Subtract |
| `POLYSUBTRACT_TEST` | PolylineBoolean.cs | 폴리라인 빼기 테스트 |
| `c_TRUECONCAVEHULL` | TrueConcaveHull.cs | True Concave Hull 생성 |

---

## 6. 교차점 / 룸 찾기

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `Find_intersect` | RoomFind.cs | 교차점 찾기 |
| `Find_intersect_Multi` | RoomFind.cs | 다중 교차점 찾기 |
| `INTERSECTION_STATS` | RoomFind.cs | 교차점 통계 |
| `SHOW_SEARCH_AREA` | RoomFind.cs | 검색 영역 표시 |
| `Line_Block_intersect` | RoomFind.cs | 라인-블럭 교차점 |
| `Check_line_intersect` | RoomFind.cs | 라인 교차 체크 |
| `Check_Geo_intersect` | RoomFind.cs | Geometry 교차 체크 |
| `c_FILTERVISIBLELINES` | RoomFind.cs | 가시 라인 필터 (XClip/뷰포트) |
| `FILTERVISIBLELINES_STATS` | RoomFind.cs | 가시 라인 필터 통계 |

---

## 7. 레이어(Layer) 관련

### 기본 레이어 제어 (LayerControl.cs)

| 명령어 | 설명 |
|--------|------|
| `LAYER_OFF` | 선택 레이어 끄기 |
| `LAYER_ON` | 선택 레이어 켜기 |
| `LAYER_ALL_ON` | 전체 레이어 켜기 |
| `Layer_SetLayer_Current` | 선택 객체의 레이어를 현재로 설정 |
| `SETLAYERCURRENT` | 레이어를 현재로 설정 |
| `CURRENT_LAYER_info` | 현재 레이어 정보 표시 |
| `ISOLATELAYER` | 현재 레이어만 격리 |
| `LyIso` | Entity 레이어 격리 |
| `SELECTLAYER` | 레이어 선택 |
| `LAYER_MANAGER` | 레이어 매니저 팔레트 (LayerPalette.cs) |

### 레이어 잠금 (LayerControl.cs)

| 명령어 | 설명 |
|--------|------|
| `UNLOCK_Layer_All` | 전체 레이어 잠금 해제 |
| `LOCK_ALL_Layer` | 전체 레이어 잠금 |
| `LIST_LOCKED_LAYERS` | 잠긴 레이어 목록 |

### 레이어 상태 저장/복원 (LayerControl.cs)

| 명령어 | 설명 |
|--------|------|
| `La_Save` | 레이어 상태 저장 |
| `La_Restore` | 레이어 상태 복원 |
| `La_List` | 저장된 레이어 상태 목록 |
| `La_Delete` | 저장된 레이어 상태 삭제 |

### 레이어 카운트/테스트 (LayerControl.cs)

| 명령어 | 설명 |
|--------|------|
| `ll_Count` | 레이어별 객체 수 카운트 |
| `TEST_LAYER` | 레이어 테스트 |
| `TEST_LAYER_ADVANCED` | 레이어 고급 테스트 |

### JLayer 단축 명령어 (JLayer.cs)

| 명령어 | 설명 |
|--------|------|
| `LMA` | 레이어 매치 (선택 객체 → 대상 레이어) |
| `SCL` | 선택 객체 레이어 → 현재 레이어로 변경 |
| `CCL` | 선택 객체 Color → 현재 레이어 Color |
| `CTL` | 선택 객체 Linetype → 현재 레이어 것으로 |
| `MEO` | 같은 레이어 객체 선택 |
| `MEL` | 동일 레이어 객체 선택 |
| `CEL` | 현재 레이어 객체 선택 |
| `LOF` | 선택 객체 레이어 끄기 |
| `LON` | 선택 객체 레이어 켜기 |
| `LOL` | 선택 객체 레이어 잠금 |
| `LBR` | 레이어 잠금 해제 |
| `FLO` | 동결(Freeze) 레이어 |
| `FOO` | 동결 해제 |
| `OLO` | 선택 객체 레이어만 켜기 (나머지 끔) |
| `LFR` | 레이어 프리즈 |
| `FRE` | 프리즈 해제 |
| `LLO` | 레이어 잠금 |
| `ALO` | 전체 레이어 잠금 |
| `LOE` | 레이어 켜기 (확장) |
| `LUL` | 레이어 잠금 해제 |
| `ULA` | 전체 레이어 잠금 해제 |
| `LPF` | 레이어 Plot Off |
| `LPO` | 레이어 Plot On |
| `XLOF` | Xref 레이어 끄기 |
| `XLON` | Xref 레이어 켜기 |
| `XFC` | Xref 레이어 동결 |
| `LWLK` | 레이어 Walk |
| `MLAY` | 레이어 이동(Merge) |
| `LALI` | 레이어 목록 정보 |
| `WELN` | 선택 레이어 이름 필터링 |
| `DLF` | 레이어 필터 삭제 |
| `DFO` | 레이어 필터 삭제 (전체) |
| `LC` | 레이어 색상 변경 |
| `MLTF` | 멀티 레이어 필터 |
| `MDLA` | 레이어 삭제 |
| `LME` | 레이어 병합(Merge) |
| `LP` | 레이어 속성 |
| `MOLP` | 선택 객체 레이어 속성 |
| `MLS` | 레이어 설정 |
| `LLP` | 레이어 목록 출력 |
| `REL` | 레이어 리네임 |
| `CLN` | 레이어 클론(복사) |
| `EEL` | 빈 레이어 삭제 |
| `RBC` | ByBlock 색상 복원 |
| `MABL` | 블럭 레이어 매치 |
| `DRLA` | 레이어 드래그 |
| `OTL` | 기타 레이어 처리 |

---

## 8. XData / Overrule

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `aag` | Overrule/ArchOverrule.cs | Wire Graphic 표시 |
| `aag3` | Overrule/ArchOverrule.cs | Wire Graphic 표시 (v3) |
| `REGISTERXDATAFILTER` | Overrule/ArchOverrule.cs | XData 필터 등록 |
| `UNREGISTERXDATAFILTER` | Overrule/ArchOverrule.cs | XData 필터 해제 |
| `ADDARCHXDATA` | Overrule/ArchOverrule.cs | 건축 XData 추가 |
| `ADDARCHXDATABATCH` | Overrule/ArchOverrule.cs | 건축 XData 일괄 추가 |
| `REMOVEARCHXDATA` | Overrule/ArchOverrule.cs | 건축 XData 제거 |
| `TESTXDATAFILTER` | Overrule/ArchOverrule.cs | XData 필터 테스트 |
| `XD_DelALL` | CadFunction.cs | 선택 객체 XData 전체 삭제. 필터 `LINE,POLYLINE,LWPOLYLINE,INSERT,TEXT,MTEXT` (2026-09-22 TEXT/MTEXT 추가 — 그전엔 Text 를 선택조차 못 했다). 잠긴 레이어는 건너뜀 |
| `GXD` | CadFunction.cs | XData 목록 조회 |
| `BP1` | CadFunction.cs | 블럭 포인트 리셋 |
| `ChangeXdataName` | ChangeXdataNameCommand.cs | 선택 객체 XData RegName 변경 |
| `TTG` | Overrule/TreeOverrule.cs | Tree XData 시각화 토글 (Line 색상/라벨 + Block `"Disp"` 텍스트 + Poly `"CFM"` 텍스트 — 2026-09-19 Poly 추가) |

---

## 9. XClip 관련

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `XCLIP2PL` | JXclip.cs | XClip → 폴리라인 변환 |
| `PROCESS_XCLIP` | Xclip.cs | XClip 처리 |

---

## 10. 클릭 선택 / Transient

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `c_SELECTBYCLICK` | SelectLinesByClickTransient.cs | 클릭으로 라인 선택 |
| `c_SELECTBYCLICKFirst` | SelectLinesByClickTransient.cs | 클릭 선택 (첫번째 우선) |
| `TESTTR` | SelectLinesByClickTransient.cs | Transient 테스트 |
| `CLEARTRANSIENTS` | SelectLinesByClickTransient.cs | Transient 그래픽 제거 |
| `SELECTBYCLICK_INFO` | SelectLinesByClickTransient.cs | 클릭 선택 정보 |
| `DRAWTEMPLINE` | SelectLinesByClickTransient.cs | 임시 라인 그리기 |
| `CLEARTEMPLINES` | SelectLinesByClickTransient.cs | 임시 라인 제거 |
| `DRAWTEMPLINES` | SelectLinesByClickTransient.cs | 임시 라인 다수 그리기 |
| `COUNTTEMPLINES` | SelectLinesByClickTransient.cs | 임시 라인 개수 |

---

## 11. 폰트 / 텍스트 스타일

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `UpdateTextStyles` | AcadUtil.cs | 텍스트 스타일 업데이트 |
| `ReplaceMissingFonts` | JFont.cs | 누락 폰트 교체 |
| `ReplaceAllMissingFonts` | JFont.cs | 전체 누락 폰트 교체 |

---

## 12. 기타 유틸리티

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `QQ` | Commands.cs | SelectSimilar 단축키 |
| `PWD` | Commands.cs | 활성 도면 풀패스 표시 (`doc.Name`) |
| `MER` | JBoundary.cs | 경계 병합 (Merge) |
| `MEC` | JBoundary.cs | 경계 병합 (Copy) |
| `DrawAdvancedGraph` | JMathUtil.cs | 고급 그래프 그리기 |
| `CreateOpenStudioSpace` | JOpenStudio.cs | OpenStudio Space 생성 |
| `SUBSEL` | SubSelCommand.cs | SelectImplied 후 사각 영역으로 sub-selection |

---

## 13. 자동 로드

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `REGISTER_AUTOLOAD` | AutoLoadInitializer.cs | 자동 로드 등록 |
| `UNREGISTER_AUTOLOAD` | AutoLoadInitializer.cs | 자동 로드 해제 |
| `LIST_AUTOLOAD` | AutoLoadInitializer.cs | 자동 로드 목록 |
| `CHECK_AUTOLOAD` | AutoLoadInitializer.cs | 자동 로드 상태 확인 |

---

## 14. 배관/덕트 Tree 분석 & DiaNote

### Tree 분석 메인

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `LINETREE` | PipeDiaCalc/LineTreeBuilder.cs | Line Tree 구조 분석 (균등표법, Supply/Return 모드는 `LINETREE_FORM`에서 선택) |
| `LINETREE_FORM` | PipeDiaCalc/LineTreeFormCommand.cs | LineTree Form (Supply/Return 모드, 결과 검토 후 Apply) |
| `LINETREE_LOADS` | PipeDiaCalc/LineTreeBuilder.cs | LineTree 노드 부하 목록 |
| `LINETREE_STATS` | PipeDiaCalc/LineTreeBuilder.cs | LineTree 통계 |
| `FCULINETREE` | PipeDiaCalc/FcuLineTreeFormCommand.cs | FCU Tree 분석 + Form (H-W 공식, Leaf tp CrossingWindow) |
| `DUCTTREE` | PipeDiaCalc/DuctTreeCommand.cs | Duct Tree 분석 + Form (CMH 누적, Spec 산정 미구현) |

### Leaf Block 부하 입력

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `PPL` | PipeDiaCalc/LineTreeBuilder.cs | Pipe Load (LineTree용 Block 부하 입력) |
| `LPM` | PipeDiaCalc/LineTreeBuilder.cs | Block LPM XData 기록 (FCU 부하, `"LPM"`+`"Disp"` 동시 기록) |
| `CMH` | PipeDiaCalc/LineTreeBuilder.cs | Block CMH XData 기록 (디퓨져 풍량, BBox 내 Text 추출) |
| `CMHT` | PipeDiaCalc/LineTreeBuilder.cs | Block CMH 키보드 입력 (`"CMH"`+`"Disp"` 기록) |
| `Insert_Diffuser` | PipeDiaCalc/DiffuserInsertCommand.cs | 룸 풍량/계통(SA·RA·EA·OA)/Type(RPD·SPD·RAD·SAD)/개수 → 선정표에서 표준풍량 ≥ 한 대당 풍량인 최소 행 선정 → 블럭 `JArch_`+Type 을 참조 도면에서 가져와 시작점부터 +X 로 ND×2 순간격 배치, XData `Diffuser`(=Type)/`SystemType`/`Type`/`Size`/`ND`/`CMH`/`Disp` 기록 (2026-09-19 신규, [[InsertDiffuser]]) |
| `Diffuser_Spec` | PipeDiaCalc/DiffuserInsertCommand.cs | `"2400,RPD,RA,3"`(CFM,Type,SystemType,개수) 형식 Text 를 검증한 뒤 XData `Diffuser`=Type 기록 + 색상 41·기울기 5° 적용. 형식 오류는 이유 출력 후 건너뜀 (2026-09-22 신규, [[InsertDiffuser]] §7) |
| `Insert_Damper` | PipeDiaCalc/DamperInsertCommand.cs | Line 선택 → 클릭점에 가까운 끝점에서 선 안쪽 225 지점에 동적 블럭 `JArch_Damper` 삽입(참조 도면에서 가져옴). Line XData `a` 의 1/2 을 `Dis1`/`Dis2` 에 대입, 회전은 가까운→먼 끝점 방향 (2026-09-21 신규, [[InsertDamper]]) |

`LineExtend2Block` (1번 섹션)도 Tree 분석 준비 단계에서 사용 — Line을 Block BBox까지 연장해 Leaf 매핑 보장.

### Tree 시각화

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `TTG` | Overrule/TreeOverrule.cs | Tree XData 시각화 토글 — Line/Block/Poly 세 인스턴스 (8번 섹션 참조) |

상세: [[LineTreeTechNote]], [[FcuLineTreeTechNote]], [[DuctTreeTechNote]], [[TreeOverrule]], [[LPM]], [[CMH]], [[InsertDiffuser]], [[InsertDamper]]

### DiaNote / DiaTree

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `cmd_DiaTree` | PipeDiaCalc/DiaNote.cs | 배관 분기 형상 폴리라인 생성 (S→E→F→G→K) |
| `cmd_DiaNoteVer` | PipeDiaCalc/DiaNote.cs | 수직 배관 치수 노트 |
| `cmd_DiaNoteVer1` | PipeDiaCalc/DiaNote.cs | 수직 배관 치수 노트 (`BaseLen` 기준, 단순) |
| `cmd_DiaNoteHor` | PipeDiaCalc/DiaNote.cs | 수평 배관 치수 노트 |
| `cmd_DiaNoteHor1` | PipeDiaCalc/DiaNote.cs | 수평 배관 치수 노트 (`BaseLen` 기준, 단순) |
| `DD` | PipeDiaCalc/DiaNote.cs | 선택 Line의 `"Dia"` XData 표시 |
| `Cmd_SetDiaNoteBase` | PipeDiaCalc/DiaNote.cs | 평행 2 Line 수선거리로 `DiaNote.BaseLen` 설정 (NOD 영속화) |
| `Cmd_SetDiaNoteBaseText` | PipeDiaCalc/DiaNote.cs | `DiaNote.BaseLen` 키보드 입력 (NOD 영속화) |
| `susDia` | PipeDiaCalc/DiaNote.cs | SusPipe 배관 치수 |
| `susDia1` | PipeDiaCalc/DiaNote.cs | SusPipe 배관 치수 (변형) |

상세: [[DiaNote 개요]], [[DiaTree]], [[DiaTreeNote]], [[SetDiaNoteBase]], [[SetDiaNoteBaseText]], [[SusPipe]]

---

## 15. Ribbon / UI

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `COLLAB_RIBBON_LOAD` | Ribbon/CollabRibbon.cs | "JArch" 탭 수동 등록 |
| `COLLAB_RIBBON_UNLOAD` | Ribbon/CollabRibbon.cs | 탭 완전 제거 |
| `SHOWPAL` | PaletteSample.cs | 도구 팔레트 표시 (lazy init) |
| `HIDEPAL` | PaletteSample.cs | 도구 팔레트 숨김 |

**JArch 탭은 Ainit 가 실행된 도면에서만 보임** (2026-04-30~). DLL 로드 시 자동 생성 안 함. 가시성은 NOD `AINIT_DEFAULTS` 사전 존재 여부로 판정 — `LoadDwgDefaults` 가 DWG open/active 마다 자동 토글. 상세는 섹션 16 참조.

상세: [[RibbonMenu]] — Ribbon UI 계층, 가시성 lifecycle, `SetRibbonTabVisible(bool)` API, 새 탭 추가 절차
상세: [[PaletteSample]] — `SHOWPAL` / `HIDEPAL` 도킹 팔레트, lazy init 패턴, 버튼 Tag 기반 `SendStringToExecute` 디스패치

---

## 16. 도면별 영구 BaseLen + JArch 리본 게이트 (Ainit / NOD)

| 명령어 | 파일 | 설명 |
|--------|------|------|
| `Ainit` | PipeDiaCalc/AinitCommand.cs | NOD `AINIT_DEFAULTS/DiaNoteHeight = 50.0` 초기화 + JArch 리본 탭 활성화 |

`AINIT_DEFAULTS` 사전 존재 여부가 두 가지 효과를 가짐:
1. **`DiaNote.BaseLen` 도면별 영구 저장** — `LoadDwgDefaults` 가 DWG open/active 시 NOD → BaseLen 동기화
2. **JArch 리본 탭 가시성** — 사전이 있으면 보이고, 없으면 숨김 (DLL NETLOAD 시 자동 생성 안 함)

관련 파일: `PipeDiaCalc/DwgDefaultLoader.cs` (NOD ↔ `DiaNote.BaseLen` + 리본 가시성 어댑터). 어셈블리 진입점은 `MyPlugin` 단일화 — Document 이벤트(Created/Activated/BecameCurrent) 마다 `LoadDwgDefaults` 호출.

상세: [[AinitCommand]] — 2-파일 구조, NOD `DxfCode.Real` 저장 포맷, `Cmd_SetDiaNoteBase` 영구 저장 통합, 리본 가시성 lifecycle, Transaction 분리 패턴

---

## 17. 배포 (Inno Setup 인스톨러)

`JArchitecture_Setup.iss` (Inno Setup) → `C:\ProgramData\Autodesk\ApplicationPlugins\JArchitecture.bundle\Contents\` 설치, AutoCAD 자동 로드 (`PackageContents.xml`).

| 핵심 | 내용 |
|---|---|
| 배포 빌드 | DLL은 **Release 산출물만** (`.iss` `Source:` 경로 `...\Release\...` 고정) |
| `JArchLicense.dll` | **네이티브 C++ → Release\|x64 필수**. Debug는 재배포 금지된 Debug CRT(`...140D.dll`) 링크 → 고객 PC 로드 실패 |
| 점검 | 배포 전 `dumpbin /dependents` 로 `D` 접미사 의존성 없음 확인 |

상세: [[autocadInstall]] — 번들 설치 구조, `Assembly.Location` 파일 접근, 빌드 구성(Debug/Release)·네이티브 DLL 의존성·인스톨러 재빌드 절차

---

## 핵심 워크플로우

```
1. 벽 라인 정리    → Wall_Line_Arrange, mmdl, mdl
2. 룸 지정/계산    → To_RoomPoly → Room_Poly_Calc / Room_Poly_ALL_Calc
3. 블럭 구분 지정  → To_Window, To_Door, To_Column, To_Symbol
4. 면적 작업       → aWork, a_Work
5. 레이어 관리     → JLayer.cs 단축 명령어 시리즈
```

## 기본 레이어 규칙

| 상수 | 레이어명 | 용도 |
|------|---------|------|
| `Layer.Wall` | `!Arch_Wall` | 벽 |
| `Layer.Room` | `!Arch_Room` | 룸 |
| `Layer.RoomPoly` | `!Arch_RoomPoly` | 룸 폴리 |
| `Layer.Block` | `!Arch_Block` | 창/문 블럭 |
| `Layer.OutWall` | `!Arch_OutWall` | 외벽 |
