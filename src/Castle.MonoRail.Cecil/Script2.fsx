// #r @"C:\dev\castle\Castle.MonoRail3\lib\Mono.Cecil.dll"

#r @"C:\Program Files (x86)\FSharpPowerPack-2.0.0.0\bin\FSharp.PowerPack.Linq.dll"

// open Mono.Cecil
open System.Collections.Generic
open System.Linq
open System.Linq.Expressions
open System.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp
open Microsoft.FSharp.Linq
open QuotationEvaluation

let asmname = @"C:\dev\castle\Castle.MonoRail3\src\TestWebApp\bin\TestWebApp.dll"
let asm = Assembly.LoadFrom asmname
let controllerType = asm.GetType "TestWebApp.Controller.HomeController"
// let assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(asmname)

type Printer() =
    inherit ExpressionVisitor() 
    
    override x.VisitConstant (node:ConstantExpression) =
        printfn "constant: %s" (node.Value.ToString())
        base.VisitConstant node 

    override x.VisitMethodCall (node:MethodCallExpression) =
        printfn "method call: %s" node.Method.Name 
        base.VisitMethodCall node
    
    override x.VisitMemberAssignment (node:MemberAssignment) =
        printfn "assign: %s = %s" node.Member.Name (node.Expression.ToString())
        base.VisitMemberAssignment node

let adderExpr = <@ fun i -> i + 1 @> // .ToLinqExpression()
let linqExp = adderExpr.ToLinqExpression()
let p = Printer()
p.Visit linqExp 


