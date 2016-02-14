using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Instructions;

namespace Model.ThreeAddressCode.Visitor
{
	public interface IInstructionVisitor
	{
		void Visit(IInstructionContainer container);
		void Visit(Instruction instruction);
		void Visit(DefinitionInstruction instruction);
		void Visit(BinaryInstruction instruction);
		void Visit(UnaryInstruction instruction);
		void Visit(LoadInstruction instruction);
		void Visit(StoreInstruction instruction);
		void Visit(NopInstruction instruction);
		void Visit(BreakpointInstruction instruction);
		void Visit(TryInstruction instruction);
		void Visit(FaultInstruction instruction);
		void Visit(FinallyInstruction instruction);
		void Visit(CatchInstruction instruction);
		void Visit(ConvertInstruction instruction);
		void Visit(ReturnInstruction instruction);
		void Visit(ThrowInstruction instruction);
		void Visit(BranchInstruction instruction);
		void Visit(ExceptionalBranchInstruction instruction);
		void Visit(UnconditionalBranchInstruction instruction);
		void Visit(ConditionalBranchInstruction instruction);
		void Visit(SwitchInstruction instruction);
		void Visit(SizeofInstruction instruction);
		void Visit(LoadTokenInstruction instruction);
		void Visit(MethodCallInstruction instruction);
		void Visit(IndirectMethodCallInstruction instruction);
		void Visit(CreateObjectInstruction instruction);
		void Visit(CopyMemoryInstruction instruction);
		void Visit(LocalAllocationInstruction instruction);
		void Visit(InitializeMemoryInstruction instruction);
		void Visit(InitializeObjectInstruction instruction);
		void Visit(CopyObjectInstruction instruction);
		void Visit(CreateArrayInstruction instruction);
		void Visit(PhiInstruction instruction);
	}
}
