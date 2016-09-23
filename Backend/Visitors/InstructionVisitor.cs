// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode.Instructions;

namespace Backend.Visitors
{
	public abstract class InstructionVisitor : IInstructionVisitor
	{
		public virtual void Visit(IInstructionContainer container)
		{
			foreach (var instruction in container.Instructions)
			{
				instruction.Accept(this);
			}
		}

		public virtual void Default(Instruction instruction) { }
		public virtual void Visit(Instruction instruction) { } // Default(instruction); }
		public virtual void Visit(DefinitionInstruction instruction) { } // { Default(instruction); }
		public virtual void Visit(BinaryInstruction instruction) { Default(instruction); }
		public virtual void Visit(UnaryInstruction instruction) { Default(instruction); }
		public virtual void Visit(LoadInstruction instruction) { Default(instruction); }
		public virtual void Visit(StoreInstruction instruction) { Default(instruction); }
		public virtual void Visit(NopInstruction instruction) { Default(instruction); }
		public virtual void Visit(BreakpointInstruction instruction) { Default(instruction); }
		public virtual void Visit(TryInstruction instruction) { Default(instruction); }
		public virtual void Visit(FaultInstruction instruction) { Default(instruction); }
		public virtual void Visit(FinallyInstruction instruction) { Default(instruction); }
		public virtual void Visit(CatchInstruction instruction) { Default(instruction); }
		public virtual void Visit(ConvertInstruction instruction) { Default(instruction); }
		public virtual void Visit(ReturnInstruction instruction) { Default(instruction); }
		public virtual void Visit(ThrowInstruction instruction) { Default(instruction); }
		public virtual void Visit(BranchInstruction instruction) { } // { Default(instruction); }
		public virtual void Visit(ExceptionalBranchInstruction instruction) { Default(instruction); }
		public virtual void Visit(UnconditionalBranchInstruction instruction) { Default(instruction); }
		public virtual void Visit(ConditionalBranchInstruction instruction) { Default(instruction); }
		public virtual void Visit(SwitchInstruction instruction) { Default(instruction); }
		public virtual void Visit(SizeofInstruction instruction) { Default(instruction); }
		public virtual void Visit(LoadTokenInstruction instruction) { Default(instruction); }
		public virtual void Visit(MethodCallInstruction instruction) { Default(instruction); }
		public virtual void Visit(IndirectMethodCallInstruction instruction) { Default(instruction); }
		public virtual void Visit(CreateObjectInstruction instruction) { Default(instruction); }
		public virtual void Visit(CopyMemoryInstruction instruction) { Default(instruction); }
		public virtual void Visit(LocalAllocationInstruction instruction) { Default(instruction); }
		public virtual void Visit(InitializeMemoryInstruction instruction) { Default(instruction); }
		public virtual void Visit(InitializeObjectInstruction instruction) { Default(instruction); }
		public virtual void Visit(CopyObjectInstruction instruction) { Default(instruction); }
		public virtual void Visit(CreateArrayInstruction instruction) { Default(instruction); }
		public virtual void Visit(PhiInstruction instruction) { Default(instruction); }
	}
}
