// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;
using Model;
using Backend.Model;
using Model.ThreeAddressCode.Visitor;

namespace Backend.Analyses
{
	// Intraprocedural May Points-To Analysis
    public class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToGraph>
    {
		public delegate PointsToGraph ProcessMethodCallDelegate(IMethodReference caller, MethodCallInstruction methodCall, IDictionary<IBasicType, PTGNode> globalNodes, UniqueIDGenerator nodeIdGenerator, PointsToGraph input);

		#region class TransferFunction

		private class TransferFunction : InstructionVisitor
		{
			private IMethodReference method;
			private UniqueIDGenerator nodeIdGenerator;
			private IDictionary<uint, PTGNode> nodeAtOffset;
			private IDictionary<IBasicType, PTGNode> globalNodes;
			private IDictionary<IVariable, IFunctionReference> functions;
			private PointsToGraph ptg;

			public TransferFunction(IMethodReference method, IDictionary<IBasicType, PTGNode> globalNodes, UniqueIDGenerator nodeIdGenerator)
			{
				this.method = method;
				this.globalNodes = globalNodes;
				this.nodeIdGenerator = nodeIdGenerator;
				this.nodeAtOffset = new Dictionary<uint, PTGNode>();
				this.functions = new Dictionary<IVariable, IFunctionReference>();
			}

			public Func<IType, bool> IsScalarType;
			public ProcessMethodCallDelegate ProcessMethodCall;

			public PointsToGraph Evaluate(CFGNode node, PointsToGraph input)
			{
				//ptg = input.Clone();
				this.ptg = input;
				Visit(node);
				var result = this.ptg;
				this.ptg = null;
				return result;
			}

			#region Visit methods

			public override void Visit(CreateObjectInstruction instruction)
			{
				ProcessAllocation(instruction.Offset, instruction.Result);
			}

			public override void Visit(CreateArrayInstruction instruction)
			{
				ProcessAllocation(instruction.Offset, instruction.Result);
			}

			public override void Visit(ConvertInstruction instruction)
			{
				// TODO: Simulate the Boxing operation by creating a new node to hold the scalar value.
				// TODO: Support the Unboxing operation.
				ProcessCopy(instruction.Result, instruction.Operand);
			}

			public override void Visit(LoadInstruction instruction)
			{
				var operand = instruction.Operand;

				if (operand is Reference)
				{
					// TODO: Hack! We are ignoring the reference and treating it
					// as a regular load (instead of an indirect load).
					var reference = operand as Reference;

					if (reference.Value is IVariable)
					{
						operand = reference.Value as IVariable;
					}
					else if (reference.Value is StaticFieldAccess)
					{
						operand = reference.Value as StaticFieldAccess;
					}
					else if (reference.Value is InstanceFieldAccess)
					{
						operand = reference.Value as InstanceFieldAccess;
					}
				}

				if (operand is Constant)
				{
					var constant = operand as Constant;

					if (constant.Value == null)
					{
						ProcessNull(instruction.Result);
					}
					else if (constant.Value is string)
					{
						ProcessString(instruction.Offset, instruction.Result);
					}
				}
				else if (operand is IVariable)
				{
					var variable = operand as IVariable;
					ProcessCopy(instruction.Result, variable);
				}
				else if (operand is StaticFieldAccess)
				{
					var access = operand as StaticFieldAccess;
					ProcessLoad(instruction.Offset, instruction.Result, access);
				}
				else if (operand is InstanceFieldAccess)
				{
					var access = operand as InstanceFieldAccess;
					ProcessLoad(instruction.Offset, instruction.Result, access);
				}
				else if (operand is ArrayElementAccess)
				{
					var access = operand as ArrayElementAccess;
					ProcessLoad(instruction.Offset, instruction.Result, access);
				}
				else if (operand is IFunctionReference)
				{
					var function = operand as IFunctionReference;
					functions[instruction.Result] = function;
				}
			}

			public override void Visit(StoreInstruction instruction)
			{
				if (instruction.Result is StaticFieldAccess)
				{
					var access = instruction.Result as StaticFieldAccess;
					ProcessStore(access, instruction.Operand);
				}
				else if (instruction.Result is InstanceFieldAccess)
				{
					var access = instruction.Result as InstanceFieldAccess;
					ProcessStore(access, instruction.Operand);
				}
				else if (instruction.Result is ArrayElementAccess)
				{
					var access = instruction.Result as ArrayElementAccess;
					ProcessStore(access, instruction.Operand);
				}
			}

			public override void Visit(PhiInstruction instruction)
			{
				ProcessPhi(instruction.Result, instruction.Arguments);
			}

