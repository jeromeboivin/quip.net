using libquip.folders;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipFolder")]
	[OutputType(typeof(QuipFoldersResponse))]
	public class GetQuipFolderCmdlet: QuipApiCmdlet
	{
		[Parameter(ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
		public string Id { get; set; }

		protected override void ProcessRecord()
		{
			QuipFolder quipFolder = new QuipFolder(ApiKey);
			var response = quipFolder.GetFolder(Id);
			WriteObject(response);
		}
	}
}
