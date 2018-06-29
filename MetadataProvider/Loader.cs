using Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace MetadataProvider
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
			using (var stream = File.OpenRead(fileName))
			using (var reader = new PEReader(stream))
			{
				if (!reader.HasMetadata)
				{
					throw new Exception("The input is not a valid CLR module or assembly.");
				}

				var assembly = ExtractAssembly(reader);

				host.Assemblies.Add(assembly);
				return assembly;
			}
		}

		private Assembly ExtractAssembly(PEReader reader)
		{
			var extractor = new AssemblyExtractor(reader);
			var result = extractor.Extract();
			return result;
		}
	}
}
