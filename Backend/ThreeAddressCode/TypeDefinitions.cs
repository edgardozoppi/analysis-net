using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.ThreeAddressCode
{
	public interface ITypeDefinition
	{
		string Name { get; set; }
	}

	public interface IValueTypeDefinition : ITypeDefinition
	{
	}

	public interface IReferenceTypeDefinition : ITypeDefinition
	{
	}

	public class StructDefinition : IValueTypeDefinition
	{
		public string Name { get; set; }
		public IList<Variable> TypeParameters { get; private set; }
		public IDictionary<string, FieldDefinition> Fields { get; private set; }
		public IDictionary<string, MethodDefinition> Methods { get; private set; }

		public StructDefinition(string name)
		{
			this.Name = name;
			this.TypeParameters = new List<Variable>();
			this.Fields = new Dictionary<string, FieldDefinition>();
			this.Methods = new Dictionary<string, MethodDefinition>();
		}
	}

	public class FieldDefinition
	{
		public IType Type { get; set; }
		public string Name { get; set; }

		public FieldDefinition(string name, IType type)
		{
			this.Name = name;
			this.Type = type;
		}
	}

	public class MethodDefinition
	{
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public IList<Variable> TypeParameters { get; private set; }
		public IList<Variable> Parameters { get; private set; }

		public MethodDefinition(string name, IType returnType)
		{
			this.Name = name;
			this.ReturnType = returnType;
			this.TypeParameters = new List<Variable>();
			this.Parameters = new List<Variable>();
		}
	}

	public class EnumDefinition : IValueTypeDefinition
	{
		public string Name { get; set; }
		public PrimitiveType UnderlayingType { get; set; }
		public IDictionary<string, Constant> Constants { get; private set; }

		public EnumDefinition(string name)
		{
			this.Name = name;
			this.Constants = new Dictionary<string, Constant>();
		}
	}

	public class InterfaceDefinition : IReferenceTypeDefinition
	{
		public string Name { get; set; }
		public IList<InterfaceDefinition> Interfaces { get; private set; }
		public IList<Variable> TypeParameters { get; private set; }
		public IDictionary<string, MethodDefinition> Methods { get; private set; }

		public InterfaceDefinition(string name)
		{
			this.Name = name;
			this.Interfaces = new List<InterfaceDefinition>();
			this.TypeParameters = new List<Variable>();
			this.Methods = new Dictionary<string, MethodDefinition>();
		}
	}
	
	public class ClassDefinition : IReferenceTypeDefinition
	{
		public string Name { get; set; }
		public ClassDefinition Base { get; set; }
		public IList<InterfaceDefinition> Interfaces { get; private set; }
		public IList<Variable> TypeParameters { get; private set; }
		public IDictionary<string, FieldDefinition> Fields { get; private set; }
		public IDictionary<string, MethodDefinition> Methods { get; private set; }

		public ClassDefinition(string name)
		{
			this.Name = name;
			this.Interfaces = new List<InterfaceDefinition>();
			this.TypeParameters = new List<Variable>();
			this.Fields = new Dictionary<string, FieldDefinition>();
			this.Methods = new Dictionary<string, MethodDefinition>();
		}
	}
}
