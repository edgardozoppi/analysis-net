using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsmProvider
{
	internal class AssemblyContext
	{
		public AssemblyContext(Host host, Assembly assembly)
		{
			this.Host = host;
			this.Assembly = assembly;
			this.DefinedTypes = new Dictionary<string, ITypeDefinition>();
			this.NestedTypes = new Dictionary<string, ISet<ITypeDefinition>>();
		}

		public Host Host { get; private set; }
		public Assembly Assembly { get; private set; }

		public IDictionary<string, ITypeDefinition> DefinedTypes { get; private set; }
		public IDictionary<string, ISet<ITypeDefinition>> NestedTypes { get; private set; }

		public void AddNestedType(string parentDescriptor, ITypeDefinition child)
		{
			ISet<ITypeDefinition> childs;
			var ok = this.NestedTypes.TryGetValue(parentDescriptor, out childs);

			if (!ok)
			{
				childs = new HashSet<ITypeDefinition>();
				this.NestedTypes.Add(parentDescriptor, childs);
			}

			childs.Add(child);
		}
	}
}
