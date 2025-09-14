using RestSharp;
using System.Collections.Generic;

namespace libquip.messages
{
	public enum MessageFrame
	{
		bubble,
		card,
		line
	}

	public class Message
	{
		public string author_id { get; set; }
		public string id { get; set; }
		public long created_usec { get; set; }
		public string text { get; set; }
		public Annotation annotation { get; set; }
		public string author_name { get; set; }
		public List<string> mention_user_ids { get; set; }
	}

	public class Annotation
	{
		public string id { get; set; }
		public List<string> highlight_section_ids { get; set; }
	}

	public class QuipMessage : QuipApi
	{
		/// <summary>
		/// Initializes a new instance of the QuipMessage class with API version 1 (default)
		/// </summary>
		/// <param name="token">The authentication token</param>
		public QuipMessage(string token)
			: base(token, QuipApiVersion.V1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the QuipMessage class with the specified API version
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use</param>
		public QuipMessage(string token, QuipApiVersion version)
			: base(token, version)
		{
		}

		/// <summary>
		/// Initializes a new instance of the QuipMessage class with the specified API version (integer)
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use (1 or 2)</param>
		public QuipMessage(string token, int version)
			: base(token, version)
		{
		}

		public List<Message> GetMessagesForThread(string threadId)
		{
			var request = new RestRequest("messages/{thread_id}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("thread_id", threadId);

			var response = ExecuteWithRateLimiting<List<Message>>(request);

			return response.Data;
		}

		public Message AddMessageForThread(string threadId, MessageFrame frame, string content)
		{
			var request = new RestRequest("messages/new", Method.POST);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddParameter("thread_id", threadId);
			request.AddParameter("frame", frame.ToString());
			request.AddParameter("content", content);

			var response = ExecuteWithRateLimiting<Message>(request);

			return response.Data;
		}
	}
}
