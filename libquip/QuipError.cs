using Newtonsoft.Json;

namespace libquip
{
	public class QuipError
	{
		public string error { get; set; }
		public int error_code { get; set; }
		public string error_description { get; set; }

		public static QuipError FromJson(string errorString)
		{
			return JsonConvert.DeserializeObject<QuipError>(errorString);
		}
	}
}
