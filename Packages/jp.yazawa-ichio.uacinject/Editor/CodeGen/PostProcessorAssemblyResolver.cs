using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace UACInject.CodeGen
{
	class PostProcessorAssemblyResolver : IAssemblyResolver
	{
		string[] m_References;
		Dictionary<string, AssemblyDefinition> m_Cache = new Dictionary<string, AssemblyDefinition>();

		public PostProcessorAssemblyResolver(string[] references)
		{
			m_References = references;
		}

		public void Dispose() { }

		public AssemblyDefinition Resolve(AssemblyNameReference name) => Resolve(name, new ReaderParameters(ReadingMode.Deferred));

		public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			lock (m_Cache)
			{
				var fileName = FindFile(name);
				if (fileName == null)
				{
					return null;
				}

				var lastWriteTime = File.GetLastWriteTime(fileName);
				var cacheKey = $"{fileName}{lastWriteTime}";
				if (m_Cache.TryGetValue(cacheKey, out var result))
				{
					return result;
				}

				parameters.AssemblyResolver = this;

				var ms = MemoryStreamFor(fileName);
				var pdb = $"{fileName}.pdb";
				if (File.Exists(pdb))
				{
					parameters.SymbolStream = MemoryStreamFor(pdb);
				}

				var assemblyDefinition = AssemblyDefinition.ReadAssembly(ms, parameters);
				m_Cache.Add(cacheKey, assemblyDefinition);

				return assemblyDefinition;
			}
		}

		private string FindFile(AssemblyNameReference name)
		{
			var fileName = m_References.FirstOrDefault(r => Path.GetFileName(r) == $"{name.Name}.dll");
			if (fileName != null)
			{
				return fileName;
			}

			return m_References
				.Select(Path.GetDirectoryName)
				.Distinct()
				.Select(parentDir => Path.Combine(parentDir, $"{name.Name}.dll"))
				.FirstOrDefault(File.Exists);
		}

		private static MemoryStream MemoryStreamFor(string fileName)
		{
			return Retry(10, TimeSpan.FromSeconds(1), () =>
			{
				byte[] byteArray;
				using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				byteArray = new byte[fileStream.Length];
				var readLength = fileStream.Read(byteArray, 0, (int)fileStream.Length);
				if (readLength != fileStream.Length)
				{
					throw new InvalidOperationException("File read length is not full length of file.");
				}

				return new MemoryStream(byteArray);
			});
		}

		private static MemoryStream Retry(int retryCount, TimeSpan waitTime, Func<MemoryStream> func)
		{
			try
			{
				return func();
			}
			catch (IOException)
			{
				if (retryCount == 0)
				{
					throw;
				}

				Console.WriteLine($"Caught IO Exception, trying {retryCount} more times");
				Thread.Sleep(waitTime);

				return Retry(retryCount - 1, waitTime, func);
			}
		}
	}

}