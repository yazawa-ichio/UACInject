using Mono.Cecil;

namespace UACInject.CodeGen
{
	class ReflectionImporterProvider : IReflectionImporterProvider
	{
		public IReflectionImporter GetReflectionImporter(ModuleDefinition moduleDefinition)
		{
			return new ReflectionImporter(moduleDefinition);
		}
	}

}