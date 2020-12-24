using System;
using System.Collections.Generic;
using System.Linq;

namespace Talib.Indicators
{

    /// <summary>
    /// Stochastic Oscillator [Wikipedia](https://en.wikipedia.org/wiki/Stochastic_oscillator)
    /// - %K = (Current Close - Lowest Low)/(Highest High - Lowest Low) * 100
    /// - %D = 3-day SMA of %K
    /// - Lowest Low = lowest low for the look-back period
    /// - Highest High = highest high for the look-back period
    /// - %K is multiplied by 100 to move the decimal point two places
    /// </summary>
    public static class Stochastic
    {
        private static bool ValidateData(double[] data, int period) =>
            (data != null && data != Array.Empty<double>() && period <= data.Length && period > 0);

        /// <summary>
        /// Price list accumulator class
        /// </summary>
        public class ListStatistics
        {
            public double Min { get; set; }
            public double Max { get; set; }

            public ListStatistics()
            {
                Min = double.MaxValue;
                Max = double.MinValue;
            }

            public ListStatistics Accumulate(double price)
            {
                Min = Math.Min(Min, price);
                Max = Math.Max(Max, price);
                return this;
            }

            public ListStatistics Compute()
            {
                return this;
            }
        }

        /// <summary>
        /// Calculate a single value of %K line
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double K_Single(double[] data, int period)
        {
            if (!ValidateData(data, period)) return 0;

            var lastPrice = data.Last();
            var selectedData = data.Skip(data.Length - period).ToArray();
            var listStatistics = selectedData.Aggregate(new ListStatistics(), (acc, p) => acc.Accumulate(p), acc => acc.Compute());
            var lowestLow = listStatistics.Min;
            var highestHigh = listStatistics.Max;
            return (lastPrice - lowestLow) / (highestHigh - lowestLow) * 100D;
        }

        /// <summary>
        /// Calculate %K line
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double?[] K(double[] data, int period)
        {
            var kList = new List<double?>();

            for (var i = 0; i < data.Length; i++)
            {
                kList.Add(null);
                if (period + i > data.Length) continue;

                var selectedData = data.SelectList(i, period);
                kList[i] = K_Single(selectedData, period);
            }
            kList.Reverse();
            return kList.ToArray();
        }

        /// <summary>
        /// Calculate %D line
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        /// <param name="kPeriod">Period of calculation for %K</param>
        public static double?[] D(double[] data, int period, int kPeriod)
        {
            var dList = new List<double?>();
            for (var i = 0; i < data.Length; i++)
            {
                dList.Add(null);
                if (period + i > data.Length) continue;

                var selectedData = data.SelectList(i, period);
                dList[i] = D_Single(selectedData, period, kPeriod);
            }
            dList.Reverse();
            return dList.ToArray();
        }

        /// <summary>
        /// Calculate a single value of %D line
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        /// <param name="kPeriod">Period of calculation for %K</param>
        public static double? D_Single(double[] data, int period, int kPeriod = 3)
        {
            if (!ValidateData(data, period)) return 0;

            var kList = new List<double>();
            for (var i = 0; i < period; i++)
            {
                var selectedData = data.SelectList(i,period);
                kList.Add(K_Single(selectedData, period));
            }
            return MA.SmaSingle(kList.ToArray(), kPeriod);
        }

        public static double[] SelectList(this double[] data,int index,int period)
        {
            return data.Skip(data.Length - (period + index)).Take(period).ToArray();
        }
    }
}
