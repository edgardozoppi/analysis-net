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
			this.GenerateGen();
			this.GenerateKill();
		}

		public override IDictionary<Variable, Operand> EntryInitialValue
		{
			get { return new Dictionary<Variable, Operand>(); }
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
			var result = new Dictionary<Variable, Operand>();
			var smaller = left;
			var bigger = right;

			if (left.Count > right.Count)
			{
				smaller = right;
				bigger = left;
			}

			foreach (var entry in smaller)
			{
				if (bigger.Contains(entry))
				{
					result.Add(entry.Key, entry.Value);
				}
			}

			return result;
		}

		public override IDictionary<Variable, Operand> Transfer(CFGNode node, IDictionary<Variable, Operand> input)
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
				foreach (var variable in instruction.ModifiedVariables)
				{
					this.RemoveCopiesWithVariable(result, variable);
				}

				var assignment = this.IsCopy(instruction);

				if (assignment != null)
				{
					var operand = assignment.Operand as Operand;
					var variable = operand as Variable;

					if (variable != null && result.ContainsKey(variable))
					{
						operand = result[variable];
					}

					result.Add(assignment.Result, operand);
				}
			}

			return result;
		}

		private void GenerateGen()
		{
			GEN = new IDictionary<Variable, Operand>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = this.Transfer(node, null);
				GEN[node.Id] = gen;
			}
		}

		private void GenerateKill()
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

		private AssignmentInstruction IsCopy(Instruction instruction)
		{
			var assignment = instruction as AssignmentInstruction;

			if (assignment != null &&
				(assignment.Operand is Variable || assignment.Operand is Constant))
			{
				return assignment;
			}

			return null;
		}
	}
}
