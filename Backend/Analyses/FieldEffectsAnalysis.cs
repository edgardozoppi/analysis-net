// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using Backend.Utils;
using Model;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Visitor;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class FieldEffect
	{
		public PTGNode Node { get; private set; }
		public PTGNodeField Field { get; private set; }

		public FieldEffect(PTGNode node, PTGNodeField field)
		{
			this.Node = node;
			this.Field = field;
		}

		public override string ToString()
		{
			return string.Format("<{0}>.{1}", this.Node, this.Field.Name);
		}

		public override int GetHashCode()
		{
			return this.Node.GetHashCode() ^
				this.Field.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as FieldEffect;

			return other != null &&
				this.Node.Equals(other.Node) &&
				this.Field.Equals(other.Field);
		}
	}

	public class FieldEffectsInfo
	{
		//public IMethodReference Method { get; private set; }
		public ISet<FieldEffect> UsedFields { get; private set; }
		public ISet<FieldEffect> ModifiedFields { get; private set; }

		//public FieldEffectsInfo(IMethodReference method)
		public FieldEffectsInfo()
		{
			//this.Method = method;
			this.UsedFields = new HashSet<FieldEffect>();
			this.ModifiedFields = new HashSet<FieldEffect>();
		}
	}

	public class FieldEffectsAnalysis
	{
		public const string INFO_FEA = "FEA";

		#region class FieldEffectsCollector

		private class FieldEffectsCollector : InstructionVisitor
		{
			private FieldEffectsInfo effectsInfo;
			private PointsToGraph ptg;
			private EscapeInfo escapeInfo;

			public FieldEffectsCollector(EscapeInfo escapeInfo, FieldEffectsInfo effectsInfo)
			{
				this.escapeInfo = escapeInfo;
				this.effectsInfo = effectsInfo;
			}

			public void Visit(CFGNode node, PointsToGraph ptg)
			{
				this.ptg = ptg;
				this.Visit(node);
				this.ptg = null;
			}

			public override void Visit(LoadInstruction instruction)
			{
				var operand = instruction.Operand;

				if (operand is StaticFieldAccess)
				{
					var access = operand as StaticFieldAccess;
					ProcessAccess(access, effectsInfo.UsedFields);
				}
				else if (operand is InstanceFieldAccess)
				{
					var access = operand as InstanceFieldAccess;
					ProcessAccess(access, effectsInfo.UsedFields);
				}
				else if (operand is ArrayElementAccess)
				{
					var access = operand as ArrayElementAccess;
					ProcessAccess(access, effectsInfo.UsedFields);
				}
			}

			public override void Visit(StoreInstruction instruction)
			{
				var result = instruction.Result;

				if (result is StaticFieldAccess)
				{
					var access = result as StaticFieldAccess;
					ProcessAccess(access, effectsInfo.ModifiedFields);
				}
				else if (result is InstanceFieldAccess)
				{
					var access = result as InstanceFieldAccess;
					ProcessAccess(access, effectsInfo.ModifiedFields);
				}
				else if (result is ArrayElementAccess)
				{
					var access = result as ArrayElementAccess;
					ProcessAccess(access, effectsInfo.ModifiedFields);
				}
			}

			private void ProcessAccess(StaticFieldAccess access, ISet<FieldEffect> fields)
			{
				var variable = access.ToPTGGlobalVariable();
				var field = access.ToPTGNodeField();
				AddFieldEffects(variable, field, fields);
			}

			private void ProcessAccess(InstanceFieldAccess access, ISet<FieldEffect> fields)
			{
				var field = access.ToPTGNodeField();
				AddFieldEffects(access.Instance, field, fields);
			}

			private void ProcessAccess(ArrayElementAccess access, ISet<FieldEffect> fields)
			{
				var field = access.ToPTGNodeField();
				AddFieldEffects(access.Array, field, fields);
			}

			private void AddFieldEffects(IVariable src, PTGNodeField field, ISet<FieldEffect> fields)
			{
				var targets = ptg.GetTargets(src);

				foreach (var node in targets)
				{
					// Only add field effects for escaping nodes
					if (escapeInfo.CapturedNodes.Contains(node)) continue;

					var effect = new FieldEffect(node, field);
					fields.Add(effect);
				}
			}
		}

		#endregion

		private ProgramAnalysisInfo programInfo;
		private CallGraph callGraph;
		private IDictionary<IMethodReference, FieldEffectsInfo> result;
		private ISet<IMethodReference> worklist;

		public FieldEffectsAnalysis(ProgramAnalysisInfo programInfo, CallGraph callGraph)
		{
			this.programInfo = programInfo;
			this.callGraph = callGraph;
		}

		public IDictionary<IMethodReference, FieldEffectsInfo> Analyze()
		{
			result = new Dictionary<IMethodReference, FieldEffectsInfo>(MethodReferenceDefinitionComparer.Default);
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

			var invocations = callGraph.GetInvocations(method);
			var callees = invocations.SelectMany(inv => inv.PossibleCallees);
			//var effectsInfo = new FieldEffectsInfo(method);
			var effectsInfo = new FieldEffectsInfo();

			result.Add(method, effectsInfo);
			worklist.Remove(method);

			var methodInfo = programInfo.GetOrAdd(method);
			methodInfo.Add(INFO_FEA, effectsInfo);

			var escapeInfo = GetEscapeInfo(methodInfo);
			if (escapeInfo == null) return;

			foreach (var callee in callees)
			{
				FieldEffectsInfo calleeEffectsInfo;
				var ok = result.TryGetValue(callee, out calleeEffectsInfo);

				if (!ok)
				{
					Analyze(callee);
					calleeEffectsInfo = result[callee];
				}

				// Only add field effects for escaping nodes
				var escapingUsedFields = from effect in calleeEffectsInfo.UsedFields
										 where !escapeInfo.CapturedNodes.Contains(effect.Node)
										 select effect;

				var escapingModifiedFields = from effect in calleeEffectsInfo.ModifiedFields
											 where !escapeInfo.CapturedNodes.Contains(effect.Node)
											 select effect;

				effectsInfo.UsedFields.UnionWith(escapingUsedFields);
				effectsInfo.ModifiedFields.UnionWith(escapingModifiedFields);
			}

			var cfg = GetControlFlowGraph(methodInfo);

			if (cfg != null)
			{
				var pointsToInfo = GetPointsToInfo(methodInfo);

				FillEffects(cfg, pointsToInfo, escapeInfo, effectsInfo);
			}
		}

		private EscapeInfo GetEscapeInfo(MethodAnalysisInfo methodInfo)
		{
			EscapeInfo esc = null;
			methodInfo.TryGet(EscapeAnalysis.INFO_ESC, out esc);
			return esc;
		}

		private ControlFlowGraph GetControlFlowGraph(MethodAnalysisInfo methodInfo)
		{
			ControlFlowGraph cfg = null;
			methodInfo.TryGet(InterPointsToAnalysis.INFO_CFG, out cfg);
			return cfg;
		}

		private InterPointsToInfo GetPointsToInfo(MethodAnalysisInfo methodInfo)
		{
			InterPointsToInfo pti = null;
			methodInfo.TryGet(InterPointsToAnalysis.INFO_IPTA_RESULT, out pti);
			return pti;
		}

		private static void FillEffects(ControlFlowGraph cfg, InterPointsToInfo pointsToInfo, EscapeInfo escapeInfo, FieldEffectsInfo effectsInfo)
		{
			var collector = new FieldEffectsCollector(escapeInfo, effectsInfo);

			foreach (var node in cfg.Nodes)
			{
				var ptg = pointsToInfo.IntraPointsToInfo[node.Id].Output;
				collector.Visit(node, ptg);
			}
		}
	}
}
