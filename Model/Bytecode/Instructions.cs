using Model.ThreeAddressCode.Values;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Bytecode
{
	public enum BasicOperation
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
		Ge,
		Throw,
		Rethrow,
		Not,
		Neg,
		Nop,
		Pop,
		Dup,
		EndFinally,
		EndFilter,
		LocalAllocation,
		InitBlock,
		InitObject,
		CopyObject,
		CopyBlock,
		LoadArrayLength,
		IndirectLoad,
		LoadArrayElementAddress,
		IndirectStore,
		StoreArrayElement,
		Breakpoint,
		Return,
		LoadArrayElement
	}

	public enum BranchOperation
	{
		False,
		True,
		Eq,
		Neq,
		Lt,
		Le,
		Gt,
		Ge,
		Branch,
		Leave
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

	public enum LoadOperation
	{
		Value,
		Content,
		Address
	}

	public enum LoadFieldOperation
	{
		Content,
		Address
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
	}

	public class BasicInstruction : Instruction
	{
		public BasicOperation Operation { get; set; }
		public bool OverflowCheck { get; set; }
		public bool UnsignedOperands { get; set; }

		public BasicInstruction(uint label, BasicOperation operation)
			: base(label)
		{
			this.Operation = operation;
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1};", this.Label, this.Operation);
		}
	}

	public class BranchInstruction : Instruction
	{
		public BranchOperation Operation { get; set; }
		public string Target { get; set; }
		public bool UnsignedOperands { get; set; }

		public BranchInstruction(uint label, BranchOperation operation, uint target)
			: base(label)
		{
			this.Target = string.Format("L_{0:X4}", target);
			this.Operation = operation;
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1};", this.Label, this.Operation);
		}
	}

	public class SwitchInstruction : Instruction
	{
		public IList<string> Targets { get; private set; }

		public SwitchInstruction(uint label, IEnumerable<uint> targets)
			: base(label)
		{
			this.Targets = targets.Select(target => string.Format("L_{0:X4}", target)).ToList();
		}

		public override string ToString()
		{
			var targets = string.Join(", ", this.Targets);
			return string.Format("{0}:  switch {1};", this.Label, targets);
		}
	}

	public class CreateArrayInstruction : Instruction
	{
		public ArrayType Type { get; set; }

		public CreateArrayInstruction(uint label, ArrayType type)
			: base(label)
		{
			this.Type = type;
		}

		public override string ToString()
		{
			return string.Format("{0}:  new {1};", this.Label, this.Type);
		}
	}

	public class CreateObjectInstruction : Instruction
	{
		public IMethodReference Constructor { get; set; }

		public CreateObjectInstruction(uint label, IMethodReference constructor)
			: base(label)
		{
			this.Constructor = constructor;
		}

		public override string ToString()
		{
			return string.Format("{0}:  new {1} with {2};", this.Label, this.Constructor.ContainingType, this.Constructor);
		}
	}

	public class MethodCallInstruction : Instruction
	{
		public MethodCallOperation Operation { get; set; }
		public IMethodReference Method { get; set; }

		public MethodCallInstruction(uint label, MethodCallOperation operation, IMethodReference method)
			: base(label)
		{
			this.Operation = operation;
			this.Method = method;
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1} {2};", this.Label, this.Operation, this.Method);
		}
	}

	public class IndirectMethodCallInstruction : Instruction
	{
		public FunctionPointerType Function { get; set; }

		public IndirectMethodCallInstruction(uint label, FunctionPointerType function)
			: base(label)
		{
			this.Function = function;
		}

		public override string ToString()
		{
			return string.Format("{0}: indirect call {1};", this.Label, this.Function);
		}
	}

	public class SizeofInstruction : Instruction
	{
		public IType MeasuredType { get; set; }

		public SizeofInstruction(uint label, IType measuredType)
			: base(label)
		{
			this.MeasuredType = measuredType;
		}

		public override string ToString()
		{
			return string.Format("{0}:  sizeof {1};", this.Label, this.MeasuredType);
		}
	}

	public class LoadInstruction : Instruction
	{
		public LoadOperation Operation { get; set; }
		public IInmediateValue Operand { get; set; }

		public LoadInstruction(uint label, LoadOperation operation, IInmediateValue operand)
			: base(label)
		{
			this.Operation = operation;
			this.Operand = operand;
		}

		public override string ToString()
		{
			return string.Format("{0}:  load {1} of {2};", this.Label, this.Operation, this.Operand);
		}
	}

	public class LoadFieldInstruction : Instruction
	{
		public LoadFieldOperation Operation { get; set; }
		public IFieldReference Field { get; set; }

		public LoadFieldInstruction(uint label, LoadFieldOperation operation, IFieldReference field)
			: base(label)
		{
			this.Operation = operation;
			this.Field = field;
		}

		public override string ToString()
		{
			return string.Format("{0}:  load {1} of {2};", this.Label, this.Operation, this.Field);
		}
	}

	public class LoadMethodAddressInstruction : Instruction
	{
		public IMethodReference Method { get; set; }

		public LoadMethodAddressInstruction(uint label, IMethodReference method)
			: base(label)
		{
			this.Method = method;
		}

		public override string ToString()
		{
			return string.Format("{0}:  load address of {2};", this.Label, this.Method);
		}
	}

	public class LoadTokenInstruction : Instruction
	{
		public IMetadataReference Token { get; set; }

		public LoadTokenInstruction(uint label, IMetadataReference token)
			: base(label)
		{
			this.Token = token;
		}

		public override string ToString()
		{
			return string.Format("{0}:  load token {1};", this.Label, this.Token);
		}
	}

	public class StoreInstruction : Instruction
	{
		public IVariable Operand { get; set; }

		public StoreInstruction(uint label, IVariable operand)
			: base(label)
		{
			this.Operand = operand;
		}

		public override string ToString()
		{
			return string.Format("{0}:  store {1};", this.Label, this.Operand);
		}
	}

	public class StoreFieldInstruction : Instruction
	{
		public IFieldReference Field { get; set; }

		public StoreFieldInstruction(uint label, IFieldReference field)
			: base(label)
		{
			this.Field = field;
		}

		public override string ToString()
		{
			return string.Format("{0}:  store {1};", this.Label, this.Field);
		}
	}

	public class ConvertInstruction : Instruction
	{
		public ConvertOperation Operation { get; set; }
		public IType ConversionType { get; set; }
		public bool OverflowCheck { get; set; }
		public bool UnsignedOperands { get; set; }

		public ConvertInstruction(uint label, ConvertOperation operation, IType conversionType)
			: base(label)
		{
			this.Operation = operation;
			this.ConversionType = conversionType;
		}

		public override string ToString()
		{
			return string.Format("{0}:  {1} {2} as {3};", this.Label, this.Operation ,this.ConversionType);
		}
	}

}
