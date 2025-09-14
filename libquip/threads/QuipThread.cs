using RestSharp;
using System;
using System.Collections.Generic;

namespace libquip.threads
{
	// V1 API Models (existing)
	public class Sharing
	{
		public string company_id { get; set; }
		public string company_mode { get; set; }
	}

	public class Thread
	{
		public string author_id { get; set; }
		public string thread_class { get; set; }
		public string id { get; set; }
		public long created_usec { get; set; }
		public long updated_usec { get; set; }
		public string title { get; set; }
		public Sharing sharing { get; set; }
		public string link { get; set; }
		public string type { get; set; }
	}

	public class Document
	{
		public Thread thread { get; set; }
		public List<string> user_ids { get; set; }
		public List<string> shared_folder_ids { get; set; }
		public List<string> expanded_user_ids { get; set; }
		public List<string> invited_user_emails { get; set; }
		public string html { get; set; }
	}

	// V2 API Models
	/// <summary>
	/// Thread information for API Version 2
	/// </summary>
	public class ThreadV2
	{
		/// <summary>
		/// The ID of the user who created the thread
		/// </summary>
		public string author_id { get; set; }

		/// <summary>
		/// The Unix timestamp in microseconds for when the thread was created
		/// </summary>
		public long created_usec { get; set; }

		/// <summary>
		/// This is the thread_id
		/// </summary>
		public string id { get; set; }

		/// <summary>
		/// Returns "true" if this thread is a template. Returns "false" if this thread isn't a template
		/// </summary>
		public bool is_template { get; set; }

		/// <summary>
		/// Link to the thread
		/// </summary>
		public string link { get; set; }

		/// <summary>
		/// ID of the company that owns the thread
		/// </summary>
		public string owning_company_id { get; set; }

		/// <summary>
		/// This is the thread's identifier that you can find in its URL
		/// </summary>
		public string secret_path { get; set; }

		/// <summary>
		/// Information about the thread's link sharing settings
		/// </summary>
		public SharingV2 sharing { get; set; }

		/// <summary>
		/// The title of the thread
		/// </summary>
		public string title { get; set; }

		/// <summary>
		/// Category that the thread belongs to: document, spreadsheet, slides, or chat
		/// </summary>
		public ThreadTypeV2 type { get; set; }

		/// <summary>
		/// The Unix timestamp in microseconds for when the thread was last changed
		/// </summary>
		public long updated_usec { get; set; }
	}

	/// <summary>
	/// Thread sharing information for API Version 2
	/// </summary>
	public class SharingV2
	{
		public string company_id { get; set; }
		public string company_mode { get; set; }
	}

	/// <summary>
	/// Thread response for API Version 2
	/// </summary>
	public class ThreadResponseV2
	{
		/// <summary>
		/// Information about a thread
		/// </summary>
		public ThreadV2 thread { get; set; }
	}

	/// <summary>
	/// Response metadata for paginated API calls
	/// </summary>
	public class ResponseMetadata
	{
		/// <summary>
		/// Cursor value that points to the next page of data. 
		/// Empty if no additional data is available.
		/// Expires 30 minutes after creation.
		/// </summary>
		public string next_cursor { get; set; }
	}

	/// <summary>
	/// HTML content response for API Version 2 with pagination support
	/// </summary>
	public class ThreadHtmlResponseV2
	{
		/// <summary>
		/// The document content rendered as HTML. 
		/// Elements that correspond to document sections will have their section ids rendered as the "id" tag.
		/// </summary>
		public string html { get; set; }

		/// <summary>
		/// Response metadata including pagination information
		/// </summary>
		public ResponseMetadata response_metadata { get; set; }
	}

	/// <summary>
	/// Thread types for API Version 2
	/// </summary>
	public enum ThreadTypeV2
	{
		DOCUMENT,
		SPREADSHEET,
		SLIDES,
		CHAT
	}

	// Existing enums
	public enum DocumentType
	{
		document,
		spreadsheet
	}

	public enum DocumentFormat
	{
		html,
		markdown
	}

	public enum DocumentLocation
	{
		Append,
		Prepend,
		AfterSection,
		BeforeSection,
		ReplaceSection,
		DeleteSection
	}

