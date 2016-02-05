using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace CCILoader
{
	public class Loader : IDisposable
	{
		private Cci.MetadataReaderHost host;

		public Loader()
		{
			host = new Cci.PeReader.DefaultHost();
		}

		public void Dispose()
		{
			host.Dispose();
			host = null;
			GC.SuppressFinalize(this);
		}

		public Assembly LoadAssembly(string fileName)
		{
			var module = host.LoadUnitFrom(fileName) as Cci.IModule;

			if (module == null || module == Cci.Dummy.Module || module == Cci.Dummy.Assembly)
				throw new Exception("The input is not a valid CLR module or assembly.");

			var pdbFileName = Path.ChangeExtension(fileName, "pdb");
			Cci.PdbReader pdbReader = null;

			if (File.Exists(pdbFileName))
			{
				using (var pdbStream = File.OpenRead(pdbFileName))
				{
					pdbReader = new Cci.PdbReader(pdbStream, host);
				}
			}

			var assembly = this.ExtractAssembly(module, pdbReader);

			if (pdbReader != null)
			{
				pdbReader.Dispose();
			}

			return assembly;
		}

		private Assembly ExtractAssembly(Cci.IModule module, Cci.PdbReader pdbReader)
		{
			var traverser = new AssemblyTraverser(host, pdbReader);
			traverser.Traverse(module.ContainingAssembly);
			var result = traverser.Result;
			return result;
		}

		#region class AssemblyTraverser

		private class AssemblyTraverser : Cci.MetadataTraverser
		{
			private Cci.IMetadataHost host;
			private Cci.PdbReader pdbReader;

			private Namespace currentNamespace;

			public Assembly Result { get; private set; }

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

				this.Result = ourAssembly;

				base.TraverseChildren(cciAssembly);
			}

			public override void TraverseChildren(Cci.INamespaceDefinition cciNamespace)
			{
				var ourNamespace = new Namespace(cciNamespace.Name.Value);

				if (currentNamespace == null)
				{
					this.Result.RootNamespace = ourNamespace;
				}
				else
				{
					currentNamespace.Namespaces.Add(ourNamespace);
				}

				currentNamespace = ourNamespace;

				base.TraverseChildren(cciNamespace);
			}

			public override void TraverseChildren(Cci.INamedTypeDefinition typedef)
			{
				ITypeDefinition result = null;

				if (typedef.IsClass)
				{
					result = TypeExtractor.ExtractClass(typedef);
				}
				else if (typedef.IsInterface)
				{
					result = TypeExtractor.ExtractInterface(typedef);
				}
				else if (typedef.IsStruct)
				{
					result = TypeExtractor.ExtractStruct(typedef);
				}
				else if (typedef.IsEnum)
				{
					result = TypeExtractor.ExtractEnum(typedef);
				}

				if (result != null)
				{
					currentNamespace.Types.Add(result);
				}

				base.TraverseChildren(typedef);
			}
		}

		#endregion

		#region class TypeExtractor

		private static class TypeExtractor
		{
			public static ITypeDefinition ExtractEnum(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new EnumDefinition(name);
				var containingType = new BasicType(name, TypeKind.ValueType);

				type.UnderlayingType = ExtractType(typedef.UnderlyingType) as BasicType;
				ExtractConstants(containingType, type.Constants, typedef.Fields);

				return type;
			}

			public static void ExtractConstants(IType containingType, IList<ConstantDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
			{
				source = source.Skip(1);

				foreach (var constdef in source)
				{
					var name = constdef.Name.Value;
					var value = constdef.CompileTimeValue.Value;
					var constant = new ConstantDefinition(name, value);

					constant.ContainingType = containingType;
					dest.Add(constant);
				}
			}

			public static ITypeDefinition ExtractInterface(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new InterfaceDefinition(name);
				var containingType = new BasicType(name, TypeKind.ReferenceType);

				ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				ExtractInterfaces(type.Interfaces, typedef.Interfaces);
				ExtractMethods(containingType, type.Methods, typedef.Methods);

				return type;
			}

			public static ITypeDefinition ExtractClass(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new ClassDefinition(name);
				var containingType = new BasicType(name, TypeKind.ReferenceType);
				Cci.ITypeReference basedef = Cci.TypeHelper.BaseClass(typedef);

				if (basedef == null)
				{
					basedef = typedef.PlatformType.SystemObject;
				}

				type.Base = ExtractType(basedef) as BasicType;

				ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				ExtractInterfaces(type.Interfaces, typedef.Interfaces);
				ExtractFields(containingType, type.Fields, typedef.Fields);
				ExtractMethods(containingType, type.Methods, typedef.Methods);

				return type;
			}

			public static ITypeDefinition ExtractStruct(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new StructDefinition(name);
				var containingType = new BasicType(name, TypeKind.ValueType);

				ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				ExtractFields(containingType, type.Fields, typedef.Fields);
				ExtractMethods(containingType, type.Methods, typedef.Methods);

				return type;
			}

			public static IType ExtractType(Cci.ITypeReference typeref)
			{
				IType result = null;

				if (typeref is Cci.IArrayTypeReference)
				{
					var atyperef = typeref as Cci.IArrayTypeReference;
					result = ExtractType(atyperef);
				}
				else if (typeref is Cci.IPointerTypeReference)
				{
					var ptyperef = typeref as Cci.IPointerTypeReference;
					result = ExtractType(ptyperef);
				}
				else if (typeref is Cci.IGenericParameterReference)
				{
					var gptyperef = typeref as Cci.IGenericParameterReference;
					result = ExtractType(gptyperef);
				}
				else if (typeref is Cci.IGenericTypeInstanceReference)
				{
					var gtyperef = typeref as Cci.IGenericTypeInstanceReference;
					result = ExtractType(gtyperef);
				}
				else if (typeref is Cci.INamedTypeReference)
				{
					var ntyperef = typeref as Cci.INamedTypeReference;
					result = ExtractType(ntyperef);
				}

				return result;
			}

			public static ArrayType ExtractType(Cci.IArrayTypeReference typeref)
			{
				var elements = ExtractType(typeref.ElementType);
				var type = new ArrayType(elements);

				return type;
			}

			public static PointerType ExtractType(Cci.IPointerTypeReference typeref)
			{
				var target = ExtractType(typeref.TargetType);
				var type = new PointerType(target);

				return type;
			}

			public static TypeVariable ExtractType(Cci.IGenericParameterReference typeref)
			{
				var name = GetTypeName(typeref);
				var type = new TypeVariable(name);

				return type;
			}

			public static BasicType ExtractType(Cci.IGenericTypeInstanceReference typeref)
			{
				var type = ExtractType(typeref.GenericType);
				ExtractGenericType(type, typeref);

				return type;
			}

			public static BasicType ExtractType(Cci.INamedTypeReference typeref)
			{
				string containingAssembly;
				string containingNamespace;
				var name = GetTypeName(typeref, out containingAssembly, out containingNamespace);
				var kind = GetTypeKind(typeref);
				var type = new BasicType(name, kind);

				type.Assembly = containingAssembly;
				type.Namespace = containingNamespace;
				return type;
			}

			private static void ExtractGenericType(BasicType type, Cci.IGenericTypeInstanceReference typeref)
			{
				foreach (var argumentref in typeref.GenericArguments)
				{
					var typearg = ExtractType(argumentref);
					type.GenericArguments.Add(typearg);
				}
			}

			private static string GetTypeName(Cci.ITypeReference typeref)
			{
				var name = Cci.TypeHelper.GetTypeName(typeref, Cci.NameFormattingOptions.OmitContainingType | Cci.NameFormattingOptions.PreserveSpecialNames);
				return name;
			}

			private static string GetTypeName(Cci.ITypeReference typeref, out string containingAssembly, out string containingNamespace)
			{
				var fullName = Cci.TypeHelper.GetTypeName(typeref, Cci.NameFormattingOptions.OmitContainingType | Cci.NameFormattingOptions.PreserveSpecialNames);
				var lastDotIndex = fullName.LastIndexOf('.');
				string name;

				if (lastDotIndex > 0)
				{
					containingNamespace = fullName.Substring(0, lastDotIndex);
					name = fullName.Substring(lastDotIndex + 1);
				}
				else
				{
					containingNamespace = string.Empty;
					name = fullName;
				}

				var definingAssembly = Cci.TypeHelper.GetDefiningUnitReference(typeref);
				containingAssembly = definingAssembly.Name.Value;
				return name;
			}

			private static TypeKind GetTypeKind(Cci.ITypeReference typeref)
			{
				var result = TypeKind.Unknown;

				if (typeref.IsValueType) result = TypeKind.ValueType;
				else result = TypeKind.ReferenceType;

				return result;
			}

			private static void ExtractMethods(IType containingType, IList<MethodDefinition> dest, IEnumerable<Cci.IMethodDefinition> source)
			{
				foreach (var methoddef in source)
				{
					var name = methoddef.Name.Value;
					var type = ExtractType(methoddef.Type);
					var method = new MethodDefinition(name, type);

					ExtractGenericParameters(method.GenericParameters, methoddef.GenericParameters);
					ExtractParameters(method.Parameters, methoddef.Parameters);

					method.IsStatic = methoddef.IsStatic;
					method.IsConstructor = methoddef.IsConstructor;
					method.ContainingType = containingType;
					dest.Add(method);
				}
			}

			private static void ExtractGenericParameters(IList<TypeVariable> dest, IEnumerable<Cci.IGenericParameter> source)
			{
				foreach (var parameterdef in source)
				{
					var name = parameterdef.Name.Value;
					var parameter = new TypeVariable(name);

					parameter.TypeKind = GetTypeParameterKind(parameterdef);

					dest.Add(parameter);
				}
			}

			private static TypeKind GetTypeParameterKind(Cci.IGenericParameter parameterdef)
			{
				var result = TypeKind.Unknown;

				if (parameterdef.MustBeValueType) result = TypeKind.ValueType;
				if (parameterdef.MustBeReferenceType) result = TypeKind.ReferenceType;

				return result;
			}

			private static void ExtractInterfaces(IList<BasicType> dest, IEnumerable<Cci.ITypeReference> source)
			{
				foreach (var interfaceref in source)
				{
					var type = ExtractType(interfaceref) as BasicType;

					dest.Add(type);
				}
			}

			private static void ExtractParameters(IList<MethodParameter> dest, IEnumerable<Cci.IParameterDefinition> source)
			{
				foreach (var parameterdef in source)
				{
					var name = parameterdef.Name.Value;
					var type = ExtractType(parameterdef.Type);
					var parameter = new MethodParameter(name, type);

					parameter.Kind = GetMethodParameterKind(parameterdef);

					dest.Add(parameter);
				}
			}

			private static MethodParameterKind GetMethodParameterKind(Cci.IParameterDefinition parameterdef)
			{
				var result = MethodParameterKind.In;

				if (parameterdef.IsOut) result = MethodParameterKind.Out;
				if (parameterdef.IsByReference) result = MethodParameterKind.Ref;

				return result;
			}

			private static void ExtractFields(IType containingType, IList<FieldDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
			{
				foreach (var fielddef in source)
				{
					var name = fielddef.Name.Value;
					var type = ExtractType(fielddef.Type);
					var field = new FieldDefinition(name, type);

					field.IsStatic = fielddef.IsStatic;
					field.ContainingType = containingType;
					dest.Add(field);
				}
			}
		}

		#endregion
	}
}
