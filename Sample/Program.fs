open System
open FsFaker
open FsFaker.Types

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

FsFakerConfig.setLocale "pt_BR"

let address' =
    BuilderFor<Address>() {
        build into address
        set address.City _.Address.City()
        set address.Street _.Address.StreetName()
        set address.Type Faker.randomUnion<AddressType>
    }

let person' =
    BuilderFor<Person>() {
        locale "en"

        build into person
        set person.Name _.Person.FirstName
        set person.Email _.Person.Email
        set person.BirthDate _.Date.Past()
        set person.Age (fun _ p -> DateTime.Today.Subtract(p.BirthDate).TotalDays |> int)
        set person.Address address'
        set person.OtherAddresses address' 2

        rand person.Id
        rand person.MaritalStatusEnum
        rand person.Status "active" "disabled"
    }

let person1 = person'.Generate()
let person1' = Faker.generate person'.Faker

let person2 =
    person' {
        rule (_.Name) "Lucas"
        rule (_.BirthDate) (DateTime.Today.AddYears -32)
        one
    }

let persons =
    person' {
        rule (_.Status) "other"
        list 5
    }

let infinityPersons = person' { toSeq }

let personTuple1, personTuple2 =
    person' {
        rule (_.Status) "cursed"
        two
    }

printfn "%A" person1
printfn "%A" person2
printfn "%A" persons
printfn "%A,%A" personTuple1 personTuple2
printfn "From faker: %A" (person'.Faker |> Faker.generate)
printfn "%A" (infinityPersons |> Seq.take 3 |> Seq.toList)


type CustomPersonBuilder(?faker) =
    inherit BaseBuilder<Person, CustomPersonBuilder>(faker)

    [<CustomOperation("withName")>]
    member _.WithName(faker: LazyFaker<Person>, name: string) =
        faker
            .RuleFor((fun x -> x.Name), name)
            .RuleFor((fun x -> x.Email), (fun f -> $"{name}@{f.Internet.DomainName()}"))

    [<CustomOperation("canDrive")>]
    member _.CanDrive(faker: LazyFaker<Person>) =
        faker
            .RuleFor((fun x -> x.Age), 18)
            .RuleFor((fun x -> x.BirthDate), (fun _ p -> DateTime.Today.AddYears(-p.Age)))

let customBuilder =
    CustomPersonBuilder() {
        withName "Artorias"
        canDrive

        rule (_.Address) address'
        rule (_.OtherAddresses) []

        build into person
        rand person.Id
        set person.Status "active"
        set person.MaritalStatusEnum MaritalStatus.Single
    }

printfn "custom: %A" (customBuilder { one })
printfn "%A" (customBuilder.Generate())
