using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Backend.ThreeAddressCode;
using Backend.Utils;
using Backend.ThreeAddressCode.Values;
using Backend.ThreeAddressCode.Instructions;

namespace Backend.Analysis
{
	public class ForwardCopyPropagationAnalysis : ForwardDataFlowAnalysis<IDictionary<IVariable, IVariable>>
	{
		private DataFlowAnalysisResult<IDictionary<IVariable, IVariable>>[] result;
		private IDictionary<IVariable, IVariable>[] GEN;
		private ISet<IVariable>[] KILL;

		public ForwardCopyPropagationAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public void Transform(MethodBody body)
		{
			if (this.result == null) throw new InvalidOperationException("Analysis result not available.");

			foreach (var node in this.cfg.Nodes)
			{
				var node_result = this.result[node.Id];
				var copies = new Dictionary<IVariable, IVariable>();

				if (node_result.Input != null)
				{
					copies.AddRange(node_result.Input);
				}

				for (var i = 0; i < node.Instructions.Count; ++i)
				{
					var instruction = node.Instructions[i];

					foreach (var variable in instruction.UsedVariables)
					{
						// Only replace temporal variables
						if (variable.IsTemporal() &&
							copies.ContainsKey(variable))
						{
							var operand = copies[variable];

							instruction.Replace(variable, operand);
						}
					}

					var isTemporalCopy = this.Flow(instruction, copies);

					// Only replace temporal variables
					if (isTemporalCopy)
					{
						// Remove the copy instruction
						if (i == 0)
						{
							// The copy is the first instruction of the basic block
							// Replace the copy instruction with a nop to preserve branch targets
							var nop = new NopInstruction(instruction.Offset);
							var index = body.Instructions.IndexOf(instruction);
							body.Instructions[index] = nop;
							node.Instructions[i] = nop;
						}
						else
						{
							// The copy is not the first instruction of the basic block
							body.Instructions.Remove(instruction);
							node.Instructions.RemoveAt(i);
							--i;
						}
					}
				}
			}

			body.UpdateVariables();
		}

		public override DataFlowAnalysisResult<IDictionary<IVariable, IVariable>>[] Analyze()
		{
			this.ComputeGen();
			this.ComputeKill();

			var result = base.Analyze();

			this.result = result;
			this.GEN = null;
			this.KILL = null;

			return result;
		}

		protected override IDictionary<IVariable, IVariable> InitialValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		protected override bool Compare(IDictionary<IVariable, IVariable> left, IDictionary<IVariable, IVariable> right)
		{
			return left.DictionaryEquals(right);
		}

		protected override IDictionary<IVariable, IVariable> Join(IDictionary<IVariable, IVariable> left, IDictionary<IVariable, IVariable> right)
		{
			// result = intersection(left, right)
			var result = new Dictionary<IVariable, IVariable>();

			foreach (var copy in left)
			{
				var variable = copy.Key;
				var leftOperand = copy.Value;

				if (right.ContainsKey(variable))
				{
					var rightOperand = right[variable];

					if (leftOperand.Equals(rightOperand))
					{
						result.Add(variable, leftOperand);
					}
				}
			}

			return result;
		}

		protected override IDictionary<IVariable, IVariable> Flow(CFGNode node, IDictionary<IVariable, IVariable> input)
		{
			var output = new Dictionary<IVariable, IVariable>(input);
			var kill = KILL[node.Id];
			var gen = GEN[node.Id];

			foreach (var variable in kill)
			{
				this.RemoveCopiesWithVariable(output, variable);
			}

			output.AddRange(gen);
			return output;
		}

		private void ComputeGen()
		{
			GEN = new IDictionary<IVariable, IVariable>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = new Dictionary<IVariable, IVariable>();

				foreach (var instruction in node.Instructions)
				{
					this.Flow(instruction, gen);
				}

				GEN[node.Id] = gen;
			}
		}

		private void ComputeKill()
		{
			KILL = new ISet<IVariable>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var kill = new HashSet<IVariable>();

				foreach (var instruction in node.Instructions)
				{
					kill.UnionWith(instruction.ModifiedVariables);
				}

				KILL[node.Id] = kill;
			}
		}

		private void RemoveCopiesWithVariable(IDictionary<IVariable, IVariable> copies, IVariable variable)
		{
			var array = copies.ToArray();

			foreach (var copy in array)
			{
				if (copy.Key == variable ||
					copy.Value == variable)
				{
					copies.Remove(copy.Key);
				}
			}
		}

		private bool Flow(Instruction instruction, IDictionary<IVariable, IVariable> copies)
		{
			IVariable left;
			IVariable right;

			var isCopy = instruction.IsCopy(out left, out right);

			if (isCopy)
			{
				// Only replace temporal variables
				if (right.IsTemporal() &&
					copies.ContainsKey(right))
				{
					right = copies[right];
				}
			}

			foreach (var variable in instruction.ModifiedVariables)
			{
				this.RemoveCopiesWithVariable(copies, variable);
			}

			if (isCopy)
			{
				copies.Add(left, right);
			}

			var result = isCopy && left.IsTemporal();
			return result;
		}
	}
}
