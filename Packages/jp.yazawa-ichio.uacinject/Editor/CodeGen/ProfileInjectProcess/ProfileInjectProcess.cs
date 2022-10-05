using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;

namespace UACInject.CodeGen
{

	class ProfileInjectProcess
	{
		Logger m_Logger;

		public ProfileInjectProcess(Logger logger)
		{
			m_Logger = logger;
		}

		public void Run(ModuleDefinition mainModule)
		{
			foreach (var typeDefinition in mainModule.Types)
			{
				Run(typeDefinition);
			}
		}

		void Run(TypeDefinition typeDefinition)
		{
			foreach (var method in typeDefinition.Methods)
			{
				if (method.CustomAttributes.Any(x => ProfileAttributeInfo.Is(x)))
				{
					Process(typeDefinition, method);
				}
			}
			foreach (var nest in typeDefinition.NestedTypes)
			{
				Run(nest);
			}
		}


		void Process(TypeDefinition typeDefinition, MethodDefinition method)
		{
			m_Logger.Debug($"Process InjectAttribute {method.FullName}");
			var attribute = method.CustomAttributes
				.Where(x => ProfileAttributeInfo.Is(x))
				.Select(x => new ProfileAttributeInfo(method, x))
				.First();

			var processor = method.Body.GetILProcessor();

			processor.Body.SimplifyMacros();

			var end = processor.Body.Instructions.Last();
			var ret = processor.Body.Instructions.LastOrDefault(x => x.OpCode == OpCodes.Ret);
			var holdLastRet = end == ret && processor.Body.Instructions.Count != 1 && processor.Body.ExceptionHandlers.Count > 0;
			Instruction tryStart = method.Body.Instructions.First();


			processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Ldstr, attribute.Name));
			if (!method.IsStatic && typeDefinition.CanToCast("UnityEngine.Object"))
			{
				processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Ldarg_0));
				processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Call, method.Module.ImportReference(typeof(Profiler).GetMethod("BeginSample", new Type[] { typeof(string), typeof(UnityEngine.Object) }))));
			}
			else
			{
				processor.InsertBefore(tryStart, Instruction.Create(OpCodes.Call, method.Module.ImportReference(typeof(Profiler).GetMethod("BeginSample", new Type[] { typeof(string) }))));
			}


			var tryStartPrev = tryStart.Previous;

			VariableDefinition retVariable = null;

			if (ret != null && !method.ReturnType.IsVoid())
			{
				end = end.Previous;
				if (end.OpCode == OpCodes.Ldloc)
				{
					holdLastRet = true;
				}
				else
				{
					retVariable = new VariableDefinition(method.Module.ImportReference(method.ReturnType));
					method.Body.Variables.Add(retVariable);
				}
			}

			var finallyInstructions = GetScopeFinallyInstruction(method, !holdLastRet && ret != null, retVariable);

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

		Instruction[] GetScopeFinallyInstruction(MethodDefinition method, bool ret, VariableDefinition variableDefinition)
		{
			var finallyInstruction = new List<Instruction>();
			var endFinally = Instruction.Create(OpCodes.Endfinally);

			finallyInstruction.Add(Instruction.Create(OpCodes.Call, method.Module.ImportReference(typeof(Profiler).GetMethod("EndSample", System.Type.EmptyTypes))));

			finallyInstruction.Add(endFinally);

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