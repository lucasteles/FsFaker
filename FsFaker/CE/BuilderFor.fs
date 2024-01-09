namespace FsFaker

open System
open System.Collections.Generic
open System.Linq.Expressions
open Bogus
open FsFaker
open FsFaker.Types
open FsFaker.Types.Internal

type BuilderFor<'t when 't: not struct>(baseFaker: Faker<'t> option) =

    member val rootFaker =
        match baseFaker with
        | Some f -> f.Clone()
        | None -> FsFakerConfig.newFaker().StrictMode(true)

    new() = BuilderFor None

    member this.Zero() = LazyFaker.New(this.rootFaker)
    member this.Yield(()) = this.Zero().UpdateTime()
    member this.Yield(BuildInto(faker): BuildInto<'t>) = faker.UpdateTime()
    member inline this.Yield(v: MapFaker< ^a >) = v

    member this.Run(v: LazyFaker<'t>) = v.GetFaker() |> Some |> BuilderFor<'t>
    member this.Run(GetFaker faker) = faker.GetFaker()
    member this.Run(GenerateOne faker) = faker.Generate()
    member this.Run(GenerateTwo faker) = faker.Generate(), faker.Generate()

    member this.Run(GenerateThree faker) =
        faker.Generate(), faker.Generate(), faker.Generate()

    member inline this.Run(MapFaker value) = value: ^a

    member this.Run(GenerateList(faker, countOrMin, max)) =
        match max with
        | None -> faker.Generate countOrMin
        | Some max -> faker.GetFaker().GenerateBetween(countOrMin, max) |> List.ofSeq

    member this.Run(GenerateLazy(faker, count)) : _ seq =
        let f = faker.GetFaker()

        match count with
        | Some n -> f.GenerateLazy(n)
        | None -> f.GenerateForever()

    member _.Combine(a: LazyFaker<'t>, b: LazyFaker<'t>) = a ++ b
    member this.Combine(BuildInto a, b: LazyFaker<'t>) = this.Combine(a, b)

    member _.Delay(f) = f ()
    member this.For(state: LazyFaker<'t>, f: unit -> LazyFaker<'t>) = this.Combine(state, f ())
    member this.For(state: BuildInto<'t>, f: unit -> LazyFaker<'t>) = this.Combine(state, f ())
    member this.For(state: LazyFaker<'t>, f: unit -> BuildInto<'t>) = this.Combine(f (), state)

    [<CustomOperation("getFaker")>]
    member this.GetFaker(faker: LazyFaker<'t>) = GetFaker faker

    [<CustomOperation("generate")>]
    member this.Generate(faker: LazyFaker<'t>) = GenerateOne faker

    [<CustomOperation("one")>]
    member this.One(faker: LazyFaker<'t>) = GenerateOne faker

    [<CustomOperation("two")>]
    member this.Two(faker: LazyFaker<'t>) = GenerateTwo faker


    [<CustomOperation("three")>]
    member this.Three(faker: LazyFaker<'t>) = GenerateThree faker

    [<CustomOperation("generate")>]
    member this.Generate(faker: LazyFaker<'t>, countOrMin: int, ?max: int) = GenerateList(faker, countOrMin, max)

    [<CustomOperation("list")>]
    member this.List(faker: LazyFaker<'t>, ?countOrMin: int, ?max: int) =
        match countOrMin, max with
        | Some count, None
        | None, Some count -> GenerateList(faker, count, None)
        | Some min, Some max -> GenerateList(faker, min, Some max)
        | None, None -> GenerateList(faker, 1, None)

    [<CustomOperation("toSeq")>]
    member this.GenerateLazy(faker: LazyFaker<'t>, ?count: int) = GenerateLazy(faker, count)

    [<CustomOperation("strict")>]
    member _.Strict(faker: LazyFaker<'t>, ?strictEnabled) =
        faker +> (fun f -> f.StrictMode(strictEnabled |> Option.defaultValue true))

    [<CustomOperation("freeze")>]
    member _.Freeze(state: LazyFaker<'t>) =
        state
        +> (fun faker ->
            let value = faker.Generate()
            let fields = Helpers.getTupleFromRecord value

            fields
            |> Seq.fold (fun f (field, value) -> f.RuleFor(field, (fun _ -> value))) faker

        )

    [<CustomOperation("locale")>]
    member _.SetLocale(faker: LazyFaker<'t>, locale: string) =
        faker
        +> (fun f ->
            f.Locale <- locale
            (f :> IFakerTInternal).FakerHub.Locale <- locale
            f)

    [<CustomOperation("rule")>]
    member _.RuleFor(faker: LazyFaker<'t>, rule: Expression<Func<'t, 'p>>, factory: Faker -> 'p) =
        faker +> (fun f -> f.RuleFor(rule, factory))

    [<CustomOperation("rule")>]
    member _.RuleFor(faker: LazyFaker<'t>, rule: Expression<Func<'t, 'p>>, value: 'p) =
        faker +> (fun f -> f.RuleFor(rule, value))

    [<CustomOperation("rule")>]
    member _.RuleFor(faker: LazyFaker<'t>, _: Expression<Func<'t, 'p>>) = faker

    [<CustomOperation("rule")>]
    member _.RuleFor(faker: LazyFaker<'t>, rule: Expression<Func<'t, 'p>>, factory: Faker -> 't -> 'p) =
        faker +> (fun f -> f.RuleFor(rule, factory))

    [<CustomOperation("rule")>]
    member _.RuleFor(faker: LazyFaker<'t>, rule: Expression<Func<'t, 'p>>, builder: BuilderFor<'p>) =
        faker
        +> (fun f -> f.RuleFor(rule, valueFunction = (fun () -> builder { generate })))

    [<CustomOperation("rule")>]
    member _.RuleFor(faker: LazyFaker<'t>, rule: Expression<Func<'t, 'c>>, propBuilder: BuilderFor<'p>, ?count: int) =
        let n = count |> Option.defaultValue 1

        faker
        +> (fun f ->
            f.RuleFor(rule, (fun () -> propBuilder { toSeq n } |> Helpers.changeUnderlyingCollectionType<'c, 'p>)))

    [<CustomOperation("build", AllowIntoPattern = true)>]
    member _.Build(faker: LazyFaker<'t>) = BuildInto faker

    [<CustomOperation("set", MaintainsVariableSpace = true)>]
    member _.Set(faker: LazyFaker<'t>, [<ProjectionParameter>] rule: Expression<Func<'t, 'p>>, factory: Faker -> 'p) =
        faker +> (fun f -> f.RuleFor(rule, factory))

    [<CustomOperation("set", MaintainsVariableSpace = true)>]
    member _.Set(faker: LazyFaker<'t>, [<ProjectionParameter>] rule: Expression<Func<'t, 'p>>, ?value: 'p) =
        faker
        +> (fun f ->
            match value with
            | Some v -> f.RuleFor(rule, v)
            | None -> f)

    [<CustomOperation("set", MaintainsVariableSpace = true)>]
    member _.Set
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, 'p>>,
            factory: Faker -> 't -> 'p
        ) =
        faker +> (fun f -> f.RuleFor(rule, factory))

    [<CustomOperation("set", MaintainsVariableSpace = true)>]
    member _.Set
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, 'p>>,
            propBuilder: BuilderFor<'p>
        ) =
        faker
        +> (fun f -> f.RuleFor(rule, valueFunction = (fun () -> propBuilder { generate })))

    [<CustomOperation("set", MaintainsVariableSpace = true)>]
    member this.Set<'c, 'p when 'p: not struct and 'c :> IEnumerable<'p>>
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, 'c>>,
            propBuilder: BuilderFor<'p>,
            ?count: int
        ) =
        match count with
        | None -> this.RuleFor(faker, rule, propBuilder)
        | Some n -> this.RuleFor(faker, rule, propBuilder, n)

    [<CustomOperation("fac", MaintainsVariableSpace = true)>]
    member _.Factory
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, 'p>>,
            valueFunction: unit -> 'p
        ) =
        faker +> (fun f -> f.RuleFor(rule, valueFunction = valueFunction))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand(faker: LazyFaker<'t>, [<ProjectionParameter>] rule: Expression<Func<'t, Guid>>) =
        faker +> (fun f -> f.RuleFor(rule, (fun (f: Faker) -> f.Random.Guid())))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand(faker: LazyFaker<'t>, [<ProjectionParameter>] rule: Expression<Func<'t, int>>, ?min: int, ?max: int) =
        let f (f: Faker) =
            match min, max with
            | None, None -> f.Random.Int()
            | Some min, None -> f.Random.Int(min = min)
            | None, Some max -> f.Random.Int(max = max)
            | Some min, Some max -> f.Random.Int(min = min, max = max)

        faker +> (fun e -> e.RuleFor(rule, f))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, Int64>>,
            ?min: Int64,
            ?max: Int64
        ) =
        let f (f: Faker) =
            match min, max with
            | None, None -> f.Random.Long()
            | Some min, None -> f.Random.Long(min = min)
            | None, Some max -> f.Random.Long(max = max)
            | Some min, Some max -> f.Random.Long(min = min, max = max)

        faker +> (fun x -> x.RuleFor(rule, f))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, Double>>,
            ?min: float,
            ?max: float
        ) =
        let f (f: Faker) =
            match min, max with
            | None, None -> f.Random.Double()
            | Some min, None -> f.Random.Double(min = min)
            | None, Some max -> f.Random.Double(max = max)
            | Some min, Some max -> f.Random.Double(min = min, max = max)

        faker +> (fun x -> x.RuleFor(rule, f))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, float32>>,
            ?min: float32,
            ?max: float32
        ) =
        let f (f: Faker) =
            match min, max with
            | None, None -> f.Random.Float()
            | Some min, None -> f.Random.Float(min = min)
            | None, Some max -> f.Random.Float(max = max)
            | Some min, Some max -> f.Random.Float(min = min, max = max)

        faker +> (fun x -> x.RuleFor(rule, f))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, decimal>>,
            ?min: decimal,
            ?max: decimal
        ) =
        let f (f: Faker) =
            match min, max with
            | None, None -> f.Random.Decimal()
            | Some min, None -> f.Random.Decimal(min = min)
            | None, Some max -> f.Random.Decimal(max = max)
            | Some min, Some max -> f.Random.Decimal(min = min, max = max)

        faker +> (fun x -> x.RuleFor(rule, f))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand(faker: LazyFaker<'t>, [<ProjectionParameter>] rule: Expression<Func<'t, 'enum>>) =

        faker +> (fun x -> x.RuleFor(rule, (fun (f: Faker) -> f.Random.Enum())))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand(faker: LazyFaker<'t>, [<ProjectionParameter>] rule: Expression<Func<'t, string>>, ?length: int) =
        faker
        +> (fun f -> f.RuleFor(rule, (fun (f: Faker) -> f.Random.String(length |> Option.toNullable))))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand(faker: LazyFaker<'t>, [<ProjectionParameter>] rule: Expression<Func<'t, 'v>>, items: 'v list) =
        faker
        +> (fun f -> f.RuleFor(rule, (fun (f: Faker) -> f.Random.ListItem(items |> Seq.toArray))))

    [<CustomOperation("rand", MaintainsVariableSpace = true)>]
    member _.Rand
        (
            faker: LazyFaker<'t>,
            [<ProjectionParameter>] rule: Expression<Func<'t, 'v>>,
            [<ParamArray>] items: 'v[]
        ) =
        faker
        +> (fun f -> f.RuleFor(rule, (fun (f: Faker) -> f.Random.ListItem(items))))

    member this.Faker = this { getFaker }
    member this.FreezeValues() = this { freeze }
    member this.Generate() = this { generate }
    member this.Generate(n: int) = this { generate n }
    member this.Generate(min: int, max: int) = this { generate min max }
