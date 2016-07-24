// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;

namespace Model.ThreeAddressCode
{
	public enum ExceptionHandlerBlockKind
	{
		Try,
		Catch,
		Fault,
		Finally
	}

	public abstract class ExceptionHandlerBlock
	{
		public abstract ExceptionHandlerBlockKind Kind { get; }
		public string Start { get; set; }
		public string End { get; set; }

		public ExceptionHandlerBlock(uint start, uint end)
		{
			this.Start = string.Format("L_{0:X4}", start);
			this.End = string.Format("L_{0:X4}", end);
		}

		public override string ToString()
		{
			var kind = this.Kind.ToString().ToLower();
			return string.Format("{0} {1} to {2}", kind, this.Start, this.End);
		}
	}

	public class ProtectedBlock : ExceptionHandlerBlock
	{
		public ExceptionHandler Handler { get; set; }

		public ProtectedBlock(uint start, uint end)
			: base(start, end)
		{
		}

		public override ExceptionHandlerBlockKind Kind
		{
			get { return ExceptionHandlerBlockKind.Try; }
		}

		public override string ToString()
		{
			var self = base.ToString();
			return string.Format("{0} {1}", self, this.Handler);
		}
	}

	public class ExceptionHandler : ExceptionHandlerBlock
	{
		private ExceptionHandlerBlockKind kind;

		public ProtectedBlock ProtectedBlock { get; set; }

		public ExceptionHandler(ExceptionHandlerBlockKind kind, uint start, uint end)
			: base(start, end)
		{
			this.kind = kind;
		}

		public override ExceptionHandlerBlockKind Kind
		{
			get { return kind; }
		}
	}

	public class CatchExceptionHandler : ExceptionHandler
	{
		public IType ExceptionType { get; set; }

		public CatchExceptionHandler(uint start, uint end, IType exceptionType)
			: base(ExceptionHandlerBlockKind.Catch, start, end)
		{
			this.ExceptionType = exceptionType;
		}
	}
}
