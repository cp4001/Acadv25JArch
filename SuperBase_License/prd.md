# Supabase 라이선스 검증 시스템 PRD

- 문서 작성일: 2026-09-01
- 대상 프로젝트: Acadv25JArch / SuperBase_License
- 작성 환경: Windows 11

---

## 1. 개요

Supabase(PostgreSQL 기반)에 라이선스 관리 테이블을 두고,
C++ 클라이언트가 `comID`로 사용 기한 유효성만 조회하는 시스템.

| 구분 | 언어 | 역할 |
|---|---|---|
| 검증 클라이언트 | C++ | comID 전달 → true/false 수신 (조회 전용) |
| 관리 프로그램 | C# | 테이블 CRUD (별도 작업, 본 문서 범위 외) |
| 백엔드 | Supabase | PostgreSQL + PostgREST |

---

## 2. 기술 선택 배경

### 2.1 Supabase 선택 이유
- 테이블 생성 시 REST 엔드포인트가 자동 생성되어 백엔드 코드 불필요
- 인증, RLS(Row Level Security), Storage 기본 내장
- Vercel + Neon 조합은 API 계층을 직접 구현해야 하므로 관리 대상 코드베이스가 늘어남

### 2.2 Supabase 제약사항 (인지 사항)
- 무료 티어는 1주 미사용 시 프로젝트 일시정지
- 복잡한 비즈니스 로직은 Edge Function(Deno/TypeScript) 필요
- 서비스 결합도가 높아 이탈 시 마이그레이션 부담

### 2.3 C++ 접근 경로
REST API(PostgREST) 방식 채택. libpq 직결 방식은 RLS/인증 활용이 불가하고
방화벽에서 5432/6543 포트가 막힐 수 있어 제외.

---

## 3. 보안 설계

### 3.1 위협 인식
C++ 네이티브 바이너리라도 다음 방법으로 키가 노출됨:

| 공격 방법 | 설명 |
|---|---|
| 문자열 추출 | `strings.exe`로 URL·JWT(`eyJ`로 시작) 노출 |
| HTTPS 가로채기 | Fiddler/mitmproxy + 로컬 인증서로 헤더 평문 확인 |
| 메모리 덤프 | 난독화해도 사용 시점에 평문으로 메모리 상주 |
| API 후킹 | `WinHttpSendRequest` 등 인자 로깅 |

난독화·안티디버깅은 시간을 벌 뿐이며, **클라이언트의 비밀은 반드시 유출된다**는
전제로 설계한다.

### 3.2 대응 원칙
1. **service_role 키는 클라이언트에 절대 포함하지 않는다.**
   유출 시 RLS를 전면 우회하여 DB 전체가 읽기·쓰기 가능해짐.
2. **anon 키는 노출 전제 키**이며, 반드시 RLS와 함께 사용한다.
3. **테이블 직접 조회를 전면 차단**하고 RPC 함수 하나만 노출한다.
   → comID 목록, 회사명, 부서명이 외부로 나가지 않음.
4. 클라이언트는 boolean 결과만 수신한다.

### 3.3 주의사항
- RLS는 테이블마다 개별 활성화 필요 (신규 테이블 기본값은 비활성)
- 뷰(view)는 `security_invoker` 옵션이 없으면 RLS를 우회함

---

## 4. 데이터 모델

### 4.1 테이블: `public.licenses`

| 컬럼 | 타입 | 설명 | 비고 |
|---|---|---|---|
| com_id | text | 라이선스 식별자 | PK |
| exp_date | date | 사용 기한 | NOT NULL |
| reg_date | date | 최초 등록일 | 기본값 = 오늘(Asia/Seoul) |
| user_name | text | 사용자 이름 | |
| comp_name | text | 회사명 | |
| part_name | text | 부서명 | |

컬럼명은 PostgreSQL 관례에 따라 snake_case 사용.

---

## 5. 확정 사양

| 항목 | 결정 |
|---|---|
| 네트워크 오류 시 | `false` 반환 (차단) |
| 만료일 당일 | 사용 가능 (`exp_date >= 오늘`) |
| 날짜 기준 시간대 | Asia/Seoul |
| com_id 타입 | text (문자열) |
| C++ HTTP 라이브러리 | **WinHTTP** (Windows 내장) — 2026-09-01 변경, 아래 §5.2 |
| JSON 라이브러리 | **없음** — 요청 본문은 직접 조립, 응답은 스칼라 boolean |

