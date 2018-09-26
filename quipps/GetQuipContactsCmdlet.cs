using libquip.users;
using System.Collections.Generic;
using System.Management.Automation;

namespace quipps
{
	[Cmdlet(VerbsCommon.Get, "QuipContacts")]
	[OutputType(typeof(List<QuipUsersResponse>))]
	public class GetQuipContactsCmdlet: QuipApiCmdlet
	{
		protected override void ProcessRecord()
		{
			QuipUser quipUser = new QuipUser(ApiKey);
			var response = quipUser.GetContacts();
			WriteObject(response);
		}
	}
}
