using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShenzhenMod
{
    public class ShenzhenPatcher : IDisposable
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ShenzhenPatcher));

        private ModuleDefinition m_module;

        public ShenzhenPatcher(string unpatchedPath)
        {
            sm_log.InfoFormat("Reading module \"{0}\"", unpatchedPath);
            m_module = ModuleDefinition.ReadModule(unpatchedPath);
        }

        public void Dispose()
        {
            if (m_module != null)
            {
                m_module.Dispose();
                m_module = null;
            }
        }

        public void ApplyPatch()
        {
            ChangeMaxPuzzleSize();
            ChangeScrollSize();
            AddSizeToPuzzle();
        }

        private void ChangeMaxPuzzleSize()
        {
            var method = m_module.FindMethod("#=qL_uhZp1CYmicXxDRy$c1Bw==", ".cctor");
            method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)22).Operand = (sbyte)44;
            method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)14).Operand = (sbyte)28;
        }

        private void ChangeScrollSize()
        {
            var method = m_module.FindMethod("#=qL_uhZp1CYmicXxDRy$c1Bw==", "#=qOM$GUjkyhx7zr2YetMuA92qBxgTzT0XpP5Hho_r9c2A=");
            method.FindInstruction(OpCodes.Ldc_R4, 1920f).Operand = 3840f;
            method.FindInstruction(OpCodes.Ldc_R4, 1080f).Operand = 2160f;
        }

        private void AddSizeToPuzzle()
        {
            var type = m_module.FindType("Puzzle");
            var index2DType = m_module.FindType("Index2");
            var sizeField = new FieldDefinition("size", FieldAttributes.Private, index2DType);
            type.Fields.Add(sizeField);
            var isSizeSetField = new FieldDefinition("isSizeSet", FieldAttributes.Private, m_module.TypeSystem.Boolean);
            type.Fields.Add(isSizeSetField);

            AddSetSize();
            AddGetSize();

            void AddSetSize()
            {
                var method = new MethodDefinition("SetSize", MethodAttributes.Public, m_module.TypeSystem.Void);
                method.Parameters.Add(new ParameterDefinition("size", ParameterAttributes.None, index2DType));
                var il = method.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, sizeField);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stfld, isSizeSetField);
                il.Emit(OpCodes.Ret);
                type.Methods.Add(method);
            }
            
            void AddGetSize()
            {
                /*
                    public Index2 GetSize()
                    {
                        if (isSizeSet)
	                    {
		                    return size;
	                    }
	                    return new Index2(22, 14);
                    }
                */
                var method = new MethodDefinition("GetSize", MethodAttributes.Public, index2DType);
                var il = method.Body.GetILProcessor();
                var label1 = il.Create(OpCodes.Nop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, isSizeSetField);
                il.Emit(OpCodes.Brfalse_S, label1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, sizeField);
                il.Emit(OpCodes.Ret);
                il.Append(label1);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)22);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)14);
                il.Emit(OpCodes.Newobj, index2DType.Methods.Single(m => m.Name == ".ctor"));
                il.Emit(OpCodes.Ret);
                type.Methods.Add(method);
            }
        }
        /*
        private void sds()
        {
.#=qWo_Ilq5Sos4EQLLY_xDRAjAw6Q83Z6ZpjGeSwvBaB5U=.#=qAmFcmFbUxPAB0DUb13KNOEmJgRvZSM6E$AgqIvIwrnk=
        }*/

        public void SavePatchedFile(string targetFile)
        {
            m_module.Write(targetFile);
        }
    }
}
 