using Backend.Analisis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Serialization
{
	public static class DOTSerializer
	{
		public static string Serialize(CFGNode node)
		{
			string result;

			switch (node.Kind)
			{
				case CFGNodeKind.Enter: result = "enter"; break;
				case CFGNodeKind.Exit: result = "exit"; break;
				case CFGNodeKind.BasicBlock: result = string.Join("\\l", node.Instructions) + "\\l"; break;
				default: throw new Exception("Unknown Control Flow Graph node kind: " + node.Kind);
			}

			return result;
		}

		public static string Serialize(ControlFlowGraph cfg)
		{
			var sb = new StringBuilder();
			sb.AppendLine("digraph ControlFlow\n{");
			sb.AppendLine("\tnode[shape=\"rect\"];");

			foreach (var node in cfg.Nodes)
			{
				var label = DOTSerializer.Serialize(node);
				sb.AppendFormat("\t{0}[label=\"{1}\"];\n", node.Id, label);

				foreach (var successor in node.Successors)
				{
					sb.AppendFormat("\t{0} -> {1};\n", node.Id, successor.Id);
				}
			}

			sb.AppendLine("}");
			return sb.ToString();
		}
	}
}
