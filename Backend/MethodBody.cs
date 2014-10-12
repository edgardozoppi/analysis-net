using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend.ThreeAddressCode;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;

namespace Backend
{
	public class MethodBody : IInstructionContainer
	{
		public IMethodDefinition MethodDefinition { get; private set; }
		public IList<ProtectedBlock> ProtectedBlocks { get; private set; }
		public IList<Instruction> Instructions { get; private set; }
		public IList<IVariable> Parameters { get; private set; }
		public ISet<IVariable> Variables { get; private set; }

		public MethodBody(IMethodDefinition methodDefinition)
		{
			this.MethodDefinition = methodDefinition;
			this.ProtectedBlocks = new List<ProtectedBlock>();
			this.Instructions = new List<Instruction>();
			this.Parameters = new List<IVariable>();
			this.Variables = new HashSet<IVariable>();
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			var header = MemberHelper.GetMethodSignature(this.MethodDefinition, NameFormattingOptions.Signature | NameFormattingOptions.ParameterName);

			result.AppendLine(header);

			foreach (var instruction in this.Instructions)
			{
				result.Append("  ");
				result.Append(instruction);
				result.AppendLine();
			}

			foreach (var handler in this.ProtectedBlocks)
			{
				result.AppendLine();
				result.Append(handler);
			}

			return result.ToString();
		}
	}
}
