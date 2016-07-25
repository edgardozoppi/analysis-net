using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Model
{
	internal class ClassHierarchyInfo
	{
		public ITypeDefinition Type { get; private set; }
		public ISet<ClassHierarchyInfo> Subtypes { get; private set; }

		public ClassHierarchyInfo(ITypeDefinition type)
		{
			this.Type = type;
			this.Subtypes = new HashSet<ClassHierarchyInfo>();
		}

		public void FillWithAllSubtypes(ICollection<ITypeDefinition> result)
		{
			foreach (var info in this.Subtypes)
			{
				result.Add(info.Type);
				info.FillWithAllSubtypes(result);
			}
		}
	}

	public class ClassHierarchyAnalysis
	{
		private Host host;
		private IDictionary<ITypeDefinition, ClassHierarchyInfo> types;
		private bool analyzed;

		public ClassHierarchyAnalysis(Host host)
		{
			this.host = host;
			this.types = new Dictionary<ITypeDefinition, ClassHierarchyInfo>();
		}

		public IEnumerable<ITypeDefinition> Types
		{
			get { return types.Keys; }
		}

		public IEnumerable<ITypeDefinition> GetSubtypes(ITypeDefinition type)
		{
			ClassHierarchyInfo info;
			var result = Enumerable.Empty<ITypeDefinition>();

			if (types.TryGetValue(type, out info))
			{
				result = info.Subtypes.Select(x => x.Type);
			}

			return result;
		}

		public IEnumerable<ITypeDefinition> GetAllSubtypes(ITypeDefinition type)
		{
			ClassHierarchyInfo info;
			var result = Enumerable.Empty<ITypeDefinition>();

			if (types.TryGetValue(type, out info))
			{
				var subtypes = new List<ITypeDefinition>();
				info.FillWithAllSubtypes(subtypes);
				result = subtypes;
			}

			return result;
		}

		public void Analyze()
		{
			if (analyzed) return;
			analyzed = true;

			var definedTypes = host.Assemblies
				.SelectMany(a => a.RootNamespace.GetAllTypes())
				.Where(t => t is StructDefinition ||
							t is ClassDefinition ||
							t is InterfaceDefinition);

			foreach (var type in definedTypes)
			{
				Analyze(type);
			}
		}

		private void Analyze(ITypeDefinition type)
		{
			if (type is ClassDefinition)
			{
				var typeDef = type as ClassDefinition;
				Analyze(typeDef);
			}
			else if (type is StructDefinition)
			{
				var typeDef = type as StructDefinition;
				Analyze(typeDef);
			}
			else if (type is InterfaceDefinition)
			{
				var typeDef = type as InterfaceDefinition;
				Analyze(typeDef);
			}
		}

		private void Analyze(ClassDefinition typeDef)
		{
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

		private void Analyze(StructDefinition typeDef)
		{
			var typeInfo = GetInfo(typeDef);

			foreach (var interfaceRef in typeDef.Interfaces)
			{
				var interfaceDef = host.ResolveReference(interfaceRef);
				if (interfaceDef == null) continue;

				var interfaceInfo = GetInfo(interfaceDef);
				interfaceInfo.Subtypes.Add(typeInfo);
			}
		}

		private void Analyze(InterfaceDefinition typeDef)
		{
			var typeInfo = GetInfo(typeDef);

			foreach (var interfaceRef in typeDef.Interfaces)
			{
				var interfaceDef = host.ResolveReference(interfaceRef);
				if (interfaceDef == null) continue;

				var interfaceInfo = GetInfo(interfaceDef);
				interfaceInfo.Subtypes.Add(interfaceInfo);
			}
		}

		private ClassHierarchyInfo GetInfo(ITypeDefinition type)
		{
			ClassHierarchyInfo result;

			if (!types.TryGetValue(type, out result))
			{
				result = new ClassHierarchyInfo(type);
				types.Add(type, result);
			}

			return result;
		}
	}
}
