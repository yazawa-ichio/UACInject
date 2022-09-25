using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace UACInject.CodeGen
{

	class InjectMethod
	{
		bool m_Error;
		Logger m_Logger;
		MethodDefinition m_Method;
		ArgumentInfo[] m_ArgumentInfos;
		CodeType m_CodeType;

		public int Priority => m_ArgumentInfos.Length;

		public InjectMethod(Logger logger, CodeType codeType, MethodDefinition method)
		{
			m_Logger = logger;
			m_CodeType = codeType;
			m_Method = method;
			m_ArgumentInfos = method.Parameters.Select(x => new ArgumentInfo(logger, x)).ToArray();
			if (!method.IsStatic)
			{
				m_Error = true;
				logger.Error($"MethodAttribute target static only. {method.FullName}");
			}
			switch (m_CodeType)
			{
				case CodeType.Execute:
					if (!method.ReturnType.IsVoid())
					{
						m_Error = true;
						logger.Error($"ExecuteMethodAttribute return void only. {method.FullName}");
					}
					break;
				case CodeType.ReturnCondition:
					if (!method.ReturnType.CanToCast("System.Boolean"))
					{
						m_Error = true;
						logger.Error($"ValidationMethodAttribute return boolean only. {method.FullName}");
					}
					break;
				case CodeType.Scope:
					if (!method.ReturnType.CanToCast("System.IDisposable"))
					{
						m_Error = true;
						logger.Error($"ScopeMethodAttribute return System.IDisposable only. {method.FullName}");
					}
					break;
			}
		}

		public bool Process(TypeDefinition callerType, MethodDefinition callerMethod, CodeInjectAttributeInfo attr)
		{
			if (!Match(callerType, callerMethod, attr))
			{
				return false;
			}

			switch (m_CodeType)
			{
				case CodeType.Execute:
					InjectExecute(callerType, callerMethod);
					break;
				case CodeType.ReturnCondition:
					InjectReturnCondition(callerType, callerMethod);
					break;
				case CodeType.Scope:
					InjectScope(callerType, callerMethod);
					break;
			}
			return true;
		}


		bool Match(TypeDefinition callerType, MethodDefinition callerMethod, CodeInjectAttributeInfo attr)
		{
			if (m_Error)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(attr.Method) && m_Method.Name != attr.Method)
			{
				return false;
			}

			foreach (var arg in m_ArgumentInfos)
			{
				if (!arg.Match(callerType, callerMethod))
				{
					return false;
				}
			}

			if (m_CodeType == CodeType.ReturnCondition)
			{
				if (!callerMethod.ReturnType.IsVoid())
				{
					m_Logger.Warning($"ReturnCondition is void only. {callerMethod.FullName}");
					return false;
				}
			}

			return true;
		}

		void InjectExecute(TypeDefinition callerType, MethodDefinition callerMethod)
		{
			Instruction target = callerMethod.Body.Instructions.First();
			var processor = callerMethod.Body.GetILProcessor();

			processor.InsertBefore(target, Instruction.Create(OpCodes.Nop));

			foreach (var arg in m_ArgumentInfos)
			{
				foreach (var item in arg.CreateInstruction(callerType, callerMethod))
				{
					processor.InsertBefore(target, item);
				}
			}

			processor.InsertBefore(target, Instruction.Create(OpCodes.Call, m_Method));

			if (m_Method.ReturnType != null && m_Method.ReturnType.FullName != "System.Void")
			{
				processor.InsertBefore(target, Instruction.Create(OpCodes.Pop));
			}

		}

		void InjectReturnCondition(TypeDefinition callerType, MethodDefinition callerMethod)
		{
			Instruction target = callerMethod.Body.Instructions.First();
			var processor = callerMethod.Body.GetILProcessor();

			processor.InsertBefore(target, Instruction.Create(OpCodes.Nop));

			foreach (var arg in m_ArgumentInfos)
			{
				foreach (var item in arg.CreateInstruction(callerType, callerMethod))
				{
					processor.InsertBefore(target, item);
				}
			}

			processor.InsertBefore(target, Instruction.Create(OpCodes.Call, m_Method));

			var variable = new VariableDefinition(m_Method.ReturnType);
			callerMethod.Body.Variables.Add(variable);

			switch (variable.Index)
			{
				case 0:
					processor.InsertBefore(target, Instruction.Create(OpCodes.Stloc_0));
					processor.InsertBefore(target, Instruction.Create(OpCodes.Ldloc_0));
					break;
				case 1:
					processor.InsertBefore(target, Instruction.Create(OpCodes.Stloc_1));
					processor.InsertBefore(target, Instruction.Create(OpCodes.Ldloc_1));
					break;
				case 2:
					processor.InsertBefore(target, Instruction.Create(OpCodes.Stloc_2));
					processor.InsertBefore(target, Instruction.Create(OpCodes.Ldloc_2));
					break;
				case 3:
					processor.InsertBefore(target, Instruction.Create(OpCodes.Stloc_3));
					processor.InsertBefore(target, Instruction.Create(OpCodes.Ldloc_3));
					break;
				default:
					processor.InsertBefore(target, Instruction.Create(OpCodes.Stloc_S, variable.Index));
					processor.InsertBefore(target, Instruction.Create(OpCodes.Ldloc_S, variable.Index));
					break;
			}

			processor.InsertBefore(target, Instruction.Create(OpCodes.Brfalse_S, target));
			processor.InsertBefore(target, Instruction.Create(OpCodes.Nop));

			var end = processor.Body.Instructions.Last(x => x.OpCode == OpCodes.Ret);
			if (!callerMethod.ReturnType.IsVoid())
			{
				end = end.Previous;
			}
			processor.InsertBefore(target, Instruction.Create(OpCodes.Br_S, end));

		}

		void InjectScope(TypeDefinition callerType, MethodDefinition callerMethod)
		{
			Instruction tryStart;
			Instruction handlerStart;

			var processor = callerMethod.Body.GetILProcessor();

			var end = processor.Body.Instructions.Last(x => x.OpCode == OpCodes.Ret);
			if (!callerMethod.ReturnType.IsVoid())
			{
				end = end.Previous;
			}
			tryStart = callerMethod.Body.Instructions.First();

			foreach (var instruction in processor.Body.Instructions.ToArray())
			{
				if (instruction.OpCode == OpCodes.Br_S && instruction.Operand == end)
				{
					processor.Replace(instruction, Instruction.Create(OpCodes.Leave_S, end));
				}
			}

			processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Nop));

			foreach (var arg in m_ArgumentInfos)
			{
				foreach (var item in arg.CreateInstruction(callerType, callerMethod))
				{
					processor.InsertBefore(tryStart, item);
				}
			}

			processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Call, m_Method));

			var variable = new VariableDefinition(m_Method.ReturnType);
			callerMethod.Body.Variables.Add(variable);

			switch (variable.Index)
			{
				case 0:
					processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Stloc_0));
					break;
				case 1:
					processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Stloc_1));
					break;
				case 2:
					processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Stloc_2));
					break;
				case 3:
					processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Stloc_3));
					break;
				default:
					processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Stloc_S, variable.Index));
					break;
			}

			Instruction leavePoint;
			if (end.Previous == null || end.Previous.OpCode != OpCodes.Leave_S)
			{
				processor.InsertBefore(end, leavePoint = Instruction.Create(OpCodes.Nop));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Leave_S, end));
			}
			else
			{
				leavePoint = end.Previous;
			}

			foreach (var exceptionHandlers in processor.Body.ExceptionHandlers)
			{
				var tmp = exceptionHandlers.TryStart;
				while (tmp != null && tmp != exceptionHandlers.TryEnd)
				{
					if (tmp.OpCode == OpCodes.Leave_S && tmp.Operand == end)
					{
						var instruction = Instruction.Create(OpCodes.Leave_S, leavePoint);
						processor.Replace(tmp, instruction);
					}
					tmp = tmp.Next;
				}
				if (exceptionHandlers.HandlerEnd == end)
				{
					var tmpEnd = exceptionHandlers.HandlerEnd;
					while (tmpEnd.OpCode != OpCodes.Endfinally)
					{
						tmpEnd = tmpEnd.Previous;
					}
					exceptionHandlers.HandlerEnd = tmpEnd.Next;
				}
			}

			if (variable.Index < 255)
			{
				processor.InsertBefore(end, Instruction.Create(OpCodes.Ldloca_S, variable));
			}
			else
			{
				processor.InsertBefore(end, Instruction.Create(OpCodes.Ldloca, variable));
			}

			handlerStart = end.Previous;
			processor.InsertBefore(end, Instruction.Create(OpCodes.Constrained, m_Method.ReturnType));
			var disposeMethod = callerType.Module.ImportReference(typeof(System.IDisposable).GetMethod("Dispose"));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, disposeMethod));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Nop));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Endfinally));

			processor.Body.ExceptionHandlers.Insert(0, new ExceptionHandler(ExceptionHandlerType.Finally)
			{
				TryStart = tryStart,
				TryEnd = handlerStart,
				HandlerStart = handlerStart,
				HandlerEnd = end,
			});



		}


	}

}