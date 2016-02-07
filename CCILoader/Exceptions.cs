using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cci = Microsoft.Cci;

namespace CCILoader
{
	public class UnknownBytecodeException : Exception
	{
		public Cci.IOperation Opcode { get; private set; }

		public UnknownBytecodeException(Cci.OperationCode opcode)
		{
			this.Opcode = Opcode;
		}

		public override string Message
		{
			get { return string.Format("Unknown bytecode: {0}", this.Opcode); }
		}
	}
}
