# UACInject (Unity Attribute Code Inject Package)

UACInjectはコード挿入を行うAttributeを手軽に再生するためのパッケージです。  
デバッグ・プロファイル用のコードなどをシンボルで外す様な物を簡単に作成できます。  

## インストール

upmで以下のURLでパッケージのインストールが出来ます。
```
https://github.com/yazawa-ichio/UACInject.git?path=Packages/jp.yazawa-ichio.uacinject
```    

## 使用方法

挿入コードを実装するには指定のAttributeを継承したクラスを実装します。  

|Attribute|挿入コードの種類|
|--|--|
|ExecuteCodeAttribute|返り値のない単純な関数実行を挿入します|
|ScopeCodeAttribute|System.IDisposableを返す関数を挿入します。Disposeが終了時に呼ばれます|
|ReturnConditionCodeAttribute|trueであればreturnする判定関数を挿入します|

### 挿入するコードを定義する

指定のAttributeを継承し、挿入コードを`CodeTargetAttribute`を設定した静的関数として実装します。  
複数のCodeTarget設定されている場合はCodeTargetのPriorityが高い順に、引数の多い順にマッチした関数を挿入します。  

```cs
using UACInject;
using UnityEngine;

class PropertyLogAttribute : ExecuteCodeAttribute
{
	[CodeTarget]
	static void Run(object value)
	{
		Debug.Log(value);
	}

	[CodeTarget]
	static void Run(object value, [CallerInstance] Object context)
	{
		Debug.Log(value, context);
	}
}

[System.Diagnostics.Conditional("DEBUG_CODE")]
class DebugIgnoreAttribute : ReturnConditionCodeAttribute
{
	public DebugIgnoreAttribute([ConstructorParameter] string flag) { }

#if DEBUG_CODE
	[CodeTarget]
	static bool Ignore(string flag)
	{
		return DebugFlag.IgnoreCheck(method);
	}
#endif
}
```
### 挿入コードの設定

定義したAttributeを関数に設定するとコードが挿入されます。  
Method指定すると優先度を無視して指定のコードを挿入します。  

```cs
class Sample
{
	// Property変更時にログが出される
	public int Value { get;  [PropertyLog] set; }

	// 条件が合えば関数の実行が無視される
	[DebugIgnore("IgnoreSample.Run", Method = "Run" )]
	public void Run()
	{
	}

	// async関数でも使用可能
	[DebugIgnore("IgnoreSample.RunAsync", Method = "Run" )]
	public async Task RunAsync()
	{
	}
}
```

### 挿入コードの引数の設定方法
挿入コードの引数の設定の仕方は複数あります。  
何も設定されていない場合はCallerArgumentAttributeと判定されます。  
引数には`ref参照`を使用して値を書き換える事も可能です。  

#### ConstructorParameterAttribute

コンストラクタのパラメーターに`ConstructorParameterAttribute`を設定します。  
挿入する実行関数の引数に設定されます。呼び出し元の関数の引数よりもこちらで設定した値が優先されます。  

```cs
class SampleAttribute : ExecuteCodeAttribute
{
	public SampleAttribute([ConstructorParameter] string arg1, [ConstructorParameter("arg2")] int value)
	{
	}
	[CodeTarget]
	static void Run(string arg1, int arg2)
	{
	}
}
```

#### CallerArgumentAttribute

呼び出し元の関数の引数に指定の名前と型が一致する物があれば設定されます。  

```cs
class SampleAttribute : ExecuteCodeAttribute
{
	[CodeTarget]
	static void Run([CallerArgument("arg")] ref int arg1)
	{
	}
}
```

#### CallerFieldAttribute

呼び出し元の関数のインスタンスが持つフィールドが設定されます。  

```cs
class SampleAttribute : ExecuteCodeAttribute
{
	[CodeTarget]
	static void Run([CallerField("m_Field")] string arg1)
	{
	}
}
```

#### CallerInstanceAttribute

呼び出し元の関数のインスタンスが設定されます。  

```cs
class SampleAttribute : ExecuteCodeAttribute
{
	[CodeTarget]
	static void Run([CallerInstance] MonoBehaviour owner)
	{
	}
}
```

#### CallerMethodNameAttribute

呼び出し元の関数の名前が設定されます。  

```cs
class SampleAttribute : ExecuteCodeAttribute
{
	[CodeTarget]
	static void Run([CallerMethodName] string name)
	{
	}
}
```

#### ReturnConditionCodeAttribute.ResultAttribute
`ReturnConditionCodeAttribute`を使用する場合に結果を返す場合に利用します。  
引数はout参照を設定する必要があります。  

```cs
class SampleAttribute : ReturnConditionCodeAttribute
{
	[CodeTarget]
	static bool Run(string value, [Result] out int ret)
	{
		ret = 10;
		return value == "";
	}
}
```