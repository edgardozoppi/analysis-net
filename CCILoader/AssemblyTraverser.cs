using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cci = Microsoft.Cci;

namespace CCILoader
{
	internal class AssemblyTraverser : Cci.MetadataTraverser
	{
		private Cci.IMetadataHost host;
		private Cci.PdbReader pdbReader;

		private Assembly currentAssembly;
		private Namespace currentNamespace;
		private ITypeDefinition currentType;

		public Assembly Result
		{
			get { return currentAssembly; }
		}

		public AssemblyTraverser(Cci.IMetadataHost host, Cci.PdbReader pdbReader)
		{
			this.host = host;
			this.pdbReader = pdbReader;
			this.TraverseIntoMethodBodies = false;
		}

		public override void TraverseChildren(Cci.IAssembly cciAssembly)
		{
			var ourAssembly = new Assembly(cciAssembly.Name.Value);

			foreach (var cciReference in cciAssembly.AssemblyReferences)
			{
				var ourReference = new AssemblyReference(cciReference.Name.Value);
				ourAssembly.References.Add(ourReference);
			}

			currentAssembly = ourAssembly;

			base.TraverseChildren(cciAssembly);
		}

		public override void TraverseChildren(Cci.INamespaceDefinition cciNamespace)
		{
			var ourNamespace = new Namespace(cciNamespace.Name.Value);

			if (currentNamespace == null)
			{
				currentAssembly.RootNamespace = ourNamespace;
			}
			else
			{
				currentNamespace.Namespaces.Add(ourNamespace);
			}

			ourNamespace.ContainingAssembly = new AssemblyReference(currentAssembly.Name);
			ourNamespace.ContainingNamespace = currentNamespace;
			currentNamespace = ourNamespace;

			base.TraverseChildren(cciNamespace);
		}

		public override void TraverseChildren(Cci.INamedTypeDefinition typedef)
		{
			ITypeDefinition result = null;

			if (typedef.IsClass)
			{
				result = TypeExtractor.ExtractClass(typedef, pdbReader);
			}
			else if (typedef.IsInterface)
			{
				result = TypeExtractor.ExtractInterface(typedef, pdbReader);
			}
			else if (typedef.IsStruct)
			{
				result = TypeExtractor.ExtractStruct(typedef, pdbReader);
			}
			else if (typedef.IsEnum)
			{
				result = TypeExtractor.ExtractEnum(typedef);
			}

			if (result != null)
			{
				result.ContainingAssembly = currentAssembly;
				result.ContainingNamespace = currentNamespace;
				result.ContainingType = currentType;

				if (currentType is ITypeDefinitionContainer)
				{
					var parentType = currentType as ITypeDefinitionContainer;
					parentType.Types.Add(result);
				}

				currentNamespace.Types.Add(result);
				currentType = result;
			}

			base.TraverseChildren(typedef);
		}
	}
}
