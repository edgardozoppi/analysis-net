using Backend.ThreeAddressCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public static class Extensions
	{
		public static Subset<T> ToSubset<T>(this T[] universe)
		{
			return new Subset<T>(universe, false);
		}

		public static Subset<T> ToEmptySubset<T>(this T[] universe)
		{
			return new Subset<T>(universe, true);
		}

		public static IExpression ReplaceVariables<T>(this IExpression expr, IDictionary<Variable, T> equalities) where T : IExpression
		{
			foreach (var variable in expr.Variables)
			{
				if (equalities.ContainsKey(variable))
				{
					var value = equalities[variable];

					if (!value.Equals(UnknownValue.Value))
					{
						expr = expr.Replace(variable, value);
					}
				}
			}

			return expr;
		}
	}
}