### 5.1 반환 규칙

| 상황 | 반환값 |
|---|---|
| com_id 미등록 | false |
| exp_date < 오늘 | false |
| exp_date >= 오늘 | true |
| HTTP 상태 != 200 | false |
| 네트워크 타임아웃 / 예외 | false |

타임아웃: 연결 5초, 읽기 5초.

### 5.2 HTTP 라이브러리 변경 (cpp-httplib → WinHTTP)

당초 cpp-httplib\[openssl\] + nlohmann/json 을 확정했으나 구현 시점에 아래 이유로 WinHTTP 로 바꿨다.

| 근거 | 내용 |
|---|---|
| vcpkg 미설치 | 이 PC 에 vcpkg 가 없다. 도입하면 OpenSSL 빌드까지 수 GB·수십 분이 선행된다 |
| 배포 부담 | cpp-httplib\[openssl\] 은 `libssl-3-x64.dll`·`libcrypto-3-x64.dll` 을 동반한다. AutoCAD 에 로드되는 DLL 이라 배포물이 늘수록 부담이 크다 |
| 기존 관례 | `JArchLicense/build.bat` 은 Developer Command Prompt 에서 `cl` 만 쓰고 시스템 라이브러리만 링크한다. 같은 방식을 유지 |
| JSON 불필요 | 응답이 스칼라 `true`/`false` 한 줄이라 파서가 필요 없다. 요청 본문도 키 1개다 |
| TLS | WinHTTP 는 Schannel + OS 인증서 저장소를 쓴다. 인증서 갱신을 OS 가 맡으므로 유지보수가 없다 |

WinHTTP 는 Windows 기본 탑재라 **추가 의존성이 0** 이고, 사내 프록시 환경에서도
`WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY` 로 시스템 프록시 설정을 그대로 따른다.

---

## 6. 구현 명세

### 6.1 1단계 — DB (Supabase SQL Editor에서 실행)

```sql
-- 테이블
create table public.licenses (
    com_id     text primary key,
    exp_date   date not null,
    reg_date   date not null default (now() at time zone 'Asia/Seoul')::date,
    user_name  text,
    comp_name  text,
    part_name  text
);

-- RLS 활성화 (정책 없음 = anon 완전 차단)
alter table public.licenses enable row level security;

-- 검증 함수
create or replace function public.check_license(p_com_id text)
returns boolean
language plpgsql
security definer
set search_path = public
as $$
declare
    v_exp date;
begin
    select exp_date into v_exp
    from public.licenses
    where com_id = p_com_id;

    if not found then
        return false;
    end if;

    return v_exp >= (now() at time zone 'Asia/Seoul')::date;
end;
$$;

-- 권한: 함수 실행만 허용
revoke all on function public.check_license(text) from public;
grant execute on function public.check_license(text) to anon, authenticated;
```

`security definer` 덕분에 함수 내부는 소유자 권한으로 실행되어 RLS를 통과하고,
외부에는 boolean 하나만 노출된다. 테이블 직접 조회는 anon 정책이 없어 0건 반환.

### 6.2 2단계 — C++ 조회 프로그램

**구현 완료 (2026-09-01) — 실제 코드는 `cpp/` 폴더가 정본이다.** 아래 코드 블록은 초안이며
WinHTTP 로 대체됐다(§5.2). 파일 구성과 빌드·검증 결과는 `CLAUDE.md` 참조.

| 파일 | 역할 |
|---|---|
| `cpp/JArchSbLicense.h` / `.cpp` / `.def` | `extern "C" int CheckLicenseOnline(const char*)` — 1=사용 가능, 0=차단 |
| `cpp/test_client.cpp` | 회귀 검증용 콘솔. 인자 없이 실행하면 `TEST-*` 5건 자동 검증 |
| `cpp/build.bat` | `cl` 로 DLL + exe 빌드. 의존성 설치 불필요 |

호출 규격:

