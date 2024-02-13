using libquip.threads;
using System;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipThread")]
	[OutputType(typeof(Thread))]
	public class GetThreadCmdlet : QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Id")]
		public string Id { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Url")]
		public Uri Url { get; set; }

		protected override void ProcessRecord()
		{
			Uri threadUri;

			if (Url != null)
			{
				// Remove trailing '/' to get thread id from url
				Id = Url.PathAndQuery.Substring(1);
			}
			else if (Uri.TryCreate(Id, UriKind.Absolute, out threadUri))
			{
				// Remove trailing '/' to get thread id from url
				Id = threadUri.PathAndQuery.Substring(1);
			}

			QuipThread quipThread = new QuipThread(ApiKey);
			var response = quipThread.GetThread(Id);
			WriteObject(response.thread);
		}
	}

	[Cmdlet(VerbsCommon.Get, "QuipThreadHtml")]
	[OutputType(typeof(Thread))]
	public class GetThreadHtmlCmdlet : QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Id")]
		public string Id { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Url")]
		public Uri Url { get; set; }

		protected override void ProcessRecord()
		{
			Uri threadUri;

			if (Url != null)
			{
				// Remove trailing '/' to get thread id from url
				Id = Url.PathAndQuery.Substring(1);
			}
			else if (Uri.TryCreate(Id, UriKind.Absolute, out threadUri))
			{
				// Remove trailing '/' to get thread id from url
				Id = threadUri.PathAndQuery.Substring(1);
			}

			QuipThread quipThread = new QuipThread(ApiKey);
			var response = quipThread.GetThread(Id);
			WriteObject(response.html);
		}
	}
}
