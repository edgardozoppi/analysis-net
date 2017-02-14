// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using Backend.Utils;
using Model;
using Model.ThreeAddressCode.Values;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class InputOutputInfo
	{
		public ISet<IVariable> Inputs { get; private set; }
		public ISet<IVariable> Outputs { get; private set; }
		public ISet<IVariable> Results { get; private set; }

		public InputOutputInfo()
		{
			this.Inputs = new HashSet<IVariable>();
			this.Outputs = new HashSet<IVariable>();
			this.Results = new HashSet<IVariable>();
		}

		public InputOutputInfo(IEnumerable<IVariable> inputs, IEnumerable<IVariable> outputs, IEnumerable<IVariable> results)
			: this()
		{
			this.Inputs.UnionWith(inputs);
			this.Outputs.UnionWith(outputs);
			this.Results.UnionWith(results);
		}
	}

	public class EscapeInfo
	{
		//public IMethodReference Method { get; private set; }
		public MapSet<IVariable, PTGNode> EscapeChannels { get; private set; }
		// Reachable nodes already existing from before.
		public ISet<PTGNode> ExternalNodes { get; private set; }
		// Internal nodes that do not escape.
		public ISet<PTGNode> CapturedNodes { get; private set; }

		//public EscapeInfo(IMethodReference method)
		public EscapeInfo()
		{
			//this.Method = method;
			this.EscapeChannels = new MapSet<IVariable, PTGNode>();
			this.ExternalNodes = new HashSet<PTGNode>();
			this.CapturedNodes = new HashSet<PTGNode>();
		}

		// Internal nodes that escape.
		public IEnumerable<PTGNode> EscapingNodes
		{
			get { return this.EscapeChannels.SelectMany(entry => entry.Value).Distinct(); }
		}

		// Newly created nodes (including callees).
		public IEnumerable<PTGNode> InternalNodes
		{
			get { return this.CapturedNodes.Union(this.EscapingNodes); }
		}
	}

	public class EscapeAnalysis
	{
		public const string INFO_ESC = "ESC";

		private ProgramAnalysisInfo programInfo;
		private CallGraph callGraph;
		private IDictionary<IMethodReference, EscapeInfo> result;
		private ISet<IMethodReference> worklist;

		public EscapeAnalysis(ProgramAnalysisInfo programInfo, CallGraph callGraph)
		{
			this.programInfo = programInfo;
			this.callGraph = callGraph;
		}

		public IDictionary<IMethodReference, EscapeInfo> Analyze()
		{
			result = new Dictionary<IMethodReference, EscapeInfo>(MethodReferenceDefinitionComparer.Default);
			worklist = new HashSet<IMethodReference>(callGraph.Methods, MethodReferenceDefinitionComparer.Default);

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
			// Avoid analyzing the same method several times in case of recursion.
			if (result.ContainsKey(method)) return;

			var calleesEscapingNodes = new HashSet<PTGNode>();
			var invocations = callGraph.GetInvocations(method);
			var callees = invocations.SelectMany(inv => inv.PossibleCallees);
			//var escapeInfo = new EscapeInfo(method);
			var escapeInfo = new EscapeInfo();

			result.Add(method, escapeInfo);
			worklist.Remove(method);

			var methodInfo = programInfo.GetOrAdd(method);
			methodInfo.Add(INFO_ESC, escapeInfo);

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
				//FillEscapeChannels(escapeInfo, ptg, calleesEscapingNodes);
				FillEscapeChannels(method, escapeInfo, ptg, calleesEscapingNodes);
				FillExternalAndCapturedNodes(method, escapeInfo, ptg, calleesEscapingNodes);
			}
		}

		private PointsToGraph GetPTGAtExit(IMethodReference method)
		{
			InterPointsToInfo pti;
			PointsToGraph ptg = null;
			var methodInfo = programInfo[method];
			var ok = methodInfo.TryGet(InterPointsToAnalysis.INFO_IPTA_RESULT, out pti);

			if (ok)
			{
				ptg = pti.Output;
				//ptg = pti.IntraPointsToInfo[ControlFlowGraph.ExitNodeId].Output;
			}

			return ptg;
		}

		private static void FillEscapeChannels(IMethodReference method, EscapeInfo escapeInfo, PointsToGraph ptg, ISet<PTGNode> calleesEscapingNodes)
		{
			var methodComparer = MethodReferenceDefinitionComparer.Default;
			var channels = escapeInfo.EscapeChannels;
			//var method = escapingInfo.Method;

			// TODO: Replace v.Name.StartsWith("#") by a better way to recognize global variables!
			var variables = ptg.Variables
							   .Where(v => v.IsParameter || v.Name.StartsWith("#"))
							   .ToSet();

			if (method.ReturnType.TypeKind != TypeKind.ValueType)
			{
				variables.Add(ptg.ResultVariable);
			}

			foreach (var variable in variables)
			{
				var nodes = ptg.GetReachableNodes(variable)
							   .Where(n => n.Kind != PTGNodeKind.Null && n.Kind != PTGNodeKind.Global &&
							   (calleesEscapingNodes.Contains(n) || methodComparer.Equals(n.Method, method)));

				channels.AddRange(variable, nodes);
			}
		}

		private void FillExternalAndCapturedNodes(IMethodReference method, EscapeInfo escapeInfo, PointsToGraph ptg, ISet<PTGNode> calleesEscapingNodes)
		{
			var methodComparer = MethodReferenceDefinitionComparer.Default;
			var escapingNodes = escapeInfo.EscapingNodes;
			var capturedNodes = escapeInfo.CapturedNodes;
			var externalNodes = escapeInfo.ExternalNodes;
			var reachableNodes = ptg.GetReachableNodes()
									.Where(n => n.Kind != PTGNodeKind.Null);

			var newlyCreatedNodes = reachableNodes
									.Where(n => n.Kind != PTGNodeKind.Global &&
									methodComparer.Equals(n.Method, method));

			capturedNodes.UnionWith(calleesEscapingNodes);
			capturedNodes.UnionWith(newlyCreatedNodes);
			capturedNodes.ExceptWith(escapingNodes);

			// External nodes include reachable global\static nodes.
			externalNodes.UnionWith(reachableNodes);
			externalNodes.ExceptWith(capturedNodes);
		}
	}
}
