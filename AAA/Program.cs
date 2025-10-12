using System;
using OpenStudio;

namespace TestOpenStudio
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Model model = new Model();
                Building building = model.getBuilding();
                building.setName("테스트 건물");

                Console.WriteLine("✓ OpenStudio 정상 작동!");
                Console.WriteLine($"건물 이름: {building.nameString()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 오류: {ex.Message}");
            }

            Console.WriteLine("\n계속하려면 아무 키나 누르세요...");
            Console.ReadKey();
        }
    }
}