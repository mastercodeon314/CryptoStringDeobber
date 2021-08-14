﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using CallingConvention = dnlib.DotNet.CallingConvention;

namespace dnpatch
{
    public class Patcher
    {
        private readonly PatchHelper _patcher = null;

        public Patcher(string file)
        {
            _patcher = new PatchHelper(file);
        }

        public Patcher(string file, bool keepOldMaxStack)
        {
            _patcher = new PatchHelper(file, keepOldMaxStack);
        }

        public Patcher(ModuleDefMD module, bool keepOldMaxStack)
        {
            _patcher = new PatchHelper(module, keepOldMaxStack);
        }

        public Patcher(ModuleDef module, bool keepOldMaxStack)
        {
            _patcher = new PatchHelper(module, keepOldMaxStack);
        }

        public Patcher(Stream stream, bool keepOldMaxStack)
        {
            _patcher = new PatchHelper(stream, keepOldMaxStack);
        }

        public ModuleDef GetModule()
        {
            return _patcher.Module;
        }

        public void Patch(Target target)
        {
            if ((target.Indices != null || target.Index != -1) &&
                (target.Instruction != null || target.Instructions != null))
            {
                _patcher.PatchOffsets(target);
            }
            else if ((target.Index == -1 && target.Indices == null) &&
                     (target.Instruction != null || target.Instructions != null))
            {
                _patcher.PatchAndClear(target);
            }
            else
            {
                throw new Exception("Check your Target object for inconsistent assignments");
            }
        }

        public void Patch(Target[] targets)
        {
            foreach (Target target in targets)
            {
                if ((target.Indices != null || target.Index != -1) &&
                    (target.Instruction != null || target.Instructions != null))
                {
                    _patcher.PatchOffsets(target);
                }
                else if ((target.Index == -1 && target.Indices == null) &&
                         (target.Instruction != null || target.Instructions != null))
                {
                    _patcher.PatchAndClear(target);
                }
                else
                {
                    throw new Exception("Check your Target object for inconsistent assignments");
                }
            }
        }

        public void Save(string name)
        {
            _patcher.Save(name);
        }

        public void Save(bool backup)
        {
           _patcher.Save(backup);
        }

        public int FindInstruction(Target target, Instruction instruction)
        {
            return _patcher.FindInstruction(target, instruction, 1);
        }

        public int FindInstruction(Target target, Instruction instruction, int occurence)
        {
            return _patcher.FindInstruction(target, instruction, occurence);
        }

        public void ReplaceInstruction(Target target)
        {
            _patcher.ReplaceInstruction(target);
        }

        public void RemoveInstruction(Target target)
        {
            _patcher.RemoveInstruction(target);
        }

        public Instruction[] GetInstructions(Target target)
        {
            return _patcher.GetInstructions(target);
        }

        public void PatchOperand(Target target, string operand)
        {
            _patcher.PatchOperand(target, operand);
        }

        public void PatchOperand(Target target, int operand)
        {
            _patcher.PatchOperand(target, operand);
        }

        public void PatchOperand(Target target, string[] operand)
        {
            _patcher.PatchOperand(target, operand);
        }

        public void PatchOperand(Target target, int[] operand)
        {
            _patcher.PatchOperand(target, operand);
        }

        public void WriteReturnBody(Target target, bool trueOrFalse)
        {
            target = _patcher.FixTarget(target);
            if (trueOrFalse)
            {
                target.Instructions = new Instruction[]
                {
                    Instruction.Create(OpCodes.Ldc_I4_1),
                    Instruction.Create(OpCodes.Ret)
                };
            }
            else
            {
                target.Instructions = new Instruction[]
                {
                    Instruction.Create(OpCodes.Ldc_I4_0),
                    Instruction.Create(OpCodes.Ret)
                };
            }

            _patcher.PatchAndClear(target);
        }

        /// <summary>
        /// Find methods that contain a certain OpCode[] signature
        /// </summary>
        /// <param name="signature"></param>
        /// <returns></returns>
        public Target[] FindMethodsByOpCodeSignature(params OpCode[] signature)
        {
            return _patcher.FindMethodsByOpCodeSignature(signature);
        }

        public void WriteEmptyBody(Target target)
        {
            target = _patcher.FixTarget(target);
            target.Instruction = Instruction.Create(OpCodes.Ret);
            _patcher.PatchAndClear(target);
        }

        public Target[] FindInstructionsByOperand(string[] operand)
        {
            return _patcher.FindInstructionsByOperand(operand);
        }

        public Target[] FindInstructionsByOperand(int[] operand)
        {
            return _patcher.FindInstructionsByOperand(operand);
        }

        public Target[] FindInstructionsByOpcode(OpCode[] opcode)
        {
            return _patcher.FindInstructionsByOpcode(opcode);
        }

        public Target[] FindInstructionsByOperand(Target target, int[] operand, bool removeIfFound = false)
        {
            return _patcher.FindInstructionsByOperand(target, operand, removeIfFound);
        }

        public Target[] FindInstructionsByOpcode(Target target, OpCode[] opcode, bool removeIfFound = false)
        {
            return _patcher.FindInstructionsByOpcode(target, opcode, removeIfFound);
        }

        [Obsolete("This functions is still in development")]
        public Target[] FindInstructionsByRegex(Target target, string pattern, bool ignoreOperand)
        {
            return _patcher.FindInstructionsByRegex(target, pattern, ignoreOperand);
        }

        public string GetOperand(Target target)
        {
            return _patcher.GetOperand(target);
        }

        public int GetLdcI4Operand(Target target)
        {
            return _patcher.GetLdcI4Operand(target);
        }

        public IMethod BuildCall(Type type, string method, Type returnType, Type[] parameters)
        {
            return _patcher.BuildCall(type, method, returnType, parameters);
        }

        public void RewriteProperty(Target target)
        {
            _patcher.RewriteProperty(target);
        }

        public void InjectMethod(Target target)
        {
            /*
             *  Example: https://github.com/0xd4d/dnlib/blob/master/Examples/Example2.cs
             *  MethodImplAttributes methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
			 *  MethodAttributes methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
			 *  MethodDef meth1 = new MethodDefUser("MyMethod", MethodSig.CreateStatic(mod.CorLibTypes.Int32, mod.CorLibTypes.Int32, mod.CorLibTypes.Int32), methImplFlags, methFlags);
             */
            _patcher.InjectMethod(target);
        }

        public void AddCustomAttribute(Target target, CustomAttribute attribute)
        {
            _patcher.AddCustomAttribute(target, attribute);
        }

        public void RemoveCustomAttribute(Target target, CustomAttribute attribute)
        {
            _patcher.RemoveCustomAttribute(target, attribute);
        }

        public void RemoveCustomAttribute(Target target, int attributeIndex)
        {
            _patcher.RemoveCustomAttribute(target, attributeIndex);
        }

        public void ClearCustomAttributes(Target target)
        {
            _patcher.ClearCustomAttributes(target);
        }

        public Target GetEntryPointTarget()
        {
            return _patcher.GetEntryPoint();
        }
    }
}
