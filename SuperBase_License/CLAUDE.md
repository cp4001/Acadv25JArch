# CLAUDE.md - Supabase 라이선스 검증 시스템

## 프로젝트 개요
Supabase(PostgreSQL + PostgREST)에 라이선스 테이블을 두고,
C++ 클라이언트가 `comID`로 사용 기한 유효성만 조회하는 시스템.

| 구분 | 언어 | 역할 |
|---|---|---|
| 검증 클라이언트 | C++ | comID 전달 → true/false 수신 (조회 전용). 배포됨 → `anon` 키 |
| 관리 프로그램 | C# | 테이블 CRUD. 관리자 PC 전용, **배포 안 함** → `service_role` 키 |
| 백엔드 | Supabase | PostgreSQL + PostgREST |

두 프로그램의 권한이 다른 것이 이 설계의 핵심이다. 배포되는 쪽은 RPC 하나만 부를 수 있고,
테이블을 직접 만지는 쪽은 관리자 PC 밖으로 나가지 않는다.

---

## 현재 상태 (2026-09-01 기준)

| 단계 | 상태 |
|---|---|
| PRD 작성 | ✅ `prd.md` |
| Supabase 프로젝트 생성 | ✅ `jarch-license` |
| MCP 서버 연결 | ✅ 설정 완료 (단, 토큰 전달 이슈 있음 — 아래 «MCP 설정» 참조) |
| DB 스키마(`licenses` 테이블 + RLS + RPC) | ✅ 적용·검증 완료 |
| REST 엔드투엔드 검증 | ✅ 완료 (`prd.md` §7 1번 해소) |
| C++ 클라이언트 | ✅ `cpp/` — 빌드·서버 검증 완료 |
| C# 관리 프로그램 | ✅ `csharp/JArchLicenseAdmin/` — 빌드·CRUD·UI 검증 완료 |
| C# P/Invoke 샘플 | ✅ `csharp/JArchSbLicenseTest/` — AutoCAD 플러그인 연결 참조 |

---

## Supabase 프로젝트

| 항목 | 값 |
|---|---|
| 이름 | `jarch-license` |
| project-ref | `bvgpukvuygluxternzig` |
| URL | `https://bvgpukvuygluxternzig.supabase.co` |
| 리전 | `ap-northeast-2` (Seoul) |
| 조직 | `cp4001's Org` (`zsqqwitshcrnfpahqfyr`) |
| 생성일 | 2026-09-01 |

- **무료 티어는 1주 미사용 시 일시정지(INACTIVE)** 된다. 세션 시작 시 상태를 확인할 것
- DB 비밀번호는 생성 시 무작위 발급 후 **어디에도 기록하지 않았다.**
  직접 접속(psql/pooler)이 필요하면 Dashboard → Project Settings → Database에서 재설정
- anon(publishable) 키는 MCP `get_publishable_keys` 도구로 조회
  (MCP가 인증 실패면 «키 조회» 절의 REST 방식 사용)

---

## MCP 설정

정의 위치: **저장소 루트** `Acadv25JArch/.mcp.json` (이 폴더가 아님)

```json
{
  "mcpServers": {
    "supabase": {
      "command": "C:\\Program Files\\nodejs\\node.exe",
      "args": [
        "C:\\home\\jjh\\.npm-global\\node_modules\\@supabase\\mcp-server-supabase\\dist\\transports\\stdio.js",
        "--project-ref=bvgpukvuygluxternzig"
      ],
      "env": { "SUPABASE_ACCESS_TOKEN": "${SUPABASE_ACCESS_TOKEN}" }
    }
  }
}
```

- 패키지: `@supabase/mcp-server-supabase@0.11.0` (npm global, `C:\home\jjh\.npm-global`)
- **`cmd /c npx` 를 쓰지 않는다** — Windows에서 stdio 파이프가 깨질 수 있어 `node.exe` 직접 호출
- `--read-only` 미지정 → **쓰기 허용** (스키마 생성이 필요하므로)
- `--project-ref` 로 이 프로젝트에만 접근 (계정의 다른 프로젝트는 차단)
- 노출 도구 20개: `list_tables`, `execute_sql`, `apply_migration`, `get_advisors`,
  `get_project_url`, `get_publishable_keys`, `deploy_edge_function`, `generate_typescript_types` 등

### 세션 시작 시 주의
1. **저장소 루트(`Acadv25JArch`)에서 세션을 시작**해야 `.mcp.json`이 로드된다.
   이 폴더에서 바로 시작하면 MCP가 안 붙는다
2. `/mcp` 로 `supabase` 가 `connected` 인지 확인
3. 안 붙으면 → 환경변수 미설정이거나 Claude Code 재시작 전 상태.
   `setx` 는 새 프로세스에만 적용된다

### ⚠ `connected` 인데도 Unauthorized 가 나는 경우 (2026-09-01 실측)

`.mcp.json` 의 `${SUPABASE_ACCESS_TOKEN}` 은 **Claude Code 프로세스의 환경**에서 확장된다.
사용자 환경변수에 값이 있어도 **Claude Code 를 `setx` 이후에 재시작하지 않았으면**
빈 문자열로 확장되어 MCP 서버가 이렇게 응답한다:

