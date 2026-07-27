using System;

namespace PipeLoad2
{
    /// <summary>
    /// 급수배관 관경결정 알고리즘 (관균등표법)
    /// 출처: 균등값·관균등표 = 건축 급배수.위생설비 (세진사)
    ///       동시사용율      = 공조위생 기술데이터북 (한미)
    /// </summary>
    public static class SupplyDiaCalc
    {
        // 위생기구별 균등값
        private static readonly double[] EquivValues =
            { 4.9, 1.0, 1.0, 1.0, 2.6, 1.0, 2.6, 1.0 };
        //  대변기FV 탱크  소변기 세면기 청소씽크 샤워  욕조  주방씽크

        // 관경 테이블 [mm, 균등값 상한]
        private static readonly (int dia, double limit)[] DiaTable =
        {
            (15,  1.0),  (20,  2.6),  (25,  4.9),  (32,  9.2),
            (40,  14.5), (50,  30.0), (65,  53.0),  (80,  84.6),
            (100, 178.0),(125, 280.0),(150, 452.0)
        };

        /// <summary>위생기구 수량으로 관경 결정</summary>
        public static int Calculate(
            int toiletFlushValve, int toiletTank,   int urinal,
            int lavatory,         int serviceSink,  int shower,
            int bathtub,          int kitchenSink)
        {
            int[]    counts = { 0, toiletTank, urinal, lavatory,
                                 serviceSink, shower, bathtub, kitchenSink };
            double toiletEquivSum   = toiletFlushValve * EquivValues[0];
            double generalEquivSum  = 0;
            int    generalCount     = 0;
            for (int i = 1; i < counts.Length; i++)
            {
                generalEquivSum += counts[i] * EquivValues[i];
                generalCount    += counts[i];
            }
            return Calculate2(toiletFlushValve, toiletEquivSum,
                              generalCount,     generalEquivSum);
        }

        /// <summary>중간값 직접 입력으로 관경 결정</summary>
        public static int Calculate2(
            int    toiletCount,
            double toiletEquivSum,
            int    generalCount,
            double generalEquivSum)
            => Calculate2(toiletCount, toiletEquivSum, generalCount, generalEquivSum, out _);

        /// <summary>중간값 직접 입력으로 관경 결정 — total(균등값×동시사용율 합) 노출</summary>
        public static int Calculate2(
            int    toiletCount,
            double toiletEquivSum,
            int    generalCount,
            double generalEquivSum,
            out double total)
        {
            double toiletSimul  = toiletCount  > 0
                ? toiletEquivSum  * GetToiletRate(toiletCount)  / 100.0 : 0;
            double generalSimul = generalCount > 0
                ? generalEquivSum * GetGeneralRate(generalCount) / 100.0 : 0;
            total = toiletSimul + generalSimul;

            return FromTotal(total);
        }

        /// <summary>균등값×동시사용율 합계(total)로 관경 lookup</summary>
        public static int FromTotal(double total)
        {
            foreach (var (dia, limit) in DiaTable)
                if (total <= limit) return dia;

            return 150;
        }

        // 동시사용율 테이블 [기구수, 동시사용율 %] — 공조위생 기술데이터북(한미)
        // 기구수 1~24 는 개별 명시, 이후 32/40/50/70/100 만 수록
        // 표에 없는 기구수는 상위 구간값 적용 (급수관경결정.xlsm 규칙: 일반기구 28개 → 32개 칸의 45%)

        // 대변기(세정밸브)
        private static readonly (int count, double rate)[] ToiletRates =
        {
            (1, 100),    (2, 100),   (3, 82.5),   (4, 65),     (5, 60),
            (6, 55),     (7, 50),    (8, 45),     (9, 43.75),  (10, 42.5),
            (11, 41.25), (12, 40),   (13, 38.75), (14, 37.5),  (15, 36.25),
            (16, 35),    (17, 33.75),(18, 32.5),  (19, 31.25), (20, 30),
            (21, 28.75), (22, 27.5), (23, 26.25), (24, 25),
            (32, 19),    (40, 17),   (50, 15),    (70, 12),    (100, 10)
        };

        // 일반기구
        private static readonly (int count, double rate)[] GeneralRates =
        {
            (1, 100),    (2, 100),   (3, 90),     (4, 80),     (5, 77.5),
            (6, 75),     (7, 72.5),  (8, 70),     (9, 66.25),  (10, 62.5),
            (11, 58.75), (12, 55),   (13, 53.75), (14, 52.5),  (15, 51.25),
            (16, 50),    (17, 49.75),(18, 49.5),  (19, 49.25), (20, 49),
            (21, 48.75), (22, 48.5), (23, 48.25), (24, 48),
            (32, 45),    (40, 40),   (50, 38),    (70, 35),    (100, 33)
        };

        private static double LookupRate((int count, double rate)[] table, int count)
        {
            foreach (var (c, rate) in table)
                if (count <= c) return rate;

            return table[^1].rate;   // 100개 초과
        }

        private static double GetToiletRate(int count)  => LookupRate(ToiletRates,  count);
        private static double GetGeneralRate(int count) => LookupRate(GeneralRates, count);
    }
}
