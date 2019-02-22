
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Timer
{
	public Action action { get; private set; }
	public DateTime executeAt { get; private set; }
	public bool isRunning { get; private set; }

	public const int TimerPollTimeMs = 1000;

	private static readonly List<Timer> _timers = new List<Timer>();

	public Timer(Action action)
	{
		this.action = action;
	}

	public void StartCountdown(TimeSpan time)
	{
		Start(DateTime.Now + time);
	}

	public void Start(DateTime executeAt)
	{
		if (_timers.Contains(this))
			throw new Exception("Timer already started.");

		this.executeAt = executeAt;

		// if no timers were running before, run async tick
		if (_timers.Count == 0)
			TickTimers();

		_timers.Add(this);
		_timers.Sort(SortTimers);
	}

	public bool Stop()
	{
		return _timers.Remove(this);
	}

	private static int SortTimers(Timer x, Timer y)
	{
		return x.executeAt.CompareTo(y.executeAt);
	}

	public static Timer StartCountdown(int seconds, Action action)
	{
		Timer timer = new Timer(action);
		timer.StartCountdown(TimeSpan.FromSeconds(seconds));
		return timer;
	}

	private static async void TickTimers()
	{
		// pop timers one by one
		while (_timers.Count > 0)
		{
			DateTime time = DateTime.Now;
			if (time >= _timers[0].executeAt)
			{
				_timers[0].action();
				_timers.RemoveAt(0);

				// try to process next timer immediately if one exists
				continue;
			}

			await Task.Delay(TimerPollTimeMs);
		}
	}
}