namespace SFS.Core.Utilities

open System

/// Helpers for working with streams.
module Streams =

    open System.IO

    /// Read a stream into a buffer.
    let readToBuffer (stream: Stream) bufferSize =
        async {
            // TODO What if more data than buffer size?
            let buffer =
                [| for i in [ 0 .. bufferSize ] -> 0uy |]

            stream.ReadAsync(buffer, 0, bufferSize)
            |> Async.AwaitTask
            |> ignore

            return buffer
        }

/// Helpers for working with threads.
module Threads =
    open System.Threading.Tasks

    /// Send a task to the thread pool
    let dispatchTask task =
        // From http://www.fssnip.net/1P/title/Easy-Wrapper-for-thread-pool-work.
        Async.StartAsTask <| async { return task }

    /// Convert a ValueTask<'a> to Async<'a>.
    let convertValueTask<'a> (task: ValueTask<'a>) = task.AsTask() |> Async.AwaitTask

    /// A helper function simulate a synchronously blocking process.
    let blockFor time =
        // Test - wait one second before replying.
        Async.Sleep time
        |> Async.RunSynchronously
        |> ignore


/// Helpers for working with messages.
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
            
        member this.TryGet (d) =
           match reader.TryRead(d) with
           | true -> Some d
           | false -> None
           // let item = reader.TryRead()

    /// A wrapper class for `System.Threading.Channels.ChannelWriter`
    type Writer<'a>(writer: ChannelWriter<'a>) =

        /// Post a message to the writer.
        member this.Post item =
            match writer.TryWrite item with
            | true -> Ok()
            | false -> Error()

    /// Create a mail box processor of type 'a and state of 'b.
    let createMBPWithState<'a,'b> (state:'b) handler =
        MailboxProcessor<'a>
            .Start(fun inbox ->
                  let rec loop (state) =
                      async {

                          let! item = inbox.Receive()

                          let (cont,newState) = handler item state

                          if cont then return! loop (newState)
                      }

                  loop (state))
    
    
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
   

    /// Create a channel, and get it's reader and writer.
    let createChannel<'a> =
        let channel = Channel.CreateUnbounded<'a>()
        let reader = Reader channel.Reader
        let writer = Writer channel.Writer
        (reader, writer)

module Results =

    let splitResult<'a, 'b> (results: ('a seq * 'b seq)) (result: Result<'a, 'b>) =
        let (success, errors) = results
        match result with
        | Ok value -> (Seq.append success [ value ], errors)
        | Error value -> (success, Seq.append errors [ value ])

    /// Split a seq of results by success and failure.
    /// Returns a tuple (successful,errors)
    let splitResults<'a, 'b> results =
        results
        |> Seq.fold splitResult (Seq.empty :> 'a seq, Seq.empty :> 'b seq)

module Maps =

    /// Join to maps.
    /// From: https://stackoverflow.com/questions/3974758/in-f-how-do-you-merge-2-collections-map-instances.
    let join (p: Map<'a, 'b>) (q: Map<'a, 'b>) =
        Map
            (Seq.concat [ (Map.toSeq p)
                          (Map.toSeq q) ])

module Pipelines =

    let bind switchFunction twoTrackInput =
        match twoTrackInput with
        | Ok v -> switchFunction v
        | Error e -> Error e

    let (>==) twoTrackInput switchFunction = bind switchFunction twoTrackInput

    let (>=>) switch1 switch2 x =
        match switch1 x with
        | Ok v -> switch2 v
        | Error e -> Error e

    let switch f x = f x |> Ok

    let map oneTrackFunction twoTrackInput =
        match twoTrackInput with
        | Ok v -> Ok(oneTrackFunction v)
        | Error e -> Error e

    let tee f x =
        f x |> ignore
        x

    let tryCatch f x =
        try
            f x |> Ok
        with ex -> Error ex.Message

    let doubleMap successFunction failureFunction twoTrackInput =
        match twoTrackInput with
        | Ok v -> Ok(successFunction v)
        | Error e -> Error(failureFunction e)

    let succeed x = Ok x

    let fail x = Error x


    let plus addSuccess addError switch1 switch2 x =
        match (switch1 x) (switch2 x) with
        | Ok v1, Ok v2 -> Ok(addSuccess v1 v2)
        | Error e, Ok _ -> Error e
        | Ok _, Error e -> Error e
        | Error e1, Error e2 -> Error(addError e1 e2)

module Hashing =
    open System.Security.Cryptography

    let sha1 (data: byte array) =
        HashAlgorithm.Create("SHA1").ComputeHash data
