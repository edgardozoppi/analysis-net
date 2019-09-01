using System;
using System.Linq;
using Model;
using Model.Types;
using SR = System.Reflection;

namespace ReflectionProvider
{
	internal class AssemblyExtractor
	{
		private Assembly assembly;
		private Namespace currentNamespace;
		private TypeDefinition currentType;
		private MethodDefinition currentMethod;

		public AssemblyExtractor(Host host)
		{
			this.Host = host;
		}

		public Host Host { get; private set; }

		public Assembly Extract(SR.Assembly assemblydef)
		{
			assembly = new Assembly(assemblydef.FullName);
			var references = assemblydef.GetReferencedAssemblies();

			foreach (var referencedef in references)
			{
				var reference = new AssemblyReference(referencedef.FullName);
				assembly.References.Add(reference);
			}

			foreach (var typedef in assemblydef.DefinedTypes)
			{
				ExtractType(typedef);
			}

			return assembly;
		}

		private void ExtractType(SR.TypeInfo typedef)
		{
			var type = new TypeDefinition(typedef.Name);

			if (currentType == null)
			{
				currentNamespace.Types.Add(type);
			}
			else
			{
				currentType.Types.Add(type);
			}

			type.ContainingType = currentType;
			type.ContainingAssembly = assembly;
			type.ContainingNamespace = currentNamespace;
			currentType = type;

			foreach (var nestedTypedef in typedef.DeclaredNestedTypes)
			{
				ExtractType(nestedTypedef);
			}

			currentType = currentType.ContainingType;
		}
	}
}