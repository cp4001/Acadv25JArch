이 폴더에 JArch_Blocks.dwg 를 둔다.

Insert_Damper / Insert_Diffuser 가 이 도면에서 블럭 정의를 가져온다(WblockCloneObjects, 기존 정의는 덮어씀).

필요한 블럭 이름:
- JArch_Damper   (동적 블럭, 거리 파라미터 Dis1 / Dis2)
- JArch_RPD
- JArch_SPD
- JArch_RAD
- JArch_SAD

csproj 가 빌드 시 출력 폴더의 Blocks\ 로 복사하고, 인스톨러가 {app}\Contents\Blocks 로 배포한다.
파일이 없으면 두 명령은 실행 시 오류 메시지를 내고 종료하며, 인스톨러 컴파일도 #error 로 실패한다.
