using NUnit.Framework;
using UACInject;
using UnityEngine;

namespace UACInjectTests
{
	public class ScopeTest
	{
		class StructScopeAttribute : ScopeCodeAttribute
		{
			public static int Count;

			internal struct Scope : System.IDisposable
			{
				int m_Count;

				public Scope(int count)
				{
					m_Count = count;
					Debug.Log("Scope" + m_Count);
				}

				void System.IDisposable.Dispose()
				{
					Debug.Log("Dispose" + m_Count);
					Count += m_Count;
				}
			}

			[CodeTarget]
			internal static Scope Run() => new Scope(1);

			[CodeTarget]
			internal static Scope Run(int count) => new Scope(count);

		}

		class ClassScopeAttribute : ScopeCodeAttribute
		{
			public static int Count;

			internal class Scope : System.IDisposable
			{
				int m_Count;

				public Scope(int count)
				{
					m_Count = count;
				}

				void System.IDisposable.Dispose()
				{
					Count += m_Count;
				}
			}

			[CodeTarget]
			internal static Scope Run() => new Scope(1);

			[CodeTarget]
			internal static Scope Run(int count) => new Scope(count);

		}


		[StructScope]
		void StructScope(int count)
		{
			if (count > 1)
			{
				return;
			}
			else
			{
				Debug.Log(count);
			}
		}

		void StructScopeMock_(int count)
		{
			if (count > 1)
			{
				return;
			}
			else
			{
				Debug.Log(count);
			}
		}

		void StructScopeMock(int count)
		{
			using (StructScopeAttribute.Run(count))
			{
				if (count > 1)
				{
					return;
				}
				else
				{
					Debug.Log(count);
				}
			}
		}

		[ClassScope]
		void ClassScope()
		{
		}

		[StructScope]
		[ClassScope]
		void NestScope(int count)
		{

		}

		[StructScope]
		void Error0(int count)
		{
		}

		void Error0Mock_(int count)
		{
		}

		void Error0Mock(int count)
		{
			using (StructScopeAttribute.Run(count))
			{
			}
		}

		[StructScope]
		void Error1(int count)
		{
			try
			{
				throw new System.Exception("Error");
			}
			finally
			{
				Debug.Log("Error:" + count);
			}
		}

		void Error1Mock(int count)
		{
			using (StructScopeAttribute.Run(count))
			{
				try
				{
					throw new System.Exception("Error");
				}
				finally
				{
					Debug.Log("Error:" + count);
				}
			}
		}

		void Error1Mock_(int count)
		{
			try
			{
				throw new System.Exception("Error");
			}
			finally
			{
				Debug.Log("Error:" + count);
			}
		}

		[StructScope]
		void Error2(int count)
		{
			throw new System.Exception("Error");
		}

		void Error2Mock(int count)
		{
			using (StructScopeAttribute.Run(count))
			{
				throw new System.Exception("Error");
			}
		}

		[StructScope]
		void Error3(int count)
		{
			if (count == 0)
			{
				return;
			}
			throw new System.Exception("Error");
		}

		void Error3Mock_(int count)
		{
			if (count == 0)
			{
				return;
			}
			throw new System.Exception("Error");
		}

		void Error3Mock(int count)
		{
			using (StructScopeAttribute.Run(count))
			{
				if (count == 0)
				{
					return;
				}
				throw new System.Exception("Error");
			}
		}

		[ClassScope]
		int Error4(int count)
		{
			if (count == 0)
			{
				return 5;
			}
			throw new System.Exception("Error");
		}

		int Error4Mock(int count)
		{
			using (ClassScopeAttribute.Run(count))
			{
				if (count == 0)
				{
					return 5;
				}
				throw new System.Exception("Error");
			}
		}

		[ClassScope]
		int Error5(int count)
		{
			throw new System.Exception("Error");
		}

		int Error5Mock(int count)
		{
			using (ClassScopeAttribute.Run(count))
			{
				throw new System.Exception("Error");
			}
		}

