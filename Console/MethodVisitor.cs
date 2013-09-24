using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using Backend;

namespace Console
{
	class MethodVisitor : MetadataRewriter
	{
		public MethodVisitor(IMetadataHost host)
			: base(host)
		{
		}

		public override IMethodDefinition Rewrite(IMethodDefinition methodDefinition)
		{
			var disassembler = new Disassembler(methodDefinition);
			var methodBody = disassembler.Execute();

			System.Console.WriteLine(methodBody);
			return base.Rewrite(methodDefinition);
		}
	}
}
