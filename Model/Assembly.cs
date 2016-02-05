using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
	public interface IAssemblyReference
	{
		string Name { get; }
	}

	public class AssemblyReference : IAssemblyReference
	{
		public string Name { get; private set; }

		public AssemblyReference(string name)
		{
			this.Name = name;
		}
	}

	public class Assembly : IAssemblyReference
	{
		public string Name { get; private set; }
		public IList<IAssemblyReference> References { get; private set; }
		public Namespace RootNamespace { get; set; }

		public Assembly(string name)
		{
			this.Name = name;
			this.References = new List<IAssemblyReference>();
		}
	}
}
