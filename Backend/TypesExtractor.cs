using Cci = Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode;

namespace Backend
{
	public class TypesExtractor
	{
		private class TypesTraverser : Cci.MetadataTraverser
		{
			private Cci.IMetadataHost host;
			private IDictionary<string, ITypeDefinition> types;

			public TypesTraverser(Cci.IMetadataHost host)
			{
				this.host = host;
				this.TraverseIntoMethodBodies = false;
				this.types = new Dictionary<string, ITypeDefinition>();
			}

			public IDictionary<string, ITypeDefinition> Types
			{
				get { return this.types; }
			}

			public override void TraverseChildren(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;

				if (typedef.IsStruct)
				{
					var type = new StructDefinition(name);

					foreach (var fielddef in typedef.Fields)
					{
						var fieldname = fielddef.Name.Value;
						var fieldtype = this.ExtractType(fielddef.Type);
						var field = new FieldDefinition(fieldname, fieldtype);

						type.Fields.Add(fieldname, field);
					}

					types.Add(name, type);
				}

				base.TraverseChildren(typedef);
			}

			private IType ExtractType(Cci.ITypeReference typeref)
			{
				return null;
			}
		}

		private TypesTraverser traverser;

		public TypesExtractor(Cci.IMetadataHost host)
		{
			this.traverser = new TypesTraverser(host);
		}

		public IDictionary<string, ITypeDefinition> Extract(Cci.IModule module)
		{
			traverser.Traverse(module);
			return traverser.Types;
		}
	}
}
