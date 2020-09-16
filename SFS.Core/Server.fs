namespace SFS.Core

open System.Data
open System.IO
open SFS.Core.Routing
open SFS.Core.WebSockets

module Server =
    open System
    open System.Text
    open SFS.Core.Logging
    open SFS.Core.Utilities
    open SFS.Core.Http
    open System.Net.Sockets
    open SFS.Core.ConnectionHandler

    let logger = Logger()

    // TODO Set these in config.
    let ip = "127.0.0.1"
    let port = 42000

    /// The tcp listener.
    let listener = TcpListener.Create(port)

    let routes =
        seq {
            { contentType = ContentTypes.Html
              routePaths = seq { "/" }
              contentPath = "/home/max/Projects/SemiFunctionalServer/ExampleWebSite/index.html"
              routeType = RouteType.Static }
            { contentType = ContentTypes.Css
              routePaths = seq { "/css/style.css" }
              contentPath = "/home/max/Projects/SemiFunctionalServer/ExampleWebSite/css/style.css"
              routeType = RouteType.Static }
            { contentType = ContentTypes.JavaScript
              routePaths = seq { "/js/index.js" }
              contentPath = "/home/max/Projects/SemiFunctionalServer/ExampleWebSite/js/index.js"
              routeType = RouteType.Static }
            { contentType = ContentTypes.Jpg
              routePaths = seq { "/img/patrik-kernstock-8yN3T4XDJ70-unsplash.jpg" }
              contentPath =
                  "/home/max/Projects/SemiFunctionalServer/ExampleWebSite/img/patrik-kernstock-8yN3T4XDJ70-unsplash.jpg"
              routeType = RouteType.Static }
        }

    let wsc =
        seq { "echo", WebSocketChannel "echo" }
        |> Map.ofSeq


    let notFound =

        let paths =
            seq {
                "NotFound"
                "404"
            }

        let content =
            File.ReadAllBytes("/home/max/Projects/SemiFunctionalServer/ExampleWebSite/404.html")
            //            |> Binary
            |> Some

        { paths = paths
          contentType = ContentTypes.Html
          routeType = RouteType.Static
          content = content }

    let routeMap = createRoutesMap routes

    let i = 0
    /// The listening loop.
    let rec listen (context: Context) =
        // Await a connection.
        // This is blocking.
        let connection = listener.AcceptTcpClient()

        let handler = handleConnection context


        //        let routes = match context.routes.["/"].content with Some d -> d | None -> [||]

        logger.Post
            { from = "Listener"
              message = "Connection received."
              time = DateTime.Now
              ``type`` = Debug }

        // Send to a background thread to handle.
        handler connection |> Async.Start |> ignore

        logger.Post
            { from = "Listener"
              message = "Back to main."
              time = DateTime.Now
              ``type`` = Debug }

        // Repeat the listen loop.
        listen (context)

    /// Start the listening loop and accept incoming requests.
    let start =

        let context =
            { logger = logger
              routes = routeMap
              wsChannels = wsc
              errorRoutes =
                  { notFound = notFound
                    internalError = notFound
                    unauthorized = notFound
                    badRequest = notFound } }

        // "Inject" the context.
        // The handler can then be passed to the listen loop.
        let handler = handleConnection context

        logger.Post
            { from = "Main"
              message = "Starting listener."
              time = DateTime.Now
              ``type`` = Information }
        // Start a tcp listener on specified ip and port.
        listener.Start()
        logger.Post
            { from = "Main"
              message = "Listener started."
              time = DateTime.Now
              ``type`` = Success }

        // Pass the listener to `listen` function.
        // This will recursively handle requests.
        listen (context) |> ignore

// TODO Add shutdown.
