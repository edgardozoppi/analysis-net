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
        public ITypeReference Type { get; set; }
        public ISet<IVariable> Variables { get; private set; }
        public MapSet<IFieldReference, PTGNode> Sources { get; private set; }
        public MapSet<IFieldReference, PTGNode> Targets { get; private set; }

		public PTGNode(int id, PTGNodeKind kind = PTGNodeKind.Object)
        {
			this.Id = id;
            this.Kind = kind;
            this.Variables = new HashSet<IVariable>();
            this.Sources = new MapSet<IFieldReference, PTGNode>();
            this.Targets = new MapSet<IFieldReference, PTGNode>();
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
				this.Type.Equals(other.Type);
		}

		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
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
			var isomorphism = new Dictionary<int, PTGNode>();
			
			isomorphism.Add(this.Null.Id, ptg.Null);

			// clone all nodes
			foreach (var node in this.Nodes)
			{
				if (node.Kind == PTGNodeKind.Null) continue;

				var clone = new PTGNode(node.Id, node.Kind)
				{
					Type = node.Type
				};

				ptg.Nodes.Add(clone);
				isomorphism.Add(node.Id, clone);
			}

			// clone all edges
			foreach (var node in this.Nodes)
			{
				var clone = isomorphism[node.Id];

				// clone variable <---> node edges
				foreach (var variable in node.Variables)
				{
					clone.Variables.Add(variable);
					ptg.Roots.Add(variable, clone);
				}

				// clone source -field-> node edges
				foreach (var entry in node.Sources)
					foreach (var source in entry.Value)
					{
						var source_clone = isomorphism[source.Id];

						clone.Sources.Add(entry.Key, source_clone);
					}

				// clone node -field-> target edges
				foreach (var entry in node.Targets)
					foreach (var target in entry.Value)
					{
						var target_clone = isomorphism[target.Id];

						clone.Targets.Add(entry.Key, target_clone);
					}
			}

			return ptg;
		}

		public void Union(PointsToGraph ptg)
		{
			var nodes = this.Nodes.ToDictionary(n => n.Id);
			var isomorphism = new Dictionary<int, PTGNode>();

			isomorphism.Add(ptg.Null.Id, this.Null);

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
					clone = new PTGNode(node.Id, node.Kind)
					{
						Type = node.Type
					};

					this.Nodes.Add(clone);
				}

				isomorphism.Add(node.Id, clone);
			}

			// add all new edges
			foreach (var node in ptg.Nodes)
			{
				var clone = isomorphism[node.Id];

				// add new variable <---> node edges
				foreach (var variable in node.Variables)
				{
					clone.Variables.Add(variable);
					this.Roots.Add(variable, clone);
				}

				// add new source -field-> node edges
				foreach (var entry in node.Sources)
					foreach (var source in entry.Value)
					{
						var source_clone = isomorphism[source.Id];

						clone.Sources.Add(entry.Key, source_clone);
					}

				// add new node -field-> target edges
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
		public PointsToAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

        protected override PointsToGraph InitialValue(CFGNode node)
        {
			var ptg = new PointsToGraph();
			var variables = node.GetVariables();

			foreach (var variable in variables)
			{
				if (variable.Type.IsValueType) continue;
				// TODO: Maybe for parameters we should assume that they points-to some node?
				ptg.Declare(variable);
			}

			return ptg;
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
            throw new NotImplementedException();
        }
    }
}
