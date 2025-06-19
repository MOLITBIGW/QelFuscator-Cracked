using dnlib.DotNet;
using Obfuscator.A.P.Analyzer;
using Obfuscator.A.Utils;
using System;

namespace Obfuscator.A.P
{
    internal class Renamer : SecureRandoms
    {
 
        public static void Execute(ModuleDefMD module)
        {
            if (Program.IsWinForms || Program.FileExtension.Contains("dll") || module.HasResources)
                return;

            foreach (var type in module.Types)
            {
                RenameType(type);
                RenameMethods(type);
                RenameProperties(type);
                RenameFields(type);
                RenameEvents(type);
            }
        }

        private static void RenameType(TypeDef type)
        {
            if (!CanRename(type))
                return;

            Console.WriteLine($"  [RENAMER] Renaming Type: \"{type.Name}\"...");
            type.Name = GenerateRandomString(Next(50, 70));

            Console.WriteLine($"  [RENAMER] Renaming Namespace: \"{type.Namespace}\"...");
            type.Namespace = GenerateRandomString(Next(50, 70));
        }

        private static void RenameMethods(TypeDef type)
        {
            foreach (var method in type.Methods)
            {
                if (!CanRename(method))
                    continue;

                Console.WriteLine($"  [RENAMER] Renaming Method: \"{method.Name}\"...");
                method.Name = GenerateRandomString(Next(50, 70));

                foreach (var param in method.Parameters)
                {
                    Console.WriteLine($"  [RENAMER] Renaming Parameter: \"{param.Name}\" in Method: \"{method.Name}\"...");
                    param.Name = GenerateRandomString(Next(50, 70));
                }
            }
        }

        private static void RenameProperties(TypeDef type)
        {
            foreach (var prop in type.Properties)
            {
                if (!CanRename(prop))
                    continue;

                Console.WriteLine($"  [RENAMER] Renaming Property: \"{prop.Name}\"...");
                prop.Name = GenerateRandomString(Next(50, 70));
            }
        }

        private static void RenameFields(TypeDef type)
        {
            foreach (var field in type.Fields)
            {
                if (!CanRename(field))
                    continue;

                Console.WriteLine($"  [RENAMER] Renaming Field: \"{field.Name}\"...");
                field.Name = GenerateRandomString(Next(50, 70));
            }
        }

        private static void RenameEvents(TypeDef type)
        {
            foreach (var evt in type.Events)
            {
                if (!CanRename(evt))
                    continue;

                Console.WriteLine($"  [RENAMER] Renaming Event: \"{evt.Name}\"...");
                evt.Name = GenerateRandomString(Next(50, 70));
            }
        }


        public static bool CanRename(object obj)
        {
            iAnalyze analyzer = null;

            if (obj is TypeDef)
                analyzer = new TypeDefAnalyzer();
            else if (obj is MethodDef)
                analyzer = new MethodDefAnalyzer();
            else if (obj is FieldDef)
                analyzer = new FieldDefAnalyzer();
            else if (obj is EventDef)
                analyzer = new EventDefAnalyzer();
            else
                return false;

            return analyzer.Execute(obj);
        }
    }
}
