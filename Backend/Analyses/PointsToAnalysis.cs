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
			private IMethodReference method;
			private UniqueIDGenerator nodeIdGenerator;
			private IDictionary<uint, PTGNode> nodeAtOffset;
			private PointsToGraph ptg;

			public TransferFunction(IMethodReference method, UniqueIDGenerator nodeIdGenerator)
			{
				this.method = method;
				this.nodeIdGenerator = nodeIdGenerator;
				this.nodeAtOffset = new Dictionary<uint, PTGNode>();
			}

			public Func<IMethodReference, MethodCallInstruction, UniqueIDGenerator, PointsToGraph, PointsToGraph> ProcessMethodCall;

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
					ptg = ProcessMethodCall(method, instruction, nodeIdGenerator, ptg);
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
					ptg.PointsTo(ptg.ResultVariable, target);
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
					var nodeTargets = ptg.GetTargets(node);
					var hasField = nodeTargets.ContainsKey(access.Field);

					// TODO: Don't create an unknown node when doing the inter PT analysis
					if (!hasField)
					{
						var target = GetOrCreateNode(offset, dst.Type, PTGNodeKind.Unknown);

						ptg.PointsTo(node, access.Field, target);
					}

					var targets = nodeTargets[access.Field];

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

				var ok = nodeAtOffset.TryGetValue(offset, out node);

				if (!ok)
				{
					// Create a new node
					var nodeId = nodeIdGenerator.Next;
					node = new PTGNode(nodeId, type, method, kind, offset);

					ptg.Add(node);
					nodeAtOffset.Add(offset, node);
				}
				else if (!ptg.Contains(node))
				{
					ptg.Add(node);
				}

				return node;
			}

			#endregion
		}

		#endregion

		private PointsToGraph initialGraph;
		private UniqueIDGenerator nodeIdGenerator;
		private TransferFunction transferFunction;
		private IMethodReference method;

		public PointsToAnalysis(ControlFlowGraph cfg, IMethodReference method)
			: base(cfg)
		{
			this.method = method;
			this.nodeIdGenerator = new UniqueIDGenerator(1);
			this.transferFunction = new TransferFunction(method, nodeIdGenerator);
		}

		public PointsToAnalysis(ControlFlowGraph cfg, IMethodReference method, UniqueIDGenerator nodeIdGenerator)
			: base(cfg)
		{
			this.method = method;
			this.nodeIdGenerator = nodeIdGenerator;
			this.transferFunction = new TransferFunction(method, nodeIdGenerator);
		}

		public Func<IMethodReference, MethodCallInstruction, UniqueIDGenerator, PointsToGraph, PointsToGraph> ProcessMethodCall
		{
			get { return transferFunction.ProcessMethodCall; }
			set { transferFunction.ProcessMethodCall = value; }
		}

		public DataFlowAnalysisResult<PointsToGraph>[] Result
		{
			get { return result; }
		}

		public override DataFlowAnalysisResult<PointsToGraph>[] Analyze()
		{
			this.initialGraph = CreateInitialGraph();
			return base.Analyze();
		}

		public DataFlowAnalysisResult<PointsToGraph>[] Analyze(PointsToGraph ptg)
		{
			this.initialGraph = CreateInitialGraph(ptg);
			return base.Analyze();
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
					var node = new PTGNode(nodeId, variable.Type, method, kind);

					ptg.Add(node);
					ptg.PointsTo(variable, node);
				}
				else
				{
					ptg.Add(variable);
				}
			}

			if (method.ReturnType.TypeKind != TypeKind.ValueType)
			{
				ptg.ResultVariable = new LocalVariable("$result") { Type = method.ReturnType };
				ptg.Add(ptg.ResultVariable);
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

			if (method.ReturnType.TypeKind != TypeKind.ValueType)
			{
				ptg.ResultVariable = new LocalVariable("$result") { Type = method.ReturnType };
				ptg.Add(ptg.ResultVariable);
			}

			return ptg;
		}
    }
}
