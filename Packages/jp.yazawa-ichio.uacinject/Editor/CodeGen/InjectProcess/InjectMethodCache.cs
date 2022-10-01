using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace UACInject.CodeGen
{
	class InjectMethodCache
	{
		Logger m_Logger;
		Dictionary<string, List<InjectMethod>> m_Dic = new Dictionary<string, List<InjectMethod>>();

		public InjectMethodCache(Logger logger)
		{
			m_Logger = logger;
		}

		public bool Process(TypeDefinition callerType, MethodDefinition callerMethod, CodeInjectAttributeInfo attribute)
		{
			if (!m_Dic.TryGetValue(attribute.AttributeType.FullName, out var entry))
			{
				m_Dic[attribute.AttributeType.FullName] = entry = Create(callerType, attribute);
			}
			foreach (var info in entry)
			{
				if (info.Process(callerType, callerMethod, attribute))
				{
					return true;
				}
			}
			return false;
		}

		List<InjectMethod> Create(TypeDefinition callerType, CodeInjectAttributeInfo attrInfo)
		{
			var type = callerType.Module.ImportReference(attrInfo.AttributeType.Resolve()).Resolve();
			List<InjectMethod> infos = new List<InjectMethod>();
			foreach (var method in type.Methods)
			{
				var attr = method.CustomAttributes
					.Where(x => CodeTargetAttributeInfo.Is(x))
					.FirstOrDefault();
				if (attr == null)
				{
					continue;
				}
				var target = new CodeTargetAttributeInfo(attr);
				m_Logger.Debug($"add InjectMethod {type.FullName}:{method.Name} Priority:{target.Priority}");
				infos.Add(new InjectMethod(m_Logger, target, attrInfo.CodeType, callerType.Module.ImportReference(method).Resolve()));
			}
			infos.Sort((x, y) =>
			{
				if (y.Priority != x.Priority)
				{
					return y.Priority - x.Priority;
				}
				return y.ArgumentPriority - x.ArgumentPriority;
			});
			return infos;
		}
	}
}