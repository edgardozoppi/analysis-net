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
	// Interprocedural May Points-To Analysis
	public class InterPointsToAnalysis
	{
		public const string CFG_INFO = "CFG";
		public const string PTG_INFO = "PTG";
		public const string PTA_INFO = "PTA";
		public const string INPUT_PTG_INFO = "INPUT_PTG";

		private CallGraph callGraph;
		private ProgramAnalysisInfo methodsInfo;
		private Stack<IMethodReference> callStack;

		public InterPointsToAnalysis(ProgramAnalysisInfo methodsInfo)
		{
			this.methodsInfo = methodsInfo;
			this.callGraph = new CallGraph();
			this.callStack = new Stack<IMethodReference>();
		}

		public CallGraph Analyze(MethodDefinition method)
		{
			callGraph.Add(method);
			callStack.Push(method);

			OnReachableMethodFound(method);

			var methodInfo = methodsInfo[method];
			var cfg = methodInfo.Get<ControlFlowGraph>(CFG_INFO);

			// TODO: Don't create unknown nodes when doing the inter PT analysis
			var pta = new PointsToAnalysis(cfg, method.ReturnType);
			pta.ProcessMethodCall = ProcessMethodCall;

			methodInfo.Add(PTA_INFO, pta);

			var result = pta.Analyze();

			methodInfo.Set(PTG_INFO, result);

			callStack.Pop();
			return callGraph;
		}

		protected PointsToGraph ProcessMethodCall(MethodCallInstruction methodCall, UniqueIDGenerator nodeIdGenerator, PointsToGraph input)
		{
			PointsToGraph output = null;
			var possibleCallees = ResolvePossibleCallees(methodCall, input);

			var caller = callStack.Peek();

			if (!callGraph.ContainsInvocation(caller, methodCall.Label))
			{
				callGraph.Add(caller, methodCall.Label, methodCall.Method);
			}

			callGraph.Add(caller, methodCall.Label, possibleCallees);

			foreach (var callee in possibleCallees)
			{
				var method = callee.ResolvedMethod;

				if (method != null)
				{
					callStack.Push(method);

					OnReachableMethodFound(method);

					var methodInfo = methodsInfo[method];

					var ptg = input.Clone();
					var binding = GetCallerCalleeBinding(methodCall.Arguments, method.Body.Parameters);
					var previousFrame = ptg.NewFrame(binding);

					// TODO: Garbage collect unreachable nodes!
					// They are nodes that the callee cannot access but the caller can.
					// I believe by doing this we can reach the fixpoint faster, but not sure. 

					PointsToGraph oldInput;
					var ok = methodInfo.TryGet(INPUT_PTG_INFO, out oldInput);

					var inputChanged = !ok || !ptg.GraphEquals(oldInput);

					if (ok && inputChanged)
					{
						ptg.Union(oldInput);
					}

					methodInfo.Set(INPUT_PTG_INFO, ptg);

					PointsToAnalysis pta;
					ok = methodInfo.TryGet(PTA_INFO, out pta);

					if (!ok)
					{
						var cfg = methodInfo.Get<ControlFlowGraph>(CFG_INFO);

						// TODO: Don't create unknown nodes when doing the inter PT analysis
						pta = new PointsToAnalysis(cfg, method.ReturnType, nodeIdGenerator);
						pta.ProcessMethodCall = ProcessMethodCall;

						methodInfo.Add(PTA_INFO, pta);
					}

					if (inputChanged)
					{
						methodInfo.Set(PTG_INFO, pta.GetResult());

						ptg = ptg.Clone();
						var result = pta.Analyze(ptg);

						ptg = result[ControlFlowGraph.ExitNodeId].Output.Clone();
					}
					else
					{
						var result = methodInfo.Get<DataFlowAnalysisResult<PointsToGraph>[]>(PTG_INFO);
						ptg = result[ControlFlowGraph.ExitNodeId].Output.Clone();
					}

					var parameterKind = method.Parameters.ToDictionary(p => p.Name, p => p.Kind);
					binding = GetCalleeCallerBinding(methodCall.Arguments, method.Body.Parameters, parameterKind, methodCall.Result, pta.ResultVariable);

					//ptg = result[ControlFlowGraph.ExitNodeId].Output.Clone();
					ptg.RestoreFrame(previousFrame, binding);

					// TODO: Garbage collect unreachable nodes!
					// They are nodes created by the callee that do not escape to the caller.
					// I believe by doing this we can reach the fixpoint faster, but not sure.

					if (output == null)
					{
						output = ptg;
					}
					else
					{
						output.Union(ptg);
					}

					callStack.Pop();
				}
			}

			if (output == null)
			{
				output = input;
			}

			return output;
		}

		// binding: parameter -> argument
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

		// binding: parameter -> argument
		private static IDictionary<IVariable, IVariable> GetCalleeCallerBinding(IList<IVariable> arguments, IList<IVariable> parameters, IDictionary<string, MethodParameterKind> parameterKind, IVariable callerResult, IVariable calleeResult)
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

				MethodParameterKind kind;
				var ok = parameterKind.TryGetValue(parameter.Name, out kind);

				if (ok && kind != MethodParameterKind.In)
				{
					binding.Add(parameter, argument);
				}
			}

			if (callerResult != null)
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

				foreach (var target in targets)
				{
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

		protected virtual void OnReachableMethodFound(MethodDefinition method)
		{
			if (method.Body.Kind == MethodBodyKind.Bytecode)
			{
				var disassembler = new Disassembler(method);
				var body = disassembler.Execute();

				method.Body = body;
			}

			MethodAnalysisInfo methodInfo;
			var ok = methodsInfo.TryGet(method, out methodInfo);

			if (!ok)
			{
				methodInfo = new MethodAnalysisInfo(method);
				methodsInfo.Add(method, methodInfo);
			}

			if (!methodInfo.Contains(CFG_INFO))
			{
				var cfa = new ControlFlowAnalysis(method.Body);
				var cfg = cfa.GenerateNormalControlFlow();
				//var cfg = cfa.GenerateExceptionalControlFlow();

				var splitter = new WebAnalysis(cfg);
				splitter.Analyze();
				splitter.Transform();

				method.Body.UpdateVariables();

				var typeAnalysis = new TypeInferenceAnalysis(cfg);
				typeAnalysis.Analyze();

				methodInfo.Add(CFG_INFO, cfg);
			}
		}
	}
}
