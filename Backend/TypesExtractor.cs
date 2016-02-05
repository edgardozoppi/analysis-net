using Cci = Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;
using Model.ThreeAddressCode;
using Model.ThreeAddressCode.Values;

namespace Backend
{
	public class TypesExtractor
	{
		#region class TypesTraverser

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
				ITypeDefinition result = null;

				if (typedef.IsClass)
				{
					result = ExtractClass(typedef);
				}
				else if (typedef.IsInterface)
				{
					result = ExtractInterface(typedef);
				}
				else if (typedef.IsStruct)
				{
					result = ExtractStruct(typedef);
				}
				else if (typedef.IsEnum)
				{
					result = ExtractEnum(typedef);
				}

				if (result != null)
				{
					types.Add(result.Name, result);
				}

				base.TraverseChildren(typedef);
			}

			private static ITypeDefinition ExtractEnum(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new EnumDefinition(name);

				type.UnderlayingType = TypesExtractor.ExtractType(typedef.UnderlyingType) as BasicType;
				ExtractConstants(type.Constants, typedef.Fields);

				return type;
			}

			private static void ExtractConstants(IList<ConstantDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
			{
				source = source.Skip(1);

				foreach (var constdef in source)
				{
					var name = constdef.Name.Value;
					var value = constdef.CompileTimeValue.Value;
					var constant = new ConstantDefinition(name, value);

					dest.Add(constant);
				}
			}

			private static ITypeDefinition ExtractInterface(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new InterfaceDefinition(name);

				ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				ExtractInterfaces(type.Interfaces, typedef.Interfaces);
				ExtractMethods(type.Methods, typedef.Methods);

				return type;
			}

			private static ITypeDefinition ExtractClass(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new ClassDefinition(name);
				Cci.ITypeReference basedef = Cci.TypeHelper.BaseClass(typedef);

				if (basedef == null)
				{
					basedef = typedef.PlatformType.SystemObject;
				}

				type.Base = TypesExtractor.ExtractType(basedef) as BasicType;

				ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				ExtractInterfaces(type.Interfaces, typedef.Interfaces);
				ExtractFields(type.Fields, typedef.Fields);
				ExtractMethods(type.Methods, typedef.Methods);

				return type;
			}

			private static ITypeDefinition ExtractStruct(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new StructDefinition(name);

				ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				ExtractFields(type.Fields, typedef.Fields);
				ExtractMethods(type.Methods, typedef.Methods);

				return type;
			}

			private static void ExtractMethods(IList<MethodDefinition> dest, IEnumerable<Cci.IMethodDefinition> source)
			{
				foreach (var methoddef in source)
				{
					var name = methoddef.Name.Value;
					var type = TypesExtractor.ExtractType(methoddef.Type);
					var method = new MethodDefinition(name, type);

					ExtractGenericParameters(method.GenericParameters, methoddef.GenericParameters);
					ExtractParameters(method.Parameters, methoddef.Parameters);

					method.IsStatic = methoddef.IsStatic;
					method.IsConstructor = methoddef.IsConstructor;
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
					var type = TypesExtractor.ExtractType(interfaceref) as BasicType;

					dest.Add(type);
				}
			}

			private static void ExtractParameters(IList<MethodParameter> dest, IEnumerable<Cci.IParameterDefinition> source)
			{
				foreach (var parameterdef in source)
				{
					var name = parameterdef.Name.Value;
					var type = TypesExtractor.ExtractType(parameterdef.Type);
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

			private static void ExtractFields(IList<FieldDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
			{
				foreach (var fielddef in source)
				{
					var name = fielddef.Name.Value;
					var type = TypesExtractor.ExtractType(fielddef.Type);
					var field = new FieldDefinition(name, type);

					field.IsStatic = fielddef.IsStatic;
					dest.Add(field);
				}
			}
		}

		#endregion

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

		public static IType ExtractType(Cci.ITypeReference typeref)
		{
			IType result = null;

			if (typeref is Cci.IArrayTypeReference)
			{
				var atyperef = typeref as Cci.IArrayTypeReference;
				result = TypesExtractor.ExtractType(atyperef);
			}
			else if (typeref is Cci.IPointerTypeReference)
			{
				var ptyperef = typeref as Cci.IPointerTypeReference;
				result = TypesExtractor.ExtractType(ptyperef);
			}
			else if (typeref is Cci.IGenericParameterReference)
			{
				var gptyperef = typeref as Cci.IGenericParameterReference;
				result = TypesExtractor.ExtractType(gptyperef);
			}
			else if (typeref is Cci.IGenericTypeInstanceReference)
			{
				var gtyperef = typeref as Cci.IGenericTypeInstanceReference;
				result = TypesExtractor.ExtractType(gtyperef);
			}
			else if (typeref is Cci.INamedTypeReference)
			{
				var ntyperef = typeref as Cci.INamedTypeReference;
				result = TypesExtractor.ExtractType(ntyperef);
			}

			return result;
		}

		public static ArrayType ExtractType(Cci.IArrayTypeReference typeref)
		{
			var elements = TypesExtractor.ExtractType(typeref.ElementType);
			var type = new ArrayType(elements);

			return type;
		}

		public static PointerType ExtractType(Cci.IPointerTypeReference typeref)
		{
			var target = TypesExtractor.ExtractType(typeref.TargetType);
			var type = new PointerType(target);

			return type;
		}

		public static TypeVariable ExtractType(Cci.IGenericParameterReference typeref)
		{
			var name = TypesExtractor.GetTypeName(typeref);
			var type = new TypeVariable(name);

			return type;
		}

		public static BasicType ExtractType(Cci.IGenericTypeInstanceReference typeref)
		{
			var type = TypesExtractor.ExtractType(typeref.GenericType);
			TypesExtractor.ExtractGenericType(type, typeref);

			return type;
		}

		public static BasicType ExtractType(Cci.INamedTypeReference typeref)
		{
			var name = TypesExtractor.GetTypeName(typeref);
			var type = new BasicType(name);

			return type;
		}

		private static void ExtractGenericType(BasicType type, Cci.IGenericTypeInstanceReference typeref)
		{
			foreach (var argumentref in typeref.GenericArguments)
			{
				var typearg = TypesExtractor.ExtractType(argumentref);
				type.GenericArguments.Add(typearg);
			}
		}

		private static string GetTypeName(Cci.ITypeReference typeref)
		{
			var name = Cci.TypeHelper.GetTypeName(typeref, Cci.NameFormattingOptions.OmitContainingType | Cci.NameFormattingOptions.PreserveSpecialNames);
			return name;
		}
	}
}