```
Unauthorized. Please provide a valid access token to the MCP server
via the --access-token flag or SUPABASE_ACCESS_TOKEN.
```

`/mcp` 는 `connected` 로 보인다 — 프로세스는 떴고 인증만 실패한 상태라 증상이 헷갈린다.
진단은 **User 스코프와 Process 스코프를 따로** 볼 것 (둘 다 보면 원인이 바로 드러난다):

```powershell
"User   : " + [Environment]::GetEnvironmentVariable('SUPABASE_ACCESS_TOKEN','User').Length
"Process: " + $env:SUPABASE_ACCESS_TOKEN.Length   # 여기가 비어 있으면 재시작 필요
```

**우회 — 재시작 없이 Management API 직접 호출.** MCP 도구와 같은 일을 다 할 수 있다:

```powershell
$t = [Environment]::GetEnvironmentVariable('SUPABASE_ACCESS_TOKEN','User')
$h = @{ Authorization = "Bearer $t"; 'Content-Type' = 'application/json' }
$ref = 'bvgpukvuygluxternzig'

# 임의 SQL (= execute_sql)
Invoke-RestMethod -Uri "https://api.supabase.com/v1/projects/$ref/database/query" `
  -Headers $h -Method Post -Body (@{ query = 'select 1 as x' } | ConvertTo-Json)

# 마이그레이션 (= apply_migration) — POST .../database/migrations, body: name + query
# 프로젝트 상태  GET  .../v1/projects/$ref            → status 가 ACTIVE_HEALTHY 인지
# 어드바이저     GET  .../v1/projects/$ref/advisors/security  (또는 /performance)
```

### 키 조회 (= `get_publishable_keys`)

```powershell
$k = Invoke-RestMethod -Uri "https://api.supabase.com/v1/projects/$ref/api-keys?reveal=true" `
       -Headers @{ Authorization = "Bearer $t" } -Method Get
($k | Where-Object name -eq 'anon').api_key            # legacy JWT — C++ 코드용
($k | Where-Object type -eq 'publishable').api_key     # sb_publishable_… (신형)
```

`?reveal=true` 없이는 키 값이 마스킹되어 온다.
**`service_role` / `sb_secret_…` 은 이 목록에 같이 나오지만 클라이언트에 넣지 않는다** (보안 규칙 3).

---

## 환경변수 (Windows 사용자 환경변수)

| 이름 | 용도 | 상태 |
|---|---|---|
| `SUPABASE_ACCESS_TOKEN` | Supabase PAT (`sbp_…`, 44자) | ✅ 등록됨. 계정 전체 권한 |
| `NEON_API_KEY` | Neon API 키 (`napi_…`, 69자) | ✅ 등록됨. `Work\Supabase\CompanyMnt` 용 |
| `JARCH_LICENSE_SERVICE_KEY` | service_role 키 (`eyJ…`) — C# 관리 프로그램용 | ⚪ **선택** — 2026-09-03 부터 소스에 키가 박혀 있어 없어도 된다. 설정하면 그쪽이 우선 |

확인:
```powershell
[Environment]::GetEnvironmentVariable('SUPABASE_ACCESS_TOKEN','User')
```

### service_role 키는 소스에 박혀 있다 (2026-09-03 변경)

`SupabaseLicenseClient.EmbeddedServiceKey` 에 들어 있다. **exe 를 더블클릭하면 그냥 뜬다.**

키 선택 순서는 `ResolveServiceKey()` 가 정한다 — **환경변수가 먼저**이고, 없으면 소스의 키를 쓴다.
그래서 키를 회전시켰을 때 재빌드 없이 `setx` 로 임시 교체할 수 있다.

```powershell
setx JARCH_LICENSE_SERVICE_KEY "eyJ..."   # 필요할 때만. 없어도 동작한다
```

**사용자가 판단해 선택한 트레이드오프다** — 관리 프로그램을 배포하지 않고 저장소가 private
이라는 전제. 다만 **git 이력은 되돌릴 수 없으므로**, 저장소를 공개하거나 외부 협업자를
추가하는 시점에는 Supabase 에서 키를 회전시켜야 한다.

---

## 보안 규칙

1. **토큰을 `.mcp.json`·소스·문서에 평문으로 쓰지 않는다.** 반드시 `${VAR}` 참조
   (`.mcp.json`은 `command`/`args`/`env`/`url`/`headers` 에서 `${VAR}`·`${VAR:-기본값}` 확장 지원)
2. 이 저장소의 `.gitignore`는 `.mcp.json`을 제외하지 않는다 → 커밋된다는 전제로 작성
3. **`service_role` 키는 배포되는 프로그램에 절대 포함하지 않는다** (RLS 전면 우회)
   - 예외는 C# 관리 프로그램 하나뿐이며, **배포하지 않는다는 전제**로만 성립한다
   - 2026-09-03 부터 그 프로그램의 소스에 키가 박혀 있다. **저장소가 private 이라는
     전제**로 사용자가 선택한 것이다 — 공개 전환 시 키 회전 필요 (§2 와 상충하는 유일한 지점)
