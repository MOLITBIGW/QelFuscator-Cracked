using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuscator.A.Utils;
using System.Linq;
using System;

namespace Obfuscator.A.P.AddRandoms
{
    internal class RandomOutlinedMethods : SecureRandoms
    {
        private static readonly Random _rnd = new Random();

        public static void Execute(ModuleDef module)
        {
            foreach (var type in module.Types)
            {
                if (type.IsInterface || type.IsAbstract || type.Name.StartsWith("<"))
                    continue;

                foreach (var method in type.Methods.ToArray())
                {
                    AddJunkMethods(type, method, module);
                }
            }
        }

        private static void AddJunkMethods(TypeDef type, MethodDef sourceMethod, ModuleDef module)
        {
            var stringMethod = CreateReturnMethodDef(
                GenerateOpaqueString(Next(30, 60)), sourceMethod, module);
            type.Methods.Add(stringMethod);

            var intMethod = CreateArithmeticIntReturnMethod(sourceMethod, module);
            type.Methods.Add(intMethod);

            var doubleMethod = CreateBranchingDoubleReturnMethod(sourceMethod, module);
            type.Methods.Add(doubleMethod);

            var boolMethod = CreateBooleanJunkMethod(sourceMethod, module);
            type.Methods.Add(boolMethod);

            var voidMethod = CreateComplexVoidJunkMethod(type, sourceMethod, module);
            type.Methods.Add(voidMethod);
        }

        #region Method Generators

        private static MethodDef CreateReturnMethodDef(string value, MethodDef sourceMethod, ModuleDef module)
        {
            var sig = MethodSig.CreateStatic(module.CorLibTypes.String);
            var newMethod = CreateMethod(module, sig);

            var il = newMethod.Body.Instructions;
            il.Add(Instruction.Create(OpCodes.Ldstr, value.Substring(0, value.Length / 2)));
            il.Add(Instruction.Create(OpCodes.Ldstr, value.Substring(value.Length / 2)));
            il.Add(Instruction.Create(OpCodes.Call, module.Import(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }))));
            il.Add(Instruction.Create(OpCodes.Ret));

