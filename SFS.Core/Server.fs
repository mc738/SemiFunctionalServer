namespace SFS.Core

module Server =

    open System.Threading
    open System.Net.Sockets

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
        ()

    let rec listen () =
        // Await a connection.
        // This is blocking.
        let connection = listener.AcceptTcpClient()

        // TODO add POC logging here.

        // Send to a background thread to handle.
        handleConnection connection |> sendToTPL |> ignore

        // TODO add POC logging here.

        // Once a connection accepted
        // pass it to the thread pool.
        // The quicker this happens last issues with connections.

        listen ()

    let start =
        // Start a tcp listener on specified ip and port.

        listener.Start()

        // Pass the listener to `listen` function.
        // This will recursively handle requests.
        listen |> ignore
        
        // TODO Add shutdown.