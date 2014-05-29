using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.Instructions;
using Backend.Utils;
using Backend.Operands;

namespace Backend.Analysis
{
	struct Copy
	{
		public Variable Target;
		public Operand Source;

		public Copy(Variable target, Operand source)
		{
			this.Target = target;
			this.Source = source;
		}

		public override string ToString()
		{
			return string.Format("{0} = {1}", this.Target, this.Source);
		}
	}

	public class CopyPropagationAnalysis : ForwardDataFlowAnalysis<ISet<Copy>> 
	{
		private ISet<Copy> GEN;
		private ISet<Copy> KILL;

		public CopyPropagationAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public override ISet<Copy> EntryInitialValue
		{
			get { return new HashSet<Copy>(); }
		}

		public override ISet<Copy> DefaultValue(CFGNode node)
		{
			return GEN;
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

		public override IDictionary<Variable, Operand> Transfer(CFGNode node, IDictionary<Variable, Operand> nodeIN)
		{
			var nodeOUT = new Dictionary<Variable, Operand>(nodeIN);

			foreach (var instruction in node.Instructions)
			{
				foreach (var variable in instruction.ModifiedVariables)
				{
					nodeOUT.Remove(variable);
					var entriesWithVariable = nodeOUT.Where(e => e.Value == variable);

					foreach (var entry in entriesWithVariable)
						nodeOUT.Remove(entry.Key);
				}

				var copy = instruction as UnaryInstruction;

				if (copy != null &&
					copy.Operation == UnaryOperation.Assign &&
					(copy.Operand is Variable || copy.Operand is Constant))
				{
					nodeOUT[copy.Result] = copy.Operand;
				}
			}

			return nodeOUT;
		}
	}
}
