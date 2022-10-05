using NUnit.Framework;
using UACInject.Profiling;
using UnityEngine;

namespace UACInjectTests
{
	class ProfileTest
	{
		class Obj : UnityEngine.ScriptableObject
		{
			[Profile]
			static void ObjStaticTest() { }

			[Profile]
			public void ObjTest()
			{
				ObjStaticTest();
			}
		}

		[Profile]
		static void StaticTest1() { }

		[Profile("StaticTest2")]
		static void StaticTest2() { }

		[Profile]
		int Test1() => 5;

		[Profile("Test2BB")]
		void Test2() { }


		[Test]
		public void Test()
		{
			StaticTest1();
			StaticTest2();
			Test1();
			Test2();

			var obj = ScriptableObject.CreateInstance<Obj>();
			obj.ObjTest();
		}

	}
}
