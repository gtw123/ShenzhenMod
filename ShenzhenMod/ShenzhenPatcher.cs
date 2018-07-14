using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ShenzhenMod
{
    public class ShenzhenPatcher : IDisposable
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ShenzhenPatcher));

        private ModuleDefinition m_module;
        private MethodDefinition m_puzzleGetSize;
        private MethodDefinition m_puzzleSetSize;

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
            FixTileGeneration();
            FixTraceLoading();
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
            var puzzleType = m_module.FindType("Puzzle");
            var index2DType = m_module.FindType("Index2");
            var sizeField = new FieldDefinition("size", FieldAttributes.Private, index2DType);
            puzzleType.Fields.Add(sizeField);
            var isSizeSetField = new FieldDefinition("isSizeSet", FieldAttributes.Private, m_module.TypeSystem.Boolean);
            puzzleType.Fields.Add(isSizeSetField);

            m_puzzleSetSize = AddSetSize();
            m_puzzleGetSize = AddGetSize();

            MethodDefinition AddSetSize()
            {
                /*
                    public void SetSize(Index2 size)
                    {
                        this.size = size;
                        isSizeSet = true;
                    }
                */
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
                puzzleType.Methods.Add(method);

                return method;
            }

            MethodDefinition AddGetSize()
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
                il.Emit(OpCodes.Newobj, index2DType.FindMethod(".ctor"));
                il.Emit(OpCodes.Ret);
                puzzleType.Methods.Add(method);

                return method;
            }
        }
        
        private void FixTileGeneration()
        {
            var method = m_module.FindMethod("#=qWo_Ilq5Sos4EQLLY_xDRAjAw6Q83Z6ZpjGeSwvBaB5U=", "#=qAmFcmFbUxPAB0DUb13KNOEmJgRvZSM6E$AgqIvIwrnk=");
            var il = method.Body.GetILProcessor();

            var instructionsToReplace = method.FindInstructions(OpCodes.Ldsfld, m_module.FindField("#=qL_uhZp1CYmicXxDRy$c1Bw==", "#=qAVzUnhNiHUMBOnnky4iCCA=="), 2);
            foreach (var instr in instructionsToReplace.ToList())
            {
                il.InsertBefore(instr, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(instr, il.Create(OpCodes.Call, m_puzzleGetSize));
                il.Remove(instr);
            }
        }

        private void FixTraceLoading()
        {
            var method = m_module.FindMethod("Solution", "#=qMQ0eadM7OBun57pFLO1wWA==");
            var il = method.Body.GetILProcessor();

            FixTraceLoading1();
            FixTraceLoading2();

            // Fix up branches instructions that can no longer be "short" branches
            method.Body.SimplifyMacros();
            method.Body.OptimizeMacros();

            void FixTraceLoading1()
            {
                /*
			        if (index == new Index2(0, 2))
			        {
				        for (int j = 0; j < 22; j++)
				        {
					        for (int k = 0; k < 11; k++)
					        {
						        this.Traces.AddTrace(new Index2(j, k), this.Traces.GetTrace(new Index2(j, k + 3)));
					        }
					        for (int l = 11; l < 14; l++)
					        {
						        this.Traces.AddTrace(new Index2(j, l), this.Trace.None);
					        }
				        }
        			}

                    int lastY = index.Y;
                    if (lastY > 0)
                    {
                        int maxY = this.Traces.GetSize().Y;
                        for (int j = 0; j < this.Traces.GetSize().X; j++)
                        {
                            for (int k = 0; k < maxY - lastY; k++)
                            {
                                this.Traces.AddTrace(new Index2(j, k), this.Traces.GetTrace(new Index2(j, k + lastY)));
                            }
                            for (int l = maxY - lastY; l < maxY; l++)
                            {
                                this.Traces.AddTrace(new Index2(j, l), Trace.None);
                            }
                        }
                    }

                 */

                // Remove the "if (index == new Index2(0, 2))" check, but leave the first instruction so we don't break branches
                var ifIndexEquals02 = method.FindInstructionAtOffset(0x04be, OpCodes.Ldloc_S, method.Body.Variables[13]);
                var jLoopStart = method.FindInstructionAtOffset(0x04ce, OpCodes.Ldc_I4_0, null);
                il.RemoveRange(ifIndexEquals02.Next, jLoopStart);

                // Add two new variables
                var lastY = new VariableDefinition(m_module.TypeSystem.Int32);
                method.Body.Variables.Add(lastY);
                var maxY = new VariableDefinition(m_module.TypeSystem.Int32);
                method.Body.Variables.Add(maxY);

                // Initialize the two new variables and add the "if (lastY > 0)" check
                var index2Type = m_module.FindType("Index2");
                ifIndexEquals02.Set(OpCodes.Ldloca_S, method.Body.Variables[13]);
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Ldfld, index2Type.Fields[2]));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Stloc_S, lastY));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Ldloc_S, lastY));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Ldc_I4_0));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Ble, method.Body.Instructions.Last()));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Ldfld, m_module.FindField("Solution", "#=qoToRfupqxh4PHUk11ckgIg==")));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Callvirt, m_module.FindMethod("#=qU2wvld4wYwd2RmifHjQEOQ==", "#=qa$TS_$HdBzzP07FjV63Yrw==")));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Ldfld, index2Type.Fields[2]));
                il.InsertBefore(jLoopStart, il.Create(OpCodes.Stloc_S, maxY));

                // Replace "k + 3" with "k + lastY"
                method.FindInstruction(OpCodes.Ldc_I4_3, null).Set(OpCodes.Ldloc_S, lastY);

                // Replace "l < 14" with "l < maxY"
                method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)14).Set(OpCodes.Ldloc_S, maxY);

                // Replace "11" with "maxY - lastY"
                foreach (var instr in method.FindInstructions(OpCodes.Ldc_I4_S, (sbyte)11, 2).ToList())
                {
                    instr.Set(OpCodes.Ldloc_S, maxY);
                    il.InsertAfter(instr, il.Create(OpCodes.Sub));
                    il.InsertAfter(instr, il.Create(OpCodes.Ldloc_S, lastY));
                }

                // Replace "22" with "this.Traces.GetSize().X"
                var instr2 = method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)22);
                instr2.Set(OpCodes.Ldarg_0, null);
                il.InsertAfter(instr2, il.Create(OpCodes.Ldfld, index2Type.Fields[1]));
                il.InsertAfter(instr2, il.Create(OpCodes.Callvirt, m_module.FindMethod("#=qU2wvld4wYwd2RmifHjQEOQ==", "#=qa$TS_$HdBzzP07FjV63Yrw==")));
                il.InsertAfter(instr2, il.Create(OpCodes.Ldfld, m_module.FindField("Solution", "#=qoToRfupqxh4PHUk11ckgIg==")));
            }

            void FixTraceLoading2()
            {
                /* Replace this:

                    if (flag)
                    {
                        index2.x++;
                        if (index2.x >= this.gclass108_0.method_0().x)
                        {
                            index2.x = 0;
                            index2.y--;
                            if (index2.y < 0)
                            {
                                break;
                            }
                        }
                    }

                with this:

                    else if (chr == '\n')
                    {
                        index2.x = 0;
                        index2.y--;
                        if (index2.y < 0)
                        {
                            break;
                        }
                    }
                    if (flag)
                    {
                        index2.x++;
                    }
                */

                var ifFlag = method.FindInstructionAtOffset(0x0463, OpCodes.Ldloc_S, method.Body.Variables[16]);
                var ifXGreater = method.FindInstructionAtOffset(0x0473, OpCodes.Ldloc_S, method.Body.Variables[13]);
                var handleEOL = method.FindInstructionAtOffset(0x048c, OpCodes.Ldloca_S, method.Body.Variables[13]);
                var loopBodyEnd = method.FindInstructionAtOffset(0x04aa, OpCodes.Ldloc_S, method.Body.Variables[4]);

                // If the if before "if (flag)" is true, jump to the "if (flag" rather than falling through
                il.InsertBefore(ifFlag, il.Create(OpCodes.Br, ifFlag));

                // Remove some of the instructions from the "if (flag)" block and shift some of them towards the bottom of the loop
                var removed = il.RemoveRange(ifFlag, handleEOL);
                il.InsertBefore(loopBodyEnd, removed.TakeWhile(i => i != ifXGreater));

                // Insert the '\n' check
                var checkNewLine = il.Create(OpCodes.Ldloc_S, method.Body.Variables[15]);
                il.InsertBefore(handleEOL, checkNewLine);
                il.InsertBefore(handleEOL, il.Create(OpCodes.Ldc_I4_S, (sbyte)10));
                il.InsertBefore(handleEOL, il.Create(OpCodes.Bne_Un, ifFlag));

                // If the if before "if (flag)" is false, jump to our new '\n' check
                method.FindInstructionAtOffset(0x044b, OpCodes.Brfalse_S, ifFlag).Operand = checkNewLine;
            }
        }

        public void SavePatchedFile(string targetFile)
        {
            m_module.Write(targetFile);
        }
    }
}
 