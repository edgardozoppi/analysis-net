using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class UnknownBytecodeException : Exception
	{
		public IOperation bytecode { get; private set; }

		public UnknownBytecodeException(IOperation bytecode)
		{
			this.bytecode = bytecode;
		}

		public override string Message
		{
			get { return string.Format("Unknown bytecode: {0}", this.bytecode.OperationCode); }
		}
	}
}
