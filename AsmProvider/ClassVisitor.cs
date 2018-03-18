using Model;
using Model.ThreeAddressCode.Values;
using Model.Types;
using System.Collections.Generic;
using AsmCore = Asm.Core;

namespace AsmProvider
{
	internal class ClassVisitor : AsmCore.ClassVisitor
	{
		private AssemblyContext context;
		private TypeDefinition clazz;

		public ClassVisitor(AssemblyContext context)
			: base(AsmCore.Opcodes.ASM6)
		{
			this.context = context;
		}

		public override AsmCore.ModuleVisitor VisitModule(string name, int access, string version)
		{
			return new ModuleVisitor();
		}

		public override void VisitInnerClass(string name, string outerName, string innerName, int access)
		{
		}

		public override void VisitOuterClass(string owner, string name, string descriptor)
		{
		}

		public override void Visit(int version, int access, string name, string signature, string superName, string[] interfaces)
		{
			var qualifiedName = Helper.ParseTypeDefinitionName(name);
			var namezpace = Helper.GetOrCreateNamespace(context.Assembly, qualifiedName.Namespace);
			var isInterface = Helper.HasFlag(access, AsmCore.Opcodes.ACC_INTERFACE);
			var kind = isInterface ? TypeDefinitionKind.Interface : TypeDefinitionKind.Class;

			var superQualifiedName = Helper.ParseTypeReferenceName(superName);

			clazz = new TypeDefinition(qualifiedName.Name, TypeKind.ReferenceType, kind)
			{
				ContainingAssembly = context.Assembly,
				ContainingNamespace = namezpace,
				Base = Helper.CreateTypeReference(context.Host, context.Assembly, superQualifiedName)
			};

			context.DefinedTypes.Add(name, clazz);

			AddInterfaces(interfaces);
			AddToParent(qualifiedName, namezpace);
			AddNestedTypes(name);
		}

		private void AddInterfaces(string[] interfaces)
		{
			// Add implemented interfaces.
			foreach (var interfaceName in interfaces)
			{
				var interfaceQualifiedName = Helper.ParseTypeReferenceName(interfaceName);
				var type = Helper.CreateTypeReference(context.Host, context.Assembly, interfaceQualifiedName);

				clazz.Interfaces.Add(type);
			}
		}

		private void AddToParent(TypeDefinitionName qualifiedName, Namespace namezpace)
		{
			// Add this type to the corresponding parent.
			if (qualifiedName.IsNested)
			{
				// This type is nested.
				TypeDefinition type;
				var ok = context.DefinedTypes.TryGetValue(qualifiedName.ParentFullName, out type);

				if (ok)
				{
					// The parent of this type was already created.
					var parentType = (ITypeDefinitionContainer)type;

					clazz.ContainingType = type;
					parentType.Types.Add(clazz);
				}
				else
				{
					// The parent of this type was not yet created,
					// so it will be added later.
					context.AddNestedType(qualifiedName.ParentFullName, clazz);
				}
			}
			else
			{
				// This type is not nested, so it is a direct child of the namespace.
				namezpace.Types.Add(clazz);
			}
		}

		private void AddNestedTypes(string parentFullName)
		{
			var parentType = clazz as ITypeDefinitionContainer;
			if (parentType == null) return;

			// Add previously created nested types.
			ISet<TypeDefinition> nestedTypes;
			var ok = context.NestedTypes.TryGetValue(parentFullName, out nestedTypes);

			if (ok)
			{
				foreach (var nestedType in nestedTypes)
				{
					nestedType.ContainingType = clazz;
					parentType.Types.Add(nestedType);
				}

				context.NestedTypes.Remove(parentFullName);
			}
		}

		public override AsmCore.FieldVisitor VisitField(int access, string name, string descriptor, string signature, object value)
		{
			var type = Helper.ParseTypeDescriptor(context.Host, context.Assembly, descriptor);

			var field = new FieldDefinition(name, type)
			{
				ContainingType = clazz,
				IsStatic = Helper.HasFlag(access, AsmCore.Opcodes.ACC_STATIC)
			};

			if (value != null)
			{
				field.Value = new Constant(value)
				{
					Type = type
				};
			}

			clazz.Fields.Add(field);

			return null;
		}

		public override AsmCore.MethodVisitor VisitMethod(int access, string name, string descriptor, string signature, string[] exceptions)
		{
			var prototype = Helper.ParseMethodDescriptor(context.Host, context.Assembly, descriptor);

			var method = new MethodDefinition(name, prototype.Return)
			{
				ContainingType = clazz,
				IsStatic = Helper.HasFlag(access, AsmCore.Opcodes.ACC_STATIC),
				IsAbstract = Helper.HasFlag(access, AsmCore.Opcodes.ACC_ABSTRACT),
				IsVirtual = !Helper.HasFlag(access, AsmCore.Opcodes.ACC_FINAL),
				IsExternal = Helper.HasAnyFlag(access, AsmCore.Opcodes.ACC_BRIDGE, AsmCore.Opcodes.ACC_SYNTHETIC)
			};

			for (ushort i = 0; i < prototype.Parameters.Count; ++i)
			{
				var type = prototype.Parameters[i];
				var defaultName = string.Format("p{0}", i + 1);
				var parameter = new MethodParameter(i, defaultName, type);
				method.Parameters.Add(parameter);
			}

			clazz.Methods.Add(method);

			return new MethodVisitor(method);
		}
	}

	internal class ModuleVisitor : AsmCore.ModuleVisitor
	{
		public ModuleVisitor()
			: base(AsmCore.Opcodes.ASM6)
		{
		}

		public override void VisitMainClass(string mainClass)
		{
		}

		public override void VisitPackage(string packaze)
		{
		}
	}

	internal class MethodVisitor : AsmCore.MethodVisitor
	{
		private MethodDefinition method;

		public MethodVisitor(MethodDefinition method)
			 : base(AsmCore.Opcodes.ASM6)
		{
			this.method = method;
		}

		public override void VisitLocalVariable(string name, string descriptor, string signature, AsmCore.Label start, AsmCore.Label end, int index)
		{
			if (!method.IsStatic)
			{
				// Make receiver local variable "this"
				// of instance methods to have index -1.
				index--;
			}

			if (index >= 0 && index < method.Parameters.Count)
			{
				// Fill parameter names.
				var parameter = method.Parameters[index];
				parameter.Name = name;
			}
			else
			{
				//var type = Helper.ParseTypeDescriptor(descriptor);
			}
		}
	}
}