using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace UACInject.CodeGen
{
	class ArgumentInfo
	{
		public enum ArgumentType
		{
			Default,
			CallerArgument,
			CallerField,
			CallerMethodName,
			CallerInstance,
		}

		static readonly string s_CallerArgument = "UACInject.CallerArgumentAttribute";
		static readonly string s_CallerField = "UACInject.CallerFieldAttribute";
		static readonly string s_CallerInstance = "UACInject.CallerInstanceAttribute";
		static readonly string s_CallerMethodName = "UACInject.CallerMethodNameAttribute";
		static readonly string s_ArgumentValueAttribute = "UACInject.ArgumentValueAttribute";

		public TypeReference ParameterType { get; private set; }

		public string Name { get; private set; }

		public ArgumentType Type { get; private set; } = ArgumentType.Default;

		Logger m_Logger;

		public ArgumentInfo(Logger logger, ParameterDefinition parameter)
		{
			m_Logger = logger;
			ParameterType = parameter.ParameterType;
			Name = parameter.Name;
			if (TryGetAttribute(parameter, s_CallerArgument, out var attr))
			{
				Type = ArgumentType.CallerArgument;
				Name = attr.ConstructorArguments[0].Value.ToString();
			}
			else if (TryGetAttribute(parameter, s_CallerField, out attr))
			{
				Type = ArgumentType.CallerField;
				Name = attr.ConstructorArguments[0].Value.ToString();
			}
			else if (TryGetAttribute(parameter, s_CallerMethodName, out attr))
			{
				Type = ArgumentType.CallerMethodName;
				if (!ParameterType.CanToCast("System.String"))
				{
					m_Logger.Warning($"CallerMethodNameAttribute is string only. {parameter.Name}");
				}
			}
			else if (TryGetAttribute(parameter, s_CallerInstance, out attr))
			{
				Type = ArgumentType.CallerInstance;
			}
		}

		bool TryGetAttribute(ParameterDefinition parameter, string name, out CustomAttribute attribute)
		{
			attribute = parameter.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == name);
			return attribute != null;
		}

		public bool Match(TypeDefinition callerType, MethodDefinition callerMethod)
		{
			m_Logger.Debug($"Match {Name} {Type}");
			switch (Type)
			{
				case ArgumentType.CallerField:
					{
						var field = callerType.Fields.FirstOrDefault(x => x.Name == Name);
						if (field != null && field.FieldType.CanToCast(ParameterType.Resolve().FullName))
						{
							return true;
						}
						return false;
					}
				case ArgumentType.CallerMethodName:
					{
						return ParameterType.CanToCast("System.String");
					}
				case ArgumentType.CallerInstance:
					{
						if (callerType.CanToCast(ParameterType.FullName))
						{
							return true;
						}
						return false;
					}
				case ArgumentType.CallerArgument:
				case ArgumentType.Default:
				default:
					{
						var param = callerMethod.Parameters.FirstOrDefault(x => x.Name == Name);
						if (param != null && param.ParameterType.CanToCast(ParameterType.FullName))
						{
							return true;
						}
						foreach (var attr in callerMethod.CustomAttributes.Where(x => x.AttributeType.CanToCast(s_ArgumentValueAttribute)))
						{
							var name = attr.ConstructorArguments[0].Value.ToString();
							var value = attr.ConstructorArguments[1].Value;
							m_Logger.Debug($"attr {ParameterType.FullName} {value.GetType().FullName}");
							if (name == Name && ParameterType.CanToCast(value.GetType().FullName))
							{
								return true;
							}
						}
						return false;
					}
			}
		}

		public IEnumerable<Instruction> CreateInstruction(TypeDefinition callerType, MethodDefinition callerMethod)
		{
			m_Logger.Debug($"ArgumentInfo {Name}");
			switch (Type)
			{
				case ArgumentType.CallerArgument:
				case ArgumentType.Default:
					return SetCallerArgumentInstruction(callerType, callerMethod);
				case ArgumentType.CallerField:
					return SetCallerFieldInstruction(callerType);
				case ArgumentType.CallerInstance:
					return SetCallerInstanceInstruction();
				case ArgumentType.CallerMethodName:
					return SetCallerMethodNameInstruction(callerMethod);
			}
			return Enumerable.Empty<Instruction>();
		}

		public IEnumerable<Instruction> SetCallerArgumentInstruction(TypeDefinition callerType, MethodDefinition callerMethod)
		{
			for (int i = 0; i < callerMethod.Parameters.Count; i++)
			{
				var parameter = callerMethod.Parameters[i];
				if (parameter.Name == Name)
				{
					switch (i)
					{
						case 0:
							yield return Instruction.Create(OpCodes.Ldarg_1);
							break;
						case 1:
							yield return Instruction.Create(OpCodes.Ldarg_2);
							break;
						case 2:
							yield return Instruction.Create(OpCodes.Ldarg_3);
							break;
						default:
							yield return Instruction.Create(OpCodes.Ldarg_S, parameter.Name);
							break;
					}
					yield break;
				}
			}
			foreach (var attr in callerMethod.CustomAttributes.Where(x => x.AttributeType.CanToCast(s_ArgumentValueAttribute)))
			{
				var name = attr.ConstructorArguments[0].Value.ToString();
				var value = attr.ConstructorArguments[1].Value;
				m_Logger.Debug($"attr {ParameterType.FullName} {value.GetType().FullName}");
				if (name == Name && ParameterType.CanToCast(value.GetType().FullName))
				{
					if (value is int)
					{
						yield return SetIntValue((int)value);
						break;
					}
					else if (value is float)
					{
						yield return Instruction.Create(OpCodes.Ldc_R4, (float)value);
						break;
					}
					else if (value is string)
					{
						yield return Instruction.Create(OpCodes.Ldstr, (string)value);
						break;
					}
				}
			}
		}

		Instruction SetIntValue(int value)
		{
			switch (value)
			{
				case -1:
					return Instruction.Create(OpCodes.Ldc_I4_M1);
				case 0:
					return Instruction.Create(OpCodes.Ldc_I4_0);
				case 1:
					return Instruction.Create(OpCodes.Ldc_I4_1);
				case 2:
					return Instruction.Create(OpCodes.Ldc_I4_2);
				case 3:
					return Instruction.Create(OpCodes.Ldc_I4_3);
				case 4:
					return Instruction.Create(OpCodes.Ldc_I4_4);
				case 5:
					return Instruction.Create(OpCodes.Ldc_I4_5);
				case 6:
					return Instruction.Create(OpCodes.Ldc_I4_6);
				case 7:
					return Instruction.Create(OpCodes.Ldc_I4_7);
				case 8:
					return Instruction.Create(OpCodes.Ldc_I4_8);
				default:
					if (value >= -128 && value < 128)
					{
						return Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)value);
					}
					else
					{
						return Instruction.Create(OpCodes.Ldc_I4, value);
					}
			}
		}

		public IEnumerable<Instruction> SetCallerFieldInstruction(TypeDefinition callerType)
		{
			var field = callerType.Fields.First(x => x.Name == Name);
			yield return Instruction.Create(OpCodes.Ldarg_0);
			if (ParameterType.IsByReference)
			{
				yield return Instruction.Create(OpCodes.Ldflda, field);
			}
			else
			{
				yield return Instruction.Create(OpCodes.Ldfld, field);
			}
		}

		public IEnumerable<Instruction> SetCallerInstanceInstruction()
		{
			yield return Instruction.Create(OpCodes.Ldarg_0);
		}

		public IEnumerable<Instruction> SetCallerMethodNameInstruction(MethodDefinition callerMethod)
		{
			yield return Instruction.Create(OpCodes.Ldstr, callerMethod.Name);
		}


	}

}