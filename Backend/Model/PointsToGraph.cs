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

namespace Backend.Model
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
        public IType Type { get; set; }
        public ISet<IVariable> Variables { get; private set; }
		// TODO: Maybe is better to use node ids instead of the nodes itself
		// to avoid cloning them when cloning the PT graph
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

		public PTGNode(int id, IType type, PTGNodeKind kind = PTGNodeKind.Object, uint offset = 0)
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
					result = string.Format("{0:X4}: {1}", this.Offset, this.Type);
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
            get { return variables.Keys; }
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
				var clone = new PTGNode(node.Id, node.Type, node.Kind, node.Offset);

				nodes.Add(clone.Id, clone);
			}

            // add all variables
			foreach (var variable in ptg.Variables)
			{
				variables.Add(variable);
			}

			// add all edges
            foreach (var node in ptg.Nodes)
            {
                var clone = nodes[node.Id];

                // add variable <---> node edges
                foreach (var variable in node.Variables)
                {
                    clone.Variables.Add(variable);
                    variables.Add(variable, clone);
                }

				// add source -field-> node edges
				foreach (var entry in node.Sources)
				{
					foreach (var source in entry.Value)
					{
						var source_clone = nodes[source.Id];

						clone.Sources.Add(entry.Key, source_clone);
					}
				}

				// add node -field-> target edges
				foreach (var entry in node.Targets)
				{
					foreach (var target in entry.Value)
					{
						var target_clone = nodes[target.Id];

						clone.Targets.Add(entry.Key, target_clone);
					}
				}
            }
		}

		public bool Contains(IVariable variable)
		{
			return variables.ContainsKey(variable);
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

		public ISet<PTGNode> GetTargets(IVariable variable, IFieldReference field)
		{
			var result = new HashSet<PTGNode>();
			var targets = this.GetTargets(variable);

			foreach (var node in targets)
			{
				if (node.Targets.ContainsKey(field))
				{
					var fieldTargets = node.Targets[field];
					result.AddRange(fieldTargets);
				}
			}

			return result;
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
            variables.Add(variable, target);
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

			var targets = variables[variable];

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
				variables.MapEquals(other.variables) &&
				nodes.DictionaryEquals(other.nodes, nodeEquals);
        }

		// binding: parameter -> argument
		public MapSet<IVariable, int> NewFrame(IDictionary<IVariable, IVariable> binding)
		{
			// Remove node -> variable edges
			foreach (var entry in variables)
			{
				foreach (var node in entry.Value)
				{
					node.Variables.Remove(entry.Key);
				}
			}

			var callerFrame = variables;
			// Clear variable -> node edges
			variables = new MapSet<IVariable, PTGNode>();

			foreach (var entry in binding)
			{
				if (entry.Key.Type.TypeKind == TypeKind.ValueType ||
					entry.Value.Type.TypeKind == TypeKind.ValueType)
					continue;

				// Get argument targets
				var targets = callerFrame[entry.Value];

				// Add parameter -> node edges
				variables.AddRange(entry.Key, targets);

				// Add node -> parameter edges
				foreach (var node in targets)
				{
					node.Variables.Add(entry.Key);
				}
			}

			// This is needed so it works with clonned PT graphs (different nodes but with the same id)
			var result = callerFrame.ToDictionary(entry => entry.Key, entry => entry.Value.Select(n => n.Id)).ToMapSet();
			return result;
		}

		// binding: parameter -> argument
		public void RestoreFrame(MapSet<IVariable, int> frame, IDictionary<IVariable, IVariable> binding)
		{
			// This is needed so it works with clonned PT graphs (different nodes but with the same id)
			var frame2 = frame.ToDictionary(entry => entry.Key, entry => entry.Value.Select(id => nodes[id])).ToMapSet();

			// Remove node -> variable edges
			foreach (var entry in variables)
			{
				foreach (var node in entry.Value)
				{
					node.Variables.Remove(entry.Key);
				}
			}

			var calleeFrame = variables;
			// Restore variable -> node edges
			variables = frame2;

			foreach (var entry in binding)
			{
				if (entry.Key.Type.TypeKind == TypeKind.ValueType ||
					entry.Value.Type.TypeKind == TypeKind.ValueType)
					continue;

				// Get parameter targets
				var targets = calleeFrame[entry.Key];

				// Set argument -> node edges
				variables[entry.Value] = targets;
			}

			// Add node -> variable edges
			foreach (var entry in variables)
			{
				foreach (var node in entry.Value)
				{
					node.Variables.Add(entry.Key);
				}
			}
		}
	}
}
