using Mono.Cecil;

namespace UACInject.CodeGen
{
	class CodeTargetAttributeInfo
	{
		static readonly string s_InterfaceFullName = "UACInject.CodeTargetAttribute";

		public static bool Is(CustomAttribute attribute)
		{
			return attribute.AttributeType.CanToCast(s_InterfaceFullName);
		}

	}
}