            return newMethod;
        }

        private static MethodDef CreateArithmeticIntReturnMethod(MethodDef sourceMethod, ModuleDef module)
        {
            var sig = MethodSig.CreateStatic(module.CorLibTypes.Int32);
            var newMethod = CreateMethod(module, sig);

            var il = newMethod.Body.Instructions;

            int a = Next(1000, 9999);
            int b = Next(1, 999);

            il.Add(Instruction.Create(OpCodes.Ldc_I4, a));
            il.Add(Instruction.Create(OpCodes.Ldc_I4, b));
            il.Add(Instruction.Create(OpCodes.Add));
            il.Add(Instruction.Create(OpCodes.Ldc_I4, b));
            il.Add(Instruction.Create(OpCodes.Sub));
            il.Add(Instruction.Create(OpCodes.Ret));

            return newMethod;
        }

        private static MethodDef CreateBranchingDoubleReturnMethod(MethodDef sourceMethod, ModuleDef module)
        {
            var sig = MethodSig.CreateStatic(module.CorLibTypes.Double);
            var newMethod = CreateMethod(module, sig);

            var il = newMethod.Body.Instructions;

            double val1 = _rnd.NextDouble() * 10000;
            double val2 = _rnd.NextDouble() * 10000;

            il.Add(Instruction.Create(OpCodes.Ldc_R8, val1));
            il.Add(Instruction.Create(OpCodes.Ldc_R8, val2));
            il.Add(Instruction.Create(OpCodes.Cgt));  

            var retVal1 = Instruction.Create(OpCodes.Ldc_R8, val1);
            var retVal2 = Instruction.Create(OpCodes.Ldc_R8, val2);

            il.Add(Instruction.Create(OpCodes.Brtrue_S, retVal1));
            il.Add(retVal2);
            il.Add(Instruction.Create(OpCodes.Ret));
            il.Add(retVal1);
            il.Add(Instruction.Create(OpCodes.Ret));

            return newMethod;
        }

        private static MethodDef CreateBooleanJunkMethod(MethodDef sourceMethod, ModuleDef module)
        {
            var sig = MethodSig.CreateStatic(module.CorLibTypes.Boolean);
            var newMethod = CreateMethod(module, sig);

            var il = newMethod.Body.Instructions;

            int rndNum = Next(1, 10000);

            il.Add(Instruction.Create(OpCodes.Ldc_I4, rndNum));
            il.Add(Instruction.Create(OpCodes.Ldc_I4_2));
            il.Add(Instruction.Create(OpCodes.Rem)); 
            il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            il.Add(Instruction.Create(OpCodes.Ceq));
            il.Add(Instruction.Create(OpCodes.Ret));

            return newMethod;
        }

        private static MethodDef CreateComplexVoidJunkMethod(TypeDef type, MethodDef sourceMethod, ModuleDef module)
        {
            var sig = MethodSig.CreateStatic(module.CorLibTypes.Void);
            var newMethod = CreateMethod(module, sig);

            var il = newMethod.Body.Instructions;

            newMethod.Body.Variables.Add(new Local(module.CorLibTypes.Int32));
            newMethod.Body.Variables.Add(new Local(module.CorLibTypes.Boolean));
            newMethod.Body.Variables.Add(new Local(module.CorLibTypes.Double));
            newMethod.Body.InitLocals = true;

            il.Add(Instruction.Create(OpCodes.Ldc_I4, Next(10, 100)));
            il.Add(Instruction.Create(OpCodes.Stloc_0));

            il.Add(Instruction.Create(_rnd.Next(2) == 0 ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1));
            il.Add(Instruction.Create(OpCodes.Stloc_1));

            il.Add(Instruction.Create(OpCodes.Ldc_R8, _rnd.NextDouble() * 5000));
            il.Add(Instruction.Create(OpCodes.Stloc_2));

            var skipBranch = Instruction.Create(OpCodes.Nop);
            il.Add(Instruction.Create(OpCodes.Ldloc_1));
            il.Add(Instruction.Create(OpCodes.Brfalse_S, skipBranch));

            il.Add(Instruction.Create(OpCodes.Ldloc_0));
            il.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            il.Add(Instruction.Create(OpCodes.Add));
            il.Add(Instruction.Create(OpCodes.Pop));

            var calledMethod = FindOrCreateRandomJunkMethod(type, module);
            il.Add(Instruction.Create(OpCodes.Call, calledMethod));
            il.Add(Instruction.Create(OpCodes.Pop));

            il.Add(skipBranch);

            il.Add(Instruction.Create(OpCodes.Ret));

            return newMethod;
        }

        private static MethodDef FindOrCreateRandomJunkMethod(TypeDef type, ModuleDef module)
        {
            var existing = type.Methods.FirstOrDefault(m => m.ReturnType.FullName == "System.Int32" && m.Parameters.Count == 0);
            if (existing != null) return existing;

            var sig = MethodSig.CreateStatic(module.CorLibTypes.Int32);
            var newMethod = CreateMethod(module, sig);

            var il = newMethod.Body.Instructions;
            il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            il.Add(Instruction.Create(OpCodes.Ret));
            type.Methods.Add(newMethod);

            return newMethod;
        }

        #endregion

        #region Helpers

        private static MethodDef CreateMethod(ModuleDef module, MethodSig sig)
        {
            return new MethodDefUser(
                GenerateRandomString(30),
                sig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            {
                Body = new CilBody()
            };
        }

        private static string GenerateOpaqueString(int length)
        {
            const string chars = "贪<=貪员<=員贴<=貼康熙字典漢語大字典爾雅astuvwxyHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[_rnd.Next(chars.Length)];
            }
            return new string(buffer);
        }

        private static new int Next(int min, int max)
        {
            return _rnd.Next(min, max);
        }

        #endregion
    }
}