	public class QuipThread : QuipApi
	{
		/// <summary>
		/// Initializes a new instance of the QuipThread class with API version 1 (default)
		/// </summary>
		/// <param name="token">The authentication token</param>
		public QuipThread(string token)
			: base(token, QuipApiVersion.V1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the QuipThread class with the specified API version
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use</param>
		public QuipThread(string token, QuipApiVersion version)
			: base(token, version)
		{
		}

		/// <summary>
		/// Initializes a new instance of the QuipThread class with the specified API version (integer)
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use (1 or 2)</param>
		public QuipThread(string token, int version)
			: base(token, version)
		{
		}

		// V1 API Methods (existing)
		public Dictionary<string, Document> GetRecent(int count = 10)
		{
			var request = new RestRequest("threads/recent?count={count}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("count", count.ToString());

			var response = ExecuteWithRateLimiting<Dictionary<string, Document>>(request);

			return response.Data;
		}

		public Document GetThread(string id)
		{
			var request = new RestRequest("threads/{id}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("id", id);

			var response = ExecuteWithRateLimiting<Document>(request);

			return response.Data;
		}

		/// <summary>
		/// Gets basic information about a Quip thread using API Version 2
		/// </summary>
		/// <param name="threadIdOrSecretPath">The ID or secret path of the thread to get information about</param>
		/// <returns>Basic thread information including ID, title, type, sharing settings, etc.</returns>
		/// <exception cref="InvalidOperationException">Thrown when called on a non-V2 API instance</exception>
		/// <exception cref="ArgumentException">Thrown when threadIdOrSecretPath is null, empty, or invalid length</exception>
		/// <exception cref="QuipException">Thrown when the API returns an error</exception>
		public ThreadResponseV2 GetThreadV2(string threadIdOrSecretPath)
		{
			if (Version != QuipApiVersion.V2)
			{
				throw new InvalidOperationException("GetThreadV2 method requires API Version 2. Current version: " + Version);
			}

			if (string.IsNullOrEmpty(threadIdOrSecretPath))
			{
				throw new ArgumentException("Thread ID or secret path cannot be null or empty", nameof(threadIdOrSecretPath));
			}

			if (threadIdOrSecretPath.Length < 10 || threadIdOrSecretPath.Length > 32)
			{
				throw new ArgumentException("Thread ID or secret path must be between 10 and 32 characters", nameof(threadIdOrSecretPath));
			}

			var request = new RestRequest("threads/{threadIdOrSecretPath}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("threadIdOrSecretPath", threadIdOrSecretPath);

			var response = ExecuteWithRateLimiting<ThreadResponseV2>(request);

			return response.Data;
		}

		/// <summary>
		/// Gets the HTML content of a Quip thread using API Version 2 with pagination support
		/// </summary>
		/// <param name="threadIdOrSecretPath">The ID or secret path of the thread to get HTML content for</param>
		/// <param name="cursor">A pointer to the next page of data. Leave null for the first call.</param>
		/// <param name="limit">The maximum number of items to return in the same call. If null, uses API default.</param>
		/// <returns>HTML content with pagination metadata</returns>
		/// <exception cref="InvalidOperationException">Thrown when called on a non-V2 API instance</exception>
		/// <exception cref="ArgumentException">Thrown when threadIdOrSecretPath is null, empty, or invalid length</exception>
		/// <exception cref="QuipException">Thrown when the API returns an error</exception>
		/// <remarks>
		/// This method supports pagination for large documents. Use the next_cursor from the response 
		/// to retrieve additional pages until next_cursor is empty.
		/// Each cursor expires 30 minutes after creation.
		/// </remarks>
		public ThreadHtmlResponseV2 GetThreadHtmlV2(string threadIdOrSecretPath, string cursor = null, int? limit = null)
		{
			if (Version != QuipApiVersion.V2)
			{
				throw new InvalidOperationException("GetThreadHtmlV2 method requires API Version 2. Current version: " + Version);
			}

			if (string.IsNullOrEmpty(threadIdOrSecretPath))
			{
				throw new ArgumentException("Thread ID or secret path cannot be null or empty", nameof(threadIdOrSecretPath));
			}

			if (threadIdOrSecretPath.Length < 10 || threadIdOrSecretPath.Length > 32)
			{
				throw new ArgumentException("Thread ID or secret path must be between 10 and 32 characters", nameof(threadIdOrSecretPath));
			}

			var request = new RestRequest("threads/{threadIdOrSecretPath}/html", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("threadIdOrSecretPath", threadIdOrSecretPath);

			// Add optional query parameters
			if (!string.IsNullOrEmpty(cursor))
			{
				request.AddQueryParameter("cursor", cursor);
			}

			if (limit.HasValue)
			{
				request.AddQueryParameter("limit", limit.Value.ToString());
			}

			var response = ExecuteWithRateLimiting<ThreadHtmlResponseV2>(request);

			return response.Data;
		}

		/// <summary>
		/// Gets the complete HTML content of a Quip thread by automatically handling pagination
		/// </summary>
		/// <param name="threadIdOrSecretPath">The ID or secret path of the thread to get HTML content for</param>
		/// <param name="limit">The maximum number of items to return per page. If null, uses API default.</param>
		/// <returns>Complete HTML content as a single string</returns>
		/// <exception cref="InvalidOperationException">Thrown when called on a non-V2 API instance</exception>
		/// <exception cref="ArgumentException">Thrown when threadIdOrSecretPath is null, empty, or invalid length</exception>
		/// <exception cref="QuipException">Thrown when the API returns an error</exception>
		/// <remarks>
		/// This is a convenience method that automatically handles pagination to retrieve 
		/// the complete HTML content in a single call.
		/// </remarks>
		public string GetCompleteThreadHtmlV2(string threadIdOrSecretPath, int? limit = null)
		{
			var htmlParts = new List<string>();
			string cursor = null;

			do
			{
				var response = GetThreadHtmlV2(threadIdOrSecretPath, cursor, limit);
				
				if (!string.IsNullOrEmpty(response.html))
				{
					htmlParts.Add(response.html);
				}

				cursor = response.response_metadata?.next_cursor;
			}
			while (!string.IsNullOrEmpty(cursor));

			return string.Join("", htmlParts);
		}

		// Existing V1 methods continue...
		public Dictionary<string, Document> GetRecentByMembers(string[] memberIds, int count = 10)
		{
			var request = new RestRequest("threads/recent?count={count}&member_ids={member_ids}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("member_ids", String.Join(",", memberIds));
			request.AddUrlSegment("count", count.ToString());

			var response = ExecuteWithRateLimiting<Dictionary<string, Document>>(request);

			return response.Data;
		}

		public Document NewDocument(string title, string content, string[] member_ids = null,  DocumentType type = DocumentType.document, DocumentFormat format = DocumentFormat.markdown)
		{
			var request = new RestRequest("threads/new-document", Method.POST);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));

			if (content != null)
			{
				request.AddParameter("content", content);
			}

			request.AddParameter("type", type.ToString());
			request.AddParameter("format", format.ToString());

			if (title != null)
			{
				request.AddParameter("title", title);
			}

			if (member_ids != null)
			{
				request.AddParameter("member_ids", string.Join(",", member_ids));
			}

			var response = ExecuteWithRateLimiting<Document>(request);

			return response.Data;
		}

		public Document EditDocument(string id, string content, string section_id, DocumentFormat format = DocumentFormat.markdown, DocumentLocation location = DocumentLocation.Append)
		{
			var request = new RestRequest("threads/edit-document", Method.POST);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddParameter("thread_id", id);

			if (content != null)
			{
				request.AddParameter("content", content);
			}

			if (section_id != null)
			{
				request.AddParameter("section_id", section_id);
			}

			request.AddParameter("format", format.ToString());

			string[] locations = {
				"0: APPEND",
				"1: PREPEND",
				"2: AFTER_SECTION",
				"3: BEFORE_SECTION",
				"4: REPLACE_SECTION",
				"5: DELETE_SECTION"
			};

			request.AddParameter("location", locations[(int)location]);

			var response = ExecuteWithRateLimiting<Document>(request);

			return response.Data;
		}
	}
}
