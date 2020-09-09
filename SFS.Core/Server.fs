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



    /// Handle a connection.
    /// This is designed to be run off of the main thread.
    let handleConnection (connection: TcpClient) (handler: (Request -> Response)) =
        async {
            // For now accept a message, convert to string and send back a message.
            logger.Post
                { from = "Connection Handler"
                  message = "In handler."
                  time = DateTime.Now
                  ``type`` = Debug }

            // Get the network stream
            let stream = connection.GetStream()

            // Read the incoming request into the buffer.
            let! buffer = Streams.readToBuffer stream 255 // |> Async.RunSynchronously

            let request = Http.deserializeRequest buffer

            // TODO Handle 500.

            // Pass too the
            // TODO if request is error return 400.
            let response = handler request

            // The message text.
            let text = Encoding.UTF8.GetString buffer

            logger.Post
                { from = "Connection Handler"
                  message = text
                  time = DateTime.Now
                  ``type`` = Information }

            // Create a basic response.
            let response =
                Encoding.Default.GetBytes "Hello, world!"

            // Write a response.
            // For now this will close the connection afterwards.
            stream.Write(response, 0, response.Length)

            connection.Close()
        }

    /// The listening loop.
    let rec listen (handler: (Request -> Response)) =
        // Await a connection.
        // This is blocking.
        let connection = listener.AcceptTcpClient()

        logger.Post
            { from = "Listener"
              message = "Connection received."
              time = DateTime.Now
              ``type`` = Debug }

        // Send to a background thread to handle.
        // **NOTE** logger needs to be passed, not referenced.
        handleConnection connection handler
        |> Async.Start
        |> ignore

        logger.Post
            { from = "Listener"
              message = "Back to main."
              time = DateTime.Now
              ``type`` = Debug }

        // Repeat the listen loop.
        listen (handler)

    /// Start the listening loop and accept incoming requests.
    let start =

        // "Inject" the route map, the 404 page and logger.
        // The handler can then be passed to the listen loop.
        let handler =
            ConnectionHandler.handle routeMap notFound logger

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
