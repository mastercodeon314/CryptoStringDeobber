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
using System.Windows.Forms;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;
using UTF8String = dnlib.DotNet.UTF8String;



/*
 * This Deobber class takes each instruction in string format, and searches it for a reference to the string and constances dec classes
 * However, the class will be supporting the deobed DecStrings and DecConsts names of Class11, and Class12. This way the file can be deobed and the res loading classes will be lfound easier
 * smethod_0 maps to getint32
 * smethod_1 maps to getint64
 * smethod_2 maps to getfloat
 * smethod_3 maps to getdouble
 * smethod_4 maps ot getarray - NOT SUPPORTED YET
 * These are the unamed methods in the deobed DecsConsts class that return the decryoted constant
 * in the og file thats not deobbed, the DecsConstants class maps to \u0006\u0006
 * the DecsStrings class maps to \u0007\u0006
 * All the methods of \u0006\u0006 are \u0007, they are overloads, additional code will be requiered to check for them
 * 
 */

namespace CryptoDeobber
{
    public enum NamingOptions
    {
        Renamed,
        DeobedNames,
        ObedNames
    }
    public class Deobber
    {

        public NamingOptions options;

        public static void Deob()
        {
            Deob(NamingOptions.Renamed);
        }

        public static void Deob(NamingOptions @options)
        {
            Deobber p = new Deobber();
            p.options = @options;
            p.Patch();
        }

        private Patcher dnPatcher;
        private ModuleContext modCtx = null;
        private ModuleDefMD asm = null;
        private Assembly exeASM = null;
        private string fileLoc = "";
        private MethodDef decsStringsMethod = null;
        private MethodSignature decsStringsSig = null;

        private MethodDef decsIntMethod = null;
        private MethodSignature decsIntSig = null;

        private MethodDef decsLongMethod = null;
        private MethodSignature decsLongSig = null;

        private MethodDef decsFloatMethod = null;
        private MethodSignature decsFloatSig = null;

        private MethodDef decsDoubleMethod = null;
        private MethodSignature decsDoubleSig = null;

        private MethodSignature decsArraySig = null;

        public int timesSaved = 0;
        private List<Target> patchedTargets = null;



        public Deobber()
        {
            this.modCtx = ModuleDef.CreateModuleContext();
            exeASM = Assembly.GetExecutingAssembly();
            Module[] modules = exeASM.GetModules();
            this.asm = ModuleDefMD.Load(modules[0], this.modCtx);
            this.fileLoc = exeASM.Location.Replace(".exe", ".cleaned.exe");
            this.Init();
        }

        public Deobber(string filePath)
        {
            this.modCtx = ModuleDef.CreateModuleContext();
            exeASM = Assembly.LoadFile(filePath);
            Module[] modules = exeASM.GetModules();
            this.asm = ModuleDefMD.Load(modules[0], this.modCtx);
            this.fileLoc = exeASM.Location.Replace(".exe", ".cleaned.exe");

            this.Init();
        }


