using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using dnpatch;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Forms;
using MethodAttributes = dnlib.DotNet.MethodAttributes;

namespace CryptoDeobber
{
    public class Utils
    {
        private static IList<MethodDef> getMethodReferences(IList<TypeDef> types, MethodDef refMethod)
        {
            ConcurrentBag<MethodDef> methodsReferenced = new ConcurrentBag<MethodDef>();
            MethodSignature refMethodSig = new MethodSignature(refMethod);
            Parallel.ForEach(types, type =>
            {
                //
                List<MethodDef> typeMethods = getAllMethodsFromType(type);

                foreach (MethodDef m in typeMethods)
                {
                    Instruction[] code = m.Body.Instructions.ToArray();

                    for (int i = 0; i < code.Length; i++)
                    {
                        if (code[i].OpCode == OpCodes.Call || code[i].OpCode == OpCodes.Callvirt || code[i].OpCode == OpCodes.Calli)
                        {
                            IMethodDefOrRef opp = null;
/*                            IMemberDef oppM = null;
                            IMemberRef oppR = null;*/

/*                            try
                            {*/
                                opp = (IMethodDefOrRef)code[i].Operand;
/*                            }
                            catch (Exception)
                            {

                            }*/

                            if (opp != null)
                            {
                                MethodDef r = opp.ResolveMethodDef();
                                MethodSignature rSig = new MethodSignature(r);

                                if (rSig.Equals(refMethodSig) == true)
                                {
                                    methodsReferenced.Add(m);
                                    break;
                                }
                            }
                            
                        }
                    }

                }

                /*if (IsPrime(number))
                {
                    primeNumbers.Add(number);
                }*/
            });

            return methodsReferenced.ToList();
        }

        // gets all methods from a given type, excluding nested types
        public static List<MethodDef> getMethodsFromType(TypeDef typeDef)
        {
            List<MethodDef> methods = typeDef.FindConstructors().ToList();
            methods.AddRange(typeDef.Methods.ToList());

            MethodDef staticConst = null;
            try
            {
                staticConst = typeDef.FindStaticConstructor();
            }
            catch (Exception)
            {
                //Console.WriteLine(e);
                staticConst = null;
            }

            if (staticConst != null)
            {
                methods.Add(staticConst);
            }

            return methods;
        }

