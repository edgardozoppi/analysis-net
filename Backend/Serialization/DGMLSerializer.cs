using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Backend.Analysis;

namespace Backend.Serialization
{
	public static class DGMLSerializer
	{
		public static string Serialize(CFGNode node)
		{
			string result;

			switch (node.Kind)
			{
				case CFGNodeKind.Entry: result = "entry"; break;
				case CFGNodeKind.Exit: result = "exit"; break;
				default: result = string.Join(Environment.NewLine, node.Instructions); break;
			}

			return result;
		}

		public static string Serialize(ControlFlowGraph cfg)
		{
			using (var stringWriter = new StringWriter())
			using (var xmlWriter = new XmlTextWriter(stringWriter))
			{
				xmlWriter.Formatting = Formatting.Indented;
				xmlWriter.WriteStartElement("DirectedGraph");
				xmlWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/vs/2009/dgml");
				xmlWriter.WriteStartElement("Nodes");

				foreach (var node in cfg.Nodes)
				{
					var nodeId = Convert.ToString(node.Id);
					var label = DGMLSerializer.Serialize(node);

					xmlWriter.WriteStartElement("Node");
					xmlWriter.WriteAttributeString("Id", nodeId);
					xmlWriter.WriteAttributeString("Label", label);

					if (node.Kind == CFGNodeKind.Entry ||
						node.Kind == CFGNodeKind.Exit)
					{
						xmlWriter.WriteAttributeString("Background", "Yellow");
					}

					xmlWriter.WriteEndElement();
				}

				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("Links");

				foreach (var node in cfg.Nodes)
				{
					foreach (var successor in node.Successors)
					{
						var sourceId = Convert.ToString(node.Id);
						var targetId = Convert.ToString(successor.Id);

						xmlWriter.WriteStartElement("Link");
						xmlWriter.WriteAttributeString("Source", sourceId);
						xmlWriter.WriteAttributeString("Target", targetId);
						xmlWriter.WriteEndElement();
					}
				}

				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("Styles");
				xmlWriter.WriteStartElement("Style");
				xmlWriter.WriteAttributeString("TargetType", "Node");

				xmlWriter.WriteStartElement("Setter");
				xmlWriter.WriteAttributeString("Property", "FontFamily");
				xmlWriter.WriteAttributeString("Value", "Consolas");
				xmlWriter.WriteEndElement();

				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
				xmlWriter.Flush();
				return stringWriter.ToString();
			}
		}
	}
}
