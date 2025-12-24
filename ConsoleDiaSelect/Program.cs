using LiteDB;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace DiaSelect
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

            return table[key] ;
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
            var a15 = A15GetValue(connDia);
            double asum = a15.HasValue ? a15.Value * connCount : 0;
            double ssValue = (GetToiletValue(connCount) ?? 0 )* 0.01; // 대변기 동시 사용율    
            double sameused = asum * ssValue;

            double? resultValue = SameUsedDiaValue(sameused) ?? 0;

            //double? resultValue = GetToiletValue(sameused ?? 0);

            return resultValue;
        }


        //// 사용 예시
        //public static void Main()
        //{
        //    Console.WriteLine(A15GetValue(25));  // 출력: 4.9
        //}





    }

    // 1. 모델 클래스 (필드 3개)
    public class PipeSizeData
    {
        public int Id { get; set; }
        public double FlowMin { get; set; }      // 유량 범위 시작
        public int PipeDiameter { get; set; }     // 관경(mm)
        public double MaxFlow { get; set; }       // 최고유량(LPM)
    }

    // 2. Repository 클래스
    public class PipeSizeRepository : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<PipeSizeData> _collection;

        public PipeSizeRepository(string dbPath = "pipesize.db")
        {
            _db = new LiteDatabase(dbPath);
            _collection = _db.GetCollection<PipeSizeData>("pipesizes");
            _collection.EnsureIndex(x => x.PipeDiameter);
        }

        // 초기 데이터 저장
        public void InitializeData()
        {
            if (_collection.Count() > 0) return;

            var data = new List<PipeSizeData>
            {
                new PipeSizeData { FlowMin = 0, PipeDiameter = 15, MaxFlow = 8.3 },
                new PipeSizeData { FlowMin = 8.4, PipeDiameter = 20, MaxFlow = 17.1 },
                new PipeSizeData { FlowMin = 17.2, PipeDiameter = 25, MaxFlow = 31.7 },
                new PipeSizeData { FlowMin = 31.8, PipeDiameter = 32, MaxFlow = 59.0 },
                new PipeSizeData { FlowMin = 59.1, PipeDiameter = 40, MaxFlow = 86.0 },
                new PipeSizeData { FlowMin = 86.1, PipeDiameter = 50, MaxFlow = 158.0 },
                new PipeSizeData { FlowMin = 158.1, PipeDiameter = 65, MaxFlow = 290.0 },
                new PipeSizeData { FlowMin = 290.1, PipeDiameter = 80, MaxFlow = 440.0 },
                new PipeSizeData { FlowMin = 440.1, PipeDiameter = 100, MaxFlow = 860.0 },
                new PipeSizeData { FlowMin = 860.1, PipeDiameter = 125, MaxFlow = 1440.0 },
                new PipeSizeData { FlowMin = 1440.1, PipeDiameter = 150, MaxFlow = 2220.0 },
                new PipeSizeData { FlowMin = 2220.1, PipeDiameter = 200, MaxFlow = 4420.0 },
                new PipeSizeData { FlowMin = 4420.1, PipeDiameter = 250, MaxFlow = 7600.0 },
                new PipeSizeData { FlowMin = 7600.1, PipeDiameter = 300, MaxFlow = 12000.0 },
                new PipeSizeData { FlowMin = 12000.1, PipeDiameter = 350, MaxFlow = 15600.0 }
            };

            _collection.InsertBulk(data);
        }

        // 유량 입력 → 관경 출력
        public int? GetPipeDiameter(double flow, bool useInterpolation = false)
        {
            if (flow < 0) return null;

            var allData = _collection.FindAll().OrderBy(x => x.FlowMin).ToList();
            if (allData.Count == 0) return null;

            if (!useInterpolation)
            {
                // 정확히 일치: 범위 내에 있는 관경 반환
                for (int i = 0; i < allData.Count; i++)
                {
                    double rangeStart = allData[i].FlowMin;
                    double rangeEnd = (i < allData.Count - 1) ? allData[i + 1].FlowMin : double.MaxValue;

                    if (flow >= rangeStart && flow < rangeEnd)
                    {
                        return allData[i].PipeDiameter;
                    }
                }
                return null; // 범위 초과
            }
            else
            {
                // 선형 보간
                // 범위 초과 체크
                if (flow < allData.First().FlowMin)
                    return null;

                // 정확히 범위 내에 있으면 해당 관경 반환
                for (int i = 0; i < allData.Count; i++)
                {
                    double rangeStart = allData[i].FlowMin;
                    double rangeEnd = (i < allData.Count - 1) ? allData[i + 1].FlowMin : double.MaxValue;

                    if (flow >= rangeStart && flow < rangeEnd)
                    {
                        // 마지막 범위면 보간 불가
                        if (i == allData.Count - 1)
                            return allData[i].PipeDiameter;

                        // 선형 보간
                        var lower = allData[i];
                        var upper = allData[i + 1];

                        double ratio = (flow - lower.FlowMin) / (upper.FlowMin - lower.FlowMin);
                        double interpolatedDiameter = lower.PipeDiameter + ratio * (upper.PipeDiameter - lower.PipeDiameter);
                        return (int)Math.Round(interpolatedDiameter);
                    }
                }
                return null;
            }
        }

        // 관경 입력 → 최고유량 출력
        public double? GetMaxFlow(int diameter, bool useInterpolation = false)
        {
            if (diameter < 0) return null;

            var allData = _collection.FindAll().OrderBy(x => x.PipeDiameter).ToList();
            if (allData.Count == 0) return null;

            if (!useInterpolation)
            {
                // 정확히 일치하는 관경
                var match = allData.FirstOrDefault(x => x.PipeDiameter == diameter);
                return match?.MaxFlow;
            }
            else
            {
                // 선형 보간
                var exactMatch = allData.FirstOrDefault(x => x.PipeDiameter == diameter);
                if (exactMatch != null)
                    return exactMatch.MaxFlow;

                // 범위 초과 체크
                if (diameter < allData.First().PipeDiameter || diameter > allData.Last().PipeDiameter)
                    return null;

                // 인접한 두 데이터 찾기
                PipeSizeData lower = null, upper = null;
                for (int i = 0; i < allData.Count - 1; i++)
                {
                    if (diameter > allData[i].PipeDiameter && diameter < allData[i + 1].PipeDiameter)
                    {
                        lower = allData[i];
                        upper = allData[i + 1];
                        break;
                    }
                }

                if (lower == null || upper == null) return null;

                // 선형 보간
                double ratio = (double)(diameter - lower.PipeDiameter) / (upper.PipeDiameter - lower.PipeDiameter);
                double interpolatedFlow = lower.MaxFlow + ratio * (upper.MaxFlow - lower.MaxFlow);
                return Math.Round(interpolatedFlow, 1);
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Pipe Dia Select");
            //Console.WriteLine($"{PipeLookup.GetResultDia(25,3).ToString()}");

            Console.WriteLine("Pipe Dia Select");

            // 기준 Dia 입력
            Console.Write("기준 Dia 입력: ");
            int baseDia = int.Parse(Console.ReadLine());

            // 입력개수 입력
            Console.Write("입력개수: ");
            int count = int.Parse(Console.ReadLine());

            // 결과 계산 및 표시
            var result = PipeLookup.GetResultDia(baseDia, count);
            Console.WriteLine($"결과: {result}");

        }
    }

    class Program1 
    {
        static void Main(string[] args)
        {
            using (var repo = new PipeSizeRepository())
            {
                // 초기 데이터 저장
                repo.InitializeData();

                // 테스트 1: 유량으로 관경 찾기 (정확히 일치)
                Console.WriteLine("=== 유량 → 관경 (정확히 일치) ===");
                Console.WriteLine($"50 LPM → {repo.GetPipeDiameter(50)} mm");
                Console.WriteLine($"100 LPM → {repo.GetPipeDiameter(100)} mm");
                Console.WriteLine($"20000 LPM → {repo.GetPipeDiameter(20000)} mm (범위 초과)");

                // 테스트 2: 유량으로 관경 찾기 (보간)
                Console.WriteLine("\n=== 유량 → 관경 (보간) ===");
                Console.WriteLine($"50 LPM → {repo.GetPipeDiameter(50, true)} mm");
                Console.WriteLine($"100 LPM → {repo.GetPipeDiameter(100, true)} mm");

                // 테스트 3: 관경으로 유량 찾기 (정확히 일치)
                Console.WriteLine("\n=== 관경 → 유량 (정확히 일치) ===");
                Console.WriteLine($"50 mm → {repo.GetMaxFlow(50)} LPM");
                Console.WriteLine($"75 mm → {repo.GetMaxFlow(75)} LPM (없음)");

                // 테스트 4: 관경으로 유량 찾기 (보간)
                Console.WriteLine("\n=== 관경 → 유량 (보간) ===");
                Console.WriteLine($"50 mm → {repo.GetMaxFlow(50, true)} LPM");
                Console.WriteLine($"75 mm → {repo.GetMaxFlow(75, true)} LPM");
            }
        }
    }
}