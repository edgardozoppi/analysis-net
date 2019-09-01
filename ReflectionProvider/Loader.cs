using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SR = System.Reflection;

namespace ReflectionProvider
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
			throw new NotSupportedException();
		}

		public Assembly LoadAssembly(SR.Assembly assembly)
		{
			var ourAssembly = ExtractAssembly(assembly);

			this.Host.Assemblies.Add(ourAssembly);
			return ourAssembly;
		}

		private Assembly ExtractAssembly(SR.Assembly assembly)
		{
			var extractor = new AssemblyExtractor(this.Host);
			var result = extractor.Extract(assembly);
			return result;
		}
	}
}
