using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend.Instructions;

namespace Backend
{
	public class MethodBody
	{
		public IMethodDefinition MethodDefinition { get; private set; }
		public IList<Instruction> Instructions { get; private set; }

		public MethodBody(IMethodDefinition methodDefinition)
		{
			this.MethodDefinition = methodDefinition;
			this.Instructions = new List<Instruction>();
		}

		public override string ToString()
		{
			var header = MemberHelper.GetMethodSignature(this.MethodDefinition, NameFormattingOptions.Signature | NameFormattingOptions.ParameterName);
			var body = string.Join("\n\t", this.Instructions);

			return string.Format("{0}\n\t{1}", header, body);
		}
	}
}
