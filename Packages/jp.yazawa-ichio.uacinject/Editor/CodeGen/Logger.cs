using System;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace UACInject.CodeGen
{
	class Logger
	{
		List<DiagnosticMessage> m_Diagnostics;

		public Logger(List<DiagnosticMessage> diagnostics)
		{
			m_Diagnostics = diagnostics;
		}

		[System.Diagnostics.Conditional("UACI_CODEGEN_DEBUG")]
		public void Debug(string message)
		{
			m_Diagnostics.Add(new DiagnosticMessage
			{
				DiagnosticType = DiagnosticType.Warning,
				MessageData = "[DEBUG]" + message.Replace("\r\n", "").Replace("\n", ""),
			});
		}

		public void Log(DiagnosticMessage message)
		{
			m_Diagnostics.Add(message);
		}

		public void Warning(string message)
		{
			m_Diagnostics.Add(new DiagnosticMessage
			{
				DiagnosticType = DiagnosticType.Warning,
				MessageData = message.Replace("\r\n", "").Replace("\n", ""),
			});
		}

		public void Error(string message)
		{
			m_Diagnostics.Add(new DiagnosticMessage
			{
				DiagnosticType = DiagnosticType.Error,
				MessageData = message.Replace("\r\n", "").Replace("\n", ""),
			});
		}

		public void Exception(Exception message)
		{
			m_Diagnostics.Add(new DiagnosticMessage
			{
				DiagnosticType = DiagnosticType.Error,
				MessageData = message.ToString().Replace("\r\n", "").Replace("\n", ""),
				File = message.Source,
			});
		}


	}
}