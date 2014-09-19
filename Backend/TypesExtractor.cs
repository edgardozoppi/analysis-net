using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend
{
	public class TypesExtractor : MetadataTraverser
	{
		private IMetadataHost host;

		public TypesExtractor(IMetadataHost host)
		{
			this.host = host;
			this.TraverseIntoMethodBodies = false;
		}

		public override void TraverseChildren(INamedTypeDefinition namedTypeDefinition)
		{
			base.TraverseChildren(namedTypeDefinition);
		}
	}
}
