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

	public class Assembly : IAssemblyReference
    {
		public string Name { get; private set; }
		public IEnumerable<IAssemblyReference> References { get; private set; }

		public Assembly(string name)
		{
			this.Name = name;
			this.References = new List<IAssemblyReference>();
		}
    }
}
