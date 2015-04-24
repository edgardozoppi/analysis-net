using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Backend.Analysis;
using Microsoft.Cci;

namespace Backend.Serialization
{
	public static class DGMLSerializer
	{
		#region Control-Flow Graph

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
					var sourceId = Convert.ToString(node.Id);

					foreach (var successor in node.Successors)
					{
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

		#endregion

		#region Points-To Graph

		public static string Serialize(PTGNode node)
		{
			string result;

			switch (node.Kind)
			{
				case PTGNodeKind.Null: result = "null"; break;
				default: result = TypeHelper.GetTypeName(node.Type); break;
			}

			return result;
		}

		public static string Serialize(PointsToGraph ptg)
		{
			using (var stringWriter = new StringWriter())
			using (var xmlWriter = new XmlTextWriter(stringWriter))
			{
				xmlWriter.Formatting = Formatting.Indented;
				xmlWriter.WriteStartElement("DirectedGraph");
				xmlWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/vs/2009/dgml");
				xmlWriter.WriteStartElement("Nodes");

				foreach (var variable in ptg.Variables)
				{
					var label = variable.Name;

					xmlWriter.WriteStartElement("Node");
					xmlWriter.WriteAttributeString("Id", label);
					xmlWriter.WriteAttributeString("Label", label);
					xmlWriter.WriteAttributeString("Background", "Yellow");
					xmlWriter.WriteEndElement();
				}

				foreach (var node in ptg.Nodes)
				{
					var nodeId = Convert.ToString(node.Id);
					var label = DGMLSerializer.Serialize(node);

					xmlWriter.WriteStartElement("Node");
					xmlWriter.WriteAttributeString("Id", nodeId);
					xmlWriter.WriteAttributeString("Label", label);

					if (node.Kind == PTGNodeKind.Null)
					{
						xmlWriter.WriteAttributeString("Background", "Red");
					}

					xmlWriter.WriteEndElement();
				}

				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("Links");

				foreach (var node in ptg.Nodes)
				{
					var targetId = Convert.ToString(node.Id);

					foreach (var variable in node.Variables)
					{
						var sourceId = variable.Name;

						xmlWriter.WriteStartElement("Link");
						xmlWriter.WriteAttributeString("Source", sourceId);
						xmlWriter.WriteAttributeString("Target", targetId);
						xmlWriter.WriteEndElement();
					}

					foreach (var entry in node.Sources)
					{
						var label = MemberHelper.GetMemberSignature(entry.Key, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);

						foreach (var source in entry.Value)
						{
							var sourceId = Convert.ToString(source.Id);

							xmlWriter.WriteStartElement("Link");
							xmlWriter.WriteAttributeString("Source", sourceId);
							xmlWriter.WriteAttributeString("Target", targetId);
							xmlWriter.WriteAttributeString("Label", label);
							xmlWriter.WriteEndElement();
						}
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

		#endregion
	}
}
