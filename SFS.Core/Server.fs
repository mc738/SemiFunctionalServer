namespace SFS.Core

open System
open System.Text
open SFS.Core.Logging
open SFS.Core.Utilities

module Server =

    open System.Threading
    open System.Net.Sockets

    let logger = Logger()
    
    // From http://www.fssnip.net/1P/title/Easy-Wrapper-for-thread-pool-work.
    let sendToTPL task =
        Async.StartAsTask <| async { return task }

    // TODO Set these in config.
    let ip = "127.0.0.1"
    let port = 42000

    let listener = TcpListener.Create(port)

    /// Handle a connection.
    /// This is designed to be run off of the main thread.
    let handleConnection (connection: TcpClient) =
        // TODO add POC logging here.
        logger.Post { from = "Connection Handler"; message = "In handler."; time = DateTime.Now; ``type`` = Debug }

        // For now accept a message, convert to string and send back a message.
       
        // Get the network stream
        let stream = connection.GetStream()
        
        // Create a buffer to hold the message.
        let buffer = [|for i in [0..255] -> 0uy|]
        
        // Read the incoming request into the buffer.
        stream.ReadAsync(buffer, 0, 255) |> Async.AwaitTask |> Async.RunSynchronously |> ignore
        
        let text = Encoding.UTF8.GetString buffer 
        
        let msg = sprintf "Message received: '%s'" text 
        
        logger.Post { from = "Connection Handler"; message = text; time = DateTime.Now; ``type`` = Information }
        
        // Create a basic response.
        let response = Encoding.Default.GetBytes "Hello, world!" 
        
        // Write a response.
        // For now this will close the connection afterwards.
        stream.Write(response, 0, response.Length)
        
        connection.Close()
       

    let rec listen () =
        // Await a connection.
        // This is blocking.
        let connection = listener.AcceptTcpClient()

        // TODO add POC logging here.
        logger.Post { from = "Listener"; message = "Connection received."; time = DateTime.Now; ``type`` = Debug }

        // Send to a background thread to handle.
        handleConnection connection |> sendToTPL |> ignore

        // TODO add POC logging here.
        logger.Post { from = "Listener"; message = "Back to main."; time = DateTime.Now; ``type`` = Debug }

        // Once a connection accepted
        // pass it to the thread pool.
        // The quicker this happens last issues with connections.

        listen ()

    let start =
        // Start a tcp listener on specified ip and port.
        logger.Post { from = "Main"; message = "Starting listener."; time = DateTime.Now; ``type`` = Information }

        listener.Start()
        logger.Post { from = "Main"; message = "Listener started."; time = DateTime.Now; ``type`` = Success }

        // Pass the listener to `listen` function.
        // This will recursively handle requests.
        listen() |> ignore
        
        // TODO Add shutdown.