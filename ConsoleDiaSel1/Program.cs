using System;
using System.Collections.Generic;
using System.Linq;

namespace PipeSizingSystem
{
    // 1. 기구 정보 (세정밸브 여부 속성 추가)
    public class FixtureType
    {
        public string Name { get; set; }
        public double UnitFactor { get; set; }
        public int MinPipeSize { get; set; }
        public bool IsFlushValve { get; set; } // 대변기 구분용

        public FixtureType(string name, double factor, int minSize, bool isFV = false)
        {
            Name = name;
            UnitFactor = factor;
            MinPipeSize = minSize;
            IsFlushValve = isFV;
        }
    }

    public class FixtureInput
    {
        public FixtureType Fixture { get; set; }
        public int Quantity { get; set; }
        public double TotalLoadUnits => Fixture.UnitFactor * Quantity;
    }

    // 2. 계산 엔진
    public class PipeSizingEngine
    {
        // 엑셀 내부의 실제 관경 선정표 (Column S 값 반영)
        // Key: 허용 가능한 최대 부하단위, Value: 관경(A)
        private readonly Dictionary<double, int> _realCapacityTable = new Dictionary<double, int>()
        {
            { 1.0, 15 },
            { 2.6, 20 },
            { 4.9, 25 },
            { 9.2, 32 },
            { 14.5, 40 }, // 40A는 14.5까지만 커버 (이것 때문에 28.42는 50A가 됨)
            { 30.0, 50 },
            { 53.0, 65 },
            { 84.6, 80 },
            { 178.0, 100 },
            { 280.0, 125 },
            { 452.0, 150 }
        };

        // 대변기(세정밸브) 전용 동시사용률 표 (Row 17)
        private double GetFlushValveSimultaneity(int qty)
        {
            if (qty <= 0) return 0;
            if (qty <= 2) return 1.0;
            if (qty == 3) return 0.825; // 3개일 때 82.5%
            if (qty == 4) return 0.65;
            if (qty == 5) return 0.60;
            if (qty <= 7) return 0.50; // 근사치
            if (qty <= 10) return 0.43;
            return 0.40; // 10개 초과 시 단순화
        }

        // 일반기구 전용 동시사용률 표 (Row 18)
        // 엑셀 특이사항: '부하단위 합계'를 '기구수(Row 16)' 구간에서 찾음
        private double GetGeneralSimultaneity(double loadSum)
        {
            if (loadSum <= 0) return 0;
            // 엑셀 Lookup Logic (Approximate match)
            if (loadSum < 2) return 1.0;
            if (loadSum < 3) return 1.0;
            if (loadSum < 4) return 0.90;
            if (loadSum < 5) return 0.80;
            if (loadSum < 10) return 0.70; // 5~9구간 근사
            if (loadSum < 20) return 0.60;
            if (loadSum < 32) return 0.48; // 24~31
            if (loadSum < 40) return 0.45; // 32~39 (36.2는 여기에 해당 -> 0.45)
            if (loadSum < 50) return 0.40;
            return 0.35;
        }

        // 관경 결정 함수
        private int DetermineSize(double effectiveLoad, int minConnSize)
        {
            if (effectiveLoad <= 0) return 0;
            int selectedSize = 0;
            foreach (var limit in _realCapacityTable.OrderBy(k => k.Key))
            {
                if (effectiveLoad <= limit.Key)
                {
                    selectedSize = limit.Value;
                    break;
                }
            }
            if (selectedSize == 0) selectedSize = _realCapacityTable.Values.Last();
            return (selectedSize < minConnSize) ? minConnSize : selectedSize;
        }

        public void Calculate(List<FixtureInput> inputs)
        {
            // 1. 그룹 분리
            var flushValves = inputs.Where(i => i.Fixture.IsFlushValve).ToList();
            var generalFixtures = inputs.Where(i => !i.Fixture.IsFlushValve).ToList();

            Console.WriteLine("===============================================================");
            Console.WriteLine("                급수 관경 결정 계산서 (최종 수정)              ");
            Console.WriteLine("===============================================================");

            // --- [A] 대변기(FV) 그룹 계산 ---
            double fvLoadSum = flushValves.Sum(i => i.TotalLoadUnits);
            int fvQtySum = flushValves.Sum(i => i.Quantity);
            // 대변기는 수량(Qty) 기준으로 동시율 적용
            double fvRate = GetFlushValveSimultaneity(fvQtySum);
            double fvEffective = fvLoadSum * fvRate;

            // --- [B] 일반기구 그룹 계산 ---
            double genLoadSum = generalFixtures.Sum(i => i.TotalLoadUnits);
            // 일반기구는 부하합계(Load)를 기준으로 동시율 적용 (엑셀 HLOOKUP 로직)
            double genRate = GetGeneralSimultaneity(genLoadSum);
            double genEffective = genLoadSum * genRate;

            // 개별 라인 출력 (생략 가능, 여기선 로직 확인용)
            foreach (var item in inputs)
            {
                double rowRate = item.Fixture.IsFlushValve ?
                                 GetFlushValveSimultaneity(item.Quantity) :
                                 GetGeneralSimultaneity(item.TotalLoadUnits); // 개별라인은 약식적용
                // *참고: 개별 라인 관경은 위 로직으로 단순 계산하면 오차가 있을 수 있으나 메인 관경이 중요함
            }

            // --- [C] 최종 메인 배관 (H24/H25) ---

            // 엑셀 G25 값: (일반 유효) + (대변기 유효)
            double totalEffective = genEffective + fvEffective;

            // 관경 결정
            int mainSize = DetermineSize(totalEffective, 0);

            Console.WriteLine($"[1] 일반기구 부하합계 : {genLoadSum:F1}");
            Console.WriteLine($"[2] 일반기구 동시율   : {genRate} (부하 {genLoadSum:F1} 기준)");
            Console.WriteLine($"[3] 일반기구 유효부하 : {genEffective:F2}");
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine($"[4] 대변기 부하합계   : {fvLoadSum:F1}");
            Console.WriteLine($"[5] 대변기 동시율     : {fvRate} (수량 {fvQtySum}개 기준)");
            Console.WriteLine($"[6] 대변기 유효부하   : {fvEffective:F2}");
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine($"★ 최종 유효부하 (G25): {totalEffective:F2} ({genEffective:F2} + {fvEffective:F2})");
            Console.WriteLine($"★ 최종 결정관경 (H25): {mainSize} A");
            Console.WriteLine("===============================================================");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // IsFlushValve = true 설정이 핵심
            var fixtures = new List<FixtureType>
            {
                new FixtureType("대변기", 4.9, 25, true), // TRUE!
                new FixtureType("소변기", 1.0, 15),
                new FixtureType("세면기", 1.0, 15),
                new FixtureType("청소씽크", 2.6, 20),
                new FixtureType("샤워", 1.0, 15),
                new FixtureType("욕조", 2.6, 20),
                new FixtureType("주방씽크", 1.0, 15)
            };

            var inputs = new List<FixtureInput>
            {
                new FixtureInput { Fixture = fixtures[0], Quantity = 3 },
                new FixtureInput { Fixture = fixtures[1], Quantity = 8 },
                new FixtureInput { Fixture = fixtures[2], Quantity = 8 },
                new FixtureInput { Fixture = fixtures[3], Quantity = 3 },
                new FixtureInput { Fixture = fixtures[4], Quantity = 2 },
                new FixtureInput { Fixture = fixtures[5], Quantity = 4 },
            };

            var engine = new PipeSizingEngine();
            engine.Calculate(inputs);
            Console.ReadLine();
        }
    }
}
