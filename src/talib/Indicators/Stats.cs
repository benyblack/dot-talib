namespace Talib.Indicators;

/// <summary>
/// Statistic functions (TA-Lib group).
///
/// <para>
///   Linear regression fits <c>y = intercept + slope * x</c> over each
///   lookback window with <c>x = 0..period - 1</c>. <see cref="LinearReg"/>
///   is the fitted value at the window's last point, <see cref="Tsf"/>
///   projects one bar ahead, and <see cref="Angle"/> is the slope in
///   degrees. <see cref="Beta"/> and <see cref="Correl"/> relate two
///   series over the window.
/// </para>
///
/// Prices are always given in chronological order, oldest first (the
/// reference module is scalar-only, so there are no series forms). All
/// scalar functions return <c>null</c> when there is not enough data.
/// </summary>
public static class Stats
{
  /// <summary>Calculates the population variance of the <c>period</c> newest prices.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The variance at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Variance(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double sum = 0;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      sum += prices[i];
    }

    double mean = sum / period;

    double total = 0;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      total = total + (prices[i] - mean) * (prices[i] - mean);
    }

    return total / period;
  }

  /// <summary>
  /// Calculates the beta of <paramref name="market"/> (the y axis) against
  /// <paramref name="prices"/> (the x axis) over the <c>period</c> newest
  /// points, following the TA-Lib C reference exactly: the slope of the
  /// regression of the two series' per-bar returns, computed with
  /// shift-anchored sliding sums and the C code's reseed triggers
  /// (denominator cancellation, outlier transit, every 32·period bars).
  ///
  /// Note: the textbook (and Elixir-reference) formulation regresses the
  /// raw prices instead of their returns — a different quantity; this
  /// implementation follows the C library, verified value by value in
  /// the audit. Degenerate windows (no return variance) produce
  /// <c>0</c>, the C library's defined reading, not <c>null</c>.
  /// </summary>
  /// <param name="prices">Prices (the x axis), oldest first.</param>
  /// <param name="market">Market prices (the y axis), oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The beta at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Beta(double[] prices, double[] market, int period = 14)
  {
    Guard.Period(period);
    double?[] series = BetaSeries(prices, market, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// A faithful port of TA-Lib's <c>ta_BETA</c>: returns-based regression
  /// slope over a sliding window of <c>period</c> returns (so values are
  /// defined from index <c>period</c> on), with the shift-anchor and
  /// reseed machinery kept operation for operation.
  /// </summary>
  /// <param name="x">The x axis series, oldest first.</param>
  /// <param name="y">The y axis series, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The null-padded beta series.</returns>
  private static double?[] BetaSeries(double[] x, double[] y, int period)
  {
    int n0 = Math.Min(x.Length, y.Length);
    var result = new double?[n0];
    if (n0 <= period)
    {
      return result;
    }

    double sXx = 0, sYy = 0, sXy = 0, sX = 0, sY = 0;
    double lastX = x[0], lastY = y[0];
    double trailingLastX = lastX, trailingLastY = lastY;
    double shiftX = 0, shiftY = 0;

    // The trailing cursor points at the oldest return still in the
    // window (bar 1), carrying bar 0 as its predecessor price.
    int trailingIdx = 1;
    double n = period;
    int barsSinceReseed = 32 * period;
    double leavingXx = 0, leavingYy = 0;

    // Anchor the returns at the first change; later re-anchors use the
    // window mean (see the reseed below).
    if (lastX != 0.0)
    {
      shiftX = (x[1] - lastX) / lastX;
    }

    if (lastY != 0.0)
    {
      shiftY = (y[1] - lastY) / lastY;
    }

    int i = 1;
    for (; i < period; i++)
    {
      double rx = lastX != 0.0 ? (x[i] - lastX) / lastX - shiftX : -shiftX;
      lastX = x[i];
      double ry = lastY != 0.0 ? (y[i] - lastY) / lastY - shiftY : -shiftY;
      lastY = y[i];
      sXx += rx * rx;
      sYy += ry * ry;
      sXy += rx * ry;
      sX += rx;
      sY += ry;
    }

    while (i <= n0 - 1)
    {
      double rx = lastX != 0.0 ? (x[i] - lastX) / lastX - shiftX : -shiftX;
      lastX = x[i];
      double ry = lastY != 0.0 ? (y[i] - lastY) / lastY - shiftY : -shiftY;
      lastY = y[i];
      i++;
      sXx += rx * rx;
      sYy += ry * ry;
      sXy += rx * ry;
      sX += rx;
      sY += ry;

      double denomScale = n * sXx;
      double denom = denomScale - sX * sX;

      // Re-anchor and rebuild when the shift has gone stale: denominator
      // cancellation, a squared outlier leaving the window, or simply
      // every 32*period bars.
      barsSinceReseed--;
      if (denom < 0.000001 * denomScale || leavingXx > 1000.0 * sXx || leavingYy > 1000.0 * sYy || barsSinceReseed <= 0)
      {
        barsSinceReseed = 32 * period;
        double sumRx = 0, sumRy = 0;
        double prevX = trailingLastX, prevY = trailingLastY;
        for (int j = trailingIdx; j < i; j++)
        {
          if (prevX != 0.0)
          {
            sumRx += (x[j] - prevX) / prevX;
          }

          prevX = x[j];
          if (prevY != 0.0)
          {
            sumRy += (y[j] - prevY) / prevY;
          }

          prevY = y[j];
        }

        shiftX = sumRx / n;
        shiftY = sumRy / n;

        prevX = trailingLastX;
        prevY = trailingLastY;
        sXx = 0;
        sYy = 0;
        sXy = 0;
        sX = 0;
        sY = 0;
        for (int j = trailingIdx; j < i; j++)
        {
          double bx = prevX != 0.0 ? (x[j] - prevX) / prevX - shiftX : -shiftX;
          prevX = x[j];
          double by = prevY != 0.0 ? (y[j] - prevY) / prevY - shiftY : -shiftY;
          prevY = y[j];
          sXx += bx * bx;
          sYy += by * by;
          sXy += bx * by;
          sX += bx;
          sY += by;
        }

        denomScale = n * sXx;
        denom = denomScale - sX * sX;
        if (denom < 0.0)
        {
          denom = 0.0;
        }
      }

      // Always read the trailing bar before writing the output.
      double tx = trailingLastX != 0.0 ? (x[trailingIdx] - trailingLastX) / trailingLastX - shiftX : -shiftX;
      trailingLastX = x[trailingIdx];
      double ty = trailingLastY != 0.0 ? (y[trailingIdx] - trailingLastY) / trailingLastY - shiftY : -shiftY;
      trailingLastY = y[trailingIdx];
      trailingIdx++;

      result[i - 1] = denom > 0.00000000000001 * denomScale ? (n * sXy - sX * sY) / denom : 0.0;

      leavingXx = tx * tx;
      leavingYy = ty * ty;
      sXx -= tx * tx;
      sYy -= ty * ty;
      sXy -= tx * ty;
      sX -= tx;
      sY -= ty;
    }

    return result;
  }

  /// <summary>
  /// Calculates the Pearson correlation of the two series over the
  /// <c>period</c> newest points.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="market">Market prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The correlation at the latest point, or <c>null</c> when there is not enough data or either variance is zero.</returns>
  public static double? Correl(double[] prices, double[] market, int period = 14)
  {
    Guard.Period(period);
    (double Cov, double VarX, double VarY)? moments = Moments(prices, market, period);
    if (moments is null)
    {
      return null;
    }

    var (cov, varX, varY) = moments.Value;
    double denominator = Math.Sqrt(varX * varY);
    return denominator == 0 ? null : cov / denominator;
  }

  /// <summary>
  /// Calculates the fitted value of the linear regression at the window's
  /// last point (TA-Lib LINEARREG).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The linear regression value at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? LinearReg(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    var (slope, intercept) = Fit(prices, period);
    return intercept + slope * (period - 1);
  }

  /// <summary>
  /// Calculates the slope of the linear regression over the
  /// <c>period</c> newest prices (TA-Lib LINEARREG_SLOPE).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The slope at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Slope(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    return Fit(prices, period).Slope;
  }

  /// <summary>
  /// Calculates the intercept of the linear regression over the
  /// <c>period</c> newest prices (TA-Lib LINEARREG_INTERCEPT).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The intercept at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Intercept(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    return Fit(prices, period).Intercept;
  }

  /// <summary>
  /// Calculates the regression slope in degrees
  /// (TA-Lib LINEARREG_ANGLE).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The angle at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Angle(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    return Math.Atan(Fit(prices, period).Slope) * (180 / Math.PI);
  }

  /// <summary>
  /// Calculates the Time Series Forecast: the regression projected one
  /// bar past the window's last point (TA-Lib TSF).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The forecast at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Tsf(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    var (slope, intercept) = Fit(prices, period);
    return intercept + slope * period;
  }

  // Least squares over the newest `period` prices, with x = 0..n-1.
  private static (double Slope, double Intercept) Fit(double[] prices, int period)
  {
    int n = period;
    long sumX = n * (n - 1L) / 2;
    long sumX2 = (n - 1L) * n * (2 * n - 1) / 6;
    int start = prices.Length - period;

    double sumY = 0;
    for (int i = start; i < prices.Length; i++)
    {
      sumY += prices[i];
    }

    double sumXy = 0;
    for (int x = 0; x < n; x++)
    {
      sumXy = sumXy + x * prices[start + x];
    }

    double denominator = n * sumX2 - sumX * sumX;
    double slope = (n * sumXy - sumX * sumY) / denominator;
    double intercept = (sumY - slope * sumX) / n;
    return (slope, intercept);
  }

  // Covariance and variances of the two newest-`period` windows
  // (population moments); null when either series is too short.
  private static (double Cov, double VarX, double VarY)? Moments(double[] prices, double[] market, int period)
  {
    if (prices.Length < period || market.Length < period)
    {
      return null;
    }

    int startX = prices.Length - period;
    int startY = market.Length - period;
    double sumX = 0;
    for (int i = startX; i < prices.Length; i++)
    {
      sumX += prices[i];
    }

    double sumY = 0;
    for (int i = startY; i < market.Length; i++)
    {
      sumY += market[i];
    }

    double meanX = sumX / period;
    double meanY = sumY / period;

    double cov = 0;
    double varX = 0;
    double varY = 0;
    for (int i = 0; i < period; i++)
    {
      double a = prices[startX + i];
      double b = market[startY + i];
      cov = cov + (a - meanX) * (b - meanY);
      varX = varX + (a - meanX) * (a - meanX);
      varY = varY + (b - meanY) * (b - meanY);
    }

    return (cov / period, varX / period, varY / period);
  }
}
