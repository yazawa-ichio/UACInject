using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
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
		CodeTargetAttributeInfo m_TargetAttributeInfo;

		public int Priority => m_TargetAttributeInfo.Priority;

		public int ArgumentPriority => m_ArgumentInfos.Length;

		public bool HasResult => m_ArgumentInfos.Any(x => x.IsResult);

		public TypeReference ResultParameterType => m_ArgumentInfos.First(x => x.IsResult).ParameterType;

		public InjectMethod(Logger logger, CodeTargetAttributeInfo targetAttributeInfo, CodeType codeType, MethodDefinition method)
		{
			m_Logger = logger;
			m_TargetAttributeInfo = targetAttributeInfo;
			m_CodeType = codeType;
			m_Method = method;
			m_ArgumentInfos = method.Parameters.Select(x => new ArgumentInfo(logger, x)).ToArray();
			if (!method.IsStatic)
			{
				m_Error = true;
				logger.Error($"MethodAttribute target static only. {method.FullName}");
			}
			// エラーにするべきか？
			if (!method.IsPublic)
			{
				method.IsPublic = true;
			}
			if (m_ArgumentInfos.Count(x => x.IsResult) > 1)
			{
				logger.Error($"[ResultAttribute] is once only. {method.FullName}");
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
					InjectExecute(callerType, callerMethod, attr);
					break;
				case CodeType.ReturnCondition:
					InjectReturnCondition(callerType, callerMethod, attr);
					break;
				case CodeType.Scope:
					InjectScope(callerType, callerMethod, attr);
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
				if (!arg.Match(callerType, callerMethod, attr))
				{
					return false;
				}
			}

			return true;
		}

		void InjectExecute(TypeDefinition callerType, MethodDefinition callerMethod, CodeInjectAttributeInfo attr)
		{
			var processor = callerMethod.Body.GetILProcessor();
			processor.Body.SimplifyMacros();
			Instruction target = callerMethod.Body.Instructions.First();

			foreach (var arg in m_ArgumentInfos)
			{
				foreach (var item in arg.CreateInstruction(callerType, callerMethod, attr))
				{
					processor.InsertBefore(target, item);
				}
			}

			processor.InsertBefore(target, Instruction.Create(OpCodes.Call, callerMethod.Module.ImportReference(m_Method)));

			if (m_Method.ReturnType != null && m_Method.ReturnType.FullName != "System.Void")
			{
				processor.InsertBefore(target, Instruction.Create(OpCodes.Pop));
			}
			processor.Body.OptimizeMacros();
		}

		void InjectReturnCondition(TypeDefinition callerType, MethodDefinition callerMethod, CodeInjectAttributeInfo attr)
		{
			m_Logger.Debug($"callerMethod:{callerMethod}");
			m_Logger.Debug($"callerMethod.ReturnType:{callerMethod.ReturnType.Resolve()}");
			m_Logger.Debug($"callerMethod.DeclaringType:{callerMethod.ReturnType.Resolve().BaseType}");
			if (callerMethod.Body == null)
			{
				callerMethod.Body = new MethodBody(callerMethod);
			}
			var processor = callerMethod.Body.GetILProcessor();
			processor.Body.SimplifyMacros();

			Instruction target = callerMethod.Body.Instructions.FirstOrDefault();

			VariableDefinition result = null;

			foreach (var arg in m_ArgumentInfos)
			{
				if (arg.IsResult)
				{
					callerMethod.Body.Variables.Add(result = new VariableDefinition(arg.ParameterType.Resolve()));
					foreach (var item in arg.SetReturnConditionResultInstruction(callerType, callerMethod, attr, result))
					{
						processor.InsertBefore(target, item);
					}
				}
				else
				{
					foreach (var item in arg.CreateInstruction(callerType, callerMethod, attr))
					{
						processor.InsertBefore(target, item);
					}
				}
			}

			processor.InsertBefore(target, Instruction.Create(OpCodes.Call, callerMethod.Module.ImportReference(m_Method)));
			var ret = Instruction.Create(OpCodes.Ret);
			processor.InsertBefore(target, ret);
			processor.InsertBefore(ret, Instruction.Create(OpCodes.Brfalse_S, target));

			if (ret.Next != null && processor.Body.ExceptionHandlers.Count > 0)
			{
				if (processor.Body.ExceptionHandlers[0].TryStart == ret.Next)
				{
					processor.InsertAfter(ret, Instruction.Create(OpCodes.Nop));
				}
			}

			if (!callerMethod.ReturnType.IsVoid())
			{
				var type = callerMethod.Module.ImportReference(callerMethod.ReturnType.Resolve()).Resolve();
				if (result != null && result.VariableType.CanToCast(callerMethod.ReturnType.FullName))
				{
					processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, result));
				}
				else if (callerMethod.ReturnType.FullName == "System.Threading.Tasks.Task")
				{
					var method = callerMethod.Module.ImportReference(type.Methods.First(x => x.Name == "get_CompletedTask"));
					processor.InsertBefore(ret, Instruction.Create(OpCodes.Call, method));
				}
				else if (callerMethod.ReturnType.FullName == "Cysharp.Threading.Tasks.UniTask")
				{
					var method = callerMethod.Module.ImportReference(type.Fields.First(x => x.Name == "CompletedTask"));
					processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldsfld, method));
				}
				else if (callerMethod.ReturnType.FullName == "System.Threading.Tasks.ValueTask")
				{
					var taskType = callerMethod.Module.ImportReference(typeof(System.Threading.Tasks.Task)).Resolve();
					var method = callerMethod.Module.ImportReference(taskType.Methods.First(x => x.Name == "get_CompletedTask"));
					processor.InsertBefore(ret, Instruction.Create(OpCodes.Call, method));
					var constructor = callerMethod.ReturnType.Resolve().GetConstructors().First(x => x.Parameters.Count == 1);
					processor.InsertBefore(ret, Instruction.Create(OpCodes.Newobj, callerMethod.Module.ImportReference(constructor)));
				}
				else
				{
					var resolveType = callerMethod.ReturnType.Resolve();
					if (resolveType.FullName == "System.Threading.Tasks.Task`1")
					{
						processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, result));
						var baseType = callerMethod.Module.ImportReference(resolveType.BaseType).Resolve();
						var method = new GenericInstanceMethod(callerMethod.Module.ImportReference(baseType.Methods.First(x => x.Name == "FromResult")));
						method.GenericArguments.Add(result.VariableType);
						processor.InsertBefore(ret, Instruction.Create(OpCodes.Call, callerMethod.Module.ImportReference(method)));
					}
					else if (resolveType.FullName == "Cysharp.Threading.Tasks.UniTask`1")
					{
						processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, result));
						var uniTaskType = resolveType.Module.GetType("Cysharp.Threading.Tasks.UniTask");
						var baseType = callerMethod.Module.ImportReference(uniTaskType).Resolve();
						var method = new GenericInstanceMethod(callerMethod.Module.ImportReference(baseType.Methods.First(x => x.Name == "FromResult")));
						method.GenericArguments.Add(result.VariableType);
						processor.InsertBefore(ret, Instruction.Create(OpCodes.Call, callerMethod.Module.ImportReference(method)));
					}
					else if (resolveType.FullName == "System.Threading.Tasks.ValueTask`1")
					{
						processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, result));
						var valueType = new GenericInstanceType(resolveType)
						{
							GenericArguments = { result.VariableType },
						};
						var methodBase = valueType.Resolve().GetConstructors().First(x => x.Parameters.Count == 1 && x.Parameters[0].ParameterType.FullName == "TResult");
						var method = callerMethod.Module.ImportReference(MakeHostInstanceGeneric(methodBase, result.VariableType));
						processor.InsertBefore(ret, Instruction.Create(OpCodes.Newobj, method));
					}
				}
			}

			processor.Body.OptimizeMacros();

		}

		MethodReference MakeHostInstanceGeneric(MethodReference self, TypeReference arg)
		{
			var declaringType = new GenericInstanceType(self.DeclaringType) { GenericArguments = { arg } };
			var reference = new MethodReference(self.Name, self.ReturnType, declaringType)
			{
				HasThis = self.HasThis,
				ExplicitThis = self.ExplicitThis,
				CallingConvention = self.CallingConvention
			};
			foreach (var parameter in self.Parameters)
			{
				reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
			}
			foreach (var genericParam in self.GenericParameters)
			{
				reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
			}
			return reference;
		}


		void InjectScope(TypeDefinition callerType, MethodDefinition callerMethod, CodeInjectAttributeInfo attr)
		{

			var processor = callerMethod.Body.GetILProcessor();

			processor.Body.SimplifyMacros();

			var end = processor.Body.Instructions.Last();
			var ret = processor.Body.Instructions.LastOrDefault(x => x.OpCode == OpCodes.Ret);
			var holdLastRet = end == ret && processor.Body.Instructions.Count != 1 && processor.Body.ExceptionHandlers.Count > 0;
			Instruction tryStart = callerMethod.Body.Instructions.First();

			foreach (var arg in m_ArgumentInfos)
			{
				foreach (var item in arg.CreateInstruction(callerType, callerMethod, attr))
				{
					processor.InsertBefore(tryStart, item);
				}
			}

			processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Call, callerType.Module.ImportReference(m_Method)));
			var variable = new VariableDefinition(callerType.Module.ImportReference(m_Method.ReturnType));
			callerMethod.Body.Variables.Add(variable);
			processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Stloc, variable));

			var tryStartPrev = tryStart.Previous;

			VariableDefinition retVariable = null;

			if (ret != null && !callerMethod.ReturnType.IsVoid())
			{
				end = end.Previous;
				if (end.OpCode == OpCodes.Ldloc)
				{
					holdLastRet = true;
				}
				else
				{
					retVariable = new VariableDefinition(callerType.Module.ImportReference(callerMethod.ReturnType));
					callerMethod.Body.Variables.Add(retVariable);
				}
			}

			var finallyInstructions = GetScopeFinallyInstruction(callerType, variable, !holdLastRet && ret != null, retVariable);

			foreach (var instruction in finallyInstructions)
			{
				if (holdLastRet)
				{
					processor.InsertBefore(end, instruction);
				}
				else
				{
					processor.Append(instruction);
				}
			}

			if (!holdLastRet && ret != null)
			{
				foreach (var replaceRet in processor.Body.Instructions.Where(x => x.OpCode == OpCodes.Ret).ToArray())
				{
					if (finallyInstructions.Contains(replaceRet))
					{
						continue;
					}

					var last = finallyInstructions.Last();
					if (retVariable != null)
					{
						last = last.Previous;
						var leave = Instruction.Create(OpCodes.Leave, last);
						processor.Replace(replaceRet, leave);
						processor.InsertBefore(leave, Instruction.Create(OpCodes.Stloc, retVariable));
					}
					else
					{
						processor.Replace(replaceRet, Instruction.Create(OpCodes.Leave, last));
					}
				}
			}

			foreach (var replaceRet in processor.Body.Instructions.Where(x => x.OpCode == OpCodes.Br).ToArray())
			{
				if (holdLastRet)
				{
					if (replaceRet.Operand == end)
					{
						processor.Replace(replaceRet, Instruction.Create(OpCodes.Leave, end));
					}
				}
				else if (replaceRet.Operand is Instruction target)
				{
					if (target.OpCode == OpCodes.Ret || (target.OpCode != OpCodes.Ret && target.Next != null && target.Next.OpCode == OpCodes.Ret))
					{
						var last = finallyInstructions.Last();
						processor.Replace(replaceRet, Instruction.Create(OpCodes.Leave, last));
					}
				}
			}

			foreach (var handler in processor.Body.ExceptionHandlers)
			{
				if (handler.HandlerEnd == null || handler.HandlerEnd == end)
				{
					handler.HandlerEnd = finallyInstructions[0];
				}
			}

			processor.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
			{
				TryStart = tryStartPrev.Next,
				TryEnd = finallyInstructions[0],
				HandlerStart = finallyInstructions[0],
				HandlerEnd = finallyInstructions.Last(x => x.OpCode == OpCodes.Endfinally).Next,
			});

			processor.Body.OptimizeMacros();
		}

		Instruction[] GetScopeFinallyInstruction(TypeDefinition callerType, VariableDefinition variable, bool ret, VariableDefinition variableDefinition)
		{
			var finallyInstruction = new List<Instruction>();
			var endFinally = Instruction.Create(OpCodes.Endfinally);
			if (!m_Method.ReturnType.IsValueType)
			{
				finallyInstruction.Add(Instruction.Create(OpCodes.Ldloc, variable));
				finallyInstruction.Add(Instruction.Create(OpCodes.Brfalse_S, endFinally));
				finallyInstruction.Add(Instruction.Create(OpCodes.Ldloc, variable));
				var disposeMethod = callerType.Module.ImportReference(typeof(System.IDisposable).GetMethod("Dispose"));
				finallyInstruction.Add(Instruction.Create(OpCodes.Callvirt, disposeMethod));
				finallyInstruction.Add(endFinally);
			}
			else
			{
				finallyInstruction.Add(Instruction.Create(OpCodes.Ldloca, variable));
				finallyInstruction.Add(Instruction.Create(OpCodes.Constrained, callerType.Module.ImportReference(m_Method.ReturnType)));
				var disposeMethod = callerType.Module.ImportReference(typeof(System.IDisposable).GetMethod("Dispose"));
				finallyInstruction.Add(Instruction.Create(OpCodes.Callvirt, disposeMethod));
				finallyInstruction.Add(endFinally);
			}
			if (variableDefinition != null)
			{
				finallyInstruction.Add(Instruction.Create(OpCodes.Ldloc, variableDefinition));
			}
			if (ret)
			{
				finallyInstruction.Add(Instruction.Create(OpCodes.Ret));
			}
			return finallyInstruction.ToArray();
		}

	}

}