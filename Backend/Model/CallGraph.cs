// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Cci;
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

		public override int GetHashCode()
		{
			return this.Caller.GetHashCode() ^
				this.Label.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as CallSite;
			return other != null &&
				this.Caller.Equals(other.Caller) &&
				this.Label.Equals(other.Label);
		}

		public override string ToString()
		{
			return string.Format("{0} at {1}", this.Caller, this.Label);
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
		#region class MethodInfo

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

		#endregion

		private IDictionary<IMethodReference, MethodInfo> methods;

		public CallGraph()
		{
			this.methods = new Dictionary<IMethodReference, MethodInfo>(MethodReferenceDefinitionComparer.Default);
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

		public bool ContainsInvocation(IMethodReference method, string label)
		{
			var methodInfo = GetMethodInfo(method);
			return methodInfo.Invocations.ContainsKey(label);
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

		public IEnumerable<IMethodReference> ComputeTopologicalSort()
		{
			var result = ComputeTopologicalSort(this.Roots);
			return result;
		}

		// Tarjan's topological sort recursive algorithm
		public IEnumerable<IMethodReference> ComputeTopologicalSort(IEnumerable<IMethodReference> roots)
		{
			var result = new List<IMethodReference>(methods.Count);
			var visited = new HashSet<IMethodReference>(MethodReferenceDefinitionComparer.Default);

			foreach (var root in roots)
			{
				ForwardDFS(result, visited, root);
			}

			return result;
		}

		// Depth First Search algorithm
		private void ForwardDFS(IList<IMethodReference> result, ISet<IMethodReference> visited, IMethodReference method)
		{
			var alreadyVisited = visited.Contains(method);

			if (!alreadyVisited)
			{
				visited.Add(method);

				var methodInfo = methods[method];
				var callees = methodInfo.Invocations.Values.SelectMany(inv => inv.PossibleCallees);

				foreach (var callee in callees)
				{
					ForwardDFS(result, visited, callee);
				}

				result.Insert(0, method);
			}
		}

		//// Kahn's topological sort iterative algorithm
		//public IEnumerable<IMethodReference> ComputeTopologicalSort(IEnumerable<IMethodReference> roots)
		//{
		//	// worklist always contains methods with no incoming edges
		//	var worklist = new Queue<IMethodReference>();
		//	var visited = new HashSet<IMethodReference>(MethodReferenceDefinitionComparer.Default);
		//	var result = new List<IMethodReference>(methods.Count);

		//	foreach (var root in roots)
		//	{
		//		worklist.Enqueue(root);
		//	}

		//	while (worklist.Count > 0)
		//	{
		//		var method = worklist.Dequeue();
		//		visited.Add(method);
		//		result.Add(method);

		//		var methodInfo = methods[method];
		//		var callees = methodInfo.Invocations.Values.SelectMany(inv => inv.PossibleCallees);

		//		foreach (var callee in callees)
		//		{
		//			if (visited.Contains(callee)) continue;

		//			methodInfo = methods[method];
		//			var callers = methodInfo.CallSites.Select(callsite => callsite.Caller);

		//			if (callers.Except(visited).Any()) continue;

		//			// callee can never be already in the worklist
		//			worklist.Enqueue(callee);
		//		}
		//	}

		//	return result;
		//}

		#endregion
	}
}
