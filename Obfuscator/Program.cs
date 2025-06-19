using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Obfuscator.A;
using Obfuscator.A.P;
using Obfuscator.A.P.AddRandoms;
using Obfuscator.A.P.MetaStrip;
using Obfuscator.A.P.StringEncrypt;
using Obfuscator.A.Utils;
using System;
using System.IO;

internal class Program
{
    public static bool IsWinForms = false;
    public static string FileExtension = string.Empty;

    private static void Main()
	{
        Console.Title = Reference.Name + " v" + Reference.Version;

        Console.WriteLine(@"
 ██████╗ ██████╗ ███████╗██╗   ██╗███████╗ ██████╗ █████╗ ████████╗ ██████╗ ██████╗  
██╔═══██╗██╔══██╗██╔════╝██║   ██║██╔════╝██╔════╝██╔══██╗╚══██╔══╝██╔═══██╗██╔══██╗
██║   ██║██████╔╝█████╗  ██║   ██║███████╗██║     ███████║   ██║   ██║   ██║██████╔╝
██║   ██║██╔══██╗██╔══╝  ██║   ██║╚════██║██║     ██╔══██║   ██║   ██║   ██║██╔══██╗
╚██████╔╝██████╔╝██║     ╚██████╔╝███████║╚██████╗██║  ██║   ██║   ╚██████╔╝██║  ██║
 ╚═════╝ ╚═════╝ ╚═╝      ╚═════╝ ╚══════╝ ╚═════╝╚═╝  ╚═╝   ╚═╝    ╚═════╝ ╚═╝  ╚═╝
");
        Console.WriteLine("Drag & drop your file here :");
        string file = Console.ReadLine().Replace("\"", "");

        FileExtension = Path.GetExtension(file);
        if (FileExtension.Contains("exe"))
        {
            Console.WriteLine("If your file is Windows Forms app Type (true) If Console app Type (false)");
            IsWinForms = Convert.ToBoolean(Console.ReadLine());
        }


        ModuleDefMD module = ModuleDefMD.Load(file);
        string fileName = Path.GetFileNameWithoutExtension(file);

        Console.WriteLine("-----------------------------------------------------------------");
        Console.WriteLine(Reference.Prefix + "Loaded : " + module.Assembly.FullName);
        Console.WriteLine(Reference.Prefix + "Has Resources : " + module.HasResources);
        if (FileExtension.Contains("exe"))
        {
            Console.WriteLine(Reference.Prefix + "Is Windows Forms : " + IsWinForms);
        }
        Console.WriteLine(Reference.Prefix + "File Extension : " + FileExtension.Replace(".", "").ToUpper());
        Console.WriteLine("-----------------------------------------------------------------");
        Console.WriteLine();


        Execute(module);
        Console.WriteLine(Reference.Prefix + "Saving file...");

        var opts = new ModuleWriterOptions(module);
        opts.Logger = DummyLogger.NoThrowInstance;
        module.Write(@"C:\Users\" + Environment.UserName + @"\Desktop\" + fileName + "_protected" + FileExtension, opts);

        Console.WriteLine(Reference.Prefix + "Done!");
        Console.ReadKey();
    }

    private static void Execute(ModuleDefMD module)
	{
        Console.WriteLine(Reference.Prefix + "Applying 'Renamer' obfuscation...");
		Renamer.Execute(module: module);
        Console.WriteLine(Reference.Prefix + "Applying 'RandomOutlinedMethods' obfuscation...");
        RandomOutlinedMethods.Execute(module: module);
        Console.WriteLine(Reference.Prefix + "Applying 'MetaStrip' obfuscation...");
        MetaStrip.Execute(module: module);
        Console.WriteLine(Reference.Prefix + "Applying 'OBAdder' obfuscation...");
        Console.WriteLine(Reference.Prefix + "Applying 'StringEncryption' obfuscation...");
        StringEncryption.Execute(module);
    }
}
