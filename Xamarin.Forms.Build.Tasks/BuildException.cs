using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.Build.Tasks
{
	[Serializable]
	class BuildException : Exception
	{
		public BuildExceptionCode Code { get; private set; }

		public IXmlLineInfo XmlInfo { get; private set; }

		public string[] MessageArgs { get; private set; }

		public override string HelpLink { get => Code.HelpLink; set => base.HelpLink = value; }

		public BuildException(BuildExceptionCode code, IXmlLineInfo xmlInfo, Exception innerException, params object[] args)
			: base(FormatMessage(code, xmlInfo, args), innerException)
		{
			Code = code;
			XmlInfo = xmlInfo;
			MessageArgs = args?.Select(a=>a?.ToString()).ToArray();
		}

		protected BuildException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
		}

		static string FormatMessage(BuildExceptionCode code, IXmlLineInfo xmlinfo, object[] args)
		{
			var message = string.Format(ErrorMessages.ResourceManager.GetString(code.ErrorMessage), args);
			var ecode = code.Code;
			var position = xmlinfo == null || !xmlinfo.HasLineInfo() ? "" : $"({xmlinfo.LineNumber},{xmlinfo.LinePosition})";

			return $"{position} : XamlC error {ecode} : {message}";
		}
	}

	class BuildExceptionCode
	{
		public static BuildExceptionCode Conversion =			new BuildExceptionCode("XFC0001", nameof(Conversion), "");
		public static BuildExceptionCode TypeResolution =		new BuildExceptionCode("XFC0002", nameof(TypeResolution), "");
		public static BuildExceptionCode PropertyResolution =	new BuildExceptionCode("XFC0003", nameof(PropertyResolution), "");
		public static BuildExceptionCode MissingEventHandler =	new BuildExceptionCode("XFC0004", nameof(MissingEventHandler), "");
		public string Code { get; private set; }
		public string ErrorMessage { get; private set; }
		public string HelpLink { get; private set; }

		private BuildExceptionCode()
		{ }
		private BuildExceptionCode(string code, string errorMessage, string helpLink)
		{
			Code = code;
			ErrorMessage = errorMessage;
			HelpLink = helpLink;
		}
	}
}