        private void Init()
        {
            //Environment.CurrentDirectory = Path.GetDirectoryName(exeASM.Location);

            this.dnPatcher = new Patcher(asm, true);
            decsStringsSig = new MethodSignature(Utils.reflectionType_To_dnType(typeof(string), this.asm, this.modCtx), Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx));
            decsIntSig = new MethodSignature(Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx), Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx));
            decsLongSig = new MethodSignature(Utils.reflectionType_To_dnType(typeof(long), this.asm, this.modCtx), Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx));
            decsFloatSig = new MethodSignature(Utils.reflectionType_To_dnType(typeof(float), this.asm, this.modCtx), Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx));
            decsDoubleSig = new MethodSignature(Utils.reflectionType_To_dnType(typeof(double), this.asm, this.modCtx), Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx));
            decsArraySig = new MethodSignature(Utils.reflectionType_To_dnType(typeof(void), this.asm, this.modCtx), new ITypeDefOrRef[] { Utils.reflectionType_To_dnType(typeof(Array), this.asm, this.modCtx), Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx) });


            //The AssemblyResolve event is called when the common language runtime tries to bind to the assembly and fails.
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(currentDomain_AssemblyResolve);

            AssemblyName[] names = exeASM.GetReferencedAssemblies();

            foreach (var asmName in names)
            {
                if (asmName.CodeBase == null)
                {
                    string codeBase = Path.GetDirectoryName(exeASM.Location) + @"\" + asmName.Name + ".dll";
                    if (File.Exists(codeBase))
                    {
                        asmName.CodeBase = codeBase;

                    }

                }
                Assembly aa = AppDomain.CurrentDomain.Load(asmName);
            }
        }

        Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            Assembly MyAssembly, objExecutingAssemblies;
            string strTempAssmbPath = "";

            AssemblyName[] arrReferencedAssmbNames = exeASM.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
                {

                    string codeBase = Path.GetDirectoryName(exeASM.Location) + @"\" + strAssmbName.Name + ".dll";
                    if (File.Exists(codeBase))
                    {
                        strAssmbName.CodeBase = codeBase;
                        strTempAssmbPath = codeBase;
                        break;
                    }
                }

            }

            if (strTempAssmbPath != "")
            {
                //Load the assembly from the specified path.
                MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

                //Return the loaded assembly.
                return MyAssembly;
            }

            return null;
        }

        private void SavePatch(Target tar, Instruction[] opCodes)
        {
            tar.Instructions = opCodes;
            tar.Instructions.OptimizeBranches();
            tar.Instructions.UpdateInstructionOffsets();

            patchedTargets.Add(tar);

            timesSaved += 1;
        }
        private void getConstantsDecsMethods(TypeDef t)
        {
            MethodDef[] methods = t.Methods.ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                MethodSignature testSig = new MethodSignature(methods[i]);

                if (MethodSignature.Equals(decsIntSig, testSig) == true)
                {
                    decsIntMethod = methods[i];
                }

                if (MethodSignature.Equals(decsLongSig, testSig) == true)
                {
                    decsLongMethod = methods[i];
                }

                if (MethodSignature.Equals(decsFloatSig, testSig) == true)
                {
                    decsFloatMethod = methods[i];
                }

                if (MethodSignature.Equals(decsDoubleSig, testSig) == true)
                {
                    decsDoubleMethod = methods[i];
                }
            }
        }

        private void getStringsMethod(TypeDef t)
        {
            MethodDef[] methods = t.Methods.ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                MethodSignature testSig = new MethodSignature(methods[i]);

                if (MethodSignature.Equals(decsStringsSig, testSig) == true)
                {
                    decsStringsMethod = methods[i];
                }
            }
        }

        private void FindConstantsDecs(ModuleDefMD asmModule)
        {
            List<TypeDef> types = asmModule.GetTypes().ToList();



            Dictionary<ITypeDefOrRef, int> decsConstantsFields = new Dictionary<ITypeDefOrRef, int>()
            {
                [Utils.reflectionType_To_dnType(typeof(byte[]), this.asm, this.modCtx)] = 1,
                [Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx)] = 2
            };

            Dictionary<MethodSignature, int> decsConstantsMethods = new Dictionary<MethodSignature, int>()
            {
                [decsIntSig] = 1,
                [decsLongSig] = 1,
                [decsFloatSig] = 1,
                [decsDoubleSig] = 1,
                [decsArraySig] = 1
            };

            foreach (TypeDef t in types)
            {
                if (Utils.hasStaticConstructor(t) == true && t.Methods.Count > 0)
                {
                    if (t.Attributes.HasFlag(TypeAttributes.NotPublic))
                    {
                        if (Utils.hasFields(t, decsConstantsFields, true) == true)
                        {
                            if (Utils.hasMethods(t, decsConstantsMethods, true) == true)
                            {
                                getConstantsDecsMethods(t);
                            }
                        }
                    }
                }
            }
        }


        private void FindStringDecs(ModuleDefMD asmModule)
        {
            List<TypeDef> types = asmModule.GetTypes().ToList();

            Dictionary<ITypeDefOrRef, int> decsStringsFields = new Dictionary<ITypeDefOrRef, int>()
            {
                [Utils.reflectionType_To_dnType(typeof(byte[]), this.asm, this.modCtx)] = 1,
                [Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx)] = 1
            };

            Dictionary<MethodSignature, int> decsStringsMethods = new Dictionary<MethodSignature, int>()
            {
                [decsStringsSig] = 1
            };

            Dictionary<ITypeDefOrRef, int> decsConstantsFields = new Dictionary<ITypeDefOrRef, int>()
            {
                [Utils.reflectionType_To_dnType(typeof(byte[]), this.asm, this.modCtx)] = 1,
                [Utils.reflectionType_To_dnType(typeof(int), this.asm, this.modCtx)] = 2
            };

            Dictionary<MethodSignature, int> decsConstantsMethods = new Dictionary<MethodSignature, int>()
            {
                [decsIntSig] = 1,
                [decsLongSig] = 1,
                [decsFloatSig] = 1,
                [decsDoubleSig] = 1,
            };

            foreach (TypeDef t in types)
            {
                if (Utils.hasStaticConstructor(t) == true && t.Methods.Count > 0)
                {
                    if (t.Attributes.HasFlag(TypeAttributes.NotPublic))
                    {
                        if (Utils.hasFields(t, decsStringsFields, true) == true)
                        {
                            if (Utils.hasMethods(t, decsStringsMethods, true) == true)
                            {
                                getStringsMethod(t);
                            }
                        }
                    }
                }
            }
        }

        private bool checkDecsFound()
        {
            bool result = true;

            if (decsStringsMethod == null)
            {
                return false;
            }

            if (decsIntMethod == null)
            {
                return false;
            }

            if (decsLongMethod == null)
            {
                return false;
            }

            if (decsDoubleMethod == null)
            {
                return false;
            }

            if (decsFloatMethod == null)
            {
                return result;
            }

            return result;
        }

        private bool isToolStripItem(FieldInfo inf)
        {
            bool result = false;

            string toolStripItemFullName = typeof(System.Windows.Forms.ToolStripItem).FullName;

            Type baseType = null;

            if (inf.FieldType.Module.FullyQualifiedName != typeof(System.Windows.Forms.Control).Module.FullyQualifiedName) return false;

            try
            {
                baseType = inf.FieldType.BaseType;
            }
            catch (Exception)
            {

            }

            if (baseType == null) return false;

            if (baseType.FullName == toolStripItemFullName)
            {
                return true;
            }
            else
            {
                while ( baseType.FullName != toolStripItemFullName)
                {
                    if (baseType.BaseType == null)
                    {
                        return false;
                    }
                    else
                    {
                        baseType = baseType.BaseType;
                    }
                }

                if (baseType.FullName == toolStripItemFullName)
                {
                    return true;
                }
            }

            return result;
        }

        private bool isControlField(FieldInfo inf)
        {
            bool result = false;

            string controlFullName = typeof(System.Windows.Forms.Control).FullName;
            string toolStripItemFullName = typeof(System.Windows.Forms.ToolStripItem).FullName;

            Type baseType = null;

            if (inf.FieldType.Module.FullyQualifiedName != typeof(System.Windows.Forms.Control).Module.FullyQualifiedName) return false;

            try
            {
                baseType = inf.FieldType.BaseType;
            }
            catch (Exception)
            {

            }

            if (baseType == null) return false;

            if (baseType.FullName == controlFullName)
            {
                return true;
            }
            else
            {
                while (baseType.FullName != controlFullName)
                {
                    if (baseType.BaseType == null)
                    {
                        return false;
                    }
                    else
                    {
                        baseType = baseType.BaseType;
                    }
                }

                if (baseType.FullName == controlFullName)
                {
                    return true;
                }
            }

            return result;
        }

        private Instruction[] removeJunkCode(Instruction[] opCodes)
        {

            if (opCodes.Length == 2)
            {
                if (opCodes[0].OpCode == OpCodes.Ldtoken && opCodes[1].OpCode == OpCodes.Ret)
                {
                    return opCodes;
                }
            }

            List<Instruction> listCopy = opCodes.ToList();

            List<int> switchJunkStartIndices = new List<int>();
            List<int> ldtokenJunkStartIndices = new List<int>();

            List<int> RuntimeHelpers_InitializeArray_JunkIndices = new List<int>();

            for (int i = 0; i < opCodes.Length; i++)
            {
                Instruction ins = opCodes[i];

                if (ins.OpCode == OpCodes.Switch)
                {
                    Instruction[] opp = (Instruction[])ins.Operand;

                    if (opCodes[i - 1].Equals(opp[0]))
                    {
                        // junk code
                        switchJunkStartIndices.AddRange(new int[] { i - 1, i });
                    }
                }

                if (ins.OpCode == OpCodes.Ldtoken)
                {
                    if (Utils.checkBR(opCodes[i - 1]))
                    {
                        if (opCodes[i - 2].OpCode == OpCodes.Ldc_I4_1 && opCodes[i + 1].OpCode == OpCodes.Pop)
                        {
                            ldtokenJunkStartIndices.AddRange(new int[] { i - 2, i - 1, i, i + 1 });
                        }
                    }
                }

                if (ins.OpCode == OpCodes.Call && i > 0)
                {
                    if (opCodes[i - 1].OpCode == OpCodes.Ldtoken)
                    {
                        RuntimeHelpers_InitializeArray_JunkIndices.AddRange(new int[] { i - 1, i });
                    }
                }

            }
            foreach (int idx in switchJunkStartIndices)
            {
                opCodes[idx].OpCode = OpCodes.Nop;
            }

            foreach (int idx in ldtokenJunkStartIndices)
            {
                opCodes[idx].OpCode = OpCodes.Nop;
            }

            foreach (int idx in RuntimeHelpers_InitializeArray_JunkIndices)
            {
                opCodes[idx].OpCode = OpCodes.Nop;
            }

            uint offset = 0;
            for (int H = 0; H < opCodes.Length; H++)
            {
                opCodes[H].Offset = offset;

                offset += (uint)opCodes[H].GetSize();
            }

            return opCodes;
        }

        private MethodDef findFormInit(TypeDef frmType)
        {
            MethodDef constr = frmType.FindDefaultConstructor();

            if (constr == null)
            {
                IEnumerable<MethodDef> defs = (IEnumerable<MethodDef>)frmType.FindConstructors();

                if (defs.Count() > 0)
                {
                    constr = defs.ToArray()[0];
                }
            }

            Instruction[] opCodes = constr.Body.Instructions.ToArray();

            for (int i = 0; i < opCodes.Length; i++)
            {
                if (opCodes[i].OpCode == OpCodes.Call)
                {
                    IMethodDefOrRef me = (IMethodDefOrRef)opCodes[i].Operand;

                    if (me.DeclaringType.FullName == frmType.FullName)
                    {
                        me.Name = "InitializeComponent";
                        return me.ResolveMethodDef();
                    }
                }
            }

            return null;
        }

        private void fixFormEventMethods(TypeDef frmType)
        {
            MethodDef frmInit = findFormInit(frmType);

            Instruction[] opCodes = frmInit.Body.Instructions.ToArray();

            for (int i = 0; i < opCodes.Length; i++)
            {
                if (opCodes[i].OpCode == OpCodes.Ldftn)
                {
                    if (opCodes[i - 2].OpCode == OpCodes.Ldfld)
                    {
                        if (opCodes[i + 2].OpCode == OpCodes.Callvirt)
                        {
                            //Debugger.Break();
                            IField fld = (IField)opCodes[i - 2].Operand;
                            IMethodDefOrRef eventAddMethod = ((IMethodDefOrRef)opCodes[i + 2].Operand).ResolveMethodDef();
                            IMethodDefOrRef eventMethod = ((IMethodDefOrRef)opCodes[i].Operand).ResolveMethodDef();
                            string eventAddName = eventAddMethod.Name.Replace("add_", "");
                            string eventName = eventMethod.Name;
                            string fldName = fld.Name;

                            eventMethod.Name = fldName + "_" + eventAddName;
                        }
                    }
                }
            }

        }

        public void Patch()
        {
            FindStringDecs(asm);
            FindConstantsDecs(asm);

            if (checkDecsFound() == false) return;

            List<TypeDef> alltypes = asm.GetTypes().ToList();
            List<TypeDef> types = new List<TypeDef>();

            Type delegateBaseType = typeof(System.MulticastDelegate);
            Type formBaseType = typeof(System.Windows.Forms.Form);
            Type controlBaseType = typeof(System.Windows.Forms.Control);
            ModuleDefMD m = ModuleDefMD.Load(delegateBaseType.Module);
            ModuleDefMD winformModule = ModuleDefMD.Load(formBaseType.Module);
            ITypeDefOrRef MulticastDelegateType = Utils.reflectionType_To_dnType(delegateBaseType, m, m.Context);
            ITypeDefOrRef formBaseTypeRef = Utils.reflectionType_To_dnType(formBaseType, winformModule, winformModule.Context);
            ITypeDefOrRef controlBaseTypeRef = Utils.reflectionType_To_dnType(controlBaseType, winformModule, winformModule.Context);

            /*Stopwatch sw = Stopwatch.StartNew();
            List<MethodDef> removedMethods = new List<MethodDef>();
            foreach (var type in alltypes)
            {
                List<MethodDef> typeMethods = Utils.getAllMethodsFromType(type);

               foreach (MethodDef method in typeMethods)
               {
                    if (method != null)
                    {
                        IList<MethodDef> MethodsUsed = null;
                        try
                        {
                            MethodsUsed  = Utils.getMethodReferences(alltypes, method);
                        }
                        catch (Exception)
                        {
                            Debugger.Break();
                        }
                        
                        if (MethodsUsed != null)
                        {
                            if (MethodsUsed.Count == 0)
                            {
                                removedMethods.Add(method);
                                type.Remove(method);
                            }
                        }
                        
                    }
               }
            }

            sw.Stop();


            List<string> strs = new List<string>();
            strs.Add("Time taken to find removed methods: " + sw.ElapsedMilliseconds.ToString() + "ms");
            foreach (MethodDef mb in removedMethods)
            {
                strs.Add(mb.FullName);
            }
            Debugger.Break();*/


            foreach (var type in alltypes)
            {
                if (type.BaseType != null)
                {
                    if (type.BaseType.FullName == MulticastDelegateType.FullName)
                    {
                        this.asm.Types.Remove(type);
                        continue;
                    }

                    if (type.BaseType.FullName == formBaseTypeRef.FullName)
                    {
                        Type reflectionFormType = this.exeASM.GetType(type.ReflectionFullName);

                        //if (reflectionFormType.Name == "MainForm") Debugger.Break();

                        object frm = null;
                        try
                        {
                            frm = Activator.CreateInstance(reflectionFormType);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == "No parameterless constructor defined for this object.")
                            {
                                frm = Activator.CreateInstance(reflectionFormType, new object[] { null });
                            }
                        }

                        if (frm != null)
                        {
                            MethodDef frmInit = findFormInit(type);

                            //object frm = reflectionFormType.Assembly.CreateInstance(reflectionFormType.FullName);

                            Type frmTP = frm.GetType();
                            FieldInfo[] infs = frmTP.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                            Dictionary<string, string> conversionMap = new Dictionary<string, string>();


                            foreach (var reField in infs)
                            {
                                if (isControlField(reField))
                                {
                                    Control ctrl = null;

                                    try
                                    {
                                        ctrl = (Control)reField.GetValue(frm);
                                    }

                                    catch (Exception ex)
                                    {

                                    }

                                    if (ctrl != null) conversionMap.Add(reField.Name, ctrl.Name);
                                }

                                if (isToolStripItem(reField))
                                {
                                    ToolStripItem item = null;

                                    try
                                    {
                                        item = (ToolStripItem)reField.GetValue(frm);
                                    }

                                    catch (Exception ex)
                                    {

                                    }

                                    if (item != null) conversionMap.Add(reField.Name, item.Name);
                                }
                            }


                            foreach (var field in type.Fields)
                            {
                                if (conversionMap.ContainsKey(field.Name) == true)
                                {
                                    field.Name = conversionMap[field.Name];
                                }
                            }

                            fixFormEventMethods(type);
                        }
                    }
                }

                if (type.NestedTypes.Count > 0)
                {
                    foreach (var Ntype in type.NestedTypes)
                    {
                        if (type.NestedTypes.Count > 0)
                        {
                            types.AddRange(Ntype.NestedTypes);
                        }
                    }
                    types.AddRange(type.NestedTypes);
                }
                types.Add(type);
            }

            if (patchedTargets != null)
            {
                patchedTargets.Clear();
                patchedTargets = null;
            }
            patchedTargets = new List<Target>();

            /*var ca = asm.CustomAttributes.Find("System.Diagnostics.DebuggableAttribute");
            if (ca is not null && ca.ConstructorArguments.Count == 1)
            {
                var arg = ca.ConstructorArguments[0];
                // VS' debugger crashes if value == 0x107, so clear EnC bit
                if (arg.Type.FullName == "System.Diagnostics.DebuggableAttribute/DebuggingModes" && arg.Value is int value && value == 0x107)
                {
                    arg.Value = value & ~(int)DebuggableAttribute.DebuggingModes.EnableEditAndContinue;
                    ca.ConstructorArguments[0] = arg;
                }
            }*/

            List<TypeDef> shitToRemove = new List<TypeDef>();

            // Type level search
            foreach (TypeDef t in types)
            {
                /*               if (t.Namespace != (UTF8String) null) 
                               {
                                   if (t.Namespace.ToString() != "SamuelTool") continue;
                               }*/

/*                if (t.Name == "MainForm")
                {
                    Debugger.Break();
                }*/

                List<MethodDef> methods = Utils.getAllMethodsFromType(t);

                // method level search
                for (int c = 0; c < methods.Count; c++)
                {
                    Instruction[] opCodes = null;
                    Target tar = null;
                    bool patchingNeeded = false;

                    /*                    try
                                        {*/
                    MethodDef patchingTargetMethod = methods[c];

                    if (t.Name == "MainForm" && c == 231)
                    {
                        //Debugger.Break();
                    }

                    tar = new Target(patchingTargetMethod);

                    if (patchingTargetMethod.HasBody)
                    {
                        if (patchingTargetMethod.Body.HasVariables)
                        {
                            tar.Locals = patchingTargetMethod.Body.Variables.Locals.ToArray();
                        }
                    }

                    tar.ReturnType = patchingTargetMethod.ReturnType.ReflectionFullName;

                    if (patchingTargetMethod.ParamDefs.Count > 0)
                    {
                        tar.ParameterDefs = patchingTargetMethod.ParamDefs.ToArray();
                    }

                    if (patchingTargetMethod.Parameters.Count > 1)
                    {
                        List<string> result = new List<string>();
                        for (int i = 0; i < patchingTargetMethod.Parameters.Count; i++)
                        {
                            result.Add(patchingTargetMethod.Parameters[i].Type.TypeName);
                        }

                        tar.Parameters = result.ToArray();
                    }
                    else if (patchingTargetMethod.Parameters.Count == 1)
                    {
                        tar.Parameters = new List<string>
                        {
                            patchingTargetMethod.Parameters[0].Type.TypeName
                        }.ToArray();
                    }

                    tar.Namespace = patchingTargetMethod.DeclaringType.Namespace;

/*                    if (tar.Namespace == "")
                    {
                        continue;
                    }*/

                    tar.Class = patchingTargetMethod.DeclaringType.Name;
                    tar.Method = patchingTargetMethod.Name;

                    

                    opCodes = Utils.getInstructions(patchingTargetMethod);

                    if (opCodes == null) continue;

                    

                    opCodes = removeJunkCode(opCodes);

                    /*                   }
                                       catch (Exception)
                                       {
                                           Debugger.Break();
                                       }*/
                    // Instruction level search
                    for (int x = 0; x < opCodes.Length; x++)
                    {
                        Instruction ins = opCodes[x];

                        if (ins.OpCode.Code == Code.Nop) continue;

                        if (ins.OpCode.Code == Code.Call)
                        {
                            IMethod me = (IMethod)opCodes[x].Operand;
                            MethodDef mm = me.ResolveMethodDef();
                            if (mm != null)
                            {
                                if (mm.ReturnType.FullName != typeof(void).FullName && mm.Body != null && mm.Module.FullName == t.Module.FullName)
                                {
                                    Instruction[] instrs = mm.Body.Instructions.ToArray();

                                    //if (mm.DeclaringType.Name == "Class_207") Debugger.Break();

                                    if (instrs.Length == 3)
                                    {
                                        opCodes[x] = instrs[1].Clone();

                                        if (shitToRemove.Contains(mm.DeclaringType) == false)
                                        {
                                            shitToRemove.Add(mm.DeclaringType);
                                            //this.asm.Types.Remove(mm.DeclaringType);
                                        }
                                            continue;
                                    }

                                    if (instrs.Length == 4)
                                    {
                                        opCodes[x] = instrs[2].Clone();
                                        if (shitToRemove.Contains(mm.DeclaringType) == false)
                                        {
                                            shitToRemove.Add(mm.DeclaringType);
                                            //this.asm.Types.Remove(mm.DeclaringType);
                                        }
                                            continue;
                                    }
                                }
                            }
                        }

                        int decCode = 0;
                        Instruction ins2 = null;

                        if (Utils.isLdcI4(ins) == true || Utils.isLdcI4_X(ins) == true)
                        {
                            decCode = Utils.getLDCxValue(ins);

                            ins2 = opCodes[x + 1];

                            if (Utils.isCallInstruction(opCodes[x + 1]) == true)
                            {
                                MethodDef obbingCall = null;
                                try
                                {
                                    obbingCall = (MethodDef)ins2.Operand;
                                }
                                catch (Exception)
                                {
                                    //Debugger.Break();
                                }

                                if (obbingCall != null)
                                {
                                    switch (obbingCall.ReturnType.ReflectionFullName)
                                    {
                                        case "System.String":
                                            {
                                                try
                                                {
                                                    if (decsStringsSig.Equals(new MethodSignature(obbingCall)) == true && Utils.typesEqual(obbingCall.DeclaringType, decsStringsMethod.DeclaringType) == true)
                                                    {
                                                        patchingNeeded = true;
                                                        object deObedValue = Utils.decryptValue((MethodDef)ins2.Operand,
                                                            decCode, exeASM);
                                                        string val = (string)deObedValue;

                                                        int[] brRefs = Utils.checkForBRref(opCodes, opCodes[x]);
                                                        int[] brRefs1 = Utils.checkForBRref(opCodes, opCodes[x + 1]);

                                                        opCodes[x].OpCode = OpCodes.Ldstr;
                                                        opCodes[x].Operand = val;

                                                        opCodes[x + 1].OpCode = OpCodes.Nop;
                                                        opCodes[x + 1].Operand = null;

                                                        uint offset = 0;
                                                        for (int H = 0; H < opCodes.Length; H++)
                                                        {
                                                            opCodes[H].Offset = offset;

                                                            offset += (uint)opCodes[H].GetSize();
                                                        }

                                                        // Fix any references in operands that point to this instruction and the next one
                                                        if (brRefs.Length > 0 || brRefs1.Length > 0)
                                                        {
                                                            Utils.FixBR_Refs(brRefs, brRefs1, opCodes, x);
                                                        }

                                                        x = x + 1;
                                                        continue;
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    Debugger.Break();
                                                }
                                                break;
                                            }
                                        case "System.Int32":
                                            {
                                                try
                                                {
                                                    if (decsIntSig.Equals(new MethodSignature(obbingCall)) == true && Utils.typesEqual(obbingCall.DeclaringType, decsIntMethod.DeclaringType) == true)
                                                    {
                                                        patchingNeeded = true;
                                                        object deObedValue = Utils.decryptValue((MethodDef)ins2.Operand,
                                                            decCode, exeASM);
                                                        int val = (int)deObedValue;

                                                        int[] brRefs = Utils.checkForBRref(opCodes, opCodes[x]);
                                                        int[] brRefs1 = Utils.checkForBRref(opCodes, opCodes[x + 1]);


                                                        opCodes[x].OpCode = OpCodes.Ldc_I4;
                                                        opCodes[x].Operand = val;

                                                        opCodes[x + 1].OpCode = OpCodes.Nop;
                                                        opCodes[x + 1].Operand = null;

                                                        // Check for (long) conversion and nop
                                                        /*if (x < opCodes.Length - 3)
                                                        {
                                                            if (opCodes[x + 2].OpCode.Code == Code.Conv_I8)
                                                            {
                                                                opCodes[x + 2].OpCode = OpCodes.Nop;
                                                            }
                                                        }*/


                                                        uint offset = 0;
                                                        for (int H = 0; H < opCodes.Length; H++)
                                                        {
                                                            opCodes[H].Offset = offset;

                                                            offset += (uint)opCodes[H].GetSize();
                                                        }

                                                        // Fix any references in operands that point to this instruction and the next one
                                                        if (brRefs.Length > 0 || brRefs1.Length > 0)
                                                        {
                                                            Utils.FixBR_Refs(brRefs, brRefs1, opCodes, x);
                                                        }

                                                        x = x + 1;
                                                        continue;
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    Debugger.Break();
                                                }
                                                break;
                                            }
                                        case "System.Int64":
                                            {
                                                try
                                                {
                                                    if (decsLongSig.Equals(new MethodSignature(obbingCall)) == true && Utils.typesEqual(obbingCall.DeclaringType, decsLongMethod.DeclaringType) == true)
                                                    {
                                                        patchingNeeded = true;
                                                        object deObedValue = Utils.decryptValue((MethodDef)ins2.Operand,
                                                            decCode, exeASM);
                                                        long val = (long)deObedValue;

                                                        int[] brRefs = Utils.checkForBRref(opCodes, opCodes[x]);
                                                        int[] brRefs1 = Utils.checkForBRref(opCodes, opCodes[x + 1]);

                                                        opCodes[x].OpCode = OpCodes.Ldc_I8;
                                                        opCodes[x].Operand = val;

                                                        opCodes[x + 1].OpCode = OpCodes.Nop;
                                                        opCodes[x + 1].Operand = null;

                                                        uint offset = 0;
                                                        for (int H = 0; H < opCodes.Length; H++)
                                                        {
                                                            opCodes[H].Offset = offset;

                                                            offset += (uint)opCodes[H].GetSize();
                                                        }

                                                        // Fix any references in operands that point to this instruction and the next one
                                                        if (brRefs.Length > 0 || brRefs1.Length > 0)
                                                        {
                                                            Utils.FixBR_Refs(brRefs, brRefs1, opCodes, x);
                                                        }

                                                        x = x + 1;
                                                        continue;
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    Debugger.Break();
                                                }
                                                break;
                                            }
                                        case "System.Single":
                                            {
                                                try
                                                {
                                                    if (decsFloatSig.Equals(new MethodSignature(obbingCall)) == true && Utils.typesEqual(obbingCall.DeclaringType, decsFloatMethod.DeclaringType) == true)
                                                    {
                                                        patchingNeeded = true;
                                                        object deObedValue = Utils.decryptValue((MethodDef)ins2.Operand,
                                                            decCode, exeASM);
                                                        float val = (float)deObedValue;

                                                        int[] brRefs = Utils.checkForBRref(opCodes, opCodes[x]);
                                                        int[] brRefs1 = Utils.checkForBRref(opCodes, opCodes[x + 1]);

                                                        opCodes[x].OpCode = OpCodes.Ldc_R4;
                                                        opCodes[x].Operand = val;

                                                        opCodes[x + 1].OpCode = OpCodes.Nop;
                                                        opCodes[x + 1].Operand = null;

                                                        uint offset = 0;
                                                        for (int H = 0; H < opCodes.Length; H++)
                                                        {
                                                            opCodes[H].Offset = offset;

                                                            offset += (uint)opCodes[H].GetSize();
                                                        }

                                                        // Fix any references in operands that point to this instruction and the next one
                                                        if (brRefs.Length > 0 || brRefs1.Length > 0)
                                                        {
                                                            Utils.FixBR_Refs(brRefs, brRefs1, opCodes, x);
                                                        }

                                                        x = x + 1;
                                                        continue;
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    Debugger.Break();
                                                }
                                                break;
                                            }
                                        case "System.Double":
                                            {
                                                try
                                                {
                                                    if (decsDoubleSig.Equals(new MethodSignature(obbingCall)) == true && Utils.typesEqual(obbingCall.DeclaringType, decsDoubleMethod.DeclaringType) == true)
                                                    {
                                                        patchingNeeded = true;
                                                        object deObedValue = Utils.decryptValue((MethodDef)ins2.Operand,
                                                            decCode, exeASM);
                                                        double val = (double)deObedValue;

                                                        int[] brRefs = Utils.checkForBRref(opCodes, opCodes[x]);
                                                        int[] brRefs1 = Utils.checkForBRref(opCodes, opCodes[x + 1]);


                                                        opCodes[x].OpCode = OpCodes.Ldc_R8;
                                                        opCodes[x].Operand = val;

                                                        opCodes[x + 1].OpCode = OpCodes.Nop;
                                                        opCodes[x + 1].Operand = null;

                                                        uint offset = 0;
                                                        for (int H = 0; H < opCodes.Length; H++)
                                                        {
                                                            opCodes[H].Offset = offset;

                                                            offset += (uint)opCodes[H].GetSize();
                                                        }

                                                        // Fix any references in operands that point to this instruction and the next one
                                                        if (brRefs.Length > 0 || brRefs1.Length > 0)
                                                        {
                                                            Utils.FixBR_Refs(brRefs, brRefs1, opCodes, x);
                                                        }

                                                        x = x + 1;
                                                        continue;
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    Debugger.Break();
                                                }
                                                break;
                                            }
                                    }
                                }
                            }
                        }
                    }

                    // instruction loop 

                    // method level

                    try
                    {
                        if (patchingNeeded == true)
                        {
                            SavePatch(tar, opCodes);
                        }

                    }
                    catch (Exception)
                    {
                        Debugger.Break();
                    }
                }

                // Only in A namespace level search
                // Type level search
            }

            

            if (patchedTargets.Count > 0)
            {
                this.dnPatcher.Patch(patchedTargets.ToArray());

/*                if (shitToRemove.Count > 0)
                {
                    foreach (TypeDef t in shitToRemove)
                    {
                        if (t.Name != "Class_207") this.asm.Types.Remove(t);
                    }
                }*/

                this.dnPatcher.Save(this.fileLoc);
            }

            //this.dnPatcher.Save( exeASM.Location.Replace(".exe", ".cleaned.exe"));
            // end of script
        }
    }
}
/*
if (fullName.Contains("Delegate") == true)
{
    delegateClasses.Add(asm.Types[i]);
    asm.Types[i].Visibility = TypeAttributes.Public;


    MethodDef delConst = asm.Types[i].FindStaticConstructor();
    
    
    if (delConst != null)
    {
        delConst.Access = MethodAttributes.Public;
        Target t = getTarget(delConst);
        
        Instruction[] ogOpcodes = getInstructions(delConst);
        
        Instruction[] delTokenSnifferCodes = null;
        if (ogOpcodes != null)
        {
            if (ogOpcodes.Length > 0)
            {
                delTokenSnifferCodes = new Instruction[]
                {
                    ogOpcodes[0],
                    ogOpcodes[1],
                    ogOpcodes[2],
                    Instruction.Create(OpCodes.Call,
                        this.dnPatcher.BuildCall(typeof(A.DN), "SniffTokens", typeof(void),
                            new[] {typeof(int), typeof(int), typeof(int)})), // Console.WriteLine call
                    ogOpcodes[0],
                    ogOpcodes[1],
                    ogOpcodes[2],
                    ogOpcodes[3],
                    ogOpcodes[4]
                    //Instruction.Create(OpCodes.Ret) // Always return smth
                };

                t.Instructions = delTokenSnifferCodes;
                this.dnPatcher.Patch(t);
            }
        }
        

            //if (delTokenSnifferCodes != null)
            //{
                /*
                Target target = new Target()
                {
                    Namespace = delC.GetType().Namespace,
                    Class = delC.GetType().Name,
                    Method = delC.Name,
                    Instructions = delTokenSnifferCodes // you can also set it later
                };
                
                
                //t.Instructions = delTokenSnifferCodes;
                
                //methodsToPatch.Add(t);

                //delegateConstructs.Add(delC);
            //}
            */



// DEBUG SHIT
/*
List<string> strs = new List<string>();
foreach (MethodDef constr in delegateConstructs)
{
    strs.Add(constr.FullName);
}
string[] data = strs.ToArray();
DebugForm.DebugOutput(data, true);
*/
// END DEBUG SHIT

/*Instruction[] opCodes = {
    Instruction.Create(OpCodes.Ldstr, "Hello Sir 1"),
    Instruction.Create(OpCodes.Ldstr, "Hello Sir 2")
};
int[] indexe = {
    0, // index of Instruction
    2
};
Target target = new Target()
{
    Namespace = "Test",
    Class = "Program",
    Method = "Print",
    Instructions = opCodes,
    Indices = indexe
};*/