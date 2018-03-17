using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;

namespace AsmProvider
{
	internal struct TypeDefinitionName
	{
		public string Namespace;
		public string ParentFullName;
		public string Name;

		public TypeDefinitionName(string @namespace, string parentName, string name)
		{
			this.Namespace = @namespace;
			this.ParentFullName = parentName;
			this.Name = name;
		}

		public bool IsNested
		{
			get { return this.ParentFullName != null; }
		}
	}

	internal struct TypeReferenceName
	{
		public string Namespace;
		public string Name;

		public TypeReferenceName(string @namespace, IEnumerable<string> parentNames, string name)
		{
			this.Namespace = @namespace;
			this.Name = name;
			this.ParentNames = new List<string>(parentNames);
		}

		public IList<string> ParentNames { get; private set; }

		public bool IsNestedType
		{
			get { return this.ParentNames.Count > 0; }
		}
	}

	internal struct MethodSignature
	{
		public IList<IType> Parameters;
		public IType Return;

		public MethodSignature(IList<IType> parameters, IType @return)
		{
			this.Parameters = parameters;
			this.Return = @return;
		}
	}

	internal static class Helper
	{
		public static TypeDefinitionName ParseTypeDefinitionName(string name)
		{
			string parentTypeName = null;
			string typeName = null;
			var @namespace = string.Empty;

			var typeNameStart = 0;
			var index = name.LastIndexOf('/');

			if (index >= 0)
			{
				@namespace = name.Substring(0, index).Replace('/', '.');
				typeNameStart = index + 1;
			}

			index = name.LastIndexOf('$');

			if (index >= 0)
			{
				parentTypeName = name.Substring(0, index);
				typeNameStart = index + 1;
			}

			typeName = name.Substring(typeNameStart);
			return new TypeDefinitionName(@namespace, parentTypeName, typeName);
		}

		public static TypeReferenceName ParseTypeReferenceName(string name)
		{
			string typeName = null;
			var @namespace = string.Empty;
			var parentTypeNames = new List<string>();

			var typeNameStart = 0;
			var index = name.LastIndexOf('/');

			if (index >= 0)
			{
				@namespace = name.Substring(0, index).Replace('/', '.');
				typeNameStart = index + 1;
			}

			index = name.IndexOf('$');

			while (index >= 0)
			{
				var parentName = name.Substring(typeNameStart, index - typeNameStart);
				parentTypeNames.Add(parentName);
				typeNameStart = index + 1;
				index = name.IndexOf('$', typeNameStart);
			}

			typeName = name.Substring(typeNameStart);
			return new TypeReferenceName(@namespace, parentTypeNames, typeName);
		}

		public static Namespace GetOrCreateNamespace(Assembly assembly, string @namespace)
		{
			var parts = @namespace.Split('.');
			var currentNamespace = assembly.RootNamespace;

			foreach (var part in parts)
			{
				var nestedNamespace = currentNamespace.Namespaces.SingleOrDefault(ns => ns.Name == part);

				if (nestedNamespace == null)
				{
					nestedNamespace = new Namespace(part)
					{
						ContainingAssembly = assembly,
						ContainingNamespace = currentNamespace
					};

					currentNamespace.Namespaces.Add(nestedNamespace);
				}

				currentNamespace = nestedNamespace;
			}

			return currentNamespace;
		}

		public static IType ParseTypeDescriptor(Host host, Assembly assembly, string descriptor)
		{
			var start = 0;
			var result = ParseTypeDescriptor(host, assembly, descriptor, ref start);
			return result;
		}

		public static MethodSignature ParseMethodDescriptor(Host host, Assembly assembly, string descriptor)
		{
			var parameters = new List<IType>();
			var start = 1; // Skip '('

			while (start < descriptor.Length && descriptor[start] != ')')
			{
				var type = ParseTypeDescriptor(host, assembly, descriptor, ref start);
				parameters.Add(type);
			}

			start++; // Skip ')'
			var result = ParseTypeDescriptor(host, assembly, descriptor, ref start);

			return new MethodSignature(parameters, result);
		}

		private static IType ParseTypeDescriptor(Host host, Assembly assembly, string descriptor, ref int index)
		{
			IType result = null;
			var arrayCount = 0;

			while (index < descriptor.Length && result == null)
			{
				var c = descriptor[index];
				index++;

				switch (c)
				{
					case 'B': result = PlatformTypes.Byte; break;
					case 'C': result = PlatformTypes.Char; break;
					case 'D': result = PlatformTypes.Double; break;
					case 'F': result = PlatformTypes.Single; break;
					case 'I': result = PlatformTypes.Int32; break;
					case 'J': result = PlatformTypes.Int64; break;
					case 'S': result = PlatformTypes.Short; break;
					case 'Z': result = PlatformTypes.Byte; break;
					case 'V': result = PlatformTypes.Void; break;
					case '[':
						arrayCount++;
						break;

					case 'L':
						var end = descriptor.IndexOf(';', index);
						var name = descriptor.Substring(index, end - index);
						var qualifiedName = ParseTypeReferenceName(name);
						result = CreateTypeReference(host, assembly, qualifiedName);
						index = end + 1;
						break;

					default: throw new NotSupportedException();
				}
			}

			while (arrayCount > 0)
			{
				result = new ArrayType(result);
				arrayCount--;
			}

			return result;
		}

		public static BasicType CreateTypeReference(Host host, Assembly assembly, TypeReferenceName qualifiedName)
		{
			BasicType parentType = null;

			foreach (var parentName in qualifiedName.ParentNames)
			{
				parentType = new BasicType(parentName, TypeKind.ReferenceType)
				{
					ContainingAssembly = assembly,
					ContainingNamespace = qualifiedName.Namespace,
					ContainingType = parentType
				};

				parentType.Resolve(host);
			}

			var result = new BasicType(qualifiedName.Name, TypeKind.ReferenceType)
			{
				ContainingAssembly = assembly,
				ContainingNamespace = qualifiedName.Namespace,
				ContainingType = parentType
			};

			result.Resolve(host);
			return result;
		}

		public static bool HasAnyFlag(int value, params int[] flags)
		{
			var result = false;

			for (var i = 0; i < flags.Length && !result; ++i)
			{
				var flag = flags[i];
				result = HasFlag(value, flag);
			}

			return result;
		}

		public static bool HasFlag(int value, int flag)
		{
			return (value & flag) == flag;
		}
	}
}
