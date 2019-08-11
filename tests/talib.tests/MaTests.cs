using System;
using System.Linq;
using Talib.Indicators;
using Xunit;

namespace Talib.Tests {
    public class MaTests {

        private readonly double[] data = { 46.282, 45.614, 46.0328, 45.8931, 46.0826, 45.8433, 45.4245, 45.0955, 44.8264, 44.3278, 43.6124, 44.1497, 44.0902, 44.3389, 44.3189, 44.3289 };
        private readonly double[] data2 = { 1208.2, 1279.15, 1317.55, 1318.15, 1306.95, 1305.15, 1308.05, 1307.4, 1297.7, 1287.65, 1291.65, 1322.9, 1350.7, 1342.7, 1341.2, 1345.35, 1348.7, 1356, 1399.1, 1386.3, 1408.7, 1389.2, 1382.05, 1372.7, 1378.35, 1395.9, 1396.75, 1399.95, 1429.9, 1438.65, 1430.1, 1399.65, 1333.95, 1334.1, 1314.15, 1302.7, 1302.15, 1295.3, 1309, 1313.3, 1336.75, 1319.2, 1319.25, 1308.95, 1303.65, 1301.8, 1299.4, 1299.6, 1300.6, 1345.6, 1307.35, 1293.2, 1287.45, 1286.7, 1276.1, 1281.1, 1309.15, 1293.2, 1258.35, 1254.6, 1235.15, 1258.9, 1264.65, 1272.3, 1272.2, 1273.3, 1257.75, 1259.9, 1266.5, 1255.15, 1233.8, 1253.15, 1250.2, 1258.2, 1246.9, 1228.5, 1242.95, 1250.5, 1245.7, 1248.3, 1246.55, 1235.75, 1225.05, 1227.05, 1244.5, 1233.7, 1244.55, 1270.7, 1266, 1266.2, 1254.25, 1296.95, 1309.6, 1303.8, 1305.75, 1319.6, 1338.7, 1340, 1334.65, 1330.95 };
        [Fact]
        public void SmaSingle_SimpleValues_Calculated () {
            var ma = new MA ();

            var result = Math.Round (ma.SmaSingle (data, 10).Value, 4);

            Assert.Equal (44.4513D, result);
        }

        [Fact]
        public void SmaSingle_PeriodBiggerThanDataSize_ReturnNull () {
            var ma = new MA ();

            var result = ma.SmaSingle (data, 1000);

            Assert.Null (result);
        }

        [Fact]
        public void SmaSingle_PeriodZeroOrLess_ReturnNull () {
            var ma = new MA ();

            var result = ma.SmaSingle (data, 0);
            var result2 = ma.SmaSingle (data, -10);

            Assert.Null (result);
            Assert.Null (result2);
        }

        [Fact]
        public void Sma_SimpleValues_calculated () {
            var ma = new MA ();

            double[] result = ma.Sma (data, 3).Where (x => x != null).Select (x => Math.Round (x.Value, 4)).ToArray ();
            int nullCounts = ma.Sma (data, 3).Where (x => x == null).Count ();
            double[] expected = { 45.9763, 45.8466, 46.0028, 45.9397, 45.7835, 45.4544, 45.1155, 44.7499, 44.2555, 44.03, 43.9508, 44.1929, 44.2493, 44.3289 };
            Assert.Equal (expected, result);
            Assert.Equal (2, nullCounts);
        }

        [Fact]
        public void Sma_PeriodBiggerThanDataSize_ReturnNull () {
            var ma = new MA ();

            double?[] result = ma.Sma (data, 300);
            double?[] expected = { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

            Assert.Equal (expected, result);
        }

        [Fact]
        public void EmaSingle_SimpleValues_Calculated () {
            //Given
            var ma = new MA ();

            //When
            var result = Math.Round(ma.EmaSingle(data2, 5).Value, 2);

            //Then
            Assert.Equal (1328.45D, result);
        }

        [Fact]
        public void EmaSingle_PeriodBiggerThanDataLength_ReturnNull () {
            //Given
            var ma = new MA ();

            //When
            var result = ma.EmaSingle(data2, 1000);
            
            //Then
            Assert.Null(result);
        }

        [Fact]
        public void EmaSingle_NullData_ReturnNull () {
            //Given
            var ma = new MA ();

            //When
            var result = ma.EmaSingle(null, 1000);
            
            //Then
            Assert.Null(result);
        }

        [Fact]
        public void EmaSingle_PeriodZeroOrLess_ReturnNull () {
            //Given
            var ma = new MA();

            //When
            var result = ma.EmaSingle(data2, 0);
            var result2 = ma.EmaSingle(data2, -10);
            
            //Then
            Assert.Null(result);
            Assert.Null(result2);
        }

        [Fact(Skip="Needs to be fixed later")]
        public void Ema_SimpleValues_Calculated()
        {
            //Given
            var ma = new MA();
            //When
            var result = ma.Ema(data, 3);
            double[] expected = { 44.3289, 44.2096, 44.1796, 43.896, 44.1119, 44.4692, 44.7823, 45.1034, 45.4734, 45.778, 45.8355, 45.9342, 45.7741, 46.028};
            var result2 = result.Select(x => Math.Round(x.Value,4)).ToArray();
            //Then
            Assert.Equal(expected, result2);
        }
    }
}