		void InnnerError()
		{
			throw new System.Exception("Error");
		}

		[ClassScope]
		int Error6(int count)
		{
			InnnerError();
			return 10;
		}

		int Error6Mock(int count)
		{
			using (ClassScopeAttribute.Run(count))
			{
				InnnerError();
				return 10;
			}
		}

		[StructScope]
		void Error7(int count)
		{
			try
			{
				throw new System.Exception("Error");
			}
			catch
			{
				throw;
			}
		}

		void Error7Mock(int count)
		{
			using (StructScopeAttribute.Run(count))
			{
				try
				{
					throw new System.Exception("Error");
				}
				catch
				{
					throw;
				}
			}
		}

		int m_Dummy1;
		int m_Dummy2;
		int m_Dummy3;

		[ClassScope]
		void Finally1(bool test)
		{
			try
			{
				if (test)
				{
					return;
				}
				if (m_Dummy1 == 1)
				{
					if (m_Dummy2 == 1)
					{

					}
					else
					{
						return;
					}
					if (m_Dummy2 == m_Dummy3)
					{
						if (m_Dummy3 > 5)
						{
							return;
						}
					}
				}

				Debug.Log(test);
			}
			finally
			{
				Debug.Log("finally:" + test);
			}
		}

		void Finally1Mock(bool test)
		{
			using (ClassScopeAttribute.Run())
			{
				try
				{
					if (test)
					{
						return;
					}
					if (m_Dummy1 == 1)
					{
						if (m_Dummy2 == 1)
						{

						}
						else
						{
							return;
						}
						if (m_Dummy2 == m_Dummy3)
						{
							if (m_Dummy3 > 5)
							{
								return;
							}
						}
					}

					Debug.Log(test);
				}
				finally
				{
					Debug.Log("finally:" + test);
				}
			}
		}

		[ClassScope]
		void Finally2(bool test)
		{
			try
			{
				Debug.Log(test);
			}
			finally
			{
				Debug.Log("finally:" + test);
			}
		}

		void Finally2Mock(bool test)
		{
			using (ClassScopeAttribute.Run())
			{
				try
				{
					Debug.Log(test);
				}
				finally
				{
					Debug.Log("finally:" + test);
				}
			}
		}

