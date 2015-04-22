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
			var clone = new PointsToGraph();

			// Note: hay que ver como clonar los nodos una sola vez
			// y reutilizarlos al agregar los ejes

			return clone;
		}

		public void Union(PointsToGraph other)
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
            throw new NotImplementedException();
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
