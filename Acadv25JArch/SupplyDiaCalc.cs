using System;

namespace PipeLoad2
{
    /// <summary>
    /// 급수배관 관경결정 알고리즘 (관균등표법)
    /// 출처: 건축 급배수.위생설비 (세진사)
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
        {
            double toiletSimul  = toiletCount  > 0
                ? toiletEquivSum  * GetToiletRate(toiletCount)  / 100.0 : 0;
            double generalSimul = generalCount > 0
                ? generalEquivSum * GetGeneralRate(generalCount) / 100.0 : 0;
            double total = toiletSimul + generalSimul;

            foreach (var (dia, limit) in DiaTable)
                if (total <= limit) return dia;

            return 150;
        }

        // 대변기(세정밸브) 동시사용율 테이블
        private static double GetToiletRate(int count)
        {
            if (count == 1)          return 100;
            if (count <= 4)          return 50;
            if (count <= 8)          return 40;
            if (count <= 12)         return 30;
            if (count <= 16)         return 27;
            if (count <= 24)         return 23;
            if (count <= 32)         return 19;
            if (count <= 40)         return 17;
            if (count <= 50)         return 15;
            if (count <= 70)         return 12;
            return 10;
        }

        // 일반기구 동시사용율 테이블
        private static double GetGeneralRate(int count)
        {
            if (count <= 2)          return 100;
            if (count <= 4)          return 70;
            if (count <= 8)          return 55;
            if (count <= 12)         return 48;
            if (count <= 16)         return 45;
            if (count <= 24)         return 42;
            if (count <= 32)         return 40;
            if (count <= 40)         return 39;
            if (count <= 50)         return 38;
            if (count <= 70)         return 35;
            return 33;
        }
    }
}
