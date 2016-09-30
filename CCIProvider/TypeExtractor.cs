// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model;
using Model.ThreeAddressCode.Values;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cci = Microsoft.Cci;

namespace CCIProvider
{
	internal class TypeExtractor
	{
		// Attribute classes can have attributes of their same type.
		// Example: [AttributeUsage]class AttributeUsage { ... }
		// So we need to cache attribute types to reuse them and avoid infinite recursion.
		// The problem is related with type references (IType) having attributes.
		// We cannot only add them to type definitions because the user may need
		// to know the attributes of a type defined in an external library.
		private static IDictionary<Cci.ITypeReference, BasicType> attributesCache;

		private Host host;

		public TypeExtractor(Host host)
		{
			this.host = host;
			attributesCache = new Dictionary<Cci.ITypeReference, BasicType>();
		}

		public EnumDefinition ExtractEnum(Cci.INamedTypeDefinition typedef)
		{
			var name = typedef.Name.Value;
			var type = new EnumDefinition(name);

			type.UnderlayingType = ExtractType(typedef.UnderlyingType) as IBasicType;
			ExtractAttributes(type.Attributes, typedef.Attributes);
			ExtractConstants(type, type.Constants, typedef.Fields);

			return type;
		}

		public InterfaceDefinition ExtractInterface(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new InterfaceDefinition(name);

			ExtractAttributes(type.Attributes, typedef.Attributes);
			ExtractGenericTypeParameters(type, typedef);
			ExtractInterfaces(type.Interfaces, typedef.Interfaces);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

			return type;
		}

		public ClassDefinition ExtractClass(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new ClassDefinition(name);
			var basedef = typedef.BaseClasses.SingleOrDefault();

			//if (basedef == null)
			//{
			//	basedef = typedef.PlatformType.SystemObject;
			//}

			type.Base = ExtractType(basedef) as IBasicType;
			type.IsDelegate = typedef.IsDelegate;

			ExtractAttributes(type.Attributes, typedef.Attributes);
			ExtractGenericTypeParameters(type, typedef);
			ExtractInterfaces(type.Interfaces, typedef.Interfaces);
			ExtractFields(type, type.Fields, typedef.Fields);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

			return type;
		}

		public StructDefinition ExtractStruct(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new StructDefinition(name);

			ExtractAttributes(type.Attributes, typedef.Attributes);
			ExtractGenericTypeParameters(type, typedef);
			ExtractInterfaces(type.Interfaces, typedef.Interfaces);
			ExtractFields(type, type.Fields, typedef.Fields);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

			return type;
		}

		public IType ExtractType(Cci.ITypeReference typeref)
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
			else if (typeref is Cci.IGenericTypeParameterReference)
			{
				var gptyperef = typeref as Cci.IGenericTypeParameterReference;
				result = ExtractType(gptyperef);
			}
			else if (typeref is Cci.IGenericMethodParameterReference)
			{
				var gptyperef = typeref as Cci.IGenericMethodParameterReference;
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
				result = ExtractType(fptyperef);
			}
			else if (typeref is Cci.INamedTypeReference)
			{
				var ntyperef = typeref as Cci.INamedTypeReference;
				result = ExtractType(ntyperef);
			}

			if (result is BasicType)
			{
				var basicType = result as BasicType;
				basicType.Resolve(host);
			}

			return result;
		}

		public ArrayType ExtractType(Cci.IArrayTypeReference typeref)
		{
			var elements = ExtractType(typeref.ElementType);
			var type = new ArrayType(elements, typeref.Rank);

			ExtractAttributes(type.Attributes, typeref.Attributes);

			return type;
		}

		public PointerType ExtractType(Cci.IPointerTypeReference typeref)
		{
			var target = ExtractType(typeref.TargetType);
			var type = new PointerType(target);

			ExtractAttributes(type.Attributes, typeref.Attributes);

			return type;
		}

