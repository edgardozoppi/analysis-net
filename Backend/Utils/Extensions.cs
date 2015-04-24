using Backend.Analysis;
using Backend.ThreeAddressCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode.Values;
using Backend.ThreeAddressCode.Expressions;
using Backend.ThreeAddressCode.Instructions;
using Backend.Visitors;

namespace Backend.Utils
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				collection.Add(element);
			}
		}

		public static MapSet<K, V> ToMapSet<K, V>(this IEnumerable<V> elements, Func<V, K> keySelector)
		{
			var result = new MapSet<K, V>();

			foreach (var element in elements)
			{
				var key = keySelector(element);
				result.Add(key, element);
			}

			return result;
		}

		public static MapList<K, V> ToMapList<K, V>(this IEnumerable<V> elements, Func<V, K> keySelector)
		{
			var result = new MapList<K, V>();

			foreach (var element in elements)
			{
				var key = keySelector(element);
				result.Add(key, element);
			}

			return result;
		}

		public static Subset<T> ToSubset<T>(this T[] universe)
		{
			return new Subset<T>(universe, false);
		}

		public static Subset<T> ToEmptySubset<T>(this T[] universe)
		{
			return new Subset<T>(universe, true);
		}

		public static ISet<IVariable> GetVariables(this IInstructionContainer block)
		{
			var result = block.Instructions.SelectMany(i => i.Variables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetModifiedVariables(this IInstructionContainer block)
		{
			var result = block.Instructions.SelectMany(i => i.ModifiedVariables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetUsedVariables(this IInstructionContainer block)
		{
			var result = block.Instructions.SelectMany(i => i.UsedVariables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetDefinedVariables(this IInstructionContainer block)
		{
			var result = new HashSet<IVariable>();

			foreach (var instruction in block.Instructions)
			{
				var definition = instruction as DefinitionInstruction;

				if (definition != null && definition.HasResult)
				{
					result.Add(definition.Result);
				}
			}

			return result;
		}

		public static ISet<IVariable> GetDefinedVariables(this CFGLoop loop)
		{
			var result = loop.Body.SelectMany(n => n.GetDefinedVariables());
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetVariables(this ControlFlowGraph cfg)
		{
			var result = cfg.Nodes.SelectMany(n => n.GetVariables());
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
