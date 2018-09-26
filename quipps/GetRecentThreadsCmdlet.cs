using libquip.threads;
using System.Collections.Generic;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipRecentThreads")]
	[OutputType(typeof(Dictionary<string, Document>))]
	public class GetRecentThreadsCmdlet: QuipApiCmdlet
	{
		[Parameter(ValueFromPipelineByPropertyName = true)]
		public int Count { get; set; }

		protected override void ProcessRecord()
		{
			QuipThread quipThread = new QuipThread(ApiKey);
			var response = Count > 0 ? quipThread.GetRecent(Count) : quipThread.GetRecent();
			WriteObject(response);
		}
	}
}
