using UACInject;
using UnityEngine;

namespace UACInjectTests
{
	class PropertyLogAttribute : ExecuteCodeAttribute
	{
		[CodeTarget]
		public static void Log(object value)
		{
			Debug.Log(value);
		}
	}
}
