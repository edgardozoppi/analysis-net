using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Backend.ThreeAddressCode;
using Backend.Utils;

namespace Backend.Analysis
{
	public class CopyPropagationAnalysis : ForwardDataFlowAnalysis<IDictionary<Variable, Operand>> 
	{
		private IDictionary<Variable, Operand>[] GEN;
		private ISet<Variable>[] KILL;

		public CopyPropagationAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.ComputeGen();
			this.ComputeKill();
		}

		public override IDictionary<Variable, Operand> InitialValue(CFGNode node)
		{
			return new Dictionary<Variable, Operand>();
		}

		public override IDictionary<Variable, Operand> DefaultValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		public override bool CompareValues(IDictionary<Variable, Operand> left, IDictionary<Variable, Operand> right)
		{
			return left.SequenceEqual(right);
		}

		public override IDictionary<Variable, Operand> Merge(IDictionary<Variable, Operand> left, IDictionary<Variable, Operand> right)
		{
			var result = new Dictionary<Variable, Operand>(left);

			foreach (var copy in right)
			{
				var variable = copy.Key;
				var rightOperand = copy.Value;

				if (left.ContainsKey(variable))
				{
					var leftOperand = left[variable];

					if (!leftOperand.Equals(rightOperand))
					{
						result[variable] = UnknownOperand.Value;
					}
				}
				else
				{
					result[variable] = rightOperand;
				}
			}

			return result;
		}

		public override IDictionary<Variable, Operand> Flow(CFGNode node, IDictionary<Variable, Operand> input)
		{
			IDictionary<Variable, Operand> result;

			if (input == null)
			{
				result = new Dictionary<Variable, Operand>();
			}
			else
			{
				result = new Dictionary<Variable, Operand>(input);
			}

			foreach (var instruction in node.Instructions)
			{
				var copy = this.Flow(instruction, result);

				foreach (var variable in instruction.ModifiedVariables)
				{
					this.RemoveCopiesWithVariable(result, variable);
				}

				if (copy.HasValue)
				{
					result.Add(copy.Value);
				}
			}

			return result;
		}

		private void ComputeGen()
		{
			GEN = new IDictionary<Variable, Operand>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = this.Flow(node, null);
				GEN[node.Id] = gen;
			}
		}

		private void ComputeKill()
		{
			KILL = new ISet<Variable>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var kill = new HashSet<Variable>();

				foreach (var instruction in node.Instructions)
				{
					kill.UnionWith(instruction.ModifiedVariables);
				}

				KILL[node.Id] = kill;
			}
		}

		private void RemoveCopiesWithVariable(IDictionary<Variable, Operand> copies, Variable variable)
		{
			var array = copies.ToArray();

			foreach (var copy in array)
			{
				if (copy.Key == variable ||
					copy.Value == variable)
				{
					copies.Remove(copy);
				}
			}
		}

		private KeyValuePair<Variable, Operand>? Flow(Instruction instruction, IDictionary<Variable, Operand> copies)
		{
			KeyValuePair<Variable, Operand>? result = null;

			if (instruction is AssignmentInstruction)
			{
				var assignment = instruction as AssignmentInstruction;

				if (assignment.Operand is Constant)
				{
					var constant = assignment.Operand as Constant;
					result = new KeyValuePair<Variable, Operand>(assignment.Result, constant);
				}
				else if (assignment.Operand is Variable)
				{
					var variable = assignment.Operand as Variable;

					if (copies.ContainsKey(variable))
					{
						var operand = copies[variable];
						result = new KeyValuePair<Variable, Operand>(assignment.Result, operand);
					}
					else
					{
						result = new KeyValuePair<Variable, Operand>(assignment.Result, variable);
					}
				}
				else
				{
					result = new KeyValuePair<Variable, Operand>(assignment.Result, UnknownOperand.Value);
				}
			}

			return result;
		}
	}
}
