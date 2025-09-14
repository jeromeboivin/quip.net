using System;

namespace libquip.RateLimiting
{
	/// <summary>
	/// Represents API rate limit information for a specific time window
	/// </summary>
	public class ApiRateLimit
	{
		/// <summary>
		/// The number of requests per time window the user can make
		/// </summary>
		public int Limit { get; set; }

		/// <summary>
		/// The number of requests remaining this user can make within the time window
		/// </summary>
		public int Remaining { get; set; }

		/// <summary>
		/// The UTC timestamp for when the rate limit resets
		/// </summary>
		public DateTime ResetTime { get; set; }

		/// <summary>
		/// Gets the time remaining until the rate limit resets
		/// </summary>
		public TimeSpan TimeUntilReset => ResetTime - DateTime.UtcNow;

		/// <summary>
		/// Gets whether the rate limit is approaching (less than 20% remaining)
		/// </summary>
		public bool IsApproachingLimit => Remaining < (Limit * 0.2);

		/// <summary>
		/// Gets whether the rate limit is nearly exhausted (less than 5% remaining)
		/// </summary>
		public bool IsNearlyExhausted => Remaining < (Limit * 0.05);

		/// <summary>
		/// Gets whether the rate limit has been exceeded
		/// </summary>
		public bool IsExhausted => Remaining <= 0;

		/// <summary>
		/// Calculates the recommended delay before making the next request
		/// </summary>
		/// <returns>TimeSpan representing the recommended delay</returns>
		public TimeSpan CalculateRecommendedDelay()
		{
			if (IsExhausted)
			{
				// Wait until reset if exhausted
				return TimeUntilReset.Add(TimeSpan.FromSeconds(1));
			}

			if (IsNearlyExhausted)
			{
				// Distribute remaining requests evenly over the remaining time
				var timePerRequest = TimeUntilReset.TotalMilliseconds / Math.Max(Remaining, 1);
				return TimeSpan.FromMilliseconds(timePerRequest);
			}

			if (IsApproachingLimit)
			{
				// Add a small delay to slow down the request rate
				return TimeSpan.FromMilliseconds(500);
			}

			// No delay needed
			return TimeSpan.Zero;
		}

		/// <summary>
		/// Creates an ApiRateLimit from HTTP response headers
		/// </summary>
		/// <param name="limitHeader">X-Ratelimit-Limit header value</param>
		/// <param name="remainingHeader">X-Ratelimit-Remaining header value</param>
		/// <param name="resetHeader">X-Ratelimit-Reset header value (Unix timestamp)</param>
		/// <returns>ApiRateLimit instance or null if headers are invalid</returns>
		public static ApiRateLimit FromHeaders(string limitHeader, string remainingHeader, string resetHeader)
		{
			if (string.IsNullOrEmpty(limitHeader) || 
				string.IsNullOrEmpty(remainingHeader) || 
				string.IsNullOrEmpty(resetHeader))
			{
				return null;
			}

			if (!int.TryParse(limitHeader, out int limit) ||
				!int.TryParse(remainingHeader, out int remaining) ||
				!long.TryParse(resetHeader, out long resetTimestamp))
			{
				return null;
			}

			var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).UtcDateTime;

			return new ApiRateLimit
			{
				Limit = limit,
				Remaining = remaining,
				ResetTime = resetTime
			};
		}

		public override string ToString()
		{
			return $"Limit: {Limit}, Remaining: {Remaining}, Reset: {ResetTime:yyyy-MM-dd HH:mm:ss} UTC";
		}
	}
}