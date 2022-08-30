module CSharpConfigurationTests

open Scrutiny
open Scrutiny.CSharp
open Expecto
open Swensen.Unquote
open System

[<Tests>]
let csharpEntryTests =
    testList
        "C# Configuration Tests"
        [ Tests.test "Should convert C# config object to scrutiny config" {
              let expected =
                  { ScrutinyConfig.Seed = 42
                    MapOnly = false
                    ComprehensiveActions = true
                    ComprehensiveStates = false
                    ScrutinyResultFilePath = "mypath"
                    Logger = ignore }

              let actual =
                  Configuration(
                      Seed = 42,
                      MapOnly = false,
                      ComprehensiveActions = true,
                      ComprehensiveStates = false,
                      ScrutinyResultFilePath = "mypath",
                      Logger = Action<string>(ignore)
                  )
                      .ToScrutinyConfig()

              test <@ actual.Seed = expected.Seed @>
              test <@ actual.MapOnly = expected.MapOnly @>
              test <@ actual.ComprehensiveStates = expected.ComprehensiveStates @>
              test <@ actual.ComprehensiveActions = expected.ComprehensiveActions @>
              test <@ actual.ScrutinyResultFilePath = expected.ScrutinyResultFilePath @>
          } ]
