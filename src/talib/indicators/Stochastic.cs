using System.Collections.Generic;
using System.Linq;

namespace Talib.Indicators
{
    public class Stochastic
    {
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