		public IGenericParameterReference ExtractType(Cci.IGenericTypeParameterReference typeref)
		{
			var containingType = GetContainingType(typeref.DefiningType);
			var startIndex = TotalGenericParameterCount(containingType);
			var name = typeref.Name.Value;
			var index = (ushort)(startIndex + typeref.Index);
			var type = new GenericParameterReference(GenericParameterKind.Type, index, name);

			ExtractAttributes(type.Attributes, typeref.Attributes);

			return type;
		}

		private Cci.ITypeReference GetContainingType(Cci.ITypeReference typeref)
		{
			Cci.ITypeReference result = null;

			if (typeref is Cci.IGenericTypeInstanceReference)
			{
				var genericTyperef = typeref as Cci.IGenericTypeInstanceReference;
				typeref = genericTyperef.GenericType;
			}

			if (typeref is Cci.INestedTypeReference)
			{
				var nestedTyperef = typeref as Cci.INestedTypeReference;
				result = nestedTyperef.ContainingType;
			}

			return result;
		}

		public IGenericParameterReference ExtractType(Cci.IGenericMethodParameterReference typeref)
		{
			var name = typeref.Name.Value;
			var index = typeref.Index;
			var type = new GenericParameterReference(GenericParameterKind.Method, index, name);

			ExtractAttributes(type.Attributes, typeref.Attributes);

			return type;
		}

		public IGenericParameterReference ExtractType(Cci.IGenericParameterReference typeref)
		{
			IGenericReference genericContainer;
			var typerefEntry = typeref as Cci.IParameterListEntry;
			var kind = GetGenericParameterKind(typeref, out genericContainer);
			var name = typeref.Name.Value;

			if (kind == GenericParameterKind.Method)
			{
				var startIndex = TotalGenericParameterCount(typeref as Cci.IGenericMethodParameterReference);
				name = string.Format("T{0}", startIndex + typerefEntry.Index);
			}

			var type = new GenericParameterReference(kind, typerefEntry.Index, name);

			ExtractAttributes(type.Attributes, typeref.Attributes);

			type.GenericContainer = genericContainer;
			return type;
		}

		private static int TotalGenericParameterCount(Cci.IGenericMethodParameterReference typeref)
		{
			var containingType = typeref.DefiningMethod.ContainingType;
			var result = TotalGenericParameterCount(containingType as Cci.ITypeReference);
			return result;
		}

		private static int TotalGenericParameterCount(Cci.ITypeReference typeref)
		{
			var result = 0;

			while (typeref != null)
			{
				if (typeref is Cci.IGenericTypeInstanceReference)
				{
					var genericTyperef = typeref as Cci.IGenericTypeInstanceReference;
					typeref = genericTyperef.GenericType;
				}

				if (typeref is Cci.INamedTypeReference)
				{
					var namedTyperef = typeref as Cci.INamedTypeReference;
					result += namedTyperef.GenericParameterCount;
				}

				if (typeref is Cci.INestedTypeReference)
				{
					var nestedTyperef = typeref as Cci.INestedTypeReference;
					typeref = nestedTyperef.ContainingType;
				}
				else
				{
					//typeref = null;
					break;
				}
			}

			return result;
		}

		public IBasicType ExtractType(Cci.IGenericTypeInstanceReference typeref)
		{
			var genericTyperef = typeref.GenericType;

			if (genericTyperef is Cci.ISpecializedNestedTypeReference)
			{
				var specializedTyperef = genericTyperef as Cci.ISpecializedNestedTypeReference;
				genericTyperef = specializedTyperef.UnspecializedVersion;
			}

			var genericType = ExtractType(genericTyperef);
            var type = ExtractType(typeref.GenericType, false);
			type.GenericType = genericType;
			ExtractGenericType(type, typeref);

			return type;
		}

		public IBasicType ExtractType(Cci.INamedTypeReference typeref)
		{
			return ExtractType(typeref, true);
		}

