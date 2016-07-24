using Model;
using Model.ThreeAddressCode.Instructions;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class ClassHierarchyInfo
	{
		public ITypeDefinition Type { get; set; }
		public ISet<ClassHierarchyInfo> Subtypes { get; private set; }

		public ClassHierarchyInfo(ITypeDefinition type)
		{
			this.Type = type;
			this.Subtypes = new HashSet<ClassHierarchyInfo>();
		}

		public IEnumerable<ITypeDefinition> GetAllSubtypes()
		{
			var result = new HashSet<ITypeDefinition>() { this.Type };

			foreach (var subtype in this.Subtypes)
			{
				var subtypes = subtype.GetAllSubtypes();
				result.UnionWith(subtypes);
			}

			return result;
		}
	}

	public class ClassHierarchy
	{
		private Host host;

		public IDictionary<ITypeDefinition, ClassHierarchyInfo> Types { get; private set; }

		public ClassHierarchy(Host host)
		{
			this.host = host;
			this.Types = new Dictionary<ITypeDefinition, ClassHierarchyInfo>();
		}

		public void Analyze()
		{
			var definedTypes = host.Assemblies
				.SelectMany(a => a.RootNamespace.GetAllTypes())
				.Where(t => t is StructDefinition ||
							t is ClassDefinition ||
							t is InterfaceDefinition);

			foreach (var type in definedTypes)
			{
				if (type is ClassDefinition)
				{
					var typeDef = type as ClassDefinition;
					var typeInfo = GetInfo(typeDef);

					var baseDef = host.ResolveReference(typeDef.Base);

					if (baseDef != null)
					{
						var baseInfo = GetInfo(baseDef);
						baseInfo.Subtypes.Add(typeInfo);
					}

					foreach (var interfaceref in typeDef.Interfaces)
					{
						var interfaceDef = host.ResolveReference(interfaceref);
						if (interfaceDef == null) continue;

						var interfaceInfo = GetInfo(interfaceDef);
						interfaceInfo.Subtypes.Add(typeInfo);
					}
				}
				else if (type is StructDefinition)
				{
					var typeDef = type as StructDefinition;
					var typeInfo = GetInfo(typeDef);

					foreach (var interfaceRef in typeDef.Interfaces)
					{
						var interfaceDef = host.ResolveReference(interfaceRef);
						if (interfaceDef == null) continue;

						var interfaceInfo = GetInfo(interfaceDef);
						interfaceInfo.Subtypes.Add(typeInfo);
					}
				}
				else if (type is InterfaceDefinition)
				{
					var typeDef = type as InterfaceDefinition;
					var typeInfo = GetInfo(typeDef);

					foreach (var interfaceRef in typeDef.Interfaces)
					{
						var interfaceDef = host.ResolveReference(interfaceRef);
						if (interfaceDef == null) continue;

						var interfaceInfo = GetInfo(interfaceDef);
						interfaceInfo.Subtypes.Add(interfaceInfo);
					}
				}
			}
		}

		private ClassHierarchyInfo GetInfo(ITypeDefinition type)
		{
			ClassHierarchyInfo result;

			if (!this.Types.TryGetValue(type, out result))
			{
				result = new ClassHierarchyInfo(type);
				this.Types.Add(type, result);
			}

			return result;
		}
	}

	public class ClassHierarchyAnalysis
	{
		private Host host;
		private ClassHierarchy classHierarchy;

		public ClassHierarchyAnalysis(Host host)
		{
			this.host = host;
			this.classHierarchy = new ClassHierarchy(host);
		}

		public void Analyze(MethodDefinition method)
		{
			classHierarchy.Analyze();
			var visitedMethods = new HashSet<MethodDefinition>();
			var worklist = new Queue<MethodDefinition>();

			worklist.Enqueue(method);
			visitedMethods.Add(method);

			while (worklist.Count > 0)
			{
				method = worklist.Dequeue();
				var methodCalls = method.Body.Instructions.OfType<MethodCallInstruction>();

				foreach (var methodCall in methodCalls)
				{
					var possibleCallees = ResolveCallee(methodCall.Method);

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
		}

		private IEnumerable<MethodDefinition> ResolveCallee(IMethodReference methodref)
		{
			var result = new HashSet<MethodDefinition>();

			if (methodref.IsStatic)
			{
				var method = host.ResolveReference(methodref) as MethodDefinition;

				if (method != null)
				{
					result.Add(method);
				}
			}
			else
			{
				var containingType = host.ResolveReference(methodref.ContainingType);

				if (containingType != null)
				{
					var subtypes = GetSubtypes(containingType);
					var compatibleMethods = from t in subtypes
											from m in t.Members.OfType<MethodDefinition>()
											where m.MatchSignature(methodref)
											select m;

					result.UnionWith(compatibleMethods);
				}
			}

			return result;
		}

		private IEnumerable<ITypeDefinition> GetSubtypes(ITypeDefinition type)
		{
			ClassHierarchyInfo info;
			var result = Enumerable.Empty<ITypeDefinition>();

			if (classHierarchy.Types.TryGetValue(type, out info))
			{
				result = info.GetAllSubtypes();
			}

			return result;
		}
	}
}
