using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;

namespace Backend.ThreeAddressCode
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

	public abstract class Instruction : IVariableContainer
	{
		public string Label { get; set; }

		protected Instruction(uint label)
		{
			this.Label = string.Format("L_{0:X4}", label);
		}

		public ISet<Variable> Variables
		{
			get
			{
				var result = new HashSet<Variable>();
				result.UnionWith(this.ModifiedVariables);
				result.UnionWith(this.UsedVariables);
				return result;
			}
		}

		public virtual ISet<Variable> ModifiedVariables
		{
			get { return new HashSet<Variable>(); }
		}

		public virtual ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>(); }
		}

		public virtual void Replace(Variable oldvar, Variable newvar)
		{
		}
	}

	public abstract class DefinitionInstruction : Instruction, IExpressible
	{
		public Variable Result { get; set; }

		public bool HasResult
		{
			get { return this.Result != null; }
		}

		public DefinitionInstruction(uint label, Variable result)
			: base(label)
		{
			this.Result = result;
		}

		public override ISet<Variable> ModifiedVariables
		{
			get
			{
				var result = new HashSet<Variable>();
				if (this.HasResult) result.Add(this.Result);
				return result;
			}
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.HasResult && this.Result.Equals(oldvar)) this.Result = newvar;
		}

		public abstract IExpression ToExpression();
	}

	public class BinaryInstruction : DefinitionInstruction
	{
		public BinaryOperation Operation { get; set; }
		public Variable LeftOperand { get; set; }
		public Variable RightOperand { get; set; }

		public BinaryInstruction(uint label, Variable result, Variable left, BinaryOperation operation, Variable right)
			: base(label, result)
		{
			this.Operation = operation;
			this.LeftOperand = left;
			this.RightOperand = right;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.LeftOperand, this.RightOperand }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.LeftOperand.Equals(oldvar)) this.LeftOperand = newvar;
			if (this.RightOperand.Equals(oldvar)) this.RightOperand = newvar;
		}

		public override IExpression ToExpression()
		{
			return new BinaryExpression(this.LeftOperand, this.Operation, this.RightOperand);
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
		public Variable Operand { get; set; }

		public UnaryInstruction(uint label, Variable result, UnaryOperation operation, Variable operand)
			: base(label, result)
		{
			this.Operation = operation;
			this.Operand = operand;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.Operand }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override IExpression ToExpression()
		{
			return new UnaryExpression(this.Operation, this.Operand);
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

		public LoadInstruction(uint label, Variable result, IValue operand)
			: base(label, result)
		{
			this.Operand = operand;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>(this.Operand.Variables); }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
			else this.Operand.Replace(oldvar, newvar);
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
		public Variable Operand { get; set; }

		public StoreInstruction(uint label, IAssignableValue result, Variable operand)
			: base(label)
		{
			this.Result = result;
			this.Operand = operand;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>(this.Result.Variables) { this.Operand }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			this.Result.Replace(oldvar, newvar);
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
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
			this.Label = string.Format("{0}'", this.Label);
		}

		public override string ToString()
		{
			return string.Format("{0}: try;", this.Label);
		}
	}

	public class FinallyInstruction : Instruction
	{
		public FinallyInstruction(uint label)
			: base(label)
		{
			this.Label = string.Format("{0}'", this.Label);
		}

		public override string ToString()
		{
			return string.Format("{0}: finally;", this.Label);
		}
	}

	public class CatchInstruction : DefinitionInstruction
	{
		public ITypeReference ExceptionType { get; set; }

		public CatchInstruction(uint label, Variable result, ITypeReference exceptionType)
			: base(label, result)
		{
			this.Label = string.Format("{0}'", this.Label);
			this.ExceptionType = exceptionType;
		}

		public override IExpression ToExpression()
		{
			return new CatchExpression(this.ExceptionType);
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.ExceptionType);
			return string.Format("{0}: catch {1} {2};", this.Label, type, this.Result);
		}
	}

	public class ConvertInstruction : DefinitionInstruction
	{
		public Variable Operand { get; set; }
		public ITypeReference Type { get; set; }
		public bool CheckNumericRange { get; set; }
		public bool TreatOperandAsUnsignedInteger { get; set; }

		public ConvertInstruction(uint label, Variable result, ITypeReference type, Variable operand)
			: base(label, result)
		{
			this.Type = type;
			this.Operand = operand;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.Operand }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override IExpression ToExpression()
		{
			return new ConvertExpression(this.Type, this.Operand);
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.Type);
			return string.Format("{0}:  {1} = {2} as {3};", this.Label, this.Result, this.Operand, type);
		}
	}

	public class ReturnInstruction : Instruction
	{
		public Variable Operand { get; set; }

		public ReturnInstruction(uint label, Variable operand)
			: base(label)
		{
			this.Operand = operand;
		}

		public bool HasOperand
		{
			get { return this.Operand != null; }
		}

		public override ISet<Variable> UsedVariables
		{
			get
			{
				var result = new HashSet<Variable>();
				if (this.HasOperand) result.Add(this.Operand);
				return result;
			}
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.HasOperand && this.Operand.Equals(oldvar)) this.Operand = newvar;
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

	public abstract class BranchInstruction : Instruction
	{
		public string Target { get; set; }

		public BranchInstruction(uint label, uint target)
			: base(label)
		{
			this.Target = string.Format("L_{0:X4}", target);
		}
	}

	public class ExceptionalBranchInstruction : BranchInstruction
	{
		public ITypeReference ExceptionType { get; set; }

		public ExceptionalBranchInstruction(uint label, uint target, ITypeReference exceptionType)
			: base (label, target)
		{
			this.Target = string.Format("{0}'", this.Target);
			this.ExceptionType = exceptionType;
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.ExceptionType);
			return string.Format("{0}:  on {1} goto {2};", this.Label, type, this.Target);
		}
	}

	public class UnconditionalBranchInstruction : BranchInstruction
	{
		public UnconditionalBranchInstruction(uint label, uint target)
			: base(label, target)
		{
		}

		public override string ToString()
		{
			return string.Format("{0}:  goto {1};", this.Label, this.Target);
		}
	}

	public class ConditionalBranchInstruction : BranchInstruction
	{
		public Variable Operand { get; set; }

		public ConditionalBranchInstruction(uint label, Variable operand, uint target)
			: base(label, target)
		{
			this.Operand = operand;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.Operand }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override string ToString()
		{
			return string.Format("{0}:  if {1} goto {2};", this.Label, this.Operand, this.Target);
		}
	}

	public class SizeofInstruction : DefinitionInstruction
	{
		public ITypeReference Type { get; set; }

		public SizeofInstruction(uint label, Variable result, ITypeReference type)
			: base(label, result)
		{
			this.Type = type;
		}

		public override IExpression ToExpression()
		{
			return new SizeofExpression(this.Type);
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.Type);
			return string.Format("{0}:  {1} = sizeof {2};", this.Label, this.Result, type);
		}
	}

	public class MethodCallInstruction : DefinitionInstruction
	{
		public IMethodReference Method { get; set; }
		public IList<Variable> Arguments { get; private set; }

		public MethodCallInstruction(uint label, Variable result, IMethodReference method, IEnumerable<Variable> arguments)
			: base(label, result)
		{
			this.Arguments = new List<Variable>(arguments);
			this.Method = method;
		}

		public override ISet<Variable> ModifiedVariables
		{
			get
			{
				//TODO: arguments could be modified only for reference types
				var result = new HashSet<Variable>(this.Arguments);
				if (this.HasResult) result.Add(this.Result);
				return result;
			}
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>(this.Arguments); }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.HasResult && this.Result.Equals(oldvar)) this.Result = newvar;

			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public override IExpression ToExpression()
		{
			return new MethodCallExpression(this.Method, this.Arguments);
		}

		public override string ToString()
		{
			var result = string.Empty;
			var type = TypeHelper.GetTypeName(this.Method.ContainingType);
			var method = MemberHelper.GetMethodSignature(this.Method, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);
			var arguments = string.Join(", ", this.Arguments);

			if (this.HasResult)
			{
				result = string.Format("{0} = ", this.Result);
			}

			return string.Format("{0}:  {1}{2}::{3}({4});", this.Label, result, type, method, arguments);
		}
	}

	public class IndirectMethodCallInstruction : DefinitionInstruction
	{
		public IFunctionPointerTypeReference Type { get; set; }
		public Variable Pointer { get; set; }
		public IList<Variable> Arguments { get; private set; }

		public IndirectMethodCallInstruction(uint label, Variable result, Variable pointer, IFunctionPointerTypeReference type, IEnumerable<Variable> arguments)
			: base(label, result)
		{
			this.Arguments = new List<Variable>(arguments);
			this.Pointer = pointer;
			this.Type = type;
		}

		public override ISet<Variable> ModifiedVariables
		{
			get
			{
				//TODO: arguments could be modified only for reference types
				var result = new HashSet<Variable>(this.Arguments);
				if (this.HasResult) result.Add(this.Result);
				return result;
			}
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>(this.Arguments) { this.Pointer }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.HasResult && this.Result.Equals(oldvar)) this.Result = newvar;
			if (this.Pointer.Equals(oldvar)) this.Pointer = newvar;

			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public override IExpression ToExpression()
		{
			return new IndirectMethodCallExpression(this.Pointer, this.Type, this.Arguments);
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
		public IMethodReference Constructor { get; set; }
		public IList<Variable> Arguments { get; private set; }

		public CreateObjectInstruction(uint label, Variable result, IMethodReference constructor, IEnumerable<Variable> arguments)
			: base(label, result)
		{
			this.Arguments = new List<Variable>(arguments);
			this.Constructor = constructor;
		}

		public override ISet<Variable> ModifiedVariables
		{
			get
			{
				//TODO: arguments could be modified only for reference types
				var result = new HashSet<Variable>(this.Arguments);
				result.Add(this.Result);
				return result;
			}
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>(this.Arguments); }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;

			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public override IExpression ToExpression()
		{
			return new CreateObjectExpression(this.Constructor, this.Arguments);
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

		public CopyMemoryInstruction(uint label, Variable target, Variable source, Variable numberOfBytes)
			: base(label)
		{
			this.NumberOfBytes = numberOfBytes;
			this.SourceAddress = source;
			this.TargetAddress = target;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.NumberOfBytes, this.SourceAddress, this.TargetAddress }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.NumberOfBytes.Equals(oldvar)) this.NumberOfBytes = newvar;
			if (this.SourceAddress.Equals(oldvar)) this.SourceAddress = newvar;
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override string ToString()
		{
			return string.Format("{0}:  copy {1} bytes from {1} to {2};", this.Label, this.NumberOfBytes, this.SourceAddress, this.TargetAddress);
		}
	}

	public class LocalAllocationInstruction : Instruction
	{
		public Variable NumberOfBytes { get; set; }
		public Variable TargetAddress { get; set; }

		public LocalAllocationInstruction(uint label, Variable target, Variable numberOfBytes)
			: base(label)
		{
			this.NumberOfBytes = numberOfBytes;
			this.TargetAddress = target;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.NumberOfBytes, this.TargetAddress }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.NumberOfBytes.Equals(oldvar)) this.NumberOfBytes = newvar;
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override string ToString()
		{
			return string.Format("{0}:  allocate {1} bytes at {2};", this.Label, this.NumberOfBytes, this.TargetAddress);
		}
	}

	public class InitializeMemoryInstruction : Instruction
	{
		public Variable NumberOfBytes { get; set; }
		public Variable Value { get; set; }
		public Variable TargetAddress { get; set; }

		public InitializeMemoryInstruction(uint label, Variable target, Variable value, Variable numberOfBytes)
			: base(label)
		{
			this.NumberOfBytes = numberOfBytes;
			this.TargetAddress = target;
			this.Value = value;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.NumberOfBytes, this.Value, this.TargetAddress }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.NumberOfBytes.Equals(oldvar)) this.NumberOfBytes = newvar;
			if (this.Value.Equals(oldvar)) this.Value = newvar;
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override string ToString()
		{
			return string.Format("{0}:  init {1} bytes at {2} with {3};", this.Label, this.NumberOfBytes, this.TargetAddress, this.Value);
		}
	}

	public class InitializeObjectInstruction : Instruction
	{
		public Variable TargetAddress { get; set; }

		public InitializeObjectInstruction(uint label, Variable target)
			: base(label)
		{
			this.TargetAddress = target;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.TargetAddress }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override string ToString()
		{
			return string.Format("{0}:  init object at {1};", this.Label, this.TargetAddress);
		}
	}

	public class CopyObjectInstruction : Instruction
	{
		public Variable SourceAddress { get; set; }
		public Variable TargetAddress { get; set; }

		public CopyObjectInstruction(uint label, Variable target, Variable source)
			: base(label)
		{
			this.SourceAddress = source;
			this.TargetAddress = target;
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>() { this.SourceAddress, this.TargetAddress }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.SourceAddress.Equals(oldvar)) this.SourceAddress = newvar;
			if (this.TargetAddress.Equals(oldvar)) this.TargetAddress = newvar;
		}

		public override string ToString()
		{
			return string.Format("{0}:  copy object from {1} to {2};", this.Label, this.SourceAddress, this.TargetAddress);
		}
	}

	public class CreateArrayInstruction : DefinitionInstruction
	{
		public ITypeReference ElementType { get; set; }
		public uint Rank { get; set; }
		public IList<Variable> LowerBounds { get; private set; }
		public IList<Variable> Sizes { get; private set; }

		public CreateArrayInstruction(uint label, Variable result, ITypeReference elementType, uint rank, IEnumerable<Variable> lowerBounds, IEnumerable<Variable> sizes)
			: base(label, result)
		{
			this.ElementType = elementType;
			this.Rank = rank;
			this.LowerBounds = new List<Variable>(lowerBounds);
			this.Sizes = new List<Variable>(sizes);
		}

		public override ISet<Variable> UsedVariables
		{
			get
			{
				var result = new HashSet<Variable>();
				result.UnionWith(this.LowerBounds);
				result.UnionWith(this.Sizes);
				return result;
			}
		}

		public override void Replace(Variable oldvar, Variable newvar)
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

		public override IExpression ToExpression()
		{
			return new CreateArrayExpression(this.ElementType, this.Rank, this.LowerBounds, this.Sizes);
		}

		public override string ToString()
		{
			var elementType = TypeHelper.GetTypeName(this.ElementType);
			var sizes = string.Join(", ", this.Sizes);

			return string.Format("{0}:  {1} = new {2}[{3}];", this.Label, this.Result, elementType, sizes);
		}
	}

	public class PhiInstruction : DefinitionInstruction
	{
		public IList<Variable> Arguments { get; private set; }

		public PhiInstruction(uint label, Variable result)
			: base(label, result)
		{
			this.Arguments = new List<Variable>();
		}

		public override ISet<Variable> UsedVariables
		{
			get { return new HashSet<Variable>(this.Arguments); }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Result.Equals(oldvar)) this.Result = newvar;

			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
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
