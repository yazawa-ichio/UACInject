using Mono.Cecil;
using System.Linq;

namespace UACInject.CodeGen
{

	class InjectProcess
	{

		Logger m_Logger;
		InjectMethodCache m_InjectMethod;

		public InjectProcess(Logger logger)
		{
			m_Logger = logger;
			m_InjectMethod = new InjectMethodCache(logger);
		}

		public void Run(ModuleDefinition mainModule)
		{
			foreach (var typeDefinition in mainModule.Types)
			{
				Run(typeDefinition);
			}
		}

		void Run(TypeDefinition typeDefinition)
		{
			foreach (var method in typeDefinition.Methods)
			{
				if (method.CustomAttributes.Any(x => CodeInjectAttributeInfo.Is(x)))
				{
					Process(typeDefinition, method);
				}
			}
			foreach (var nest in typeDefinition.NestedTypes)
			{
				Run(nest);
			}
		}

		void Process(TypeDefinition typeDefinition, MethodDefinition method)
		{
			m_Logger.Debug($"Process InjectAttribute {method.FullName}");
			var attributes = method.CustomAttributes
				.Where(x => CodeInjectAttributeInfo.Is(x))
				.Select(x => new CodeInjectAttributeInfo(x))
				.ToArray();

			foreach (var attr in attributes.Reverse())
			{
				m_Logger.Debug($"Process AttributeType[{attr.AttributeType.FullName}]  TargetMethod[{attr.Method}]");
				if (!m_InjectMethod.Process(typeDefinition, method, attr))
				{
					m_Logger.Debug($"inject target not found. Type:{typeDefinition.FullName} Method:{method.Name} Id:{attr.Method}");
					continue;
				}
			}
		}

	}
}