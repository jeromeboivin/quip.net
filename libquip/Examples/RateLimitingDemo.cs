using System;
using System.Threading.Tasks;
using libquip.threads;
using libquip.RateLimiting;

namespace libquip.Examples
{
	/// <summary>
	/// Demonstrates the usage of Quip API rate limiting features
	/// </summary>
	public class RateLimitingDemo
	{
		/// <summary>
		/// Demonstrates basic rate limiting functionality
		/// </summary>
		/// <param name="token">Your Quip API token</param>
		public static void BasicRateLimitingDemo(string token)
		{
			Console.WriteLine("=== Basic Rate Limiting Demo ===");

			// Create a QuipThread instance with automatic rate limiting enabled (default)
			var quipThread = new QuipThread(token, QuipApiVersion.V2);

			// Set up event handlers to monitor rate limiting
			quipThread.RateLimitManager.RateLimitUpdated += (sender, e) =>
			{
				var limitType = e.IsMinuteLimit ? "Per-Minute" : "Per-Hour";
				Console.WriteLine($"Rate Limit Updated ({limitType}): {e.RateLimit}");
			};

			quipThread.RateLimitManager.DelayApplied += (sender, e) =>
			{
				Console.WriteLine($"Rate Limit Delay Applied: {e.Delay.TotalSeconds:F2} seconds - {e.Reason}");
			};

			try
			{
				// Make multiple API calls - rate limiting will be applied automatically
				Console.WriteLine("Making API calls with automatic rate limiting...");
				
				for (int i = 0; i < 5; i++)
				{
					Console.WriteLine($"API Call #{i + 1}");
					var recent = quipThread.GetRecent(5);
					Console.WriteLine($"Retrieved {recent.Count} recent documents");
					
					// Display current rate limit status
					Console.WriteLine(quipThread.RateLimitManager.GetRateLimitStatus());
					Console.WriteLine();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
		}

		/// <summary>
		/// Demonstrates manual rate limit control
		/// </summary>
		/// <param name="token">Your Quip API token</param>
		public static void ManualRateLimitingDemo(string token)
		{
			Console.WriteLine("=== Manual Rate Limiting Demo ===");

			// Create instance with automatic rate limiting disabled
			var quipThread = new QuipThread(token, QuipApiVersion.V2);
			quipThread.EnableAutoRateLimiting = false;

			Console.WriteLine("Automatic rate limiting disabled - manual control enabled");

			try
			{
				// Manually check and apply rate limiting
				if (quipThread.RateLimitManager.AreRateLimitsExceeded())
				{
					Console.WriteLine("Rate limits exceeded - applying delay manually");
					quipThread.RateLimitManager.ApplyRateLimitDelay();
				}

				// Make API call
				var recent = quipThread.GetRecent(3);
				Console.WriteLine($"Retrieved {recent.Count} recent documents");

				// Check rate limit status after call
				Console.WriteLine(quipThread.RateLimitManager.GetRateLimitStatus());
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
		}

		/// <summary>
		/// Demonstrates asynchronous rate limiting
		/// </summary>
		/// <param name="token">Your Quip API token</param>
		public static async Task AsyncRateLimitingDemo(string token)
		{
			Console.WriteLine("=== Async Rate Limiting Demo ===");

			var quipThread = new QuipThread(token, QuipApiVersion.V2);

			// Set up monitoring
			quipThread.RateLimitManager.DelayApplied += (sender, e) =>
			{
				Console.WriteLine($"Async Delay: {e.Delay.TotalSeconds:F2} seconds - {e.Reason}");
			};

			try
			{
				Console.WriteLine("Applying rate limiting asynchronously...");
				
				// Apply rate limiting delay asynchronously
				await quipThread.RateLimitManager.ApplyRateLimitDelayAsync();
				
				// Note: The synchronous API methods will still apply their own rate limiting
				// For truly async operations, you would need async versions of the API methods
				var recent = quipThread.GetRecent(3);
				Console.WriteLine($"Retrieved {recent.Count} recent documents after async delay");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
		}

		/// <summary>
		/// Demonstrates rate limit monitoring and reporting
		/// </summary>
		/// <param name="token">Your Quip API token</param>
		public static void RateLimitMonitoringDemo(string token)
		{
			Console.WriteLine("=== Rate Limit Monitoring Demo ===");

			var quipThread = new QuipThread(token, QuipApiVersion.V2);

			// Detailed rate limit monitoring
			quipThread.RateLimitManager.RateLimitUpdated += (sender, e) =>
			{
				var manager = sender as QuipRateLimitManager;
				var limitType = e.IsMinuteLimit ? "Minute" : "Hour";
				
				Console.WriteLine($"\n--- Rate Limit Update ({limitType}) ---");
				Console.WriteLine($"Limit: {e.RateLimit.Limit}");
				Console.WriteLine($"Remaining: {e.RateLimit.Remaining}");
				Console.WriteLine($"Reset Time: {e.RateLimit.ResetTime:yyyy-MM-dd HH:mm:ss} UTC");
				Console.WriteLine($"Time Until Reset: {e.RateLimit.TimeUntilReset.TotalMinutes:F1} minutes");
				Console.WriteLine($"Is Approaching Limit: {e.RateLimit.IsApproachingLimit}");
				Console.WriteLine($"Is Nearly Exhausted: {e.RateLimit.IsNearlyExhausted}");
				Console.WriteLine($"Is Exhausted: {e.RateLimit.IsExhausted}");
				
				var recommendedDelay = e.RateLimit.CalculateRecommendedDelay();
				if (recommendedDelay > TimeSpan.Zero)
				{
					Console.WriteLine($"Recommended Delay: {recommendedDelay.TotalSeconds:F2} seconds");
				}
				Console.WriteLine("--- End Rate Limit Update ---\n");
			};

			try
			{
				// Make an API call to get initial rate limit information
				Console.WriteLine("Making initial API call to establish rate limit baseline...");
				var recent = quipThread.GetRecent(1);
				Console.WriteLine($"Baseline established with {recent.Count} documents retrieved");

				// Display full status
				Console.WriteLine("\nComplete Rate Limit Status:");
				Console.WriteLine(quipThread.RateLimitManager.GetRateLimitStatus());
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
		}

		/// <summary>
		/// Main demo method that runs all examples
		/// </summary>
		/// <param name="token">Your Quip API token</param>
		public static async Task RunAllDemos(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				Console.WriteLine("Please provide a valid Quip API token to run the demos.");
				Console.WriteLine("Get your token from: https://quip.com/dev/token");
				return;
			}

			Console.WriteLine("Quip API Rate Limiting Demonstration");
			Console.WriteLine("===================================\n");

			try
			{
				BasicRateLimitingDemo(token);
				Console.WriteLine("\n" + new string('=', 50) + "\n");

				ManualRateLimitingDemo(token);
				Console.WriteLine("\n" + new string('=', 50) + "\n");

				await AsyncRateLimitingDemo(token);
				Console.WriteLine("\n" + new string('=', 50) + "\n");

				RateLimitMonitoringDemo(token);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Demo failed with error: {ex.Message}");
			}

			Console.WriteLine("\nDemo completed!");
		}
	}
}