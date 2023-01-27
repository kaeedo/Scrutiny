module Web.App

open Giraffe
open Giraffe.ViewEngine
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.IO

// ---------------------------------
// Models
// ---------------------------------

type Message = { Header: string; Links: string list }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    let layout (content: XmlNode list) =
        html
            []
            [ head
                  []
                  [ title [] [ encodedText "Crappy test site" ]
                    link
                        [ _rel "stylesheet"
                          _type "text/css"
                          _href "/main.css" ] ]
              body [] content ]

    let getLinks links =
        ul
            []
            (links
             |> List.map (fun (l: string) ->
                 let text = l.Substring(1)
                 li [] [ a [ _href l; _id text ] [ encodedText text ] ]))

    let home (username: string option) (model: Message) =
        [ h1 [ _id "header" ] [ encodedText model.Header ]
          (match username with
           | None -> div [] []
           | Some u ->
               div
                   []
                   [ div [ _id "welcomeText" ] [ encodedText <| sprintf "Welcome %s" u ]
                     div
                         []
                         [ a
                               [ _href "javascript:void(0);"
                                 _id "logout" ]
                               [ encodedText "Logout" ] ] ])
          getLinks model.Links
          script [ _src "/home.js" ] [] ]
        |> layout

    let comment (username: string option) (model: Message) =
        [ h1 [ _id "header" ] [ encodedText model.Header ]
          div
              [ _id "modal"; _class "modal" ]
              [ div
                    [ _class "modal-dialog" ]
                    [ div
                          [ _class "modal-content" ]
                          [ div [ _class "modal-header" ] [ encodedText "Header" ]
                            div
                                [ _class "modal-body" ]
                                [ label [ _for "comment" ] [ encodedText "Comment:" ]
                                  div
                                      []
                                      [ textarea
                                            [ _rows "10"
                                              _cols "30"
                                              _id "comment" ]
                                            [] ] ]
                            div
                                [ _class "modal-footer" ]
                                [ button [ _id "modalFooterSave" ] [ encodedText "Save" ]
                                  button [ _id "modalFooterClose" ] [ encodedText "Close" ] ] ] ] ]
          p
              []
              [ (match username with
                 | Some _ ->
                     div
                         []
                         [ a
                               [ _href "javascript:void(0);"
                                 _id "openModal" ]
                               [ encodedText "Click here" ]
                           span [] [ encodedText " to open a modal to add a comment" ] ]
                 | None -> span [] [ encodedText "Sign in to comment" ]) ]
          getLinks model.Links
          hr []
          div [] [ ul [ _id "commentsUl" ] [] ]
          script [ _src "/rmodal.js" ] []
          script [ _src "/comment.js" ] [] ]
        |> layout

    let signIn (model: Message) =
        [ h1 [ _id "header" ] [ encodedText model.Header ]
          div
              [ _id "ErrorMessage"
                _style "display:none;" ]
              [ encodedText "Form invalid" ]
          form
              [ _action "javascript:void(0)"
                _id "signInForm" ]
              [ div
                    []
                    [ label [ _for "username" ] [ encodedText "Username:" ]
                      input [ _id "username"; _type "text" ] ]
                div
                    []
                    [ label [ _for "number" ] [ encodedText "Enter a number for fun" ]
                      input [ _id "number"; _type "text" ] ]
                div [] [ button [ _type "submit" ] [ encodedText "Sign In" ] ] ]
          getLinks model.Links
          script [ _src "/signIn.js" ] [] ]
        |> layout

// ---------------------------------
// Web app
// ---------------------------------

let homeHandler: HttpHandler =
    fun next ctx ->
        let username = ctx.GetCookieValue "username"

        let links =
            match username with
            | None -> [ "/comment"; "/signin" ]
            | Some _ -> [ "/comment" ]

        htmlView
            (Views.home
                username
                { Message.Header = "Home"
                  Links = links })
            next
            ctx

let signInHandler: HttpHandler =
    fun next ctx ->
        match ctx.GetCookieValue "username" with
        | None ->
            htmlView
                (Views.signIn
                    { Message.Header = "Sign In"
                      Links = [ "/home" ] })
                next
                ctx
        | Some _ -> redirectTo false "/home" next ctx

let commentHandler: HttpHandler =
    fun next ctx ->
        let username = ctx.GetCookieValue "username"

        let links =
            match username with
            | None -> [ "/home"; "/signin" ]
            | Some _ -> [ "/home" ]

        htmlView
            (Views.comment
                username
                { Message.Header = "Comments"
                  Links = links })
            next
            ctx

let webApp =
    choose
        [ GET
          >=> choose
                  [ route "/" >=> redirectTo true "/home"
                    route "/home" >=> homeHandler
                    route "/comment" >=> commentHandler
                    route "/signin" >=> signInHandler ]
          setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")

    clearResponse
    >=> setStatusCode 500
    >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder: CorsPolicyBuilder) =
    builder
        .WithOrigins("http://localhost:8080")
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore

let configureApp (app: IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.EnvironmentName = "development" with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =

    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder
        .AddFilter(fun l -> l.Equals LogLevel.Error)
        .AddConsole()
        .AddDebug()
    |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()

    0
