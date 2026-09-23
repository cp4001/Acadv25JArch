using Autodesk.AutoCAD.DatabaseServices;

namespace Acadv25JArch.PolyView
{
    /// <summary>
    /// 도면에서 수집한 PolyView 1개. 실제 Poly 엔티티가 존재하는 항목만 만들어진다.
    /// 이름/내용의 정본은 Poly 의 XData 이고 이 클래스는 그 스냅샷이다.
    /// </summary>
    public class PolyViewItem
    {
        public string Handle = "";   // 16진 Handle 문자열 (같은 도면 안에서만 유효)
        public string Name = "";
        public string Memo = "";
        public ObjectId Id;
    }

    /// <summary>
    /// 팔레트 트리의 그룹 노드. 도면 도형이 없는 분류용 폴더이며 NOD 에만 저장된다.
    /// Root 는 Name 이 빈 문자열인 그룹으로 표현한다.
    /// </summary>
    public class PolyViewGroup
    {
        public string Name = "";
        public List<PolyViewGroup> Groups = new();
        public List<string> Handles = new();   // 이 그룹 직속 PolyView Handle (순서 보존)
    }
}
