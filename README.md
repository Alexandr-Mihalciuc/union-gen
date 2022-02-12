# union-gen

C# 10 and before is missing union types, which might apprear later. 
Experimental project, models missing c# feature for discriminated unions, uses source code generators.

##Usage

Compile solution
In your project add reference to `UnionGen.csproj` for example

```
<ItemGroup>
    <ProjectReference Include="..\UnionGen\UnionGen.csproj" OutputItemType="Analyzer" />
</ItemGroup>
```

Add and interface to your project with Union attribute for example:

```
[Union]
public interface IMayBe<T> 
{
    void Some(T val);
    void None();
}
```

The project autogenerates code:

```
public class MayBe<T>
{
	private MayBe(){}
	
	private int caseNum = -1;
	
	private T caseStore0_val = default;
	public static MayBe<T> Some(T val)
	{
		var res = new MayBe<T>();
		res.caseNum = 0;
		res.caseStore0_val = val;
		return res;
	}
	
	public static MayBe<T> None()
	{
		var res = new MayBe<T>();
		res.caseNum = 1;
		return res;
	}
	
	public TRes Match<TRes>(Func<T, TRes> Some, Func<TRes> None)
	{
		return this.caseNum switch 
		{
			0 => Some(this.caseStore0_val),
			1 => None(),
			var n => throw new Exception("Should never reach here, unknown case:  " + n)
		};
	}
}
```

The generated class can be used like:

```
var str = MayBe<string>.Some("some text");
Console.WriteLine("Hello: " + str.Match(str => str, () => ""));
```
