// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Expressions;
using Model.ThreeAddressCode.Visitor;

namespace Model.ThreeAddressCode.Instructions
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
		Not,
		Neg
	}

	public enum BranchOperation
	{
		Eq,
		Neq,
		Lt,
		Le,
		Gt,
		Ge
	}

	public enum ConvertOperation
	{
		Conv,
		Cast,
		Box,
		Unbox,
		UnboxPtr
	}

	public enum MethodCallOperation
	{
		Static,
		Virtual,
		Jump
	}

	public abstract class Instruction : IInstruction
	{
		public uint Offset { get; set; }
		public string Label { get; set; }

		protected Instruction(uint offset)
		{
			this.Offset = offset;
			this.Label = string.Format("L_{0:X4}", offset);
		}

		public ISet<IVariable> Variables
		{
			get
			{
				var result = new HashSet<IVariable>();
				result.UnionWith(this.ModifiedVariables);
				result.UnionWith(this.UsedVariables);
				return result;
			}
		}

		public virtual ISet<IVariable> ModifiedVariables
		{
			get { return new HashSet<IVariable>(); }
		}

		public virtual ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>(); }
		}

		public virtual void Replace(IVariable oldvar, IVariable newvar)
		{
		}

		public virtual void Accept(IInstructionVisitor visitor)
		{
			visitor.Visit(this);
		}
	}

	public abstract class DefinitionInstruction : Instruction, IExpressible
	{
		public IVariable Result { get; set; }

		public bool HasResult
		{
			get { return this.Result != null; }
		}

		protected DefinitionInstruction(uint label, IVariable result)
			: base(label)
		{
			this.Result = result;
		}

		public override ISet<IVariable> ModifiedVariables
		{
			get
			{
				var result = new HashSet<IVariable>();
				if (this.HasResult) result.Add(this.Result);
				return result;
			}
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.HasResult && this.Result.Equals(oldvar)) this.Result = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public abstract IExpression ToExpression();
	}

	public class BinaryInstruction : DefinitionInstruction
	{
		public BinaryOperation Operation { get; set; }
		public IVariable LeftOperand { get; set; }
		public IVariable RightOperand { get; set; }
		public bool OverflowCheck { get; set; }
		public bool UnsignedOperands { get; set; }

		public BinaryInstruction(uint label, IVariable result, IVariable left, BinaryOperation operation, IVariable right)
			: base(label, result)
		{
			this.Operation = operation;
			this.LeftOperand = left;
			this.RightOperand = right;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.LeftOperand, this.RightOperand }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.LeftOperand.Equals(oldvar)) this.LeftOperand = newvar;
			if (this.RightOperand.Equals(oldvar)) this.RightOperand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			var expression = new BinaryExpression(this.LeftOperand, this.Operation, this.RightOperand);
			expression.OverflowCheck = this.OverflowCheck;
			expression.UnsignedOperands = this.UnsignedOperands;
			expression.Type = this.Result.Type;
			return expression;
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
				case BinaryOperation.Ge: operation = ">="; break;
				case BinaryOperation.Lt: operation = "<"; break;
				case BinaryOperation.Le: operation = "<="; break;
			}

			return string.Format("{0}:  {1} = {2} {3} {4}", this.Label, this.Result, this.LeftOperand, operation, this.RightOperand);
		}
	}

	public class UnaryInstruction : DefinitionInstruction
	{
		public UnaryOperation Operation { get; set; }
		public IVariable Operand { get; set; }

		public UnaryInstruction(uint label, IVariable result, UnaryOperation operation, IVariable operand)
			: base(label, result)
		{
			this.Operation = operation;
			this.Operand = operand;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.Operand }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new UnaryExpression(this.Operation, this.Operand) { Type = this.Result.Type };
		}

		public override string ToString()
		{
			var operation = string.Empty;

			switch (this.Operation)
			{
				case UnaryOperation.Neg: operation = "-"; break;
				case UnaryOperation.Not: operation = "!"; break;
			}

			return string.Format("{0}:  {1} = {2}{3};", this.Label, this.Result, operation, this.Operand);
		}
	}

	public class LoadInstruction : DefinitionInstruction
	{
		public IValue Operand { get; set; }

		public LoadInstruction(uint label, IVariable result, IValue operand)
			: base(label, result)
		{
			this.Operand = operand;
		}

		public override ISet<IVariable> ModifiedVariables
		{
			get
			{
				// For copies like x = x where variable x
                // is assigned to itself, we still want to
                // consider x a modified variable.
				var result = new HashSet<IVariable>();
				if (this.HasResult) result.Add(this.Result);
				return result;
			}
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>(this.Operand.Variables); }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
			else this.Operand.Replace(oldvar, newvar);
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return this.Operand.ToExpression();
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1} = {2};", this.Label, this.Result, this.Operand);
		}
	}

	public class StoreInstruction : Instruction
	{
		public IAssignableValue Result { get; set; }
		public IVariable Operand { get; set; }

		public StoreInstruction(uint label, IAssignableValue result, IVariable operand)
			: base(label)
		{
			this.Result = result;
			this.Operand = operand;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>(this.Result.Variables) { this.Operand }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			this.Result.Replace(oldvar, newvar);
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1} = {2};", this.Label, this.Result, this.Operand);
		}
	}

	public class NopInstruction : Instruction
	{
		public NopInstruction(uint label)
			: base(label)
		{
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  nop;", this.Label);
		}
	}

	public class BreakpointInstruction : Instruction
	{
		public BreakpointInstruction(uint label)
			: base(label)
		{
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  breakpoint;", this.Label);
		}
	}

	public class TryInstruction : Instruction
	{
		public TryInstruction(uint label)
			: base(label)
		{
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  try;", this.Label);
		}
	}

	public class FaultInstruction : Instruction
	{
		public FaultInstruction(uint label)
			: base(label)
		{
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  fault;", this.Label);
		}
	}

	public class FinallyInstruction : Instruction
	{
		public FinallyInstruction(uint label)
			: base(label)
		{
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  finally;", this.Label);
		}
	}

	public class FilterInstruction : DefinitionInstruction
	{
		public IType ExceptionType { get; set; }

		public FilterInstruction(uint label, IVariable result, IType exceptionType)
			: base(label, result)
		{
			this.ExceptionType = exceptionType;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new FilterExpression(this.ExceptionType);
		}

		public override string ToString()
		{
			return string.Format("{0}:  filter {1} {2};", this.Label, this.ExceptionType, this.Result);
		}
	}

	public class CatchInstruction : DefinitionInstruction
	{
		public IType ExceptionType { get; set; }

		public CatchInstruction(uint label, IVariable result, IType exceptionType)
			: base(label, result)
		{
			this.ExceptionType = exceptionType;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new CatchExpression(this.ExceptionType);
		}

		public override string ToString()
		{
			return string.Format("{0}:  catch {1} {2};", this.Label, this.ExceptionType, this.Result);
		}
	}

	public class ConvertInstruction : DefinitionInstruction
	{
		public ConvertOperation Operation { get; set; }
		public IVariable Operand { get; set; }
		public IType ConversionType { get; set; }
		public bool OverflowCheck { get; set; }
		public bool UnsignedOperands { get; set; }

		public ConvertInstruction(uint label, IVariable result, IVariable operand, ConvertOperation operation, IType conversionType)
			: base(label, result)
		{
			this.Operand = operand;
			this.Operation = operation;
			this.ConversionType = conversionType;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.Operand }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			var expression = new ConvertExpression(this.Operand, this.Operation, this.ConversionType);
			expression.OverflowCheck = this.OverflowCheck;
			expression.UnsignedOperands = this.UnsignedOperands;
			return expression;
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1} = {2} as {3};", this.Label, this.Result, this.Operand, this.ConversionType);
		}
	}

	public class ReturnInstruction : Instruction
	{
		public IVariable Operand { get; set; }

		public ReturnInstruction(uint label, IVariable operand)
			: base(label)
		{
			this.Operand = operand;
		}

		public ReturnInstruction(uint label)
			: base(label)
		{
		}

		public bool HasOperand
		{
			get { return this.Operand != null; }
		}

		public override ISet<IVariable> UsedVariables
		{
			get
			{
				var result = new HashSet<IVariable>();
				if (this.HasOperand) result.Add(this.Operand);
				return result;
			}
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.HasOperand && this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			var operand = string.Empty;

			if (this.HasOperand)
			{
				operand = string.Format(" {0}", this.Operand);
			}

			return string.Format("{0}:  return{1};", this.Label, operand);
		}
	}

	public class ThrowInstruction : Instruction
	{
		public IVariable Operand { get; set; }

		public ThrowInstruction(uint label, IVariable operand)
			: base(label)
		{
			this.Operand = operand;
		}

		public ThrowInstruction(uint label)
			: base(label)
		{
		}

		public bool HasOperand
		{
			get { return this.Operand != null; }
		}

		public override ISet<IVariable> UsedVariables
		{
			get
			{
				var result = new HashSet<IVariable>();
				if (this.HasOperand) result.Add(this.Operand);
				return result;
			}
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.HasOperand && this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			var operation = "rethrow";

			if (this.HasOperand)
			{
				operation = string.Format("throw {0}", this.Operand);
			}

			return string.Format("{0}:  {1};", this.Label, operation);
		}
	}

	public abstract class BranchInstruction : Instruction
	{
		public string Target { get; set; }

		protected BranchInstruction(uint label, uint target)
			: base(label)
		{
			this.Target = string.Format("L_{0:X4}", target);
		}

		protected BranchInstruction(uint label, string target)
			: base(label)
		{
			this.Target = target;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}
	}

	public class ExceptionalBranchInstruction : BranchInstruction
	{
		public IType ExceptionType { get; set; }

		public ExceptionalBranchInstruction(uint label, uint target, IType exceptionType)
			: base(label, target)
		{
			this.ExceptionType = exceptionType;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  on {1} goto {2};", this.Label, this.ExceptionType, this.Target);
		}
	}

	public class UnconditionalBranchInstruction : BranchInstruction
	{
		public UnconditionalBranchInstruction(uint label, uint target)
			: base(label, target)
		{
		}

		public UnconditionalBranchInstruction(uint label, string target)
			: base(label, target)
		{
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  goto {1};", this.Label, this.Target);
		}
	}

	public class ConditionalBranchInstruction : BranchInstruction, IExpressible
	{
		public BranchOperation Operation { get; set; }
		public IVariable LeftOperand { get; set; }
		public IInmediateValue RightOperand { get; set; }
		public bool UnsignedOperands { get; set; }

		public ConditionalBranchInstruction(uint label, IVariable left, BranchOperation operation, IInmediateValue right, uint target)
			: base(label, target)
		{
			this.Operation = operation;
			this.LeftOperand = left;
			this.RightOperand = right;
		}

		public ConditionalBranchInstruction(uint label, IVariable left, BranchOperation operation, IInmediateValue right, string target)
			: base(label, target)
		{
			this.Operation = operation;
			this.LeftOperand = left;
			this.RightOperand = right;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>(this.RightOperand.Variables) { this.LeftOperand }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.LeftOperand.Equals(oldvar)) this.LeftOperand = newvar;
			if (this.RightOperand.Equals(oldvar)) this.RightOperand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public IExpression ToExpression()
		{
			var operation = this.Operation.ToBinaryOperation();
			var expression = new BinaryExpression(this.LeftOperand, operation, this.RightOperand);
			expression.Type = PlatformTypes.Boolean;
			return expression;
		}

		public override string ToString()
		{
			var operation = string.Empty;

			switch (this.Operation)
			{
				case BranchOperation.Eq: operation = "=="; break;
				case BranchOperation.Neq: operation = "!="; break;
				case BranchOperation.Gt: operation = ">"; break;
				case BranchOperation.Ge: operation = ">="; break;
				case BranchOperation.Lt: operation = "<"; break;
				case BranchOperation.Le: operation = "<="; break;
			}

			return string.Format("{0}:  if {1} {2} {3} goto {4};", this.Label, this.LeftOperand, operation, this.RightOperand, this.Target);
		}
	}

	public class SwitchInstruction : Instruction
	{
		public IVariable Operand { get; set; }
		public IList<string> Targets { get; private set; }

		public SwitchInstruction(uint label, IVariable operand, IEnumerable<uint> targets)
			: base(label)
		{
			this.Operand = operand;
			this.Targets = targets.Select(target => string.Format("L_{0:X4}", target)).ToList();
		}

		public SwitchInstruction(uint label, IVariable operand, IEnumerable<string> targets)
			: base(label)
		{
			this.Operand = operand;
			this.Targets = targets.ToList();
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.Operand }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			var targets = string.Join(", ", this.Targets);
			return string.Format("{0}:  if {1} < {2} goto {3};", this.Label, this.Operand, this.Targets.Count, targets);
		}
	}

	public class SizeofInstruction : DefinitionInstruction
	{
		public IType MeasuredType { get; set; }

		public SizeofInstruction(uint label, IVariable result, IType measuredType)
			: base(label, result)
		{
			this.MeasuredType = measuredType;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new SizeofExpression(this.MeasuredType);
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1} = sizeof {2};", this.Label, this.Result, this.MeasuredType);
		}
	}

	public class LoadTokenInstruction : DefinitionInstruction
	{
		public IMetadataReference Token { get; set; }

		public LoadTokenInstruction(uint label, IVariable result, IMetadataReference token)
			: base(label, result)
		{
			this.Token = token;
		}

		public override IExpression ToExpression()
		{
			return new TokenExpression(this.Token);
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			//var type = TypeHelper.GetTypeName(this.Token);
			return string.Format("{0}:  {1} = token {2};", this.Label, this.Result, this.Token);
		}
	}

	public class MethodCallInstruction : DefinitionInstruction
	{
		public MethodCallOperation Operation { get; set; }
		public IMethodReference Method { get; set; }
		public IList<IVariable> Arguments { get; private set; }

		public MethodCallInstruction(uint label, IVariable result, MethodCallOperation operation, IMethodReference method, IEnumerable<IVariable> arguments)
			: base(label, result)
		{
			this.Arguments = new List<IVariable>(arguments);
			this.Operation = operation;
			this.Method = method;
		}

		public override ISet<IVariable> ModifiedVariables
		{
			get
			{
				var result = new HashSet<IVariable>();
				// The first argument could actually be the implicit this parameter,
				// so if the method is not static we need to offset by 1 the remaining arguments.
				var argumentIndex = this.Method.IsStatic ? 0 : 1;

				// Optimization to improve precision:
				// Argument variables could be modified only if they are passed by ref
				// We don't care here if the objects referenced by arguments could be modified.
				foreach (var parameterInfo in this.Method.Parameters)
				{
					if (parameterInfo.Kind == MethodParameterKind.Ref)
					{
						var argument = this.Arguments[argumentIndex++];
						result.Add(argument);
					}
				}

				if (this.HasResult) result.Add(this.Result);
				return result;
			}
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>(this.Arguments); }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.HasResult && this.Result.Equals(oldvar)) this.Result = newvar;

			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new MethodCallExpression(this.Method, this.Arguments);
		}

		public override string ToString()
		{
			var result = string.Empty;
			var arguments = string.Join(", ", this.Arguments);

			if (this.HasResult)
			{
				result = string.Format("{0} = ", this.Result);
			}

			return string.Format("{0}:  {1}{2}::{3}({4});", this.Label, result, this.Method.ContainingType.Name, this.Method.Name, arguments);
		}
	}

	public class IndirectMethodCallInstruction : DefinitionInstruction
	{
		public FunctionPointerType Function { get; set; }
		public IVariable Pointer { get; set; }
		public IList<IVariable> Arguments { get; private set; }

		public IndirectMethodCallInstruction(uint label, IVariable result, IVariable pointer, FunctionPointerType function, IEnumerable<IVariable> arguments)
			: base(label, result)
		{
			this.Arguments = new List<IVariable>(arguments);
			this.Pointer = pointer;
			this.Function = function;
		}

		public override ISet<IVariable> ModifiedVariables
		{
			get
			{
				var result = new HashSet<IVariable>();
				// The first argument could actually be the implicit this parameter,
				// so if the method is not static we need to offset by 1 the remaining arguments.
				var argumentIndex = this.Function.IsStatic ? 0 : 1;

				// Optimization to improve precision:
				// Argument variables could be modified only if they are passed by ref
				// We don't care here if the objects referenced by arguments could be modified.
				foreach (var parameterInfo in this.Function.Parameters)
				{
					if (parameterInfo.Kind == MethodParameterKind.Ref)
					{
						var argument = this.Arguments[argumentIndex++];
						result.Add(argument);
					}
				}

				if (this.HasResult) result.Add(this.Result);
				return result;
			}
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>(this.Arguments) { this.Pointer }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.HasResult && this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.Pointer.Equals(oldvar)) this.Pointer = newvar;

			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new IndirectMethodCallExpression(this.Pointer, this.Function, this.Arguments);
		}

		public override string ToString()
		{
			var result = string.Empty;
			var arguments = string.Join(", ", this.Arguments);

			if (this.HasResult)
			{
				result = string.Format("{0} = ", this.Result);
			}

			return string.Format("{0}:  {1}(*{2})({3});", this.Label, result, this.Pointer, arguments);
		}
	}

	public class CreateObjectInstruction : DefinitionInstruction
	{
		public IType AllocationType { get; set; }

		public CreateObjectInstruction(uint label, IVariable result, IType allocationType)
			: base(label, result)
		{
			this.AllocationType = allocationType;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new CreateObjectExpression(this.AllocationType);
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1} = new {2};", this.Label, this.Result, this.AllocationType);
		}
	}

	public class CopyMemoryInstruction : Instruction
	{
		public IVariable NumberOfBytes { get; set; }
		public IVariable SourceAddress { get; set; }
		public IVariable TargetAddress { get; set; }

		public CopyMemoryInstruction(uint label, IVariable target, IVariable source, IVariable numberOfBytes)
			: base(label)
		{
			this.NumberOfBytes = numberOfBytes;
			this.SourceAddress = source;
			this.TargetAddress = target;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.NumberOfBytes, this.SourceAddress, this.TargetAddress }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.NumberOfBytes.Equals(oldvar)) this.NumberOfBytes = newvar;
			if (this.SourceAddress.Equals(oldvar)) this.SourceAddress = newvar;
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  copy {1} bytes from {1} to {2};", this.Label, this.NumberOfBytes, this.SourceAddress, this.TargetAddress);
		}
	}

	public class LocalAllocationInstruction : Instruction
	{
		public IVariable NumberOfBytes { get; set; }
		public IVariable TargetAddress { get; set; }

		public LocalAllocationInstruction(uint label, IVariable target, IVariable numberOfBytes)
			: base(label)
		{
			this.NumberOfBytes = numberOfBytes;
			this.TargetAddress = target;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.NumberOfBytes, this.TargetAddress }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.NumberOfBytes.Equals(oldvar)) this.NumberOfBytes = newvar;
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  allocate {1} bytes at {2};", this.Label, this.NumberOfBytes, this.TargetAddress);
		}
	}

	public class InitializeMemoryInstruction : Instruction
	{
		public IVariable NumberOfBytes { get; set; }
		public IVariable Value { get; set; }
		public IVariable TargetAddress { get; set; }

		public InitializeMemoryInstruction(uint label, IVariable target, IVariable value, IVariable numberOfBytes)
			: base(label)
		{
			this.NumberOfBytes = numberOfBytes;
			this.TargetAddress = target;
			this.Value = value;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.NumberOfBytes, this.Value, this.TargetAddress }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.NumberOfBytes.Equals(oldvar)) this.NumberOfBytes = newvar;
			if (this.Value.Equals(oldvar)) this.Value = newvar;
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  init {1} bytes at {2} with {3};", this.Label, this.NumberOfBytes, this.TargetAddress, this.Value);
		}
	}

	public class InitializeObjectInstruction : Instruction
	{
		public IVariable TargetAddress { get; set; }

		public InitializeObjectInstruction(uint label, IVariable target)
			: base(label)
		{
			this.TargetAddress = target;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.TargetAddress }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  init object at {1};", this.Label, this.TargetAddress);
		}
	}

	public class CopyObjectInstruction : Instruction
	{
		public IVariable SourceAddress { get; set; }
		public IVariable TargetAddress { get; set; }

		public CopyObjectInstruction(uint label, IVariable target, IVariable source)
			: base(label)
		{
			this.SourceAddress = source;
			this.TargetAddress = target;
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>() { this.SourceAddress, this.TargetAddress }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.SourceAddress.Equals(oldvar)) this.SourceAddress = newvar;
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return string.Format("{0}:  copy object from {1} to {2};", this.Label, this.SourceAddress, this.TargetAddress);
		}
	}

	public class CreateArrayInstruction : DefinitionInstruction
	{
		public IType ElementType { get; set; }
		public uint Rank { get; set; }
		public IList<IVariable> LowerBounds { get; private set; }
		public IList<IVariable> Sizes { get; private set; }

		public CreateArrayInstruction(uint label, IVariable result, IType elementType, uint rank, IEnumerable<IVariable> lowerBounds, IEnumerable<IVariable> sizes)
			: base(label, result)
		{
			this.ElementType = elementType;
			this.Rank = rank;
			this.LowerBounds = new List<IVariable>(lowerBounds);
			this.Sizes = new List<IVariable>(sizes);
		}

		public override ISet<IVariable> UsedVariables
		{
			get
			{
				var result = new HashSet<IVariable>();
				result.UnionWith(this.LowerBounds);
				result.UnionWith(this.Sizes);
				return result;
			}
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;

			for (var i = 0; i < this.LowerBounds.Count; ++i)
			{
				var bound = this.LowerBounds[i];
				if (bound.Equals(oldvar)) this.LowerBounds[i] = newvar;
			}

			for (var i = 0; i < this.Sizes.Count; ++i)
			{
				var size = this.Sizes[i];
				if (size.Equals(oldvar)) this.Sizes[i] = newvar;
			}
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new CreateArrayExpression(this.ElementType, this.Rank, this.LowerBounds, this.Sizes);
		}

		public override string ToString()
		{
			var sizes = string.Join(", ", this.Sizes);
			return string.Format("{0}:  {1} = new {2}[{3}];", this.Label, this.Result, this.ElementType, sizes);
		}
	}

	public class PhiInstruction : DefinitionInstruction
	{
		public IList<IVariable> Arguments { get; private set; }

		public PhiInstruction(uint label, IVariable result)
			: base(label, result)
		{
			this.Arguments = new List<IVariable>();
		}

		public override ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>(this.Arguments); }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;

			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override IExpression ToExpression()
		{
			return new PhiExpression(this.Arguments);
		}

		public override string ToString()
		{
			var arguments = string.Join(", ", this.Arguments);
			return string.Format("{0}:  {1} = Φ({2});", this.Label, this.Result, arguments);
		}
	}
}
