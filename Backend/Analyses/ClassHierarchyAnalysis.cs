// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using Backend.Transformations;
using Backend.Utils;
using Model;
using Model.ThreeAddressCode.Instructions;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class ClassHierarchyAnalysis
	{
		private ClassHierarchy classHierarchy;

		public ClassHierarchyAnalysis(ClassHierarchy hierarchy)
		{
			this.classHierarchy = hierarchy;
			this.OnReachableMethodFound = DefaultReachableMethodFound;
		}

		public ClassHierarchyAnalysis()
			: this(new ClassHierarchy())
		{
		}

		public Action<MethodDefinition> OnReachableMethodFound;

		public CallGraph Analyze(Host host)
		{
			var definedMethods = from a in host.Assemblies
								 from t in a.RootNamespace.GetAllTypes()
								 from m in t.Members.OfType<MethodDefinition>()
								 where m.HasBody
								 select m;

			classHierarchy.Analyze(host);
			var result = Analyze(definedMethods);
			return result;
		}

		public CallGraph Analyze(Assembly assembly)
		{
			var definedMethods = from t in assembly.RootNamespace.GetAllTypes()
								 from m in t.Members.OfType<MethodDefinition>()
								 where m.HasBody
								 select m;

			classHierarchy.Analyze(assembly);
			var result = Analyze(definedMethods);
			return result;
		}

		public CallGraph Analyze(Host host, IEnumerable<MethodDefinition> roots)
		{
			classHierarchy.Analyze(host);
			var result = Analyze(roots);
			return result;
		}

		private CallGraph Analyze(IEnumerable<MethodDefinition> roots)
		{
			var result = new CallGraph();
			var visitedMethods = new HashSet<MethodDefinition>();
			var worklist = new Queue<MethodDefinition>();

			foreach (var root in roots)
			{
				worklist.Enqueue(root);
				visitedMethods.Add(root);
				result.Add(root);
			}

			while (worklist.Count > 0)
			{
				var method = worklist.Dequeue();

				OnReachableMethodFound(method);

				var methodCalls = method.Body.Instructions.OfType<MethodCallInstruction>();

				foreach (var methodCall in methodCalls)
				{
					var isVirtual = methodCall.Operation == MethodCallOperation.Virtual;
					var staticCallee = ResolveStaticCallee(methodCall);
					var possibleCallees = ResolvePossibleCallees(staticCallee, isVirtual);

					result.Add(method, methodCall.Label, staticCallee);
					result.Add(method, methodCall.Label, possibleCallees);

					foreach (var calleeref in possibleCallees)
					{
						var calleedef = calleeref.ResolvedMethod;

						if (calleedef != null)
						{
							var isNewMethod = visitedMethods.Add(calleedef);

							if (isNewMethod)
							{
								worklist.Enqueue(calleedef);
							}
						}
					}
				}
			}

			return result;
		}

		protected virtual void DefaultReachableMethodFound(MethodDefinition method)
		{
			if (method.Body.Kind == MethodBodyKind.Bytecode)
			{
				var disassembler = new Disassembler(method);
				var body = disassembler.Execute();

				method.Body = body;
			}
		}

		private static IMethodReference ResolveStaticCallee(MethodCallInstruction methodCall)
		{
			var staticCallee = methodCall.Method;

			if (!staticCallee.IsStatic &&
				methodCall.Operation == MethodCallOperation.Virtual)
			{
				var receiver = methodCall.Arguments.First();
				var receiverType = receiver.Type as IBasicType;

				staticCallee = Helper.FindMethodImplementation(receiverType, staticCallee);
			}

			return staticCallee;
		}

		private IEnumerable<IMethodReference> ResolvePossibleCallees(IMethodReference method, bool isVirtualCall)
		{
			var result = new HashSet<IMethodReference>(MethodReferenceDefinitionComparer.Default);

			result.Add(method);

			if (!method.IsStatic && isVirtualCall)
			{
				var subtypes = classHierarchy.GetAllSubtypes(method.ContainingType);
				var compatibleMethods = from t in subtypes
										from m in t.Members.OfType<MethodDefinition>()
										where m.MatchSignature(method)
										select m;

				result.UnionWith(compatibleMethods);
			}

			return result;
		}
	}
}
