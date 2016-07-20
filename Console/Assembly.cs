// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;

namespace Console
{
	public class Assembly : IDisposable
	{
		public string FileName { get; private set; }
		public bool IsLoaded { get; private set; }
		public IMetadataHost Host { get; private set; }
		public IModule Module { get; private set; }
		public PdbReader PdbReader { get; private set; }

		public Assembly(IMetadataHost host)
		{
			this.Host = host;
		}

		public Assembly(IMetadataHost host, IModule module)
		{
			this.Host = host;
			this.Module = module;
			this.IsLoaded = true;
		}

		public void Load(string fileName)
		{
			this.Module = this.Host.LoadUnitFrom(fileName) as IModule;

			if (this.Module == null || this.Module == Dummy.Module || this.Module == Dummy.Assembly)
				throw new Exception("The input is not a valid CLR module or assembly.");

			var pdbFileName = Path.ChangeExtension(fileName, "pdb");

			if (File.Exists(pdbFileName))
			{
				using (var pdbStream = File.OpenRead(pdbFileName))
					this.PdbReader = new PdbReader(pdbStream, this.Host);
			}

			this.Module = MetadataCopier.DeepCopy(this.Host, this.Module);

			this.FileName = fileName;
			this.IsLoaded = true;
		}

		public void Save(string fileName)
		{
			var pdbName = Path.ChangeExtension(fileName, "pdb");

			using (var peStream = File.Create(fileName))
			{
				if (this.PdbReader == null)
				{
					PeWriter.WritePeToStream(this.Module, this.Host, peStream);
				}
				else
				{
					using (var pdbWriter = new PdbWriter(pdbName, this.PdbReader))
						PeWriter.WritePeToStream(this.Module, this.Host, peStream, this.PdbReader, this.PdbReader, pdbWriter);
				}
			}
		}

		public void Unload()
		{
			if (!this.IsLoaded) return;

			if (this.PdbReader != null)
			{
				this.PdbReader.Dispose();
				this.PdbReader = null;
			}

			this.Module = null;
			this.FileName = null;
			this.IsLoaded = false;
		}

		public void Dispose()
		{
			this.Unload();
		}
	}
}
