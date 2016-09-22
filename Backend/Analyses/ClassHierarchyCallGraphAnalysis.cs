// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using Backend.ThreeAddressCode.Instructions;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class ClassHierarchyCallGraphAnalysis
	{
		private IMetadataHost host;
		private ClassHierarchyAnalysis classHierarchy;

		public ClassHierarchyCallGraphAnalysis(IMetadataHost host, ClassHierarchyAnalysis cha)
		{
			this.host = host;
			this.classHierarchy = cha;
		}

		public ClassHierarchyCallGraphAnalysis(IMetadataHost host)
			: this(host, new ClassHierarchyAnalysis(host))
		{
		}

		public CallGraph Analyze()
		{
			var allDefinedMethods = from a in host.LoadedUnits.OfType<IModule>()
									from t in a.GetAllTypes()
									from m in t.Members.OfType<IMethodDefinition>()
									where m.Body != null
									select m;

			var result = Analyze(allDefinedMethods);
			return result;
		}

		public CallGraph Analyze(IEnumerable<IMethodDefinition> roots)
		{
			var result = new CallGraph();
			var visitedMethods = new HashSet<IMethodDefinition>();
			var worklist = new Queue<IMethodDefinition>();

			classHierarchy.Analyze();

			foreach (var root in roots)
			{
				worklist.Enqueue(root);
				visitedMethods.Add(root);
				result.Add(root);
			}

			while (worklist.Count > 0)
			{
				var method = worklist.Dequeue();
				var body = MethodBodyProvider.Instance.GetBody(method);
				var methodCalls = body.Instructions.OfType<MethodCallInstruction>();

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

		private IMethodReference ResolveStaticCallee(MethodCallInstruction methodCall)
		{
			var staticCallee = methodCall.Method;

			if (!staticCallee.IsStatic &&
				methodCall.Operation == MethodCallOperation.Virtual)
			{
				var receiver = methodCall.Arguments.First();
				var receiverType = receiver.Type as INamedTypeReference;

				staticCallee = FindMethodImplementation(receiverType, staticCallee);
			}

			return staticCallee;
		}

		private IMethodReference FindMethodImplementation(INamedTypeReference receiverType, IMethodReference method)
		{
			var result = method;

			while (receiverType != null && !method.ContainingType.Equals(receiverType))
			{
				var receiverTypeDef = receiverType.ResolvedType;
				if (receiverTypeDef == null) break;

				var matchingMethod = receiverTypeDef.Methods.SingleOrDefault(m => MemberHelper.SignaturesAreEqual(m, method));

				if (matchingMethod != null)
				{
					result = matchingMethod;
					break;
				}
				else
				{
					receiverType = receiverTypeDef.BaseClasses.SingleOrDefault() as INamedTypeReference;
				}

			}

			return result;
		}

		private IEnumerable<IMethodReference> ResolvePossibleCallees(IMethodReference methodref, bool isVirtualCall)
		{
			var result = new HashSet<IMethodReference>();

			result.Add(methodref);

			if (!methodref.IsStatic && isVirtualCall)
			{
				var subtypes = classHierarchy.GetAllSubtypes(methodref.ContainingType);
				var compatibleMethods = from t in subtypes
										from m in t.Members.OfType<IMethodDefinition>()
										where MemberHelper.SignaturesAreEqual(m, methodref)
										select m;

				result.UnionWith(compatibleMethods);
			}

			return result;
		}
	}
}
