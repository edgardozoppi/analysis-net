using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Backend.ThreeAddressCode;
using Backend.Utils;

namespace Backend.Analysis
{
	public class StaticSingleAssignmentAnalysis : ForwardDataFlowAnalysis<IDictionary<Variable, ISet<uint>>>
	{
		private MethodBody method;
		private IDictionary<Variable, uint>[] nodesWithPhi;

		public StaticSingleAssignmentAnalysis(MethodBody method, ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.method = method;
			this.nodesWithPhi = new IDictionary<Variable, uint>[cfg.Nodes.Count];
		}

		public override IDictionary<Variable, ISet<uint>> InitialValue(CFGNode node)
		{
			var result = new Dictionary<Variable, ISet<uint>>();

			if (node.Kind == CFGNodeKind.Entry)
			{
				var set = new HashSet<uint>() { 0 };

				foreach (var variable in method.Variables)
				{					
					result.Add(variable, set);
				}
			}

			return result;
		}

		public override IDictionary<Variable, ISet<uint>> DefaultValue(CFGNode node)
		{
			return new Dictionary<Variable, ISet<uint>>();
		}

		public override bool CompareValues(IDictionary<Variable, ISet<uint>> left, IDictionary<Variable, ISet<uint>> right)
		{
			var result = true;

			if (left.Count == right.Count)
			{
				foreach (var entry in left)
				{
					if (!right.ContainsKey(entry.Key))
					{
						result = false;
						break;
					}

					var value = right[entry.Key];

					if (!entry.Value.SetEquals(value))
					{
						result = false;
						break;
					}
				}
			}
			else
			{
				result = false;
			}

			return result;
		}
		
		public override IDictionary<Variable, ISet<uint>> MergeValues(IDictionary<Variable, ISet<uint>> left, IDictionary<Variable, ISet<uint>> right)
		{
			var result = new Dictionary<Variable, ISet<uint>>();

			foreach (var entry in left)
			{
				var variable = entry.Key;
				var leftIndices = entry.Value;

				result[variable] = new HashSet<uint>(leftIndices);
			}

			foreach (var entry in right)
			{
				var variable = entry.Key;
				var rightIndices = entry.Value;

				if (left.ContainsKey(variable))
				{
					var indices = result[variable];

					indices.UnionWith(rightIndices);
				}
				else
				{
					result[variable] = new HashSet<uint>(rightIndices);
				}
			}

			return result;
		}

		public override IDictionary<Variable, ISet<uint>> Flow(CFGNode node, IDictionary<Variable, ISet<uint>> input)
		{
			var result = new Dictionary<Variable, ISet<uint>>();
			var variablesWithPhi = this.nodesWithPhi[node.Id];

			foreach (var entry in input)
			{
				var variable = entry.Key;
				var oldIndices = entry.Value;
				var newIndices = new HashSet<uint>();
				uint index;

				if (oldIndices.Count > 1)
				{
					var variableHasPhi = variablesWithPhi != null && variablesWithPhi.ContainsKey(variable);

					if (variableHasPhi)
					{
						index = variablesWithPhi[variable];
					}
					else
					{
						index = oldIndices.Max() + 1;

						if (variablesWithPhi == null)
						{
							variablesWithPhi = new Dictionary<Variable, uint>();
							this.nodesWithPhi[node.Id] = variablesWithPhi;
						}

						variablesWithPhi.Add(variable, index);
					}
				}
				else
				{
					index = oldIndices.Single();
				}

				newIndices.Add(index);
				result[variable] = newIndices;
			}

			foreach (var instruction in node.Instructions)
			{
				this.Flow(instruction, result);
			}

			return result;
		}

		private void Flow(Instruction instruction, IDictionary<Variable, ISet<uint>> result)
		{
			if (instruction is AssignmentInstruction)
			{
				var assignment = instruction as AssignmentInstruction;
				var variable = assignment.Result;

				if (result.ContainsKey(variable))
				{
					var indices = result[variable];
					var index = indices.Single();
					indices.Remove(index);
					indices.Add(index + 1);
				}
				else
				{
					var indices = new HashSet<uint>();
					indices.Add(1);
					result[variable] = indices;
				}
			}
		}

		public void Transform()
		{
			foreach (var node in cfg.Nodes)
			{
				if (node.Kind != CFGNodeKind.BasicBlock)
					continue;

				var variablesWithPhi = this.nodesWithPhi[node.Id];

				if (variablesWithPhi == null)
				{
					variablesWithPhi = new Dictionary<Variable, uint>();
				}

				var node_result = this.result[node.Id];
				var input = node_result.Input;

				foreach (var entry in variablesWithPhi)
				{
					Instruction instruction;
					var indices = input[entry.Key];

					instruction = new PhiInstruction(0, entry.Key, entry.Value, indices.ToList());
					node.Instructions.Insert(0, instruction);
					//else
					//{
					//	var index = entry.Value.Single();
					//	if (index == 0) continue;

					//	var result = new DerivedVariable(entry.Key, resultIndex);
					//	var operand = new DerivedVariable(entry.Key, index);
					//	instruction = new AssignmentInstruction(0, result, operand);
					//}

					//node.Instructions.Insert(0, instruction);
				}

				foreach (var entry in input)
				{
					if (entry.Value.Count > 1) continue;
					var index = entry.Value.Single();

					variablesWithPhi.Add(entry.Key, index);
				}

				foreach (var instruction in node.Instructions)
				{
					this.Transform(instruction, variablesWithPhi);
				}
			}
		}

		private void Transform(Instruction instruction, IDictionary<Variable, uint> variablesWithPhi)
		{
			if (instruction is AssignmentInstruction)
			{
				var assignment = instruction as AssignmentInstruction;
				var variable = assignment.Result;

				if (variablesWithPhi.ContainsKey(variable))
				{
					var index = variablesWithPhi[variable] + 1;

					assignment.Result = new DerivedVariable(variable, index);
					variablesWithPhi[variable] = index;
				}
			}
		}
	}
}
