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
	[Obsolete("The analysis implementation could have some bugs!")]
	public class BackwardCopyPropagationAnalysis : BackwardDataFlowAnalysis<IDictionary<IVariable, IInmediateValue>> 
	{
		private DataFlowAnalysisResult<IDictionary<IVariable, IInmediateValue>>[] result;
		private IDictionary<IVariable, IInmediateValue>[] GEN;
		private ISet<IVariable>[] KILL;

		public BackwardCopyPropagationAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public void Transform(MethodBody body)
		{
			if (this.result == null) throw new InvalidOperationException("Analysis result not available.");

			foreach (var node in this.cfg.Nodes)
			{
				var node_result = this.result[node.Id];
				var copies = new Dictionary<IVariable, IInmediateValue>();

				if (node_result.Output != null)
				{
					foreach (var copy in node_result.Output)
					{
						// Only replace temporal variables
						if (copy.Key.IsTemporal() &&
							copy.Value is IVariable)
						{
							copies.Add(copy.Key, copy.Value);
						}
					}
				}

				for (var i = node.Instructions.Count - 1; i >= 0; --i)
				{
					var instruction = node.Instructions[i];
					var copy = this.Flow(instruction, copies);

					foreach (var variable in instruction.ModifiedVariables)
					{
						if (copies.ContainsKey(variable))
						{
							var operand = copies[variable] as IVariable;

							instruction.Replace(variable, operand);
						}
					}

					foreach (var variable in instruction.ModifiedVariables)
					{
						this.RemoveCopiesWithVariable(copies, variable);
					}

					foreach (var variable in instruction.UsedVariables)
					{
						if (copies.ContainsKey(variable))
						{
							var operand = copies[variable] as IVariable;

							instruction.Replace(variable, operand);
						}
					}

					if (copy.HasValue &&
						// Only replace temporal variables
						copy.Value.Key.IsTemporal() &&
						copy.Value.Value is IVariable)
					{
						this.RemoveCopiesWithVariable(copies, copy.Value.Key);
						copies[copy.Value.Key] = copy.Value.Value;

						// Remove the copy instruction
						body.Instructions.Remove(instruction);
						node.Instructions.RemoveAt(i);
					}
				}
			}

			body.UpdateVariables();
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
			return left.DictionaryEquals(right);
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

		protected override IDictionary<IVariable, IInmediateValue> Flow(CFGNode node, IDictionary<IVariable, IInmediateValue> output)
		{
			var input = new Dictionary<IVariable, IInmediateValue>(output);
			var kill = KILL[node.Id];
			var gen = GEN[node.Id];

			foreach (var variable in kill)
			{
				this.RemoveCopiesWithVariable(input, variable);
			}

			input.AddRange(gen);
			return input;
		}

		private void ComputeGen()
		{
			GEN = new IDictionary<IVariable, IInmediateValue>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = new Dictionary<IVariable, IInmediateValue>();

				for (var i = node.Instructions.Count - 1; i >= 0; --i)
				{
					var instruction = node.Instructions[i];
					var copy = this.Flow(instruction, gen);

					foreach (var variable in instruction.ModifiedVariables)
					{
						this.RemoveCopiesWithVariable(gen, variable);
					}

					if (copy.HasValue)
					{
						this.RemoveCopiesWithVariable(gen, copy.Value.Key);
						gen.Add(copy.Value.Key, copy.Value.Value);
					}
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

		private KeyValuePair<IVariable, IInmediateValue>? Flow(Instruction instruction, IDictionary<IVariable, IInmediateValue> copies)
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

					if (assignment.Operand is IVariable)
					{
						var operand = assignment.Operand as IVariable;

						// Only replace temporal variables
						if (operand.IsTemporal() &&
							copies.ContainsKey(assignment.Result) &&
							copies[assignment.Result] != UnknownValue.Value)
						{
							operand = copies[assignment.Result] as IVariable;
						}

						result = new KeyValuePair<IVariable, IInmediateValue>(operand, assignment.Result);
					}
				}
			}

			return result;
		}
	}
}
