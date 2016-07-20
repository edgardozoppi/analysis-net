// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Utils;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	// Unknown PTG nodes represent placeholders
	// (external objects that can be null or
	// stand for multiple objects).
	// Useful to model parameter values.
    public enum PTGNodeKind
    {
        Null,
        Object,
		Unknown
    }

    public class PTGNode
    {
		public int Id { get; private set; }
		public PTGNodeKind Kind { get; private set; }
		public uint Offset { get; set; }
        public ITypeReference Type { get; set; }
        public ISet<IVariable> Variables { get; private set; }
        public MapSet<IFieldReference, PTGNode> Sources { get; private set; }
        public MapSet<IFieldReference, PTGNode> Targets { get; private set; }

		public PTGNode(int id, PTGNodeKind kind = PTGNodeKind.Null)
        {
			this.Id = id;
            this.Kind = kind;
            this.Variables = new HashSet<IVariable>();
            this.Sources = new MapSet<IFieldReference, PTGNode>();
            this.Targets = new MapSet<IFieldReference, PTGNode>();
        }

		public PTGNode(int id, ITypeReference type, uint offset = 0, PTGNodeKind kind = PTGNodeKind.Object)
			: this(id, kind)
		{
			this.Offset = offset;
			this.Type = type;
		}

		public bool SameEdges(PTGNode node)
		{
			if (node == null) throw new ArgumentNullException("node");

			return this.Variables.SetEquals(node.Variables) &&
				this.Sources.MapEquals(node.Sources) &&
				this.Targets.MapEquals(node.Targets);
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as PTGNode;

			return other != null &&
				this.Id == other.Id &&
				this.Kind == other.Kind &&
				this.Offset == other.Offset &&
				object.Equals(this.Type, other.Type);
		}

		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

		public override string ToString()
		{
			string result;

			switch (this.Kind)
			{
				case PTGNodeKind.Null:
					result = "null";
					break;

				default:
					var type = TypeHelper.GetTypeName(this.Type);
					result = string.Format("{0:X4}: {1}", this.Offset, type);
					break;
			}

			return result;
		}
    }

    public class PointsToGraph
    {
		private MapSet<IVariable, PTGNode> variables;
		private IDictionary<int, PTGNode> nodes;

        public PTGNode Null { get; private set; }

        public PointsToGraph()
        {
            this.Null = new PTGNode(0, PTGNodeKind.Null);
            this.variables = new MapSet<IVariable, PTGNode>();            
			this.nodes = new Dictionary<int, PTGNode>();

			this.Add(this.Null);
        }

        public IEnumerable<IVariable> Variables
        {
            get { return this.variables.Keys; }
        }

		public IEnumerable<PTGNode> Nodes
		{
			get { return nodes.Values; }
		}

		public PointsToGraph Clone()
		{
			var ptg = new PointsToGraph();
            ptg.Union(this);
			return ptg;
		}

		public void Union(PointsToGraph ptg)
		{
			// add all new nodes
			foreach (var node in ptg.Nodes)
			{
				if (this.Contains(node)) continue;
				var clone = new PTGNode(node.Id, node.Type, node.Offset, node.Kind);

				nodes.Add(clone.Id, clone);
			}

            // add all variables
			foreach (var variable in ptg.Variables)
			{
				this.variables.Add(variable);
			}

			// add all edges
            foreach (var node in ptg.Nodes)
            {
                var clone = nodes[node.Id];

                // add variable <---> node edges
                foreach (var variable in node.Variables)
                {
                    clone.Variables.Add(variable);
                    this.variables.Add(variable, clone);
                }

                // add source -field-> node edges
                foreach (var entry in node.Sources)
                    foreach (var source in entry.Value)
                    {
                        var source_clone = nodes[source.Id];

                        clone.Sources.Add(entry.Key, source_clone);
                    }

                // add node -field-> target edges
                foreach (var entry in node.Targets)
                    foreach (var target in entry.Value)
                    {
                        var target_clone = nodes[target.Id];

                        clone.Targets.Add(entry.Key, target_clone);
                    }
            }
		}

		public bool Contains(IVariable variable)
		{
			return this.variables.ContainsKey(variable);
		}

		public bool Contains(PTGNode node)
		{
			return this.ContainsNode(node.Id);
		}

		public bool ContainsNode(int id)
		{
			return nodes.ContainsKey(id);
		}

		public void Add(IVariable variable)
		{
			variables.Add(variable);
		}

		public void Add(PTGNode node)
		{
			nodes.Add(node.Id, node);
		}

		public PTGNode GetNode(int id)
		{
			return nodes[id];
		}

		public ISet<PTGNode> GetTargets(IVariable variable)
		{
			return variables[variable];
		}

		public void Remove(IVariable variable)
		{
			this.RemoveEdges(variable);
			variables.Remove(variable);
		}

        public void PointsTo(IVariable variable, PTGNode target)
        {
#if DEBUG
			if (!this.Contains(target))
				throw new ArgumentException("Target node does not belong to this Points-to graph.", "target");
#endif

            target.Variables.Add(variable);
            this.variables.Add(variable, target);
        }

        public void PointsTo(PTGNode source, IFieldReference field, PTGNode target)
        {
#if DEBUG
			if (!this.Contains(source))
				throw new ArgumentException("Source node does not belong to this Points-to graph.", "source");

			if (!this.Contains(target))
				throw new ArgumentException("Target node does not belong to this Points-to graph.", "target");
#endif

            source.Targets.Add(field, target);
            target.Sources.Add(field, source);
        }

        public void RemoveEdges(IVariable variable)
        {
			var hasVariable = this.Contains(variable);
			if (!hasVariable) return;

			var targets = this.variables[variable];

			foreach (var target in targets)
			{
				target.Variables.Remove(variable);
			}

			// If we uncomment the next line
			// the variable will be removed from
			// the graph, not only its edges
			//this.Roots.Remove(variable);

			// Remove only the edges of the variable,
			// but not the variable itself
			targets.Clear();
        }

        public bool GraphEquals(object obj)
        {
			if (object.ReferenceEquals(this, obj)) return true;
            var other = obj as PointsToGraph;

			Func<PTGNode, PTGNode, bool> nodeEquals = (a, b) => a.Equals(b) && a.SameEdges(b);

			return other != null &&
				this.variables.MapEquals(other.variables) &&
				this.nodes.DictionaryEquals(other.nodes, nodeEquals);
        }
    }

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

		private void Flow(PointsToGraph ptg, Instruction instruction)
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
				if (variable.Type.IsValueType) continue;

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
			if (dst.Type.IsValueType) return;

			ptg.RemoveEdges(dst);
			ptg.PointsTo(dst, ptg.Null);
		}

        private void ProcessObjectAllocation(PointsToGraph ptg, uint offset, IVariable dst)
		{
			if (dst.Type.IsValueType) return;

			var node = this.GetNode(ptg, offset, dst.Type);

            ptg.RemoveEdges(dst);
            ptg.PointsTo(dst, node);
        }

		private void ProcessArrayAllocation(PointsToGraph ptg, uint offset, IVariable dst)
        {
			if (dst.Type.IsValueType) return;

			var node = this.GetNode(ptg, offset, dst.Type);

            ptg.RemoveEdges(dst);
            ptg.PointsTo(dst, node);
        }

        private void ProcessCopy(PointsToGraph ptg, IVariable dst, IVariable src)
        {
			if (dst.Type.IsValueType || src.Type.IsValueType) return;

            ptg.RemoveEdges(dst);
            var targets = ptg.GetTargets(src);

            foreach (var target in targets)
            {
                ptg.PointsTo(dst, target);
            }
        }

		private void ProcessLoad(PointsToGraph ptg, uint offset, IVariable dst, InstanceFieldAccess access)
        {
			if (dst.Type.IsValueType || access.Type.IsValueType) return;

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
			if (access.Type.IsValueType || src.Type.IsValueType) return;

			var nodes = ptg.GetTargets(access.Instance);
			var targets = ptg.GetTargets(src);

			foreach (var node in nodes)
				foreach (var target in targets)
				{
					ptg.PointsTo(node, access.Field, target);
				}
        }

		private PTGNode GetNode(PointsToGraph ptg, uint offset, ITypeReference type, PTGNodeKind kind = PTGNodeKind.Object)
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