4. `anon` 키는 노출 전제 키 → 반드시 RLS와 함께 사용
5. 테이블 직접 조회는 차단하고 **RPC 함수 하나만** 노출 (comID 목록·회사명 유출 방지)
6. 배포되는 클라이언트는 boolean만 수신

---

## 적용된 스키마 (2026-09-01)

마이그레이션 `create_licenses_and_check_license` — `prd.md` §6.1 SQL 그대로.
적용 전 `public` 스키마는 비어 있었다.

| 확인 항목 | 실측값 |
|---|---|
| `public.licenses` | 존재, `rowsecurity=true`. **INSERT 정책 1건**(자가 등록용, 2026-09-02 추가). SELECT/UPDATE/DELETE 는 정책 0건 = 여전히 완전 차단 |
| `public.check_license(text)` | `security definer`, `search_path=public`, owner `postgres` |
| EXECUTE 권한 | `anon`, `authenticated`, `postgres`, `service_role` |

### 마이그레이션 `license_status_and_self_registration` (2026-09-02)

AutoCAD 플러그인이 쓰는 RPC 둘과 등록용 RLS 를 추가했다.

| 객체 | 종류 | 반환/역할 |
|---|---|---|
| `jarch_trial_exp_date()` | sql stable | 체험 만료일 = 오늘+30. **체험 일수는 여기만 고친다** (등록 함수와 RLS 정책이 같이 본다) |
| `check_license_status(text)` | **security definer** | `NONE` / `YYYY-MM-DD\|Y` / `YYYY-MM-DD\|N` — 등록여부·만료일·유효성을 한 번에 |
| `register_license(text,text,text,text)` | **security invoker** | `OK\|YYYY-MM-DD` / `DUP` / `BAD` |
| `licenses_anon_self_register` | RLS policy (INSERT, to anon) | com_id 형식 + `exp_date = 체험만료일` + `reg_date = 오늘` 을 강제 |

**`register_license` 가 `security invoker` 인 것이 핵심이다.** `security definer` 로 두면
소유자(postgres, BYPASSRLS) 권한으로 돌아 위 정책을 우회해 버린다. 호출자(anon) 권한으로
실행돼야 정책이 실제로 적용된다.

응답을 **스칼라 text 고정 포맷**으로 만든 이유는 C++ 클라이언트에 JSON 파서가 없기 때문이다
(`prd.md` §5.2 의 "응답이 스칼라라 파서 불필요" 를 그대로 유지).

#### anon 으로 실측한 경계 (2026-09-02)

| 시도 | 결과 |
|---|---|
| `check_license_status` 4가지 상태 | 전부 사양대로 |
| `register_license` 신규 / 재시도 / 잘못된 형식 | `OK\|2026-10-02` / `DUP` / `BAD` |
| `GET /licenses?select=*` | `[]` — SELECT 정책 없음 |
| `POST /licenses` 로 `exp_date=2099-12-31` | **HTTP 401** `new row violates row-level security policy` |
| `POST /licenses` 로 정상 기한 | 201 — 정책이 허용하는 범위. 자가 등록 자체는 열려 있다 |
| `PATCH` 로 남의 행 기한 연장 / `DELETE` | HTTP 204 지만 **실제로는 0건**. RLS 가 막았다 (표 아래 함정 참조) |

⚠️ **PostgREST 는 0건이 처리돼도 UPDATE/DELETE 에 204 를 준다.** 위 PATCH/DELETE 가
성공처럼 보였지만 테이블을 직접 조회해 `TEST-EXPIRED` 의 기한이 그대로이고 `TEST-VALID` 가
살아 있음을 확인했다. **상태코드만 보고 "뚫렸다/막혔다" 를 판단하지 말 것.**

#### 자가 등록을 열면서 받아들인 것

- 누구든 **형식이 맞는 com_id 로 30일 체험을 만들 수 있다.** 서버는 그 ID 가 정말 그 PC 의
  하드웨어에서 나왔는지 확인할 방법이 없다. 자가 등록을 택한 이상 피할 수 없는 비용이다
- 막을 수 있는 것은 막았다 — 기한을 스스로 늘리는 것, 남의 행을 고치는 것, 목록을 읽는 것,
  같은 PC 를 두 번 등록하는 것
- 남은 노출은 **무의미한 행을 대량 생성하는 것** 하나다. 필요해지면 Rate limiting
  (`prd.md` §7)이 이 지점에 걸린다

> `$$` 대신 `$fn$` 태그를 썼다. Management API에 JSON으로 넘길 때 익명 달러 인용이
> 다른 도구(psql 메타명령 등)와 섞이면 모호해질 수 있어 이름 있는 태그가 안전하다.

### REST 엔드투엔드 검증 결과

`POST https://bvgpukvuygluxternzig.supabase.co/rest/v1/rpc/check_license`
— 모두 HTTP 200 + **스칼라 boolean** 응답 (`prd.md` §5.1 사양 전부 일치):

| `p_com_id` | exp_date | 응답 |
|---|---|---|
| `TEST-VALID` | 오늘+30 | `true` |
| `TEST-TODAY` | 오늘 | `true` ← 만료일 당일 사용 가능 확인 |
| `TEST-EXPIRED` | 오늘−1 | `false` |
| `NO-SUCH-ID` | (미등록) | `false` |

