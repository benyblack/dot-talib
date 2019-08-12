using System;
using System.Linq;
using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators
{
    public class MacdTests
    {
        private readonly double[] data = { 1208.2, 1279.15, 1317.55, 1318.15, 1306.95, 1305.15, 1308.05, 1307.4, 1297.7, 1287.65, 1291.65, 1322.9, 1350.7, 1342.7, 1341.2, 1345.35, 1348.7, 1356, 1399.1, 1386.3, 1408.7, 1389.2, 1382.05, 1372.7, 1378.35, 1395.9, 1396.75, 1399.95, 1429.9, 1438.65, 1430.1, 1399.65, 1333.95, 1334.1, 1314.15, 1302.7, 1302.15, 1295.3, 1309, 1313.3, 1336.75, 1319.2, 1319.25, 1308.95, 1303.65, 1301.8, 1299.4, 1299.6, 1300.6, 1345.6, 1307.35, 1293.2, 1287.45, 1286.7, 1276.1, 1281.1, 1309.15, 1293.2, 1258.35, 1254.6, 1235.15, 1258.9, 1264.65, 1272.3, 1272.2, 1273.3, 1257.75, 1259.9, 1266.5, 1255.15, 1233.8, 1253.15, 1250.2, 1258.2, 1246.9, 1228.5, 1242.95, 1250.5, 1245.7, 1248.3, 1246.55, 1235.75, 1225.05, 1227.05, 1244.5, 1233.7, 1244.55, 1270.7, 1266, 1266.2, 1254.25, 1296.95, 1309.6, 1303.8, 1305.75, 1319.6, 1338.7, 1340, 1334.65, 1330.95 };

        [Fact]
        public void MacdSingle_SimpleValues_Calculated()
        {
            //When
            double? result = Macd.MacdSingle(data, fast: 12, slow: 26);

            //Then
            Assert.Equal(20.36D, Math.Round(result.Value, 2));
        }

        [Fact]
        public void SignalSingle_SimpleValues_Calculated()
        {
            //When
            double? result = Macd.SignalSingle(data, period: 9);

            //Then
            Assert.Equal(12.44D, Math.Round(result.Value, 2));
        }

        [Fact]
        public void HistogramSingle_SimpleValues_Calculated()
        {
            //When
            double? result = Macd.HistogramSingle(data);
            //Then
            Assert.Equal(7.92D, Math.Round(result.Value, 2));
        }

    }
}