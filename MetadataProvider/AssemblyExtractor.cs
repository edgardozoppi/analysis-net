using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Model.ThreeAddressCode.Values;
using Model.Types;
using SR = System.Reflection;
using SRM = System.Reflection.Metadata;

namespace MetadataProvider
{
	internal class AssemblyExtractor
	{
		private IDictionary<SRM.TypeDefinitionHandle, ClassDefinition> definedTypes;

		private SRM.MetadataReader metadata;
		private GenericContext genericContext;
		private SignatureTypeProvider signatureTypeProvider;
		private Assembly assembly;
		private Namespace currentNamespace;
		private ClassDefinition currentType;

		public AssemblyExtractor(SRM.MetadataReader metadata)
		{
			this.metadata = metadata;
			this.definedTypes = new Dictionary<SRM.TypeDefinitionHandle, ClassDefinition>();
			this.genericContext = new GenericContext();
			this.signatureTypeProvider = new SignatureTypeProvider(this);
		}

		public ClassDefinition GetDefinedType(SRM.TypeDefinitionHandle handle)
		{
			ClassDefinition result;
			var ok = definedTypes.TryGetValue(handle, out result);

			if (!ok)
			{
				var typedef = metadata.GetTypeDefinition(handle);
				var name = metadata.GetString(typedef.Name);
				name = GetGenericName(name);

				result = new ClassDefinition(name);
				definedTypes.Add(handle, result);
			}

			return result;
		}

		public Assembly Extract()
		{
			var assemblydef = metadata.GetAssemblyDefinition();
			var name = metadata.GetString(assemblydef.Name);
			assembly = new Assembly(name);

			foreach (var handle in metadata.AssemblyReferences)
			{
				var referencedef = metadata.GetAssemblyReference(handle);
				name = metadata.GetString(referencedef.Name);
				var reference = new AssemblyReference(name);

				assembly.References.Add(reference);
			}

			var namespacedef = metadata.GetNamespaceDefinitionRoot();
			ExtractNamespace(namespacedef);

			return assembly;
		}

		private void ExtractNamespace(SRM.NamespaceDefinitionHandle handle)
		{
			var namespacedef = metadata.GetNamespaceDefinition(handle);
			ExtractNamespace(namespacedef);
		}

		private void ExtractNamespace(SRM.NamespaceDefinition namespacedef)
		{
			var name = metadata.GetString(namespacedef.Name);
			var namespaze = new Namespace(name);

			if (currentNamespace == null)
			{
				assembly.RootNamespace = namespaze;
			}
			else
			{
				currentNamespace.Namespaces.Add(namespaze);
			}

			namespaze.ContainingAssembly = assembly;
			namespaze.ContainingNamespace = currentNamespace;
			currentNamespace = namespaze;

			foreach (var handle in namespacedef.NamespaceDefinitions)
			{
				ExtractNamespace(handle);
			}

			foreach (var handle in namespacedef.TypeDefinitions)
			{
				ExtractType(handle);
			}

			currentNamespace = currentNamespace.ContainingNamespace;
		}

		private void ExtractType(SRM.TypeDefinitionHandle typedefHandle)
		{
			var typedef = metadata.GetTypeDefinition(typedefHandle);
			var type = GetDefinedType(typedefHandle);

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

			foreach (var handle in typedef.GetGenericParameters())
			{
				ExtractGenericParameter(GenericParameterKind.Type, type, handle);
			}

			foreach (var handle in typedef.GetFields())
			{
				ExtractField(handle);
			}

			foreach (var handle in typedef.GetMethods())
			{
				ExtractMethod(handle);
			}

			genericContext.TypeParameters.Clear();

			foreach (var handle in typedef.GetNestedTypes())
			{
				ExtractType(handle);
			}

			currentType = currentType.ContainingType as ClassDefinition;
		}

