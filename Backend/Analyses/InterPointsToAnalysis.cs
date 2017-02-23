// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using Backend.Transformations;
using Backend.Utils;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;

namespace Backend.Analyses
{
	public class InterPointsToInfo : DataFlowAnalysisResult<PointsToGraph>
	{
		public DataFlowAnalysisResult<PointsToGraph>[] IntraPointsToInfo { get; set; }
	}

	// Interprocedural May Points-To Analysis
	public class InterPointsToAnalysis
	{
		public const string INFO_CG = "CG";
		public const string INFO_CFG = "CFG";
		public const string INFO_PTA = "PTA";
		public const string INFO_IPTA_RESULT = "IPTA_RESULT";

		private CallGraph callGraph;
		private ProgramAnalysisInfo programInfo;
		//private Stack<IMethodReference> callStack;

		public InterPointsToAnalysis(ProgramAnalysisInfo programInfo)
		{
			this.programInfo = programInfo;
			this.OnReachableMethodFound = DefaultReachableMethodFound;
			this.OnUnknownMethodFound = DefaultUnknownMethodFound;
			this.ProcessUnknownMethod = DefaultProcessUnknownMethod;
		}

		public Func<MethodDefinition, ControlFlowGraph> OnReachableMethodFound;
		public Func<IMethodReference, bool> OnUnknownMethodFound;
		public Func<IMethodReference, IMethodReference, MethodCallInstruction, UniqueIDGenerator, PointsToGraph, PointsToGraph> ProcessUnknownMethod;

		public CallGraph Analyze(MethodDefinition method)
		{
			callGraph = new CallGraph();
			callGraph.Add(method);

			programInfo.Add(INFO_CG, callGraph);

			//callStack = new Stack<IMethodReference>();
			//callStack.Push(method);

			var methodInfo = programInfo.GetOrAdd(method);

			var cfg = OnReachableMethodFound(method);
			// TODO: Don't create unknown nodes when doing the inter PT analysis
			var pta = new PointsToAnalysis(cfg, method);
			pta.ProcessMethodCall = ProcessMethodCall;

			methodInfo.Add(INFO_PTA, pta);

			var info = new InterPointsToInfo();
			methodInfo.Add(INFO_IPTA_RESULT, info);
			info.IntraPointsToInfo = pta.Result;

			var result = pta.Analyze();
			
			var ptg = result[ControlFlowGraph.ExitNodeId].Output;
			info.Output = ptg;

			//callStack.Pop();

			// TODO: Remove INFO_PTA from all method infos.
			return callGraph;
		}

		protected PointsToGraph ProcessMethodCall(IMethodReference caller, MethodCallInstruction methodCall, IDictionary<IBasicType, PTGNode> globalNodes, UniqueIDGenerator nodeIdGenerator, PointsToGraph input)
		{
			PointsToGraph output = null;
			var possibleCallees = ResolvePossibleCallees(methodCall, input);

			{
				// Call graph construction
				if (!callGraph.ContainsInvocation(caller, methodCall.Label))
				{
					callGraph.Add(caller, methodCall.Label, methodCall.Method);
				}

				callGraph.Add(caller, methodCall.Label, possibleCallees);
			}

			foreach (var callee in possibleCallees)
			{
				var method = callee.ResolvedMethod;
				var isUnknownMethod = method == null || method.IsExternal;

				InterPointsToInfo info;
				var processCallee = true;
				var methodInfo = programInfo.GetOrAdd(callee);
				var ok = methodInfo.TryGet(INFO_IPTA_RESULT, out info);

				if (!ok)
				{
					if (isUnknownMethod)
					{
						processCallee = OnUnknownMethodFound(callee);

						if (processCallee)
						{
							info = new InterPointsToInfo();
							methodInfo.Add(INFO_IPTA_RESULT, info);
						}
					}
					else
					{
						var cfg = OnReachableMethodFound(method);
						// TODO: Don't create unknown nodes when doing the inter PT analysis
						var pta = new PointsToAnalysis(cfg, method, globalNodes, nodeIdGenerator);
						pta.ProcessMethodCall = ProcessMethodCall;

						methodInfo.Add(INFO_PTA, pta);

						info = new InterPointsToInfo();
						methodInfo.Add(INFO_IPTA_RESULT, info);
						info.IntraPointsToInfo = pta.Result;
					}
				}

				// TODO: If the callee is not processed we should
				// return an unknown node representing the result
				// of the method call (if the return type is a
				// reference type).

				if (processCallee)
				{
					//callStack.Push(callee);					

					IList<IVariable> parameters;

					if (isUnknownMethod)
					{
						parameters = new List<IVariable>();

						if (!callee.IsStatic)
						{
							var parameter = new LocalVariable("this", true) { Type = callee.ContainingType };

							parameters.Add(parameter);
						}

						foreach (var p in callee.Parameters)
						{
							var name = string.Format("p{0}", p.Index + 1);
							var parameter = new LocalVariable(name, true) { Type = p.Type };

							parameters.Add(parameter);
						}
					}
					else
					{
						parameters = method.Body.Parameters;
					}

					var ptg = input.Clone();
					var binding = GetCallerCalleeBinding(methodCall.Arguments, parameters);
					var previousFrame = ptg.NewFrame(binding);

					//// Garbage collect unreachable nodes.
					//// They are nodes that the callee cannot access but the caller can.
					//// I believe by doing this we can reach the fixpoint faster, but not sure.
					//// [Important] This doesn't work because we are removing
					//// nodes and edges that cannot be restored later!!
					//ptg.CollectGarbage();

					var oldInput = info.Input;
					var inputChanged = true;

					if (oldInput != null)
					{
						inputChanged = !ptg.GraphEquals(oldInput);

						if (inputChanged)
						{
							ptg.Union(oldInput);
							// Even when the graphs were different,
							// it could be the case that one (ptg)
							// is a subgraph of the other (oldInput)
							// so the the result of the union of both
							// graphs is exactly the same oldInput graph.
							inputChanged = !ptg.GraphEquals(oldInput);
						}
					}

					if (inputChanged)
					{
						info.Input = ptg;

						if (isUnknownMethod)
						{
							ptg = ProcessUnknownMethod(callee, caller, methodCall, nodeIdGenerator, ptg);
						}
						else
						{
							var pta = methodInfo.Get<PointsToAnalysis>(INFO_PTA);
							var result = pta.Analyze(ptg);

							ptg = result[ControlFlowGraph.ExitNodeId].Output;
						}

						info.Output = ptg;
					}
					else
					{
						if (isUnknownMethod)
						{
							ptg = info.Output;
						}
						else
						{
							// We cannot use info.Output here because it could be a recursive call
							// and info.Output is assigned after analyzing the callee.
							ptg = info.IntraPointsToInfo[ControlFlowGraph.ExitNodeId].Output;
							info.Output = ptg;
						}
					}
					
					ptg = ptg.Clone();
					binding = GetCalleeCallerBinding(methodCall.Result, ptg.ResultVariable);
					ptg.RestoreFrame(previousFrame, binding);

					//// Garbage collect unreachable nodes.
					//// They are nodes created by the callee that do not escape to the caller.
					//// I believe by doing this we can reach the fixpoint faster, but not sure.
					//ptg.CollectGarbage();

					//callStack.Pop();

					if (ptg != null)
					{
						if (output == null)
						{
							output = ptg;
						}
						else
						{
							output.Union(ptg);
						}
					}
				}
			}

			// This is commented because we want to return null
			// when the method call is not processed.
			//
			//if (output == null)
			//{
			//	output = input;
			//}

			return output;
		}

