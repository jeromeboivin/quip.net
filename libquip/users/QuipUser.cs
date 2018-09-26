using RestSharp;
using System.Collections.Generic;

namespace libquip.users
{
	public class QuipUsersResponse
	{
		public string name { get; set; }
		public string id { get; set; }
		public int affinity { get; set; }
		public string desktop_folder_id { get; set; }
		public string archive_folder_id { get; set; }
		public string starred_folder_id { get; set; }
		public string private_folder_id { get; set; }
		public List<string> shared_folder_ids { get; set; }
		public List<string> group_folder_ids { get; set; }
		public string profile_picture_url { get; set; }
	}

	public class QuipUser : QuipApi
	{
		public QuipUser(string token)
			: base(token)
		{
		}

		public QuipUsersResponse GetUser(string id)
		{
			var request = new RestRequest("users/" + id, Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));

			var response = _client.Execute<QuipUsersResponse>(request);
			CheckResponse(response);

			return response.Data;
		}

		public QuipUsersResponse GetCurrentUser()
		{
			var request = new RestRequest("users/current", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));

			var response = _client.Execute<QuipUsersResponse>(request);
			CheckResponse(response);

			return response.Data;
		}
	}
}
