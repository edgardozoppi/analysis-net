// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using Model;
using Model.ThreeAddressCode.Instructions;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class ClassHierarchyCallGraphAnalysis
	{
		private Host host;
		private ClassHierarchyAnalysis classHierarchy;

		public ClassHierarchyCallGraphAnalysis(Host host, ClassHierarchyAnalysis cha)
		{
			this.host = host;
			this.classHierarchy = cha;
		}

		public ClassHierarchyCallGraphAnalysis(Host host)
			: this(host, new ClassHierarchyAnalysis(host))
		{
		}

		public CallGraph Analyze()
		{
			var allDefinedMethods = from a in host.Assemblies
									from t in a.RootNamespace.GetAllTypes()
									from m in t.Members.OfType<MethodDefinition>()
									where m.Body != null
									select m;

			var result = Analyze(allDefinedMethods);
			return result;
		}

		public CallGraph Analyze(IEnumerable<MethodDefinition> roots)
		{
			var result = new CallGraph();
			var visitedMethods = new HashSet<MethodDefinition>();
			var worklist = new Queue<MethodDefinition>();

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
				var methodCalls = method.Body.Instructions.OfType<MethodCallInstruction>();

				foreach (var methodCall in methodCalls)
				{
					var staticCallee = ResolveStaticCallee(methodCall);
					var possibleCallees = ResolvePossibleCallees(staticCallee);

					result.Add(method, methodCall.Label, staticCallee);
					result.Add(method, methodCall.Label, possibleCallees);

					foreach (var calleeref in possibleCallees)
					{
						var calleedef = host.ResolveReference(calleeref) as MethodDefinition;

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

			if (!staticCallee.IsStatic)
			{
				var receiver = methodCall.Arguments.First();
				var receiverType = receiver.Type as IBasicType;

				staticCallee = FindMethodImplementation(receiverType, staticCallee);

				// For debugging purposes only
				if (staticCallee != methodCall.Method)
				{
					var method = host.ResolveReference(staticCallee) as MethodDefinition;

					if (method != null)
					{
						// Make sure it is an overridable instance method
						var isPolymorphic = method.IsAbstract || method.IsVirtual || method.IsConstructor;
						System.Diagnostics.Debug.Assert(isPolymorphic);
					}
				}
			}

			return staticCallee;
		}

		private IMethodReference FindMethodImplementation(IBasicType receiverType, IMethodReference method)
		{
			var result = method;

			while (receiverType != null && !method.ContainingType.Equals(receiverType))
			{
				var receiverTypeDef = receiverType.ResolvedType as ClassDefinition;
				if (receiverTypeDef == null) break;

				var matchingMethod = receiverTypeDef.Methods.SingleOrDefault(m => m.MatchSignature(method));

				if (matchingMethod != null)
				{
					result = matchingMethod;
					break;
				}
				else
				{
					receiverType = receiverTypeDef.Base;
				}

			}

			return result;
		}

		private IEnumerable<IMethodReference> ResolvePossibleCallees(IMethodReference methodref)
		{
			var result = new HashSet<IMethodReference>();

			result.Add(methodref);

			if (!methodref.IsStatic)
			{
				var subtypes = classHierarchy.GetAllSubtypes(methodref.ContainingType);
				var compatibleMethods = from t in subtypes
										from m in t.Members.OfType<MethodDefinition>()
										where m.MatchSignature(methodref)
										select m;

				result.UnionWith(compatibleMethods);

				// For debugging purposes only
				foreach (var method in compatibleMethods)
				{
					// Make sure it is an overridable instance method
					var isPolymorphic = method.IsAbstract || method.IsVirtual || method.IsConstructor;
					System.Diagnostics.Debug.Assert(isPolymorphic);
				}
			}

			return result;
		}
	}
}
