using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Model
{
	public class CallSite
	{
		public IMethodReference Caller { get; private set; }
		public string Label { get; private set; }

		public CallSite(IMethodReference caller, string label)
		{
			this.Caller = caller;
			this.Label = label;
		}
	}

	public class InvocationInfo
	{
		public IMethodReference StaticCallee { get; private set; }
		public ISet<IMethodReference> PossibleCallees { get; private set; }
		public string Label { get; private set; }

		public InvocationInfo(string label, IMethodReference staticCallee)
		{
			this.Label = label;
			this.StaticCallee = staticCallee;
			this.PossibleCallees = new HashSet<IMethodReference>();
		}
	}

	public class CallGraph
	{
		private class MethodInfo
		{
			public IMethodReference Method { get; private set; }
			public ISet<CallSite> CallSites { get; private set; }
			public IDictionary<string, InvocationInfo> Invocations { get; private set; }
			public bool IsRoot { get; private set; }

			public MethodInfo(IMethodReference method, bool isRoot = false)
			{
				this.Method = method;
				this.IsRoot = isRoot;
				this.CallSites = new HashSet<CallSite>();
				this.Invocations = new Dictionary<string, InvocationInfo>();
			}
		}

		private IDictionary<IMethodReference, MethodInfo> methods;

		public CallGraph()
		{
			this.methods = new Dictionary<IMethodReference, MethodInfo>(new MethodReferenceDefinitionComparer());
		}

		public IEnumerable<IMethodReference> Methods
		{
			get { return methods.Keys; }
		}

		public IEnumerable<IMethodReference> Roots
		{
			get
			{
				var result = from m in methods.Values
							 where m.IsRoot
							 select m.Method;
				return result;
			}
		}

		public IEnumerable<CallSite> GetCallSites(IMethodReference method)
		{
			var methodInfo = GetMethodInfo(method);
			return methodInfo.CallSites;
		}

		public IEnumerable<InvocationInfo> GetInvocations(IMethodReference method)
		{
			var methodInfo = GetMethodInfo(method);
			return methodInfo.Invocations.Values;
		}

		public InvocationInfo GetInvocation(IMethodReference method, string label)
		{
			var methodInfo = GetMethodInfo(method);
			return methodInfo.Invocations[label];
		}

		public void Add(IMethodReference root)
		{
			var info = new MethodInfo(root, true);
			methods.Add(root, info);
		}

		public void Add(IMethodReference caller, string label, IMethodReference staticCallee)
		{
			var callerInfo = GetMethodInfo(caller);
			var invocationInfo = new InvocationInfo(label, staticCallee);

			callerInfo.Invocations.Add(label, invocationInfo);
		}

		public void Add(IMethodReference caller, string label, IEnumerable<IMethodReference> callees)
		{
			var callerInfo = GetMethodInfo(caller);
			var invocationInfo = callerInfo.Invocations[label];

			invocationInfo.PossibleCallees.UnionWith(callees);

			foreach (var callee in callees)
			{
				var calleeInfo = GetMethodInfo(callee);
				var callSite = new CallSite(caller, label);

				calleeInfo.CallSites.Add(callSite);
			}
		}

		private MethodInfo GetMethodInfo(IMethodReference method)
		{
			MethodInfo info;

			if (!methods.TryGetValue(method, out info))
			{
				info = new MethodInfo(method);
				methods.Add(method, info);
			}

			return info;
		}

		#region Topological Sort

		//private CFGNode[] ComputeTopologicalSort()
		//{
		//	var result = new CFGNode[this.Nodes.Count];
		//	var visited = new bool[this.Nodes.Count];
		//	var index = this.Nodes.Count - 1;

		//	foreach (var node in this.Entries)
		//	{
		//		ControlFlowGraph.DepthFirstSearch(result, visited, node, ref index);
		//	}

		//	return result;
		//}

		//private static void DepthFirstSearch(CFGNode[] result, bool[] visited, CFGNode node, ref int index)
		//{
		//	var alreadyVisited = visited[node.Id];

		//	if (!alreadyVisited)
		//	{
		//		visited[node.Id] = true;

		//		foreach (var succ in node.Successors)
		//		{
		//			ControlFlowGraph.DepthFirstSearch(result, visited, succ, ref index);
		//		}

		//		node.ForwardIndex = index;
		//		result[index] = node;
		//		index--;
		//	}
		//}

		private enum TopologicalSortNodeStatus
		{
			NeverVisited, // never pushed into stack
			FirstVisit, // pushed into stack for the first time
			SecondVisit // pushed into stack for the second time
		}

		public IEnumerable<IMethodReference> ComputeTopologicalSort()
		{
			var result = ComputeTopologicalSort(this.Roots);
			return result;
		}

		public IEnumerable<IMethodReference> ComputeTopologicalSort(IEnumerable<IMethodReference> roots)
		{
			// reverse postorder traversal from root methods
			var stack = new Stack<IMethodReference>();
			var result = new List<IMethodReference>();
			var status = new Dictionary<IMethodReference, TopologicalSortNodeStatus>();

			foreach (var methodInfo in methods.Keys)
			{
				status[methodInfo] = TopologicalSortNodeStatus.NeverVisited;
			}

			foreach (var method in roots)
			{
				stack.Push(method);
				status[method] = TopologicalSortNodeStatus.FirstVisit;
			}

			do
			{
				var method = stack.Peek();
				var node_status = status[method];

				if (node_status == TopologicalSortNodeStatus.FirstVisit)
				{
					status[method] = TopologicalSortNodeStatus.SecondVisit;

					var methodInfo = methods[method];
					var callees = methodInfo.Invocations.Values.SelectMany(inv => inv.PossibleCallees);

					foreach (var callee in callees)
					{
						var callee_status = status[callee];

						if (callee_status == TopologicalSortNodeStatus.NeverVisited)
						{
							stack.Push(callee);
							status[callee] = TopologicalSortNodeStatus.FirstVisit;
						}
					}
				}
				else if (node_status == TopologicalSortNodeStatus.SecondVisit)
				{
					stack.Pop();
					result.Insert(0, method);
				}
			}
			while (stack.Count > 0);

			return result;
		}

		#endregion
	}
}
