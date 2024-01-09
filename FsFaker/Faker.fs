namespace FsFaker

open Bogus

module Faker =
    let generate (f: #Faker<_>) = f.Generate()
    let one (f: #Faker<_>) = generate f
    let two (f: #Faker<_>) = one f, one f
    let three (f: #Faker<_>) = one f, one f, one f
    let between initValue endValue (f: #Faker<_>) = f.GenerateBetween(initValue, endValue)
    let many (count: int) (f: #Faker<_>) = f.Generate(count) |> List.ofSeq
    let seq (count: int) (f: #Faker<'t>) : 't seq = f.GenerateLazy(count)
    let forever (f: #Faker<'t>) : 't seq = f.GenerateForever()
    let clone (f: #Faker<_>) = f.Clone()

    module Random =
        let union<'t> (rand: Randomizer) : 't =
            let cases = Reflection.FSharpType.GetUnionCases(typeof<'t>)
            let index = rand.Int(min = 0, max = cases.Length - 1)
            let case = cases[index]
            Reflection.FSharpValue.MakeUnion(case, [||]) :?> 't

    let randomUnion<'t> (f: Faker) = Random.union<'t> f.Random
