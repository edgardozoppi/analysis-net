// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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
		public IList<ProtectedBlock> ExceptionInformation { get; private set; }
		public IList<Instruction> Instructions { get; private set; }
		public IList<IVariable> Parameters { get; private set; }
		public ISet<IVariable> Variables { get; private set; }

		public MethodBody(IMethodDefinition methodDefinition)
		{
			this.MethodDefinition = methodDefinition;
			this.ExceptionInformation = new List<ProtectedBlock>();
			this.Instructions = new List<Instruction>();
			this.Parameters = new List<IVariable>();
			this.Variables = new HashSet<IVariable>();
		}

		public void UpdateVariables()
		{
			this.Variables.Clear();
			//this.Variables.UnionWith(this.Parameters);

			// TODO: SSA is not inserting phi instructions into method's body instructions collection.
			foreach (var instruction in this.Instructions)
			{
				this.Variables.UnionWith(instruction.Variables);
			}
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			var header = MemberHelper.GetMethodSignature(this.MethodDefinition, NameFormattingOptions.Signature | NameFormattingOptions.ParameterName);

			result.AppendLine(header);

			foreach (var variable in this.Variables)
			{
				var type = "unknown";

				if (variable.Type != null)
				{
					type = TypeHelper.GetTypeName(variable.Type);
				}

				result.AppendFormat("  {0} {1};", type, variable.Name);
				result.AppendLine();
			}

			result.AppendLine();

			foreach (var instruction in this.Instructions)
			{
				result.Append("  ");
				result.Append(instruction);
				result.AppendLine();
			}

			foreach (var handler in this.ExceptionInformation)
			{
				result.AppendLine();
				result.Append(handler);
			}

			return result.ToString();
		}
	}
}
