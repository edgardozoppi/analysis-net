// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Backend.Analyses;
using Model.Types;
using Model;
using Backend.Model;

namespace Backend.Serialization
{
	// TODO: Check this https://gist.github.com/azukipochette/4644291
	// using Microsoft.VisualStudio.GraphModel;
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
				default:
					result = string.Join(Environment.NewLine, node.Instructions);
					result = string.Format("Node ID: {0}{1}{2}", node.Id, Environment.NewLine, result);
					break;
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
					var nodeVariables = ptg.GetVariables(node);

					foreach (var variable in nodeVariables)
					{
						var sourceId = variable.Name;

						xmlWriter.WriteStartElement("Link");
						xmlWriter.WriteAttributeString("Source", sourceId);
						xmlWriter.WriteAttributeString("Target", targetId);
						xmlWriter.WriteEndElement();
					}

					var fieldsBySource = from e in ptg.GetSources(node)
										 from s in e.Value
										 group e.Key by s into g
										 select g;

					foreach (var g in fieldsBySource)
					{
						var sourceId = Convert.ToString(g.Key.Id);
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
				default:
					result = string.Format("Node ID: {0}{1}{2}", node.Id, Environment.NewLine, node.Type);
					break;
			}

			return result;
		}

		#endregion

		#region Call Graph

		public static string Serialize(CallGraph cg)
		{
			using (var stringWriter = new StringWriter())
			using (var xmlWriter = new XmlTextWriter(stringWriter))
			{
				var reachableMethods = new Dictionary<IMethodReference, int>(MethodReferenceDefinitionComparer.Default);

				xmlWriter.Formatting = Formatting.Indented;
				xmlWriter.WriteStartElement("DirectedGraph");
				xmlWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/vs/2009/dgml");
				xmlWriter.WriteStartElement("Nodes");

				foreach (var method in cg.Methods)
				{
					reachableMethods.Add(method, reachableMethods.Count);
				}

				foreach (var method in cg.Roots)
				{
					var methodId = reachableMethods[method];
					var nodeId = Convert.ToString(methodId);
					//var label = string.Format("{0}.{1}", method.ContainingType.Name, method.Name);
					var label = method.ToSignatureString();

					xmlWriter.WriteStartElement("Node");
					xmlWriter.WriteAttributeString("Id", nodeId);
					xmlWriter.WriteAttributeString("Label", label);
					xmlWriter.WriteAttributeString("Background", "Yellow");
					xmlWriter.WriteEndElement();
				}

				var otherMethods = cg.Methods.Except(cg.Roots);

				foreach (var method in otherMethods)
				{
					var methodId = reachableMethods[method];
					var nodeId = Convert.ToString(methodId);
					//var label = string.Format("{0}.{1}", method.ContainingType.Name, method.Name);
					var label = method.ToSignatureString();

					xmlWriter.WriteStartElement("Node");
					xmlWriter.WriteAttributeString("Id", nodeId);
					xmlWriter.WriteAttributeString("Label", label);

					if (method.ResolvedMethod == null)
					{
						// Unknown method
						xmlWriter.WriteAttributeString("Background", "#FFB445");
						xmlWriter.WriteAttributeString("StrokeDashArray", "6,6");
					}

					xmlWriter.WriteEndElement();
				}

				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("Links");

				foreach (var entry in reachableMethods)
				{
					var sourceId = Convert.ToString(entry.Value);
					var invocationsPerCallee = from inv in cg.GetInvocations(entry.Key)
											   from callee in inv.PossibleCallees
											   group inv by callee into g
											   select g;

					foreach (var invocations in invocationsPerCallee)
					{
						var calleeId = reachableMethods[invocations.Key];
						var targetId = Convert.ToString(calleeId);
						var label = string.Join("\n", invocations.Select(inv => inv.Label));

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

		#endregion

		#region Class Hierarchy

		public static string Serialize(ClassHierarchy ch)
		{
			using (var stringWriter = new StringWriter())
			using (var xmlWriter = new XmlTextWriter(stringWriter))
			{
				var allDefinedTypes = new Dictionary<IBasicType, int>(BasicTypeDefinitionComparer.Default);

				xmlWriter.Formatting = Formatting.Indented;
				xmlWriter.WriteStartElement("DirectedGraph");
				xmlWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/vs/2009/dgml");
				xmlWriter.WriteStartElement("Nodes");

				foreach (var type in ch.Types)
				{
					allDefinedTypes.Add(type, allDefinedTypes.Count);
				}

				foreach (var entry in allDefinedTypes)
				{
					var nodeId = Convert.ToString(entry.Value);
					var label = entry.Key.GenericName;

					xmlWriter.WriteStartElement("Node");
					xmlWriter.WriteAttributeString("Id", nodeId);
					xmlWriter.WriteAttributeString("Label", label);
					xmlWriter.WriteEndElement();
				}

				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("Links");

				foreach (var entry in allDefinedTypes)
				{
					var sourceId = Convert.ToString(entry.Value);
					var subtypes = ch.GetSubtypes(entry.Key);

					foreach (var subtype in subtypes)
					{
						var subtypeId = allDefinedTypes[subtype];
						var targetId = Convert.ToString(subtypeId);

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

		#endregion

		#region Type Graph

		public static string Serialize(Host host, ITypeDefinition type)
		{
			return DGMLSerializer.Serialize(host, type.ToEnumerable());
		}

		public static string Serialize(Host host, Assembly assembly)
		{
			var types = assembly.RootNamespace.GetAllTypes();
			return DGMLSerializer.Serialize(host, types);
		}

		private static string Serialize(Host host, IEnumerable<ITypeDefinition> types)
		{
			using (var stringWriter = new StringWriter())
			using (var xmlWriter = new XmlTextWriter(stringWriter))
			{
				var allReferencedTypes = new Dictionary<IBasicType, int>();
				var allDefinedTypes = new Dictionary<ITypeDefinition, int>();
				var visitedTypes = new HashSet<ITypeDefinition>();
				var newTypes = new HashSet<ITypeDefinition>();

				xmlWriter.Formatting = Formatting.Indented;
				xmlWriter.WriteStartElement("DirectedGraph");
				xmlWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/vs/2009/dgml");
				xmlWriter.WriteStartElement("Links");

				foreach (var type in types)
				{
					allDefinedTypes.Add(type, allDefinedTypes.Count);
					newTypes.Add(type);
				}

				while (newTypes.Count > 0)
				{
					var type = newTypes.First();
					newTypes.Remove(type);
					visitedTypes.Add(type);

					var typeId = allDefinedTypes[type];
					var sourceId = string.Format("d{0}", typeId);
					var targetId = string.Empty;

					var fieldsByType = from m in type.Members
									   let f = m as FieldDefinition
									   where f != null && f.Type is IBasicType
									   let ftype = f.Type as IBasicType
									   group f by ftype into g
									   select g;

					foreach (var g in fieldsByType)
					{
						var fieldTypeRef = g.Key;
						var fieldTypeDef = host.ResolveReference(g.Key);

						if (fieldTypeDef == null)
						{
							if (!allReferencedTypes.ContainsKey(fieldTypeRef))
							{
								allReferencedTypes.Add(fieldTypeRef, allReferencedTypes.Count);
							}

							typeId = allReferencedTypes[fieldTypeRef];
							targetId = string.Format("r{0}", typeId);
						}
						else
						{
							if (!allDefinedTypes.ContainsKey(fieldTypeDef))
							{
								allDefinedTypes.Add(fieldTypeDef, allDefinedTypes.Count);
							}

							typeId = allDefinedTypes[fieldTypeDef];
							targetId = string.Format("d{0}", typeId);

							if (!visitedTypes.Contains(fieldTypeDef))
							{
								newTypes.Add(fieldTypeDef);
							}
						}

						var label = DGMLSerializer.GetLabel(g);

						xmlWriter.WriteStartElement("Link");
						xmlWriter.WriteAttributeString("Source", sourceId);
						xmlWriter.WriteAttributeString("Target", targetId);
						xmlWriter.WriteAttributeString("Label", label);
						xmlWriter.WriteEndElement();
					}
				}

				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("Nodes");

				foreach (var entry in allReferencedTypes)
				{
					var typeId = string.Format("r{0}", entry.Value);
					var label = entry.Key.GenericName;

					xmlWriter.WriteStartElement("Node");
					xmlWriter.WriteAttributeString("Id", typeId);
					xmlWriter.WriteAttributeString("Label", label);
					xmlWriter.WriteEndElement();
				}

				foreach (var entry in allDefinedTypes)
				{
					var typeId = string.Format("d{0}", entry.Value);
					var label = entry.Key.GenericName;

					xmlWriter.WriteStartElement("Node");
					xmlWriter.WriteAttributeString("Id", typeId);
					xmlWriter.WriteAttributeString("Label", label);
					xmlWriter.WriteEndElement();
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

		#endregion

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

		private static string GetLabel(IEnumerable<PTGNodeField> fields)
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
