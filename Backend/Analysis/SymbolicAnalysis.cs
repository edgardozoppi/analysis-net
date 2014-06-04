using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Backend.ThreeAddressCode;
using Backend.Utils;

namespace Backend.Analysis
{
	public class SymbolicAnalysis : ForwardDataFlowAnalysis<IDictionary<Variable, IExpression>>
	{
		private IDictionary<Variable, IExpression>[] GEN;
		private ISet<Variable>[] KILL;

		public SymbolicAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.GenerateGen();
			this.GenerateKill();
		}

		public override IDictionary<Variable, IExpression> EntryInitialValue
		{
			get { return new Dictionary<Variable, IExpression>(); }
		}

		public override IDictionary<Variable, IExpression> DefaultValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		public override bool CompareValues(IDictionary<Variable, IExpression> left, IDictionary<Variable, IExpression> right)
		{
			return left.SequenceEqual(right);
		}

		public override IDictionary<Variable, IExpression> Merge(IDictionary<Variable, IExpression> left, IDictionary<Variable, IExpression> right)
		{
			var result = new Dictionary<Variable, IExpression>();
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

		public override IDictionary<Variable, IExpression> Transfer(CFGNode node, IDictionary<Variable, IExpression> input)
		{
			IDictionary<Variable, IExpression> result;

			if (input == null)
			{
				result = new Dictionary<Variable, IExpression>();
			}
			else
			{
				result = new Dictionary<Variable, IExpression>(input);
			}

			foreach (var instruction in node.Instructions)
			{
				foreach (var variable in instruction.ModifiedVariables)
				{
					this.RemoveEqualitiesWithVariable(result, variable);
				}

				this.TransferInstruction(instruction, result);
			}

			return result;
		}

		private void GenerateGen()
		{
			GEN = new IDictionary<Variable, IExpression>[this.cfg.Nodes.Count];

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

		private void RemoveEqualitiesWithVariable(IDictionary<Variable, IExpression> equalities, Variable variable)
		{
			var array = equalities.ToArray();

			foreach (var equality in array)
			{
				if (equality.Key == variable ||
					equality.Value == variable)
				{
					equalities.Remove(equality);
				}
			}
		}

		private void TransferInstruction(Instruction instruction, IDictionary<Variable, IExpression> equalities)
		{
			if (instruction is AssignmentInstruction)
			{
				var assignment = instruction as AssignmentInstruction;
				var expr = assignment.Operand.ReplaceVariables(equalities);
				equalities.Add(assignment.Result, expr);
			}
		}
	}
}
