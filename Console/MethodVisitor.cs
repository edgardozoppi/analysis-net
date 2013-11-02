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
		private ISourceLocationProvider sourceLocationProvider;

		public MethodVisitor(IMetadataHost host, ISourceLocationProvider sourceLocationProvider)
			: base(host)
		{
			this.sourceLocationProvider = sourceLocationProvider;
		}

		public override IMethodDefinition Rewrite(IMethodDefinition methodDefinition)
		{
			var disassembler = new Disassembler(host, methodDefinition, sourceLocationProvider);
			var methodBody = disassembler.Execute();

			System.Console.WriteLine(methodBody);
			System.Console.WriteLine();

			return base.Rewrite(methodDefinition);
		}
	}
}
