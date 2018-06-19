using System;
using System.Runtime.Serialization;

namespace libquip
{
	[Serializable]
	public class QuipException : Exception
	{
		public QuipError QuipError { get; private set; }

		public QuipException(QuipError quipError)
			:base(quipError.error_description)
		{
			QuipError = quipError;
		}

		public QuipException(string message) : base(message)
		{
		}

		public QuipException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected QuipException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}