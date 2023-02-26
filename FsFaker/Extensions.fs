namespace FsFaker

open System
open Bogus

[<AutoOpen>]
module FakerExtensions =

    type Randomizer with

        member _.Union<'t>() =
            let cases = Reflection.FSharpType.GetUnionCases(typeof<'t>)

            let index = Random.Shared.Next(cases.Length)
            let case = cases[index]
            Reflection.FSharpValue.MakeUnion(case, [||]) :?> 't


module FsFakerConfig =
    let mutable internal globalLocale = "en"
    let setLocale locale = globalLocale <- locale
    let internal newFaker () = Faker<'t> globalLocale