			public override void Visit(ReturnInstruction instruction)
			{
				if (instruction.HasOperand)
				{
					ProcessReturn(instruction.Operand);
				}
			}

			public override void Visit(MethodCallInstruction instruction)
			{
				PointsToGraph output = null;

				if (ProcessMethodCall != null)
				{
					output = ProcessMethodCall(method, instruction, globalNodes, nodeIdGenerator, ptg);
				}

				if (output == null)
				{
					// Method call not processed.
					if (instruction.HasResult)
					{
						DefaultProcessMethodCall(instruction.Offset, instruction.Result);
					}
					else if (instruction.Method.Name == ".ctor" && instruction.Method.ContainingType.IsDelegate())
					{
						// Delegate constructor call.
						ProcessDelegateConstructorCall(instruction);
					}
				}
				else
				{
					ptg = output;
				}
			}

			private void ProcessDelegateConstructorCall(MethodCallInstruction instruction)
			{
				var receiver = instruction.Arguments.First();
				var functionArg = instruction.Arguments.Last();
				var function = functions[functionArg];
				var nodes = ptg.GetTargets(receiver).OfType<PTGDelegateNode>();

				foreach (var node in nodes)
				{
					node.Target = function;
				}

				if (instruction.Arguments.Count == 3)
				{
					var field = new PTGNodeField("instance", PlatformTypes.Object);
					var instance = instruction.Arguments[1];
					var targets = ptg.GetTargets(instance);

					foreach (var node in nodes)
					{
						foreach (var target in targets)
						{
							ptg.PointsTo(node, field, target);
						}
					}
				}
			}

			#endregion

			#region Private methods

			private void ProcessNull(IVariable dst)
			{
				if (IsScalarType(dst.Type)) return;

				ptg.RemoveEdges(dst);
				ptg.PointsTo(dst, ptg.Null);
			}

			private void ProcessString(uint offset, IVariable dst)
			{
				if (IsScalarType(dst.Type)) return;

				// TODO: Maybe we can avoid creating a new node for each string literal and use
				// only one unique global node to represent all string literals (like Null node)

				// Create a new dummy node to represent the string
				var node = GetOrCreateNode(offset, dst.Type, PTGNodeKind.Object);

				ptg.RemoveEdges(dst);
				ptg.PointsTo(dst, node);
			}

			private void ProcessReturn(IVariable src)
			{
				if (IsScalarType(src.Type)) return;

				// Weak update to preserve all possible return values
				var targets = ptg.GetTargets(src);

				foreach (var target in targets)
				{
					ptg.PointsTo(ptg.ResultVariable, target);
				}
			}

			private void ProcessAllocation(uint offset, IVariable dst)
			{
				if (IsScalarType(dst.Type)) return;

				var kind = PTGNodeKind.Object;

				if (dst.Type.IsDelegate())
				{
					kind = PTGNodeKind.Delegate;
				}

				var node = GetOrCreateNode(offset, dst.Type, kind);

				ptg.RemoveEdges(dst);
				ptg.PointsTo(dst, node);
			}

			private void DefaultProcessMethodCall(uint offset, IVariable dst)
			{
				if (IsScalarType(dst.Type)) return;

				// If the callee is was not processed we should return an
				// unknown node representing the result of the method call. 
				var targets = ptg.GetTargets(dst);

				if (targets.Count == 0)
				{
					var node = GetOrCreateNode(offset, dst.Type, PTGNodeKind.Unknown);

					ptg.RemoveEdges(dst);
					ptg.PointsTo(dst, node);
				}
			}

			private void ProcessCopy(IVariable dst, IVariable src)
			{
				if (IsScalarType(dst.Type) || IsScalarType(src.Type)) return;

				// Avoid the following case:
				// v = v
				// Otherwise we will need to copy the
				// targets before calling RemoveEdges.
				if (dst.Equals(src)) return;

				var targets = ptg.GetTargets(src);
				ptg.RemoveEdges(dst);

				foreach (var target in targets)
				{
					ptg.PointsTo(dst, target);
				}
			}

			private void ProcessPhi(IVariable dst, IEnumerable<IVariable> srcs)
			{
				if (IsScalarType(dst.Type)) return;
				var allTargets = new HashSet<PTGNode>();

				foreach (var src in srcs)
				{
					if (IsScalarType(src.Type)) continue;

					var targets = ptg.GetTargets(src);
					allTargets.UnionWith(targets);
				}
				
				ptg.RemoveEdges(dst);

				foreach (var target in allTargets)
				{
					ptg.PointsTo(dst, target);
				}
			}

