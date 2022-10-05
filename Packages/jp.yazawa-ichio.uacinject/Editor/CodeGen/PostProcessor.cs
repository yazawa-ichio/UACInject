using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace UACInject.CodeGen
{

	class PostProcessor : ILPostProcessor
	{
		private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

		public override ILPostProcessor GetInstance()
		{
			return this;
		}

		public override bool WillProcess(ICompiledAssembly compiledAssembly)
		{
			var referenceDlls = compiledAssembly.References
					.Select(Path.GetFileNameWithoutExtension);

			return referenceDlls.Any(x => x == "UACInject");
		}

		public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
		{
			if (!WillProcess(compiledAssembly))
			{
				return null;
			}
			m_Diagnostics.Clear();
			var logger = new Logger(m_Diagnostics);
			AssemblyDefinition assemblyDefinition = null;
			try
			{

				assemblyDefinition = AssemblyDefinitionFor(compiledAssembly);
				if (assemblyDefinition == null)
				{
					return null;
				}

				var mainModule = assemblyDefinition.MainModule;
				if (mainModule == null)
				{
					return null;
				}
				new ProfileInjectProcess(logger).Run(mainModule);
				new InjectProcess(logger).Run(mainModule);
			}
			catch (Exception error)
			{
				logger.Exception(error);
			}

			var pe = new MemoryStream();
			var pdb = new MemoryStream();

			var writerParameters = new WriterParameters
			{
				SymbolWriterProvider = new PortablePdbWriterProvider(),
				SymbolStream = pdb,
				WriteSymbols = true
			};
			try
			{
				assemblyDefinition.Write(pe, writerParameters);
			}
			catch (Exception error)
			{
				logger.Exception(error);
			}

			return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), m_Diagnostics);
		}

		AssemblyDefinition AssemblyDefinitionFor(ICompiledAssembly compiledAssembly)
		{
			var readerParameters = new ReaderParameters
			{
				SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData.ToArray()),
				SymbolReaderProvider = new PortablePdbReaderProvider(),
				AssemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly.References),
				ReflectionImporterProvider = new ReflectionImporterProvider(),
				ReadingMode = ReadingMode.Immediate
			};

			var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData.ToArray());
			return AssemblyDefinition.ReadAssembly(peStream, readerParameters);
		}
	}

}