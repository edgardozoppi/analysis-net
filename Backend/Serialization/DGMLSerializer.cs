using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Backend.Analysis;
using Model.Types;

namespace Backend.Serialization
{
	public static class DGMLSerializer
	{
		#region Control-Flow Graph

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

				xmlWriter.WriteStartElement("Setter");
				xmlWriter.WriteAttributeString("Property", "NodeRadius");
				xmlWriter.WriteAttributeString("Value", "5");
				xmlWriter.WriteEndElement();

				xmlWriter.WriteStartElement("Setter");
				xmlWriter.WriteAttributeString("Property", "MinWidth");
				xmlWriter.WriteAttributeString("Value", "0");
				xmlWriter.WriteEndElement();

				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
				xmlWriter.Flush();
				return stringWriter.ToString();
			}
		}

		private static string Serialize(CFGNode node)
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

		#endregion

		#region Points-To Graph

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
					xmlWriter.WriteAttributeString("Shape", "None");
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
						xmlWriter.WriteAttributeString("Background", "Yellow");
					}
					else if (node.Kind == PTGNodeKind.Unknown)
					{
						xmlWriter.WriteAttributeString("Background", "#FFB445");
						xmlWriter.WriteAttributeString("StrokeDashArray", "6,6");
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

					var fieldsBySource = from e in node.Sources
										 from s in e.Value
										 group e.Key by s into g
										 select g;

					foreach (var g in fieldsBySource)
					{
						var sourceId = Convert.ToString(g.Key);
						var label = DGMLSerializer.GetLabel(g);

						xmlWriter.WriteStartElement("Link");
						xmlWriter.WriteAttributeString("Source", sourceId);
						xmlWriter.WriteAttributeString("Target", targetId);
						xmlWriter.WriteAttributeString("Label", label);
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

				xmlWriter.WriteStartElement("Setter");
				xmlWriter.WriteAttributeString("Property", "NodeRadius");
				xmlWriter.WriteAttributeString("Value", "5");
				xmlWriter.WriteEndElement();

				xmlWriter.WriteStartElement("Setter");
				xmlWriter.WriteAttributeString("Property", "MinWidth");
				xmlWriter.WriteAttributeString("Value", "0");
				xmlWriter.WriteEndElement();

				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
				xmlWriter.Flush();
				return stringWriter.ToString();
			}
		}

		private static string Serialize(PTGNode node)
		{
			string result;

			switch (node.Kind)
			{
				case PTGNodeKind.Null: result = "null"; break;
				default: result = node.Type.ToString(); break;
			}

			return result;
		}

		#endregion

		//TODO: Fix this code
		//#region Type Graph

		//public static string Serialize(IType type)
		//{
		//	var types = new IType[] { type };
		//	return DGMLSerializer.Serialize(types);
		//}

		//public static string Serialize(IModule module)
		//{
		//	var types = module.GetAllTypes();
		//	return DGMLSerializer.Serialize(types);
		//}

		//private static string Serialize(IEnumerable<IType> types)
		//{
		//	using (var stringWriter = new StringWriter())
		//	using (var xmlWriter = new XmlTextWriter(stringWriter))
		//	{
		//		var allTypes = new Dictionary<IType, int>();
		//		var visitedTypes = new HashSet<IType>();
		//		var newTypes = new HashSet<IType>();

		//		xmlWriter.Formatting = Formatting.Indented;
		//		xmlWriter.WriteStartElement("DirectedGraph");
		//		xmlWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/vs/2009/dgml");
		//		xmlWriter.WriteStartElement("Links");

		//		foreach (var type in types)
		//		{
		//			allTypes.Add(type, allTypes.Count);

		//			if (type is INamedTypeDefinition)
		//			{
		//				var namedType = type as INamedTypeDefinition;
		//				newTypes.Add(namedType);
		//			}
		//		}

		//		while (newTypes.Count > 0)
		//		{
		//			var type = newTypes.First();
		//			newTypes.Remove(type);
		//			visitedTypes.Add(type);

		//			var typeId = allTypes[type];
		//			var sourceId = Convert.ToString(typeId);

		//			var fieldsByType = from f in type.Fields
		//							   group f by f.Type into g
		//							   select g;

		//			foreach (var g in fieldsByType)
		//			{
		//				if (g.Key is INamedTypeReference)
		//				{
		//					var fieldType = g.Key as INamedTypeReference;

		//					if (!allTypes.ContainsKey(fieldType))
		//					{
		//						allTypes.Add(fieldType, allTypes.Count);
		//					}

		//					if (fieldType is INamedTypeDefinition)
		//					{
		//						var namedFieldType = fieldType as INamedTypeDefinition;

		//						if (!visitedTypes.Contains(namedFieldType))
		//						{
		//							newTypes.Add(namedFieldType);
		//						}
		//					}

		//					typeId = allTypes[fieldType];
		//					var targetId = Convert.ToString(typeId);
		//					var label = DGMLSerializer.GetLabel(g);

		//					xmlWriter.WriteStartElement("Link");
		//					xmlWriter.WriteAttributeString("Source", sourceId);
		//					xmlWriter.WriteAttributeString("Target", targetId);
		//					xmlWriter.WriteAttributeString("Label", label);
		//					xmlWriter.WriteEndElement();
		//				}
		//			}
		//		}

		//		xmlWriter.WriteEndElement();
		//		xmlWriter.WriteStartElement("Nodes");

		//		foreach (var entry in allTypes)
		//		{
		//			var typeId = Convert.ToString(entry.Value);
		//			var label = TypeHelper.GetTypeName(entry.Key);

		//			xmlWriter.WriteStartElement("Node");
		//			xmlWriter.WriteAttributeString("Id", typeId);
		//			xmlWriter.WriteAttributeString("Label", label);
		//			xmlWriter.WriteEndElement();
		//		}

		//		xmlWriter.WriteEndElement();
		//		xmlWriter.WriteStartElement("Styles");
		//		xmlWriter.WriteStartElement("Style");
		//		xmlWriter.WriteAttributeString("TargetType", "Node");

		//		xmlWriter.WriteStartElement("Setter");
		//		xmlWriter.WriteAttributeString("Property", "FontFamily");
		//		xmlWriter.WriteAttributeString("Value", "Consolas");
		//		xmlWriter.WriteEndElement();

		//		xmlWriter.WriteStartElement("Setter");
		//		xmlWriter.WriteAttributeString("Property", "NodeRadius");
		//		xmlWriter.WriteAttributeString("Value", "5");
		//		xmlWriter.WriteEndElement();

		//		xmlWriter.WriteStartElement("Setter");
		//		xmlWriter.WriteAttributeString("Property", "MinWidth");
		//		xmlWriter.WriteAttributeString("Value", "0");
		//		xmlWriter.WriteEndElement();

		//		xmlWriter.WriteEndElement();
		//		xmlWriter.WriteEndElement();
		//		xmlWriter.WriteEndElement();
		//		xmlWriter.Flush();
		//		return stringWriter.ToString();
		//	}
		//}

		//#endregion

		#region Private Methods

		private static string GetLabel(IEnumerable<IFieldReference> fields)
		{
			var result = new StringBuilder();

			foreach (var field in fields)
			{
				result.Append(field.Name);
				result.AppendLine();
			}

			result.Remove(result.Length - 2, 2);
			return result.ToString();
		}

		#endregion
	}
}