		private void ExtractGenericParameter(GenericParameterKind parameterKind, IGenericDefinition genericContainer, SRM.GenericParameterHandle handle)
		{
			var genericParameterdef = metadata.GetGenericParameter(handle);
			var typeKind = TypeKind.Unknown;

			if (genericParameterdef.Attributes.HasFlag(SR.GenericParameterAttributes.ReferenceTypeConstraint))
			{
				typeKind = TypeKind.ReferenceType;
			}

			if (genericParameterdef.Attributes.HasFlag(SR.GenericParameterAttributes.NotNullableValueTypeConstraint))
			{
				typeKind = TypeKind.ValueType;
			}

			var name = metadata.GetString(genericParameterdef.Name);
			var genericParameter = new GenericParameter(parameterKind, (ushort)genericParameterdef.Index, name, typeKind)
			{
				GenericContainer = genericContainer
			};

			if (parameterKind == GenericParameterKind.Type)
			{
				genericContext.TypeParameters.Add(genericParameter);
			}
			else if (parameterKind == GenericParameterKind.Method)
			{
				genericContext.MethodParameters.Add(genericParameter);
			}
			else
			{
				throw parameterKind.ToUnknownValueException();
			}

			genericContainer.GenericParameters.Add(genericParameter);
		}

		private void ExtractField(SRM.FieldDefinitionHandle handle)
		{
			var fielddef = metadata.GetFieldDefinition(handle);
			var name = metadata.GetString(fielddef.Name);
			var type = fielddef.DecodeSignature(signatureTypeProvider, genericContext);
			var field = new FieldDefinition(name, type)
			{
				ContainingType = currentType,
				IsStatic = fielddef.Attributes.HasFlag(SR.FieldAttributes.Static)
			};

			currentType.Fields.Add(field);
		}

		private void ExtractMethod(SRM.MethodDefinitionHandle methoddefHandle)
		{
			var methoddef = metadata.GetMethodDefinition(methoddefHandle);
			var name = metadata.GetString(methoddef.Name);
			var method = new MethodDefinition(name, null)
			{
				ContainingType = currentType,
				IsStatic = methoddef.Attributes.HasFlag(SR.MethodAttributes.Static),
				IsAbstract = methoddef.Attributes.HasFlag(SR.MethodAttributes.Abstract),
				IsVirtual = methoddef.Attributes.HasFlag(SR.MethodAttributes.Virtual),
				IsConstructor = name.EndsWith(".ctor")
			};

			currentType.Methods.Add(method);

			foreach (var handle in methoddef.GetGenericParameters())
			{
				ExtractGenericParameter(GenericParameterKind.Method, method, handle);
			}

			var signature = methoddef.DecodeSignature(signatureTypeProvider, genericContext);
			method.ReturnType = signature.ReturnType;

			//TODO: agregar body, etc.

			foreach (var handle in methoddef.GetParameters())
			{
				ExtractParameter(method, signature, handle);
			}

			genericContext.MethodParameters.Clear();
		}

		private void ExtractParameter(MethodDefinition method, SRM.MethodSignature<IType> signature, SRM.ParameterHandle handle)
		{
			var parameterdef = metadata.GetParameter(handle);
			var type = signature.ParameterTypes[parameterdef.SequenceNumber - 1];
			var parameterName = metadata.GetString(parameterdef.Name);

			var parameter = new MethodParameter((ushort)parameterdef.SequenceNumber, parameterName, type)
			{
				Kind = GetParameterKind(parameterdef.Attributes, type),
				DefaultValue = ExtractParameterDefaultValue(parameterdef, type)
			};

			method.Parameters.Add(parameter);
		}

		private static MethodParameterKind GetParameterKind(SR.ParameterAttributes attributes, IType type)
		{
			var result = MethodParameterKind.In;
			var isOut = attributes.HasFlag(SR.ParameterAttributes.Out);

			if (isOut)
			{
				result = MethodParameterKind.Out;
			}
			else if (type.IsPointer())
			{
				result = MethodParameterKind.Ref;
			}

			return result;
		}

		private Constant ExtractParameterDefaultValue(SRM.Parameter parameterdef, IType type)
		{
			Constant result = null;
			var hasDefaultValue = parameterdef.Attributes.HasFlag(SR.ParameterAttributes.HasDefault);

			if (hasDefaultValue)
			{
				var defaultValueHandle = parameterdef.GetDefaultValue();
				var constant = metadata.GetConstant(defaultValueHandle);
				var reader = metadata.GetBlobReader(constant.Value);
				var value = reader.ReadConstant(constant.TypeCode);

				result = new Constant(value)
				{
					Type = type
				};
			}

			return result;
		}

		private static string GetGenericName(string name)
		{
			var start = name.LastIndexOf('`');

			if (start > -1)
			{
				name = name.Remove(start);
			}

			return name;
		}
	}
}