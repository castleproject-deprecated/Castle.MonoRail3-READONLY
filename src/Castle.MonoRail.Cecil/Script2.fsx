#r @"C:\dev\castle\Castle.MonoRail3\lib\Mono.Cecil.dll"

open Mono.Cecil
open System.Collections.Generic

let asmname = @"C:\dev\castle\Castle.MonoRail3\src\TestWebApp\bin\TestWebApp.dll"
let assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(asmname)

let mainmod = assembly

let allTypes : seq<TypeDefinition> = 
    mainmod.Modules |> 
    Seq.map (fun m -> seq<TypeDefinition>(m.Types)) |> 
    Seq.concat

let (|ValidType|InvalidType|) (typeref : TypeDefinition) = 
    if typeref.IsAbstract || typeref.IsInterface || typeref.IsNotPublic then 
        InvalidType
    else
        ValidType(typeref)

let buildMap (input:seq<'a>) (k: 'a -> 'K option) (v: 'a -> 'V)  = 
    let dict = new Dictionary<'K, 'V>()
    input |> Seq.iter (fun item -> 
        let kval = k item
        let vval = v item
        match kval with 
        | Some(t) -> dict.[t] <- vval 
        | _ -> ()
    ) 
    dict

let mydict = buildMap (allTypes) (fun i -> 
    match i with 
    | ValidType(i) -> Some(i.FullName)
    | _ -> None) (fun i -> i)

let typeDef = mydict.["TestWebApp.Controller.HomeController"]

let actions = seq<MethodDefinition>(typeDef.Methods) |> Seq.filter (fun m -> m.IsPublic && m.IsSpecialName = false && m.IsStatic = false)
    

