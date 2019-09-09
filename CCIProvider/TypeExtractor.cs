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
	internal class GenericContext
	{
		public IList<IGenericParameterReference> TypeParameters { get; private set; }
		public IList<IGenericParameterReference> MethodParameters { get; private set; }

		public GenericContext()
		{
			this.TypeParameters = new List<IGenericParameterReference>();
			this.MethodParameters = new List<IGenericParameterReference>();
		}

		public void Clear()
		{
			this.TypeParameters.Clear();
			this.MethodParameters.Clear();
		}

		public override string ToString()
		{
			return string.Format("Type: {0}, Method: {1}", this.TypeParameters.Count, this.MethodParameters.Count);
		}
	}

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
		private GenericContext defGenericContext;
		private GenericContext refGenericContext;
		private GenericContext genericContext;

		public TypeExtractor(Host host)
		{
			this.host = host;
			this.defGenericContext = new GenericContext();
			this.refGenericContext = new GenericContext();
			this.genericContext = defGenericContext;
			attributesCache = new Dictionary<Cci.ITypeReference, BasicType>();
		}

		public TypeDefinition ExtractEnum(Cci.INamedTypeDefinition typedef)
		{
			var name = typedef.Name.Value;
			var type = new TypeDefinition(name, TypeKind.ValueType, TypeDefinitionKind.Enum);

			type.UnderlayingType = ExtractType(typedef.UnderlyingType) as IBasicType;
			ExtractAttributes(type.Attributes, typedef.Attributes);
			ExtractConstants(type, type.Fields, typedef.Fields);

			return type;
		}

		public TypeDefinition ExtractInterface(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new TypeDefinition(name, TypeKind.ReferenceType, TypeDefinitionKind.Interface);

			ExtractAttributes(type.Attributes, typedef.Attributes);
			ExtractGenericTypeParameters(type, typedef);
			ExtractInterfaces(type.Interfaces, typedef.Interfaces);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

			defGenericContext.TypeParameters.Clear();
			return type;
		}

		public TypeDefinition ExtractClass(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new TypeDefinition(name, TypeKind.ReferenceType, TypeDefinitionKind.Class);
			var basedef = typedef.BaseClasses.SingleOrDefault();

			//if (basedef == null)
			//{
			//	basedef = typedef.PlatformType.SystemObject;
			//}

			type.Base = ExtractType(basedef) as IBasicType;

			if (typedef.IsDelegate)
			{
				type.Kind = TypeDefinitionKind.Delegate;
			}

			ExtractAttributes(type.Attributes, typedef.Attributes);
			ExtractGenericTypeParameters(type, typedef);
			ExtractInterfaces(type.Interfaces, typedef.Interfaces);
			ExtractFields(type, type.Fields, typedef.Fields);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

			defGenericContext.TypeParameters.Clear();
			return type;
		}

		public TypeDefinition ExtractStruct(Cci.INamedTypeDefinition typedef, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			var name = typedef.Name.Value;
			var type = new TypeDefinition(name, TypeKind.ValueType, TypeDefinitionKind.Struct);

			ExtractAttributes(type.Attributes, typedef.Attributes);
			ExtractGenericTypeParameters(type, typedef);
			ExtractInterfaces(type.Interfaces, typedef.Interfaces);
			ExtractFields(type, type.Fields, typedef.Fields);
			ExtractMethods(type, type.Methods, typedef.Methods, sourceLocationProvider);

			defGenericContext.TypeParameters.Clear();
			return type;
		}

		public IType ExtractType(Cci.ITypeReference typeref, bool isReference)
		{
			var type = ExtractType(typeref);

			if (isReference)
			{
				type = new PointerType(type);
			}

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
			else if (typeref is Cci.IManagedPointerTypeReference)
			{
				var ptyperef = typeref as Cci.IManagedPointerTypeReference;
				result = ExtractType(ptyperef);
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

				if (basicType.GenericType is BasicType)
				{
					basicType = basicType.GenericType as BasicType;
					basicType.Resolve(host);
				}
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

		public PointerType ExtractType(Cci.IManagedPointerTypeReference typeref)
		{
			var target = ExtractType(typeref.TargetType);
			var type = new PointerType(target);

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
			var index = startIndex + typeref.Index;

			return genericContext.TypeParameters[index];
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
			return genericContext.MethodParameters[typeref.Index];
		}

		public IGenericParameterReference ExtractType(Cci.IGenericParameterReference typeref)
		{
			IGenericParameterReference result;

			if (typeref is Cci.IGenericTypeParameterReference)
			{
				var typeParameter = typeref as Cci.IGenericTypeParameterReference;
				result = ExtractType(typeParameter);
			}
			else if (typeref is Cci.IGenericMethodParameterReference)
			{
				var methodParameter = typeref as Cci.IGenericMethodParameterReference;
				result = ExtractType(methodParameter);
			}
			else
			{
				throw new Exception("Unknown generic parameter reference kind");
			}

			return result;
		}

		private static int TotalGenericParameterCount(Cci.IGenericTypeParameterReference typeref)
		{
			var containingType = typeref.DefiningType;
			var result = TotalGenericParameterCount(containingType as Cci.ITypeReference);
			return result;
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

				if (type == null)
				{
					attributesCache.Add(typeref, newType);

					ExtractAttributes(newType.Attributes, typeref.Attributes);
				}
				else
				{
					newType.Attributes.UnionWith(type.Attributes);
				}

				newType.ContainingAssembly = new AssemblyReference(containingAssembly);
				newType.ContainingNamespace = containingNamespace;
				newType.GenericParameterCount = typeref.GenericParameterCount;

				if (typeref is Cci.INestedTypeReference)
				{
					var nestedTyperef = typeref as Cci.INestedTypeReference;
					newType.ContainingType = (IBasicType)ExtractType(nestedTyperef.ContainingType);
					newType.GenericParameterCount += newType.ContainingType.GenericParameterCount;
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
			var genericFieldref = fieldref;

			if (fieldref is Cci.ISpecializedFieldReference)
			{
				var specializedFieldref = fieldref as Cci.ISpecializedFieldReference;
				genericFieldref = specializedFieldref.UnspecializedVersion;
			}

			var containingType = (IBasicType)ExtractType(fieldref.ContainingType);
			CreateGenericParameterReferences(GenericParameterKind.Type, containingType.GenericParameterCount);

			var type = ExtractType(genericFieldref.Type);
			var field = new FieldReference(fieldref.Name.Value, type);

			ExtractAttributes(field.Attributes, fieldref.Attributes);

			field.ContainingType = containingType;
			field.IsStatic = fieldref.IsStatic || fieldref.ResolvedField.IsStatic;
			//field.IsStatic = fieldref.IsStatic;
			//field.IsStatic = fieldref.ResolvedField.IsStatic;

			BindGenericParameterReferences(GenericParameterKind.Type, containingType);
			return field;
		}

		public IMethodReference ExtractReference(Cci.IMethodReference methodref)
		{
			IMethodReference result;

			if (methodref is Cci.IGenericMethodInstanceReference)
			{
				var genericMethodref = methodref as Cci.IGenericMethodInstanceReference;
				result = ExtractMethodReference(genericMethodref);
			}
			else
			{
				result = ExtractMethodReference(methodref);
			}

			return result;
		}

		public IMethodReference ExtractMethodReference(Cci.IGenericMethodInstanceReference methodref)
		{
			var genericArguments = new List<IType>();

			foreach (var typeParameterref in methodref.GenericArguments)
			{
				var typeArgumentref = ExtractType(typeParameterref);
				genericArguments.Add(typeArgumentref);
			}

			CreateGenericParameterReferences(GenericParameterKind.Method, genericArguments.Count);

			var method = ExtractReference(methodref.GenericMethod);
			method = method.Instantiate(genericArguments);

			BindGenericParameterReferences(GenericParameterKind.Method, method);
			return method;
		}

		public IMethodReference ExtractMethodReference(Cci.IMethodReference methodref)
		{
			var genericMethodref = methodref;

			if (methodref is Cci.ISpecializedMethodReference)
			{
				var specializedMethodref = methodref as Cci.ISpecializedMethodReference;
				genericMethodref = specializedMethodref.UnspecializedVersion;
			}

			var containingType = (IBasicType)ExtractType(methodref.ContainingType);
			CreateGenericParameterReferences(GenericParameterKind.Type, containingType.GenericParameterCount);
			
			var returnType = ExtractType(genericMethodref.Type);
			var method = new MethodReference(methodref.Name.Value, returnType);

			ExtractAttributes(method.Attributes, methodref.Attributes);
			ExtractParameters(method.Parameters, genericMethodref.Parameters);

			method.GenericParameterCount = genericMethodref.GenericParameterCount;
			method.ContainingType = containingType;
			method.IsStatic = methodref.IsStatic;

			method.Resolve(host);
			BindGenericParameterReferences(GenericParameterKind.Type, containingType);
			return method;
		}

		private void CreateGenericParameterReferences(GenericParameterKind kind, int genericParameterCount)
		{
			IList<IGenericParameterReference> parameters;

			switch (kind)
			{
				case GenericParameterKind.Type:
					parameters = refGenericContext.TypeParameters;
					break;

				case GenericParameterKind.Method:
					parameters = refGenericContext.MethodParameters;
					break;

				default:
					throw kind.ToUnknownValueException();
			}

			for (var i = 0; i < genericParameterCount; ++i)
			{
				var genericParameterReference = new GenericParameterReference(kind, (ushort)i);
				parameters.Add(genericParameterReference);
			}

			genericContext = refGenericContext;
		}

		private void BindGenericParameterReferences(GenericParameterKind kind, IGenericReference genericContainer)
		{
			IList<IGenericParameterReference> parameters;

			switch (kind)
			{
				case GenericParameterKind.Type:
					parameters = refGenericContext.TypeParameters;
					break;

				case GenericParameterKind.Method:
					parameters = refGenericContext.MethodParameters;
					break;

				default:
					throw kind.ToUnknownValueException();
			}

			foreach (var parameter in parameters)
			{
				var param = parameter as GenericParameterReference;
				param.GenericContainer = genericContainer;
			}

			parameters.Clear();
			genericContext = defGenericContext;
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
					argument = ExtractConstant(mconstant);
				}
				else
				{
					//throw new NotImplementedException();
				}

				dest.Add(argument);
			}
		}

		private void ExtractConstants(TypeDefinition containingType, IList<FieldDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
		{
			source = source.Skip(1);

			foreach (var constdef in source)
			{
				var name = constdef.Name.Value;
				// Not sure if the type of the constant should be the enum type or the enum underlaying type.
				var constant = new FieldDefinition(name, containingType)
				{
					Value = ExtractConstant(constdef.CompileTimeValue)
				};

				constant.ContainingType = containingType;
				dest.Add(constant);
			}
		}

		private Constant ExtractConstant(Cci.IMetadataConstant constant)
		{
			var result = new Constant(constant.Value);
			result.Type = ExtractType(constant.Type);
			return result;
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

		private void ExtractMethods(TypeDefinition containingType, IList<MethodDefinition> dest, IEnumerable<Cci.IMethodDefinition> source, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			foreach (var methoddef in source)
			{
				var name = methoddef.Name.Value;
				var method = new MethodDefinition(name, null);

				ExtractAttributes(method.Attributes, methoddef.Attributes);
				ExtractGenericMethodParameters(method, methoddef);
				ExtractParameters(method.Parameters, methoddef.Parameters);

				method.ReturnType = ExtractType(methoddef.Type);

				if (!methoddef.IsExternal)
				{
					method.Body = ExtractBody(methoddef.Body, sourceLocationProvider);
				}

				method.Visibility = ExtractVisibilityKind(methoddef.Visibility);
				method.IsStatic = methoddef.IsStatic;
				method.IsAbstract = methoddef.IsAbstract;
				method.IsVirtual = methoddef.IsVirtual;
				method.IsConstructor = methoddef.IsConstructor;
				method.IsExternal = methoddef.IsExternal;
				method.ContainingType = containingType;
				dest.Add(method);

				defGenericContext.MethodParameters.Clear();
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

				defGenericContext.TypeParameters.Add(parameter);
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

				defGenericContext.MethodParameters.Add(parameter);
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

		private MethodBody ExtractBody(Cci.IMethodBody cciBody, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			// TODO: Is not a good idea to extract all method bodies defined
			// in an assembly when loading it. It would be better to delay 
			// the extraction so it take place when it is actually needed,
			// like on demand in a lazy fashion.
			var codeProvider = new CodeProvider(this, sourceLocationProvider);
			var result = codeProvider.ExtractBody(cciBody);
			return result;
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
				var type = ExtractType(parameterref.Type, parameterref.IsByReference);
				var parameter = new MethodParameterReference(parameterref.Index, type);

				parameter.Kind = GetMethodParameterKind(parameterref);
				dest.Add(parameter);
			}
		}

		private void ExtractParameters(IList<MethodParameter> dest, IEnumerable<Cci.IParameterDefinition> source)
		{
			foreach (var parameterdef in source)
			{
				var name = parameterdef.Name.Value;
				var type = ExtractType(parameterdef.Type, parameterdef.IsByReference);
				var parameter = new MethodParameter(parameterdef.Index, name, type);

				ExtractAttributes(parameter.Attributes, parameterdef.Attributes);

				if (parameterdef.HasDefaultValue)
				{
					parameter.DefaultValue = ExtractConstant(parameterdef.Constant);
				}

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

		private void ExtractFields(TypeDefinition containingType, IList<FieldDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
		{
			foreach (var fielddef in source)
			{
				var name = fielddef.Name.Value;
				var type = ExtractType(fielddef.Type);
				var field = new FieldDefinition(name, type);

				ExtractAttributes(field.Attributes, fielddef.Attributes);

				field.Visibility = ExtractVisibilityKind(fielddef.Visibility);
				field.IsStatic = fielddef.IsStatic;
				field.ContainingType = containingType;
				dest.Add(field);
			}
		}

		private static VisibilityKind ExtractVisibilityKind(Cci.TypeMemberVisibility visibility)
		{
			switch (visibility)
			{
				case Cci.TypeMemberVisibility.Public: return VisibilityKind.Public;
				case Cci.TypeMemberVisibility.Private: return VisibilityKind.Private;
				case Cci.TypeMemberVisibility.Family: return VisibilityKind.Protected;
				case Cci.TypeMemberVisibility.Assembly: return VisibilityKind.Internal;
				case Cci.TypeMemberVisibility.FamilyOrAssembly: return VisibilityKind.Protected | VisibilityKind.Internal;
				default: return VisibilityKind.Unknown;
			}
		}
	}
}
