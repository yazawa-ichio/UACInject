using Mono.Cecil;

namespace UACInject.CodeGen
{
	class ProfileAttributeInfo
	{
		static readonly string s_FullName = "UACInject.Profiling.ProfileAttribute";

		public static bool Is(CustomAttribute attribute)
		{
			return attribute.AttributeType.CanToCast(s_FullName);
		}

		public string Name { get; set; }

		public ProfileAttributeInfo(MethodDefinition method, CustomAttribute attribute)
		{
			if (attribute.ConstructorArguments.Count == 1)
			{
				Name = attribute.ConstructorArguments[0].Value.ToString();
			}
			if (string.IsNullOrEmpty(Name))
			{
				Name = method.Name;
			}
		}
	}
}