- `GET /rest/v1/licenses?select=*` (anon) → HTTP 200 **`[]`** — RLS가 정상 차단
- **legacy `anon` JWT와 신형 `sb_publishable_…` 키 둘 다 동작**한다.
  `prd.md` §6.2 C++ 코드는 legacy anon 키 기준
- `TEST-*` 3건은 테이블에 남겨 뒀다 (C++ 클라이언트 검증용). 불필요해지면
  `delete from public.licenses where com_id like 'TEST-%'`

### 어드바이저 경고 — 모두 의도된 설계

`security` 3건, `performance` 0건. 아래는 **수정 대상이 아니다**:

| 경고 | 판단 |
|---|---|
| `rls_enabled_no_policy` (INFO) | 2026-09-02 이후 INSERT 정책 1건이 생겼다. SELECT 등에 정책이 없는 것은 여전히 의도된 차단 |
| `anon_security_definer_function_executable` (WARN) | RPC 하나만 노출하는 것이 목적 |
| `authenticated_security_definer_function_executable` (WARN) | 위와 동일. 클라이언트는 anon만 쓰므로 `authenticated` 회수도 가능하나 `prd.md` §6.1 대로 유지 |

---

## C++ 클라이언트 — `cpp/`

| 파일 | 역할 |
|---|---|
| `JArchSbLicense.h` / `.cpp` / `.def` | DLL 본체 |
| `test_client.cpp` | 회귀 검증용 콘솔 |
| `build.bat` | DLL + exe 빌드 |

```c
extern "C" __declspec(dllexport) int CheckLicenseOnline(const char* comID);
// 1 = 사용 가능 / 0 = 미등록·기간초과·HTTP오류·네트워크오류

extern "C" __declspec(dllexport) int GetMachineId(char* buf, int bufSize);
// 이 PC 의 고유 ID 를 만든다. buf 는 JARCH_MACHINE_ID_BUFSIZE(32) 이상.
// 1 = 성공 / 0 = 실패(buf 는 빈 문자열)
```

### 컴퓨터 고유 ID — `GetMachineId` (2026-09-02 추가)

```
정상        XXXX-XXXX-XXXX-XXXX-XXXX      (24자)   예: 5D07-1088-5DF0-6EF3-A203
UUID 불량   M-XXXX-XXXX-XXXX-XXXX-XXXX    (26자)   MachineGuid 단독 폴백
```

이 값을 그대로 `com_id` 로 써서 `CheckLicenseOnline` 에 넘긴다.

**⚠ 아래 입력 규칙은 절대 바꾸지 않는다.** 한 글자만 달라져도 등록된 모든 PC 의
ID 가 바뀌어 전부 재등록해야 한다:

| 항목 | 확정값 |
|---|---|
| 소스 1 | `HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid` |
| 소스 2 | SMBIOS Type 1 구조체의 UUID (offset 0x08, 16바이트) |
| 대소문자 | 두 값 모두 **대문자** |
| 구분자 | `\|` 한 글자 |
| GUID 형식 | 하이픈 포함 원문 그대로 |
| 해시 | SHA-256, 출력은 **대문자 hex** 64자 |
| 자르기 | 앞 **20자** (= 80비트) |
| 포맷 | 4자씩 끊어 `-` 로 연결 |

**두 소스를 섞는 이유**는 약점이 서로 반대이기 때문이다. MachineGuid 는 OS 재설치로
바뀌지만 하드웨어 교체에는 불변, SMBIOS UUID 는 메인보드 교체로 바뀌지만 OS 재설치에는
불변이다. 대신 **어느 쪽이 바뀌어도 ID 가 달라져 재등록이 필요하다** — 의도된 트레이드오프다.

- 80비트라 1만 대 등록 시 충돌 확률 약 4×10⁻¹⁷. 실질적으로 0
- SMBIOS UUID 가 전부 `00` 또는 전부 `FF` 인 OEM 미설정 기종은 MachineGuid 단독으로
  해시하고 접두어 `M-` 를 붙여 구분한다
- MachineGuid 조차 못 읽으면 **0 을 반환한다.** 추측해서 ID 를 만들지 않는다
- **관리자 권한 불필요.** TPM EK 가 더 강하지만 관리자 권한이 필요해 탈락시켰다
- WMI(COM)를 쓰지 않는다 — `RegGetValueW` + `GetSystemFirmwareTable('RSMB')` 직접 호출.
  AutoCAD 안에서 COM 초기화 상태가 꼬일 수 있어서다
- `RRF_SUBKEY_WOW6464KEY` 를 명시했다. x64 에서는 기본값과 같지만 32비트로 잘못 빌드하면
  `WOW6432Node` 를 보게 되어 "값 없음" 으로 오진한다
- SMBIOS UUID 는 앞 세 필드를 리틀엔디언으로 읽어 표준 GUID 형식으로 만든다(SMBIOS 2.6 규정).
  **`Win32_ComputerSystemProduct.UUID` 와 같은 값이 나오는 것을 실측 확인**했다 —
  관리자가 PowerShell 로 대조할 수 있어야 디버깅이 된다

