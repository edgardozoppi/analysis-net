using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Instructions;
using Model;

namespace Backend.Analysis
{
	public class CopyPropagationAnalysis : ForwardDataFlowAnalysis<IDictionary<IVariable, IInmediateValue>> 
	{
		private DataFlowAnalysisResult<IDictionary<IVariable, IInmediateValue>>[] result;
		private IDictionary<IVariable, IInmediateValue>[] GEN;
		private ISet<IVariable>[] KILL;

		public CopyPropagationAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public override DataFlowAnalysisResult<IDictionary<IVariable, IInmediateValue>>[] Analyze()
		{
			this.ComputeGen();
			this.ComputeKill();

			var result = base.Analyze();

			this.result = result;
			this.GEN = null;
			this.KILL = null;

			return result;
		}

		protected override IDictionary<IVariable, IInmediateValue> InitialValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		protected override bool Compare(IDictionary<IVariable, IInmediateValue> left, IDictionary<IVariable, IInmediateValue> right)
		{
			return left.SequenceEqual(right);
		}

		protected override IDictionary<IVariable, IInmediateValue> Join(IDictionary<IVariable, IInmediateValue> left, IDictionary<IVariable, IInmediateValue> right)
		{
			var result = new Dictionary<IVariable, IInmediateValue>(left);

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

		protected override IDictionary<IVariable, IInmediateValue> Flow(CFGNode node, IDictionary<IVariable, IInmediateValue> input)
		{
			IDictionary<IVariable, IInmediateValue> result;

			if (input == null)
			{
				result = new Dictionary<IVariable, IInmediateValue>();
			}
			else
			{
				result = new Dictionary<IVariable, IInmediateValue>(input);
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
			GEN = new IDictionary<IVariable, IInmediateValue>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = this.Flow(node, null);
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

		private void RemoveCopiesWithVariable(IDictionary<IVariable, IInmediateValue> copies, IVariable variable)
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

		private KeyValuePair<IVariable, IInmediateValue>? Flow(IInstruction instruction, IDictionary<IVariable, IInmediateValue> copies)
		{
			KeyValuePair<IVariable, IInmediateValue>? result = null;

			if (instruction is DefinitionInstruction)
			{
				var definition = instruction as DefinitionInstruction;

				if (definition.HasResult)
				{
					result = new KeyValuePair<IVariable, IInmediateValue>(definition.Result, UnknownValue.Value);
				}
				
				if (definition is LoadInstruction)
				{
					var assignment = definition as LoadInstruction;

					if (assignment.Operand is Constant)
					{
						var constant = assignment.Operand as Constant;
						result = new KeyValuePair<IVariable, IInmediateValue>(assignment.Result, constant);
					}
					else if (assignment.Operand is IVariable)
					{
						var variable = assignment.Operand as IVariable;
						IInmediateValue operand = variable;

						if (copies.ContainsKey(variable))
						{
							operand = copies[variable];
						}

						result = new KeyValuePair<IVariable, IInmediateValue>(assignment.Result, operand);
					}
				}
			}

			return result;
		}
	}
}
