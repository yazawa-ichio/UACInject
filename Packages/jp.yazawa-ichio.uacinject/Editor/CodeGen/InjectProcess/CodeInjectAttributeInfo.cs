using Mono.Cecil;
using System.Linq;

namespace UACInject.CodeGen
{

	public enum CodeType
	{
		Execute = 0,
		Scope = 1,
		ReturnCondition = 2,
	}

	class CodeInjectAttributeInfo
	{
		static readonly string s_InterfaceFullName = "UACInject.ICodeInjectAttribute";
		static readonly string s_ExecuteMethodFullName = "UACInject.ExecuteCodeAttribute";
		static readonly string s_ExecuteScopeFullName = "UACInject.ScopeCodeAttribute";
		static readonly string s_ReturnConditionFullName = "UACInject.ReturnConditionCodeAttribute";

		public static bool Is(CustomAttribute attribute)
		{
			return attribute.AttributeType.CanToCast(s_InterfaceFullName);
		}

		public TypeReference AttributeType { get; private set; }

		public CodeType CodeType { get; private set; } = CodeType.Execute;

		public string Method { get; set; } = "";

		public ConstructorParameterAttributeInfo[] Parameters { get; private set; }

		public CodeInjectAttributeInfo(CustomAttribute attr)
		{
			AttributeType = attr.AttributeType;
			Parameters = ConstructorParameterAttributeInfo.Get(attr).ToArray();

			if (attr.AttributeType.CanToCast(s_ExecuteMethodFullName))
			{
				CodeType = CodeType.Execute;
			}
			else if (attr.AttributeType.CanToCast(s_ExecuteScopeFullName))
			{
				CodeType = CodeType.Scope;
			}
			else if (attr.AttributeType.CanToCast(s_ReturnConditionFullName))
			{
				CodeType = CodeType.ReturnCondition;
			}
			foreach (var property in attr.Properties)
			{
				switch (property.Name)
				{
					case nameof(Method):
						Method = property.Argument.Value.ToString();
						break;
				}
			}
		}

	}
}