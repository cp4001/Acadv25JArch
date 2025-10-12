using OpenStudio;
using System;
using System.Reflection;

namespace OpenStudioExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // 새 모델 생성
            Model model = new Model();

            Console.WriteLine("OpenStudio 모델이 생성되었습니다!");
            Console.WriteLine($"모델 UUID: {model.handle()}");
        }
    }
}