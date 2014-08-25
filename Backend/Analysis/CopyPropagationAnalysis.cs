using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Backend.ThreeAddressCode;
using Backend.Utils;

namespace Backend.Analysis
{
	public class CopyPropagationAnalysis : ForwardDataFlowAnalysis<IDictionary<Variable, IInmediateValue>> 
	{
		private IDictionary<Variable, IInmediateValue>[] GEN;
		private ISet<Variable>[] KILL;

		public CopyPropagationAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.ComputeGen();
			this.ComputeKill();
		}

		protected override IDictionary<Variable, IInmediateValue> InitialValue(CFGNode node)
		{
			return new Dictionary<Variable, IInmediateValue>();
		}

		protected override IDictionary<Variable, IInmediateValue> DefaultValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		protected override bool Compare(IDictionary<Variable, IInmediateValue> left, IDictionary<Variable, IInmediateValue> right)
		{
			return left.SequenceEqual(right);
		}

		protected override IDictionary<Variable, IInmediateValue> Join(IDictionary<Variable, IInmediateValue> left, IDictionary<Variable, IInmediateValue> right)
		{
			var result = new Dictionary<Variable, IInmediateValue>(left);

			foreach (var copy in right)
			{
				var variable = copy.Key;
				var rightOperand = copy.Value;

				if (left.ContainsKey(variable))
				{
					var leftOperand = left[variable];

					if (!leftOperand.Equals(rightOperand))
					{
						result[variable] = UnknownValue.Value;
					}
				}
				else
				{
					result[variable] = rightOperand;
				}
			}

			return result;
		}

		protected override IDictionary<Variable, IInmediateValue> Flow(CFGNode node, IDictionary<Variable, IInmediateValue> input)
		{
			IDictionary<Variable, IInmediateValue> result;

			if (input == null)
			{
				result = new Dictionary<Variable, IInmediateValue>();
			}
			else
			{
				result = new Dictionary<Variable, IInmediateValue>(input);
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
			GEN = new IDictionary<Variable, IInmediateValue>[this.cfg.Nodes.Count];

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

		private void RemoveCopiesWithVariable(IDictionary<Variable, IInmediateValue> copies, Variable variable)
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

		private KeyValuePair<Variable, IInmediateValue>? Flow(Instruction instruction, IDictionary<Variable, IInmediateValue> copies)
		{
			KeyValuePair<Variable, IInmediateValue>? result = null;

			if (instruction is DefinitionInstruction)
			{
				var definition = instruction as DefinitionInstruction;

				if (definition.HasResult)
				{
					result = new KeyValuePair<Variable, IInmediateValue>(definition.Result, UnknownValue.Value);
				}
				
				if (definition is LoadInstruction)
				{
					var assignment = definition as LoadInstruction;

					if (assignment.Operand is Constant)
					{
						var constant = assignment.Operand as Constant;
						result = new KeyValuePair<Variable, IInmediateValue>(assignment.Result, constant);
					}
					else if (assignment.Operand is Variable)
					{
						var variable = assignment.Operand as Variable;
						IInmediateValue operand = variable;

						if (copies.ContainsKey(variable))
						{
							operand = copies[variable];
						}

						result = new KeyValuePair<Variable, IInmediateValue>(assignment.Result, operand);
					}
				}
			}

			return result;
		}
	}
}
