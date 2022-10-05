using System;

namespace UACInject.Profiling
{
	[System.Diagnostics.Conditional("ENABLE_PROFILER")]
	[AttributeUsage(AttributeTargets.Method)]
	public class ProfileAttribute : Attribute
	{
		public ProfileAttribute() { }

		public ProfileAttribute(string name) { }
	}
}