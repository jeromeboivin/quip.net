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
		/// <summary>
		/// Initializes a new instance of the QuipUser class with API version 1 (default)
		/// </summary>
		/// <param name="token">The authentication token</param>
		public QuipUser(string token)
			: base(token, QuipApiVersion.V1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the QuipUser class with the specified API version
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use</param>
		public QuipUser(string token, QuipApiVersion version)
			: base(token, version)
		{
		}

		/// <summary>
		/// Initializes a new instance of the QuipUser class with the specified API version (integer)
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use (1 or 2)</param>
		public QuipUser(string token, int version)
			: base(token, version)
		{
		}

		public QuipUsersResponse GetUser(string id)
		{
			var request = new RestRequest("users/" + id, Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));

			var response = ExecuteWithRateLimiting<QuipUsersResponse>(request);

			return response.Data;
		}

		public QuipUsersResponse GetCurrentUser()
		{
			var request = new RestRequest("users/current", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));

			var response = ExecuteWithRateLimiting<QuipUsersResponse>(request);

			return response.Data;
		}

		public List<QuipUsersResponse> GetContacts()
		{
			var request = new RestRequest("users/contacts", Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));

			var response = ExecuteWithRateLimiting<List<QuipUsersResponse>>(request);

			return response.Data;
		}
	}
}
