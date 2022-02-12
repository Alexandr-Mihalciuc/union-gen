using System.Diagnostics;

namespace UnionGen;

[Generator]
public class UnionGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        //Debugger.Launch();

        var files = context.Compilation
                   .SyntaxTrees
                   .Bind(t => GenUtils.GenerateUnions(t).IfLeft(e => throw new Exception(e))).ToList();

        files.Iter(f => context.AddSource(f.name, f.contents));
    }

    public void Initialize(GeneratorInitializationContext context) { }
}


