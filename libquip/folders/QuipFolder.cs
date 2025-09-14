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
		/// <summary>
		/// Initializes a new instance of the QuipFolder class with API version 1 (default)
		/// </summary>
		/// <param name="token">The authentication token</param>
		public QuipFolder(string token) : base(token, QuipApiVersion.V1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the QuipFolder class with the specified API version
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use</param>
		public QuipFolder(string token, QuipApiVersion version) : base(token, version)
		{
		}

		/// <summary>
		/// Initializes a new instance of the QuipFolder class with the specified API version (integer)
		/// </summary>
		/// <param name="token">The authentication token</param>
		/// <param name="version">The API version to use (1 or 2)</param>
		public QuipFolder(string token, int version) : base(token, version)
		{
		}

		public QuipFoldersResponse GetFolder(string id)
		{
			var request = new RestRequest("folders/" + id, Method.GET);
			request.AddHeader("Authorization", string.Format("Bearer {0}", _token));

			var response = ExecuteWithRateLimiting<QuipFoldersResponse>(request);

			return response.Data;
		}
	}
}
