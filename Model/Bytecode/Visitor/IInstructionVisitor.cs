// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Bytecode.Visitor
{
	public interface IInstructionVisitor
	{
		void Visit(IInstructionContainer container);
		void Visit(Instruction instruction);
		void Visit(BasicInstruction instruction);
		void Visit(LoadInstruction instruction);
		void Visit(LoadFieldInstruction instruction);
		void Visit(LoadMethodAddressInstruction instruction);
		void Visit(StoreInstruction instruction);
		void Visit(StoreFieldInstruction instruction);
		void Visit(ConvertInstruction instruction);
		void Visit(BranchInstruction instruction);
		void Visit(SwitchInstruction instruction);
		void Visit(SizeofInstruction instruction);
		void Visit(LoadTokenInstruction instruction);
		void Visit(MethodCallInstruction instruction);
		void Visit(IndirectMethodCallInstruction instruction);
		void Visit(CreateObjectInstruction instruction);
		void Visit(CreateArrayInstruction instruction);
	}
}
