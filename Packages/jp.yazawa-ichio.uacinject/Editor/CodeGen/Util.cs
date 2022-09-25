using Mono.Cecil;
using System.Linq;

namespace UACInject.CodeGen
{
	static class Util
	{
		public static bool HasInterface(this TypeReference type, string name)
		{
			var typeDef = type.Resolve();
			while (true)
			{
				if (typeDef.Interfaces.Any(x => x.InterfaceType.FullName == name))
				{
					return true;
				}
				if (typeDef.BaseType == null)
				{
					break;
				}
				typeDef = typeDef.BaseType.Resolve();
			}
			return false;
		}

		public static bool CanToCast(this TypeReference type, string name)
		{
			if (type.HasInterface(name))
			{
				return true;
			}
			if (type.FullName == name)
			{
				return true;
			}
			var typeDef = type.Resolve();
			while (true)
			{
				if (typeDef.FullName == name)
				{
					return true;
				}
				if (typeDef.BaseType == null)
				{
					break;
				}
				typeDef = typeDef.BaseType.Resolve();
			}
			return false;
		}


		public static bool IsVoid(this TypeReference type)
		{
			return type.FullName == "System.Void";
		}
	}

}