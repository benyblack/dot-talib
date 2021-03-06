using System;
using System.Linq;
using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators
{
    public class MacdTests
    {
        private readonly double[] data = { 1208.2, 1279.15, 1317.55, 1318.15, 1306.95, 1305.15, 1308.05, 1307.4, 1297.7, 1287.65, 1291.65, 1322.9, 1350.7, 1342.7, 1341.2, 1345.35, 1348.7, 1356, 1399.1, 1386.3, 1408.7, 1389.2, 1382.05, 1372.7, 1378.35, 1395.9, 1396.75, 1399.95, 1429.9, 1438.65, 1430.1, 1399.65, 1333.95, 1334.1, 1314.15, 1302.7, 1302.15, 1295.3, 1309, 1313.3, 1336.75, 1319.2, 1319.25, 1308.95, 1303.65, 1301.8, 1299.4, 1299.6, 1300.6, 1345.6, 1307.35, 1293.2, 1287.45, 1286.7, 1276.1, 1281.1, 1309.15, 1293.2, 1258.35, 1254.6, 1235.15, 1258.9, 1264.65, 1272.3, 1272.2, 1273.3, 1257.75, 1259.9, 1266.5, 1255.15, 1233.8, 1253.15, 1250.2, 1258.2, 1246.9, 1228.5, 1242.95, 1250.5, 1245.7, 1248.3, 1246.55, 1235.75, 1225.05, 1227.05, 1244.5, 1233.7, 1244.55, 1270.7, 1266, 1266.2, 1254.25, 1296.95, 1309.6, 1303.8, 1305.75, 1319.6, 1338.7, 1340, 1334.65, 1330.95 };

        [Fact(Skip="Results are close to ta-lib in python but not match exactly. it must be discovered why!")]
        public void MACD_SimpleValue_Calculated()
        {
            //When
            (double?[] macd, double?[] signal, double?[] histogram) = Macd.MACD(data);
            //Then
            double?[] expected_macd = {         null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null, 
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,  20.9580082 ,  13.95922686,   7.40338135,
                    2.13879148,  -2.55669962,  -5.11348952,  -6.71537917,
                    -6.0232371 ,  -6.81231968,  -7.34892489,  -8.50724607,
                    -9.74060601, -10.74348795, -11.59824174, -12.11979253,
                -12.31052557,  -8.7299258 ,  -8.87640831, -10.01879309,
                -11.25833908, -12.16102225, -13.57524954, -14.12969828,
                -12.16546641, -11.76026518, -14.08883669, -16.05180483,
                -18.95838324, -19.12498019, -18.57886691, -17.329019  ,
                -16.16028893, -14.9727051 , -15.11208865, -14.87756479,
                -13.99778039, -14.05438486, -15.64170307, -15.16348567,
                -14.8513383 , -13.79935574, -13.71931977, -14.96807426,
                -14.62316027, -13.5840036 , -12.99795205, -12.18326263,
                -11.54573373, -11.77620843, -12.67613923, -13.07721163,
                -11.85039129, -11.61569854, -10.43392275,  -7.30308505,
                    -5.14185226,  -3.37402769,  -2.90380584,   0.9039612 ,
                    4.88606972,   7.48759555,   9.59605486,  12.24346823,
                    15.70177252,  18.3360363 ,  19.76417987,  20.36270627};
            double?[] expected_signal ={        null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,  34.71873678,  30.5668348 ,  25.93414411,
                    21.17507358,  16.42871894,  12.12027725,   8.35314596,
                    5.47786935,   3.01983155,   0.94608026,  -0.94458501,
                    -2.70378921,  -4.31172896,  -5.76903151,  -7.03918372,
                    -8.09345209,  -8.22074683,  -8.35187913,  -8.68526192,
                    -9.19987735,  -9.79210633, -10.54873497, -11.26492764,
                -11.44503539, -11.50808135, -12.02423242, -12.8297469 ,
                -14.05547417, -15.06937537, -15.77127368, -16.08282274,
                -16.09831598, -15.8731938 , -15.72097277, -15.55229118,
                -15.24138902, -15.00398819, -15.13153116, -15.13792207,
                -15.08060531, -14.8243554 , -14.60334827, -14.67629347,
                -14.66566683, -14.44933418, -14.15905776, -13.76389873,
                -13.32026573, -13.01145427, -12.94439126, -12.97095534,
                -12.74684253, -12.52061373, -12.10327553, -11.14323744,
                    -9.9429604 ,  -8.62917386,  -7.48410025,  -5.80648796,
                    -3.66797643,  -1.43686203,   0.76972135,   3.06447072,
                    5.59193108,   8.14075213,  10.46543768,  12.44489139};
            double?[] expected_histogram = {       null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null,          null,          null,          null,
                            null, -13.76072858, -16.60760793, -18.53076276,
                -19.0362821 , -18.98541856, -17.23376677, -15.06852513,
                -11.50110645,  -9.83215122,  -8.29500515,  -7.56266106,
                    -7.0368168 ,  -6.43175899,  -5.82921023,  -5.08060881,
                    -4.21707348,  -0.50917897,  -0.52452919,  -1.33353117,
                    -2.05846173,  -2.36891592,  -3.02651457,  -2.86477065,
                    -0.72043102,  -0.25218383,  -2.06460427,  -3.22205793,
                    -4.90290907,  -4.05560482,  -2.80759323,  -1.24619625,
                    -0.06197295,   0.9004887 ,   0.60888412,   0.67472639,
                    1.24360863,   0.94960333,  -0.5101719 ,  -0.02556361,
                    0.22926701,   1.02499966,   0.88402851,  -0.29178079,
                    0.04250656,   0.86533058,   1.16110571,   1.5806361 ,
                    1.774532  ,   1.23524584,   0.26825204,  -0.10625629,
                    0.89645124,   0.90491519,   1.66935278,   3.84015239,
                    4.80110814,   5.25514617,   4.58029441,   6.71044916,
                    8.55404615,   8.92445758,   8.82633352,   9.17899751,
                    10.10984143,  10.19528417,   9.2987422 ,   7.91781487};

            Assert.Equal(expected_macd.Select(x => x.HasValue ? (double?)Math.Round(x.Value, 4) : null).ToArray(),
                                  macd.Select(x => x.HasValue ? (double?)Math.Round(x.Value, 4) : null).ToArray());
            Assert.Equal(expected_signal, signal);
            Assert.Equal(expected_histogram, histogram);
        }

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