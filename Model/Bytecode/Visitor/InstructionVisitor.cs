// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Bytecode.Visitor
{
	public abstract class InstructionVisitor : IInstructionVisitor
	{
		public virtual void Visit(IInstructionContainer container)
		{
			foreach (var instruction in container.Instructions)
			{
				var bytecodeInstruction = instruction as Instruction;
				bytecodeInstruction.Accept(this);
			}
		}

		public virtual void Visit(Instruction instruction) { }
		public virtual void Visit(BasicInstruction instruction) { }
		public virtual void Visit(LoadInstruction instruction) { }
		public virtual void Visit(LoadFieldInstruction instruction) { }
		public virtual void Visit(LoadMethodAddressInstruction instruction) { }
		public virtual void Visit(StoreInstruction instruction) { }
		public virtual void Visit(StoreFieldInstruction instruction) { }
		public virtual void Visit(ConvertInstruction instruction) { }
		public virtual void Visit(BranchInstruction instruction) { }
		public virtual void Visit(SwitchInstruction instruction) { }
		public virtual void Visit(SizeofInstruction instruction) { }
		public virtual void Visit(LoadTokenInstruction instruction) { }
		public virtual void Visit(MethodCallInstruction instruction) { }
		public virtual void Visit(IndirectMethodCallInstruction instruction) { }
		public virtual void Visit(CreateObjectInstruction instruction) { }
		public virtual void Visit(CreateArrayInstruction instruction) { }
        public virtual void Visit(LoadArrayElementInstruction instruction) { }
        public virtual void Visit(StoreArrayElementInstruction instruction) { }
	}
}
