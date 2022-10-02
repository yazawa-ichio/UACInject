using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Threading.Tasks;
using UACInject;
using UnityEngine;
using UnityEngine.TestTools;

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

		class CondResultAttribute : ReturnConditionCodeAttribute
		{
			[CodeTarget]
			public static bool Run(bool enabled, int value, [Result] out int result)
			{
				result = value;
				return enabled;
			}

			[CodeTarget]
			public static bool Run(bool enabled, float value, [Result] out float result)
			{
				result = value;
				return enabled;
			}

			[CodeTarget]
			public static bool Run(bool enabled, string value, [Result] out string result)
			{
				result = value;
				return enabled;
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
			await Task.Delay(100);
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

		ValueTask ValueTaskTest()
		{
			return new ValueTask(Task.CompletedTask);
		}

		[Cond]
		async UniTask UniTaskAsync(bool enabled)
		{
			await Task.Delay(100);
		}

		[Cond]
		async ValueTask ValueTaskAsync(bool enabled)
		{
			await Task.Delay(100);
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
			{
				var tmp = Async(false);
				Assert.IsTrue(tmp.IsCompleted);
				tmp = Async(true);
				Assert.IsFalse(tmp.IsCompleted);
			}
			{
				var tmp = ValueTaskAsync(false);
				Assert.IsTrue(tmp.IsCompleted);
				tmp = ValueTaskAsync(true);
				Assert.IsFalse(tmp.IsCompleted);
			}
			{
				var tmp = UniTaskAsync(false);
				Assert.IsTrue(tmp.Status == UniTaskStatus.Succeeded);
				tmp = UniTaskAsync(true);
				Assert.IsFalse(tmp.Status == UniTaskStatus.Succeeded);
			}
		}

		[CondResult]
		int GetResult(bool enabled, int value)
		{
			return value * value;
		}

		[CondResult]
		float GetResult(bool enabled, float value)
		{
			return value * value;
		}

		//[CondResult]
		float GetResultMock(bool enabled, float value)
		{
			if (CondResultAttribute.Run(enabled, value, out var ret))
			{
				return ret;
			}
			return value * value;
		}


		[CondResult]
		string GetResult(bool enabled, string value)
		{
			return "skip:" + value;
		}

		[Test]
		public void CondResultTest()
		{
			Assert.AreEqual(5, GetResult(true, 5));
			Assert.AreEqual(5 * 5, GetResult(false, 5));
			Assert.AreEqual(2.4f, GetResult(true, 2.4f));
			Assert.AreEqual(2.4f * 2.4f, GetResult(false, 2.4f));
			Assert.AreEqual("bb", GetResult(true, "bb"));
			Assert.AreEqual("skip:cc", GetResult(false, "cc"));
		}


		[CondResult]
		async Task<int> GetResultAsync(bool enabled, int value)
		{
			await Task.Delay(10);
			return value * value;
		}

		Task<int> GetResultAsyncMock(bool enabled, int value)
		{
			if (CondResultAttribute.Run(enabled, value, out var ret))
			{
				return Task.FromResult(ret);
			}
			return Task.FromResult(value * value);
		}

		[CondResult]
		async ValueTask<int> GetResultValueAsync(bool enabled, int value)
		{
			await Task.Delay(10);
			return value * value;
		}

		ValueTask<int> GetResultValueAsyncMock(bool enabled, int value)
		{
			if (CondResultAttribute.Run(enabled, value, out var ret))
			{
				return new ValueTask<int>(Task.FromResult(ret));
			}
			return new ValueTask<int>(Task.FromResult(value * value));
		}

		[CondResult]
		async UniTask<int> GetResultUniTaskAsync(bool enabled, int value)
		{
			await Task.Delay(10);
			return value * value;
		}

		[CondResult]
		UniTask<int> GetResultUniTaskAsyncMock(bool enabled, int value)
		{
			if (CondResultAttribute.Run(enabled, value, out var ret))
			{
				return UniTask.FromResult(ret);
			}
			return UniTask.FromResult(value * value);
		}

		[UnityTest]
		public System.Collections.IEnumerator AsyncCondResultTest()
		{
			yield return UniTask.ToCoroutine(AsyncCondResultTestImpl);
		}

		UniTask Comp() => UniTask.CompletedTask;

		async UniTask AsyncCondResultTestImpl()
		{
			Assert.AreEqual(5, await GetResultAsync(true, 5));
			Assert.AreEqual(5 * 5, await GetResultAsync(false, 5));
			Assert.AreEqual(5, await GetResultValueAsync(true, 5));
			Assert.AreEqual(5 * 5, await GetResultValueAsync(false, 5));
			Assert.AreEqual(5, await GetResultUniTaskAsync(true, 5));
			Assert.AreEqual(5 * 5, await GetResultUniTaskAsync(false, 5));
		}

	}
}