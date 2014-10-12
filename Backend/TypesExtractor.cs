using Cci = Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode;
using Backend.ThreeAddressCode.Types;
using Backend.ThreeAddressCode.Values;

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
				if (typedef.IsClass)
				{
					this.ExtractClass(typedef);
				}
				else if (typedef.IsInterface)
				{
					this.ExtractInterface(typedef);
				}
				else if (typedef.IsStruct)
				{
					this.ExtractStruct(typedef);
				}
				else if (typedef.IsEnum)
				{
					this.ExtractEnum(typedef);
				}

				base.TraverseChildren(typedef);
			}

			private void ExtractEnum(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new EnumDefinition(name);

				type.UnderlayingType = TypesExtractor.ExtractType(typedef.UnderlyingType) as BasicType;
				this.ExtractConstants(type.Constants, typedef.Fields);

				types.Add(name, type);
			}

			private void ExtractConstants(IDictionary<string, Constant> dest, IEnumerable<Cci.IFieldDefinition> source)
			{
				source = source.Skip(1);

				foreach (var constdef in source)
				{
					var name = constdef.Name.Value;
					var value = constdef.CompileTimeValue.Value;
					var constant = new Constant(value);

					dest.Add(name, constant);
				}
			}

			private void ExtractInterface(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new InterfaceDefinition(name);

				this.ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				this.ExtractInterfaces(type.Interfaces, typedef.BaseClasses);
				this.ExtractMethods(type.Methods, typedef.Methods);

				types.Add(name, type);
			}

			private void ExtractClass(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new ClassDefinition(name);
				Cci.ITypeReference basedef = Cci.TypeHelper.BaseClass(typedef);

				if (basedef == null)
				{
					basedef = host.PlatformType.SystemObject;
				}

				type.Base = TypesExtractor.ExtractType(basedef) as BasicType;

				this.ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				this.ExtractInterfaces(type.Interfaces, typedef.BaseClasses);
				this.ExtractFields(type.Fields, typedef.Fields);
				this.ExtractMethods(type.Methods, typedef.Methods);

				types.Add(name, type);
			}

			private void ExtractStruct(Cci.INamedTypeDefinition typedef)
			{
				var name = typedef.Name.Value;
				var type = new StructDefinition(name);

				this.ExtractGenericParameters(type.GenericParameters, typedef.GenericParameters);
				this.ExtractFields(type.Fields, typedef.Fields);
				this.ExtractMethods(type.Methods, typedef.Methods);

				types.Add(name, type);
			}

			private void ExtractMethods(IDictionary<string, MethodDefinition> dest, IEnumerable<Cci.IMethodDefinition> source)
			{
				foreach (var methoddef in source)
				{
					var name = methoddef.Name.Value;
					var type = TypesExtractor.ExtractType(methoddef.Type);
					var method = new MethodDefinition(name, type);

					this.ExtractGenericParameters(method.GenericParameters, methoddef.GenericParameters);
					this.ExtractParameters(method.Parameters, methoddef.Parameters);

					method.IsStatic = methoddef.IsStatic;
					method.IsConstructor = methoddef.IsConstructor;
					dest.Add(name, method);
				}
			}

			private void ExtractGenericParameters(IList<TypeVariable> dest, IEnumerable<Cci.IGenericParameter> source)
			{
				foreach (var parameterdef in source)
				{
					var name = parameterdef.Name.Value;
					var parameter = new TypeVariable(name);

					dest.Add(parameter);
				}
			}

			private void ExtractInterfaces(IList<BasicType> dest, IEnumerable<Cci.ITypeReference> source)
			{
				foreach (var interfaceref in source)
				{
					var type = TypesExtractor.ExtractType(interfaceref) as BasicType;

					dest.Add(type);
				}
			}

			private void ExtractParameters(IList<IVariable> dest, IEnumerable<Cci.IParameterDefinition> source)
			{
				foreach (var parameterdef in source)
				{
					var name = parameterdef.Name.Value;
					var type = TypesExtractor.ExtractType(parameterdef.Type);
					var parameter = new LocalVariable(name);

					dest.Add(parameter);
				}
			}

			private void ExtractFields(IDictionary<string, FieldDefinition> dest, IEnumerable<Cci.IFieldDefinition> source)
			{
				foreach (var fielddef in source)
				{
					var name = fielddef.Name.Value;
					var type = TypesExtractor.ExtractType(fielddef.Type);
					var field = new FieldDefinition(name, type);

					field.IsStatic = fielddef.IsStatic;
					dest.Add(name, field);
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
				var elements = TypesExtractor.ExtractType(atyperef.ElementType);
				var type = new ArrayType(elements);

				result = type;
			}
			else if (typeref is Cci.IPointerTypeReference)
			{
				var ptyperef = typeref as Cci.IPointerTypeReference;
				var target = TypesExtractor.ExtractType(ptyperef.TargetType);
				var type = new PointerType(target);

				result = type;
			}
			else if (typeref is Cci.IGenericParameterReference)
			{
				var gptyperef = typeref as Cci.IGenericParameterReference;
				var name = Cci.TypeHelper.GetTypeName(gptyperef, Cci.NameFormattingOptions.OmitContainingType | Cci.NameFormattingOptions.PreserveSpecialNames);
				var type = new TypeVariable(name);

				result = type;
			}
			else if (typeref is Cci.IGenericTypeInstanceReference)
			{
				var gtyperef = typeref as Cci.IGenericTypeInstanceReference;
				var name = Cci.TypeHelper.GetTypeName(gtyperef, Cci.NameFormattingOptions.OmitContainingType | Cci.NameFormattingOptions.PreserveSpecialNames);
				var type = new BasicType(name);

				TypesExtractor.ExtractGenericType(type, gtyperef);
				result = type;
			}
			else if (typeref is Cci.INamedTypeReference)
			{
				var ntyperef = typeref as Cci.INamedTypeReference;
				var name = Cci.TypeHelper.GetTypeName(ntyperef, Cci.NameFormattingOptions.OmitContainingType | Cci.NameFormattingOptions.PreserveSpecialNames);
				var type = new BasicType(name);

				result = type;
			}

			return result;
		}

		private static void ExtractGenericType(BasicType type, Cci.IGenericTypeInstanceReference typeref)
		{
			foreach (var argumentref in typeref.GenericArguments)
			{
				var typearg = TypesExtractor.ExtractType(argumentref);
				type.GenericArguments.Add(typearg);
			}
		}
	}
}
