// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Model
{
	public class ClassHierarchy
	{
		#region class ClassHierarchyInfo

		private class ClassHierarchyInfo
		{
			public IBasicType Type { get; private set; }
			public ISet<ITypeDefinition> Subtypes { get; private set; }

			public ClassHierarchyInfo(IBasicType type)
			{
				this.Type = type;
				this.Subtypes = new HashSet<ITypeDefinition>();
			}
		}

		#endregion

		private Host host;
		private IDictionary<IBasicType, ClassHierarchyInfo> types;
		private bool analyzed;

		public ClassHierarchy(Host host)
		{
			this.host = host;
			this.types = new Dictionary<IBasicType, ClassHierarchyInfo>(BasicTypeDefinitionComparer.Default);
		}

		public IEnumerable<IBasicType> Types
		{
			get { return types.Keys; }
		}

		public IEnumerable<ITypeDefinition> GetSubtypes(IBasicType type)
		{
			ClassHierarchyInfo info;
			var result = Enumerable.Empty<ITypeDefinition>();

			if (types.TryGetValue(type, out info))
			{
				result = info.Subtypes;
			}

			return result;
		}

		public IEnumerable<ITypeDefinition> GetAllSubtypes(IBasicType type)
		{
			var result = new HashSet<ITypeDefinition>();
			var worklist = new HashSet<ITypeDefinition>();

			var subtypes = GetSubtypes(type);
			worklist.UnionWith(subtypes);

			while (worklist.Count > 0)
			{
				var subtype = worklist.First();
				worklist.Remove(subtype);

				var isNewSubtype = result.Add(subtype);

				if (isNewSubtype)
				{
					subtypes = GetSubtypes(subtype);
					worklist.UnionWith(subtypes);
				}
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
				var typedef = type as ClassDefinition;
				Analyze(typedef);
			}
			else if (type is StructDefinition)
			{
				var typedef = type as StructDefinition;
				Analyze(typedef);
			}
			else if (type is InterfaceDefinition)
			{
				var typedef = type as InterfaceDefinition;
				Analyze(typedef);
			}
		}

		private void Analyze(ClassDefinition type)
		{
			GetOrAddInfo(type);

			if (type.Base != null)
			{
				var baseInfo = GetOrAddInfo(type.Base);
				baseInfo.Subtypes.Add(type);
			}

			foreach (var interfaceref in type.Interfaces)
			{
				var interfaceInfo = GetOrAddInfo(interfaceref);
				interfaceInfo.Subtypes.Add(type);
			}
		}

		private void Analyze(StructDefinition type)
		{
			GetOrAddInfo(type);

			foreach (var interfaceref in type.Interfaces)
			{
				var interfaceInfo = GetOrAddInfo(interfaceref);
				interfaceInfo.Subtypes.Add(type);
			}
		}

		private void Analyze(InterfaceDefinition type)
		{
			GetOrAddInfo(type);

			foreach (var interfaceref in type.Interfaces)
			{
				var interfaceInfo = GetOrAddInfo(interfaceref);
				interfaceInfo.Subtypes.Add(type);
			}
		}

		private ClassHierarchyInfo GetOrAddInfo(IBasicType type)
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