		private BasicType ExtractType(Cci.INamedTypeReference typeref, bool canReturnFromCache)
		{
			//string containingAssembly;
			//string containingNamespace;
			//var name = GetTypeName(typeref, out containingAssembly, out containingNamespace);
			//var kind = GetTypeKind(typeref);
			//var type = new IBasicType(name, kind);

			BasicType type = null;

			if (!attributesCache.TryGetValue(typeref, out type) || !canReturnFromCache)
			{
				string containingAssembly;
				string containingNamespace;
				var name = GetTypeName(typeref, out containingAssembly, out containingNamespace);
				var kind = GetTypeKind(typeref);
				var newType = new BasicType(name, kind);

				newType.ContainingAssembly = new AssemblyReference(containingAssembly);
				newType.ContainingNamespace = containingNamespace;
				newType.GenericParameterCount = typeref.GenericParameterCount;

				if (typeref is Cci.INestedTypeReference)
				{
					var nestedTyperef = typeref as Cci.INestedTypeReference;
					newType.ContainingType = (IBasicType)ExtractType(nestedTyperef.ContainingType);
					newType.GenericParameterCount += newType.ContainingType.GenericParameterCount;
				}

				if (type == null)
				{
					attributesCache.Add(typeref, newType);

					ExtractAttributes(newType.Attributes, typeref.Attributes);
				}
				else
				{
					newType.Attributes.UnionWith(type.Attributes);
				}

				type = newType;
			}

			//ExtractAttributes(type.Attributes, typeref.Attributes);

			//type.Assembly = new AssemblyReference(containingAssembly);
			//type.Namespace = containingNamespace;

			return type;
		}

		#region Extract SpecializedType

		//public SpecializedType ExtractType(Cci.IGenericTypeInstanceReference typeref)
		//{
		//	var genericType = (GenericType)ExtractType(typeref.GenericType);
		//	var type = new SpecializedType(genericType);

		//	foreach (var argumentref in typeref.GenericArguments)
		//	{
		//		var typearg = ExtractType(argumentref);
		//		type.GenericArguments.Add(typearg);
		//	}

		//	return type;
		//}

		#endregion

		#region Extract GenericType and IBasicType

		//public IBasicType ExtractType(Cci.INamedTypeReference typeref)
		//{
		//	IBasicType type;
		//	string containingAssembly;
		//	string containingNamespace;
		//	var name = GetTypeName(typeref, out containingAssembly, out containingNamespace);
		//	var kind = GetTypeKind(typeref);

		//	if (typeref.GenericParameterCount > 0)
		//	{
		//		type = new GenericType(name, kind);
		//	}
		//	else
		//	{
		//		type = new IBasicType(name, kind);
		//	}

		//	ExtractAttributes(type.Attributes, typeref.Attributes);

		//	type.Assembly = new AssemblyReference(containingAssembly);
		//	type.Namespace = containingNamespace;

		//	return type;
		//}

		#endregion
		
		public FunctionPointerType ExtractType(Cci.IFunctionPointerTypeReference typeref)
		{
			var returnType = ExtractType(typeref.Type);
			var type = new FunctionPointerType(returnType);

			ExtractAttributes(type.Attributes, typeref.Attributes);
			ExtractParameters(type.Parameters, typeref.Parameters);

			type.IsStatic = typeref.IsStatic;

			return type;
		}

		public IMetadataReference ExtractToken(Cci.IReference token)
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

		public IFieldReference ExtractReference(Cci.IFieldReference fieldref)
		{
			var type = ExtractType(fieldref.Type);
			var field = new FieldReference(fieldref.Name.Value, type);

			ExtractAttributes(field.Attributes, fieldref.Attributes);

			field.ContainingType = (IBasicType)ExtractType(fieldref.ContainingType);
			//field.IsStatic = fieldref.IsStatic;
			field.IsStatic = fieldref.ResolvedField.IsStatic;

			return field;
		}

