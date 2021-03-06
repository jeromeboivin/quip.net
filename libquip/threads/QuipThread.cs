﻿using RestSharp;
using System;
using System.Collections.Generic;

namespace libquip.threads
{
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
		public QuipThread(string token)
			:base(token)
		{
		}

		public Dictionary<string, Document> GetRecent(int count = 10)
		{
			var request = new RestRequest("threads/recent?count={count}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("count", count.ToString());

			var response = _client.Execute<Dictionary<string, Document>>(request);
			CheckResponse(response);

			return response.Data;
		}

		public Document GetThread(string id)
		{
			var request = new RestRequest("threads/{id}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("id", id);

			var response = _client.Execute<Document>(request);
			CheckResponse(response);

			return response.Data;
		}

		public Dictionary<string, Document> GetRecentByMembers(string[] memberIds, int count = 10)
		{
			var request = new RestRequest("threads/recent?count={count}&member_ids={member_ids}", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));
			request.AddUrlSegment("member_ids", String.Join(",", memberIds));
			request.AddUrlSegment("count", count.ToString());

			var response = _client.Execute<Dictionary<string, Document>>(request);
			CheckResponse(response);

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

			var response = _client.Execute<Document>(request);

			CheckResponse(response);

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

			var response = _client.Execute<Document>(request);

			CheckResponse(response);

			return response.Data;
		}
	}
}
