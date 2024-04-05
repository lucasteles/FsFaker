namespace FsFaker

open System
open Bogus

module FsFakerConfig =
    let mutable internal globalLocale = "en"
    let setLocale locale = globalLocale <- locale

    let internal fakerDate () =
        DataSets.Date(globalLocale, LocalSystemClock = (fun () -> DateTime.UtcNow))

    let internal newFaker () =
        let faker = Faker<'t> globalLocale
        (Faker.getInternalFaker faker).Date <- fakerDate ()
        faker

    let internal newDataFaker () =
        Faker(globalLocale, Date = fakerDate ())
