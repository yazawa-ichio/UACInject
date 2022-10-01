using System;

namespace UACInject
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class ConstructorParameterAttribute : Attribute
	{
		public string Name { get; private set; }

		public ConstructorParameterAttribute() { }

		public ConstructorParameterAttribute(string name)
		{
			Name = name;
		}
	}

}