| 항목 | 값 |
|---|---|
| 메서드 | POST |
| 경로 | `/rest/v1/rpc/check_license` |
| 헤더 | `apikey`, `Authorization: Bearer <anon>`, `Content-Type: application/json` |
| 본문 | `{"p_com_id":"ABC-1234"}` |
| 응답 | `true` 또는 `false` (스칼라 boolean) |

구현 코드 (초안 — 채택되지 않음, §5.2 참조):

```cpp
#define CPPHTTPLIB_OPENSSL_SUPPORT
#include <httplib.h>
#include <nlohmann/json.hpp>
#include <string>

namespace {
    const char* SUPABASE_HOST     = "https://xxxxxxxx.supabase.co";
    const char* SUPABASE_ANON_KEY = "eyJhbGciOi...";  // anon key
}

bool CheckLicense(const std::string& comID)
{
    try {
        httplib::Client cli(SUPABASE_HOST);
        cli.set_connection_timeout(5, 0);
        cli.set_read_timeout(5, 0);

        httplib::Headers headers = {
            { "apikey",        SUPABASE_ANON_KEY },
            { "Authorization", std::string("Bearer ") + SUPABASE_ANON_KEY },
            { "Content-Type",  "application/json" }
        };

        nlohmann::json body;
        body["p_com_id"] = comID;

        auto res = cli.Post("/rest/v1/rpc/check_license",
                            headers, body.dump(), "application/json");

        if (!res || res->status != 200)
            return false;                     // 네트워크/서버 오류 → 차단

        auto j = nlohmann::json::parse(res->body, nullptr, false);
        if (j.is_discarded() || !j.is_boolean())
            return false;

        return j.get<bool>();
    }
    catch (...) {
        return false;                         // 예외도 차단
    }
}
```

호출 예:

```cpp
if (CheckLicense("ABC-1234")) {
    // 정상 실행
} else {
    // 라이선스 만료 또는 미등록
}
```

### 6.3 3단계 — C# 관리 프로그램

**구현 완료 (2026-09-01) — `csharp/` 폴더.** .NET 8 WinForms, `licenses` CRUD 전부.

| 항목 | 결정 |
|---|---|
| 접근 권한 | **`service_role`** — 관리자 PC 전용, 배포하지 않음 |
| 키 보관 | 환경변수 `JARCH_LICENSE_SERVICE_KEY` (저장소가 커밋되므로 소스에 넣지 않음) |
| API | PostgREST `/rest/v1/licenses` (GET / POST / PATCH / DELETE) |
| RLS 정책 | **추가하지 않았다** — service_role 이 우회하므로 불필요.
`licenses` 는 정책 0건을 유지해 anon 차단이 그대로 살아 있다 |

`service_role` 키는 RLS 를 전면 우회한다(§3.2 1번). 이 프로그램을 배포하면
DB 전체가 읽기·쓰기 가능해지므로 **관리자 PC 밖으로 내보내지 않는다.**

빌드·검증 결과와 설계 메모는 `CLAUDE.md` «C# 관리 프로그램» 참조.

---

## 7. 미결 / 향후 검토 항목

- [x] 실제 프로젝트 URL·anon 키를 넣고 응답이 스칼라 `true`/`false`로 오는지 검증
      → 2026-09-01 확인 완료. HTTP 200 + 스칼라 boolean, §5.1 반환 규칙 전부 일치.
      결과 표는 `CLAUDE.md` «REST 엔드투엔드 검증 결과»
- [ ] 만료 사유 구분(미등록 vs 기간초과)이 필요할 경우 반환 타입을 int 또는 json으로 변경
- [ ] Rate limiting 설정 (키 유출 시 요금 폭탄 방지)
- [ ] 검증 결과 캐싱 정책 (매 실행마다 조회할지, 일정 시간 캐시할지)
- [ ] 오프라인 유예 기간(grace period) 도입 여부

## 8. 언급만 해둔 대안 (미채택)

- 자체 인증 프록시 서버를 두고 Supabase 키는 서버에만 보관
- 라이선스 서버에서 실행 시 단기 토큰 발급
- 코드 서명 + 무결성 검증
- Supabase 셀프 호스팅 (Docker Compose 기반, 클라이언트 배포용으로는 부적합)
- Neon + PostgREST 자체 호스팅
- libpq 직결 (libpqxx)
