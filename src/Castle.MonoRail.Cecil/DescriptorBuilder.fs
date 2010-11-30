
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

    let resolveRefType (t:TypeDefinition) = 
        // this is weak as it relies on the load context
        // and ideally we should be agnostic about contexts
        let retType = System.Type.GetType t.FullName
        assert (retType.MetadataToken = t.MetadataToken.ToInt32())
        retType

    let lazyActionFunc (action:MethodDefinition, controllerType:System.Type) : Lazy<System.Func<obj,obj[],obj>> = 
        lazy
            let refMethods = controllerType.GetMethods(BindingFlags.Public ||| BindingFlags.Instance)
            let refMethod = refMethods 
                            |> Seq.find (fun m -> m.MetadataToken = action.MetadataToken.ToInt32()) 

            let target = Expression.Parameter (typeof<obj>, "target")
            let args = Expression.Parameter (typeof<obj[]>, "args")
            
            let rec buildConversionExpressions index = 
                if action.Parameters.Count = index then
                    List.empty<Expression>
                else
                    let p = action.Parameters.Item(index)
                    let argAccess = Expression.ArrayAccess (args, Expression.Constant index)
                    let exp = Expression.Convert (argAccess, resolveRefType (p.ParameterType.Resolve())) :> Expression
                    exp :: buildConversionExpressions (index + 1)
                    // let list = (buildConversionExpressions (index + 1))
                    // list |> List.append [exp]

            let expParams = buildConversionExpressions 0 |> Seq.toArray

            let convExp = Expression.Convert(target, controllerType)
            let call = Expression.Call(convExp, refMethod, expParams)

            let body : Expression =
              if action.ReturnType.FullName = "System.Void"
                then Expression.Block(call, Expression.Constant(null, typeof<obj>)) :> Expression
                else Expression.Convert(call, typeof<obj>) :> Expression

            let lambda = Expression.Lambda<_>(body, target, args)

            lambda.Compile()

    let (|ControllerName|Unamed|) (name : string) = 
        if name.EndsWith("Controller") then ControllerName else Unamed;

    let correctControllerName (name : string) = 
        match name with 
        | ControllerName -> name.Substring(0, name.Length - 10)
        | _ -> name

    let isValidType (typeref : TypeDefinition) = 
        not (typeref.IsAbstract || typeref.IsInterface || typeref.IsNotPublic)

    type CecilBasedActionDescriptor(func: Lazy<System.Func<obj,obj[],obj>>, name: string, parameters: seq<ParameterDescriptor>) as this = 
        inherit ActionDescriptor()
        let lazyFunc = func
        do
            // this.lazyFunc <- func
            this.Name <- name
            // this.Action <- func
            this.Parameters <- Seq.toArray(parameters)

        // member c.R with get() = radius and set(inp) = radius <- inp 
        override c.Action with get() = lazyFunc.Value 
            


    [<Export(typeof<ControllerDescriptorBuilder>)>]
    [<PartCreationPolicy(CreationPolicy.Shared)>]
    type CecilBasedControllerDescriptorBuilder = 
        val asm2AsmRef : ConcurrentDictionary<Assembly, ModuleDefinition>
        new() = { asm2AsmRef = new ConcurrentDictionary<Assembly, ModuleDefinition>() }
        inherit ControllerDescriptorBuilder 

        // public virtual ControllerDescriptor Build(Type controllerType)
        override x.Build controllerType = 
            let name = correctControllerName(controllerType.Name).ToLower()

            let desc = ControllerDescriptor(controllerType, name, null)
            let asm = controllerType.Assembly

            let assemblyDef = AssemblyDefinition.ReadAssembly(asm.Location)

            let allTypes = 
                assemblyDef.Modules |> Seq.collect (fun m -> m.Types)

            // should be cached per assembly instance
            let mydict = 
                allTypes
                |> Seq.filter isValidType
                |> Seq.map (fun t -> t.FullName,t)
                |> dict

            let res, controllerTypeDef = mydict.TryGetValue controllerType.FullName

            if not res then 
                failwithf "Could not find TypeReference for controller %s" controllerType.FullName

            let actions = 
                controllerTypeDef.Methods
                |> Seq.filter (fun m -> m.IsPublic && m.IsSpecialName = false && m.IsStatic = false)
                |> Seq.toArray

            // side effecty (calls Add on collection)
            let rec buildActionDescriptors i = 
                if actions.Length = i then
                    ()
                else
                    let act = actions.[i]
                    let lazyActionFuncInstance = lazyActionFunc (act, controllerType)

                    let rec recbuildParamDescriptor index  = 
                        if act.Parameters.Count = index then
                            []
                        else 
                            let param = act.Parameters.Item(index)
                            let paramType = resolveRefType (param.ParameterType.Resolve())
                            let paramDesc = ParameterDescriptor(param.Name, paramType)
                            recbuildParamDescriptor(index + 1) |> List.append [paramDesc] 
                    
                    let paramDefs = recbuildParamDescriptor 0
                    let actionName = act.Name
                    let actionDesc = CecilBasedActionDescriptor(lazyActionFuncInstance, actionName, paramDefs)
                    desc.Actions.Add actionDesc
                    buildActionDescriptors(i + 1) 
                    ()
            
            buildActionDescriptors 0

            desc

