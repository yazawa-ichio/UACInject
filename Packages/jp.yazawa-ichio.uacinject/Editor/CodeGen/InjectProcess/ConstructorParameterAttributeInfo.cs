using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace UACInject.CodeGen
{
	class ConstructorParameterAttributeInfo
	{
		static readonly string s_FullName = "UACInject.ConstructorParameterAttribute";

		public static IEnumerable<ConstructorParameterAttributeInfo> Get(CustomAttribute attribute)
		{
			for (int i = 0; i < attribute.Constructor.Parameters.Count; i++)
			{
				var param = attribute.Constructor.Parameters[i];
				var attr = param.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == s_FullName);
				if (attr != null)
				{
					var value = attribute.ConstructorArguments[i];
					yield return new ConstructorParameterAttributeInfo(param, attr, value);
				}
			}
		}

		ParameterDefinition m_Param;
		CustomAttribute m_Attribute;
		CustomAttributeArgument m_Argument;

		public string Name { get; private set; }
		public object Value => m_Argument.Value;

		public ConstructorParameterAttributeInfo(ParameterDefinition param, CustomAttribute attribute, CustomAttributeArgument argument)
		{
			m_Param = param;
			m_Attribute = attribute;
			if (attribute.ConstructorArguments.Any())
			{
				Name = (string)attribute.ConstructorArguments[0].Value;
			}
			else
			{
				Name = param.Name;
			}
			m_Argument = argument;
		}

	}
}