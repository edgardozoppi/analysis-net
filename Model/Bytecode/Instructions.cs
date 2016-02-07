using Model.ThreeAddressCode.Values;
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
		Return
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
		Branch
	}

	public enum ConvertOperation
	{
		Conv,
		Cast,
		Box,
		Unbox,
		UnboxPtr
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

}
