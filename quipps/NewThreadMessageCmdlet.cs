using libquip.messages;
using libquip.threads;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.New, "QuipThreadMessage")]
	[OutputType(typeof(Message))]
	public class NewThreadMessageCmdlet: QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByThreadId", Mandatory = true)]
		public string ThreadId { get; set; }

		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "ByThread", Mandatory = true)]
		public Thread Thread { get; set; }

		[Parameter(Position = 1, ValueFromPipelineByPropertyName = true, Mandatory = true)]
		public string Content { get; set; }

		[Parameter(Position = 2, ValueFromPipelineByPropertyName = true, Mandatory = true)]
		public MessageFrame Frame { get; set; }

		protected override void ProcessRecord()
		{
			QuipMessage quipMessage = new QuipMessage(ApiKey);

			if (Thread != null)
			{
				ThreadId = Thread.id;
			}

			var response = quipMessage.AddMessageForThread(ThreadId, Frame, Content);
			WriteObject(response);
		}
	}
}
