using NUnit.Framework;
using UACInject;

namespace UACInjectTests
{

	public class ArgumentMatchTest
	{

		class TestExecute1Attribute : ExecuteCodeAttribute
		{
			public static string LastRun { get; [PropertyLog] set; }

			public TestExecute1Attribute() { }

			public TestExecute1Attribute([ConstructorParameter("arg1")] int arg1) { }

			[CodeTarget]
			static void Run(int arg1)
			{
				LastRun = $"Run(int {arg1})";
			}

			[CodeTarget]
			static void RunII(int arg1, int arg2)
			{
				LastRun = $"Run(int {arg1}, int {arg2})";
			}

			[CodeTarget]
			static void Run([CallerField("m_FieldValue")] string field1, int arg1, int arg2)
			{
				LastRun = $"Run(string {field1}, int {arg1}, int {arg2})";
			}

			[CodeTarget]
			public static void RunIF(int arg1, float arg2)
			{
				LastRun = $"Run(int {arg1}, float {arg2})";
			}

			[CodeTarget]
			static void RunIS(int arg1, string arg2)
			{
				LastRun = $"Run(int {arg1}, string {arg2})";
			}

			[CodeTarget]
			public static void RunLong(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9, int arg10, int arg11, int arg12, int arg13, int arg14, int arg15, int arg16, int arg17)
			{
				LastRun = $"Run(int {arg1}, int {arg2},*, int {arg16}, int {arg17})";
			}

		}

		class TestExecute2Attribute : ExecuteCodeAttribute
		{
			public static object LastValue;

			public TestExecute2Attribute([ConstructorParameter("b")] bool valueB) { }
			public TestExecute2Attribute([ConstructorParameter] byte valueB) { }
			public TestExecute2Attribute([ConstructorParameter] short valueS) { }
			public TestExecute2Attribute([ConstructorParameter] long valueL) { }
			public TestExecute2Attribute([ConstructorParameter] ulong valueUL) { }
			public TestExecute2Attribute([ConstructorParameter] string valueS) { }

			[CodeTarget]
			static void Run(bool b)
			{
				LastValue = b;
			}

			[CodeTarget]
			static void Run(byte valueB)
			{
				LastValue = valueB;
			}

			[CodeTarget]
			static void Run(short valueS)
			{
				LastValue = valueS;
			}

			[CodeTarget]
			static void Run(long valueL)
			{
				LastValue = valueL;
			}

			[CodeTarget]
			static void Run(ulong valueUL)
			{
				LastValue = valueUL;
			}

			[CodeTarget]
			static void Run(string valueS)
			{
				LastValue = valueS;
			}

		}

		string m_FieldValue;

