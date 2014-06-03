using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.ThreeAddressCode
{
	public interface IExpression
	{
		ISet<Variable> Variables { get; }
	}

	public class BinaryExpression : IExpression
	{
		public BinaryOperation Operation { get; set; }
		public IExpression LeftOperand { get; set; }
		public IExpression RightOperand { get; set; }

		public BinaryExpression(IExpression left, BinaryOperation operation, IExpression right)
		{
			this.Operation = operation;
			this.LeftOperand = left;
			this.RightOperand = right;
		}

		public ISet<Variable> Variables
		{
			get
			{
				var result = new HashSet<Variable>(this.LeftOperand.Variables);
				result.UnionWith(this.RightOperand.Variables);
				return result;
			}
		}

		public override string ToString()
		{
			var operation = string.Empty;

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

			return string.Format("{0} {1} {2}", this.LeftOperand, operation, this.RightOperand);
		}
	}

	public class UnaryExpression : IExpression
	{
		public UnaryOperation Operation { get; set; }
		public IExpression Operand { get; set; }

		public UnaryExpression(UnaryOperation operation, IExpression operand)
		{
			this.Operation = operation;
			this.Operand = operand;
		}

		public ISet<Variable> Variables
		{
			get { return this.Operand.Variables; }
		}

		public override string ToString()
		{
			var operation = string.Empty;

			switch (this.Operation)
			{
				case UnaryOperation.AddressOf: operation = "&"; break;
				case UnaryOperation.GetValueAt: operation = "*"; break;
				case UnaryOperation.Neg: operation = "-"; break;
				case UnaryOperation.Not: operation = "!"; break;

				case UnaryOperation.Copy:
				case UnaryOperation.SetValueAt:
					throw new Exception("Invalid unary expression operation.");
			}

			return string.Format("{0}{1}", operation, this.Operand);
		}
	}
}