			private void ProcessLoad(uint offset, IVariable dst, StaticFieldAccess access)
			{
				// We want to create the static/global node even when the field have a value type
				var src = GetGlobalVariable(access);

				if (IsScalarType(dst.Type) || IsScalarType(access.Type)) return;

				var field = access.ToPTGNodeField();
				ProcessLoad(offset, dst, src, field);
			}

			private void ProcessLoad(uint offset, IVariable dst, InstanceFieldAccess access)
			{
				if (IsScalarType(dst.Type) || IsScalarType(access.Type)) return;

				var field = access.ToPTGNodeField();
				ProcessLoad(offset, dst, access.Instance, field);
			}

			private void ProcessLoad(uint offset, IVariable dst, ArrayElementAccess access)
			{
				if (IsScalarType(dst.Type) || IsScalarType(access.Type)) return;

				var field = access.ToPTGNodeField();
				ProcessLoad(offset, dst, access.Array, field);
			}

			private void ProcessLoad(uint offset, IVariable dst, IVariable src, PTGNodeField field)
			{
				IEnumerable<PTGNode> nodes = ptg.GetTargets(src);

				// We need to copy the targets before calling
				// RemoveEdges because of the following cases:
				// v = v.f
				// where dst == src == v
				if (dst.Equals(src))
				{
					nodes = nodes.ToArray();
				}

				ptg.RemoveEdges(dst);

				foreach (var node in nodes)
				{
					// Null node cannot points-to other nodes.
					// We don't want to create an unknown node to be the target of null (null -field-> unknown).
					// TODO: Maybe we can simulate throwing a null reference exception.
					if (node.Kind == PTGNodeKind.Null) continue;

					var nodeTargets = ptg.GetTargets(node);
					var hasField = nodeTargets.ContainsKey(field);

					// TODO: Don't create an unknown node when doing the inter PT analysis
					if (!hasField)
					{
						var target = GetOrCreateNode(offset, dst.Type, PTGNodeKind.Unknown);

						ptg.PointsTo(node, field, target);
					}

					var targets = nodeTargets[field];

					foreach (var target in targets)
					{
						ptg.PointsTo(dst, target);
					}
				}
			}

			private void ProcessStore(StaticFieldAccess access, IVariable src)
			{
				// We want to create the static/global node even when the field have a value type
				var dst = GetGlobalVariable(access);

				if (IsScalarType(access.Type) || IsScalarType(src.Type)) return;
				
				var field = access.ToPTGNodeField();
				ProcessStore(dst, src, field);
			}

			private void ProcessStore(InstanceFieldAccess access, IVariable src)
			{
				if (IsScalarType(access.Type) || IsScalarType(src.Type)) return;

				var field = access.ToPTGNodeField();
				ProcessStore(access.Instance, src, field);
			}

			private void ProcessStore(ArrayElementAccess access, IVariable src)
			{
				if (IsScalarType(access.Type) || IsScalarType(src.Type)) return;

				var field = access.ToPTGNodeField();
				ProcessStore(access.Array, src, field);
			}

			private void ProcessStore(IVariable dst, IVariable src, PTGNodeField field)
			{
				// Weak update
				var nodes = ptg.GetTargets(dst);
				var targets = ptg.GetTargets(src);

				// There should be at least one node in nodes.

				foreach (var node in nodes)
				{
					// Null node cannot points-to other nodes.
					// TODO: Maybe we can simulate throwing a null reference exception.
					if (node.Kind == PTGNodeKind.Null) continue;

					foreach (var target in targets)
					{
						ptg.PointsTo(node, field, target);
					}
				}
			}

			private PTGNode GetOrCreateNode(uint offset, IType type, PTGNodeKind kind)
			{
				PTGNode node;

				var ok = nodeAtOffset.TryGetValue(offset, out node);

				if (!ok)
				{
					// Create a new node
					var nodeId = nodeIdGenerator.Next;

					if (kind == PTGNodeKind.Delegate)
					{
						node = new PTGDelegateNode(nodeId, type, method, offset);
					}
					else
					{
						node = new PTGNode(nodeId, type, method, kind, offset);
					}

					ptg.Add(node);
					nodeAtOffset.Add(offset, node);
				}
				else if (!ptg.Contains(node))
				{
					ptg.Add(node);
				}

				return node;
			}

			private IVariable GetGlobalVariable(StaticFieldAccess access)
			{
				var variable = access.ToPTGGlobalVariable();
				var global = GetOrCreateGlobalNode(access.Field.ContainingType);

				ptg.PointsTo(variable, global);
				return variable;
			}

