// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using Backend.Utils;
using Model.ThreeAddressCode.Values;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class EscapeInfo
	{
		public IMethodReference Method { get; private set; }
		public MapSet<IVariable, PTGNode> Channels { get; private set; }

		public EscapeInfo(IMethodReference method)
		{
			this.Method = method;
			this.Channels = new MapSet<IVariable, PTGNode>();
		}

		public IEnumerable<PTGNode> EscapingNodes
		{
			get { return this.Channels.SelectMany(entry => entry.Value).Distinct(); }
		}
	}

	public class EscapeAnalysis
	{
		private ProgramAnalysisInfo methodsInfo;
		private CallGraph callGraph;
		private Dictionary<IMethodReference, EscapeInfo> result;
		private ISet<IMethodReference> worklist;
		private MethodReferenceDefinitionComparer methodComparer;

		public EscapeAnalysis(ProgramAnalysisInfo methodsInfo, CallGraph callGraph)
		{
			this.methodsInfo = methodsInfo;
			this.callGraph = callGraph;
			this.methodComparer = new MethodReferenceDefinitionComparer();
		}

		public IDictionary<IMethodReference, EscapeInfo> Analyze()
		{
			result = new Dictionary<IMethodReference, EscapeInfo>();
			worklist = new HashSet<IMethodReference>(callGraph.Methods);

			while (worklist.Count > 0)
			{
				var method = worklist.First();
				worklist.Remove(method);

				Analyze(method);
			}
			
			return result;
		}

		private void Analyze(IMethodReference method)
		{
			var calleesEscapingNodes = new HashSet<PTGNode>();
			var invocations = callGraph.GetInvocations(method);
			var callees = invocations.SelectMany(inv => inv.PossibleCallees);
			var escapeInfo = new EscapeInfo(method);

			result.Add(method, escapeInfo);
			worklist.Remove(method);

			foreach (var callee in callees)
			{
				EscapeInfo calleeEscapeInfo;
				var ok = result.TryGetValue(callee, out calleeEscapeInfo);

				if (!ok)
				{
					Analyze(callee);
					calleeEscapeInfo = result[callee];
				}

				calleesEscapingNodes.UnionWith(calleeEscapeInfo.EscapingNodes);
			}

			var ptg = GetPTGAtExit(method);

			if (ptg != null)
			{
				FillEscapingChannels(escapeInfo, ptg, calleesEscapingNodes);
			}
		}

		private PointsToGraph GetPTGAtExit(IMethodReference method)
		{
			PointsToGraph ptg = null;
			MethodAnalysisInfo methodInfo;
			var ok = methodsInfo.TryGet(method, out methodInfo);

			if (ok)
			{
				var result = methodInfo.Get<DataFlowAnalysisResult<PointsToGraph>[]>(InterPointsToAnalysis.PTG_INFO);
				ptg = result[ControlFlowGraph.ExitNodeId].Output;
			}

			return ptg;
		}

		private void FillEscapingChannels(EscapeInfo escapingInfo, PointsToGraph ptg, ISet<PTGNode> calleesEscapingNodes)
		{
			var method = escapingInfo.Method;
			var channels = escapingInfo.Channels;
			var parameters = ptg.Variables.Where(v => v.IsParameter);

			foreach (var parameter in parameters)
			{
				var nodes = ptg.GetReachableNodes(parameter)
							   .Where(n => calleesEscapingNodes.Contains(n) || methodComparer.Equals(n.Method, method));

				channels.AddRange(parameter, nodes);
			}

			if (method.ReturnType.TypeKind != TypeKind.ValueType)
			{
				var nodes = ptg.GetReachableNodes(ptg.ResultVariable)
							   .Where(n => calleesEscapingNodes.Contains(n) || methodComparer.Equals(n.Method, method));

				channels.AddRange(ptg.ResultVariable, nodes);
			}
		}
	}
}
