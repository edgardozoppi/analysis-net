using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public interface IInstruction
	{
		uint Offset { get; set; }
		string Label { get; set; }
	}
}
