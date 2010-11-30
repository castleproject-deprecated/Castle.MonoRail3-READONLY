
namespace Castle.MonoRail.Mvc.Typed

module CecilReflectionMod2 = 

    open Castle.MonoRail.Mvc.Typed
    open System.Collections.Generic
    open System.Collections.Concurrent
    open System.ComponentModel.Composition
    open System.Linq.Expressions
    open System.Reflection
    open Microsoft.FSharp.Collections
    open Microsoft.FSharp.Control.LazyExtensions

    let lazyActionFunc (action:MethodInfo, controllerType:System.Type) : Lazy<System.Func<obj,obj[],obj>> = 
        lazy
            let target = Expression.Parameter (typeof<obj>, "target")
            let args = Expression.Parameter (typeof<obj[]>, "args")
            
            let rec buildConversionExpressions (parameters:ParameterInfo []) index count = 
                if count = index then
                    List.empty<Expression>
                else
                    let p = parameters.[index]
                    let argAccess = Expression.ArrayAccess (args, Expression.Constant index)
                    let exp = Expression.Convert (argAccess, p.ParameterType) :> Expression
                    exp :: buildConversionExpressions parameters (index + 1) count

            let parameters = action.GetParameters()
            let expParams = buildConversionExpressions parameters 0 parameters.Length |> Seq.toArray

            let convExp = Expression.Convert(target, controllerType)
            let call = Expression.Call(convExp, action, expParams)

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

    type CecilBasedActionDescriptor(func: Lazy<System.Func<obj,obj[],obj>>, name: string, parameters) as this = 
        inherit ActionDescriptor()
        let lazyFunc = func
        do
            this.Name <- name
            this.Parameters <- parameters

        override c.Action with get() = lazyFunc.Value 


    [<Export(typeof<ControllerDescriptorBuilder>)>]
    [<PartCreationPolicy(CreationPolicy.Shared)>]
    type CecilBasedControllerDescriptorBuilder = 
        val type2descriptor : ConcurrentDictionary<System.Type, ControllerDescriptor>
        new() = { type2descriptor = new ConcurrentDictionary<System.Type, ControllerDescriptor>() }
        inherit ControllerDescriptorBuilder 

        override x.Build controllerType = 

            let desc = 
                let res, tmp = x.type2descriptor.TryGetValue controllerType

                if res then
                    tmp
                else
                    let name = correctControllerName(controllerType.Name).ToLowerInvariant()
                    let descriptor = ControllerDescriptor(controllerType, name, null)

                    let actions = 
                        controllerType.GetMethods(BindingFlags.Public ||| BindingFlags.Instance)
                        |> Seq.filter (fun m -> m.IsPublic && m.IsSpecialName = false && m.IsStatic = false)
                        |> Seq.toList

                    // side effecty (calls Add on collection)
                    let rec buildActionDescriptors i count = 
                        if count = i then
                            ()
                        else
                            let act = actions.[i]
                            let lazyActionFuncInstance = lazyActionFunc (act, controllerType)

                            let rec recbuildParamDescriptor (parameters:ParameterInfo []) index count = 
                                if count = index then
                                    []
                                else 
                                    let param = parameters.[index]
                                    let paramType = param.ParameterType
                                    let paramDesc = ParameterDescriptor(param.Name, paramType)
                                    recbuildParamDescriptor parameters (index + 1) count |> List.append [paramDesc] 
                    
                            let parameters = act.GetParameters()
                            let paramDefs = recbuildParamDescriptor parameters 0 parameters.Length |> Seq.toArray
                            let actionName = act.Name
                            let actionDesc = CecilBasedActionDescriptor(lazyActionFuncInstance, actionName, paramDefs)
                            descriptor.Actions.Add actionDesc
                            buildActionDescriptors(i + 1) count 
                            ()
            
                    buildActionDescriptors 0 actions.Length

                    ignore(x.type2descriptor.TryAdd (controllerType, descriptor))

                    descriptor

            desc