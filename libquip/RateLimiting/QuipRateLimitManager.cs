using System;
using System.Threading;
using System.Threading.Tasks;

namespace libquip.RateLimiting
{
	/// <summary>
	/// Manages API rate limiting for Quip API requests
	/// </summary>
	public class QuipRateLimitManager
	{
		private ApiRateLimit _minuteRateLimit;
		private ApiRateLimit _hourRateLimit;
		private readonly object _lockObject = new object();
		private static readonly TimeSpan DefaultMinuteWindow = TimeSpan.FromMinutes(1);
		private static readonly TimeSpan DefaultHourWindow = TimeSpan.FromHours(1);

		/// <summary>
		/// Gets the current per-minute rate limit information
		/// </summary>
		public ApiRateLimit MinuteRateLimit
		{
			get
			{
				lock (_lockObject)
				{
					return _minuteRateLimit;
				}
			}
		}

		/// <summary>
		/// Gets the current per-hour rate limit information
		/// </summary>
		public ApiRateLimit HourRateLimit
		{
			get
			{
				lock (_lockObject)
				{
					return _hourRateLimit;
				}
			}
		}

		/// <summary>
		/// Event raised when rate limit information is updated
		/// </summary>
		public event EventHandler<RateLimitUpdatedEventArgs> RateLimitUpdated;

		/// <summary>
		/// Event raised when a delay is applied due to rate limiting
		/// </summary>
		public event EventHandler<RateLimitDelayEventArgs> DelayApplied;

		/// <summary>
		/// Updates the rate limit information from API response headers
		/// </summary>
		/// <param name="limitHeader">X-Ratelimit-Limit header value</param>
		/// <param name="remainingHeader">X-Ratelimit-Remaining header value</param>
		/// <param name="resetHeader">X-Ratelimit-Reset header value</param>
		/// <param name="isMinuteLimit">True if this represents per-minute limits, false for per-hour limits</param>
		public void UpdateRateLimit(string limitHeader, string remainingHeader, string resetHeader, bool isMinuteLimit = true)
		{
			var rateLimit = ApiRateLimit.FromHeaders(limitHeader, remainingHeader, resetHeader);
			if (rateLimit == null) return;

			lock (_lockObject)
			{
				if (isMinuteLimit)
				{
					_minuteRateLimit = rateLimit;
				}
				else
				{
					_hourRateLimit = rateLimit;
				}
			}

			RateLimitUpdated?.Invoke(this, new RateLimitUpdatedEventArgs(rateLimit, isMinuteLimit));
		}

		/// <summary>
		/// Calculates and applies the appropriate delay before making an API request
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Task representing the delay operation</returns>
		public async Task ApplyRateLimitDelayAsync(CancellationToken cancellationToken = default)
		{
			var delay = CalculateRequiredDelay();
			
			if (delay > TimeSpan.Zero)
			{
				DelayApplied?.Invoke(this, new RateLimitDelayEventArgs(delay, GetDelayReason()));
				await Task.Delay(delay, cancellationToken);
			}
		}

		/// <summary>
		/// Synchronously applies the appropriate delay before making an API request
		/// </summary>
		public void ApplyRateLimitDelay()
		{
			var delay = CalculateRequiredDelay();
			
			if (delay > TimeSpan.Zero)
			{
				DelayApplied?.Invoke(this, new RateLimitDelayEventArgs(delay, GetDelayReason()));
				Thread.Sleep(delay);
			}
		}

		/// <summary>
		/// Calculates the required delay based on current rate limit information
		/// </summary>
		/// <returns>TimeSpan representing the required delay</returns>
		private TimeSpan CalculateRequiredDelay()
		{
			TimeSpan minuteDelay = TimeSpan.Zero;
			TimeSpan hourDelay = TimeSpan.Zero;

			lock (_lockObject)
			{
				if (_minuteRateLimit != null)
				{
					minuteDelay = _minuteRateLimit.CalculateRecommendedDelay();
				}

				if (_hourRateLimit != null)
				{
					hourDelay = _hourRateLimit.CalculateRecommendedDelay();
				}
			}

			// Return the longer delay to respect both limits
			return minuteDelay > hourDelay ? minuteDelay : hourDelay;
		}

		/// <summary>
		/// Gets the reason for the current delay
		/// </summary>
		/// <returns>String describing why a delay is being applied</returns>
		private string GetDelayReason()
		{
			lock (_lockObject)
			{
				if (_minuteRateLimit?.IsExhausted == true || _hourRateLimit?.IsExhausted == true)
				{
					return "Rate limit exceeded";
				}

				if (_minuteRateLimit?.IsNearlyExhausted == true || _hourRateLimit?.IsNearlyExhausted == true)
				{
					return "Rate limit nearly exhausted";
				}

				if (_minuteRateLimit?.IsApproachingLimit == true || _hourRateLimit?.IsApproachingLimit == true)
				{
					return "Approaching rate limit";
				}

				return "Proactive rate limiting";
			}
		}

		/// <summary>
		/// Checks if any rate limits are currently exceeded
		/// </summary>
		/// <returns>True if rate limits are exceeded</returns>
		public bool AreRateLimitsExceeded()
		{
			lock (_lockObject)
			{
				return (_minuteRateLimit?.IsExhausted == true) || (_hourRateLimit?.IsExhausted == true);
			}
		}

		/// <summary>
		/// Gets a summary of the current rate limit status
		/// </summary>
		/// <returns>String describing the current rate limit status</returns>
		public string GetRateLimitStatus()
		{
			lock (_lockObject)
			{
				var status = "Rate Limit Status:\n";
				
				if (_minuteRateLimit != null)
				{
					status += $"Per Minute: {_minuteRateLimit}\n";
				}
				else
				{
					status += "Per Minute: Unknown\n";
				}

				if (_hourRateLimit != null)
				{
					status += $"Per Hour: {_hourRateLimit}\n";
				}
				else
				{
					status += "Per Hour: Unknown\n";
				}

				return status.TrimEnd();
			}
		}
	}

	/// <summary>
	/// Event arguments for rate limit updates
	/// </summary>
	public class RateLimitUpdatedEventArgs : EventArgs
	{
		public ApiRateLimit RateLimit { get; }
		public bool IsMinuteLimit { get; }

		public RateLimitUpdatedEventArgs(ApiRateLimit rateLimit, bool isMinuteLimit)
		{
			RateLimit = rateLimit;
			IsMinuteLimit = isMinuteLimit;
		}
	}

	/// <summary>
	/// Event arguments for rate limit delays
	/// </summary>
	public class RateLimitDelayEventArgs : EventArgs
	{
		public TimeSpan Delay { get; }
		public string Reason { get; }

		public RateLimitDelayEventArgs(TimeSpan delay, string reason)
		{
			Delay = delay;
			Reason = reason;
		}
	}
}