using libquip.threads;
using System.Collections.Generic;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipRecentThreads")]
	[OutputType(typeof(Dictionary<string, Document>))]
	public class GetRecentThreadsCmdlet: QuipApiCmdlet
	{
		protected override void ProcessRecord()
		{
			QuipThread quipThread = new QuipThread(ApiKey);
			var response = quipThread.GetRecent();
			WriteObject(response);
		}
	}
}
