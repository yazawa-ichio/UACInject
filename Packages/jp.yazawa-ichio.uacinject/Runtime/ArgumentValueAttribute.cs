using System;

namespace UACInject
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class ArgumentValueAttribute : Attribute
	{
		public string Name { get; private set; }

		public object Value { get; private set; }

		public ArgumentValueAttribute(string name, string value)
		{
			Name = name;
			Value = value;
		}
		public ArgumentValueAttribute(string name, int value)
		{
			Name = name;
			Value = value;
		}
		public ArgumentValueAttribute(string name, float value)
		{
			Name = name;
			Value = value;
		}

	}
}