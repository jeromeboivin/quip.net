using libquip.messages;
using libquip.threads;
using System.Collections.Generic;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipThreadMessages")]
	[OutputType(typeof(List<Message>))]
	public class GetThreadMessagesCmdlet : QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByThreadId")]
		public string ThreadId { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "ByThread")]
		public Thread Thread { get; set; }

		protected override void ProcessRecord()
		{
			QuipMessage quipMessage = new QuipMessage(ApiKey);

			if (Thread != null)
			{
				ThreadId = Thread.id;
			}

			var response = quipMessage.GetMessagesForThread(ThreadId);
			WriteObject(response);
		}
	}
}
