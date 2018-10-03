using libquip.threads;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsData.Edit, "QuipDocument")]
	[OutputType(typeof(Thread))]
	public class EditQuipDocumentCmdlet : QuipApiCmdlet
	{
		[Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
		public string Id;

		[Parameter(ValueFromPipelineByPropertyName = true)]
		public string SectionId;

		[Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
		public string Content;

		[Parameter(ValueFromPipelineByPropertyName = true)]
		public DocumentFormat Format = DocumentFormat.markdown;

		[Parameter(ValueFromPipelineByPropertyName = true)]
		public DocumentLocation Location = DocumentLocation.Append;

		[Parameter(ValueFromPipelineByPropertyName = true)]
		public Thread Thread { get; set; }

		protected override void ProcessRecord()
		{
			QuipThread quipThread = new QuipThread(ApiKey);
			var result = quipThread.EditDocument(Thread != null ? Thread.id : Id,
				Content,
				SectionId,
				Format,
				Location);
			WriteObject(result.thread);
		}
	}
}
