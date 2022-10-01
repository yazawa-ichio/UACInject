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

		public int Priority { get; private set; }

		public CodeTargetAttributeInfo(CustomAttribute attribute)
		{
			foreach (var param in attribute.Properties)
			{
				switch (param.Name)
				{
					case nameof(Priority):
						Priority = (int)param.Argument.Value;
						break;
				}
			}
		}

	}
}