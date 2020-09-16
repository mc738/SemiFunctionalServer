namespace SFS.Core

open System
open System.Net.Sockets
open System.Text
open SFS.Core.Utilities

module ConnectionHandler =

    open SFS.Core.Http
    open SFS.Core.WebSockets
    open SFS.Core.Logging
    open SFS.Core.Routing
    open SFS.Core.ContentTypes


    type Context =
        { routes: Map<string, Route>
          errorRoutes: ErrorRoutes
          logger: Logger
          connection: TcpClient }

    let standardHeaders =
        seq {
            "Server", "SFS"
            "Connection", "close"
        }

    module internal HttpHandler =

        let createResponseHeaders (contentType: ContentType) contentLength (otherHeaders: (string * string) seq) =

            let cDetails =
                seq {
                    ("Content-Type", (getCTString contentType))
                    ("Content-Length", contentLength.ToString())
                }

            standardHeaders
            |> Seq.append cDetails
            |> Seq.append otherHeaders
            |> Map.ofSeq

        let createResponse (route: Route) (context: Context) =

            // Get the content type and content.
            let (contentType, content) =
                match route.routeType with
                | RouteType.Static -> (route.contentType, route.content)

            let contentLength =
                match content with
                | Some c -> c.Length
                | None -> 0

            // Any other headers associated with this response,
            // i.e. none standard and now generated.
            let otherHeaders = Seq.empty

            // Make the headers.
            let headers =
                createResponseHeaders contentType contentLength otherHeaders

            // Create the body.
            let body =
                match content with
                | Some v -> Some(Http.Binary v)
                | None -> None

            // Create the response.
            createResponse 200s headers body

        /// Handle a request and return a response.
        /// This function is designed to be testable against, with out network infrastructure.
        let handlerRequest context request =
            let route =
                matchRoute context.routes context.errorRoutes.notFound request.route

            // Create the response and serialize it.
            createResponse route context

    module internal WebSocketHandler =
            
        let wsHandler (context: Context) (connection: NetworkStream) =
            
            // TODO Get channel.
            
            let rec handlerLoop () = async {
                // Try read from the stream
                if connection.DataAvailable then
                    let! data = Streams.readToBuffer connection 256
                    ()
        
                // Try to read from channel.
                
                            
                return! handlerLoop ()
            }
                
            handlerLoop () |> Async.RunSynchronously // TODO is this needed?

            // TODO handle connection shutdown.



    let handleWebSockets (context: Context) (connection: NetworkStream) (request: Request) =
        // Valid WS handshake and send server handshake.
        let response = createWSHttpResponse request

        match response with
        | Ok r ->
            let data = r |> serializeResponse
            connection.Write(data, 0, data.Length)

        | Result.Error e ->

            ()





        // ...

        ()

    let handleHttp (context: Context) (connection: NetworkStream) (request: Request) =
        let response =
            HttpHandler.handlerRequest context request
            |> serializeResponse

        //        let test = Encoding.UTF8.GetString(response)

        // Send the response.
        connection.Write(response, 0, response.Length)

        connection.Close()


    let handle (context: Context) (connection: NetworkStream) (request: Result<Request, string>) =

        match request with
        | Ok r ->
            // TODO handle case insensitivity.
            let upgrade = getHeader r "Upgrade"
            match upgrade with
            | Some "websockets" -> handleWebSockets context connection r
            | Some _ -> handleHttp context connection r
            | None -> handleHttp context connection r
        | Result.Error e ->
            // TODO Log request error.
            let response =
                HttpHandler.createResponse context.errorRoutes.badRequest context
                |> serializeResponse

            connection.Write(response, 0, response.Length)
            connection.Close()



    /// Accepts a context and a connection and handles it.
    /// This is meant to be run on a background thread.
    let handleConnection (context: Context) (connection: TcpClient) =
        async {

            // For now accept a message, convert to string and send back a message.
            context.logger.Post
                { from = "Connection Handler"
                  message = "In handler."
                  time = DateTime.Now
                  ``type`` = Debug }

            // Get the network stream
            let stream = connection.GetStream()

            // Rec loop to make sure we don't read into buffer before data is available.
            // Github issue: `Issue with request buffer #4`
            let rec waitForData () =
                if stream.DataAvailable then
                    ()
                else
                    Async.Sleep 100 |> ignore
                    waitForData ()

            waitForData ()

            // Read the incoming request into the buffer.
            let! buffer = Streams.readToBuffer stream 1056

            let request = deserializeRequest buffer

            // TODO Need to check if there is anything special with this request, i.e. WS upgrade.

            // The `handle` function will take care of responses from here.
            handle context stream request

            return ()
        }
