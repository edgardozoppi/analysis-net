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
    public enum PTGNodeKind
    {
        Null,
        Object
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
				this.Sources.Equals(node.Sources) &&
				this.Targets.Equals(node.Targets);
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
        public PTGNode Null { get; private set; }
        public MapSet<IVariable, PTGNode> Roots { get; private set; }
        public ISet<PTGNode> Nodes { get; private set; }

        public PointsToGraph()
        {
            this.Null = new PTGNode(0, PTGNodeKind.Null);
            this.Roots = new MapSet<IVariable, PTGNode>();
            this.Nodes = new HashSet<PTGNode>() { this.Null };
        }

        public IEnumerable<IVariable> Variables
        {
            get { return this.Roots.Keys; }
        }

		// TODO: quizas es mejor sacar estos metodos que tienen que ver
		// con el reticulado (Clone, Union, Instersection) a otra clase

		public PointsToGraph Clone()
		{
			var ptg = new PointsToGraph();
            ptg.Union(this);
			return ptg;
		}

		public void Union(PointsToGraph ptg)
		{
			var nodes = this.Nodes.ToDictionary(n => n.Id);
			var isomorphism = new Dictionary<int, PTGNode>();

			// add all new nodes
			foreach (var node in ptg.Nodes)
			{
				PTGNode clone = null;

				if (nodes.ContainsKey(node.Id))
				{
					clone = nodes[node.Id];
				}
				else
				{
					clone = new PTGNode(node.Id, node.Type, node.Offset, node.Kind);
					this.Nodes.Add(clone);
				}

				isomorphism.Add(node.Id, clone);
			}

            // add all variables
			foreach (var variable in ptg.Variables)
			{
				this.Roots.Add(variable);
			}

			// add all edges
            foreach (var node in ptg.Nodes)
            {
                var clone = isomorphism[node.Id];

                // add variable <---> node edges
                foreach (var variable in node.Variables)
                {
                    clone.Variables.Add(variable);
                    this.Roots.Add(variable, clone);
                }

                // add source -field-> node edges
                foreach (var entry in node.Sources)
                    foreach (var source in entry.Value)
                    {
                        var source_clone = isomorphism[source.Id];

                        clone.Sources.Add(entry.Key, source_clone);
                    }

                // add node -field-> target edges
                foreach (var entry in node.Targets)
                    foreach (var target in entry.Value)
                    {
                        var target_clone = isomorphism[target.Id];

                        clone.Targets.Add(entry.Key, target_clone);
                    }
            }
		}

		public void Declare(IVariable variable)
		{
			this.PointsTo(variable, this.Null);
		}

        public void PointsTo(IVariable variable, PTGNode target)
        {
            target.Variables.Add(variable);
            this.Roots.Add(variable, target);
            this.Nodes.Add(target);
        }

        public void PointsTo(PTGNode source, IFieldReference field, PTGNode target)
        {
            source.Targets.Add(field, target);
            target.Sources.Add(field, source);
            this.Nodes.Add(source);
            this.Nodes.Add(target);
        }

        public void RemoveTargets(IVariable variable)
        {
			var hasVariable = this.Roots.ContainsKey(variable);
			if (!hasVariable) return;

			var targets = this.Roots[variable];

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

        public override bool Equals(object obj)
        {
			if (object.ReferenceEquals(this, obj)) return true;
            var other = obj as PointsToGraph;

            return other != null &&
                this.Roots.Equals(other.Roots) &&
                this.Nodes.SetEquals(other.Nodes) &&
				this.SameEdges(other);
        }

        public override int GetHashCode()
        {
            return this.Roots.GetHashCode() ^
                this.Nodes.GetHashCode();
        }

		// We are assuming that ptg has the same nodes
		private bool SameEdges(PointsToGraph ptg)
		{
			var nodes = ptg.Nodes.ToDictionary(n => n.Id);

			foreach (var node in this.Nodes)
			{
				var other = nodes[node.Id];
				var sameEdges = node.SameEdges(other);
				if (!sameEdges) return false;
			}

			return true;
		}
    }

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
            return left.Equals(right);
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
						this.ProcessLoad(ptg, load.Result, access);
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

            return ptg;
        }

		private void CreateInitialGraph()
		{
			var ptg = new PointsToGraph();
			var variables = cfg.GetVariables();

			foreach (var variable in variables)
			{
				if (variable.Type.IsValueType) continue;
				// TODO: Maybe for parameters we should assume that they already points-to some node?

				if (variable.IsParameter && variable.Name == "this")
				{
					var node = new PTGNode(nextPTGNodeId++, variable.Type);
					ptg.PointsTo(variable, node);
				}
				else
				{
					//ptg.Declare(variable);
					ptg.Roots.Add(variable);
				}
			}

			this.initialGraph = ptg;
		}

		private void ProcessNull(PointsToGraph ptg, IVariable dst)
		{
			if (dst.Type.IsValueType) return;

			ptg.RemoveTargets(dst);
			ptg.PointsTo(dst, ptg.Null);
		}

        private void ProcessObjectAllocation(PointsToGraph ptg, uint offset, IVariable dst)
		{
			if (dst.Type.IsValueType) return;

			var nodeId = this.GetNodeId(offset);
			var node = new PTGNode(nodeId, dst.Type, offset);

            ptg.RemoveTargets(dst);
            ptg.PointsTo(dst, node);
        }

		private void ProcessArrayAllocation(PointsToGraph ptg, uint offset, IVariable dst)
        {
			if (dst.Type.IsValueType) return;

			var nodeId = this.GetNodeId(offset);
			var node = new PTGNode(nodeId, dst.Type, offset);

            ptg.RemoveTargets(dst);
            ptg.PointsTo(dst, node);
        }

        private void ProcessCopy(PointsToGraph ptg, IVariable dst, IVariable src)
        {
			if (dst.Type.IsValueType || src.Type.IsValueType) return;

            ptg.RemoveTargets(dst);
            var targets = ptg.Roots[src];

            foreach (var target in targets)
            {
                ptg.PointsTo(dst, target);
            }
        }

        private void ProcessLoad(PointsToGraph ptg, IVariable dst, InstanceFieldAccess access)
        {
			if (dst.Type.IsValueType || access.Type.IsValueType) return;

            ptg.RemoveTargets(dst);
            var nodes = ptg.Roots[access.Instance];

            foreach (var node in nodes)
            {
                var hasField = node.Targets.ContainsKey(access.Field);
                if (!hasField) continue;

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

			var nodes = ptg.Roots[access.Instance];
			var targets = ptg.Roots[src];

			foreach (var node in nodes)
				foreach (var target in targets)
				{
					ptg.PointsTo(node, access.Field, target);
				}
        }

		private int GetNodeId(uint offset)
		{
			int nodeId;

			if (nodeIdAtOffset.ContainsKey(offset))
			{
				nodeId = nodeIdAtOffset[offset];
			}
			else
			{
				nodeId = nextPTGNodeId++;
				nodeIdAtOffset.Add(offset, nodeId);
			}

			return nodeId;
		}
    }
}
