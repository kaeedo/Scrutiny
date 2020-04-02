# Scrutiny

## Description
Describe your UI as a state machine, and then use Scrutiny to simulate a "User" that randomly clicks around on your site. 
Scrutiny will attempt to create a Directed Adjacency Graph of your states, and then randomly choose an unvisited state to navigate to. 
It will repeat this process untill all states have been visited.
During each state, Scrutiny will attempt to run any defined actions within that state.
Once all states have been visited, if an exit action has been defined it will then navigate there and quit.
Scrutiny will then also generate an HTML file which visualizes the State Machine as a graph.

Scrutiny was designed to run UI tests, but using e.g. CanopyUI or Selenium is only an implementation detail. In theory, any state machine can be tested with Scrutiny.

## Usage
Check the [UsageExample](/tree/master/src/UsageExample) for a sample test implemented with [CanopyUI](https://github.com/lefthandedgoat/canopy).

### 
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
      ReportPath = Directory.GetCurrentDirectory() }

`Seed` is printed during each test to be able to recreate a specific test run.
`MapOnly` won't run the test at all, but only generate the HTML Graph report.
`ComprehensiveActions` will run ALL defined actions anytime it enters a state with actions defined. If false, it will run a random subset of actions.
`ComprehensiveStates` will visit ALL states in the state machine. If this is false, then it will visit at least half of all states before randomly quitting.

To actually run the test, call the `scrutinize` function with your entry state, config, and global state object

`scrutinize config (new GlobalState()) home` or `scrutinizeWithDefaultConfig (new GlobalState()) home`

## Development
Run:
* `dotnet tool restore`
* `dotnet tool paket install`

To run the UsageExample, you must start the web project.

## TODO for Alpha release
- [ ] Finish HTML report
- [ ] Create and publish NuGet package

## TODO for Beta release
- [ ] Create nice interface for usage from C#
- [ ] Use Fable to create a javascript release and npm package for usage from Node.js
- [ ] Write unit tests 