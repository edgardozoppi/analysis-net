// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Backend.ThreeAddressCode;
using Backend.Utils;
using Backend.ThreeAddressCode.Values;
using Backend.ThreeAddressCode.Instructions;
using Backend.Model;

namespace Backend.Analyses
{
	public class ForwardCopyPropagationAnalysis : ForwardDataFlowAnalysis<IDictionary<IVariable, IVariable>>
	{
		#region struct InstructionLocation

		private struct InstructionLocation
		{
			public CFGNode CFGNode;
			public Instruction Instruction;
		}

		#endregion

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
			var copiesToRemove = new Dictionary<IVariable, InstructionLocation>();

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
						// If the variable definition is marked to be removed
						// but it is not a copy reaching this instruction,
						// then we cannot remove the definition because the
						// variable cannot be replaced with a copy.
						if (copiesToRemove.ContainsKey(variable) &&
							!copies.ContainsKey(variable))
						{
							copiesToRemove.Remove(variable);
						}
						// Only replace temporal variables
						else if (variable.IsTemporal() &&
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
						var definition = instruction as DefinitionInstruction;

						// Mark the copy instruction to be removed later
						var location = new InstructionLocation()
						{
							CFGNode = node,
							Instruction = instruction
						};

						// TODO: Check this, not sure if this is the right fix.
						if (!copiesToRemove.ContainsKey(definition.Result))
						{
							copiesToRemove.Add(definition.Result, location);
						}
					}
				}
			}

			// Remove unnecessary copy instructions
			foreach (var location in copiesToRemove.Values)
			{
				var bodyIndex = body.Instructions.IndexOf(location.Instruction);
				var nodeIndex = location.CFGNode.Instructions.IndexOf(location.Instruction);

				if (nodeIndex == 0)
				{
					// The copy is the first instruction of the basic block
					// Replace the copy instruction with a nop to preserve branch targets
					var nop = new NopInstruction(location.Instruction.Offset);
					body.Instructions[bodyIndex] = nop;
					location.CFGNode.Instructions[nodeIndex] = nop;
				}
				else
				{
					// The copy is not the first instruction of the basic block
					body.Instructions.RemoveAt(bodyIndex);
					location.CFGNode.Instructions.RemoveAt(nodeIndex);
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
			Func<IVariable, IVariable, IVariable> intersectVariables = (a, b) => a.Equals(b) ? a : null;
			var result = left.Intersect(right, intersectVariables);
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

			output.SetRange(gen);
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

			// Here we are also removing 'left'.
			foreach (var variable in instruction.ModifiedVariables)
			{
				this.RemoveCopiesWithVariable(copies, variable);
			}

			if (isCopy)
			{
				// 'left' should be already removed.
				//this.RemoveCopiesWithVariable(copies, left);
				copies.Add(left, right);
			}

			var result = isCopy && left.IsTemporal();
			return result;
		}
	}
}
