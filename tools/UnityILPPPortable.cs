using System;
using System.IO;
using Mono.Cecil;

// Re-emit copies of Unity's managed ILPP assemblies without x64 ReadyToRun code.
// The original Unity installation's assemblies are never rewritten.
internal static class UnityILPPPortable
{
    private static void Main(string[] args)
    {
        using (var resolver = new DefaultAssemblyResolver())
        {
            resolver.AddSearchDirectory(args[0]);
            resolver.AddSearchDirectory(args[2]);
            Directory.CreateDirectory(args[1]);
            int count = 0;
            foreach (string file in Directory.GetFiles(args[0], "*.dll"))
            {
                using (var module = ModuleDefinition.ReadModule(file,
                    new ReaderParameters { AssemblyResolver = resolver, InMemory = true }))
                {
                    module.Architecture = TargetArchitecture.I386;
                    module.Attributes = (module.Attributes | ModuleAttributes.ILOnly)
                        & ~ModuleAttributes.Required32Bit & ~ModuleAttributes.Preferred32Bit;
                    module.Write(Path.Combine(args[1], Path.GetFileName(file)));
                }
                count++;
            }
            Console.WriteLine("Prepared " + count + " portable managed ILPP assemblies.");
        }
    }
}