		[StructScope] // 1
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope] // 10
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope] // 20
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope] // 30
		void Nest()
		{
			int count1 = 0;
			int count2 = 0;
			int count3 = 0;
			int count4 = 0;
			int count5 = 0;
			int count6 = 0;
			int count7 = 0;
			int count8 = 0;
			int count9 = 0;
			int count10 = 0;
		}

		[StructScope] // 1
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope]
		[StructScope]
		[ClassScope] // 10
		void NestBB()
		{
			Debug.Log("");
		}

		void NestBBMock()
		{
			using (ClassScopeAttribute.Run())
			{
				using (StructScopeAttribute.Run())
				{
					using (ClassScopeAttribute.Run())
					{
						Debug.Log("");
					}
				}
			}

		}
		void NestMock()
		{
			using (ClassScopeAttribute.Run())
			{
				using (StructScopeAttribute.Run())
				{
					using (ClassScopeAttribute.Run())
					{
						int count1 = 0;
						int count2 = 0;
						int count3 = 0;
						int count4 = 0;
						int count5 = 0;
						int count6 = 0;
						int count7 = 0;
						int count8 = 0;
						int count9 = 0;
						int count10 = 0;
					}
				}
			}

		}

		[StructScope]
		string Get1(string text)
		{
			return "get:" + text;
		}


		[StructScope]
		long Get2(int num, bool ret)
		{
			if (ret)
			{
				if (num > 10)
				{
					if (num % 2 == 0)
					{
						return num + 1;
					}
				}
				else
				{
					return num * 2;
				}
			}
			return -5555;
		}

		[StructScope]
		long Get3(int num, bool ret)
		{
			try
			{
				if (ret)
				{
					if (num > 10)
					{
						try
						{
							if (num % 2 == 0)
							{
								return num + 1;
							}
						}
						finally
						{
							num += 5;
						}
						return num;
					}
					else
					{
						return num * 2;
					}
				}
				return -5555;
			}
			catch
			{
				throw;
			}
		}

		long Get3Mock(int num, bool ret)
		{
			using (StructScopeAttribute.Run())
			{
				try
				{
					if (ret)
					{
						if (num > 10)
						{
							try
							{
								if (num % 2 == 0)
								{
									return num + 1;
								}
							}
							finally
							{
								num += 5;
							}
							return num;
						}
						else
						{
							return num * 2;
						}
					}
					return -5555;
				}
				catch
				{
					throw;
				}
			}
		}

		[Test]
		public void StructScopeTest()
		{
			var prev = StructScopeAttribute.Count;
			StructScope(1);
			Assert.AreEqual(prev + 1, StructScopeAttribute.Count);
			StructScope(5);
			Assert.AreEqual(prev + 6, StructScopeAttribute.Count);
		}


		[Test]
		public void ClassScopeTest()
		{
			var prev = ClassScopeAttribute.Count;
			ClassScope();
			Assert.AreEqual(prev + 1, ClassScopeAttribute.Count);
			ClassScope();
			Assert.AreEqual(prev + 2, ClassScopeAttribute.Count);
		}

		[Test]
		public void NestScopeTest()
		{
			var classPrev = ClassScopeAttribute.Count;
			var structPrev = StructScopeAttribute.Count;
			Nest();
			Assert.AreEqual(classPrev + 15, ClassScopeAttribute.Count);
			Assert.AreEqual(structPrev + 15, StructScopeAttribute.Count);

			NestScope(1);
			Assert.AreEqual(classPrev + 16, ClassScopeAttribute.Count);
			Assert.AreEqual(structPrev + 16, StructScopeAttribute.Count);
		}


		[TestCase(12)]
		[TestCase(2)]
		[TestCase(500)]
		[TestCase(5)]
		public void ErrorTest(int arg)
		{
			static void Test1(int count, System.Action<int> action)
			{
				var prev = StructScopeAttribute.Count;
				Debug.Log("prev" + prev + "count" + count);
				Assert.Throws<System.Exception>(() =>
				{
					action(count);
				});
				Debug.Log("prev" + StructScopeAttribute.Count);
				Assert.AreEqual(prev + count, StructScopeAttribute.Count);
			}

			static void Test2(int count, System.Func<int, int> action)
			{
				var prev = ClassScopeAttribute.Count;
				Assert.Throws<System.Exception>(() =>
				{
					action(count);
				});
				Assert.AreEqual(prev + count, ClassScopeAttribute.Count);
			}

			Test1(arg, Error1);
			Test1(arg, Error2);
			Test1(arg, Error3);
			Test2(arg, Error4);
			Test2(arg, Error5);
			Test2(arg, Error6);
			Test1(arg, Error7);
		}

		[Test]
		public void FinallyTest()
		{
			Finally1(true);
			Finally1(false);
		}

		[Test]
		public void GetTest()
		{
			var prev = StructScopeAttribute.Count;
			var count = 0;
			Assert.AreEqual("get:test", Get1("test"));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);

			Assert.AreEqual(-5555, Get2(22, false));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);
			Assert.AreEqual(23, Get2(22, true));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);
			Assert.AreEqual(-5555, Get2(23, true));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);
			Assert.AreEqual(10, Get2(5, true));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);

			Assert.AreEqual(-5555, Get3(22, false));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);
			Assert.AreEqual(23, Get3(22, true));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);
			Assert.AreEqual(28, Get3(23, true));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);
			Assert.AreEqual(10, Get3(5, true));
			Assert.AreEqual(prev + (++count), StructScopeAttribute.Count);
		}
	}
}
