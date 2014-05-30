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

	public class CopyPropagationAnalysis : ForwardDataFlowAnalysis<ISet<Copy>> 
	{
		private ISet<Copy>[] GEN;
		private ISet<Variable>[] KILL;

		public CopyPropagationAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.GenerateGen();
			this.GenerateKill();
		}

		public override ISet<Copy> EntryInitialValue
		{
			get { return new HashSet<Copy>(); }
		}

		public override ISet<Copy> DefaultValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		public override bool CompareValues(ISet<Copy> left, ISet<Copy> right)
		{
			return left.SetEquals(right);
		}

		public override ISet<Copy> Meet(ISet<Copy> left, ISet<Copy> right)
		{
			var result = new HashSet<Copy>(left);

			result.IntersectWith(right);
			return result;
		}

		public override ISet<Copy> Transfer(CFGNode node, ISet<Copy> input)
		{
			var output = new HashSet<Copy>(input);
			var gen = GEN[node.Id];
			var kill = KILL[node.Id];

			foreach (var variable in kill)
			{
				this.RemoveCopiesWithVariable(output, variable);
			}

			output.UnionWith(gen);
			return output;
		}

		private void GenerateGen()
		{
			GEN = new ISet<Copy>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = new HashSet<Copy>();

				foreach (var instruction in node.Instructions)
				{
					foreach (var variable in instruction.ModifiedVariables)
					{
						this.RemoveCopiesWithVariable(gen, variable);
					}

					var unaryInstruction = this.IsCopy(instruction);

					if (unaryInstruction != null)
					{
						var copy = new Copy(unaryInstruction);
						gen.Add(copy);
					}
				}

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
					foreach (var variable in instruction.ModifiedVariables)
					{
						kill.Add(variable);
					}
				}

				KILL[node.Id] = kill;
			}
		}

		private void RemoveCopiesWithVariable(ISet<Copy> copies, Variable variable)
		{
			var array = copies.ToArray();

			foreach (var copy in array)
			{
				if (copy.Target == variable ||
					copy.Source == variable)
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
