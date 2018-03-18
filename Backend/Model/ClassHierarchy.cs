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
			public ISet<TypeDefinition> Subtypes { get; private set; }

			public ClassHierarchyInfo(IBasicType type)
			{
				this.Type = type;
				this.Subtypes = new HashSet<TypeDefinition>();
			}
		}

		#endregion

		private IDictionary<IBasicType, ClassHierarchyInfo> types;
		private bool analyzed;

		public ClassHierarchy()
		{
			this.types = new Dictionary<IBasicType, ClassHierarchyInfo>(BasicTypeDefinitionComparer.Default);
		}

		public IEnumerable<IBasicType> Types
		{
			get { return types.Keys; }
		}

		public IEnumerable<TypeDefinition> GetSubtypes(IBasicType type)
		{
			ClassHierarchyInfo info;
			var result = Enumerable.Empty<TypeDefinition>();

			if (types.TryGetValue(type, out info))
			{
				result = info.Subtypes;
			}

			return result;
		}

		public IEnumerable<TypeDefinition> GetAllSubtypes(IBasicType type)
		{
			var result = new HashSet<TypeDefinition>();
			var worklist = new HashSet<TypeDefinition>();

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

		public void Analyze(Host host)
		{
			if (analyzed) return;
			analyzed = true;

			var definedTypes = from a in host.Assemblies
							   from t in a.RootNamespace.GetAllTypes()
							   select t;

			foreach (var type in definedTypes)
			{
				Analyze(type);
			}
		}

		public void Analyze(Assembly assembly)
		{
			if (analyzed) return;
			analyzed = true;

			var definedTypes = assembly.RootNamespace.GetAllTypes();

			foreach (var type in definedTypes)
			{
				Analyze(type);
			}
		}

		private void Analyze(TypeDefinition type)
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