의존성은 `bcrypt.dll` + `ADVAPI32.dll` 이 늘었다. 둘 다 Windows 내장이라
배포물은 그대로다(OpenSSL 없음, `dumpbin /dependents` 확인).

### WinHTTP 를 쓴다 — cpp-httplib 아님

`prd.md` 최초 확정 사양은 cpp-httplib\[openssl\] + nlohmann/json 이었으나
**WinHTTP 로 바꿨다** (근거는 `prd.md` §5.2). 요점:

- 이 PC 에 **vcpkg 가 없다.** 도입하면 OpenSSL 빌드까지 수 GB·수십 분 선행
- cpp-httplib\[openssl\] 은 OpenSSL DLL 2개를 배포물에 얹는다 — AutoCAD 로드 DLL 이라 부담
- 응답이 스칼라 `true`/`false` 한 줄이라 **JSON 파서가 필요 없다**
- 기존 `JArchLicense/build.bat` 관례(=`cl` + 시스템 라이브러리만)와 동일

`dumpbin /dependents` 실측 — **OpenSSL 없음**:

```
MSVCP140.dll  WINHTTP.dll  KERNEL32.dll  VCRUNTIME140.dll
VCRUNTIME140_1.dll  api-ms-win-crt-*.dll
```

VC++ 재배포 패키지(Release CRT)만 요구하며 이는 AutoCAD 가 이미 갖고 있다.
`/MD` 로 빌드한다 — **`/MTd`·Debug CRT 금지** (재배포 불가, `JArchLicense` 때와 동일한 제약).

### 빌드

**x64 Native Tools Command Prompt for VS** 에서:

```
cd SuperBase_License\cpp
build.bat
```

Claude Code 등 비대화형에서는 vcvars 를 먼저 불러야 한다:

```powershell
$vc = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -property installationPath
cmd /c "call `"$vc\VC\Auxiliary\Build\vcvars64.bat`" >nul 2>&1 && cd /d <cpp경로> && call .\build.bat"
```

- `cl` 에 **`/utf-8` 필수** — 없으면 한글 주석이 CP949 로 읽혀 C4819 경고가 쏟아진다
- `cmd /c` 에서 exe·bat 을 부를 때는 `.\` 를 붙일 것 (cmd 는 PATH 에 `.` 이 없다)
- `build.bat` 에는 성공 시 `pause` 가 없다(자동 실행 가능하게). 종료코드로 성패를 판단

### 검증 결과 (2026-09-01, 실제 서버 대상)

```
> .\test_client.exe
[OK  ] TEST-VALID     기대=true  실제=true
[OK  ] TEST-TODAY     기대=true  실제=true
[OK  ] TEST-EXPIRED   기대=false 실제=false
[OK  ] NO-SUCH-ID     기대=false 실제=false
[OK  ] (빈값)         기대=false 실제=false
전부 통과
```

- `test_client.exe <comID>` — 단건 조회. 종료코드 **0=유효 / 1=차단**
- `test_client.exe --id` — 이 PC 의 고유 ID 만 출력 (`JArchSbLicenseTest.exe --id` 도 동일)
- **fail-closed 검증 완료**: 호스트를 `no-such-host-xyzq.invalid` 로 바꿔 빌드한 사본에서
  `false` + 0.52초 반환. 네트워크가 끊겨도 통과되지 않는다
- ⚠️ 종료코드를 `cmd /c "... & echo %errorlevel%"` 로 읽으면 **파싱 시점에 확장되어 이전 값이 나온다.**
  PowerShell `$LASTEXITCODE` 를 쓸 것

### 설계 메모

- **판정은 전부 서버가 한다.** 클라이언트에 날짜 비교 로직이 없어 PC 시계를 돌려도 무의미
- 응답이 64바이트를 넘으면 차단 — 스칼라 boolean 이 아닌 것은 응답으로 보지 않는다
- 모든 실패 경로가 `result = 0` 에서 시작해 `goto cleanup` 으로 빠진다 (fail-closed 기본값)
- anon 키는 소스에 평문으로 박혀 있다. **의도된 것이다** (`prd.md` §3.1 — 클라이언트 비밀은
  반드시 유출된다는 전제). 이 키로 가능한 건 `check_license` RPC 호출 하나뿐

---

## C# 관리 프로그램 — `csharp/`

.NET 8 WinForms. `licenses` 테이블 CRUD 전부.

| 파일 | 역할 |
|---|---|
| `JArchLicenseAdmin.sln` | VS 솔루션 (classic `.sln`) |
| `JArchLicenseAdmin/License.cs` | 행 모델 |
| `JArchLicenseAdmin/SupabaseLicenseClient.cs` | PostgREST CRUD |
| `JArchLicenseAdmin/MainForm.cs` / `.Designer.cs` | 화면 |
| `JArchLicenseAdmin/Program.cs` | 진입점 + 키 확인 |

```
cd SuperBase_License\csharp
dotnet build -c Release
dotnet run --project JArchLicenseAdmin
```

- TFM `net8.0-windows`. **SDK 8 이 없어도 된다** — 설치된 SDK 9/10 이 net8.0 을 타깃팅한다
  (WindowsDesktop 런타임 8.0.30 존재 확인)
- `dotnet new sln` 은 SDK 10 에서 기본이 **`.slnx`** 다. 저장소 관례(`.sln`)에 맞추려면
  **`--format sln`** 을 붙일 것
- 빌드 산출물 경로: `JArchLicenseAdmin\bin\Release\net8.0-windows\JArchLicenseAdmin.exe`
- **실행 전 `JARCH_LICENSE_SERVICE_KEY` 가 필요하다.** 아직 미등록 상태다 — «환경변수» 절 참조

### 화면

```
┌─ 검색 [           ] [새로 고침] ─────────────────────┐
│  라이선스 ID │ 사용 기한 │ 등록일 │ 사용자 │ 회사명 │ 부서명 │  ← DataGridView
│  (행 클릭 → 아래 편집칸 자동 채움)                    │
├───────────────────────────────────────────────────────┤
│  라이선스 ID * [    ]      사용 기한 * [yyyy-MM-dd ▼] │
│  사용자        [    ]      회사명      [    ]         │
│  부서명        [    ]      등록일      (읽기 전용)    │
│      [새 입력] [추가] [수정] [삭제]                   │
├───────────────────────────────────────────────────────┤
│  3건 조회됨 — 22:21:05                    ← StatusStrip│
└───────────────────────────────────────────────────────┘
```

- 검색은 입력 즉시 필터 (라이선스 ID / 사용자 / 회사명 / 부서명, 대소문자 무시)
- 삭제는 확인 대화상자를 거치며 **기본 버튼이 «아니오»** 다
- `com_id` 는 수정되지 않는다 — 바꾸려면 삭제 후 다시 추가

### service_role 키 — 소스에 박혀 있다 (환경변수가 우선)

관리자 PC 전용이고 배포하지 않으므로 **`service_role` 을 쓴다** — RLS 를 우회해
테이블을 직접 읽고 쓴다. anon 쪽 차단(SELECT 정책 0건)은 그대로 유지된다.

```csharp
// SupabaseLicenseClient.cs
private const string EmbeddedServiceKey = "eyJ...";

