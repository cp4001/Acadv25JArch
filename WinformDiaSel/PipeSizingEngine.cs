using System.Collections.Generic;
using System.Linq;

namespace WinformDiaSel
{
    /// <summary>
    /// 급수 관경 계산 엔진
    /// </summary>
    public class PipeSizingEngine
    {
        // 관경 선정표 (허용 최대 부하단위 -> 관경)
        private readonly Dictionary<double, int> _realCapacityTable = new Dictionary<double, int>()
        {
            { 1.0, 15 },
            { 2.6, 20 },
            { 4.9, 25 },
            { 9.2, 32 },
            { 14.5, 40 },
            { 30.0, 50 },
            { 53.0, 65 },
            { 84.6, 80 },
            { 178.0, 100 },
            { 280.0, 125 },
            { 452.0, 150 }
        };

        /// <summary>
        /// 대변기(세정밸브) 동시사용률 (수량 기준)
        /// </summary>
        public double GetFlushValveSimultaneity(int qty)
        {
            if (qty <= 0) return 0;
            if (qty <= 2) return 1.0;
            if (qty == 3) return 0.825;
            if (qty == 4) return 0.65;
            if (qty == 5) return 0.60;
            if (qty <= 7) return 0.50;
            if (qty <= 10) return 0.43;
            return 0.40;
        }

        /// <summary>
        /// 일반기구 동시사용률 (부하합계 기준)
        /// </summary>
        public double GetGeneralSimultaneity(double loadSum)
        {
            if (loadSum <= 0) return 0;
            if (loadSum < 2) return 1.0;
            if (loadSum < 3) return 1.0;
            if (loadSum < 4) return 0.90;
            if (loadSum < 5) return 0.80;
            if (loadSum < 10) return 0.70;
            if (loadSum < 20) return 0.60;
            if (loadSum < 32) return 0.48;
            if (loadSum < 40) return 0.45;
            if (loadSum < 50) return 0.40;
            return 0.35;
        }

        /// <summary>
        /// 관경 결정 함수
        /// </summary>
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

        /// <summary>
        /// 계산 수행 및 결과 반환
        /// </summary>
        public CalcResult Calculate(List<FixtureInput> inputs)
        {
            var result = new CalcResult();

            // 1. 그룹 분리
            var flushValves = inputs.Where(i => i.Fixture.IsFlushValve).ToList();
            var generalFixtures = inputs.Where(i => !i.Fixture.IsFlushValve).ToList();

            // 2. 대변기(FV) 그룹 계산
            result.FvLoadSum = flushValves.Sum(i => i.TotalLoadUnits);
            result.FvQtySum = flushValves.Sum(i => i.Quantity);
            result.FvRate = GetFlushValveSimultaneity(result.FvQtySum);
            result.FvEffective = result.FvLoadSum * result.FvRate;

            // 3. 일반기구 그룹 계산
            result.GenLoadSum = generalFixtures.Sum(i => i.TotalLoadUnits);
            result.GenRate = GetGeneralSimultaneity(result.GenLoadSum);
            result.GenEffective = result.GenLoadSum * result.GenRate;

            // 4. 최종 계산
            result.TotalEffective = result.GenEffective + result.FvEffective;
            result.MainSize = DetermineSize(result.TotalEffective, 0);

            return result;
        }

        /// <summary>
        /// 간편 계산 함수 - 다른 프로그램에서 호출용
        /// </summary>
        /// <param name="대변기개수">대변기 수량</param>
        /// <param name="소변기개수">소변기 수량</param>
        /// <param name="세면기개수">세면기 수량</param>
        /// <param name="청소씽크개수">청소씽크 수량</param>
        /// <param name="샤워개수">샤워 수량</param>
        /// <param name="욕조개수">욕조 수량</param>
        /// <param name="주방씽크개수">주방씽크 수량</param>
        /// <returns>결정된 관경(A)</returns>
        public int CalcSelDia(int 대변기개수, int 소변기개수, int 세면기개수, int 청소씽크개수, int 샤워개수, int 욕조개수, int 주방씽크개수)
        {
            // 기구 타입 정의 (WinForm과 동일)
            var fixtures = new List<FixtureType>
            {
                new FixtureType("대변기", 4.9, 25, true),  // 세정밸브
                new FixtureType("소변기", 1.0, 15),
                new FixtureType("세면기", 1.0, 15),
                new FixtureType("청소씽크", 2.6, 20),
                new FixtureType("샤워", 1.0, 15),
                new FixtureType("욕조", 2.6, 20),
                new FixtureType("주방씽크", 1.0, 15)
            };

            // 입력 데이터 생성
            var inputs = new List<FixtureInput>
            {
                new FixtureInput { Fixture = fixtures[0], Quantity = 대변기개수 },
                new FixtureInput { Fixture = fixtures[1], Quantity = 소변기개수 },
                new FixtureInput { Fixture = fixtures[2], Quantity = 세면기개수 },
                new FixtureInput { Fixture = fixtures[3], Quantity = 청소씽크개수 },
                new FixtureInput { Fixture = fixtures[4], Quantity = 샤워개수 },
                new FixtureInput { Fixture = fixtures[5], Quantity = 욕조개수 },
                new FixtureInput { Fixture = fixtures[6], Quantity = 주방씽크개수 }
            };

            // 계산 수행
            var result = Calculate(inputs);

            // 최종 관경 반환
            return result.MainSize;
        }
    }
}
