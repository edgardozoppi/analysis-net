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
        public PTGNodeKind Kind { get; private set; }
        public ITypeReference Type { get; set; }
        public ISet<IVariable> Variables { get; private set; }
        public MapSet<IFieldReference, PTGNode> Sources { get; private set; }
        public MapSet<IFieldReference, PTGNode> Targets { get; private set; }

        public PTGNode(PTGNodeKind kind = PTGNodeKind.Object)
        {
            this.Kind = kind;
            this.Variables = new HashSet<IVariable>();
            this.Sources = new MapSet<IFieldReference, PTGNode>();
            this.Targets = new MapSet<IFieldReference, PTGNode>();
        }

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as PTGNode;

			return other != null &&
				this.Kind.Equals(other.Kind) &&
				this.Type.Equals(other.Type) &&
				this.Variables.SetEquals(other.Variables) &&
				this.Sources.Equals(other.Sources) &&
				this.Targets.Equals(other.Targets);
		}

		public override int GetHashCode()
		{
			return this.Kind.GetHashCode() ^
				this.Type.GetHashCode() ^
				this.Variables.GetHashCode() ^
				this.Sources.GetHashCode() ^
				this.Targets.GetHashCode();
		}
    }

    public class PointsToGraph
    {
        public PTGNode Null { get; private set; }
        public MapSet<IVariable, PTGNode> Roots { get; private set; }
        public ISet<PTGNode> Nodes { get; private set; }

        public PointsToGraph()
        {
            this.Null = new PTGNode(PTGNodeKind.Null);
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
			var isomorphism = new Dictionary<PTGNode, PTGNode>(ReferenceEqualityComparer.Instance);
			
			isomorphism.Add(this.Null, ptg.Null);

			// clone all nodes
			foreach (var node in this.Nodes)
			{
				if (node.Kind == PTGNodeKind.Null) continue;

				var clone = new PTGNode(node.Kind)
				{
					Type = node.Type
				};

				ptg.Nodes.Add(clone);
				isomorphism.Add(node, clone);
			}

			// clone variable <---> node edges
			foreach (var entry in this.Roots)
				foreach (var node in entry.Value)
				{
					var clone = isomorphism[node];

					clone.Variables.Add(entry.Key);
					ptg.Roots.Add(entry.Key, clone);
				}

			// clone node <-field-> node edges
			foreach (var node in this.Nodes)
			{
				var clone = isomorphism[node];

				// clone source -field-> node edges
				foreach (var entry in node.Sources)
					foreach (var source in entry.Value)
					{
						var source_clone = isomorphism[source];

						clone.Sources.Add(entry.Key, source_clone);
					}

				// clone node -field-> target edges
				foreach (var entry in node.Targets)
					foreach (var target in entry.Value)
					{
						var target_clone = isomorphism[target];

						clone.Targets.Add(entry.Key, target_clone);
					}
			}

			return ptg;
		}

		public void Union(PointsToGraph ptg)
		{
			throw new NotImplementedException();
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

		// TODO: cuidado con el Equals de los nodos que es recursivo y no termina cuando hay ciclos en el grafo!!
		// Hay que cambiar el codigo para que los nodos tenga un Id unico y se consideren iguales sii tienen el mismo Id
		// Esto facilita la comparacion, clonado y union de los grafos y es mas performante.

        public override bool Equals(object obj)
        {
			if (object.ReferenceEquals(this, obj)) return true;
            var other = obj as PointsToGraph;

            return other != null &&
                this.Null.Equals(other.Null) &&
                this.Roots.Equals(other.Roots) &&
                this.Nodes.SetEquals(other.Nodes);
        }

        public override int GetHashCode()
        {
            return this.Null.GetHashCode() ^
                this.Roots.GetHashCode() ^
                this.Nodes.GetHashCode();
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
