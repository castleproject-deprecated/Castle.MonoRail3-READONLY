// This file is a script that can be executed with the F# Interactive.  
// It can be used to explore and test the library project.
// Note that script files will not be part of the project build.
#r @"C:\dev\castle\Castle.MonoRail3\lib\Mono.Cecil.dll"
#r @"C:\dev\ms\ccimetadata\Bin\Debug\Microsoft.Cci.PeReader.dll"
#r @"C:\dev\ms\ccimetadata\Bin\Debug\Microsoft.Cci.MetadataModel.dll"
#r @"C:\dev\ms\ccimetadata\Bin\Debug\Microsoft.Cci.MetadataHelper.dll"
// #load "DescriptorBuilder.fs"
open Mono.Cecil
open System.Diagnostics
open Microsoft.Cci

// let asmname = @"C:\dev\castle\Castle.MonoRail3\src\TestWebApp\bin\TestWebApp.dll"
let asmname = @"C:\dev\mef\new_codeplex_release\oct-10\build\System.ComponentModel.Composition.UnitTests.dll"

let w = Stopwatch.StartNew()


w.Restart()
let host = new PeReader.DefaultHost()
let asmRef = host.LoadUnitFrom(asmname)
let asm = asmRef :?> IAssembly

for t in asm.GetAllTypes() do
    for att in t.Attributes do
        printf " %s" (att.ToString())

w.Stop()
printfn ""
printfn "CCI loading assembly %f" (w.Elapsed.TotalMilliseconds)



let name = System.Reflection.AssemblyName.GetAssemblyName(asmname)
w.Restart()
let refAsm = System.Reflection.Assembly.Load(name)
w.Stop()
printfn ""
printfn "Sys.Ref loading %f" (w.Elapsed.TotalMilliseconds)

w.Restart()
for t in refAsm.GetTypes() do
    for att in t.GetCustomAttributes(true) do 
        printf " %s" (att.ToString())

w.Stop()
printfn ""
printfn "Sys.Ref get custom attrs %f" (w.Elapsed.TotalMilliseconds)



w.Restart()

let assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(asmname)
for t in assembly.MainModule.Types do
    for att in t.CustomAttributes do
        printf " %s" att.AttributeType.Name

w.Stop()
printfn ""
printfn "Cecil loading assembly %f" (w.Elapsed.TotalMilliseconds)
