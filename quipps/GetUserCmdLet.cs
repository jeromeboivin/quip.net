using libquip.users;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipUser")]
	[OutputType(typeof(QuipUsersResponse))]
	public class GetUserCmdLet: QuipApiCmdlet
	{
		[Parameter(ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
		public string Id { get; set; }

		protected override void ProcessRecord()
		{
			QuipUser quipUser = new QuipUser(ApiKey);
			var response = string.IsNullOrEmpty(Id) ? quipUser.GetCurrentUser() : quipUser.GetUser(Id);
			WriteObject(response);
		}
	}
}
