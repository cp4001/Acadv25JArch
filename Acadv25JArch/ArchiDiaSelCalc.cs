using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiaSelCalc
{
    public class PipeLookup
    {
        // 15A 테이블 조회 함수
        public static double? A15GetValue(int input)
        {
            var table = new Dictionary<int, double>
        {
            { 15, 1.00 },
            { 20, 2.60 },
            { 25, 4.90 },
            { 32, 9.20 },
            { 40, 14.50 },
            { 50, 30.00 },
            { 65, 53.00 },
            { 80, 84.60 },
            { 100, 178.00 },
            { 125, 280.00 },
            { 150, 452.00 }
        };

            return table.ContainsKey(input) ? table[input] : null;
        }

        // 동시사용량 입력 → 관경 반환 함수
        public static double? SameUsedDiaValue(double input)
        {
            var table = new Dictionary<double, int>
        {
            { 0, 15 },
            { 1.01, 20 },
            { 2.61, 25 },
            { 4.91, 32 },
            { 9.21, 40 },
            { 14.51, 50 },
            { 30.01, 65 },
            { 53.01, 80 },
            { 84.61, 100 },
            { 178.01, 125 },
            { 280.01, 150 }
        };

            // 빈 값 처리
            if (input <= 0)
                return null;

            // input 이하의 최대 키 찾기
            var key = table.Keys
                .Where(k => k <= input)
                .OrderByDescending(k => k)
                .FirstOrDefault();

            // 키가 없으면 null 반환
            if (key == 0 && !table.ContainsKey(key))
                return null;

            return table[key]*0.01;
        }

        // 대변기 값 조회 함수
        public static double? GetToiletValue(int fixtureCount)
        {
            var table = new Dictionary<int, double>
        {
            { 1, 100 }, { 2, 100 }, { 3, 82.50 }, { 4, 65.00 }, { 5, 60.00 },
            { 6, 55.00 }, { 7, 50.00 }, { 8, 45.00 }, { 9, 43.75 }, { 10, 42.50 },
            { 11, 41.25 }, { 12, 40.00 }, { 13, 38.75 }, { 14, 37.50 }, { 15, 36.25 },
            { 16, 35.00 }, { 17, 33.75 }, { 18, 32.50 }, { 19, 31.25 }, { 20, 30.00 },
            { 21, 28.75 }, { 22, 27.50 }, { 23, 26.25 }, { 24, 25.00 }, { 32, 19.00 },
            { 40, 17.00 }, { 50, 15.00 }, { 70, 12.00 }, { 100, 10.00 }
        };

            return table.ContainsKey(fixtureCount) ? table[fixtureCount] : null;
        }

        // 일반기구 값 조회 함수
        public static double? GetGeneralValue(int fixtureCount)
        {
            var table = new Dictionary<int, double>
        {
            { 1, 100 }, { 2, 100 }, { 3, 90.00 }, { 4, 80.00 }, { 5, 77.50 },
            { 6, 75.00 }, { 7, 72.50 }, { 8, 70.00 }, { 9, 66.25 }, { 10, 62.50 },
            { 11, 58.75 }, { 12, 55.00 }, { 13, 53.75 }, { 14, 52.50 }, { 15, 51.25 },
            { 16, 50.00 }, { 17, 49.75 }, { 18, 49.50 }, { 19, 49.25 }, { 20, 49.00 },
            { 21, 48.75 }, { 22, 48.50 }, { 23, 48.25 }, { 24, 48.00 }, { 32, 45.00 },
            { 40, 40.00 }, { 50, 38.00 }, { 70, 35.00 }, { 100, 33.00 }
        };

            return table.ContainsKey(fixtureCount) ? table[fixtureCount] : null;
        }

        //접속관경 과 개수에 따른 결정 관경 

        public static double? GetResultDia(int connDia, int connCount)
        {
            var a15= A15GetValue(connDia);
            double asum = a15.HasValue ? a15.Value * connCount : 0; 
            double ssValue = GetToiletValue(connCount) ?? 0 *0.01; // 대변기 동시 사용율    
            double? sameused = asum * ssValue;

            double? resultValue = SameUsedDiaValue(ssValue) ?? 0;
           
            //double? resultValue = GetToiletValue(sameused ?? 0);

            return resultValue;
        }


        //// 사용 예시
        //public static void Main()
        //{
        //    Console.WriteLine(A15GetValue(25));  // 출력: 4.9
        //}





    }


    public class Pipe15AValueLookup  // 관경별 15A 환산값 조회 클래스
    {
        // 관경 → 값 테이블
        private static Dictionary<int, double> valueTable = new Dictionary<int, double>
    {
        { 15, 1.00 },
        { 20, 2.60 },
        { 25, 4.90 },
        { 32, 9.20 },
        { 40, 14.50 },
        { 50, 30.00 },
        { 65, 53.00 },
        { 80, 84.60 },
        { 100, 178.00 },
        { 125, 280.00 },
        { 150, 452.00 }
    };

        // 값 조회
        public static double? GetValue(int input)
        {
            if (valueTable.ContainsKey(input))
                return valueTable[input];
            return null;
        }

        // 사용 예시
        public static void Main()
        {
            int input = 25;
            double? result = GetValue(input);

            Console.WriteLine($"입력: {input}");
            Console.WriteLine($"출력: {result}");  // 출력: 4.9
        }
    }

    public class PipeSametimeCountLookup  // 동시 사용개수별 관경 조회 클래스
    {
        // 관경 조회 테이블 (기준값 → 관경)
        private static Dictionary<double, int> pipeTable = new Dictionary<double, int>
    {
        { 0, 15 },
        { 1.01, 20 },
        { 2.61, 25 },
        { 4.91, 32 },
        { 9.21, 40 },
        { 14.51, 50 },
        { 30.01, 65 },
        { 53.01, 80 },
        { 84.61, 100 },
        { 178.01, 125 },
        { 280.01, 150 }
    };

        // LOOKUP 함수 구현
        public static int? Lookup(double value)
        {
            // 빈 값 처리
            if (value <= 0)
                return null;

            // value 이하의 최대 키 찾기
            var key = pipeTable.Keys
                .Where(k => k <= value)
                .OrderByDescending(k => k)
                .FirstOrDefault();

            // 키가 없으면 null 반환
            if (key == 0 && !pipeTable.ContainsKey(key))
                return null;

            return pipeTable[key];
        }

        // 사용 예시
        public static void Main()
        {
            double d17 = 12.2;
            int? result = Lookup(d17);

            Console.WriteLine($"D17 = {d17}");
            Console.WriteLine($"관경 = {result}"); // 출력: 40
        }
    }

    public class FixtureValueLookup
    {
        // 대변기(세정밸브) 테이블
        private static Dictionary<int, double> toiletTable = new Dictionary<int, double>
    {
        { 1, 100 }, { 2, 100 }, { 3, 82.50 }, { 4, 65.00 }, { 5, 60.00 },
        { 6, 55.00 }, { 7, 50.00 }, { 8, 45.00 }, { 9, 43.75 }, { 10, 42.50 },
        { 11, 41.25 }, { 12, 40.00 }, { 13, 38.75 }, { 14, 37.50 }, { 15, 36.25 },
        { 16, 35.00 }, { 17, 33.75 }, { 18, 32.50 }, { 19, 31.25 }, { 20, 30.00 },
        { 21, 28.75 }, { 22, 27.50 }, { 23, 26.25 }, { 24, 25.00 }, { 32, 19.00 },
        { 40, 17.00 }, { 50, 15.00 }, { 70, 12.00 }, { 100, 10.00 }
    };

        // 일반기구 테이블
        private static Dictionary<int, double> generalTable = new Dictionary<int, double>
    {
        { 1, 100 }, { 2, 100 }, { 3, 90.00 }, { 4, 80.00 }, { 5, 77.50 },
        { 6, 75.00 }, { 7, 72.50 }, { 8, 70.00 }, { 9, 66.25 }, { 10, 62.50 },
        { 11, 58.75 }, { 12, 55.00 }, { 13, 53.75 }, { 14, 52.50 }, { 15, 51.25 },
        { 16, 50.00 }, { 17, 49.75 }, { 18, 49.50 }, { 19, 49.25 }, { 20, 49.00 },
        { 21, 48.75 }, { 22, 48.50 }, { 23, 48.25 }, { 24, 48.00 }, { 32, 45.00 },
        { 40, 40.00 }, { 50, 38.00 }, { 70, 35.00 }, { 100, 33.00 }
    };

        // 대변기 값 조회
        public static double? GetToiletValue(int fixtureCount)
        {
            if (toiletTable.ContainsKey(fixtureCount))
                return toiletTable[fixtureCount];
            return null;
        }

        // 일반기구 값 조회
        public static double? GetGeneralValue(int fixtureCount)
        {
            if (generalTable.ContainsKey(fixtureCount))
                return generalTable[fixtureCount];
            return null;
        }

        // 사용 예시
        public static void Main()
        {
            int fixtureCount = 3;

            double? toiletValue = GetToiletValue(fixtureCount);
            double? generalValue = GetGeneralValue(fixtureCount);

            Console.WriteLine($"기구수: {fixtureCount}");
            Console.WriteLine($"대변기: {toiletValue}");      // 출력: 82.5
            Console.WriteLine($"일반기구: {generalValue}");    // 출력: 90
        }
    }

}
