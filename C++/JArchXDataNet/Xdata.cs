using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace JArch
{
    /// <summary>
    /// JArchXData.arx 의 Xdata 쓰기 함수를 감싼 래퍼.
    /// AutoCAD 프로세스 안(NETLOAD 된 어셈블리)에서만 사용할 수 있다.
    /// </summary>
    public static class Xdata
    {
        private const string ArxModule = "JArchXData.arx";

        [DllImport(ArxModule, CallingConvention = CallingConvention.Cdecl,
                   CharSet = CharSet.Unicode)]
        private static extern int JArchXDataSet(IntPtr objId, string regName, string value);

        [DllImport(ArxModule, CallingConvention = CallingConvention.Cdecl,
                   CharSet = CharSet.Unicode)]
        private static extern int JArchXDataSetEnt(IntPtr pEnt, string regName, string value);

        [DllImport(ArxModule, CallingConvention = CallingConvention.Cdecl)]
        private static extern int JArchGetLicenseInfo(out SYSTEMTIME nowUtc,
                                                      out SYSTEMTIME endUtc,
                                                      out int fromInternet);

        [DllImport(ArxModule, CallingConvention = CallingConvention.Cdecl,
                   CharSet = CharSet.Unicode)]
        private static extern int JArchGetMachineId(System.Text.StringBuilder buf, int cch);

        [DllImport(ArxModule, CallingConvention = CallingConvention.Cdecl,
                   CharSet = CharSet.Unicode)]
        private static extern int JArchRegisterLicense(string userName,
                                                       string compName,
                                                       string partName,
                                                       System.Text.StringBuilder outExp,
                                                       int cch);

        // 네이티브의 최소 버퍼 크기와 같아야 한다.
        private const int MachineIdBufSize = 32;

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort Year, Month, DayOfWeek, Day, Hour, Minute, Second, Milliseconds;

            public DateTime ToUtc()
            {
                // 확정 실패 등으로 0 이 오면 최소값으로 방어.
                if (Year == 0)
                    return DateTime.MinValue;
                return new DateTime(Year, Month, Day, Hour, Minute, Second,
                                    DateTimeKind.Utc);
            }
        }

        /// <summary>라이선스 상태. 네이티브 JArchGetLicenseInfo 의 반환값과 같다.</summary>
        public enum LicenseStatus
        {
            Valid         = 0,   // 사용 가능
            Expired       = 1,   // 등록돼 있으나 사용 기한이 지남
            NotRegistered = 2,   // 등록된 사용자가 아님
            Unreachable   = 3,   // 서버 조회 실패 - 차단(fail-closed)
        }

        /// <summary>로드 시 표시할 라이선스 정보.</summary>
        public sealed class LicenseInfo
        {
            public DateTime      NowUtc;       // 확정된 현재 시각(UTC)
            public DateTime      EndDateUtc;   // 만료일(UTC) - 서버에서 옴
            public bool          FromInternet; // true = 서버 응답을 받음
            public LicenseStatus Status;

            /// <summary>사용 가능한 상태인가.</summary>
            public bool Usable => Status == LicenseStatus.Valid;

            /// <summary>사용할 수 없는 상태인가(만료·미등록·조회실패 전부 포함).</summary>
            public bool Expired => Status != LicenseStatus.Valid;

            /// <summary>보여 줄 만료일이 있는가(미등록·조회실패면 없다).</summary>
            public bool HasEndDate => Status == LicenseStatus.Valid ||
                                      Status == LicenseStatus.Expired;

            /// <summary>프롬프트 표시용 만료일 (로컬 날짜).</summary>
            public DateTime EndDate => EndDateUtc.ToLocalTime().Date;
        }

        static Xdata()
        {
            // 안전망: 어떤 경로로 처음 호출되든 .arx 로드는 보장한다.
            // (정상 흐름에서는 Startup.Initialize 가 NETLOAD 시점에 이미 로드함)
            EnsureArxLoaded();
        }

        /// <summary>
        /// 이 어셈블리와 같은 폴더의 JArchXData.arx 를 로드한다(이미 로드돼 있으면 무시).
        /// </summary>
        public static void EnsureArxLoaded()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(dir, ArxModule);
            if (File.Exists(path) && !SystemObjects.DynamicLinker.IsModuleLoaded(path))
                SystemObjects.DynamicLinker.LoadModule(path, false, false);
        }

        /// <summary>
        /// 활성 문서 명령창에 라이선스 만료일을 출력한다.
        /// 최초 호출 시 인터넷 시각을 한 번 확인한다(이후 Set 은 재사용).
        /// </summary>
        public static void ShowBanner()
        {
            LicenseInfo lic = GetInfo();

            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            Editor ed = doc.Editor;

            switch (lic.Status)
            {
                case LicenseStatus.Valid:
                    ed.WriteMessage("\n[JArch] 사용기한: {0:yyyy-MM-dd} 까지  (확인 {1:yyyy-MM-dd})",
                                    lic.EndDate, lic.NowUtc.ToLocalTime());
                    break;

                case LicenseStatus.Expired:
                    ed.WriteMessage("\n[JArch] 사용기한이 지났습니다. (만료일 {0:yyyy-MM-dd})",
                                    lic.EndDate);
                    break;

                case LicenseStatus.NotRegistered:
                    ed.WriteMessage("\n[JArch] 등록된 사용자가 아닙니다. JARCLICENSE 로 등록하세요.");
                    break;

                default:
                    ed.WriteMessage("\n[JArch] 라이선스 서버에 연결할 수 없어 사용할 수 없습니다.");
                    break;
            }
        }

        /// <summary>
        /// 엔티티에 Xdata (1001 . regName) (1000 . value) 를 기록한다.
        /// 같은 regName 의 기존 Xdata 는 교체되고, 다른 앱의 Xdata 는 유지된다.
        /// 문서 잠금이 걸린 상태(CommandMethod 안 등)에서 호출해야 한다.
        /// </summary>
        public static void Set(ObjectId id, string regName, string value)
        {
            ErrorStatus es = (ErrorStatus)JArchXDataSet(id.OldIdPtr, regName, value);
            if (es != ErrorStatus.OK)
                throw new Autodesk.AutoCAD.Runtime.Exception(es);
        }

        /// <summary>
        /// 이미 쓰기로 열려 있는 객체에 Xdata 를 기록한다. 기록 내용은 Set 과 동일
        /// (regName 그룹 + JLicense 표식, 만료 시 조용히 무시).
        /// Transaction 안에서 GetObject(ForWrite)/UpgradeOpen 으로 열어 둔 객체는
        /// ObjectId 로 다시 열 수 없으므로(eWasOpenForWrite) 이쪽을 쓴다.
        /// DB 에 아직 추가되지 않은 신규 엔티티에도 사용할 수 있다.
        /// </summary>
        public static void SetOpen(DBObject obj, string regName, string value)
        {
            ErrorStatus es = (ErrorStatus)JArchXDataSetEnt(obj.UnmanagedObject, regName, value);
            if (es != ErrorStatus.OK)
                throw new Autodesk.AutoCAD.Runtime.Exception(es);
        }

        /// <summary>
        /// 라이선스 정보를 조회한다. 최초 호출에서 인터넷 시각을 한 번 확인/확정하며,
        /// 이후 Set() 은 이 확정값을 재사용해 다시 네트워크를 타지 않는다.
        /// 보통 모듈 로드 직후 1회 호출해 프롬프트에 EndDate 를 표시하는 용도.
        /// </summary>
        public static LicenseInfo GetInfo()
        {
            int status = JArchGetLicenseInfo(out SYSTEMTIME now,
                                             out SYSTEMTIME end,
                                             out int fromNet);
            return new LicenseInfo
            {
                NowUtc       = now.ToUtc(),
                EndDateUtc   = end.ToUtc(),
                FromInternet = fromNet != 0,
                // 모르는 값이 오면 차단 쪽으로 해석한다.
                Status       = System.Enum.IsDefined(typeof(LicenseStatus), status)
                                   ? (LicenseStatus)status
                                   : LicenseStatus.Unreachable,
            };
        }

        /// <summary>
        /// 이 PC 의 고유 ID (예 <c>5D07-1088-5DF0-6EF3-A203</c>).
        /// 만들지 못하면 null.
        /// </summary>
        public static string GetMachineId()
        {
            var sb = new System.Text.StringBuilder(MachineIdBufSize);
            if (JArchGetMachineId(sb, MachineIdBufSize) == 0)
                return null;
            string s = sb.ToString();
            return s.Length == 0 ? null : s;
        }

        /// <summary>등록 결과.</summary>
        public enum RegisterResult
        {
            Registered   = 0,   // 등록됨. ExpDate 에 체험 만료일이 온다
            AlreadyExists = 1,  // 이미 등록된 PC - 추가 등록 불가
            BadId         = 2,  // ID 형식 오류
            Failed        = 3,  // 통신 실패
        }

        /// <summary>
        /// 이 PC 를 자가 등록한다. com_id 와 만료일은 클라이언트가 정하지 않는다
        /// (ID 는 하드웨어에서 계산되고, 체험 만료일은 서버가 부여한다).
        /// </summary>
        public static RegisterResult Register(string userName, string compName,
                                              string partName, out string expDate)
        {
            var sb = new System.Text.StringBuilder(32);
            int r = JArchRegisterLicense(userName ?? "", compName ?? "", partName ?? "",
                                         sb, sb.Capacity);
            expDate = sb.ToString();
            return System.Enum.IsDefined(typeof(RegisterResult), r)
                       ? (RegisterResult)r
                       : RegisterResult.Failed;
        }
    }
}
