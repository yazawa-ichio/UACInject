using System;

namespace UACInject
{

	[AttributeUsage(AttributeTargets.Parameter)]
	public class CallerArgumentAttribute : Attribute
	{
		public string Name { get; private set; }

		public CallerArgumentAttribute(string name)
		{
			Name = name;
		}
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	public class CallerFieldAttribute : Attribute
	{
		public string Name { get; private set; }

		public CallerFieldAttribute(string name)
		{
			Name = name;
		}
	}
	[AttributeUsage(AttributeTargets.Parameter)]
	public class CallerInstanceAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	public class CallerMethodNameAttribute : Attribute
	{
	}

}