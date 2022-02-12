using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;
using System.Collections.Generic;

namespace UnionGen.Tests;

public class GenUtilsTest
{
    [Fact]
    public void Can_Generate_Simple_Union()
    {
        var unionText = @"
using UnionGen;
namespace TestTwoCaseUnion;

[Union]
interface IMaybe<T>
{
    IMaybe None();
    IMayBe Some<T>(T val);
}

";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(unionText);

        IEnumerable<Union> result = GenUtils.GetUnions(tree).IfLeft(x => throw new System.Exception(x));

        result.Should().BeEquivalentTo(
            new[] {
            new Union("TestTwoCaseUnion", "IMaybe", "Maybe",
                List(new GenericType("T")),
                List(
                    new Case("None", List<GenericType>(), List<Arg>()),
                    new Case("Some", List(new GenericType("T")), List(new Arg("T", "val")))
                    ))}
            );
    }

    [Fact]
    public void Can_Generate_Simple_Args_With_Tuples()
    {
        var unionText = @"
using UnionGen;
namespace TupleNamespace;

[Union]
interface IMaybe<T>
{
    IMaybe None();
    IMayBe Some<T>((T, int) val);
}

";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(unionText);

        IEnumerable<Union> result = GenUtils.GetUnions(tree).IfLeft(x => throw new System.Exception(x));

        result.Should().BeEquivalentTo(
            new[] {
            new Union("TupleNamespace", "IMaybe", "Maybe",
                List(new GenericType("T")),
                List(
                    new Case("None", List<GenericType>(), List<Arg>()),
                    new Case("Some", List(new GenericType("T")), List(new Arg("(T, int)", "val")))
                    ))}
            );
    }

    [Fact]
    public void Can_Generate_Text()
    {
        var unionText = @"
using UnionGen;
namespace TupleNamespace;

[Union]
public interface IMaybe<T>
{
    IMaybe<T> None();
    IMayBe<T> Some<T>(T val);
}

";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(unionText);

        var expected =
@"using TupleNamespace;
public class Maybe<T>
{   
    private Maybe(){}
    
    private int caseNum = -1;
    
    public static Maybe<T> None()
    {
    	var res = new Maybe<T>();
    	res.caseNum = 0;
    	return res;
    }    
    
    private T caseStore1_val = default;		
    public static Maybe<T> Some(T val)
    {
    	var res = new Maybe<T>();
    	res.caseNum = 1;
    	res.caseStore1_val = val;
    	return res;
    }
        
    public TRes Match<TRes>(Func<TRes> None, Func<T, TRes> Some)
    {
    	return this.caseNum switch
    	{
    		0 => None(),
    		1 => Some(this.caseStore1_val),
    		var n => throw new Exception(""Should never reach here, unknown case:  "" + n)    
        };
    }
}";

        var res = GenUtils.GenerateUnions(tree).IfLeft(x => throw new System.Exception(x));

        var (same, diff) = AreSame(res[0].contents, expected);

        same.Should().BeTrue(diff);

    }

    [Fact]
    public void Can_Generate_Text_With_Scoped_NamesSpace()
    {
        var unionText = @"
using UnionGen;
namespace V.E.R.Y
{
[Union]
public interface IOption
{
    IOption None();    
}
}

";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(unionText);

        var expected =
@"using V.E.R.Y;
public class Option
{   
    private Option(){}
    
    private int caseNum = -1;
    
    public static Option None()
    {
    	var res = new Option();
    	res.caseNum = 0;
    	return res;
    }   
        
    public TRes Match<TRes>(Func<TRes> None)
    {
    	return this.caseNum switch
    	{
    		0 => None(),    		
    		var n => throw new Exception(""Should never reach here, unknown case:  "" + n)    
        };
    }
}";

        var res = GenUtils.GenerateUnions(tree).IfLeft(x => throw new System.Exception(x));

        var (same, diff) = AreSame(res[0].contents, expected);

        same.Should().BeTrue(diff);

    }

    private (bool, string) AreSame(string expected, string actual)
    {
        var diff = InlineDiffBuilder.Diff(actual, expected);
        List<string> lines = new List<string>();

        var cont = diff.Lines.Map(l => l.Type switch
        {
            ChangeType.Inserted => "+" + l.Text,
            ChangeType.Deleted => "-" + l.Text,
            _ => l.Text
        });

        var total = string.Join(Environment.NewLine, cont);

        return (!diff.HasDifferences, total);
    }
}
