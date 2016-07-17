using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Instructions;
using Model;
using Backend.Model;
using Backend.Utils;
using Model.Types;

namespace Backend.Analyses
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

		public void Transform(MethodBody body)
		{
			if (this.result == null) throw new InvalidOperationException("Analysis result not available.");

			foreach (var node in this.cfg.Nodes)
			{
				var node_result = this.result[node.Id];
				var copies = new Dictionary<IVariable, IInmediateValue>();

				if (node_result.Input != null)
				{
					foreach (var copy in node_result.Input)
					{
						// Only replace temporal variables
						if (copy.Key.IsTemporal() &&
							copy.Value is IVariable)
						{
							copies.Add(copy.Key, copy.Value);
						}
					}
				}

				for (var i = 0; i < node.Instructions.Count; ++i)
				{
					var instruction = node.Instructions[i];
					var copy = this.Flow(instruction, copies);

					foreach (var variable in instruction.UsedVariables)
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

					if (copy.HasValue &&
						// Only replace temporal variables
						copy.Value.Key.IsTemporal() &&
						copy.Value.Value is IVariable)
					{
						copies.Add(copy.Value.Key, copy.Value.Value);

						// Remove the copy instruction
						body.Instructions.Remove(instruction);
						node.Instructions.RemoveAt(i);
						--i;
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

		protected override IDictionary<IVariable, IInmediateValue> Flow(CFGNode node, IDictionary<IVariable, IInmediateValue> input)
		{
			var output = new Dictionary<IVariable, IInmediateValue>(input);
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
			GEN = new IDictionary<IVariable, IInmediateValue>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = new Dictionary<IVariable, IInmediateValue>();

				foreach (var instruction in node.Instructions)
				{
					var copy = this.Flow(instruction, gen);

					foreach (var variable in instruction.ModifiedVariables)
					{
						this.RemoveCopiesWithVariable(gen, variable);
					}

					if (copy.HasValue)
					{
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

						// Only replace temporal variables
						if (variable.IsTemporal() &&
							copies.ContainsKey(variable) &&
							copies[variable] != UnknownValue.Value)
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
