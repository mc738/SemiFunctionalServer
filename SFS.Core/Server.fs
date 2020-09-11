namespace SFS.Core

open System.Data
open SFS.Core.Routing

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
              contentPath = ""
              routeType = RouteType.Static }
            { contentType = ContentTypes.Css
              routePaths = seq { "/css/style.css" }
              contentPath = ""
              routeType = RouteType.Static }
            { contentType = ContentTypes.Css
              routePaths = seq { "/js/index.js" }
              contentPath = ""
              routeType = RouteType.Static }
        }

    let notFound =

        let paths = Seq.empty

        { paths = paths
          contentType = ContentTypes.Html
          routeType = RouteType.Static
          content = None }

    let routeMap = createRoutesMap routes

    /// The listening loop.
    let rec listen (handler: (TcpClient -> Async<unit>)) =
        // Await a connection.
        // This is blocking.
        let connection = listener.AcceptTcpClient()

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
        listen (handler)

    /// Start the listening loop and accept incoming requests.
    let start =

        let context =
            { logger = logger
              routes = routeMap
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
        listen (handler) |> ignore

// TODO Add shutdown.
