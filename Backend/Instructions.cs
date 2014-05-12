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
		GetValueAt,
		SetValueAt,
		Not,
		Neg
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
			var rightOperation = string.Empty;
			var leftOperation = string.Empty;

			switch (this.Operation)
			{
				case UnaryOperation.Assign: break;
				case UnaryOperation.AddressOf: leftOperation = "&"; break;
				case UnaryOperation.GetValueAt: leftOperation = "*"; break;
				case UnaryOperation.SetValueAt: rightOperation = "*"; break;
				case UnaryOperation.Neg: leftOperation = "-"; break;
				case UnaryOperation.Not: leftOperation = "!"; break;
			}

			return string.Format("{0}:  {1}{2} = {3}{4};", this.Label, rightOperation, this.Result, leftOperation, this.Operand);
		}
	}

	public class EmptyInstruction : Instruction
	{
		public EmptyInstruction(uint label)
		{
			this.Label = string.Format("L_{0:X4}", label);
		}

		public override string ToString()
		{
			return string.Format("{0}:  nop;", this.Label);
		}
	}

	public class BreakpointInstruction : Instruction
	{
		public BreakpointInstruction(uint label)
		{
			this.Label = string.Format("L_{0:X4}", label);
		}

		public override string ToString()
		{
			return string.Format("{0}:  breakpoint;", this.Label);
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
			var type = TypeHelper.GetTypeName(this.Type);
			return string.Format("{0}:  {1} = {2} as {3};", this.Label, this.Result, this.Operand, type);
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

	public class SizeofInstruction : Instruction
	{
		public ITypeReference Type { get; set; }
		public Variable Result { get; set; }

		public SizeofInstruction(uint label, Variable result, ITypeReference type)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Result = result;
			this.Type = type;
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.Type);
			return string.Format("{0}:  sizeof {1};", this.Label, this.Result, type);
		}
	}

	public class MethodCallInstruction : Instruction
	{
		public IMethodReference Method { get; set; }
		public Variable Result { get; set; }
		public List<Operand> Arguments { get; private set; }

		public MethodCallInstruction(uint label, Variable result, IMethodReference method, IEnumerable<Operand> arguments)
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

	public class CreateObjectInstruction : Instruction
	{
		public IMethodReference Constructor { get; set; }
		public Variable Result { get; set; }
		public List<Operand> Arguments { get; private set; }

		public CreateObjectInstruction(uint label, Variable result, IMethodReference constructor, IEnumerable<Operand> arguments)
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
		public Operand NumberOfBytes { get; set; }
		public Operand SourceAddress { get; set; }
		public Operand TargetAddress { get; set; }

		public CopyMemoryInstruction(uint label, Operand target, Operand source, Operand numberOfBytes)
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

	public class LocalAllocationInstruction : Instruction
	{
		public Operand NumberOfBytes { get; set; }
		public Operand TargetAddress { get; set; }

		public LocalAllocationInstruction(uint label, Operand target, Operand numberOfBytes)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.NumberOfBytes = numberOfBytes;
			this.TargetAddress = target;
		}

		public override string ToString()
		{
			return string.Format("{0}:  allocate {1} bytes at {2};", this.Label, this.NumberOfBytes, this.TargetAddress);
		}
	}

	public class InitializeMemoryInstruction : Instruction
	{
		public Operand NumberOfBytes { get; set; }
		public Operand Value { get; set; }
		public Operand TargetAddress { get; set; }

		public InitializeMemoryInstruction(uint label, Operand target, Operand value, Operand numberOfBytes)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.NumberOfBytes = numberOfBytes;
			this.TargetAddress = target;
			this.Value = value;
		}

		public override string ToString()
		{
			return string.Format("{0}:  init {1} bytes at {2} with {3};", this.Label, this.NumberOfBytes, this.TargetAddress, this.Value);
		}
	}

	public class InitializeObjectInstruction : Instruction
	{
		public Operand TargetAddress { get; set; }

		public InitializeObjectInstruction(uint label, Operand target)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.TargetAddress = target;
		}

		public override string ToString()
		{
			return string.Format("{0}:  init object at {1};", this.Label, this.TargetAddress);
		}
	}

	public class CopyObjectInstruction : Instruction
	{
		public Operand SourceAddress { get; set; }
		public Operand TargetAddress { get; set; }

		public CopyObjectInstruction(uint label, Operand target, Operand source)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.SourceAddress = source;
			this.TargetAddress = target;
		}

		public override string ToString()
		{
			return string.Format("{0}:  copy object from {1} to {2};", this.Label, this.SourceAddress, this.TargetAddress);
		}
	}

	public class CreateArrayInstruction : Instruction
	{
		public Variable Result { get; set; }
		public ITypeReference ElementType { get; set; }
		public uint Rank { get; set; }
		public List<Operand> LowerBounds { get; private set; }
		public List<Operand> Sizes { get; private set; }

		public CreateArrayInstruction(uint label, Variable result, ITypeReference elementType, uint rank, IEnumerable<Operand> lowerBounds, IEnumerable<Operand> sizes)
		{
			this.Label = string.Format("L_{0:X4}", label);
			this.Result = result;
			this.ElementType = elementType;
			this.Rank = rank;
			this.LowerBounds = new List<Operand>(lowerBounds);
			this.Sizes = new List<Operand>(sizes);
		}

		public override string ToString()
		{
			var elementType = TypeHelper.GetTypeName(this.ElementType);
			var sizes = string.Join(", ", this.Sizes);

			return string.Format("{0}:  {1} = new {2}[{3}];", this.Label, this.Result, elementType, sizes);
		}
	}
}
