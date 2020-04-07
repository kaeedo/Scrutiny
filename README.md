# Scrutiny

[![Nuget](https://img.shields.io/nuget/vpre/scrutiny?color=blue&style=for-the-badge)](https://www.nuget.org/packages/Scrutiny/)

## Description
Describe your UI as a state machine, and then use Scrutiny to simulate a "User" that randomly clicks around on your site. 
Scrutiny will attempt to create a Directed Adjacency Graph of your states, and then randomly choose an unvisited state to navigate to. 
It will repeat this process untill all states have been visited.
During each state, Scrutiny will attempt to run any defined actions within that state.
Once all states have been visited, if an exit action has been defined it will then navigate there and quit.
Scrutiny will then also generate an HTML file which visualizes the State Machine as a graph.

Scrutiny was designed to run UI tests, but using e.g. CanopyUI or Selenium is only an implementation detail. In theory, any state machine can be tested with Scrutiny.

## Usage
Check the [UsageExample](src/UsageExample) for a sample test implemented with [CanopyUI](https://github.com/lefthandedgoat/canopy).
A tiny sample site exists in the [Web directory](src/Web). This is the website that the [UsageExample](src/UsageExample) is testing. It features three pages, a home page, comment page, and a sign in page. A user can only leave a comment if they are signed in. 
The [UsageExample](src/UsageExample) showcases a certain approach a developer can take as to how to model their web site as a state machine. In this case, the home and comment page are each listed twice, once as logged out, and once as logged in.
This is only one way to handle this case, and the developer could choose to model it in any other way.

Scrutiny will also draw a diagram representing the system under test as has been modeled by the various `page`s. The [Sample Web site](src/Web) looks like this: 

![SUT sample report](https://raw.githubusercontent.com/kaeedo/Scrutiny/master/images/SampleWebsiteReport.png)


Define one `page` object for each state in your UI. A state can be anything from a page, or an individual modal, or the same page as a different state, but altered, for example a logged in user.
A `page` looks like this:

    let comment = fun (globalState: GlobalState) ->
        page {
            name "Comment"
            onEnter (fun () ->
                printfn "Checking on page comment"
                "#header" == "Comments"
            )
            onExit (fun () ->
                printfn "Exiting comment"
            )

            transition ((fun () -> click "#home") ==> home)
            transition ((fun () -> click "#signin") ==> signIn)
            
            action (fun () -> () /*do something on the page*/)
            action (fun () -> () /*do something else on the page*/)

            exitAction (fun () -> () /*final action to perform before exiting the test*/)
        }

The `name` must be unique. Any number of `transition`s and any number of `action`s can be defined.
The `exitAction` is optional, and multiple `page`s can have an `exitAction`. If multiple are defined, Scrutiny will randomly choose one to perform.

The `GlobalState` in the example is any type defined in your test that you can use to pass data between states, e.g. `Username` or `IsLoggedIn`

Some things can be configured via `ScrutinyConfig`. The default config is:

    { ScrutinyConfig.Seed = Environment.TickCount
      MapOnly = false
      ComprehensiveActions = true
      ComprehensiveStates = true
      ScrutinyResultFilePath = Directory.GetCurrentDirectory() + "/ScrutinyResult.html"}

`Seed` is printed during each test to be able to recreate a specific test run.
`MapOnly` won't run the test at all, but only generate the HTML Graph report.
`ComprehensiveActions` will run ALL defined actions anytime it enters a state with actions defined. If false, it will run a random subset of actions.
`ComprehensiveStates` will visit ALL states in the state machine. If this is false, then it will visit at least half of all states before randomly quitting.
`ScrutinyResultFilePath` is the directory and specified file name that the generated HTML report will be saved in

To actually run the test, call the `scrutinize` function with your entry state, config, and global state object

`scrutinize config (new GlobalState()) home` or `scrutinizeWithDefaultConfig (new GlobalState()) home`

#### Important note for F# users
As the transitions ultimately depict a cyclic graph, it is necessary to declare module or namespace as recursive so that pages defined later can be referenced by pages earlier. Note the usage of the `rec` keyword.
e.g.:

    module rec MyPages =
        let firstPage = fun (globalState: GlobalState) ->
            page {
                name "First Page"
                transition ((fun () -> click "#second") ==> secondPage)
            }

        let secondPage = fun (globalState: GlobalState) ->
            page {
                name "Second Page"
                transition ((fun () -> click "#first") ==> firstPage)
            }

## Development
Run:
* `dotnet tool restore`
* `dotnet tool paket install`

To run the UsageExample, you must start the web project.

## TODO for Alpha release
- [x] Finish initial HTML report
- [x] Create and publish NuGet package
- [x] Documentation

## TODO for Beta release
- [ ] Create nice interface for usage from C#
- [ ] Documentation
- [ ] Write unit tests 
- [ ] Documentation

## TODO General
- [ ] Documentation
- [ ] Setup proper build scripts
- [ ] Use Fable to create a javascript release and npm package for usage from Node.js
- [ ] Documentation