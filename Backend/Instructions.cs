using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;

namespace Backend
{
	public enum BinaryOperation
	{
		Add,
		Sub,
		Mul,
		Div,
		Rem,
		And,
		Or,
		Xor,
		Shl,
		Shr,
		Eq,
		Neq,
		Lt,
		Le,
		Gt,
		Ge
	}

	public enum UnaryOperation
	{
		Assign,
		AddressOf,
		Not,
		Neg
	}

	public enum EmptyOperation
	{
		Nop,
		Break
	}

	public enum BranchCondition
	{
		Eq,
		Neq,
		Lt,
		Le,
		Gt,
		Ge
	}

	public abstract class Instruction
	{
		public string Label { get; set; }
	}

	public class BinaryInstruction : Instruction
	{
		public Operand LeftOperand { get; set; }
		public Operand RightOperand { get; set; }
		public BinaryOperation Operation { get; set; }
		public Variable Result { get; set; }
		public bool CheckOverflow { get; set; }
		public bool TreatOperandsAsUnsignedIntegers { get; set; }

		public BinaryInstruction(uint label, Variable result, Operand left, BinaryOperation operation, Operand right)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Result = result;
			this.LeftOperand = left;
			this.Operation = operation;
			this.RightOperand = right;
		}

		public override string ToString()
		{
			var operation = "??";

			switch (this.Operation)
			{
				case BinaryOperation.Add: operation = "+"; break;
				case BinaryOperation.Sub: operation = "-"; break;
				case BinaryOperation.Mul: operation = "*"; break;
				case BinaryOperation.Div: operation = "/"; break;
				case BinaryOperation.Rem: operation = "%"; break;
				case BinaryOperation.And: operation = "&"; break;
				case BinaryOperation.Or: operation = "|"; break;
				case BinaryOperation.Xor: operation = "^"; break;
				case BinaryOperation.Shl: operation = "<<"; break;
				case BinaryOperation.Shr: operation = ">>"; break;
				case BinaryOperation.Eq: operation = "=="; break;
				case BinaryOperation.Neq: operation = "!="; break;
				case BinaryOperation.Gt: operation = ">"; break;
				case BinaryOperation.Ge: operation = "<="; break;
				case BinaryOperation.Lt: operation = ">"; break;
				case BinaryOperation.Le: operation = "<="; break;
			}

			return string.Format("{0}:  {1} = {2} {3} {4};", this.Label, this.Result, this.LeftOperand, operation, this.RightOperand);
		}
	}

	public class UnaryInstruction : Instruction
	{
		public Operand Operand { get; set; }
		public UnaryOperation Operation { get; set; }
		public Variable Result { get; set; }

		public UnaryInstruction(uint label, Variable result, UnaryOperation operation, Operand operand)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Result = result;
			this.Operation = operation;
			this.Operand = operand;
		}

		public override string ToString()
		{
			var operation = "??";

			switch (this.Operation)
			{
				case UnaryOperation.Assign: operation = string.Empty; break;
				case UnaryOperation.AddressOf: operation = "&"; break;
				case UnaryOperation.Neg: operation = "-"; break;
				case UnaryOperation.Not: operation = "!"; break;
			}

			return string.Format("{0}:  {1} = {2}{3};", this.Label, this.Result, operation, this.Operand);
		}
	}

	public class EmptyInstruction : Instruction
	{
		public EmptyOperation Operation { get; set; }

		public EmptyInstruction(uint label, EmptyOperation operation)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Operation = operation;
		}

		public override string ToString()
		{
			var operation = "??";

			switch (this.Operation)
			{
				case EmptyOperation.Nop: operation = "nop"; break;
				case EmptyOperation.Break: operation = "break"; break;
			}

			return string.Format("{0}:  {1};", this.Label, operation);
		}
	}

	public class ConvertInstruction : Instruction
	{
		public Operand Operand { get; set; }
		public ITypeReference Type { get; set; }
		public Variable Result { get; set; }
		public bool CheckNumericRange { get; set; }
		public bool TreatOperandAsUnsignedInteger { get; set; }

		public ConvertInstruction(uint label, Variable result, ITypeReference type, Operand operand)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Result = result;
			this.Type = type;
			this.Operand = operand;
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1} = {2} as {3};", this.Label, this.Result, this.Operand, this.Type);
		}
	}

	public class ReturnInstruction : Instruction
	{
		public Operand Operand { get; set; }

		public ReturnInstruction(uint label, Operand operand)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Operand = operand;
		}

		public bool HasOperand
		{
			get { return this.Operand != null; }
		}

		public override string ToString()
		{
			var operand = string.Empty;

			if (this.HasOperand)
				operand = string.Format(" {0}", this.Operand);

			return string.Format("{0}:  ret{1};", this.Label, operand);
		}
	}

	public class UnconditionalBranchInstruction : Instruction
	{
		public string Target { get; set; }

		public UnconditionalBranchInstruction(uint label, uint target)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Target = string.Format("L_{0:X4}", target);
		}

		public override string ToString()
		{
			return string.Format("{0}:  goto {1};", this.Label, this.Target);
		}
	}

	public class ConditionalBranchInstruction : Instruction
	{
		public Operand LeftOperand { get; set; }
		public Operand RightOperand { get; set; }
		public BranchCondition Condition { get; set; }
		public string Target { get; set; }

		public ConditionalBranchInstruction(uint label, Operand left, BranchCondition condition, Operand right, uint target)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Target = string.Format("L_{0:X4}", target);
			this.LeftOperand = left;
			this.Condition = condition;
			this.RightOperand = right;
		}

		public override string ToString()
		{
			var condition = "??";

			switch (this.Condition)
			{
				case BranchCondition.Eq: condition = "=="; break;
				case BranchCondition.Neq: condition = "!="; break;
				case BranchCondition.Gt: condition = ">"; break;
				case BranchCondition.Ge: condition = "<="; break;
				case BranchCondition.Lt: condition = ">"; break;
				case BranchCondition.Le: condition = "<="; break;
			}

			return string.Format("{0}:  if {1} {2} {3} goto {4};", this.Label, this.LeftOperand, condition, this.RightOperand, this.Target);
		}
	}

	public class SizeOfInstruction : Instruction
	{
		public ITypeReference Type { get; set; }
		public Variable Result { get; set; }

		public SizeOfInstruction(uint label, Variable result, ITypeReference type)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Result = result;
			this.Type = type;
		}

		public override string ToString()
		{
			return string.Format("{0}:  sizeOf {1};", this.Label, this.Result, this.Type);
		}
	}

	public class CallInstruction : Instruction
	{
		public IMethodReference Method { get; set; }
		public Variable Result { get; set; }
		public List<Operand> Arguments { get; private set; }

		public CallInstruction(uint label, Variable result, IMethodReference method, IEnumerable<Operand> arguments)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Arguments = new List<Operand>(arguments);
			this.Result = result;
			this.Method = method;
		}

		public override string ToString()
		{
			var result = string.Empty;
			var type = TypeHelper.GetTypeName(this.Method.ContainingType);
			var method = MemberHelper.GetMethodSignature(this.Method, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);
			var arguments = string.Join(", ", this.Arguments);

			if (this.Result != null)
				result = string.Format("{0} = ", this.Result);			

			return string.Format("{0}:  {1}{2}::{3}({4});", this.Label, result, type, method, arguments);
		}
	}

	public class NewInstruction : Instruction
	{
		public IMethodReference Constructor { get; set; }
		public Variable Result { get; set; }
		public List<Operand> Arguments { get; private set; }

		public NewInstruction(uint label, Variable result, IMethodReference constructor, IEnumerable<Operand> arguments)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Arguments = new List<Operand>(arguments);
			this.Result = result;
			this.Constructor = constructor;
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.Constructor.ContainingType);
			var arguments = string.Join(", ", this.Arguments.Skip(1));

			return string.Format("{0}:  {1} = new {2}({3});", this.Label, this.Result, type, arguments);
		}
	}

	public class CopyMemoryInstruction : Instruction
	{
		public Variable NumberOfBytes { get; set; }
		public Variable SourceAddress { get; set; }
		public Variable TargetAddress { get; set; }

		public CopyMemoryInstruction(uint label, Variable source, Variable target, Variable numberOfBytes)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.NumberOfBytes = numberOfBytes;
			this.SourceAddress = source;
			this.TargetAddress = target;
		}

		public override string ToString()
		{
			return string.Format("{0}:  copy {1} bytes from {1} to {2};", this.Label, this.NumberOfBytes, this.SourceAddress, this.TargetAddress);
		}
	}
}
