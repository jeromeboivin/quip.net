using libquip.messages;
using System.Collections.Generic;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipRecentMessages")]
	[OutputType(typeof(List<Message>))]
	public class GetRecentMessagesCmdlet : QuipApiCmdlet
	{
		[Parameter(ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
		public string ThreadId { get; set; }

		protected override void ProcessRecord()
		{
			QuipMessage quipMessage = new QuipMessage(ApiKey);
			var response = quipMessage.GetMessagesForThread(ThreadId);
			WriteObject(response);
		}
	}
}
