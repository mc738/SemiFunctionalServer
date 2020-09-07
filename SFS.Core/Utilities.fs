namespace SFS.Core.Utilities


module Streams =

    open System.IO
    
    let readToBuffer (stream:Stream) bufferSize = async {
        let buffer = [|for i in [0..bufferSize] -> 0uy|]
        stream.ReadAsync(buffer, 0, bufferSize) |> Async.AwaitTask |> ignore
        return buffer
    }
        

module Threads =
    open System.Threading.Tasks

    /// Send a task to the thread pool
    let dispatchTask task =
        // From http://www.fssnip.net/1P/title/Easy-Wrapper-for-thread-pool-work.
        Async.StartAsTask <| async { return task }

    /// Convert a ValueTask<'a> to Async<'a>.
    let convertValueTask<'a> (task: ValueTask<'a>) = task.AsTask() |> Async.AwaitTask


module Messaging =
    open System.Threading.Channels

    /// A wrapper class for `System.Threading.Channels.ChannelRead`
    type Reader<'a>(reader: ChannelReader<'a>) =

        /// Set the reader listening for items.
        member this.Start handler =
            let rec loop () =
                async {

                    // This should get the next item async.
                    // Thus not annoying the thread pool
                    let! item = reader.ReadAsync() |> Threads.convertValueTask

                    // Once an item is received send it to the handler function.
                    let cont = handler item

                    // Recursive loop.
                    if cont then loop () |> ignore
                }

            loop ()

    /// A wrapper class for `System.Threading.Channels.ChannelWriter`
    type Writer<'a>(writer: ChannelWriter<'a>) =

        /// Post a message to the writer.
        member this.Post item =
            match writer.TryWrite item with
            | true -> Ok()
            | false -> Error()

    /// Create a mail box processor of type 'a
    /// and assign it a handler for incoming items.
    let createMBP<'a> handler =
        MailboxProcessor<'a>
            .Start(fun inbox ->
                  let rec loop () =
                      async {

                          let! item = inbox.Receive()

                          let cont = handler item

                          if cont then return! loop ()
                      }

                  loop ())

    let createChannel<'a> =
        let channel = Channel.CreateUnbounded<'a>()
        let reader = Reader channel.Reader
        let writer = Writer channel.Writer
        (reader, writer)