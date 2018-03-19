using Model;
using Model.Bytecode;
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

			return new MethodVisitor(context, method);
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
		private AssemblyContext context;
		private MethodDefinition method;
		private uint offset;

		public MethodVisitor(AssemblyContext context, MethodDefinition method)
			: base(AsmCore.Opcodes.ASM6)
		{
			this.context = context;
			this.method = method;
		}

		public override void VisitCode()
		{
			method.Body = new MethodBody(MethodBodyKind.Bytecode);
			offset = 0;
		}

		public override void VisitMaxs(int maxStack, int maxLocals)
		{
			method.Body.MaxStack = (ushort)maxStack;
		}

		public override void VisitParameter(string name, int access)
		{
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

			var isParameter = index < method.Parameters.Count;
			var variable = new LocalVariable(name, isParameter)
			{
				Type = Helper.ParseTypeDescriptor(context.Host, context.Assembly, descriptor)
			};

			if (isParameter)
			{
				method.Body.Parameters.Add(variable);
			}
			else
			{
				method.Body.LocalVariables.Add(variable);
			}
		}

		public override void VisitInsn(int opcode)
		{
			IInstruction instruction = null;

			switch (opcode)
			{
				case AsmCore.Opcodes.NOP:
				case AsmCore.Opcodes.IALOAD:
				case AsmCore.Opcodes.LALOAD:
				case AsmCore.Opcodes.FALOAD:
				case AsmCore.Opcodes.DALOAD:
				case AsmCore.Opcodes.AALOAD:
				case AsmCore.Opcodes.BALOAD:
				case AsmCore.Opcodes.CALOAD:
				case AsmCore.Opcodes.SALOAD:
				case AsmCore.Opcodes.IASTORE:
				case AsmCore.Opcodes.LASTORE:
				case AsmCore.Opcodes.FASTORE:
				case AsmCore.Opcodes.DASTORE:
				case AsmCore.Opcodes.AASTORE:
				case AsmCore.Opcodes.BASTORE:
				case AsmCore.Opcodes.CASTORE:
				case AsmCore.Opcodes.SASTORE:
				case AsmCore.Opcodes.POP:
				case AsmCore.Opcodes.POP2:
				case AsmCore.Opcodes.DUP:
				case AsmCore.Opcodes.DUP_X1:
				case AsmCore.Opcodes.DUP_X2:
				case AsmCore.Opcodes.DUP2:
				case AsmCore.Opcodes.DUP2_X1:
				case AsmCore.Opcodes.DUP2_X2:
				//case AsmCore.Opcodes.SWAP:		
				case AsmCore.Opcodes.IADD:
				case AsmCore.Opcodes.LADD:
				case AsmCore.Opcodes.FADD:
				case AsmCore.Opcodes.DADD:
				case AsmCore.Opcodes.ISUB:
				case AsmCore.Opcodes.LSUB:
				case AsmCore.Opcodes.FSUB:
				case AsmCore.Opcodes.DSUB:
				case AsmCore.Opcodes.IMUL:
				case AsmCore.Opcodes.LMUL:
				case AsmCore.Opcodes.FMUL:
				case AsmCore.Opcodes.DMUL:
				case AsmCore.Opcodes.IDIV:
				case AsmCore.Opcodes.LDIV:
				case AsmCore.Opcodes.FDIV:
				case AsmCore.Opcodes.DDIV:
				case AsmCore.Opcodes.IREM:
				case AsmCore.Opcodes.LREM:
				case AsmCore.Opcodes.FREM:
				case AsmCore.Opcodes.DREM:
				case AsmCore.Opcodes.INEG:
				case AsmCore.Opcodes.LNEG:
				case AsmCore.Opcodes.FNEG:
				case AsmCore.Opcodes.DNEG:
				case AsmCore.Opcodes.ISHL:
				case AsmCore.Opcodes.LSHL:
				case AsmCore.Opcodes.ISHR:
				case AsmCore.Opcodes.LSHR:
				case AsmCore.Opcodes.IUSHR:
				case AsmCore.Opcodes.LUSHR:
				case AsmCore.Opcodes.IAND:
				case AsmCore.Opcodes.LAND:
				case AsmCore.Opcodes.IOR:
				case AsmCore.Opcodes.LOR:
				case AsmCore.Opcodes.IXOR:
				case AsmCore.Opcodes.LXOR:
				//case AsmCore.Opcodes.LCMP:
				//case AsmCore.Opcodes.FCMPL:
				//case AsmCore.Opcodes.FCMPG:
				//case AsmCore.Opcodes.DCMPL:
				//case AsmCore.Opcodes.DCMPG:		
				case AsmCore.Opcodes.IRETURN:
				case AsmCore.Opcodes.LRETURN:
				case AsmCore.Opcodes.FRETURN:
				case AsmCore.Opcodes.DRETURN:
				case AsmCore.Opcodes.ARETURN:
				case AsmCore.Opcodes.RETURN:
				case AsmCore.Opcodes.ARRAYLENGTH:
				case AsmCore.Opcodes.ATHROW:
				//case AsmCore.Opcodes.MONITORENTER:
				//case AsmCore.Opcodes.MONITOREXIT:	
					instruction = ProcessBasic(opcode);
					break;

				default:
					//Console.WriteLine("Unknown bytecode: {0}", opcode);
					//throw new UnknownBytecodeException(operation);
					//continue;

					// Quick fix to preserve the offset in case it is a target location of some jump
					// Otherwise it will break the control-flow analysis later.
					instruction = new BasicInstruction(offset++, BasicOperation.Nop);
					break;
			}

			method.Body.Instructions.Add(instruction);
		}

		private IInstruction ProcessBasic(int op)
		{
			var operation = OperationHelper.ToBasicOperation(op);
			//var overflow = OperationHelper.PerformsOverflowCheck(op);
			//var unsigned = OperationHelper.OperandsAreUnsigned(op);

			var instruction = new BasicInstruction(offset++, operation);
			//instruction.OverflowCheck = overflow;
			//instruction.UnsignedOperands = unsigned;
			return instruction;
		}
	}
}