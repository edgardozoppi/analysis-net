using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cci = Microsoft.Cci;

namespace CCILoader
{
	internal static class TypeExtractor
	{
		public static EnumDefinition ExtractEnum(Cci.INamedTypeDefinition typedef)
		{
			var name = typedef.Name.Value;
			var type = new EnumDefinition(name);

			type.UnderlayingType = ExtractType(typedef.UnderlyingType) as BasicType;
			ExtractConstants(type, type.Constants, typedef.Fields);

			return type;
		}

		public static InterfaceDefinition ExtractInterface(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new InterfaceDefinition(name);

			ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
			ExtractInterfaces(type.Interfaces, typedef.Interfaces);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

			return type;
		}

		public static ClassDefinition ExtractClass(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new ClassDefinition(name);
			Cci.ITypeReference basedef = Cci.TypeHelper.BaseClass(typedef);

			if (basedef == null)
			{
				basedef = typedef.PlatformType.SystemObject;
			}

			type.Base = ExtractType(basedef) as BasicType;

			ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
			ExtractInterfaces(type.Interfaces, typedef.Interfaces);
			ExtractFields(type, type.Fields, typedef.Fields);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

			return type;
		}

		public static StructDefinition ExtractStruct(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new StructDefinition(name);

			ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
			ExtractFields(type, type.Fields, typedef.Fields);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

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
			else if (typeref is Cci.IFunctionPointerTypeReference)
			{
				var fptyperef = typeref as Cci.IFunctionPointerTypeReference;
				result = ExtractType((Cci.ITypeReference)fptyperef);
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

			type.Assembly = new AssemblyReference(containingAssembly);
			type.Namespace = containingNamespace;
			return type;
		}

		public static FunctionPointerType ExtractType(Cci.IFunctionPointerTypeReference typeref)
		{
			var returnType = ExtractType(typeref.Type);
			var type = new FunctionPointerType(returnType);

			ExtractParameters(type.Parameters, typeref.Parameters);

			type.IsStatic = typeref.IsStatic;
			return type;
		}

		public static IMetadataReference ExtractToken(Cci.IReference token)
		{
			IMetadataReference result = PlatformTypes.Unknown;

			if (token is Cci.IMethodReference)
			{
				var methodref = token as Cci.IMethodReference;
				result = ExtractReference(methodref);
			}
			else if (token is Cci.ITypeReference)
			{
				var typeref = token as Cci.ITypeReference;
				result = ExtractType(typeref);
			}
			else if (token is Cci.IFieldReference)
			{
				var fieldref = token as Cci.IFieldReference;
				result = ExtractReference(fieldref);
			}

			return result;
		}

		public static IFieldReference ExtractReference(Cci.IFieldReference fieldref)
		{
			var type = ExtractType(fieldref.Type);
			var field = new FieldReference(fieldref.Name.Value, type);

			field.ContainingType = (BasicType)ExtractType(fieldref.ContainingType);
			field.IsStatic = fieldref.IsStatic;
			return field;
		}

		public static IMethodReference ExtractReference(Cci.IMethodReference methodref)
		{
			var returnType = ExtractType(methodref.Type);
			var method = new MethodReference(methodref.Name.Value, returnType);

			ExtractParameters(method.Parameters, methodref.Parameters);

			method.GenericParameterCount = methodref.GenericParameterCount;
			method.ContainingType = (BasicType)ExtractType(methodref.ContainingType);
			method.IsStatic = methodref.IsStatic;
			return method;
		}

		private static void ExtractConstants(ITypeDefinition containingType, IList<ConstantDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
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

		private static void ExtractMethods(ITypeDefinition containingType, IList<MethodDefinition> dest, IEnumerable<Cci.IMethodDefinition> source, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			foreach (var methoddef in source)
			{
				var name = methoddef.Name.Value;
				var type = ExtractType(methoddef.Type);
				var method = new MethodDefinition(name, type);

				ExtractGenericParameters(method.GenericParameters, methoddef.GenericParameters);
				ExtractParameters(method.Parameters, methoddef.Parameters);
				ExtractBody(method.Body, methoddef.Body, sourceLocationProvider);

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

		private static void ExtractBody(MethodBody ourBody, Cci.IMethodBody cciBody, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			// TODO: Is not a good idea to extract all method bodies defined
			// in an assembly when loading it. It would be better to delay 
			// the extraction so it take place when it is actually needed,
			// like on demand in a lazy fashion.
			var codeProvider = new CodeProvider(sourceLocationProvider);

			codeProvider.ExtractBody(ourBody, cciBody);
		}

		private static void ExtractInterfaces(IList<BasicType> dest, IEnumerable<Cci.ITypeReference> source)
		{
			foreach (var interfaceref in source)
			{
				var type = ExtractType(interfaceref) as BasicType;

				dest.Add(type);
			}
		}

		private static void ExtractParameters(IList<IMethodParameterReference> dest, IEnumerable<Cci.IParameterTypeInformation> source)
		{
			foreach (var parameterref in source)
			{
				var type = TypeExtractor.ExtractType(parameterref.Type);
				var parameter = new MethodParameterReference(type);

				parameter.Kind = GetMethodParameterKind(parameterref);

				dest.Add(parameter);
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

		private static MethodParameterKind GetMethodParameterKind(Cci.IParameterTypeInformation parameterref)
		{
			var result = MethodParameterKind.In;

			if (parameterref.IsByReference) result = MethodParameterKind.Ref;

			return result;
		}

		private static MethodParameterKind GetMethodParameterKind(Cci.IParameterDefinition parameterdef)
		{
			var result = MethodParameterKind.In;

			if (parameterdef.IsOut) result = MethodParameterKind.Out;
			if (parameterdef.IsByReference) result = MethodParameterKind.Ref;

			return result;
		}

		private static void ExtractFields(ITypeDefinition containingType, IList<FieldDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
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
}
