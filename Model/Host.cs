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

		public Assembly ResolveReference(IAssemblyReference assembly)
		{
			if (assembly is Assembly)
			{
				return assembly as Assembly;
			}

			var result = this.Assemblies.SingleOrDefault(a => a.MatchReference(assembly));
			return result;
		}

		public ITypeDefinition ResolveReference(IBasicType type)
		{
			if (type is ITypeDefinition)
			{
				return type as ITypeDefinition;
			}

			// Find containing assembly
			var assembly = ResolveReference(type.ContainingAssembly);
			if (assembly == null) return null;

			// Find containing namespace
			var namespaces = type.ContainingNamespace.Split(".".ToArray(), StringSplitOptions.RemoveEmptyEntries);
			var containingNamespace = assembly.RootNamespace;

            foreach (var name in namespaces)
            {
                containingNamespace = containingNamespace.Namespaces.SingleOrDefault(n => n.Name == name);
                if (containingNamespace == null) return null;
            }

			// Find containing type
			var containingTypes = new Stack<IBasicType>();
			var containingType = type.ContainingType;
			ITypeDefinitionContainer container = containingNamespace;

			while (containingType != null)
			{
				containingTypes.Push(containingType);
				containingType = containingType.ContainingType;
			}

			while (containingTypes.Count > 0)
			{
				containingType = containingTypes.Pop();
				container = container.Types.SingleOrDefault(t => t.MatchReference(containingType)) as ITypeDefinitionContainer;
				if (container == null) return null;
			}

			// Find type
			var result = container.Types.SingleOrDefault(t => t.MatchReference(type));
            return result;
		}

		public ITypeMemberDefinition ResolveReference(ITypeMemberReference member)
		{
			if (member is ITypeMemberDefinition)
			{
				return member as ITypeMemberDefinition;
			}

			// Find containing type
			var typedef = ResolveReference(member.ContainingType);
			if (typedef == null) return null;

			// Find member
			var result = typedef.Members.SingleOrDefault(m => m.MatchReference(member));
			return result;
		}
	}
}
