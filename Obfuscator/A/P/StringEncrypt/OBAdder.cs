using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Text;

namespace Obfuscator.A.Utils
{
    internal class OBAdder
    {
        private static Random random = new Random();

        public static void Execute(ModuleDefMD module)
        {
            MethodDef cctor = module.GlobalType.FindOrCreateStaticConstructor();
            string originalValue = "Obfuscated with " + Reference.Name + " v" + Reference.Version;
            byte[] encrypted = XorEncrypt(Encoding.UTF8.GetBytes(originalValue), 0x5A);
            string methodName = GenerateRandomName(12);
            MethodDef obfMethod = CreateDecryptMethod(module, encrypted, (byte)0x5A, methodName);
            Console.WriteLine($"  [OBADDER] Adding method \"{obfMethod.Name}\"...");
            module.GlobalType.Methods.Add(obfMethod);
        }

        private static MethodDef CreateDecryptMethod(ModuleDef module, byte[] encryptedData, byte xorKey, string methodName)
        {
            var corlib = module.CorLibTypes;
            var stringType = corlib.String;
            var byteArrayType = new SZArraySig(corlib.Byte);

            MethodDef method = new MethodDefUser(
                methodName,
                MethodSig.CreateStatic(stringType),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            {
                Body = new CilBody()
            };

            var body = method.Body;
            var instr = body.Instructions;

            body.Variables.Add(new Local(byteArrayType));
            body.Variables.Add(new Local(byteArrayType));
            body.Variables.Add(new Local(module.CorLibTypes.Int32));
            body.Variables.Add(new Local(module.CorLibTypes.Int32));

            body.InitLocals = true;

            instr.Add(Instruction.Create(OpCodes.Ldc_I4, encryptedData.Length));
            instr.Add(Instruction.Create(OpCodes.Newarr, corlib.Byte));
            instr.Add(Instruction.Create(OpCodes.Stloc_0));

            for (int i = 0; i < encryptedData.Length; i++)
            {
                instr.Add(Instruction.Create(OpCodes.Ldloc_0));
                instr.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                instr.Add(Instruction.Create(OpCodes.Ldc_I4, encryptedData[i]));
                instr.Add(Instruction.Create(OpCodes.Stelem_I1));
            }

            instr.Add(Instruction.Create(OpCodes.Ldloc_0));
            instr.Add(Instruction.Create(OpCodes.Ldlen));
            instr.Add(Instruction.Create(OpCodes.Conv_I4));
            instr.Add(Instruction.Create(OpCodes.Stloc_3));

            instr.Add(Instruction.Create(OpCodes.Ldloc_3));
            instr.Add(Instruction.Create(OpCodes.Newarr, corlib.Byte));
            instr.Add(Instruction.Create(OpCodes.Stloc_1));

            instr.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            instr.Add(Instruction.Create(OpCodes.Stloc_2));

            var loopCheck = Instruction.Create(OpCodes.Ldloc_2);
            instr.Add(loopCheck);

            instr.Add(Instruction.Create(OpCodes.Ldloc_3));
            var loopEnd = Instruction.Create(OpCodes.Bge_S, null);
            instr.Add(Instruction.Create(OpCodes.Bge_S, loopEnd));

            instr.Add(Instruction.Create(OpCodes.Ldloc_1));
            instr.Add(Instruction.Create(OpCodes.Ldloc_2));
            instr.Add(Instruction.Create(OpCodes.Ldloc_0));
            instr.Add(Instruction.Create(OpCodes.Ldloc_2));
            instr.Add(Instruction.Create(OpCodes.Ldelem_U1));
            instr.Add(Instruction.Create(OpCodes.Ldc_I4, xorKey));
            instr.Add(Instruction.Create(OpCodes.Xor));
            instr.Add(Instruction.Create(OpCodes.Conv_U1));
            instr.Add(Instruction.Create(OpCodes.Stelem_I1));

            instr.Add(Instruction.Create(OpCodes.Ldloc_2));
            instr.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            instr.Add(Instruction.Create(OpCodes.Add));
            instr.Add(Instruction.Create(OpCodes.Stloc_2));

            instr.Add(Instruction.Create(OpCodes.Br_S, loopCheck));

            loopEnd.Operand = Instruction.Create(OpCodes.Nop);
            instr.Add(loopEnd.Operand);

            var encodingGetString = GetEncodingGetStringMethod(module);
            instr.Add(Instruction.Create(OpCodes.Call, GetEncodingUTF8Property(module)));
            instr.Add(Instruction.Create(OpCodes.Ldloc_1));
            instr.Add(Instruction.Create(OpCodes.Callvirt, encodingGetString));
            instr.Add(Instruction.Create(OpCodes.Ret));

            instr.Insert(2, Instruction.Create(OpCodes.Nop));
            instr.Insert(instr.Count - 2, Instruction.Create(OpCodes.Nop));
            instr.Insert(instr.Count - 2, Instruction.Create(OpCodes.Nop));

            return method;
        }

        private static MethodDefUser GetEncodingUTF8Property(ModuleDef module)
        {
            var encodingType = module.Import(typeof(Encoding));
            var utf8Prop = encodingType.ResolveTypeDef().FindProperty("UTF8");
            var getter = utf8Prop.GetMethod;
            return module.Import(getter);
        }

        private static MethodDefUser GetEncodingGetStringMethod(ModuleDef module)
        {
            var encodingType = module.Import(typeof(Encoding));
            var getStringMethod = encodingType.ResolveTypeDef().FindMethod("GetString", m =>
                m.Parameters.Count == 1 && m.Parameters[0].Type.FullName == "System.Byte[]");
            return module.Import(getStringMethod);
        }

        private static byte[] XorEncrypt(byte[] data, byte key)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                result[i] = (byte)(data[i] ^ key);
            return result;
        }

        private static string GenerateRandomName(int length)
        {
            const string chars = "贪<=貪 ,员<=員 ,贴<=貼康熙字典漢語大字典爾雅";
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
                buffer[i] = chars[random.Next(chars.Length)];
            return new string(buffer);
        }
    }
}
