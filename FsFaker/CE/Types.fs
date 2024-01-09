namespace FsFaker.CE.Types

open System
open System.Diagnostics
open System.Linq.Expressions
open Bogus

type MapFaker<'t> = MapFaker of 't

type LazyFaker<'t> when 't: not struct =
    private
        { Map: Faker<'t> -> Faker<'t>
          RootFaker: Faker<'t>
          Timestamp: int64 }

    static member internal New(rootFaker: Faker<'t>) : LazyFaker<'t> =
        { Map = id
          RootFaker = rootFaker
          Timestamp = 0 }

    member internal this.GetFaker() = this.RootFaker.Clone() |> this.Map

    member internal this.UpdateTime() =
        { this with
            Timestamp = Stopwatch.GetTimestamp() }

    member internal this.Append(fn) = { this with Map = this.Map >> fn }

    member internal this.Combine(other: LazyFaker<'t>) =
        let first, last =
            if this.Timestamp <= other.Timestamp then
                this, other
            else
                other, this

        { RootFaker = first.RootFaker
          Timestamp = last.Timestamp
          Map = first.Map >> last.Map }

    member this.RuleFor(rule: Expression<Func<'t, 'p>>, value: 'p) =
        this.Append(fun f -> f.RuleFor(rule, value))

    member this.RuleFor(rule: Expression<Func<'t, 'p>>, factory: Faker -> 'p) =
        this.Append(fun f -> f.RuleFor(rule, factory))

    member this.RuleFor(rule: Expression<Func<'t, 'p>>, factory: Faker -> 't -> 'p) =
        this.Append(fun f -> f.RuleFor(rule, factory))

    member this.WithRules(config: Faker<'t> -> Faker<'t>) = this.Append(config)

    member this.Generate() = this.GetFaker().Generate()

    member this.Generate(n: int) =
        this.GetFaker().Generate(n) |> List.ofSeq

namespace FsFaker.CE.Types.Internal

open FsFaker.CE.Types
type GetFaker<'t> when 't: not struct = GetFaker of LazyFaker<'t>
type BuildInto<'t> when 't: not struct = BuildInto of LazyFaker<'t>
type GenerateOne<'t> when 't: not struct = GenerateOne of LazyFaker<'t>
type GenerateTwo<'t> when 't: not struct = GenerateTwo of LazyFaker<'t>
type GenerateThree<'t> when 't: not struct = GenerateThree of LazyFaker<'t>
type GenerateList<'t> when 't: not struct = GenerateList of LazyFaker<'t> * int * int option
type GenerateLazy<'t> when 't: not struct = GenerateLazy of LazyFaker<'t> * int option

[<AutoOpen>]
module internal InternalOperators =
    let (+>) (a: LazyFaker<'t>) b = a.Append b
    let (++) (a: LazyFaker<'t>) b = a.Combine b
