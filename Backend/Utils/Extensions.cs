using Backend.Analysis;
using Backend.ThreeAddressCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode.Values;
using Backend.ThreeAddressCode.Expressions;

namespace Backend.Utils
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				list.Add(element);
			}
		}

		public static Subset<T> ToSubset<T>(this T[] universe)
		{
			return new Subset<T>(universe, false);
		}

		public static Subset<T> ToEmptySubset<T>(this T[] universe)
		{
			return new Subset<T>(universe, true);
		}

		public static ISet<IVariable> ModifiedVariables(this CFGNode node)
		{
			var result = node.Instructions.SelectMany(i => i.ModifiedVariables);
			return new HashSet<IVariable>(result);
		}

		public static IExpression ToExpression(this IValue value)
		{
			return value as IExpression;
		}

		public static IExpression GetValue(this IDictionary<IVariable, IExpression> equalities, IVariable variable)
		{
			var result = equalities.ContainsKey(variable) ? equalities[variable] : variable;
			return result;
		}

		public static IExpression ReplaceVariables<T>(this IExpression expr, IDictionary<IVariable, T> equalities) where T : IExpression
		{
			foreach (var variable in expr.Variables)
			{
				var isTemporal = variable is TemporalVariable;

				if (variable is DerivedVariable)
				{
					var derived = variable as DerivedVariable;
					isTemporal = derived.Original is TemporalVariable;
				}

				if (isTemporal)
				{
					if (equalities.ContainsKey(variable))
					{
						var value = equalities[variable];
						var isUnknownValue = value is UnknownValue;
						var isPhiExpression = value is PhiExpression;

						if (isUnknownValue || isPhiExpression)
							continue;

						expr = expr.Replace(variable, value);
					}
				}
			}

			return expr;
		}
	}
}
