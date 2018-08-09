using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShenzhenMod.Patching
{
    public class MethodPatcher
    {
        private readonly MemberMap m_memberMap;

        public MethodPatcher(MemberMap memberMap)
        {
            m_memberMap = memberMap;
        }

        public MethodDefinition InjectMethod(MethodDefinition sourceMethod, TypeDefinition targetType)
        {
            var targetMethod = new MethodDefinition(sourceMethod.Name, sourceMethod.Attributes, m_memberMap.ConvertType(sourceMethod.ReturnType));
            targetType.Methods.Add(targetMethod);

            foreach (var sourceParam in sourceMethod.Parameters)
            {
                var targetParam = new ParameterDefinition(sourceParam.Name, sourceParam.Attributes, m_memberMap.ConvertType(sourceParam.ParameterType));
                targetParam.Constant = sourceParam.Constant;
                targetMethod.Parameters.Add(targetParam);
            }

            // TODO: Handle recursion

            ReplaceMethod(sourceMethod, targetMethod);

            return targetMethod;
        }

        public void ReplaceMethod(MethodDefinition source, MethodDefinition target)
        {
            MethodBody body = target.Body;

            body.Instructions.Clear();
            foreach (var instruction in source.Body.Instructions)
            {
                var newInstr = Instruction.Create(OpCodes.Nop);
                newInstr.Set(instruction.OpCode, ConvertOperand(instruction.Operand));
                body.Instructions.Add(newInstr);
            }

            body.Variables.Clear();
            foreach (var variable in source.Body.Variables)
            {
                body.Variables.Add(new VariableDefinition(m_memberMap.ConvertType(variable.VariableType)));
            }

            body.ExceptionHandlers.Clear();
            foreach (var exHandler in source.Body.ExceptionHandlers)
            {
                body.ExceptionHandlers.Add(exHandler);
            }

            // TODO: Optimize instructions?
        }

        private object ConvertOperand(object operand)
        {
            switch (operand)
            {
                case TypeReference type:
                    return m_memberMap.ConvertType(type);
                case MethodReference method:
                    return m_memberMap.ConvertMethod(method);
                case FieldReference field:
                    return m_memberMap.ConvertField(field);
                default:
                    return operand;
            }
        }
    }
}
