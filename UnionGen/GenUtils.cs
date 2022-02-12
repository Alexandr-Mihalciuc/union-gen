using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionGen;

public static class GenUtils 
{
    public static Either<string, GenFile[]> GenerateUnions(SyntaxTree tree)
        => GetUnions(tree).Map(u => u.Map(GenFile).ToArray());

    public static Either<string, Union[]> GetUnions(SyntaxTree tree) 
    {
        Either<string, SyntaxNode> node = tree.TryGetRoot(out var n) ? Right(n) : Left("Can not get syntax root"); 
        
        Seq<InterfaceDeclarationSyntax> GetUnionInterfaces(SyntaxNode node) 
        {
            if (node == null)
                return Seq<InterfaceDeclarationSyntax>();

            if (node is InterfaceDeclarationSyntax potential) 
            {
                var containsUnionAttr = potential.AttributeLists.Bind(a => a.Attributes)
                                            .Map(a => a.Name)
                                            .OfType<IdentifierNameSyntax>()                                            
                                            .Any(a => a?.Identifier.ValueText == "Union");
                if (containsUnionAttr)                                            
                {
                    return Seq1(potential);
                }
            }

            return node.ChildNodes().ToSeq().Bind(n => GetUnionInterfaces(n));
        }

        Lst<string> FromName(NameSyntax q)
        {
            return q switch
            {
                null => List<string>(),
                IdentifierNameSyntax id => List(id.Identifier.ValueText),
                QualifiedNameSyntax qa => FromName(qa.Left).Add(qa.Right.Identifier.ValueText),
                _ => List<string>()
            };            
        }

        string NameSpace(SyntaxNode? n) 
        {            
            if (n == null)
                return "";

            if (n is BaseNamespaceDeclarationSyntax ns)
                return ns.Name switch
                {
                    QualifiedNameSyntax q => string.Join(".", FromName(q)),
                    IdentifierNameSyntax id => id.Identifier.ValueText,
                    _ => ""
                };

            return NameSpace(n.Parent);
        }

        Lst<GenericType> FromTypeParameters(TypeParameterListSyntax? s)
            => toList(s?.Parameters
                        .Map(p => p.Identifier.ValueText)
                        .Map(t => new GenericType(t))
                        .ToArray() ?? new GenericType[0]);

        Case FromMethod(MethodDeclarationSyntax m) 
        {
            string typeName(TypeSyntax? t)
                => t is IdentifierNameSyntax name
                    ? name.Identifier.ValueText
                    : t?.ToString() ?? "";

            var args = m.ParameterList.Parameters.Map(p => new Arg(typeName(p.Type), p.Identifier.ValueText));

            return new Case(m.Identifier.ValueText, FromTypeParameters(m?.TypeParameterList), toList(args));
        }

        Union FromInterface(InterfaceDeclarationSyntax unInterface) 
        {
            var interfaceName = unInterface.Identifier.ValueText;
            var className = interfaceName.StartsWith("I") 
                                ? interfaceName.Substring(1)
                                : interfaceName + "Union";

            var generics = FromTypeParameters(unInterface.TypeParameterList);

            var cases = unInterface.Members.OfType<MethodDeclarationSyntax>()
                                           .Map(FromMethod);
                                            
            return new Union(NameSpace(unInterface), interfaceName, className, toList(generics), toList(cases));
        }

        var unionInterfaces = node.Map(n => GetUnionInterfaces(n).Map(FromInterface).ToArray());

        return unionInterfaces;
    }

    static GenFile GenFile(Union union) 
    {
        string Generics(Lst<GenericType> g)
            => g.IsEmpty
                   ? ""
                   : $"<{string.Join(", ", g.Select(g => g.name))}>";   

        string RenderClass()
            => $"{union.classType}{Generics(union.genTypes)}";

        string storeField(int i, string name) => $"caseStore{i}_{name}";
        

        Lst<string> RenderCase(int i, Case c) 
        {                      

            return Printer.Start()
                .Add(c.content.Map(a => $"private {a.type} {storeField(i, a.name)} = default;"))                
                .Add($"public static {RenderClass()} {c.label}({c.content.Map(c => $"{c.type} {c.name}").CommarLink()})")
                .Add("{")
                .Tab()
                    .Add($"var res = new {RenderClass()}();")
                    .Add($"res.caseNum = {i};")
                    .Add(c.content.Map(a => $"res.{storeField(i, a.name)} = {a.name};"))
                    .Add($"return res;")
                .DelTab()
                .Add("}")                
                .Add("")
                .lines;
        }

        Lst<string> RenderMatch() 
        {
            var casesFunctions = union.cases
            .Select((c, i) =>
            {
                var types = c.content.Map(c => c.type).Concat(new[] { "TRes" }).CommarLink();
                return $"Func<{types}> {c.label}";
            })
            .CommarLink();

            var casesChecks = union.cases
            .Select(
               (c, i) => $"{i} => {c.label}({c.content.Map(x => $"this.{storeField(i, x.name)}").CommarLink()})")
            .Concat(new[]
            {
                "var n => throw new Exception(\"Should never reach here, unknown case:  \" + n)"
            })
            .AddCommars();

            return Printer.Start()
                .Add($"public TRes Match<TRes>({casesFunctions})")
                .Add("{")
                .Tab()
                    .Add("return this.caseNum switch ")
                    .Add("{")
                    .Tab()
                    .Add(casesChecks)
                    .DelTab()
                    .Add("};")
                .DelTab()
                .Add("}")
                .lines;
        }


        var str = Printer.Start()
            .Add($"using {union.ns};")
            .Add($"public class {RenderClass()}")
            .Add("{")
            .Tab()
                .Add($"private {union.classType}(){{}}")
                .Add("")
                .Add("private int caseNum = -1;")
                .Add("")
                .Add(union.cases.SelectMany((c, i) => RenderCase(i, c)))
                .Add(RenderMatch())
            .DelTab()
            .Add("}")
            .Contents();

     
        return new GenFile($"{union.ns}.{union.classType}.g.cs", str);
    }

    record Printer(Lst<string> lines, int tabs) 
    {
        public static Printer Start() => new Printer(List<string>(), 0);
        string tabsStr => string.Concat(Enumerable.Repeat("\t", tabs));
        public Printer Add(string line) => this with { lines = lines.Add($"{tabsStr}{line}") };
        public Printer Add(IEnumerable<string> ls) => this with { lines = lines.AddRange(ls.Map(l => $"{tabsStr}{l}")) };

        public Printer Tab() => this with { tabs = tabs + 1 };
        public Printer DelTab() => this with { tabs = tabs > 0 ? tabs - 1 :0 };
        public string Contents() => string.Join(Environment.NewLine, lines);
    }
}