        public static bool isConstructorCall(MethodDef delegateClassStaticConstructor)
        {
            Instruction[] instructions = ((CilBody) delegateClassStaticConstructor.MethodBody).Instructions.ToArray();

            foreach (var ins in instructions)
            {
                if (ins.OpCode.Code == Code.Call || ins.OpCode.Code == Code.Callvirt)
                {
                    IMethodDefOrRef delegateMethodDef = null;
                    try
                    {
                        delegateMethodDef = (IMethodDefOrRef) ins.Operand;
                    }
                    catch (Exception)
                    {
                        
                    }

                    if (delegateMethodDef != null)
                    {
                        MethodDef d = delegateMethodDef.ResolveMethodDef();
                        Instruction[] delegateConstIns = d.Body.Instructions.ToArray();

                        for (int c = 0; c < delegateConstIns.Length; c++)
                        {
                            Instruction instr = delegateConstIns[c];
                            if (instr.OpCode.Code == Code.Ldsfld)
                            {
                                MemberRef opperandField = null;
                                try
                                {
                                    opperandField = (MemberRef) instr.Operand;
                                    if (opperandField.DeclaringType.FullName == "System.Reflection.Emit.OpCodes")
                                    {
                                        if (opperandField.Name == "Call")
                                        {
                                            //Debugger.Break();
                                            return false;
                                        }

                                        if (opperandField.Name == "Newobj")
                                        {
                                            //Debugger.Break();
                                            return true;
                                        }
                                    }
                                    
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static int[] getTokensFromDelegateConstructor(MethodDef delegateClassStaticConstructor)
        {
            List<int> result = new List<int>();

            Instruction[] instructions = ((CilBody) delegateClassStaticConstructor.MethodBody).Instructions.ToArray();
            
            foreach (var ins in instructions)
            {
                if (ins.IsLdcI4() == true)
                {
                    result.Add((int)ins.Operand);
                }
            }

            return result.ToArray();
        }

        public static TypeDef getMethodType(Instruction ins)
        {
            IMethodDefOrRef delegateMethodDef = null;
            try
            {
                delegateMethodDef = (IMethodDefOrRef) ins.Operand;
            }
            catch (Exception)
            {
                        
            }

            if (delegateMethodDef != null)
            {
                return delegateMethodDef.DeclaringType.ResolveTypeDef();
            }

            return null;
        }

        public static bool isDelegateCall(Instruction ins, MethodDef targetMethod)
        {
            if (ins != null)
            {
                if (ins.OpCode.Code == Code.Call || ins.OpCode.Code == Code.Callvirt || ins.OpCode.Code == Code.Calli)
                {
                    IMethodDefOrRef delegateMethodDef = null;
                    try
                    {
                        delegateMethodDef = (IMethodDefOrRef) ins.Operand;
                    }
                    catch (Exception)
                    {
                        return false;
                    }

                    if (delegateMethodDef != null)
                    {
                        ITypeDefOrRef baseType = delegateMethodDef.DeclaringType.GetBaseType();

                        if (baseType != null)
                        {
                            if (baseType.FullName.Contains("System.MulticastDelegate") == true)
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }
            return false;
        }

        public static bool isArrayInstalizerCall(Instruction ins, MethodDef targetMethod)
        {
            if (ins.OpCode.Code == Code.Call || ins.OpCode.Code == Code.Callvirt || ins.OpCode.Code == Code.Calli)
            {
                IMethodDefOrRef delegateMethodDef = null;
                try
                {
                    delegateMethodDef = (IMethodDefOrRef) ins.Operand;
                    
                }
                catch (Exception)
                {
                        
                }

                if (delegateMethodDef != null)
                {

                    string DecTypeFullName = delegateMethodDef.DeclaringType.FullName;
                    ITypeDefOrRef baseType = null;
                    try
                    {
                        baseType = delegateMethodDef.DeclaringType.GetBaseType();
                    }
                    catch (Exception)
                    {
                        return false;
                    }

                    if (baseType != null)
                    {
                        string DecTypeBaseFullName = baseType.FullName;
                        //string DecTypeFullName1 = delegateMethodDef.DeclaringType.FullName;

                        if (DecTypeBaseFullName.Contains("System.MulticastDelegate") == true)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return false;
        }

        public static bool checkForInstructionReference(Instruction ins)
        {
            try
            {
                Instruction insOp = (Instruction) ins.Operand;
                
                if (insOp != null) return true;
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
        
        public static ITypeDefOrRef reflectionType_To_dnType(Type t, ModuleDefMD asm, ModuleContext modCtx)
        {
            ITypeDefOrRef res = null;

            if (t.Module.FullyQualifiedName == asm.Location)
            {
                try
                {
                    Importer imp = new Importer(asm);
                    ITypeDefOrRef tt = imp.Import(t);

                    if (tt != null)
                    {
                        res = tt;
                        return res;
                    }
                }
                catch (Exception)
                {
                    Debugger.Break();
                }
                //res = asm.Find(t.FullName, true);
            }
            else
            {
                try
                {
                    ModuleDefMD mod = ModuleDefMD.Load(t.Module, modCtx);
                    Importer imp = new Importer(mod);
                    ITypeDefOrRef tt = imp.Import(t);

                    if (tt != null)
                    {
                        res = tt;
                        return res;
                    }
                
                }
                catch (Exception)
                {
                    Debugger.Break();
                }
            }

            return res;
        }
        
        public static bool typesEqual(TypeSig a, TypeSig b)
        {
            return a.ReflectionFullName.Equals(b.ReflectionFullName);
        }
        
        public static bool typesEqual(ITypeDefOrRef a, TypeSig b)
        {
            return a.ReflectionFullName.Equals(b.ReflectionFullName);
        }
        
        public static bool typesEqual(TypeSig a, ITypeDefOrRef b)
        {
            return a.ReflectionFullName.Equals(b.ReflectionFullName);
        }

        public static bool typesEqual(ITypeDefOrRef a, ITypeDefOrRef b)
        {
            return a.ReflectionFullName.Equals(b.ReflectionFullName);
        }
        
        private bool hasConstructor(TypeDef typeDef)
        {
            IEnumerable<MethodDef> possibleConstructors = null;

            possibleConstructors = typeDef.FindConstructors();

            if (possibleConstructors != null)
            {
                return possibleConstructors.Any();
            }

            return false;
        }

        public static bool hasStaticConstructor(TypeDef typeDef)
        {
            MethodDef staticConst = null;
            try
            {
                staticConst = typeDef.FindStaticConstructor();
            }
            catch (Exception)
            {
                staticConst = null;
            }

            if (staticConst != null)
            {
                return true;
            }

            return false;
        }
        
        

        public static bool methodsEqual(MethodDef a, MethodDef b)
        {
            bool res = false;

            if (Utils.typesEqual(a.DeclaringType, b.DeclaringType) == true && Utils.typesEqual(a.ReturnType, b.ReturnType) == true)
            {
                res = a.FullName.Equals(b.FullName);// && a.Parameters.SequenceEqual(b.Parameters);
            }
            
            return res;
        }

        public static bool hasMethods(TypeDef typeToCheckForMethods, Dictionary<MethodDef, int> methods, bool Explicit = true)
        {
            MethodDef[] tMethods = typeToCheckForMethods.Methods.ToArray();
            bool res = false;
            
            Dictionary<MethodDef, int> matchedMethods = new Dictionary<MethodDef, int>();
            int totalMethodsSuspected = 0;

            for (int i = 0; i < tMethods.Length; i++)
            {
                MethodDef fld = tMethods[i];
                foreach (MethodDef t in methods.Keys)
                {
                    totalMethodsSuspected += methods[t];

                    if (methodsEqual(fld, t) == true)
                    {
                        if (matchedMethods.ContainsKey(t) == false)
                        {
                            matchedMethods.Add(t, 1);
                        }
                        else
                        {
                            matchedMethods[t] = matchedMethods[t] + 1;
                        }
                    }
                }

            }

            res = true;

            if (matchedMethods.Keys.Count != methods.Keys.Count)
            {
                res = false;
            }
            else if (matchedMethods.Keys.Count == methods.Keys.Count)
            {
                if (Explicit == true)
                {
                    int a = 0;
                    foreach (MethodDef t in matchedMethods.Keys)
                    {
                        a += matchedMethods[t];
                    }

                    if (a != totalMethodsSuspected && a != (tMethods.Length - 1))
                    {
                        res = false;
                    }
                    else
                    {
                        res = true;
                    }
                }
            }

            return res;
        }

        // hasFields method usage: 
        // typeToCheckForMethods is the TypeDef of the type to check for fields
        // methodSigs is a Dictionary<TypeDef, int>, Key is a TypeDef of a fields type, and value is the amount of fields with the same TypeDef as the Key
        // Explicit: edge case detection of fields, in case of the fields specified by methodSigs and more fields are present,
        // the Explicit can be set to true, meaning that the method will only return true if ONLY the fields specified by methodSigs is found, and the amount of fields being seen in the
        // type being checked is no more than the 
        // total amount of fields as defined in methodSigs.
        public static bool hasFields(ITypeDefOrRef typeToCheckForFields, Dictionary<ITypeDefOrRef, int> fieldTypes, bool Explicit = true)
        {
            TypeDef tt = (TypeDef) typeToCheckForFields;
            FieldDef[] tFields = tt.Fields.ToArray();
            bool res = false;
            
            Dictionary<ITypeDefOrRef, int> matchedFieldTypes = new Dictionary<ITypeDefOrRef, int>();
            int totalFieldsSuspected = 0;

            for (int i = 0; i < tFields.Length; i++)
            {
                FieldDef fld = tFields[i];
                foreach (ITypeDefOrRef t in fieldTypes.Keys)
                {
                    if (Utils.typesEqual(fld.FieldType, t) == true)
                    {
                        if (matchedFieldTypes.ContainsKey(t) == false)
                        {
                            matchedFieldTypes.Add(t, 1);
                        }
                        else
                        {
                            matchedFieldTypes[t] = matchedFieldTypes[t] + 1;
                        }

                        totalFieldsSuspected += 1;
                    }
                }

            }

            res = true;

            if (matchedFieldTypes.Keys.Count != fieldTypes.Keys.Count)
            {
                res = false;
            }
            else if (matchedFieldTypes.Keys.Count == fieldTypes.Keys.Count)
            {
                if (Explicit == true)
                {
                    int a = 0;
                    foreach (ITypeDefOrRef t in matchedFieldTypes.Keys)
                    {
                        a += matchedFieldTypes[t];
                    }

                    if (a != totalFieldsSuspected)
                    {
                        res = false;
                    }
                    if (a != tFields.Length)
                    {
                        res = false;
                    }
                }
            }

            return res;
        }
        
        // hasMethods method usage: 
        // typeToCheckForMethods is the TypeDef of the type to check for fields
        // methodSigs is a Dictionary<MethodSignature, int>, Key is a MethodSignature of a method, and value is the amount of methods with the same MethodSignature as the Key
        // Explicit: edge case detection of methods, in case of the methods specified by methodSigs and more methods are present,
        // the Explicit can be set to true, meaning that the method will only return true if ONLY the methods specified by methodSigs is found, and the amount of methods being seen in the
        // type being checked is no more than the 
        // total amount of methods as defined in methodSigs.
        public static bool hasMethods(ITypeDefOrRef typeToCheckForMethods, Dictionary<MethodSignature, int> methodSigs, bool Explicit = true)
        {
            TypeDef tt = (TypeDef) typeToCheckForMethods;
            MethodDef[] methods = tt.Methods.ToArray();
            List<MethodDef> mts = new List<MethodDef>();
            for (int i = 0; i < methods.Length; i++)
            {
                if (!methods[i].Name.Contains("ctor") && !methods[i].Name.Contains("cctor"))
                {
                    mts.Add(methods[i]);
                }
            }

            methods = mts.ToArray();
            bool res = false;
            
            Dictionary<MethodSignature, int> matchedFieldTypes = new Dictionary<MethodSignature, int>();
            int totalFieldsSuspected = 0;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodSignature testSig = new MethodSignature(methods[i]);
                foreach (MethodSignature methodSig in methodSigs.Keys)
                {
                    if (testSig.Equals(methodSig) == true)
                    {
                        if (matchedFieldTypes.ContainsKey(methodSig) == false)
                        {
                            matchedFieldTypes.Add(methodSig, 1);
                        }
                        else
                        {
                            matchedFieldTypes[methodSig] = matchedFieldTypes[methodSig] + 1;
                        }

                        totalFieldsSuspected += 1;
                    }
                }

            }

            res = true;

            if (matchedFieldTypes.Keys.Count != methodSigs.Keys.Count)
            {
                res = false;
            }
            else if (matchedFieldTypes.Keys.Count == methodSigs.Keys.Count)
            {
                if (Explicit == true)
                {
                    int a = 0;
                    foreach (MethodSignature t in matchedFieldTypes.Keys)
                    {
                        a += matchedFieldTypes[t];
                    }

                    if (a != totalFieldsSuspected)
                    {
                        res = false;
                    }

                    if (a != methods.Length)
                    {
                        res = false;
                    }
                }
            }

            return res;
        }

        public static bool hasField(ITypeDefOrRef typeToCheckForField, Type fieldType, ModuleDefMD asm, ModuleContext modCtx)
        {
            return hasField(typeToCheckForField, reflectionType_To_dnType(fieldType, asm, modCtx));
        }
        
        public static bool hasField(ITypeDefOrRef typeToCheckForField, ITypeDefOrRef fieldType)
        {
            TypeDef tt = (TypeDef) typeToCheckForField;
            FieldDef[] fields = tt.Fields.ToArray();
            bool res = false;

            for (int i = 0; i < fields.Length; i++)
            {
                FieldDef fld = fields[i];
                if (Utils.typesEqual(fld.FieldType, fieldType) == true)
                {
                    res = true;
                    break;
                }
            }

            return res;
        }
        
        public static Target getTarget(MethodDef method)
        {
            return new Target(method);
        }

        public static Instruction[] getInstructions(MethodDef t)
        {
            if (t.Body != null)
            {
                return t.Body.Instructions.ToArray();
            }

            return null;
        }

        // Gets all methods from a given type, including nested types
        public static List<MethodDef> getAllMethodsFromType(TypeDef typeDef)
        {
            List<MethodDef> methods = typeDef.FindConstructors().ToList();
            methods.AddRange(typeDef.Methods.ToList());

            MethodDef staticConst = null;
            try
            {
                staticConst = typeDef.FindStaticConstructor();
            }
            catch (Exception)
            {
                //Console.WriteLine(e);
                staticConst = null;
            }

            if (staticConst != null)
            {
                methods.Add(staticConst);
            }

            return methods;
        }

        public static bool isLdcI4_X(Instruction ins)
        {
            switch (ins.OpCode.Code)
            {
                case Code.Ldc_I4_0:
                case Code.Ldc_I4_1:
                case Code.Ldc_I4_2:
                case Code.Ldc_I4_3:
                case Code.Ldc_I4_4:
                case Code.Ldc_I4_5:
                case Code.Ldc_I4_6:
                case Code.Ldc_I4_7:
                case Code.Ldc_I4_8:
                    return true;
                default:
                    return false;
            }
        }

        public static bool isLdcI4_S(Instruction ins)
        {
            switch (ins.OpCode.Code)
            {
                case Code.Ldc_I4_S:
                    return true;
                default:
                    return false;
            }
        }

        public static bool isLdcI4(Instruction ins)
        {
            switch (ins.OpCode.Code)
            {
                case Code.Ldc_I4_S:
                case Code.Ldc_I4:
                    return true;
                default:
                    return false;
            }
        }

        public static int getLDCxValue(Instruction ins)
        {
            return ins.GetLdcI4Value();
        }

        public static bool isCallInstruction(Instruction ins)
        {
            return ins.OpCode.Code == Code.Call;
        }


        public static object decryptValue(MethodDef decryptMethod, int dataIndex)
        {
            System.Type tp = System.Reflection.Assembly.GetExecutingAssembly()
                .GetType(decryptMethod.DeclaringType.FullName);
            object inst = Activator.CreateInstance(tp);
            MethodInfo[] infs = tp.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Default);

            object res = null;

            for (int i = 0; i < infs.Length; i++)
            {
                MethodInfo x = infs[i];
                if (x.Name == decryptMethod.Name)
                {
                    if (x.ReturnType.FullName == decryptMethod.ReturnType.FullName)
                    {
                        res = x.Invoke(inst, new object[] {dataIndex});
                    }
                }
            }
            
            if (res == null) Debugger.Break();
            
            return res;
        }

        public static int[] checkForBRref(Instruction[] opCodes, Instruction targetInstruction)
        {
            List<int> BrRefInstructionResult = new List<int>();

            for (int i = 0; i < opCodes.Length; i++)
            {
                Instruction ins = opCodes[i];

                if (checkBR(ins) == true)
                {
                    Instruction BRref = (Instruction) ins.Operand;

                    if (targetInstruction.Equals(BRref) == true)
                    {
                        BrRefInstructionResult.Add(i);
                    }
                }
            }

            return BrRefInstructionResult.ToArray();
        }

        public static bool checkBR(Instruction ins)
        {
            switch (ins.OpCode.Code)
            {
                case Code.Br:
                case Code.Brfalse:
                case Code.Brtrue:
                case Code.Br_S:
                case Code.Brfalse_S:
                case Code.Brtrue_S:
                case Code.Leave:
                case Code.Leave_S:
                    return true;
                default:
                    return false;
            }
        }

        public static void FixBR_Refs(int[] brRefs, int[] brRefs1, Instruction[] opCodes, int x)
        {
            if (brRefs.Length > 0)
            {
                for (int Z = 0; Z < brRefs.Length; Z++)
                {
                    int opCodeIndex = brRefs[Z];
                    opCodes[opCodeIndex].Operand = opCodes[x];
                }
            }

            if (brRefs1.Length > 0)
            {
                for (int Z = 0; Z < brRefs1.Length; Z++)
                {
                    int opCodeIndex = brRefs1[Z];
                    opCodes[opCodeIndex].Operand = opCodes[x + 1];
                }
            }
        }
    }
}