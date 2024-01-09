[<AutoOpen>]
module FsFaker.FakerExtensions

open System.Runtime.CompilerServices
open Bogus

[<Extension>]
type RandomizerExtensions() =

    [<Extension>]
    static member Union<'t>(rand: Randomizer) : 't = Faker.Random.union<'t> rand
