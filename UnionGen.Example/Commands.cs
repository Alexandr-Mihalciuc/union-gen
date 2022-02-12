using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnionGen.Example;

[Union]
public interface ICommand
{
    void ScoreHome(int points, string name);
    void ScoreAway(int points, string name);
    void FaultHome(string name);
    void FaultAway(string name);
}

[Union]
public interface IMayBe<T> 
{
    void Some(T val);
    void None();
}

[Union]
public interface IEither<L, R> 
{
    void Left(L left);
    void Right(R right);
}

