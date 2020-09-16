namespace SFS.Core

open SFS.Core.Utilities.Messaging

module WebSockets =

    open System
    open System.Text
    open System.Threading.Channels
    open SFS.Core.Http
    open SFS.Core.Utilities
    open Hashing

    type WebSocketMessage =
        | Binary of byte array
        | Text of string
        | NewSubscription of Subscription
        
    /// This represents a client connection, however it is separate for network infrastructure.
    /// This means it doesn't rely on a network for testing.  
    and Subscription = {
        ref: Guid
        writer: Writer<WebSocketMessage>
        started: DateTime
    }
    and ClientConnectionDetails = {
        get: (unit -> WebSocketMessage option)
        post: (WebSocketMessage -> unit)
    }
        
    type WebSocketChannel(name: string) =

        let write message (clientConnection: Subscription) = clientConnection.writer.Post message

        let handler (message: WebSocketMessage) (state: Map<Guid,Subscription>) =
            // Handle incoming message.
            let writer = write (Text "Hello From Server!")
            
            let newState =
                match message with
                | Text t ->
                    // Test - echo response.
                    state |> Map.map (fun k v -> writer v) |> ignore // Ignore for now.
                    state
                | Binary b ->
                    state
                | NewSubscription s ->
                    // Add the subscription to a new state.
                    Map.add s.ref s state
                    
            Async.Sleep 500 |> ignore
                
            true, newState

        let listener =
            let state: Map<Guid,Subscription> = Map.empty
            Messaging.createMBPWithState<WebSocketMessage, Map<Guid,Subscription>> state handler
        
        member this.Post(message) =
            listener.Post message
            
        /// Get client connections details for the channel.
        member this.GetClientConnectionDetails (reader) =
            let post = this.Post
            let get = reader
            ()


    // A wrapper around ChannelReader.TryGet.
    // To allow for checking with in a cycle.
    let createGet (reader:Reader<WebSocketMessage>) () =
        let mutable d = ref (Text "")
        
        match reader.TryGet d with
        | Some v -> Some !v
        | None -> None

    let createSubscription (channel:WebSocketChannel) =
        let id = Guid.NewGuid()
        
        let (reader,writer) = createChannel<WebSocketMessage>
        
        let subscription = {
            ref = id
            writer = writer
            started = DateTime.Now
        }
        
        let get = createGet reader
       
        let clientConnection = {
            get = get
            post = channel.Post
        }
        
        (subscription, clientConnection)
        
        
    
    /// Standard WebSockets guid.
    let wsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"

    let createHandshake wsKey =
        let key = sprintf "%s%s" wsKey wsGuid
        Encoding.UTF8.GetBytes key
        |> sha1
        |> System.Convert.ToBase64String

    let createWSHttpResponse (request: Request) =
        let key = getHeader request "Sec-WebSocket-Key"

        // Check if `Sec-WebSocket-key` is found.
        // TODO add other validations.
        match key with
        | Some k ->
            let handshake = createHandshake k
            //let acceptHeader = "Sec-WebSocket-Accept",

            let i = 0

            let headers =
                seq {
                    "Upgrade", "websocket"
                    "Connection", "Upgrade"
                    "Sec-WebSocket-Accept", handshake
                    "Sec-WebSocket-Version", "13"
                }

            let mappedHeaders = headers |> Map.ofSeq

            Ok(createResponse 101s mappedHeaders None)

        | None -> Error "No `Sec-WebSocket-Key` header present."

    /// Handle the `msglen` value and return a length and offset.
    let handleMsgLen (msgLen: byte) (data: byte array) =
        match msgLen with
        | d when d < 126uy ->
            let v : uint16 = uint16 d
            (v, 2)
        | 126uy ->
            let length =
                BitConverter.ToUInt16([| data.[2]; data.[3] |], 0)
            (length, 4)
        | _ -> (0us, 0)

    /// Decode data from a client.
    let decode (masks: byte array) start (data: byte array) =

        let rec loop (masks: byte array, data: byte array, result: byte seq, index) =
            if index < data.Length then
                let value = data.[index] ^^^ masks.[(index % 4)]

                let newResult = Seq.append result (Seq.singleton value)

                loop (masks, data, newResult, index + 1)
            else
                result

        let result = loop (masks, data, Seq.empty, start)

        result

    let handleRead (data: byte array) =
        // Handle the first bytes.
        // Then decode the message.
        let fin = (data.[0] &&& 0b10000000uy) <> 0uy
        let mask = (data.[1] &&& 0b10000000uy) <> 0uy // must be true, "All messages from the client to the server have this bit set"
        let optCode = data.[0] &&& 0b00001111uy
        let msgLen = data.[1] - 128uy

        let (length, offset) = handleMsgLen msgLen data

        match (length, mask) with
        | (_, false) ->
            // No mask, error!
            Error "No mask present."
        | (0us, _) ->
            // No message len.
            Error "Message length is 0."
        | (127us, true) ->
            // Not implemented.
            Error "Not implemented yet."
        | (_, true) ->
            let maskBytes = data.[offset..(offset + 3)]
            let newOffset = offset + 4
            let message = decode maskBytes newOffset data
            Ok message

    let handlerWrite (data: byte array) =
        // TODO handle bigger data (i.e. split it).
        let fin = 0x80uy
        let byte1 = fin ||| 1uy
        let mask = 0uy // No mask, not client. 0x80 for client

        /// TODO this only supports short message so far.
        let byte2 = mask ||| (byte) data.Length

        let fBytes = [| byte1; byte2 |]

        Array.append fBytes data
