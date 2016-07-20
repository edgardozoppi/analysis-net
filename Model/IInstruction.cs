// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public interface IInstructionContainer
	{
		IList<IInstruction> Instructions { get; }
	}

	public interface IInstruction : IVariableContainer
	{
		uint Offset { get; set; }
		string Label { get; set; }
		ISet<IVariable> ModifiedVariables { get; }
		ISet<IVariable> UsedVariables { get; }
	}
}
