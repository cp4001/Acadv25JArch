//
// FcdXData.arx - AutoCAD 2025 (ObjectARX 2025)
//
// SETFCD 명령 : 선택한 엔티티의 Xdata 에
//               RegApp 이름 "Group", 문자열 값 "FCD" 를 기록한다.
//
//   Xdata 구조 : (1001 . "Group") (1000 . "FCD")
//

#include <rxregsvc.h>
#include <aced.h>
#include <dbmain.h>
#include <dbents.h>
#include <adslib.h>
#include <tchar.h>

static const ACHAR* kCmdGroup = _T("JARCH_XDATA");
static const ACHAR* kAppName  = _T("Group");
static const ACHAR* kXdValue  = _T("FCD");

// 엔티티 하나에 (1001 . "Group") (1000 . "FCD") 를 기록한다.
// setXData 는 같은 RegApp 이름의 기존 Xdata 만 교체하므로
// 다른 애플리케이션의 Xdata 는 그대로 남는다.
static bool writeFcdXData(const ads_name en)
{
    AcDbObjectId id;
    if (acdbGetObjectId(id, en) != Acad::eOk)
        return false;

    AcDbEntity* pEnt = NULL;
    if (acdbOpenObject(pEnt, id, AcDb::kForWrite) != Acad::eOk)
        return false;

    struct resbuf* pRb = acutBuildList(
        (int)AcDb::kDxfRegAppName,    // 1001
        kAppName,
        (int)AcDb::kDxfXdAsciiString, // 1000
        kXdValue,
        RTNONE);

    Acad::ErrorStatus es = Acad::eOutOfMemory;
    if (pRb != NULL) {
        es = pEnt->setXData(pRb);
        acutRelRb(pRb);
    }
    pEnt->close();

    return (es == Acad::eOk);
}

static void setFcd()
{
    // Xdata 를 붙이려면 RegApp 테이블에 이름이 등록돼 있어야 한다.
    // 이미 등록돼 있으면 eDuplicateKey 가 반환되며 그대로 진행하면 된다.
    acdbRegApp(kAppName);

    ads_name ss;
    if (acedSSGet(NULL, NULL, NULL, NULL, ss) != RTNORM) {
        acutPrintf(_T("\n선택된 객체가 없습니다."));
        return;
    }

    Adesk::Int32 len = 0;
    acedSSLength(ss, &len);

    int done = 0;
    for (Adesk::Int32 i = 0; i < len; i++) {
        ads_name en;
        if (acedSSName(ss, (int)i, en) != RTNORM)
            continue;
        if (writeFcdXData(en))
            done++;
    }
    acedSSFree(ss);

    acutPrintf(_T("\n%d개 객체에 Xdata (\"%s\" = \"%s\") 를 기록했습니다."),
               done, kAppName, kXdValue);
}

static void initApp()
{
    acedRegCmds->addCommand(kCmdGroup,
        _T("SETFCD"), _T("SETFCD"), ACRX_CMD_MODAL, setFcd);
}

static void unloadApp()
{
    acedRegCmds->removeGroup(kCmdGroup);
}

extern "C"
AcRx::AppRetCode acrxEntryPoint(AcRx::AppMsgCode msg, void* appId)
{
    switch (msg) {
    case AcRx::kInitAppMsg:
        acrxDynamicLinker->unlockApplication(appId);
        acrxDynamicLinker->registerAppMDIAware(appId);
        initApp();
        break;
    case AcRx::kUnloadAppMsg:
        unloadApp();
        break;
    default:
        break;
    }
    return AcRx::kRetOK;
}
