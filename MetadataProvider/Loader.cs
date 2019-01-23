using Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SRPE = System.Reflection.PortableExecutable;
using SRM = System.Reflection.Metadata;
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
			using (var reader = new SRPE.PEReader(stream))
			{
				if (!reader.HasMetadata)
				{
					throw new Exception("The input is not a valid CLR module or assembly.");
				}

                Func<string, Stream> streamProvider = path =>
                {
                    Stream result = null;

                    if (File.Exists(path))
                    {
                        result = File.OpenRead(path);
                    }

                    return result;
                };

                var ok = reader.TryOpenAssociatedPortablePdb(fileName, streamProvider,
                    out SRM.MetadataReaderProvider pdbProvider, out _);

                var assembly = ExtractAssembly(reader, pdbProvider);

				this.Host.Assemblies.Add(assembly);
				return assembly;
			}
		}

		private Assembly ExtractAssembly(SRPE.PEReader reader, SRM.MetadataReaderProvider pdbProvider)
		{
			var extractor = new AssemblyExtractor(this.Host, reader, pdbProvider);
			var result = extractor.Extract();
			return result;
		}
	}
}
