// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;
using Model;
using Backend.Model;
using Model.ThreeAddressCode.Visitor;

namespace Backend.Analyses
{
	// Intraprocedural May Points-To Analysis
    public class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToGraph>
    {
		#region class TransferFunction

		private class TransferFunction : InstructionVisitor
		{
			private UniqueIDGenerator nodeIdGenerator;
			private IDictionary<uint, int> nodeIdAtOffset;
			private IVariable resultVariable;
			private PointsToGraph ptg;

			public TransferFunction(IType returnType, UniqueIDGenerator nodeIdGenerator)
			{
				this.nodeIdGenerator = nodeIdGenerator;
				this.nodeIdAtOffset = new Dictionary<uint, int>();

				if (returnType.TypeKind != TypeKind.ValueType)
				{
					this.resultVariable = new LocalVariable("$result");
					this.resultVariable.Type = returnType;
				}
			}

			public Func<MethodCallInstruction, UniqueIDGenerator, PointsToGraph, PointsToGraph> ProcessMethodCall;

			public IVariable ResultVariable
			{
				get { return resultVariable; }
			}

			public PointsToGraph Evaluate(CFGNode node, PointsToGraph input)
			{
				//ptg = input.Clone();
				this.ptg = input;
				Visit(node);
				var result = this.ptg;
				this.ptg = null;
				return result;
			}

			#region Visit methods

			public override void Visit(CreateObjectInstruction instruction)
			{
				ProcessAllocation(instruction.Offset, instruction.Result);
			}

			public override void Visit(CreateArrayInstruction instruction)
			{
				ProcessAllocation(instruction.Offset, instruction.Result);
			}

			public override void Visit(LoadInstruction instruction)
			{
				if (instruction.Operand is Constant)
				{
					var constant = instruction.Operand as Constant;

					if (constant.Value == null)
					{
						ProcessNull(instruction.Result);
					}
				}
				else if (instruction.Operand is IVariable)
				{
					var variable = instruction.Operand as IVariable;
					ProcessCopy(instruction.Result, variable);
				}
				else if (instruction.Operand is InstanceFieldAccess)
				{
					var access = instruction.Operand as InstanceFieldAccess;
					ProcessLoad(instruction.Offset, instruction.Result, access);
				}
			}

			public override void Visit(StoreInstruction instruction)
			{
				if (instruction.Result is InstanceFieldAccess)
				{
					var access = instruction.Result as InstanceFieldAccess;
					ProcessStore(access, instruction.Operand);
				}
			}

			public override void Visit(ReturnInstruction instruction)
			{
				if (instruction.HasOperand)
				{
					ProcessReturn(instruction.Operand);
				}
			}

			public override void Visit(MethodCallInstruction instruction)
			{
				if (ProcessMethodCall != null)
				{
					ptg = ProcessMethodCall(instruction, nodeIdGenerator, ptg);
				}
			}

			#endregion

			#region Private methods

			private void ProcessNull(IVariable dst)
			{
				if (dst.Type.TypeKind == TypeKind.ValueType) return;

				ptg.RemoveEdges(dst);
				ptg.PointsTo(dst, ptg.Null);
			}

			private void ProcessReturn(IVariable src)
			{
				if (src.Type.TypeKind == TypeKind.ValueType) return;

				// Weak update to preserve all possible return values
				var targets = ptg.GetTargets(src);

				foreach (var target in targets)
				{
					ptg.PointsTo(resultVariable, target);
				}
			}

			private void ProcessAllocation(uint offset, IVariable dst)
			{
				if (dst.Type.TypeKind == TypeKind.ValueType) return;

				var node = GetOrCreateNode(offset, dst.Type, PTGNodeKind.Object);

				ptg.RemoveEdges(dst);
				ptg.PointsTo(dst, node);
			}

			private void ProcessCopy(IVariable dst, IVariable src)
			{
				if (dst.Type.TypeKind == TypeKind.ValueType || src.Type.TypeKind == TypeKind.ValueType) return;

				// Avoid the following case:
				// v = v
				// Otherwise we will need to copy the
				// targets before calling RemoveEdges.
				if (dst.Equals(src)) return;

				var targets = ptg.GetTargets(src);
				ptg.RemoveEdges(dst);

				foreach (var target in targets)
				{
					ptg.PointsTo(dst, target);
				}
			}

