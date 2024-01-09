namespace FsFaker

open Bogus

module FsFakerConfig =
    let mutable internal globalLocale = "en"
    let setLocale locale = globalLocale <- locale
    let internal newFaker () = Faker<'t> globalLocale
    let internal newDataFaker () = Faker globalLocale
