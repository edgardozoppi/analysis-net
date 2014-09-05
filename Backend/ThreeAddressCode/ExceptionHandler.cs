using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;

namespace Backend.ThreeAddressCode
{
	public enum ExceptionHandlerBlockKind
	{
		Try,
		Catch,
		Finally
	}

	public interface IExceptionHandlerBlock
	{
		ExceptionHandlerBlockKind Kind { get; }
		string Start { get; }
		string End { get; }
	}

	public interface IExceptionHandler : IExceptionHandlerBlock
	{
	}

	public class TryExceptionHandler : IExceptionHandlerBlock
	{
		public ExceptionHandlerBlockKind Kind { get; private set; }
		public string Start { get; set; }
		public string End { get; set; }
		public IExceptionHandler Handler { get; set; }

		public TryExceptionHandler(uint start, uint end)
		{
			this.Kind = ExceptionHandlerBlockKind.Try;
			this.Start = string.Format("L_{0:X4}", start);
			this.End = string.Format("L_{0:X4}", end);
		}

		public override string ToString()
		{
			return string.Format("try {0} to {1} {2}", this.Start, this.End, this.Handler);
		}
	}

	public class CatchExceptionHandler : IExceptionHandler
	{
		public ExceptionHandlerBlockKind Kind { get; private set; }
		public string Start { get; set; }
		public string End { get; set; }
		public ITypeReference ExceptionType { get; set; }

		public CatchExceptionHandler(uint start, uint end, ITypeReference exceptionType)
		{
			this.Kind = ExceptionHandlerBlockKind.Catch;
			this.Start = string.Format("L_{0:X4}", start);
			this.End = string.Format("L_{0:X4}", end);
			this.ExceptionType = exceptionType;
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.ExceptionType);
			return string.Format("catch {0} handler {1} to {2}", type, this.Start, this.End);
		}
	}

	public class FinallyExceptionHandler : IExceptionHandler
	{
		public ExceptionHandlerBlockKind Kind { get; private set; }
		public string Start { get; set; }
		public string End { get; set; }

		public FinallyExceptionHandler(uint start, uint end)
		{
			this.Kind = ExceptionHandlerBlockKind.Finally;
			this.Start = string.Format("L_{0:X4}", start);
			this.End = string.Format("L_{0:X4}", end);
		}

		public override string ToString()
		{
			return string.Format("finally handler {0} to {1}", this.Start, this.End);
		}
	}
}
