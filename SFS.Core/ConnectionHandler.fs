namespace SFS.Core

open System
open System.Net.Sockets
open System.Text
open SFS.Core.Utilities

module ConnectionHandler =

    open SFS.Core.Http
    open SFS.Core.Logging
    open SFS.Core.Routing
    open SFS.Core.ContentTypes


    type Context =
        { routes: Map<string, Route>
          errorRoutes: ErrorRoutes
          logger: Logger }

    let standardHeaders =
        seq {
            "Server", "SFS"
            "Connection", "close"
        }


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
        let headers = createResponseHeaders contentType contentLength otherHeaders

        // Create the body.
        let body =
            match content with
            | Some v -> Some(Binary v)
            | None -> None

        // Create the response.
        createResponse 200s headers body

    /// Handle a request and return a response.
    /// This function is designed to be testable against, with out network infrastructure.
    let handlerRequest context request =
        let route =
                match request with
                | Ok r -> matchRoute context.routes context.errorRoutes.notFound r.route
                | Result.Error e ->
                    // TODO Log error.
                    context.errorRoutes.badRequest

            // Create the response and serialize it.
        createResponse route context
        
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
            let rec waitForData() =
                if stream.DataAvailable then
                    ()
                else
                    Async.Sleep 100 |> ignore
                    waitForData()
            
            waitForData()
            
            // Read the incoming request into the buffer.
            let! buffer = Streams.readToBuffer stream 1056 // |> Async.RunSynchronously

            let request = deserializeRequest buffer

            // Handle the request, and serialize the response.
            let response = handlerRequest context request |> serializeResponse
            
            let test = Encoding.UTF8.GetString(response)
            
            
            // Send the response.
            stream.Write(response, 0, response.Length)

            // Create the headers.
            
            connection.Close()

            return ()
        }