			private PTGNode GetOrCreateGlobalNode(IBasicType type)
			{
				PTGNode global;
				var ok = globalNodes.TryGetValue(type, out global);

				if (!ok)
				{
					// Create a new global/static node
					var nodeId = nodeIdGenerator.Next;
					global = new PTGNode(nodeId, type, null, PTGNodeKind.Global);

					globalNodes.Add(type, global);

					// TODO: Simulate an invocation to the static constructor.
					// Since this is the first time the global/static node is used
					// we should create a fake invocation to the static constructor
					// of "type" (if exists).
				}

				ptg.Add(global);
				return global;
			}

			#endregion
		}

		#endregion

		private PointsToGraph initialGraph;
		private UniqueIDGenerator nodeIdGenerator;
		private TransferFunction transferFunction;
		private IMethodReference method;

		public PointsToAnalysis(ControlFlowGraph cfg, IMethodReference method)
			: base(cfg)
		{
			var globalNodes = new Dictionary<IBasicType, PTGNode>();

			this.method = method;
			this.nodeIdGenerator = new UniqueIDGenerator(1);
			this.transferFunction = new TransferFunction(method, globalNodes, nodeIdGenerator);
			this.IsScalarType = DefaultIsScalarType;
		}

		public PointsToAnalysis(ControlFlowGraph cfg, IMethodReference method, IDictionary<IBasicType, PTGNode> globalNodes, UniqueIDGenerator nodeIdGenerator)
			: base(cfg)
		{
			this.method = method;
			this.nodeIdGenerator = nodeIdGenerator;
			this.transferFunction = new TransferFunction(method, globalNodes, nodeIdGenerator);
			this.IsScalarType = DefaultIsScalarType;
		}

		public Func<IType, bool> IsScalarType
		{
			get { return transferFunction.IsScalarType; }
			set { transferFunction.IsScalarType = value; }
		}

		public ProcessMethodCallDelegate ProcessMethodCall
		{
			get { return transferFunction.ProcessMethodCall; }
			set { transferFunction.ProcessMethodCall = value; }
		}

		public override DataFlowAnalysisResult<PointsToGraph>[] Analyze()
		{
			this.initialGraph = CreateInitialGraph();
			return base.Analyze();
		}

		public DataFlowAnalysisResult<PointsToGraph>[] Analyze(PointsToGraph ptg)
		{
			this.initialGraph = CreateInitialGraph(ptg);
			return base.Analyze();
		}

		protected override PointsToGraph InitialValue(CFGNode node)
        {
			return this.initialGraph;
        }

        protected override bool Compare(PointsToGraph left, PointsToGraph right)
        {
            return left.GraphEquals(right);
        }

        protected override PointsToGraph Join(PointsToGraph left, PointsToGraph right)
        {
			var result = left.Clone();
			result.Union(right);
			return result;
        }

        protected override PointsToGraph Flow(CFGNode node, PointsToGraph input)
        {
            input = input.Clone();
			var output = transferFunction.Evaluate(node, input);
            return output;
        }

		protected bool DefaultIsScalarType(IType type)
		{
			var result = type.TypeKind == TypeKind.ValueType;
			return result;
		}

		private PointsToGraph CreateInitialGraph()
		{
			var ptg = new PointsToGraph();
			// TODO: We are missing to add the parameters
			// that are not actually used in the cfg.
			var variables = cfg.GetVariables();

			foreach (var variable in variables)
			{
				if (IsScalarType(variable.Type)) continue;

				if (variable.IsParameter)
				{
					var isThisParameter = variable.Name == "this";
					var kind = isThisParameter ? PTGNodeKind.Object : PTGNodeKind.Unknown;
					var nodeId = nodeIdGenerator.Next;
					var node = new PTGNode(nodeId, variable.Type, method, kind);

					ptg.Add(node);
					ptg.PointsTo(variable, node);
				}
				else
				{
					ptg.Add(variable);
				}
			}

			if (!IsScalarType(method.ReturnType))
			{
				ptg.ResultVariable = new LocalVariable(PointsToGraph.ResultVariableName) { Type = method.ReturnType };
				ptg.Add(ptg.ResultVariable);
			}

			return ptg;
		}

		private PointsToGraph CreateInitialGraph(PointsToGraph ptg)
		{
			// Add all variables except parameters
			var variables = cfg.GetVariables();

			foreach (var variable in variables)
			{
				if (IsScalarType(variable.Type)) continue;

				if (!variable.IsParameter)
				{
					ptg.Add(variable);
				}
			}

			if (!IsScalarType(method.ReturnType))
			{
				ptg.ResultVariable = new LocalVariable(PointsToGraph.ResultVariableName) { Type = method.ReturnType };
				ptg.Add(ptg.ResultVariable);
			}
			else
			{
				//ptg.ResultVariable = null;
			}

			return ptg;
		}
    }
}
