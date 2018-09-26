using RestSharp;

namespace libquip
{
	public abstract class QuipApi
	{
		protected RestClient _client;
		protected string _token;

		public QuipApi(string token, int version = 1)
		{
			_token = token;
			_client = new RestClient($"https://platform.quip.com/{version.ToString()}/");
		}

		protected static void CheckResponse<T>(IRestResponse<T> response)
		{
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new QuipException(QuipError.FromJson(response.Content));
			}
		}
	}
}
