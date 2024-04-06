namespace FsFaker

open System
open Bogus
open FsFaker

type DataBuilder<'t> =
    { Constructor: Faker -> 't
      Faker: Faker
      Mapper: 't -> 't }

    member this.Generate() =
        this.Faker |> this.Constructor |> this.Mapper

    member this.Generate(count: int) =
        [ for _ = 0 to count do
              this.Generate() ]

    member this.GenerateInfinite() =
        Seq.initInfinite (fun _ -> this.Generate())

[<RequireQualifiedAccess>]
module DataBuilder =

    let create<'t> ctor : DataBuilder<'t> =
        { Constructor = ctor
          Faker = FsFakerConfig.newDataFaker ()
          Mapper = id }

    let generate (b: DataBuilder<_>) = b.Generate()
    let infinite (b: DataBuilder<_>) : 't seq = b.GenerateInfinite()
    let one (b: DataBuilder<_>) = generate b
    let two (b: DataBuilder<_>) = one b, one b
    let three (b: DataBuilder<_>) = one b, one b, one b

    let between initValue endValue (f: DataBuilder<_>) =
        let count = f.Faker.Random.Int(min = initValue, max = endValue - 1)
        f.Generate count

    let list (count: int) (b: DataBuilder<_>) = b.Generate count
    let seq (count: int) (b: DataBuilder<_>) = b.GenerateInfinite() |> Seq.take count
    let array (count: int) (b: DataBuilder<_>) = b |> list count |> List.toArray

    let setLocale (locale: string) (b: DataBuilder<_>) = b.Faker.Locale <- locale
    let setDateTimeReference (date: DateTime) (b: DataBuilder<_>) = b.Faker.DateTimeReference <- date

    let seed (localSeed: int) (b: DataBuilder<_>) = b.Faker.Random <- Randomizer localSeed

    let freeze (b: DataBuilder<_>) =
        let currentValue = b.Generate()

        { Constructor = fun _ -> currentValue
          Faker = b.Faker
          Mapper = id }

    let update fn (b: DataBuilder<_>) =
        { b with
            Mapper = b.Mapper >> fn b.Faker }

    let map (fn: 'a -> 'b) (b: DataBuilder<'a>) =
        { Constructor = fun _ -> generate b |> fn
          Faker = b.Faker
          Mapper = id }

    let tap (fn: Faker -> 'a -> unit) (b: DataBuilder<'a>) =
        b
        |> update (fun faker model ->
            fn faker model
            model)

    let zip (a: DataBuilder<'a>) (b: DataBuilder<'b>) =
        { Constructor = fun _ -> generate a, generate b
          Faker = b.Faker
          Mapper = id }

    let zip3 (a: DataBuilder<'a>) (b: DataBuilder<'b>) (c: DataBuilder<'c>) =
        { Constructor = fun _ -> generate a, generate b, generate c
          Faker = b.Faker
          Mapper = id }

    let fromValue (value: 'a) : DataBuilder<'a> = create (fun _ -> value)

    let bind (f: 'a -> DataBuilder<'b>) (b: DataBuilder<'a>) : DataBuilder<'b> =
        { Constructor = fun _ -> b |> generate |> f |> generate
          Faker = b.Faker
          Mapper = id }
