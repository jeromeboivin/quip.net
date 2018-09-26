using System;
using System.Management.Automation;
using System.Net;

namespace quipps
{
	public abstract class QuipApiCmdlet: Cmdlet
	{
		protected string ApiKey { get; private set; }
		private const string ApiKeyEnvVar = "QuipApiKey";

		protected override void BeginProcessing()
		{
			base.BeginProcessing();

			ApiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar, EnvironmentVariableTarget.User);

			if (string.IsNullOrEmpty(ApiKey))
			{
				throw new InvalidProgramException($"User environment variable '{ApiKeyEnvVar}' not set. Please go to https://quip.com/dev/token to generate your access token.");
			}

			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
		}
	}
}
