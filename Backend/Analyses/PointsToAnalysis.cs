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

namespace Backend.Analyses
{
	// May Points-To Analysis
    public class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToGraph>
    {
        private int nextPTGNodeId;
		private PointsToGraph initialGraph;
		private IDictionary<uint, int> nodeIdAtOffset;

		public PointsToAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
            this.nextPTGNodeId = 1;
			this.nodeIdAtOffset = new Dictionary<uint, int>();
			this.CreateInitialGraph();
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
            var ptg = input.Clone();

            foreach (var instruction in node.Instructions)
            {
				this.Flow(ptg, instruction);
            }

            return ptg;
        }

		private void Flow(PointsToGraph ptg, IInstruction instruction)
		{
			var offset = instruction.Offset;

			if (instruction is CreateObjectInstruction)
			{
				var allocation = instruction as CreateObjectInstruction;
				this.ProcessObjectAllocation(ptg, offset, allocation.Result);
			}
			else if (instruction is CreateArrayInstruction)
			{
				var allocation = instruction as CreateArrayInstruction;
				this.ProcessArrayAllocation(ptg, offset, allocation.Result);
			}
			else if (instruction is LoadInstruction)
			{
				var load = instruction as LoadInstruction;

				if (load.Operand is Constant)
				{
					var constant = load.Operand as Constant;

					if (constant.Value == null)
					{
						this.ProcessNull(ptg, load.Result);
					}
				}
				if (load.Operand is IVariable)
				{
					var variable = load.Operand as IVariable;
					this.ProcessCopy(ptg, load.Result, variable);
				}
				else if (load.Operand is InstanceFieldAccess)
				{
					var access = load.Operand as InstanceFieldAccess;
					this.ProcessLoad(ptg, offset, load.Result, access);
				}
			}
			else if (instruction is StoreInstruction)
			{
				var store = instruction as StoreInstruction;

				if (store.Result is InstanceFieldAccess)
				{
					var access = store.Result as InstanceFieldAccess;
					this.ProcessStore(ptg, access, store.Operand);
				}
			}
		}

		private void CreateInitialGraph()
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
					var node = new PTGNode(nextPTGNodeId++, variable.Type, 0, kind);

					ptg.Add(node);
					ptg.PointsTo(variable, node);
				}
				else
				{
					ptg.Add(variable);
				}
			}

			this.initialGraph = ptg;
		}

		private void ProcessNull(PointsToGraph ptg, IVariable dst)
		{
			if (dst.Type.TypeKind == TypeKind.ValueType) return;

			ptg.RemoveEdges(dst);
			ptg.PointsTo(dst, ptg.Null);
		}

        private void ProcessObjectAllocation(PointsToGraph ptg, uint offset, IVariable dst)
		{
			if (dst.Type.TypeKind == TypeKind.ValueType) return;

			var node = this.GetNode(ptg, offset, dst.Type);

            ptg.RemoveEdges(dst);
            ptg.PointsTo(dst, node);
        }

		private void ProcessArrayAllocation(PointsToGraph ptg, uint offset, IVariable dst)
        {
			if (dst.Type.TypeKind == TypeKind.ValueType) return;

			var node = this.GetNode(ptg, offset, dst.Type);

            ptg.RemoveEdges(dst);
            ptg.PointsTo(dst, node);
        }

        private void ProcessCopy(PointsToGraph ptg, IVariable dst, IVariable src)
        {
			if (dst.Type.TypeKind == TypeKind.ValueType || src.Type.TypeKind == TypeKind.ValueType) return;

            ptg.RemoveEdges(dst);
            var targets = ptg.GetTargets(src);

            foreach (var target in targets)
            {
                ptg.PointsTo(dst, target);
            }
        }

		private void ProcessLoad(PointsToGraph ptg, uint offset, IVariable dst, InstanceFieldAccess access)
        {
			if (dst.Type.TypeKind == TypeKind.ValueType || access.Type.TypeKind == TypeKind.ValueType) return;

            ptg.RemoveEdges(dst);
			var nodes = ptg.GetTargets(access.Instance);

            foreach (var node in nodes)
            {
                var hasField = node.Targets.ContainsKey(access.Field);

                if (!hasField)
				{
					var target = this.GetNode(ptg, offset, dst.Type, PTGNodeKind.Unknown);

					ptg.PointsTo(node, access.Field, target);
				}

                var targets = node.Targets[access.Field];

                foreach (var target in targets)
                {
                    ptg.PointsTo(dst, target);
                }
            }
        }

        private void ProcessStore(PointsToGraph ptg, InstanceFieldAccess access, IVariable src)
        {
			if (access.Type.TypeKind == TypeKind.ValueType || src.Type.TypeKind == TypeKind.ValueType) return;

			var nodes = ptg.GetTargets(access.Instance);
			var targets = ptg.GetTargets(src);

			foreach (var node in nodes)
				foreach (var target in targets)
				{
					ptg.PointsTo(node, access.Field, target);
				}
        }

		private PTGNode GetNode(PointsToGraph ptg, uint offset, IType type, PTGNodeKind kind = PTGNodeKind.Object)
		{
			PTGNode node;

			if (nodeIdAtOffset.ContainsKey(offset))
			{
				var nodeId = nodeIdAtOffset[offset];
				node = ptg.GetNode(nodeId);
			}
			else
			{
				var nodeId = nextPTGNodeId++;
				node = new PTGNode(nodeId, type, offset, kind);

				ptg.Add(node);
				nodeIdAtOffset.Add(offset, nodeId);
			}

			return node;
		}
    }
}
