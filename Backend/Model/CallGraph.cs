using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Model
{
	public class CallSite
	{
		public MethodDefinition Caller { get; private set; }
		public string Label { get; private set; }

		public CallSite(MethodDefinition caller, string label)
		{
			this.Caller = caller;
			this.Label = label;
		}
	}

	public class InvocationInfo
	{
		public IMethodReference StaticCallee { get; private set; }
		public ISet<MethodDefinition> PossibleCallees { get; private set; }
		public string Label { get; private set; }

		public InvocationInfo(string label, IMethodReference staticCallee)
		{
			this.StaticCallee = staticCallee;
			this.Label = label;
			this.PossibleCallees = new HashSet<MethodDefinition>();
		}
	}

	public class CallGraph
	{
		private class MethodInfo
		{
			public MethodDefinition Method { get; private set; }
			public ISet<CallSite> CallSites { get; private set; }
			public IDictionary<string, InvocationInfo> Invocations { get; private set; }
			public bool IsRoot { get; private set; }

			public MethodInfo(MethodDefinition method, bool isRoot = false)
			{
				this.Method = method;
				this.IsRoot = isRoot;
				this.CallSites = new HashSet<CallSite>();
				this.Invocations = new Dictionary<string, InvocationInfo>();
			}
		}

		private IDictionary<MethodDefinition, MethodInfo> methods;

		public CallGraph()
		{
			this.methods = new Dictionary<MethodDefinition, MethodInfo>();
		}

		public IEnumerable<MethodDefinition> Methods
		{
			get { return methods.Keys; }
		}

		public IEnumerable<MethodDefinition> Roots
		{
			get
			{
				var result = from m in methods.Values
							 where m.IsRoot
							 select m.Method;
				return result;
			}
		}

		public IEnumerable<CallSite> GetCallSites(MethodDefinition method)
		{
			var methodInfo = GetMethodInfo(method);
			return methodInfo.CallSites;
		}

		public IEnumerable<InvocationInfo> GetInvocations(MethodDefinition method)
		{
			var methodInfo = GetMethodInfo(method);
			return methodInfo.Invocations.Values;
		}

		public void Add(MethodDefinition root)
		{
			var info = new MethodInfo(root, true);
			methods.Add(root, info);
		}

		public void Add(MethodDefinition caller, string label, IMethodReference staticCallee)
		{
			var callerInfo = GetMethodInfo(caller);
			var invocationInfo = new InvocationInfo(label, staticCallee);

			callerInfo.Invocations.Add(label, invocationInfo);
		}

		public void Add(MethodDefinition caller, string label, IEnumerable<MethodDefinition> callees)
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

		private MethodInfo GetMethodInfo(MethodDefinition method)
		{
			MethodInfo info;

			if (!methods.TryGetValue(method, out info))
			{
				info = new MethodInfo(method);
				methods.Add(method, info);
			}

			return info;
		}
	}
}
