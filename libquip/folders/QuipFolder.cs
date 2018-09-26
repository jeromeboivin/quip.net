using RestSharp;
using System.Collections.Generic;

namespace libquip.folders
{
	public class QuipFoldersResponse
	{
		public Folder folder { get; set; }
		public List<string> member_ids { get; set; }
		public List<ChildFolder> children { get; set; }
	}

	public class Folder
	{
		public string title { get; set; }
		public string creator_id { get; set; }
		public string parent_id { get; set; }
		public string color { get; set; }
		public string id { get; set; }
		public long created_usec { get; set; }
		public long updated_usec { get; set; }
	}

	public class ChildFolder
	{
		public string thread_id { get; set; }
		public string folder_id { get; set; }
	}

	public class QuipFolder : QuipApi
	{
		public QuipFolder(string token) : base(token)
		{
		}

		public QuipFoldersResponse GetFolder(string id)
		{
			var request = new RestRequest("folders/" + id, Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));

			var response = _client.Execute<QuipFoldersResponse>(request);
			CheckResponse(response);

			return response.Data;
		}
	}
}
