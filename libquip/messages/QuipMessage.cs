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
		public QuipMessage(string token)
			: base(token)
		{
		}

		public List<Message> GetMessagesForThread(string threadId)
		{
			var request = new RestRequest("messages/{thread_id}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("thread_id", threadId);

			var response = _client.Execute<List<Message>>(request);
			CheckResponse(response);

			return response.Data;
		}

		public Message AddMessageForThread(string threadId, MessageFrame frame, string content)
		{
			var request = new RestRequest("messages/new", Method.POST);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddParameter("thread_id", threadId);
			request.AddParameter("frame", frame.ToString());
			request.AddParameter("content", content);

			var response = _client.Execute<Message>(request);
			CheckResponse(response);

			return response.Data;
		}
	}
}
