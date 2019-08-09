using System.Linq;

namespace Talib.Indicators {
    public class MA {

        public double? SmaSingle (double[] data, int period) {
            if (period > data.Length || period <= 0) {
                return null;
            }

            double sum = 0;
            for (int i = data.Length - 1; i >= data.Length - period; i--) {
                sum += data[i];
            }
            return sum / period;
        }

        public double?[] Sma (double[] data, int period) {
            var result = new double?[data.Length];
            for (int i = 0; i < data.Length; i++) {
                result[i] = SmaSingle(data.Take (i + 1).ToArray(), period);
            }
            return result;
        }
    }
}