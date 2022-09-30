using NUnit.Framework;
using System.Threading.Tasks;
using UACInject;
using UnityEngine;

namespace UACInjectTests
{
	class ReturnConditionTest
	{
		class CondAttribute : ReturnConditionCodeAttribute
		{
			[CodeTarget]
			public static bool Run(bool enabled)
			{
				return !enabled;
			}
		}

		[Cond]
		void Error1(bool enabled)
		{
			throw new System.Exception("Error");
		}

		void Error1Mock(bool enabled)
		{
			if (CondAttribute.Run(enabled))
			{
				return;
			}
			throw new System.Exception("Error");
		}

		[Cond]
		void Error2(bool enabled)
		{
			try
			{
				throw new System.Exception("Error");
			}
			finally
			{
				Debug.Log(enabled);
			}
		}

		void Error2Mock(bool enabled)
		{
			if (CondAttribute.Run(enabled))
			{
				return;
			}
			try
			{
				throw new System.Exception("Error");
			}
			finally
			{
				Debug.Log(enabled);
			}
		}

		[Cond]
		void Error3(bool enabled)
		{
			try
			{
				throw new System.Exception("Error");
			}
			catch
			{
				Debug.Log(enabled);
			}
		}

		void Error3Mock(bool enabled)
		{
			if (CondAttribute.Run(enabled))
			{
				return;
			}
			try
			{
				throw new System.Exception("Error");
			}
			catch
			{
				Debug.Log(enabled);
			}
		}

		[Cond]
		[Cond]
		void Error4(bool enabled)
		{
			try
			{
				throw new System.Exception("Error");
			}
			catch
			{
				Debug.Log(enabled);
				throw;
			}
		}

		void Error4Mock(bool enabled)
		{
			if (CondAttribute.Run(enabled))
			{
				return;
			}
			if (CondAttribute.Run(enabled))
			{
				return;
			}
			try
			{
				throw new System.Exception("Error");
			}
			catch
			{
				Debug.Log(enabled);
				throw;
			}
		}

		[Cond]
		void Error5(bool enabled)
		{
			if (enabled)
			{
				Debug.Log(enabled);
			}
		}

		void Error5Mock(bool enabled)
		{
			if (CondAttribute.Run(enabled))
			{
				return;
			}
			if (enabled)
			{
				Debug.Log(enabled);
			}
		}

		[Cond]
		async Task Async(bool enabled)
		{
			await Task.Delay(1);
		}

		async Task AsyncMock(bool enabled)
		{
			if (CondAttribute.Run(enabled))
			{
				return;
			}
			await Task.Delay(1);
		}

		Task AsyncMock2(bool enabled)
		{
			if (CondAttribute.Run(enabled))
			{
				return Task.CompletedTask;
			}
			return Task.Delay(1);
		}


		[Test]
		public void CondTest()
		{
			void Handle(bool enabled, System.Action<bool> action)
			{
				Assert.Throws<System.Exception>(() =>
				{
					action(enabled);
				});
			}
			void Skip(bool enabled, System.Action<bool> action)
			{
				action(enabled);
			}
			Handle(true, Error1);
			Skip(false, Error1);
			Handle(true, Error2);
			Skip(false, Error2);
			Skip(true, Error3);
			Skip(false, Error3);
			Handle(true, Error4);
			Skip(false, Error4);
			Skip(true, Error5);
			Skip(false, Error5);
		}

		[Test]
		public void AsyncTest()
		{
			var tmp = Async(false);
			Assert.IsTrue(tmp.IsCompleted);
			tmp = Async(true);
			Assert.IsFalse(tmp.IsCompleted);
		}


	}
}