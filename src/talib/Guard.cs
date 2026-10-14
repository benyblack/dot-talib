namespace Talib;

/// <summary>Shared argument validation.</summary>
internal static class Guard
{
  /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> unless <paramref name="period"/> is a positive integer.</summary>
  /// <param name="period">The lookback period to validate.</param>
  public static void Period(int period)
  {
    if (period < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(period), period, "period must be a positive integer");
    }
  }

  /// <summary>Throws <see cref="ArgumentException"/> unless the history is long enough to warm up a streaming state.</summary>
  /// <param name="length">The number of available points.</param>
  /// <param name="period">The lookback period the state needs.</param>
  /// <param name="name">The name of the init function, for the error message.</param>
  public static void Warmup(int length, int period, string name)
  {
    if (length < period)
    {
      throw new ArgumentException($"{name} needs at least period ({period}) prices, got {length}");
    }
  }
}