		// binding: callee parameter -> caller argument
		private static IDictionary<IVariable, IVariable> GetCallerCalleeBinding(IList<IVariable> arguments, IList<IVariable> parameters)
		{
			var binding = new Dictionary<IVariable, IVariable>();

#if DEBUG
			if (arguments.Count != parameters.Count)
				throw new Exception("Different ammount of parameters and arguments");
#endif

			for (var i = 0; i < arguments.Count; ++i)
			{
				var argument = arguments[i];
				var parameter = parameters[i];

				binding.Add(parameter, argument);
			}

			return binding;
		}

		// binding: callee variable -> caller variable
		private static IDictionary<IVariable, IVariable> GetCalleeCallerBinding(IVariable callerResult, IVariable calleeResult)
		{
			var binding = new Dictionary<IVariable, IVariable>();

			if (calleeResult != null && callerResult != null)
			{
				binding.Add(calleeResult, callerResult);
			}

			return binding;
		}

		private static IEnumerable<IMethodReference> ResolvePossibleCallees(MethodCallInstruction methodCall, PointsToGraph ptg)
		{
			var result = new HashSet<IMethodReference>();
			var staticCallee = methodCall.Method;

			if (!staticCallee.IsStatic &&
				methodCall.Operation == MethodCallOperation.Virtual)
			{
				var receiver = methodCall.Arguments.First();
				var targets = ptg.GetTargets(receiver);

				if (targets.Count == 0)
				{
					// TODO: Unknown receiver found. Use CHA to get all possible subtypes of the receiver's declared static type.
					// We should create an unknown ptg node for the receiver.
					// But it would be much better to have the node already created from before.
					// It make no sense to have a reference pointing to nothing at all,
					// it should at least points-to Null or an unknown node.
#if DEBUG
					System.Diagnostics.Debugger.Break();
#endif
				}

				foreach (var target in targets)
				{
					// If the receiver points-to null the callee cannot be resolved.
					// TODO: Maybe we can simulate throwing a null reference exception.
					if (target.Kind == PTGNodeKind.Null) continue;

					var receiverType = target.Type as IBasicType;
					var callee = Helper.FindMethodImplementation(receiverType, staticCallee);

					result.Add(callee);
				}
			}
			else
			{
				result.Add(staticCallee);
			}

			return result;
		}

		protected virtual ControlFlowGraph DefaultReachableMethodFound(MethodDefinition method)
		{
			ControlFlowGraph cfg;
			var methodInfo = programInfo.GetOrAdd(method);
			var ok = methodInfo.TryGet(INFO_CFG, out cfg);

			if (!ok)
			{
				if (method.Body.Kind == MethodBodyKind.Bytecode)
				{
					var disassembler = new Disassembler(method);
					var body = disassembler.Execute();

					method.Body = body;
				}

				var cfa = new ControlFlowAnalysis(method.Body);
				cfg = cfa.GenerateNormalControlFlow();
				//cfg = cfa.GenerateExceptionalControlFlow();

				var splitter = new WebAnalysis(cfg);
				splitter.Analyze();
				splitter.Transform();

				method.Body.UpdateVariables();

				var typeAnalysis = new TypeInferenceAnalysis(cfg);
				typeAnalysis.Analyze();

				methodInfo.Add(INFO_CFG, cfg);
			}

			return cfg;
		}

		protected virtual bool DefaultUnknownMethodFound(IMethodReference method)
		{
			return false;
		}

		protected virtual PointsToGraph DefaultProcessUnknownMethod(IMethodReference callee, IMethodReference caller, MethodCallInstruction methodCall, UniqueIDGenerator nodeIdGenerator, PointsToGraph input)
		{
			return input;
		}
	}
}
