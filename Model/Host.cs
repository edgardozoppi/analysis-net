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

		public ITypeDefinition ResolveType(BasicType type)
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
	}
}