		public IMethodReference ExtractReference(Cci.IMethodReference methodref)
		{
			var returnType = ExtractType(methodref.Type);
			var method = new MethodReference(methodref.Name.Value, returnType);

			ExtractAttributes(method.Attributes, methodref.Attributes);
			ExtractParameters(method.Parameters, methodref.Parameters);

			method.GenericParameterCount = methodref.GenericParameterCount;
			method.ContainingType = (IBasicType)ExtractType(methodref.ContainingType);
			method.IsStatic = methodref.IsStatic;

            if (methodref is Cci.IGenericMethodInstanceReference)
            {
                var genericInstanceMethodref = methodref as Cci.IGenericMethodInstanceReference;

                foreach (var typeParameterref in genericInstanceMethodref.GenericArguments)
                {
                    var typeArgumentref = ExtractType(typeParameterref);
                    method.GenericArguments.Add(typeArgumentref);
                }

				var genericMethodref = genericInstanceMethodref.GenericMethod;

				if (genericMethodref is Cci.ISpecializedMethodReference)
				{
					var specializedMethodref = genericMethodref as Cci.ISpecializedMethodReference;
					genericMethodref = specializedMethodref.UnspecializedVersion;
				}

				method.GenericMethod = ExtractReference(genericMethodref);
			}

			return method;
		}

		private void ExtractAttributes(ISet<CustomAttribute> dest, IEnumerable<Cci.ICustomAttribute> source)
		{
			foreach (var attrib in source)
			{
				var attribute = new CustomAttribute();

				attribute.Type = ExtractType(attrib.Type);
				attribute.Constructor = ExtractReference(attrib.Constructor);

				ExtractArguments(attribute.Arguments, attrib.Arguments);

				dest.Add(attribute);
			}
		}

		private void ExtractArguments(IList<Constant> dest, IEnumerable<Cci.IMetadataExpression> source)
		{
			foreach (var mexpr in source)
			{
				Constant argument = null;

				if (mexpr is Cci.IMetadataConstant)
				{
					var mconstant = mexpr as Cci.IMetadataConstant;
					argument = new Constant(mconstant.Value);
				}
				else
				{
					//throw new NotImplementedException();
				}

				dest.Add(argument);
			}
		}

