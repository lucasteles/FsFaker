[![CI](https://github.com/lucasteles/FsFaker/actions/workflows/ci.yml/badge.svg)](https://github.com/lucasteles/FsFaker/actions/workflows/ci.yml)
[![Nuget](https://img.shields.io/nuget/v/FsFaker.svg?style=flat)](https://www.nuget.org/packages/FsFaker)

# FsFaker

Easily define data builders with [Bogus](https://github.com/bchavez/Bogus)

## Getting started

[NuGet package](https://www.nuget.org/packages/FsFaker) available:

```ps
$ dotnet add package FsFaker
```

## Simple Builders

```fsharp
type AddressType = Principal | Secondary
type Address =
    { Street: string
      City: string
      Type: AddressType }

// defining builders
let addressBuilder =
    Builder.create (fun f ->
        { City = f.Address.City()
          Street = f.Address.StreetName()
          Type = f.Random.Union<AddressType>() })

let addresses = addressBuilder.Generate(10) 

// extending / updating builders
let addressesChanged =
    addressBuilder
    |> Builder.update (fun f model ->
        { model with City = $"TEST {f.Random.Int()}" })
    |> Builder.one
      
```

## Defining Builders

```fsharp
open System
open FsFaker

type Status =
    | Enabled
    | Disabled

type MaritalStatus =
    | Single = 1
    | Married = 2
    | Widowed = 3

type AddressType =
    | Principal
    | Secondary

[<CLIMutable>]
type Address =
    { Street: string
      City: string
      Type: AddressType }

[<CLIMutable>]
type Person =
    { Id: Guid
      Name: string
      Email: string
      Age: int
      Status: string
      BirthDate: DateTime
      MaritalStatusEnum: MaritalStatus
      Address: Address
      OtherAddresses: Address list }

// default locale 
FsFakerConfig.setLocale "pt_BR"

let address' =
    BuilderFor<Address>() {
        locale "en"
        build into address
        set address.City (fun f -> f.Address.City())
        set address.Street (fun f -> f.Address.StreetName())
        set address.Type (fun f -> f.Random.Union<AddressType>())
    }

let person' =
    BuilderFor<Person>() {
        build into person
        set person.Name (fun f -> f.Person.FirstName)
        set person.Email (fun f -> f.Person.Email)
        set person.BirthDate (fun f -> f.Date.Past())
        set person.Age (fun f p -> DateTime.Today.Subtract(p.BirthDate).TotalDays |> int)
        set person.Address address'
        set person.OtherAddresses address' 2

        rand person.Id
        rand person.MaritalStatusEnum
        rand person.Status "active" "disabled"
    }

let person1 = person'.Generate()

let person2 =
    person' {
        rule (fun p -> p.Name) "Lucas"
        rule (fun p -> p.BirthDate) (DateTime.Today.AddYears -32)
        one
    }

let persons =
    person' {
        rule (fun p -> p.Status) "other"
        list 5
    }

let infinitePersons = person' { lazy_seq }
```

## Extending Builders

You can extend builders with your own operations:

```fsharp
open FsFaker
open FsFaker.Types

type CustomPersonBuilder(?faker) =
    inherit BaseBuilder<Person, CustomPersonBuilder>(faker)

    [<CustomOperation("withName")>]
    member _.WithName(faker: LazyFaker<Person>, name: string) =
        faker
            .RuleFor((fun x -> x.Name), name)
            .RuleFor((fun x -> x.Email), (fun f -> $"{name}@{f.Internet.DomainName()}.com"))

    [<CustomOperation("canDrive")>]
    member _.CanDrive(faker: LazyFaker<Person>) =
        faker
            .RuleFor((fun x -> x.Age), 18)
            .RuleFor((fun x -> x.BirthDate), (fun _ p -> DateTime.Today.AddYears(-p.Age)))

let customResult =
    CustomPersonBuilder() {
    
        // your operations
        withName "Artorias"
        canDrive

        // you can mix with property rules
        rule (fun p -> p.Address) address'
        rule (fun p -> p.OtherAddresses) []

        // or even with build syntax
        build into person
        rand person.Id
        set person.Status "active"
        set person.MaritalStatusEnum MaritalStatus.Single

        one
    }
```
