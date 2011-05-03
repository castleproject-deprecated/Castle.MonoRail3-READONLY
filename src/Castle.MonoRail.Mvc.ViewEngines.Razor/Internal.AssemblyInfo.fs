﻿
module AssemblyLevelDeclarations

open System.Reflection
open System.Security
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Castle.MonoRail.Mvc.ViewEngines.Razor

[<assembly: AssemblyVersion("0.0.1.0")>]
[<assembly: AssemblyFileVersion("0.0.1.0")>]
// [<assembly: InternalsVisibleToAttribute("Castle.MonoRail.Tests") >]

[<assembly: AllowPartiallyTrustedCallers()>]
[<assembly: SecurityTransparent()>]

[<assembly: System.Web.PreApplicationStartMethod(typeof<RazorViewEngine>, "Initialize")>]


do()