public static string? ResolveServiceKey()   // 환경변수 > 소스 상수 > null
```

⚠️ **이 프로그램은 절대 배포하지 않는다.** exe 하나만 나가도 키가 그대로 나간다 —
DB 전체가 읽기·쓰기 가능해진다 (`prd.md` §3.2 1번).

⚠️ **이 프로그램은 절대 배포하지 않는다.** `service_role` 키가 유출되면 DB 전체가
읽기·쓰기 가능해진다 (`prd.md` §3.2 1번).

### 설계 메모

- **날짜는 전부 문자열(`yyyy-MM-dd`)로 다룬다.** `DateTime` 으로 바꾸면 로컬 시간대가
  개입해 하루씩 밀린다. 만료 판정은 서버 `check_license` 가 Asia/Seoul 로 하고
  관리 프로그램은 문자열만 나른다
- `reg_date` 는 쓰기 페이로드에서 뺀다 — DB 기본값(오늘, Asia/Seoul)이 채운다
- `com_id` 는 PK 라 수정 대상에서 제외. 바꾸려면 삭제 후 다시 추가
- 빈 입력칸은 `null` 로 보낸다 (빈 문자열과 미입력을 DB 에서 구분하지 않으려고)
- PATCH/DELETE 응답이 `[]` 면 "해당 행 없음"으로 보고 예외를 던진다.
  PostgREST 는 0건이 갱신돼도 HTTP 200 을 주기 때문에 이 검사가 없으면 조용히 성공한다
- 검색은 **서버가 아니라 받아 온 목록에서** 거른다. 건수가 적고, PostgREST 의
  `or=(...)` 필터는 값에 쉼표·괄호가 섞이면 깨지기 쉽다
- **그리드 컬럼은 `MainForm.cs` 생성자(`BuildGridColumns`)에서 만든다.**
  VS Designer 를 열었다 저장하면 코드로 추가한 컬럼을 날려먹는 전례가 있어서다.
  Designer 에는 `DataGridView` 자체만 있다
- 도킹은 z-order 역순으로 처리된다. `Controls.Add` 순서가 `grid`(Fill) → `pnlEdit`(Bottom)
  → `statusStrip` → `pnlTop`(Top) 이어야 의도대로 나온다

### 검증 결과 (2026-09-01, 실제 서버 대상)

CRUD 계층은 **배포 소스 파일(`License.cs`·`SupabaseLicenseClient.cs`)을 그대로 컴파일한
콘솔 하네스**로 12개 항목 검사 — 전부 통과:

| 검사 | 결과 |
|---|---|
| LIST — service_role 이 RLS 우회 | 3건 (anon 은 `[]`) |
| CREATE / `reg_date` 서버 채움 / 건수 +1 | OK |
| UPDATE — exp_date·user_name·빈칸→NULL | OK |
| UPDATE / DELETE — 없는 행이면 예외 | OK |
| CREATE — PK 중복 시 HTTP 409 | OK |
| DELETE — 원상복구 | OK |

UI 는 실행 후 화면 캡처로 확인: 목록 3건 표시, 행 선택 시 편집칸 자동 채움,
검색 `VALID` → 1건 / `ZZZZ` → 0건, 상태바 `3건 조회됨`.

---

## C# → C++ P/Invoke 샘플 — `csharp/JArchSbLicenseTest/`

C++ DLL 을 C# 에서 부르는 콘솔 샘플이자 회귀 검증. **AutoCAD 플러그인 연결 시 이걸 참조한다.**

| 파일 | 역할 |
|---|---|
| `NativeLicense.cs` | P/Invoke 래퍼 — **플러그인에 이 파일만 복사하면 된다** |
| `Program.cs` | `TEST-*` 회귀 검증 + 단건 조회 |

```csharp
[DllImport("JArchSbLicense.dll", CallingConvention = CallingConvention.Cdecl)]
private static extern int CheckLicenseOnline([MarshalAs(UnmanagedType.LPUTF8Str)] string comID);

