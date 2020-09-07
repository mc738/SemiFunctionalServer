namespace SFS.Core

module Server =
    open System
    open System.Text
    open SFS.Core.Logging
    open SFS.Core.Utilities
    open System.Net.Sockets

    let logger = Logger()

    // TODO Set these in config.
    let ip = "127.0.0.1"
    let port = 42000

    /// The tcp listener.
    let listener = TcpListener.Create(port)

    /// Handle a connection.
    /// This is designed to be run off of the main thread.
    let handleConnection (logger: Logger) (connection: TcpClient) =
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
    let rec listen () =
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
        handleConnection logger connection
        |> Async.Start
        |> ignore

        logger.Post
            { from = "Listener"
              message = "Back to main."
              time = DateTime.Now
              ``type`` = Debug }

        // Repeat the listen loop.
        listen ()

    /// Start the listening loop and accept incoming requests.
    let start =

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
        listen () |> ignore

// TODO Add shutdown.
