using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Backend.ThreeAddressCode;
using Backend.Utils;

namespace Backend.Analysis
{
	public class StaticSingleAssignmentAnalysis : ForwardDataFlowAnalysis<IDictionary<Variable, DerivedVariable>>
	{
		public StaticSingleAssignmentAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public override IDictionary<Variable, DerivedVariable> InitialValue(CFGNode node)
		{
			return new Dictionary<Variable, DerivedVariable>();
		}

		public override IDictionary<Variable, DerivedVariable> DefaultValue(CFGNode node)
		{
			return new Dictionary<Variable, DerivedVariable>();
		}

		public override bool CompareValues(IDictionary<Variable, DerivedVariable> left, IDictionary<Variable, DerivedVariable> right)
		{
			return left.SequenceEqual(right);
		}

		public override IDictionary<Variable, DerivedVariable> Merge(IDictionary<Variable, DerivedVariable> left, IDictionary<Variable, DerivedVariable> right)
		{
			var result = new Dictionary<Variable, DerivedVariable>(left);

			foreach (var equality in right)
			{
				var original = equality.Key;
				var rightDerived = equality.Value;

				if (left.ContainsKey(original))
				{
					var leftDerived = left[original];

					if (!leftDerived.Equals(rightDerived))
					{
						var index = Math.Max(leftDerived.Index, rightDerived.Index) + 1;
						result[original] = new DerivedVariable(original, index);
					}
				}
				else
				{
					result[original] = rightDerived;
				}
			}

			return result;
		}

		public override IDictionary<Variable, DerivedVariable> Flow(CFGNode node, IDictionary<Variable, DerivedVariable> input)
		{
			IDictionary<Variable, DerivedVariable> result = new Dictionary<Variable, DerivedVariable>(input);

			foreach (var instruction in node.Instructions)
			{
				var equality = this.Flow(instruction, input);

				if (equality.HasValue)
				{
					var original = equality.Value.Key;
					var derived = equality.Value.Value;

					result[original] = derived;
				}
			}

			return result;
		}

		private KeyValuePair<Variable, DerivedVariable>? Flow(Instruction instruction, IDictionary<Variable, DerivedVariable> input)
		{
			KeyValuePair<Variable, DerivedVariable>? result = null;

			if (instruction is AssignmentInstruction)
			{
				var assignment = instruction as AssignmentInstruction;
				var original = assignment.Result;
				var index = 0u;

				if (original is DerivedVariable)
				{
					var oldDerived = original as DerivedVariable;
					original = oldDerived.Original;
				}				

				if (input.ContainsKey(original))
				{
					index = input[original].Index + 1;
				}

				var newDerived = new DerivedVariable(original, index);

				assignment.Result = newDerived;
				assignment.Expression = assignment.Expression.ReplaceVariables(input);

				result = new KeyValuePair<Variable, DerivedVariable>(original, newDerived);
			}

			return result;
		}
	}
}
