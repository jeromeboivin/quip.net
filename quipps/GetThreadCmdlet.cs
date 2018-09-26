using libquip.threads;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipThread")]
	[OutputType(typeof(Document))]
	public class GetThreadCmdlet : QuipApiCmdlet
	{
		[Parameter(ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
		public string Id { get; set; }

		protected override void ProcessRecord()
		{
			QuipThread quipThread = new QuipThread(ApiKey);
			var response = quipThread.GetThread(Id);
			WriteObject(response);
		}
	}
}