			private void ProcessLoad(uint offset, IVariable dst, InstanceFieldAccess access)
			{
				if (dst.Type.TypeKind == TypeKind.ValueType || access.Type.TypeKind == TypeKind.ValueType) return;

				IEnumerable<PTGNode> nodes = ptg.GetTargets(access.Instance);

				// We need to copy the targets before calling
				// RemoveEdges because of the following case:
				// v = v.f
				// where dst == access.Instance == v
				if (dst.Equals(access.Instance))
				{
					nodes = nodes.ToArray();
				}

				ptg.RemoveEdges(dst);

				foreach (var node in nodes)
				{
					var hasField = node.Targets.ContainsKey(access.Field);

					// TODO: Don't create an unknown node when doing the inter PT analysis
					if (!hasField)
					{
						var target = GetOrCreateNode(offset, dst.Type, PTGNodeKind.Unknown);

						ptg.PointsTo(node, access.Field, target);
					}

					var targets = node.Targets[access.Field];

					foreach (var target in targets)
					{
						ptg.PointsTo(dst, target);
					}
				}
			}

			private void ProcessStore(InstanceFieldAccess access, IVariable src)
			{
				if (access.Type.TypeKind == TypeKind.ValueType || src.Type.TypeKind == TypeKind.ValueType) return;

				// Weak update
				var nodes = ptg.GetTargets(access.Instance);
				var targets = ptg.GetTargets(src);

				foreach (var node in nodes)
				{
					foreach (var target in targets)
					{
						ptg.PointsTo(node, access.Field, target);
					}
				}
			}

			private PTGNode GetOrCreateNode(uint offset, IType type, PTGNodeKind kind)
			{
				PTGNode node;
				int nodeId;

				var ok = nodeIdAtOffset.TryGetValue(offset, out nodeId);

				if (ok)
				{
					// Get already existing node
					node = ptg.GetNode(nodeId);
				}
				else
				{
					// Create a new node
					nodeId = nodeIdGenerator.Next;
					node = new PTGNode(nodeId, type, kind, offset);

					ptg.Add(node);
					nodeIdAtOffset.Add(offset, nodeId);
				}

				return node;
			}

			#endregion
		}

		#endregion

		private PointsToGraph initialGraph;
		private UniqueIDGenerator nodeIdGenerator;
		private TransferFunction transferFunction;

		public PointsToAnalysis(ControlFlowGraph cfg, IType returnType)
			: base(cfg)
		{
			this.nodeIdGenerator = new UniqueIDGenerator(1);
			this.transferFunction = new TransferFunction(returnType, nodeIdGenerator);
			this.initialGraph = CreateInitialGraph();
		}

		public PointsToAnalysis(ControlFlowGraph cfg, IType returnType, UniqueIDGenerator nodeIdGenerator, PointsToGraph ptg)
			: base(cfg)
		{
            this.nodeIdGenerator = nodeIdGenerator;
			this.transferFunction = new TransferFunction(returnType, nodeIdGenerator);
			this.initialGraph = CreateInitialGraph(ptg);
		}

		public Func<MethodCallInstruction, UniqueIDGenerator, PointsToGraph, PointsToGraph> ProcessMethodCall
		{
			get { return transferFunction.ProcessMethodCall; }
			set { transferFunction.ProcessMethodCall = value; }
		}

		public IVariable ResultVariable
		{
			get { return transferFunction.ResultVariable; }
		}

		protected override PointsToGraph InitialValue(CFGNode node)
        {
			return this.initialGraph;
        }

        protected override bool Compare(PointsToGraph left, PointsToGraph right)
        {
            return left.GraphEquals(right);
        }

        protected override PointsToGraph Join(PointsToGraph left, PointsToGraph right)
        {
			var result = left.Clone();
			result.Union(right);
			return result;
        }

        protected override PointsToGraph Flow(CFGNode node, PointsToGraph input)
        {
            input = input.Clone();
			var output = transferFunction.Evaluate(node, input);
            return output;
        }

		private PointsToGraph CreateInitialGraph()
		{
			var ptg = new PointsToGraph();
			var variables = cfg.GetVariables();

			foreach (var variable in variables)
			{
				if (variable.Type.TypeKind == TypeKind.ValueType) continue;

				if (variable.IsParameter)
				{
					var isThisParameter = variable.Name == "this";
					var kind = isThisParameter ? PTGNodeKind.Object : PTGNodeKind.Unknown;
					var nodeId = nodeIdGenerator.Next;
					var node = new PTGNode(nodeId, variable.Type, kind);

					ptg.Add(node);
					ptg.PointsTo(variable, node);
				}
				else
				{
					ptg.Add(variable);
				}
			}

			if (this.ResultVariable != null)
			{
				ptg.Add(this.ResultVariable);
			}

			return ptg;
		}

		private PointsToGraph CreateInitialGraph(PointsToGraph ptg)
		{
			// Add all variables except parameters
			var variables = cfg.GetVariables();

			foreach (var variable in variables)
			{
				if (variable.Type.TypeKind == TypeKind.ValueType) continue;

				if (!variable.IsParameter)
				{
					ptg.Add(variable);
				}
			}

			if (this.ResultVariable != null)
			{
				ptg.Add(this.ResultVariable);
			}

			return ptg;
		}
    }
}
