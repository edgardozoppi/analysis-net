using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace CCILoader
{
	public class Loader : IDisposable
	{
		private Cci.MetadataReaderHost host;

		public Loader()
		{
			host = new Cci.PeReader.DefaultHost();
		}

		public void Dispose()
		{
			host.Dispose();
			host = null;
			GC.SuppressFinalize(this);
		}

		public Assembly LoadAssembly(string fileName)
		{
			var module = host.LoadUnitFrom(fileName) as Cci.IModule;

			if (module == null || module == Cci.Dummy.Module || module == Cci.Dummy.Assembly)
				throw new Exception("The input is not a valid CLR module or assembly.");

			var pdbFileName = Path.ChangeExtension(fileName, "pdb");
			Cci.PdbReader pdbReader = null;

			if (File.Exists(pdbFileName))
			{
				using (var pdbStream = File.OpenRead(pdbFileName))
				{
					pdbReader = new Cci.PdbReader(pdbStream, host);
				}
			}

			var assembly = this.ExtractAssembly(module, pdbReader);

			if (pdbReader != null)
			{
				pdbReader.Dispose();
			}

			return assembly;
		}

		private Assembly ExtractAssembly(Cci.IModule module, Cci.PdbReader pdbReader)
		{
			var traverser = new AssemblyTraverser(host, pdbReader);
			traverser.Traverse(module.ContainingAssembly);
			var result = traverser.Result;
			return result;
		}
	}
}
