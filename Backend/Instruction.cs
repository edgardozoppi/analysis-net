using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend
{
	public enum BinaryOperation
	{
		Add,
		Sub,
		Mul,
		Div,
		And,
		Gt,
		Lt,
		Eq,
		Or,
		Rem,
		Shl,
		Shr,
		Xor
	}

	public enum UnaryOperation
	{
		Copy,
		Not,
		Neg
	}

	public enum EmptyOperation
	{
		Nop,
		Break
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
				case BinaryOperation.And: operation = "&"; break;
				case BinaryOperation.Gt: operation = ">"; break;
				case BinaryOperation.Lt: operation = "<"; break;
				case BinaryOperation.Eq: operation = "=="; break;
				case BinaryOperation.Or: operation = "|"; break;
				case BinaryOperation.Rem: operation = "%"; break;
				case BinaryOperation.Shl: operation = "<<"; break;
				case BinaryOperation.Shr: operation = ">>"; break;
				case BinaryOperation.Xor: operation = "^"; break;
			}

			return string.Format("{0}:\t{1} = {2} {3} {4};", this.Label, this.Result, this.LeftOperand, operation, this.RightOperand);
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
				case UnaryOperation.Copy: operation = string.Empty; break;
				case UnaryOperation.Neg: operation = "-"; break;
				case UnaryOperation.Not: operation = "!"; break;
			}

			return string.Format("{0}:\t{1} = {2}{3};", this.Label, this.Result, operation, this.Operand);
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

			return string.Format("{0}:\t{1};", this.Label, operation);
		}
	}
}
