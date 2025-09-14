using RestSharp;
using System;
using libquip.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace libquip
{
	/// <summary>
	/// Supported Quip API versions
	/// </summary>
	public enum QuipApiVersion
	{
		/// <summary>
		/// Quip API Version 1
		/// </summary>
		V1 = 1,
		/// <summary>
		/// Quip API Version 2
		/// </summary>
		V2 = 2
	}

	public abstract class QuipApi
	{
		protected RestClient _client;
		protected string _token;
		private readonly QuipApiVersion _version;
		private readonly QuipRateLimitManager _rateLimitManager;

		/// <summary>
		/// Gets the current API version being used
		/// </summary>
		public QuipApiVersion Version => _version;

		/// <summary>
		/// Gets the rate limit manager for this API instance
		/// </summary>
		public QuipRateLimitManager RateLimitManager => _rateLimitManager;

		/// <summary>
		/// Gets or sets whether to automatically apply rate limiting delays (default: true)
		/// </summary>
		public bool EnableAutoRateLimiting { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of the QuipApi class with the specified token and API version
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use (defaults to V1)</param>
		/// <exception cref="ArgumentNullException">Thrown when token is null or empty</exception>
		/// <exception cref="ArgumentException">Thrown when an unsupported API version is specified</exception>
		public QuipApi(string token, QuipApiVersion version = QuipApiVersion.V1)
		{
			if (string.IsNullOrEmpty(token))
				throw new ArgumentNullException(nameof(token), "Token cannot be null or empty");

			ValidateApiVersion(version);

			_token = token;
			_version = version;
			_client = new RestClient(GetBaseUrl(version));
			_rateLimitManager = new QuipRateLimitManager();
		}

		/// <summary>
		/// Initializes a new instance of the QuipApi class with the specified token and API version (integer)
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use (1 or 2, defaults to 1)</param>
		/// <exception cref="ArgumentNullException">Thrown when token is null or empty</exception>
		/// <exception cref="ArgumentException">Thrown when an unsupported API version is specified</exception>
		public QuipApi(string token, int version) : this(token, (QuipApiVersion)version)
		{
		}

		/// <summary>
		/// Validates that the specified API version is supported
		/// </summary>
		/// <param name="version">The API version to validate</param>
		/// <exception cref="ArgumentException">Thrown when an unsupported API version is specified</exception>
		private static void ValidateApiVersion(QuipApiVersion version)
		{
			if (!Enum.IsDefined(typeof(QuipApiVersion), version))
			{
				throw new ArgumentException($"Unsupported API version: {version}. Supported versions are V1 (1) and V2 (2).", nameof(version));
			}
		}

		/// <summary>
		/// Gets the base URL for the specified API version
		/// </summary>
		/// <param name="version">The API version</param>
		/// <returns>The base URL for the API</returns>
		private static string GetBaseUrl(QuipApiVersion version)
		{
			return $"https://platform.quip.com/{(int)version}/";
		}

		/// <summary>
		/// Executes a REST request with automatic rate limiting
		/// </summary>
		/// <typeparam name="T">The response type</typeparam>
		/// <param name="request">The REST request to execute</param>
		/// <param name="cancellationToken">Cancellation token for async operations</param>
		/// <returns>The response from the API</returns>
		protected async Task<IRestResponse<T>> ExecuteWithRateLimitingAsync<T>(IRestRequest request, CancellationToken cancellationToken = default) where T : new()
		{
			if (EnableAutoRateLimiting)
			{
				await _rateLimitManager.ApplyRateLimitDelayAsync(cancellationToken);
			}

			var response = await _client.ExecuteTaskAsync<T>(request, cancellationToken);
			
			UpdateRateLimitFromResponse(response);
			CheckResponse(response);

			return response;
		}

		/// <summary>
		/// Executes a REST request with automatic rate limiting (synchronous)
		/// </summary>
		/// <typeparam name="T">The response type</typeparam>
		/// <param name="request">The REST request to execute</param>
		/// <returns>The response from the API</returns>
		protected IRestResponse<T> ExecuteWithRateLimiting<T>(IRestRequest request) where T : new()
		{
			if (EnableAutoRateLimiting)
			{
				_rateLimitManager.ApplyRateLimitDelay();
			}

			var response = _client.Execute<T>(request);
			
			UpdateRateLimitFromResponse(response);
			CheckResponse(response);

			return response;
		}

		/// <summary>
		/// Updates rate limit information from API response headers
		/// </summary>
		/// <param name="response">The REST response containing rate limit headers</param>
		private void UpdateRateLimitFromResponse<T>(IRestResponse<T> response)
		{
			if (response?.Headers == null) return;

			string limitHeader = null;
			string remainingHeader = null;
			string resetHeader = null;

			foreach (var header in response.Headers)
			{
				switch (header.Name?.ToLowerInvariant())
				{
					case "x-ratelimit-limit":
						limitHeader = header.Value?.ToString();
						break;
					case "x-ratelimit-remaining":
						remainingHeader = header.Value?.ToString();
						break;
					case "x-ratelimit-reset":
						resetHeader = header.Value?.ToString();
						break;
				}
			}

			// Update rate limits (assuming per-minute limits for now)
			// In a real implementation, you might need to differentiate between minute and hour limits
			// based on additional header information or API documentation
			if (!string.IsNullOrEmpty(limitHeader) || !string.IsNullOrEmpty(remainingHeader) || !string.IsNullOrEmpty(resetHeader))
			{
				_rateLimitManager.UpdateRateLimit(limitHeader, remainingHeader, resetHeader, true);
			}
		}

		/// <summary>
		/// Checks the response for errors and throws a QuipException if the request was not successful
		/// </summary>
		/// <typeparam name="T">The response type</typeparam>
		/// <param name="response">The REST response to check</param>
		/// <exception cref="QuipException">Thrown when the response indicates an error</exception>
		protected static void CheckResponse<T>(IRestResponse<T> response)
		{
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new QuipException(QuipError.FromJson(response.Content));
			}
		}

		/// <summary>
		/// Gets information about the current API configuration
		/// </summary>
		/// <returns>A string describing the API configuration</returns>
		public virtual string GetApiInfo()
		{
			return $"Quip API Version {(int)_version} - Base URL: {_client.BaseUrl}";
		}
	}
}
