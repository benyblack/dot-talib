namespace Talib;

// O(1) stepping primitives for the streaming (Init/Next) APIs: a rolling
// window sum and a monotonic-deque rolling extreme. The fill and slide
// arithmetic deliberately matches the batch sliding walks operation for
// operation — the fill accumulates `sum + v` left to right exactly like a
// plain sum over the first window, and the slide is the batch walks'
// `sum - leaving + adding` — so a state stepped through a history continues
// the corresponding batch series bit for bit.

/// <summary>A rolling window sum, for O(1) sliding-window series.</summary>
internal sealed class SumState
{
  private readonly int _period;
  private readonly double[] _queue;
  private int _head;
  private int _count;
  private double _sum;
  private bool _filled;

  /// <summary>Creates an empty rolling sum over windows of <paramref name="period"/> values.</summary>
  /// <param name="period">The window size.</param>
  public SumState(int period)
  {
    _period = period;
    _queue = new double[period];
  }

  /// <summary>The raw sum of the values currently inside the window.</summary>
  public double CurrentSum => _sum;

  /// <summary>Pushes a value and returns the full-window sum, or <c>null</c> until the window has filled.</summary>
  /// <param name="value">The incoming value.</param>
  /// <returns>The window sum once <c>period</c> values have been seen, the sliding sum after.</returns>
  public double? Push(double value)
  {
    if (!_filled)
    {
      _queue[_count] = value;
      _sum += value;
      _count++;
      if (_count == _period)
      {
        _filled = true;
        return _sum;
      }

      return null;
    }

    double leaving = _queue[_head];
    _sum = _sum - leaving + value;
    _queue[_head] = value;
    _head = (_head + 1) % _period;
    return _sum;
  }
}

/// <summary>
/// A rolling window extreme (max or min) via a monotonic deque, for O(1)
/// sliding max/min series. Equal values never evict each other, so the
/// earliest tie wins — the same deque evolution as the batch
/// <see cref="Window"/> walk.
/// </summary>
internal sealed class ExtremeState
{
  private readonly int _period;
  private readonly bool _isMax;
  private readonly int[] _ticks;
  private readonly double[] _values;
  private readonly int _capacity;
  private int _head;
  private int _count;
  private int _tick = -1;

  /// <summary>Creates an empty rolling extreme over windows of <paramref name="period"/> values.</summary>
  /// <param name="period">The window size.</param>
  /// <param name="isMax"><c>true</c> for a rolling maximum, <c>false</c> for a minimum.</param>
  public ExtremeState(int period, bool isMax)
  {
    _period = period;
    _isMax = isMax;
    _capacity = period + 1;
    _ticks = new int[_capacity];
    _values = new double[_capacity];
  }

  /// <summary>Pushes a value and returns the window extreme with its absolute tick, or <c>null</c> until <c>period</c> values have been seen.</summary>
  /// <param name="value">The incoming value.</param>
  /// <returns>The extreme (earliest tie winning) once the window has filled.</returns>
  public (int Tick, double Value)? Push(double value)
  {
    _tick++;

    // Evict strictly-worse candidates from the rear (equals stay, so
    // the earliest tie keeps the front).
    while (_count > 0 && (_isMax ? _values[Rear] < value : _values[Rear] > value))
    {
      _count--;
    }

    int rear = (_head + _count) % _capacity;
    _ticks[rear] = _tick;
    _values[rear] = value;
    _count++;

    // Drop the front once it leaves the window.
    if (_ticks[_head] <= _tick - _period)
    {
      _head = (_head + 1) % _capacity;
      _count--;
    }

    if (_tick >= _period - 1)
    {
      return (_ticks[_head], _values[_head]);
    }

    return null;
  }

  private int Rear => (_head + _count - 1) % _capacity;
}
