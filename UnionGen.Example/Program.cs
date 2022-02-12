// See https://aka.ms/new-console-template for more information
using UnionGen.Example;

Console.WriteLine("Hello, World!");

var score = Command.FaultAway("Alex");

var result = score.Match(
    ScoreHome: (p, name) => $"Home Scored Points:{p}, name: {name}",
    ScoreAway: (p, name) => $"Away Scored Points:{p}, name: {name}",
    FaultHome: name => "Fault Home:" + name,
    FaultAway: name => "Fault Away:" + name
    );

Console.WriteLine("Command was: " + result);

var str = MayBe<string>.Some("Liuba");

Console.WriteLine("Hello: " + str.Match(str => str, () => ""));

var either = Either<string, int>.Left("text");

Console.WriteLine("Either: " + either.Match(str => str, n => n.ToString()));

