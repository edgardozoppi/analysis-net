// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cci = Microsoft.Cci;

namespace CCIProvider
{
	internal class AssemblyTraverser : Cci.MetadataTraverser
	{
		//private Host ourHost;
		private Cci.IMetadataHost cciHost;
		private Cci.PdbReader pdbReader;

		private Assembly currentAssembly;
		private Namespace currentNamespace;
		private TypeDefinition currentType;
		private TypeExtractor typeExtractor;

		public Assembly Result
		{
			get { return currentAssembly; }
		}

		public AssemblyTraverser(Host host, Cci.IMetadataHost cciHost, Cci.PdbReader pdbReader)
		{
			//this.ourHost = host;
			this.cciHost = cciHost;
			this.pdbReader = pdbReader;
			this.TraverseIntoMethodBodies = false;
			this.typeExtractor = new TypeExtractor(host);
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

			currentNamespace = ourNamespace.ContainingNamespace;
		}

		public override void TraverseChildren(Cci.INamedTypeDefinition typedef)
		{
			TypeDefinition result = null;

			if (typedef.IsClass)
			{
				result = typeExtractor.ExtractClass(typedef, pdbReader);
			}
			else if (typedef.IsInterface)
			{
				result = typeExtractor.ExtractInterface(typedef, pdbReader);
			}
			else if (typedef.IsStruct)
			{
				result = typeExtractor.ExtractStruct(typedef, pdbReader);
			}
			else if (typedef.IsEnum)
			{
				result = typeExtractor.ExtractEnum(typedef);
			}
			else if (typedef.IsDelegate)
			{
				// TODO: Fix! Create a specific DelegateDefinition model class instead of reusing ClassDefinition.
				result = typeExtractor.ExtractClass(typedef, pdbReader);
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
                else
                {
					currentNamespace.Types.Add(result);
                }
				currentType = result;
			}

			base.TraverseChildren(typedef);

			if (result != null)
			{
				currentType = result.ContainingType;
			}
		}
	}
}
