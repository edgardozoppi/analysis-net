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

					foreach (var callee in possibleCallees)
					{
						if (!visitedMethods.Contains(callee))
						{
							worklist.Enqueue(callee);
							visitedMethods.Add(callee);
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
				// Instance method
				var method = host.ResolveReference(staticCallee) as MethodDefinition;

				if (method != null && (method.IsVirtual || method.IsAbstract))
				{
					// Overridable instance method
					var receiver = methodCall.Arguments.First();
					var receiverType = receiver.Type as BasicType;

					staticCallee = FindMethodImplementation(receiverType, staticCallee);
				}
			}

			return staticCallee;
		}

		private IMethodReference FindMethodImplementation(BasicType receiverType, IMethodReference method)
		{
			var result = method;

			if (!method.ContainingType.Equals(receiverType))
			{
				while (receiverType != null)
				{
					var receiverTypeDef = host.ResolveReference(receiverType) as ClassDefinition;

					if (receiverTypeDef != null)
					{
						var matchingMethod = receiverTypeDef.Methods.SingleOrDefault(m => m.MatchSignature(method));

						if (matchingMethod != null)
						{
							var matchingMethodRef = new MethodReference(method.Name, method.ReturnType)
							{
								Name = method.Name,
								IsStatic = method.IsStatic,
								GenericParameterCount = method.GenericParameterCount,
								ContainingType = receiverType
							};

							matchingMethodRef.Attributes.UnionWith(method.Attributes);
							matchingMethodRef.Parameters.AddRange(method.Parameters);

							result = matchingMethodRef;
							break;
						}
						else
						{
							receiverType = receiverTypeDef.Base;
						}
					}
				}
			}

			return result;
		}

		private IEnumerable<MethodDefinition> ResolvePossibleCallees(IMethodReference methodref)
		{
			var result = new HashSet<MethodDefinition>();
			var method = host.ResolveReference(methodref) as MethodDefinition;

			if (method != null)
			{
				result.Add(method);

				if (!method.IsStatic && (method.IsVirtual || method.IsAbstract))
				{
					var subtypes = classHierarchy.GetAllSubtypes(method.ContainingType);
					var compatibleMethods = from t in subtypes
											from m in t.Members.OfType<MethodDefinition>()
											where m.MatchSignature(methodref)
											select m;

					result.UnionWith(compatibleMethods);
				}
			}

			return result;
		}
	}
}
