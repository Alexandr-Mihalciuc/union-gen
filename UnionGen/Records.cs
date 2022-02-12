using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace UnionGen
{
    public record Union(
      string ns,
      string interfaceType,
      string classType,
      Lst<GenericType> genTypes,
      Lst<Case> cases);

    public record GenericType(string name);
    public record Case(string label, Lst<GenericType> types, Lst<Arg> content);
    public record Arg(string type, string name);

    public record GenFile(string name, string contents);
}
