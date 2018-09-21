// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public class Namespace : ITypeDefinitionContainer
	{
		public IAssemblyReference ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public string Name { get; private set; }
		public IList<Namespace> Namespaces { get; private set; }
		public IList<ITypeDefinition> Types { get; private set; }

		public Namespace(string name)
		{
			this.Name = name;
			this.Namespaces = new List<Namespace>();
			this.Types = new List<ITypeDefinition>();
		}

		public string FullName
		{
			get
			{
				var containingNamespace = string.Empty;

				if (this.ContainingNamespace != null &&
					!string.IsNullOrWhiteSpace(this.ContainingNamespace.Name))
				{
					containingNamespace = string.Format("{0}.", this.ContainingNamespace.FullName);
				}

				return string.Format("{0}{1}", containingNamespace, this.Name);
			}
		}

		public override string ToString()
		{
			return string.Format("namespace {0}", this.FullName);
		}
	}
}
