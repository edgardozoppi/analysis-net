using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public class Namespace : ITypeDefinitionContainer
	{
		public string Name { get; private set; }
		public IList<Namespace> Namespaces { get; private set; }
		public IList<ITypeDefinition> Types { get; private set; }

		public Namespace(string name)
		{
			this.Name = name;
			this.Namespaces = new List<Namespace>();
			this.Types = new List<ITypeDefinition>();
		}
	}
}
