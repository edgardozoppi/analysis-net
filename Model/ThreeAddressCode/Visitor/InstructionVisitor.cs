// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Instructions;

namespace Model.ThreeAddressCode.Visitor
{
	public abstract class InstructionVisitor : IInstructionVisitor
	{
		public virtual void Visit(IInstructionContainer container)
		{
			foreach (var instruction in container.Instructions)
			{
				var tacInstruction = instruction as Instruction;
				tacInstruction.Accept(this);
			}
		}

		public virtual void Visit(Instruction instruction) { }
		public virtual void Visit(DefinitionInstruction instruction) { }
		public virtual void Visit(BinaryInstruction instruction) { }
		public virtual void Visit(UnaryInstruction instruction) { }
		public virtual void Visit(LoadInstruction instruction) { }
		public virtual void Visit(StoreInstruction instruction) { }
		public virtual void Visit(NopInstruction instruction) { }
		public virtual void Visit(BreakpointInstruction instruction) { }
		public virtual void Visit(TryInstruction instruction) { }
		public virtual void Visit(FaultInstruction instruction) { }
		public virtual void Visit(FinallyInstruction instruction) { }
        public virtual void Visit(FilterInstruction instruction) { }
        public virtual void Visit(CatchInstruction instruction) { }
		public virtual void Visit(ConvertInstruction instruction) { }
		public virtual void Visit(ReturnInstruction instruction) { }
		public virtual void Visit(ThrowInstruction instruction) { }
		public virtual void Visit(BranchInstruction instruction) { }
		public virtual void Visit(ExceptionalBranchInstruction instruction) { }
		public virtual void Visit(UnconditionalBranchInstruction instruction) { }
		public virtual void Visit(ConditionalBranchInstruction instruction) { }
		public virtual void Visit(SwitchInstruction instruction) { }
		public virtual void Visit(SizeofInstruction instruction) { }
		public virtual void Visit(LoadTokenInstruction instruction) { }
		public virtual void Visit(MethodCallInstruction instruction) { }
		public virtual void Visit(IndirectMethodCallInstruction instruction) { }
		public virtual void Visit(CreateObjectInstruction instruction) { }
		public virtual void Visit(CopyMemoryInstruction instruction) { }
		public virtual void Visit(LocalAllocationInstruction instruction) { }
		public virtual void Visit(InitializeMemoryInstruction instruction) { }
		public virtual void Visit(InitializeObjectInstruction instruction) { }
		public virtual void Visit(CopyObjectInstruction instruction) { }
		public virtual void Visit(CreateArrayInstruction instruction) { }
		public virtual void Visit(PhiInstruction instruction) { }
	}
}
