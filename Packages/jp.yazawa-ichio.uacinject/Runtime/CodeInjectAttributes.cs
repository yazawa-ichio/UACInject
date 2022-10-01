using System;

namespace UACInject
{
	interface ICodeInjectAttribute { }

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public abstract class ExecuteCodeAttribute : Attribute, ICodeInjectAttribute
	{
		public string Method { get; set; }
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public abstract class ScopeCodeAttribute : Attribute, ICodeInjectAttribute
	{
		public string Method { get; set; }
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public abstract class ReturnConditionCodeAttribute : Attribute, ICodeInjectAttribute
	{
		public string Method { get; set; }
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class CodeTargetAttribute : Attribute
	{
		public int Priority { get; set; }
	}

}