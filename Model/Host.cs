// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public class Host
	{
		public IList<Assembly> Assemblies { get; private set; }

		public Host()
		{
			this.Assemblies = new List<Assembly>();
		}

		public void ResolvePlatformTypes()
		{
			var basicTypeInfo = typeof(BasicType);
			var typeInfo = typeof(PlatformTypes);
			var fieldsInfo = from f in typeInfo.GetFields()
							 where f.FieldType.Equals(basicTypeInfo)
							 select f;

			foreach (var fieldInfo in fieldsInfo)
			{
				var value = fieldInfo.GetValue(null) as BasicType;
				value.Resolve(this);
			}			
		}

		public ITypeDefinition ResolveReference(BasicType type)
		{
			var assembly = this.Assemblies.SingleOrDefault(a => a.MatchReference(type.Assembly));
			if (assembly == null) return null;

			var namespaces = type.Namespace.Split('.');
			var containingNamespace = assembly.RootNamespace;

			foreach (var name in namespaces)
			{
				containingNamespace = containingNamespace.Namespaces.SingleOrDefault(n => n.Name == name);
				if (containingNamespace == null) return null;
			}
			
			var result = containingNamespace.Types.SingleOrDefault(t => t.MatchReference(type));
			return result;
		}

		public ITypeMemberDefinition ResolveReference(ITypeMemberReference member)
		{
			var typedef = ResolveReference(member.ContainingType);
			if (typedef == null) return null;

			var result = typedef.Members.SingleOrDefault(m => m.MatchReference(member));
			return result;
		}
	}
}
