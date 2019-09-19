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
    public class Stochastic
    {

        /// <summary>
        /// Calculate a single value of %K line
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double K_Single(double[] data, int period)
        {
            if (data.Length < period)
            {
                return 0;
            }
            var price = data.Last();
            var selected_data = data.Skip(data.Length - period).ToArray();
            var lowest_low = selected_data.Min();
            var highest_high = selected_data.Max();
            return (price - lowest_low) / (highest_high - lowest_low) * 100D;
        }

        /// <summary>
        /// Calculate %K line
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double?[] K(double[] data, int period)
        {
            var k_list = new List<double?>();
            for (int i = 0; i < data.Length; i++)
            {
                if (period + i > data.Length)
                {
                    k_list.Add(null);
                }
                else
                {
                    var selected_data = data.Skip(data.Length - (period + i)).Take(period).ToArray();
                    k_list.Add(K_Single(selected_data, period));
                }
            }
            k_list.Reverse();
            return k_list.ToArray();
        }

        /// <summary>
        /// Calculate %D line
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        /// <param name="k_period">Period of calculation for %K</param>
        public static double?[] D(double[] data, int period, int k_period)
        {
            var d_list = new List<double?>();
            for (int i = 0; i < data.Length; i++)
            {
                if (period + i > data.Length)
                {
                    d_list.Add(null);
                }
                else
                {
                    var selected_data = data.Skip(data.Length - (period + i)).Take(period).ToArray();
                    d_list.Add(D_Single(selected_data, period, k_period));
                }
            }
            d_list.Reverse();
            return d_list.ToArray();
        }

        /// <summary>
        /// Calculate a single value of %D line
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        /// <param name="k_period">Period of calculation for %K</param>
        public static double? D_Single(double[] data, int period, int k_period = 3)
        {
            if (data.Length < period)
            {
                return 0;
            }
            var k_list = new List<double>();
            for (int i = 0; i < period; i++)
            {
                var selected_data = data.Skip(data.Length - (period + i)).Take(period).ToArray();
                k_list.Add(K_Single(selected_data, period));
            }
            var ma = new MA();
            return ma.SmaSingle(k_list.ToArray(), k_period);
        }
    }
}