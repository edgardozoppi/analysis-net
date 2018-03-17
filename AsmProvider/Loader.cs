using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsmCore = Asm.Core;

namespace AsmProvider
{
	public class Loader : ILoader
	{
		private Host host;

		public Loader(Host host)
		{
			this.host = host;
		}

		public void Dispose()
		{
			this.host = null;
			GC.SuppressFinalize(this);
		}

		public Host Host
		{
			get { return host; }
		}

		public Assembly LoadAssembly(string fileName)
		{
			var name = Path.GetFileNameWithoutExtension(fileName);
			var assembly = new Assembly(name);

			assembly.RootNamespace = new Namespace(string.Empty)
			{
				ContainingAssembly = assembly
			};

			using (var jar = ZipFile.OpenRead(fileName))
			{
				var context = new AssemblyContext(host, assembly);

				foreach (var entry in jar.Entries)
				{
					if (!entry.Name.EndsWith(".class"))
						continue;

					using (var stream = entry.Open())
					{
						this.ExtractClass(context, stream);
					}
				}
			}

			host.Assemblies.Add(assembly);
			return assembly;
		}

		private void ExtractClass(AssemblyContext context, Stream stream)
		{
			var reader = new AsmCore.ClassReader(stream);
			var visitor = new ClassVisitor(context);
			reader.Accept(visitor, 0);
		}
	}
}
