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
		public Loader(Host host)
		{
			this.Host = host;
		}

		public void Dispose()
		{
			this.Host = null;
			GC.SuppressFinalize(this);
		}

		public Host Host { get; private set; }

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

				this.Host.Assemblies.Add(assembly);
				return assembly;
			}
		}

		private Assembly ExtractAssembly(PEReader reader)
		{
			var extractor = new AssemblyExtractor(this.Host, reader);
			var result = extractor.Extract();
			return result;
		}
	}
}
