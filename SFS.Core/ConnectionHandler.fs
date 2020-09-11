namespace SFS.Core

open System
open System.Net.Sockets
open SFS.Core.Utilities

module ConnectionHandler =
        
    open SFS.Core.Http
    open SFS.Core.Logging
    open SFS.Core.Routing
    
    
    type Context = {
        routes: Map<string,Route>
        errorRoutes: ErrorRoutes
        logger: Logger
    }
    
    /// Accepts a context and a connection and handles it.
    /// This is meant to be run on a background thread.
    let handleConnection (context:Context) (connection: TcpClient) = async {
        
      // For now accept a message, convert to string and send back a message.
        context.logger.Post
            { from = "Connection Handler"
              message = "In handler."
              time = DateTime.Now
              ``type`` = Debug }

        // Get the network stream
        let stream = connection.GetStream()

        // Read the incoming request into the buffer.
        let! buffer = Streams.readToBuffer stream 255 // |> Async.RunSynchronously

        let request = deserializeRequest buffer
        
        let route =
            match request with
            | Ok r ->
                matchRoute context.routes context.errorRoutes.notFound r.route
            | Result.Error e ->
                // TODO Log error.
                context.errorRoutes.badRequest
        
        
        // Create the response.
        
        // Send the response.
        stream.Write(response, 0, response.Length)
        
        // Create the headers.
        
        return ()
    }
        
        