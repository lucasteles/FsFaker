open System
open FsFaker
open FsFaker.Internal.Types

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

// FsFakerConfig.setLocale "pt_BR"

let address' =
    BuilderFor<Address>() {
        locale "pt_BR"
        build into address
        set address.City (fun f -> f.Address.City())
        set address.Street (fun f -> f.Address.StreetName())
        set address.Type (fun f -> f.Random.Union<AddressType>())
    }

let person' =
    BuilderFor<Person>() {
        locale "pt_BR"

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

type CustomPersonBuilder(?faker) =
    inherit BaseBuilder<Person, CustomPersonBuilder>(faker)

    [<CustomOperation("withName")>]
    member _.WithName(faker: LazyFaker<Person>, name: string) = faker.RuleFor((fun x -> x.Name), name)

    [<CustomOperation("canDrive")>]
    member _.CanDrive(faker: LazyFaker<Person>) =
        faker.RuleFor((fun x -> x.BirthDate), DateTime.Today.AddYears(-18))

let customResult =
    CustomPersonBuilder() {
        withName "Andre of Astora"
        canDrive
        one
    }
