using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnionGen.Tests
{
	public interface IMaybe<T>
	{
		IMaybe<T> None();
		IMaybe<T> Some(T val);
	}



	public class Maybe<T> : IMaybe<T>
	{
		private Maybe() { }

		private int caseNum = -1;
		
		public static IMaybe<T> None()
		{
			var res = new Maybe<T>();
			res.caseNum = 0;
			return res;
		}
		IMaybe<T> IMaybe<T>.None() =>
			Maybe<T>.None();

		private T caseStore1_val = default;		
		public static IMaybe<T> Some(T val)
		{
			var res = new Maybe<T>();
			res.caseNum = 1;
			res.caseStore1_val = val;
			return res;
		}
		IMaybe<T> IMaybe<T>.Some(T val) =>
			Maybe<T>.Some(val);

		public static TRes Match<TRes>(Maybe<T> that, Func<TRes> None, Func<T, TRes> Some)
		{
			return that.caseNum switch
			{
				0 => None(),
				1 => Some(that.caseStore1_val),
				var n => throw new Exception("Should never reach here, unknown case:  " + n)
			};
		}
	}
}

