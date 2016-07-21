// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Expressions;
using Model.ThreeAddressCode.Instructions;
using Backend.Analyses;
using Model.Types;
using Model;
using Backend.Model;

namespace Backend.Utils
{
	public static class Extensions
	{
		public static bool DictionaryEquals<K,V>(this IDictionary<K,V> self, IDictionary<K,V> other, Func<V, V, bool> valueEquals = null)
		{
			if (object.ReferenceEquals(self, other)) return true;
			if (self.Count != other.Count) return false;

			if (valueEquals == null)
			{
				valueEquals = (a, b) => object.Equals(a, b);
			}

			foreach (var key in self.Keys)
			{
				var otherContainsKey = other.ContainsKey(key);
				if (!otherContainsKey) return false;
			}

			foreach (var entry in self)
			{
				var value = other[entry.Key];
				var valuesAreEquals = valueEquals(entry.Value, value);

				if (!valuesAreEquals) return false;
			}

			return true;
		}

		//public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
		//{
		//	foreach (var element in elements)
		//	{
		//		collection.Add(element);
		//	}
		//}

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
			var result = from i in block.Instructions
						 from v in i.Variables
						 select v;

			//var result = block.Instructions.SelectMany(i => i.Variables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetModifiedVariables(this IInstructionContainer block)
		{
			var result = from i in block.Instructions
						 from v in i.ModifiedVariables
						 select v;

			//var result = block.Instructions.SelectMany(i => i.ModifiedVariables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetUsedVariables(this IInstructionContainer block)
		{
			var result = from i in block.Instructions
						 from v in i.UsedVariables
						 select v;

			//var result = block.Instructions.SelectMany(i => i.UsedVariables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetDefinedVariables(this IInstructionContainer block)
		{
			var result = from i in block.Instructions
						 let d = i as DefinitionInstruction
						 where d != null && d.HasResult
						 select d.Result;

			return new HashSet<IVariable>(result);
			//var result = new HashSet<IVariable>();

			//foreach (var instruction in block.Instructions)
			//{
			//    var definition = instruction as DefinitionInstruction;

			//    if (definition != null && definition.HasResult)
			//    {
			//        result.Add(definition.Result);
			//    }
			//}

			//return result;
		}

		public static ISet<IVariable> GetModifiedVariables(this CFGLoop loop)
		{
			var result = from n in loop.Body
						 from v in n.GetModifiedVariables()
						 select v;

			//var result = loop.Body.SelectMany(n => n.GetModifiedVariables());
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetVariables(this ControlFlowGraph cfg)
		{
			var result = from n in cfg.Nodes
						 from v in n.GetVariables()
						 select v;

			//var result = cfg.Nodes.SelectMany(n => n.GetVariables());
			return new HashSet<IVariable>(result);
		}

		public static ISet<CFGNode> GetExitNodes(this CFGLoop loop)
		{
			var result = from n in loop.Body
						 from m in n.Successors
						 where !loop.Body.Contains(m)
						 select n;

			//var result = loop.Body.Where(n => n.Successors.Any(m => !loop.Body.Contains(m)));
			return new HashSet<CFGNode>(result);
		}

		public static IExpression ToExpression(this IValue value)
		{
			return value as IExpression;
		}

		//public static IExpression GetValueOriginal(this IDictionary<IVariable, IExpression> equalities, IVariable variable)
		//{
		//    var result = equalities.ContainsKey(variable) ? equalities[variable] : variable;
		//    return result;
		//}
		
		public static IExpression GetValue(this IDictionary<IVariable, IExpression> equalities, IVariable variable)
		{
			IExpression result = variable;

			while (variable != null && equalities.ContainsKey(variable))
			{
				result = equalities[variable];
				variable = result as IVariable;
			}

			return result;
		}

		public static bool IsTemporal(this IVariable variable)
		{
			while (variable is DerivedVariable)
			{
				var derived = variable as DerivedVariable;
				variable = derived.Original;
			}

			var result = variable is TemporalVariable;
			return result;
		}

		public static bool IsCopy(this IInstruction instruction, out IVariable left, out IVariable right)
		{
			var result = false;
			left = null;
			right = null;

			if (instruction is LoadInstruction)
			{
				var load = instruction as LoadInstruction;

				if (load.Operand is IVariable)
				{
					left = load.Result;
					right = load.Operand as IVariable;
					result = true;
				}
			}

			return result;
		}

		public static IExpression ReplaceVariables<T>(this IExpression expr, IDictionary<IVariable, T> equalities) where T : IExpression
		{
			foreach (var variable in expr.Variables)
			{
				if (variable.IsTemporal())
				{
					var hasValue = equalities.ContainsKey(variable);

					if (hasValue)
					{
						var value = equalities[variable];
						var isUnknown = value is UnknownValue;
						var isPhi = value is PhiExpression;
						var isMethodCall = value is MethodCallExpression;

						if (isUnknown || isPhi || isMethodCall)
							continue;

						expr = expr.Replace(variable, value);
					}
				}
			}

			return expr;
		}

		public static void RemoveTemporalVariables(this PointsToGraph ptg)
		{
			var temporals = ptg.Variables.OfType<TemporalVariable>().ToArray();

			foreach (var temporal in temporals)
			{
				ptg.Remove(temporal);
			}
		}

		public static bool IsPure(this IMethodReference method)
		{
			var result = method.Attributes.Any(a => a.Type.Equals(PlatformTypes.PureAttribute));

			if (!result)
			{
				result = method.Name == "get_Count" && TypeHelper.IsContainer(method.ContainingType);
			}

			return result;
		}

		public static void Inline(this MethodBody callerBody, MethodCallInstruction methodCall, MethodBody calleeBody)
		{
			// TODO: Fix local variables (and parameters) name clashing
			// TODO: Fix instruction labels clashing

			var index = callerBody.Instructions.IndexOf(methodCall);
			callerBody.Instructions.RemoveAt(index);

			IInstruction nextInstruction = null;

			if (callerBody.Instructions.Count > index)
			{
				// The caller method has more instructions after the method call
				nextInstruction = callerBody.Instructions[index];
			}			

			for (var i = 0; i < calleeBody.Parameters.Count; ++i, ++index)
			{
				var parameter = calleeBody.Parameters[i];
				var argument = methodCall.Arguments[i];
				var copy = new LoadInstruction(methodCall.Offset, parameter, argument);

				callerBody.Instructions.Insert(index, copy);
			}

			var lastCalleeInstructionIndex = calleeBody.Instructions.Count - 1;

			for (var i = 0; i < calleeBody.Instructions.Count; ++i)
			{
				var instruction = calleeBody.Instructions[i];

				if (instruction is ReturnInstruction)
				{
					var ret = instruction as ReturnInstruction;

					if (ret.HasOperand && methodCall.HasResult)
					{
						// Copy the return value of the callee to the result variable of the method call
						var copy = new LoadInstruction(ret.Offset, methodCall.Result, ret.Operand);

						callerBody.Instructions.Insert(index, copy);
						index++;
					}

					if (nextInstruction != null && i < lastCalleeInstructionIndex)
					{
						// Jump to the instruction after the method call
						var branch = new UnconditionalBranchInstruction(ret.Offset, nextInstruction.Offset);

						callerBody.Instructions.Insert(index, branch);
						index++;
					}
				}
				else
				{
					callerBody.Instructions.Insert(index, instruction);
					index++;
				}
			}
		}
	}
}
