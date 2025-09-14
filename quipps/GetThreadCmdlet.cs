using libquip;
using libquip.threads;
using System;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipThread")]
	[OutputType(typeof(Thread))]
	public class GetThreadCmdlet : QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Id")]
		public string Id { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Url")]
		public Uri Url { get; set; }

		protected override void ProcessRecord()
		{
			Uri threadUri;

			if (Url != null)
			{
				// Remove trailing '/' to get thread id from url
				Id = Url.PathAndQuery.Substring(1);
			}
			else if (Uri.TryCreate(Id, UriKind.Absolute, out threadUri))
			{
				// Remove trailing '/' to get thread id from url
				Id = threadUri.PathAndQuery.Substring(1);
			}

			QuipThread quipThread = new QuipThread(ApiKey);
			var response = quipThread.GetThread(Id);
			WriteObject(response.thread);
		}
	}

	[Cmdlet(VerbsCommon.Get, "QuipThreadV2")]
	[OutputType(typeof(ThreadV2))]
	public class GetThreadV2Cmdlet : QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Id")]
		[ValidateLength(10, 32)]
		[ValidateNotNullOrEmpty]
		public string Id { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Url")]
		[ValidateNotNull]
		public Uri Url { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "SecretPath")]
		[ValidateLength(10, 32)]
		[ValidateNotNullOrEmpty]
		public string SecretPath { get; set; }

		protected override void ProcessRecord()
		{
			string threadIdOrSecretPath = null;

			// Handle different input types
			if (Url != null)
			{
				threadIdOrSecretPath = ExtractIdentifierFromUrl(Url);
			}
			else if (!string.IsNullOrEmpty(SecretPath))
			{
				threadIdOrSecretPath = SecretPath;
			}
			else if (!string.IsNullOrEmpty(Id))
			{
				// Check if Id is actually a URL
				if (Uri.TryCreate(Id, UriKind.Absolute, out Uri parsedUri))
				{
					threadIdOrSecretPath = ExtractIdentifierFromUrl(parsedUri);
				}
				else
				{
					threadIdOrSecretPath = Id;
				}
			}

			if (string.IsNullOrEmpty(threadIdOrSecretPath))
			{
				throw new ArgumentException("No valid thread identifier provided. Please specify Id, SecretPath, or Url.");
			}

			// Create V2 API instance and call V2 method
			var quipThread = new QuipThread(ApiKey, QuipApiVersion.V2);
			var response = quipThread.GetThreadV2(threadIdOrSecretPath);
			WriteObject(response.thread);
		}

		/// <summary>
		/// Extracts thread identifier (secret path or ID) from a Quip URL
		/// </summary>
		/// <param name="url">The Quip URL to parse</param>
		/// <returns>The thread identifier</returns>
		private string ExtractIdentifierFromUrl(Uri url)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));

			// Handle different Quip URL formats:
			// https://quip.com/{secretPath}/{title}
			// https://quip.com/{secretPath}
			// https://company.quip.com/{secretPath}/{title}
			// https://company.quip.com/{secretPath}

			var path = url.PathAndQuery.TrimStart('/').TrimEnd('/');
			
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException($"Unable to extract thread identifier from URL: {url}");
			}

			// Split by '/' and take the first part (should be the secret path or thread ID)
			var parts = path.Split('/');
			var identifier = parts[0];

			// Validate the extracted identifier
			if (string.IsNullOrEmpty(identifier) || identifier.Length < 10 || identifier.Length > 32)
			{
				throw new ArgumentException($"Invalid thread identifier extracted from URL: {identifier}. Must be 10-32 characters.");
			}

			return identifier;
		}
	}

	[Cmdlet(VerbsCommon.Get, "QuipThreadHtml")]
	[OutputType(typeof(Thread))]
	public class GetThreadHtmlCmdlet : QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Id")]
		public string Id { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Url")]
		public Uri Url { get; set; }

		protected override void ProcessRecord()
		{
			Uri threadUri;

			if (Url != null)
			{
				// Remove trailing '/' to get thread id from url
				Id = Url.PathAndQuery.Substring(1);
			}
			else if (Uri.TryCreate(Id, UriKind.Absolute, out threadUri))
			{
				// Remove trailing '/' to get thread id from url
				Id = threadUri.PathAndQuery.Substring(1);
			}

			QuipThread quipThread = new QuipThread(ApiKey);
			var response = quipThread.GetThread(Id);
			WriteObject(response.html);
		}
	}

	[Cmdlet(VerbsCommon.Get, "QuipThreadHtmlV2")]
	[OutputType(typeof(string))]
	public class GetThreadHtmlV2Cmdlet : QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Id", Mandatory = true)]
		[ValidateLength(10, 32)]
		[ValidateNotNullOrEmpty]
		public string Id { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Url", Mandatory = true)]
		[ValidateNotNull]
		public Uri Url { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "SecretPath", Mandatory = true)]
		[ValidateLength(10, 32)]
		[ValidateNotNullOrEmpty]
		public string SecretPath { get; set; }

		[Parameter(ValueFromPipelineByPropertyName = true)]
		[ValidateNotNullOrEmpty]
		public string Cursor { get; set; }

		[Parameter(ValueFromPipelineByPropertyName = true)]
		[ValidateRange(1, int.MaxValue)]
		public int? Limit { get; set; }

		[Parameter]
		public SwitchParameter Paginated { get; set; }

		[Parameter]
		public SwitchParameter IncludeMetadata { get; set; }

		protected override void ProcessRecord()
		{
			string threadIdOrSecretPath = null;

			// Handle different input types
			if (Url != null)
			{
				threadIdOrSecretPath = ExtractIdentifierFromUrl(Url);
			}
			else if (!string.IsNullOrEmpty(SecretPath))
			{
				threadIdOrSecretPath = SecretPath;
			}
			else if (!string.IsNullOrEmpty(Id))
			{
				// Check if Id is actually a URL
				if (Uri.TryCreate(Id, UriKind.Absolute, out Uri parsedUri))
				{
					threadIdOrSecretPath = ExtractIdentifierFromUrl(parsedUri);
				}
				else
				{
					threadIdOrSecretPath = Id;
				}
			}

			if (string.IsNullOrEmpty(threadIdOrSecretPath))
			{
				throw new ArgumentException("No valid thread identifier provided. Please specify Id, SecretPath, or Url.");
			}

			// Create V2 API instance
			var quipThread = new QuipThread(ApiKey, QuipApiVersion.V2);

			if (Paginated.IsPresent)
			{
				// Get single page with optional pagination (when -Paginated is specified)
				var response = quipThread.GetThreadHtmlV2(threadIdOrSecretPath, Cursor, Limit);
				
				if (IncludeMetadata.IsPresent)
				{
					// Return the full response object including metadata
					WriteObject(response);
				}
				else
				{
					// Return just the HTML content
					WriteObject(response.html);

					// If there's a next cursor, write a warning to inform about pagination
					if (!string.IsNullOrEmpty(response.response_metadata?.next_cursor))
					{
						WriteWarning($"More content available. Use -Cursor '{response.response_metadata.next_cursor}' to get the next page, or omit -Paginated to get all content automatically.");
					}
				}
			}
			else
			{
				// Default behavior: Get complete HTML content automatically handling pagination
				var completeHtml = quipThread.GetCompleteThreadHtmlV2(threadIdOrSecretPath, Limit);
				WriteObject(completeHtml);
			}
		}

		/// <summary>
		/// Extracts thread identifier (secret path or ID) from a Quip URL
		/// </summary>
		/// <param name="url">The Quip URL to parse</param>
		/// <returns>The thread identifier</returns>
		private string ExtractIdentifierFromUrl(Uri url)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));

			var path = url.PathAndQuery.TrimStart('/').TrimEnd('/');
			
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException($"Unable to extract thread identifier from URL: {url}");
			}

			var parts = path.Split('/');
			var identifier = parts[0];

			if (string.IsNullOrEmpty(identifier) || identifier.Length < 10 || identifier.Length > 32)
			{
				throw new ArgumentException($"Invalid thread identifier extracted from URL: {identifier}. Must be 10-32 characters.");
			}

			return identifier;
		}
	}
}
