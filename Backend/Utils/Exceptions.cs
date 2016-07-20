// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class UnknownBytecodeException : Exception
	{
		public IOperation Opcode { get; private set; }

		public UnknownBytecodeException(OperationCode opcode)
		{
			this.Opcode = Opcode;
		}

		public override string Message
		{
			get { return string.Format("Unknown bytecode: {0}", this.Opcode); }
		}
	}
}
