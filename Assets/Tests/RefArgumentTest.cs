using NUnit.Framework;
using UACInject;
using UnityEngine;

namespace UACInjectTests
{
	class RefArgumentTest
	{
		class Pow1Attribute : ExecuteCodeAttribute
		{
			[CodeTarget]
			internal static void Run(float f, float p, ref float result)
			{
				result = Mathf.Pow(f, p);
			}

		}

		class Pow2Attribute : ExecuteCodeAttribute
		{
			[CodeTarget]
			internal static void Run(float f, float p, [CallerField("m_Result")] ref float result, [CallerField("m_Result")] ref float result2)
			{
				result = Mathf.Pow(f, p);
			}
		}


		float m_Result = 0;

		[Pow1]
		void Pow1(float f, float p, ref float result)
		{
			Debug.Log($"Pow1:{result} = Mathf.Pow({f}, {p})");
		}

		void Pow1Mock(float f, float p, ref float result)
		{
			Pow1Attribute.Run(f, p, ref result);
			Debug.Log($"Pow1:{result} = Mathf.Pow({f}, {p})");
		}

		[Pow2]
		void Pow2(float f, float p)
		{
			Debug.Log($"Pow2:{m_Result} = Mathf.Pow({f}, {p})");
		}

		void Pow2Mock(float f, float p)
		{
			Pow2Attribute.Run(f, p, ref m_Result, ref m_Result);
			Debug.Log($"Pow2:{m_Result} = Mathf.Pow({f}, {p})");
		}

		[TestCase(1, 1)]
		[TestCase(2, 2)]
		[TestCase(4, 2)]
		[TestCase(22, 22)]
		public void Test(float f, float p)
		{
			float result = 0;
			Pow1(f, p, ref result);
			Assert.AreEqual(Mathf.Pow(f, p), result);
			Pow2(f, p);
			Assert.AreEqual(Mathf.Pow(f, p), m_Result);
		}

	}
}