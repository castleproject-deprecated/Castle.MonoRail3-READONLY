
#light

namespace Castle.MonoRail.Mvc.Typed

module CecilReflectionMod = 

    open Mono.Cecil
    open Castle.MonoRail.Mvc.Typed
    open System.Collections.Generic
    open System.Collections.Concurrent
    open System.ComponentModel.Composition
    open System.Linq.Expressions
    open System.Reflection
    open Microsoft.FSharp.Collections
    open Microsoft.FSharp.Control.LazyExtensions

    let resolveRefType (t:TypeDefinition) : System.Type = 
        // this is weak as it relies on the load context
        // and ideally we should be agnostic about contexts
        let retType = System.Type.GetType t.FullName
        assert (retType.MetadataToken = t.MetadataToken.ToInt32())
        retType

    let lazyActionFunc = lazy(fun (action:MethodDefinition, controllerType:System.Type) ->
            
            let refMethods = controllerType.GetMethods(BindingFlags.Public ||| BindingFlags.Instance)
            let refMethod = seq<MethodInfo>(refMethods) |> 
                            Seq.find (fun m -> m.MetadataToken = action.MetadataToken.ToInt32()) 

            let target = Expression.Parameter (typeof<obj>, "target")
            let args = Expression.Parameter (typeof<obj[]>, "args")
            
            let rec buildConversionExpressions index = 
                if action.Parameters.Count = index then
                    List.empty<Expression>
                else
                    let p = action.Parameters.Item(index)
                    let argAccess = Expression.ArrayAccess (args, Expression.Constant index)
                    let exp = Expression.Convert (argAccess, resolveRefType (p.ParameterType.Resolve()))
                    let list = (buildConversionExpressions (index + 1))
                    list |> List.append [exp]

            let expParams = buildConversionExpressions 0

            let convExp = Expression.Convert(target, controllerType)
            let call = Expression.Call(convExp, refMethod, List.toArray(expParams))
            let mutable body : Expression = null

            if action.ReturnType.FullName = "System.Void" then
                body <- Expression.Block(call, Expression.Constant(null, typeof<obj>));
            else
                body <- Expression.Convert(call, typeof<obj>)

            let lambda = Expression.Lambda<System.Func<obj, obj[], obj>>(body, target, args)

            lambda.Compile
        )

    let fmt ((str:string), (l:list<obj>)) = 
        let array = List.toArray l
        System.String.Format(str, array)

    let (|ControllerName|Unamed|) (name : string) = 
        if name.EndsWith("Controller") then ControllerName else Unamed;

    let correctControllerName (name : string) = 
        match name with 
        | ControllerName -> name.Substring(0, name.Length - 10)
        | _ -> name

    let (|ValidType|InvalidType|) (typeref : TypeDefinition) = 
        if typeref.IsAbstract || typeref.IsInterface || typeref.IsNotPublic then 
            InvalidType
        else
            ValidType(typeref)

    // given a seq, builds a dictionary
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

    type CecilBasedActionDescriptor() = 
        class
            inherit ActionDescriptor()
        
            new (func:System.Func<obj, obj[], obj>, name:string, parameters:seq<ParameterDescriptor>)  = 
                    base.Name <- name
                    base.Action <- func
                    base.Parameters <- Seq.toArray(parameters)
                    new CecilBasedActionDescriptor()
        end


    [<Export(typeof<ControllerDescriptorBuilder>)>]
    [<PartCreationPolicy(CreationPolicy.Shared)>]
    type CecilBasedControllerDescriptorBuilder = 
        class 

            val asm2AsmRef : ConcurrentDictionary<Assembly, ModuleDefinition>

            inherit ControllerDescriptorBuilder 
        
            // parameterless constructor needs to be added explictly
            new() = 
                { 
                    asm2AsmRef = new ConcurrentDictionary<Assembly, ModuleDefinition>() 
                }

            // public virtual ControllerDescriptor Build(Type controllerType)
            override x.Build(controllerType) = 
                let name = correctControllerName(controllerType.Name).ToLower()

                let desc = new ControllerDescriptor(controllerType, name, null)
                let asm = controllerType.Assembly

                let assemblyDef = AssemblyDefinition.ReadAssembly(asm.Location)

                let allTypes : seq<TypeDefinition> = 
                    assemblyDef.Modules |> 
                    Seq.map (fun m -> seq<TypeDefinition>(m.Types)) |> 
                    Seq.concat // |>
                    // shall we get nested types? help test cases
                    // Seq.map (fun t -> seq<TypeDefinition>(t.NestedTypes)) |> 
                    // Seq.concat

                // should be cached per assembly instance
                let mydict = buildMap (allTypes) (fun i -> 
                    match i with 
                    | ValidType(i) -> Some(i.FullName)
                    | _ -> None) (fun i -> i)

                let res, controllerTypeDef = mydict.TryGetValue controllerType.FullName

                if not res then 
                    failwith (fmt ("Could not find TypeReference for controller {0}", [controllerType.FullName]))

                let actions = seq<MethodDefinition>(controllerTypeDef.Methods) |> 
                              Seq.filter (fun m -> m.IsPublic && m.IsSpecialName = false && m.IsStatic = false) |>
                              Seq.toArray

                // side effecty (calls Add on collection)
                let rec buildActionDescriptors i = 
                    if actions.Length = i then
                        ()
                    else
                        let act = actions.[i]
                        let x = lazyActionFunc.Value (act, controllerType)
                        let func = x(System.Runtime.CompilerServices.DebugInfoGenerator.CreatePdbGenerator())

                        let rec recbuildParamDescriptor index  = 
                            if act.Parameters.Count = index then
                                []
                            else 
                                let param = act.Parameters.Item(index)
                                let paramType = resolveRefType (param.ParameterType.Resolve())
                                let paramDesc = new ParameterDescriptor(param.Name, paramType)
                                recbuildParamDescriptor(index + 1) |> List.append [paramDesc] 
                    
                        let paramDefs = recbuildParamDescriptor 0
                        let actionName = act.Name
                        let actionDesc = new CecilBasedActionDescriptor(func, actionName, paramDefs)
                        desc.Actions.Add actionDesc
                        buildActionDescriptors(i + 1) 
                        ()
            
                buildActionDescriptors 0

                desc
        end

