namespace Talib.Indicators;

/// <summary>
/// Money Flow Index
/// (<see href="https://en.wikipedia.org/wiki/Money_flow_index">Wikipedia</see>).
///
/// With the typical price <c>TP = (high + low + close) / 3</c> and the
/// raw money flow <c>TP * volume</c>, over the lookback window:
///
/// <c>MFI = 100 - 100 / (1 + positive_flow / negative_flow)</c>
///
/// Bars are always given in chronological order, oldest first. Series
/// functions return results aligned with the input, and all functions
/// return <c>null</c> when there is not enough data or the window has
/// no movement. Only the High, Low, Close and Volume of each bar are
/// read.
/// </summary>
public static class Mfi
{
  /// <summary>Calculates the latest Money Flow Index.</summary>
  /// <param name="bars">Bars, oldest first; High, Low, Close and Volume are read.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>
  /// The MFI at the latest point, or <c>null</c> when there is not
  /// enough data (the earliest usable window starts at the second bar)
  /// or the window has no movement.
  /// </returns>
  public static double? Latest(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    var series = MfiSeries(bars, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full Money Flow Index series.
  ///
  /// Returns an array aligned with <paramref name="bars"/>: the first
  /// <c>period</c> entries are <c>null</c>, and every later entry is
  /// the MFI of the window ending at that point. A window without
  /// movement (neither positive nor negative flow) leaves the entry
  /// <c>null</c>.
  /// </summary>
  /// <param name="bars">Bars, oldest first; High, Low, Close and Volume are read.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The null-padded MFI series.</returns>
  public static double?[] MfiSeries(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[bars.Length];

    var tps = new double[bars.Length];
    var flows = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      tps[i] = TypicalPrice(bars[i]);
      flows[i] = tps[i] * bars[i].Volume;
    }

    // Each flow counts as positive/negative by the change of the
    // typical price from the previous bar; the first flow has no
    // previous typical price to compare against, so the earliest
    // usable window starts at the second bar.
    var directions = new int[bars.Length];
    for (int i = 1; i < bars.Length; i++)
    {
      directions[i] = tps[i] > tps[i - 1] ? 1 : tps[i] < tps[i - 1] ? -1 : 0;
    }

    for (int i = period; i < bars.Length; i++)
    {
      double positive = 0;
      double negative = 0;
      for (int j = i - period + 1; j <= i; j++)
      {
        if (directions[j] == 1)
        {
          positive = positive + flows[j];
        }
        else if (directions[j] == -1)
        {
          negative = negative + flows[j];
        }
      }

      if (positive == 0 && negative == 0)
      {
        continue;
      }

      result[i] = negative == 0 ? 100.0 : 100.0 - 100.0 / (1.0 + positive / negative);
    }

    return result;
  }

  private static double TypicalPrice(Bar bar) => (bar.High + bar.Low + bar.Close) / 3;
}
