using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Text;

namespace Obfuscator.A.P
{
    internal class StringObfuscate
    {
        private static readonly byte[] xorKey = Encoding.UTF8.GetBytes("NIGGER");

        public static void Execute(ModuleDefMD module)
        {
            InjectDecryptMethod(module);

            ObfuscateStrings(module);

            ObfuscateNames(module);
        }

        private static void ObfuscateStrings(ModuleDefMD module)
        {
            var decryptMethod = module.GlobalType.FindMethod("DecryptString");

            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;

                    var instrs = method.Body.Instructions;
                    for (int i = 0; i < instrs.Count; i++)
                    {
                        var instr = instrs[i];
                        if (instr.OpCode == OpCodes.Ldstr)
                        {
                            string original = (string)instr.Operand;
                            if (string.IsNullOrEmpty(original)) continue;

                            string encrypted = EncryptString(original);
                            instr.Operand = encrypted;

                            instrs.Insert(i + 1, Instruction.Create(OpCodes.Call, decryptMethod));
                            i++;
                        }
                    }
                }
            }
        }

        private static void ObfuscateNames(ModuleDefMD module)
        {
            foreach (var type in module.Types)
            {
                if (type == module.GlobalType)
                    continue;

                type.Name = GenerateRandomName();

                foreach (var method in type.Methods)
                {
                    if (method.IsConstructor) continue; 
                    method.Name = GenerateRandomName();
                }

                foreach (var field in type.Fields)
                {
                    field.Name = GenerateRandomName();
                }
            }
        }

        private static string GenerateRandomName(int length = 10)
        {
            const string chars = "贪<=貪 ,员<=員 ,贴<=貼康熙字典漢語大字典爾雅";
            var random = new Random();
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(chars[random.Next(chars.Length)]);
            return sb.ToString();
        }

        private static string EncryptString(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] ^= xorKey[i % xorKey.Length];
            return Convert.ToBase64String(bytes);
        }

        private static void InjectDecryptMethod(ModuleDefMD module)
        {
            var globalType = module.GlobalType;
            if (globalType.FindMethod("DecryptString") != null)
                return;

            var decryptMethod = new MethodDefUser(
                "DecryptString",
                MethodSig.CreateStatic(module.CorLibTypes.String, module.CorLibTypes.String),
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);

            globalType.Methods.Add(decryptMethod);

            var body = new CilBody();
            decryptMethod.Body = body;

            var instrs = body.Instructions;



            var corLibTypes = module.CorLibTypes;

            body.Variables.Add(new Local(new SZArraySig(corLibTypes.Byte))); 
            body.Variables.Add(new Local(corLibTypes.Int32));
            body.InitLocals = true;

            instrs.Add(Instruction.Create(OpCodes.Ldarg_0));
            instrs.Add(Instruction.Create(OpCodes.Ret));
        }
    }
}
