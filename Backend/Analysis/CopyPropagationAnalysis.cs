using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.Instructions;
using Backend.Utils;
using Backend.Operands;

namespace Backend.Analysis
{
	#region struct Copy

	public struct Copy
	{
		public Variable Target;
		public Operand Source;

		public Copy(Variable target, Operand source)
		{
			this.Target = target;
			this.Source = source;
		}

		public Copy(UnaryInstruction instruction)
		{
			this.Target = instruction.Result;
			this.Source = instruction.Operand;
		}

		public override string ToString()
		{
			return string.Format("{0} = {1}", this.Target, this.Source);
		}
	}

	#endregion

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

		public override IDictionary<Variable, Operand> Meet(IDictionary<Variable, Operand> left, IDictionary<Variable, Operand> right)
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
					result.Add(entry.Key, entry.Value);
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

				var unaryInstruction = this.IsCopy(instruction);

				if (unaryInstruction != null)
				{
					var operand = unaryInstruction.Operand;
					var variable = operand as Variable;

					if (variable != null && result.ContainsKey(variable))
					{
						operand = result[variable];
					}

					result.Add(unaryInstruction.Result, operand);
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

		private UnaryInstruction IsCopy(Instruction instruction)
		{
			var unaryInstruction = instruction as UnaryInstruction;

			if (unaryInstruction != null &&
				unaryInstruction.Operation == UnaryOperation.Assign &&
				(unaryInstruction.Operand is Variable || unaryInstruction.Operand is Constant))
			{
				return unaryInstruction;
			}

			return null;
		}
	}
}
