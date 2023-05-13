[<AutoOpen>]
module FsFaker.FakerExtensions

open System
open Bogus


type Randomizer with

    member _.Union<'t>() =
        let cases = Reflection.FSharpType.GetUnionCases(typeof<'t>)

        let index = Random.Shared.Next(cases.Length)
        let case = cases[index]
        Reflection.FSharpValue.MakeUnion(case, [||]) :?> 't


[<AutoOpen>]
type FakerUtils =
    static member generate(builder: BuilderFor<'t>) = builder.Generate()
    static member generate(builder: BaseBuilder<'a, 'b>) = builder.Generate()
    static member generate(builder: #Faker<'t>) = builder.Generate()
