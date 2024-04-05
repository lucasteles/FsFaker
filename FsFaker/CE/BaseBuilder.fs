namespace FsFaker.Builders

open System
open Bogus
open FsFaker.Builders.Types

[<AbstractClass>]
type BaseBuilder<'t, 'builder when 't: not struct and 'builder :> BuilderFor<'t>>(baseFaker: Faker<'t> option) =
    inherit BuilderFor<'t>(baseFaker)

    member this.Run(faker: LazyFaker<'t>) : 'builder =
        if typeof<'builder> <> this.GetType() then
            invalidOp "Generic builder type should be the same type as builder"

        Activator.CreateInstance(this.GetType(), [| faker.GetFaker() |> Some |> box |]) :?> 'builder