		[TestExecute1]
		void RunCode1(int arg1)
		{
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1})");
		}

		[TestExecute1]
		void RunCode1_Dummy(int arg1, int _arg2)
		{
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1})");
			Assert.AreNotEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1}, int {_arg2})", "マッチングされない");
		}

		[TestExecute1]
		void RunCode2II(int arg1, int arg2)
		{
			Assert.AreNotEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1}, int {arg2})", "引数が少ない方は優先されない");
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(string {m_FieldValue}, int {arg1}, int {arg2})", "引数が多いほうが優先される");
		}

		[TestExecute1(Method = "RunII")]
		void RunCode2II_Target(int arg1, int arg2)
		{
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1}, int {arg2})", "明示的に関数を指定");
			Assert.AreNotEqual(TestExecute1Attribute.LastRun, $"Run(string {m_FieldValue}, int {arg1}, int {arg2})");
		}

		[TestExecute1]
		void RunCode2IF(int arg1, float arg2)
		{
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1}, float {arg2})");
		}

		void RunCode2IFMock(int arg1, float arg2)
		{
			TestExecute1Attribute.RunIF(arg1, arg2);
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1}, float {arg2})");
		}


		[TestExecute1(arg1: 10)]
		void RunCode2IF_FixedArg(float arg2)
		{
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {10}, float {arg2})");
		}

		[TestExecute1]
		void RunCode2IS(int arg1, string arg2)
		{
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1}, string {arg2})");
		}

		[TestExecute1]
		void RunCodeLong(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9, int arg10, int arg11, int arg12, int arg13, int arg14, int arg15, int arg16, int arg17)
		{
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1}, int {arg2},*, int {arg16}, int {arg17})");
		}

		void RunCodeLongMock(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9, int arg10, int arg11, int arg12, int arg13, int arg14, int arg15, int arg16, int arg17)
		{
			TestExecute1Attribute.RunLong(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17);
			Assert.AreEqual(TestExecute1Attribute.LastRun, $"Run(int {arg1}, int {arg2},*, int {arg16}, int {arg17})");
		}

		[TestCase(0, 0)]
		[TestCase(int.MaxValue, int.MaxValue)]
		[TestCase(int.MinValue, int.MinValue)]
		[TestCase(8, 3)]
		[TestCase(22, 27)]
		[TestCase(200, 400)]
		[TestCase(500, 300)]
		public void TestCode1(int arg1, int arg2)
		{
			RunCode1(arg1);
			RunCode1_Dummy(arg1, arg2);

		}

		[TestCase(0, 0)]
		[TestCase(int.MaxValue, int.MaxValue)]
		[TestCase(int.MinValue, int.MinValue)]
		[TestCase(8, 3)]
		[TestCase(22, 27)]
		[TestCase(200, 400)]
		[TestCase(500, 300)]
		public void TestCode2II(int arg1, int arg2)
		{
			m_FieldValue = $"({arg1}, {arg2})";
			RunCode2II(arg1, arg2);
			RunCode2II_Target(arg1, arg2);
		}

		[TestCase(0, 0)]
		[TestCase(int.MaxValue, float.MaxValue)]
		[TestCase(int.MinValue, float.MinValue)]
		[TestCase(8, 3.2f)]
		[TestCase(22, 27.2f)]
		[TestCase(200, 400.2f)]
		[TestCase(500, 300.6f)]
		public void TestCode2IF(int arg1, float arg2)
		{
			RunCode2IF(arg1, arg2);
			RunCode2IF_FixedArg(arg2);
			RunCode2IF_FixedArg((float)arg1);
		}

		[TestCase(0, null)]
		[TestCase(int.MaxValue, "vv")]
		[TestCase(int.MinValue, "test")]
		[TestCase(8, "bb")]
		public void TestCode2IS(int arg1, string arg2)
		{
			RunCode2IS(arg1, arg2);
		}

		[Test]
		public void TestCodeLong()
		{
			RunCodeLong(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17);
			RunCodeLong(100, 2222, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 1500, 1724);
			RunCodeLong(102, 5, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, -15, -17);
		}


		[TestExecute2(true)]
		void ConstructorParameterBool() { }

		[TestExecute2(1)]
		void ConstructorParameterByte() { }

		[TestExecute2(1002)]
		void ConstructorParameterShort() { }

		[TestExecute2(long.MaxValue - 1)]
		void ConstructorParameterLong() { }

		[TestExecute2(ulong.MaxValue - 1)]
		void ConstructorParameterULong() { }

		[TestExecute2("TEST")]
		void ConstructorParameterString() { }

		[Test]
		public void TestConstructorParameter()
		{
			ConstructorParameterBool();
			Assert.AreEqual(true, TestExecute2Attribute.LastValue);

			ConstructorParameterByte();
			Assert.AreEqual(1, TestExecute2Attribute.LastValue);

			ConstructorParameterShort();
			Assert.AreEqual(1002, TestExecute2Attribute.LastValue);

			ConstructorParameterLong();
			Assert.AreEqual(long.MaxValue - 1, TestExecute2Attribute.LastValue);

			ConstructorParameterULong();
			Assert.AreEqual(ulong.MaxValue - 1, TestExecute2Attribute.LastValue);

			ConstructorParameterString();
			Assert.AreEqual("TEST", TestExecute2Attribute.LastValue);
		}
	}

}
