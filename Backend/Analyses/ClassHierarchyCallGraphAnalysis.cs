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
					var possibleCallees = ResolveCallees(methodCall.Method);

					result.Add(method, methodCall.Label, methodCall.Method);
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

		private IEnumerable<MethodDefinition> ResolveCallees(IMethodReference methodref)
		{
			var result = new HashSet<MethodDefinition>();
			var method = host.ResolveReference(methodref) as MethodDefinition;

			if (method != null)
			{
				result.Add(method);

				if (!method.IsStatic)
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