		private void ExtractConstants(ITypeDefinition containingType, IList<ConstantDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
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

		private void ExtractGenericType(BasicType type, Cci.IGenericTypeInstanceReference typeref)
		{
			if (type.ContainingType != null)
			{
				type.GenericArguments.AddRange(type.ContainingType.GenericArguments);
			}

			foreach (var argumentref in typeref.GenericArguments)
			{
				var typearg = ExtractType(argumentref);
				type.GenericArguments.Add(typearg);
			}
		}

		private string GetTypeName(Cci.ITypeReference typeref)
		{
			var name = Cci.TypeHelper.GetTypeName(typeref, Cci.NameFormattingOptions.OmitContainingType | Cci.NameFormattingOptions.PreserveSpecialNames);
			return name;
		}

		private string GetTypeName(Cci.INamedTypeReference type, out string containingAssembly, out string containingNamespace)
		{
			var namespaceParts = new List<string>();
			var result = type.Name.Value;
			Cci.ITypeReference typeref = type;

			while (typeref is Cci.INestedTypeReference ||
				   typeref is Cci.IGenericTypeInstanceReference ||
				   typeref is Cci.IGenericTypeParameterReference)
			{
				if (typeref is Cci.INestedTypeReference)
				{
					var nestedTyperef = typeref as Cci.INestedTypeReference;
					typeref = nestedTyperef.ContainingType;
				}
				else if (typeref is Cci.IGenericTypeInstanceReference)
				{
					var genericInstanceTyperef = typeref as Cci.IGenericTypeInstanceReference;
					typeref = genericInstanceTyperef.GenericType;
				}
				else if (typeref is Cci.IGenericTypeParameterReference)
				{
					var genericParameterTyperef = typeref as Cci.IGenericTypeParameterReference;
					typeref = genericParameterTyperef.DefiningType;
				}
			}

			var namespaceTyperef = typeref as Cci.INamespaceTypeReference;
			var namespaceref = namespaceTyperef.ContainingUnitNamespace;

			while (namespaceref is Cci.INestedUnitNamespaceReference)
			{				
				var nestedNamespaceref = namespaceref as Cci.INestedUnitNamespaceReference;
				namespaceParts.Insert(0, nestedNamespaceref.Name.Value);
				namespaceref = nestedNamespaceref.ContainingUnitNamespace;
			}

			var assemblyref = namespaceref as Cci.IUnitNamespaceReference;

			containingAssembly = assemblyref.Unit.Name.Value;
			containingNamespace = string.Join(".", namespaceParts);
			return result;
		}

		private TypeKind GetTypeKind(Cci.ITypeReference typeref)
		{
			var result = TypeKind.Unknown;

			if (typeref.IsValueType) result = TypeKind.ValueType;
			else result = TypeKind.ReferenceType;

			return result;
		}

		private void ExtractMethods(ITypeDefinition containingType, IList<MethodDefinition> dest, IEnumerable<Cci.IMethodDefinition> source, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			foreach (var methoddef in source)
			{
				var name = methoddef.Name.Value;
				var type = ExtractType(methoddef.Type);
				var method = new MethodDefinition(name, type);

				ExtractAttributes(method.Attributes, methoddef.Attributes);
				ExtractGenericMethodParameters(method, methoddef);
				ExtractParameters(method.Parameters, methoddef.Parameters);
				ExtractBody(method.Body, methoddef.Body, sourceLocationProvider);

				method.IsStatic = methoddef.IsStatic;
				method.IsAbstract = methoddef.IsAbstract;
				method.IsVirtual = methoddef.IsVirtual;
				method.IsConstructor = methoddef.IsConstructor;
				method.ContainingType = containingType;
				dest.Add(method);
			}
		}

		private void ExtractGenericTypeParameters(IGenericDefinition definingType, Cci.INamedTypeDefinition typedef)
		{
			var allGenericTypeParameters = GetAllGenericTypeParameters(typedef);

			for (var i = 0; i < allGenericTypeParameters.Count; ++i)
			{
				var parameterdef = allGenericTypeParameters[i];
				//var index = parameterdef.Index;
				var index = (ushort)i;
				var name = parameterdef.Name.Value;
				var typeKind = GetGenericParameterTypeKind(parameterdef);
				var parameter = new GenericParameter(GenericParameterKind.Type, index, name, typeKind);

				ExtractAttributes(parameter.Attributes, parameterdef.Attributes);

				parameter.GenericContainer = definingType;
				definingType.GenericParameters.Add(parameter);
			}
		}

		private static IList<Cci.IGenericTypeParameter> GetAllGenericTypeParameters(Cci.ITypeDefinition typedef)
		{
			var result = new List<Cci.IGenericTypeParameter>();

			while (typedef != null)
			{
				result.InsertRange(0, typedef.GenericParameters);

				if (typedef is Cci.INestedTypeDefinition)
				{
					var nestedTypedef = typedef as Cci.INestedTypeDefinition;
					typedef = nestedTypedef.ContainingTypeDefinition;
				}
				else
				{
					//typedef = null;
					break;
				}
			}

			return result;
		}

		private void ExtractGenericMethodParameters(IGenericDefinition definingMethod, Cci.IMethodDefinition methoddef)
		{
			foreach (var parameterdef in methoddef.GenericParameters)
			{
				var index = parameterdef.Index;
				var name = parameterdef.Name.Value;
				var typeKind = GetGenericParameterTypeKind(parameterdef);
				var parameter = new GenericParameter(GenericParameterKind.Method, index, name, typeKind);

				ExtractAttributes(parameter.Attributes, parameterdef.Attributes);

				parameter.GenericContainer = definingMethod;
				definingMethod.GenericParameters.Add(parameter);
			}
		}

		private TypeKind GetGenericParameterTypeKind(Cci.IGenericParameter parameterdef)
		{
			var result = TypeKind.Unknown;

			if (parameterdef.MustBeValueType) result = TypeKind.ValueType;
			if (parameterdef.MustBeReferenceType) result = TypeKind.ReferenceType;

			return result;
		}

		private GenericParameterKind GetGenericParameterKind(Cci.IGenericParameter parameterdef)
		{
			GenericParameterKind result;

			if (parameterdef is Cci.IGenericTypeParameter)
			{
				result = GenericParameterKind.Type;
			}
			else if (parameterdef is Cci.IGenericMethodParameter)
			{
				result = GenericParameterKind.Method;
			}
			else
			{
				throw new Exception("Unknown generic parameter kind");
			}

			return result;
		}

		private GenericParameterKind GetGenericParameterKind(Cci.IGenericParameterReference parameterref, out IGenericReference genericContainer)
		{
			GenericParameterKind result;
			genericContainer = null;

			if (parameterref is Cci.IGenericTypeParameterReference)
			{
				result = GenericParameterKind.Type;
				var typeParameter = parameterref as Cci.IGenericTypeParameterReference;
				//genericContainer = (IGenericReference)ExtractType(typeParameter.DefiningType);
			}
			else if (parameterref is Cci.IGenericMethodParameterReference)
			{
				result = GenericParameterKind.Method;
				var methodParameter = parameterref as Cci.IGenericMethodParameterReference;
				//genericContainer = (IGenericReference)ExtractReference(methodParameter.DefiningMethod);
			}
			else
			{
				throw new Exception("Unknown generic parameter reference kind");
			}

			return result;
		}

		private void ExtractBody(MethodBody ourBody, Cci.IMethodBody cciBody, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			// TODO: Is not a good idea to extract all method bodies defined
			// in an assembly when loading it. It would be better to delay 
			// the extraction so it take place when it is actually needed,
			// like on demand in a lazy fashion.
			var codeProvider = new CodeProvider(this, sourceLocationProvider);

			codeProvider.ExtractBody(ourBody, cciBody);
		}

		private void ExtractInterfaces(IList<IBasicType> dest, IEnumerable<Cci.ITypeReference> source)
		{
			foreach (var interfaceref in source)
			{
				var type = ExtractType(interfaceref) as IBasicType;

				dest.Add(type);
			}
		}

		private void ExtractParameters(IList<IMethodParameterReference> dest, IEnumerable<Cci.IParameterTypeInformation> source)
		{
			foreach (var parameterref in source)
			{
				var type = ExtractType(parameterref.Type);
				var parameter = new MethodParameterReference(type);

				parameter.Kind = GetMethodParameterKind(parameterref);
				dest.Add(parameter);
			}
		}

		private void ExtractParameters(IList<MethodParameter> dest, IEnumerable<Cci.IParameterDefinition> source)
		{
			foreach (var parameterdef in source)
			{
				var name = parameterdef.Name.Value;
				var type = ExtractType(parameterdef.Type);
				var parameter = new MethodParameter(name, type);

				ExtractAttributes(parameter.Attributes, parameterdef.Attributes);

				parameter.Kind = GetMethodParameterKind(parameterdef);
				dest.Add(parameter);
			}
		}

		private MethodParameterKind GetMethodParameterKind(Cci.IParameterTypeInformation parameterref)
		{
			var result = MethodParameterKind.In;

			if (parameterref.IsByReference) result = MethodParameterKind.Ref;

			return result;
		}

		private MethodParameterKind GetMethodParameterKind(Cci.IParameterDefinition parameterdef)
		{
			var result = MethodParameterKind.In;

			if (parameterdef.IsOut) result = MethodParameterKind.Out;
			if (parameterdef.IsByReference) result = MethodParameterKind.Ref;

			return result;
		}

		private void ExtractFields(ITypeDefinition containingType, IList<FieldDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
		{
			foreach (var fielddef in source)
			{
				var name = fielddef.Name.Value;
				var type = ExtractType(fielddef.Type);
				var field = new FieldDefinition(name, type);

				ExtractAttributes(field.Attributes, fielddef.Attributes);

				field.IsStatic = fielddef.IsStatic;
				field.ContainingType = containingType;
				dest.Add(field);
			}
		}
	}
}