public static bool IsValid(string comID) => CheckLicenseOnline(comID) != 0;
```

```
JArchSbLicenseTest.exe            → 회귀 검증
JArchSbLicenseTest.exe ABC-1234   → 단건 조회 (종료코드 0=유효 / 1=차단)
```

### 붙일 때 반드시 지킬 것 셋

1. **`<PlatformTarget>x64</PlatformTarget>`** — 네이티브가 x64 다. AnyCPU 로 두면
   32비트로 실행될 때 `BadImageFormatException` 이 난다
2. **`UnmanagedType.LPUTF8Str`** — `CharSet.Ansi` 로 두면 시스템 ANSI 코드페이지(이 PC 는
   CP949)로 변환되어, comID 에 한글이 섞이면 서버로 가는 JSON 본문이 깨진 UTF-8 이 된다
3. **`JArchSbLicense.dll` 을 exe(플러그인 DLL) 옆에 둘 것.** csproj 가 `cpp/` 에서
   출력 폴더로 복사하게 해 뒀다 — `cpp\build.bat` 을 먼저 돌려야 원본이 존재한다

### 검증 결과 (2026-09-01, 실제 서버 대상)

```
프로세스   : x64
[OK  ] TEST-VALID     기대=true  실제=true   647ms
[OK  ] TEST-TODAY     기대=true  실제=true    39ms
[OK  ] TEST-EXPIRED   기대=false 실제=false   39ms
[OK  ] NO-SUCH-ID     기대=false 실제=false   41ms
[OK  ] (빈값)         기대=false 실제=false    0ms
전부 통과
```

- **첫 호출 647ms, 이후 39ms** — 차이는 DNS + TLS 핸드셰이크다.
  기동 시 1회 검증이면 체감되지 않지만, 명령마다 부르면 매번 왕복이 붙는다
  (`prd.md` §7 «검증 결과 캐싱» 이 여기에 걸린다)
- 빈 문자열은 **0ms** — 요청을 보내지 않고 C++ 쪽에서 바로 차단
- **UTF-8 마샬링 실증**: `한글-테스트-①` 을 등록해 조회 → `true`.
  C# → `LPUTF8Str` → C++ `JsonEscape` → JSON 본문 → PostgREST → Postgres 매칭까지
  한글·전각문자가 온전히 전달됨을 확인하고 행은 삭제했다

---

## 검증할 때 걸렸던 PowerShell 함정 (전부 실측)

이 프로젝트는 검증을 PowerShell 로 하는데, 아래 넷은 **틀린 결과를 조용히 내놓아서**
서버 문제로 오진하기 쉽다. 글로벌 `CLAUDE.md` 의 PowerShell 규칙에 더해 기억할 것.

| 함정 | 증상 | 해결 |
|---|---|---|
| `"$base?select=*"` | PS7 은 `?` 를 변수명에 허용 → `$base?select` 을 변수로 파싱해 URI 가 깨진다 | **`"${base}?select=*"`** 로 감쌀 것 |
| `@($x).Count` where `$x=$null` | `1` 이 나온다. "삭제 0건"인데 "1건"으로 보인다 | 실제 조회로 재확인하거나 `Invoke-WebRequest` + `ConvertFrom-Json` |
| `Invoke-RestMethod` 배열 | JSON 배열이 단일 객체로 잡혀 `.Count=1`, 원소가 `System.Object[]` | **`Invoke-WebRequest` + `ConvertFrom-Json`** 을 쓰면 명확하다 |
| `cmd /c "exe & echo %errorlevel%"` | `%errorlevel%` 가 **파싱 시점에** 확장돼 직전 값이 찍힌다 | PowerShell `$LASTEXITCODE` |

GUI 캡처는 DPI 때문에 좌표가 어긋난다. `SetProcessDPIAware()` 를 부르고
`MoveWindow(h, 0, 0, …)` 로 좌상단에 붙인 뒤 `GetWindowRect` 로 잡을 것.
`SendKeys` 는 포커스가 검색창에 있어야 먹으므로 **먼저 클릭**해야 한다
(폼이 뜨면 포커스가 그리드에 있다).

---

## git 산출물 취급

- `cpp/` 의 `.dll`·`.lib`·`.exe` 는 **추적 대상**이다 — `JArchLicense/` 가 dll·lib 를
  커밋해 온 관례와 동일. `.gitignore` 가 걸러 주지 않으므로 의도한 것인지 확인하고 커밋할 것
- `csharp/` 의 `bin/`·`obj/` 는 `.gitignore` 로 **무시된다** (확인 완료)
- 이 폴더의 어떤 파일에도 `service_role`·PAT 는 들어 있지 않다.
  `cpp/JArchSbLicense.cpp` 의 `anon` 키는 의도된 노출이다

---

## AutoCAD 플러그인 연결 (2026-09-02)

**`JArchLicense.dll` 이 아니라 `JArchXData.arx` 에 붙였다.** 그 DLL 은 2026-08-26 에
라이선스 체계가 arx 로 넘어가면서 배포 중단됐고, 설치 프로그램이 오히려 삭제한다
(`JArchitecture_Setup.iss` `[InstallDelete]`). 고쳐도 아무 효과가 없다.

| 파일 | 변경 |
|---|---|
| `C++/JArchXData/JArchXData.cpp` | 하드코딩 만료일(2027-04-01 XOR 은닉) **제거**. Supabase 조회로 대체 + 머신ID/등록 export 추가 |
| `C++/JArchXDataNet/Xdata.cs` | `LicenseStatus` enum, `GetMachineId()`, `Register()` 추가 |
| `Acadv25JArch/MyPlugIn.cs` | 배너에 미등록·조회실패 분기 추가. `IsLicenseValid` 를 `Usable` 기준으로 |
| `Acadv25JArch/LicenseCommands.cs` | **신규** — `JARCHID` / `JARCLICENSE` + 등록 폼 |

```
JARCHID       이 컴퓨터의 ID 를 명령창에 출력(클립보드에도 복사)
JARCLICENSE   등록창. 이미 등록된 PC 는 창을 띄우지 않는다
```

- 등록 폼은 **Designer 파일을 두지 않고 생성자에서 배치**했다. 이 저장소에서 VS Designer 가
  코드로 추가한 설정을 지운 전례가 있어, 작은 입력창은 코드로만 만든다
- 등록 성공 후 `MyPlugin.ResetLicenseCache()` 로 캐시를 버리지만, arx 쪽 확정값도 함께
  풀리므로 **AutoCAD 재시작을 안내**한다

### ⚠️ 머신ID 구현이 두 곳에 있다

`SuperBase_License/cpp/JArchSbLicense.cpp` 와 `C++/JArchXData/JArchXData.cpp` 가
같은 알고리즘을 각자 갖고 있다. **한쪽만 고치면 등록된 모든 PC 가 미등록이 된다.**
현재 두 구현이 같은 값을 낸다는 것은 ARX 소스에서 함수를 그대로 추출·컴파일해
실행 비교하는 방식으로 확인했다(둘 다 `5D07-1088-5DF0-6EF3-A203`).

---

## 다음 작업 후보

- `TEST-*` 3건 정리 — C# 관리 프로그램으로 지울 수 있다
  (SQL 로 하려면 `delete from public.licenses where com_id like 'TEST-%'`)
- `TEST-TODAY` 는 `exp_date` 가 고정이라 **하루 지나면 회귀 테스트가 실패한다.**
  실행 시점에 갱신하도록 바꾸든지, 회귀 케이스에서 빼든지 정할 것
- `prd.md` §7 나머지 — Rate limiting(자가 등록 스팸이 여기 걸린다), 검증 결과 캐싱
- `JArchSbLicense.dll` 의 위치 정리 — 지금은 `check_license` 만 쓰는 별도 경로다.
  arx 가 라이선스를 전담하므로 **이 DLL 이 계속 필요한지** 판단이 필요하다

---

## 확정 사양

| 항목 | 결정 |
|---|---|
| 네트워크 오류 시 | `false` (차단) |
| 만료일 당일 | 사용 가능 (`exp_date >= 오늘`) |
| 시간대 | Asia/Seoul |
| `com_id` 타입 | text |
| C++ HTTP | **WinHTTP** (Windows 내장, 의존성 0) — 2026-09-01 변경 |
| C++ JSON | **없음** (응답이 스칼라 boolean) |
| 타임아웃 | 연결 5초 / 읽기 5초 |
| 컴퓨터 고유 ID | `SHA256(대문자MachineGuid + "\|" + 대문자SMBIOS_UUID)` 앞 20자 hex, 4자씩 하이픈 — 2026-09-02 확정, **변경 불가** |
| 라이선스 판정 위치 | `JArchXData.arx` (2026-09-02). `JArchLicense.dll` 은 배포 중단 상태 |
| 자가 등록 만료일 | 체험 **30일**. 서버가 부여하며 클라이언트가 지정할 수 없다 (`jarch_trial_exp_date()`) |
| 재등록 | 불가. 한 com_id 는 한 번만 (PK 충돌 → `DUP`) |
| 미등록 PC | 라이선스 false + "등록된 사용자가 아닙니다" 안내 |

---

## 참조 문서
- **`prd.md`** — 전체 PRD. 위협 모델, 데이터 모델, SQL 전문, C++ 구현 코드, 미결 항목
- 관련 기존 프로젝트: `JArchLicense/` (네이티브 라이선스 DLL), `Work\Supabase\CompanyMnt` (Supabase 선례)
