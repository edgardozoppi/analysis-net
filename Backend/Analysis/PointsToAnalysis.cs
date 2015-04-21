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
        protected override PointsToGraph InitialValue(CFGNode node)
        {
            throw new NotImplementedException();
        }

        protected override bool Compare(PointsToGraph left, PointsToGraph right)
        {
            throw new NotImplementedException();
        }

        protected override PointsToGraph Join(PointsToGraph left, PointsToGraph right)
        {
            throw new NotImplementedException();
        }

        protected override PointsToGraph Flow(CFGNode node, PointsToGraph input)
        {
            throw new NotImplementedException();
        }
    }
}
