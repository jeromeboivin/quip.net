using libquip.threads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace quipps
{
	[Cmdlet(VerbsCommon.New, "QuipDocument")]
	[OutputType(typeof(Thread))]
	public class NewQuipDocumentCmdlet : QuipApiCmdlet
	{
		[Parameter(ValueFromPipelineByPropertyName = true)]
		public string Title;

		[Parameter(ValueFromPipelineByPropertyName = true)]
		public string Content;

		[Parameter(ValueFromPipelineByPropertyName = true)]
		public string MemberIds = null;

		[Parameter(ValueFromPipelineByPropertyName = true)]
		public DocumentType Type = DocumentType.document;

		[Parameter(ValueFromPipelineByPropertyName = true)]
		public DocumentFormat Format = DocumentFormat.markdown;

		protected override void ProcessRecord()
		{
			QuipThread quipThread = new QuipThread(ApiKey);
			var result = quipThread.NewDocument(Title, 
				Content, 
				string.IsNullOrEmpty(MemberIds) ? null : MemberIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
				Type,
				Format);
			WriteObject(result.thread);
		}
	}
}
