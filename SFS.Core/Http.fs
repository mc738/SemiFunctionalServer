namespace SFS.Core

open System.ComponentModel.Design.Serialization
open System.Text.Encodings.Web

module ContentTypes =

    type ContentType =
        | Html
        | Css
        | Jpg
        | Gif
        | Json
        | JavaScript
        | Xml

    let getCTString contentType =
        match contentType with
        | Html -> "text/html"
        | Css -> "text/css"
        | Jpg -> "image/jpeg"
        | Gif -> "image/gif"
        | Json -> "application/json"
        | JavaScript -> "text/javascript"
        | Xml -> "application/xml"

module Http =

    open System.IO
    open System
    open System.Text
    open SFS.Core.Logging

    type Request =
        { verb: Verb
          route: string
          version: string
          headers: Map<string, string>
          body: Body option }

    and Response =
        { code: StatusCode
          version: string
          headers: Map<string, string>
          body: Body option }

    and Verb =
        | Get
        | Head
        | Post
        | Put
        | Delete
        | Connect
        | Options
        | Trace
        | Patch

    and StatusCode = { name: string; code: int16 }

    and Body =
        | Text of string
        | Binary of byte array

    let getVerb (verb: string) =
        match verb.ToUpper() with
        | "GET" -> Get
        | "HEAD" -> Head
        | "POST" -> Post
        | "PUT" -> Put
        | "DELETE" -> Delete
        | "CONNECT" -> Connect
        | "OPTIONS" -> Options
        | "TRACE" -> Trace
        | "PATCH" -> Patch
        | _ -> Get

    let switchingProtocols = { name = "Switching Protocols"; code = 101s }
        
    let ok = { name = "Ok"; code = 200s }

    let created = { name = "Created"; code = 201s }

    let badRequest = { name = "Bad Request"; code = 400s }

    let unauthorized = { name = "Unauthorized"; code = 401s }

    let forbidden = { name = "Forbidden"; code = 403s }

    let notFound = { name = "Not Found"; code = 404s }

    let internalError =
        { name = "Internal Server Error"
          code = 500s }

    let notImplemented =
        { name = "Not Implemented"
          code = 501s }

    let getStatus code =
        match code with
        | 101s -> switchingProtocols
        | 200s -> ok
        | 201s -> created
        | 400s -> badRequest
        | 401s -> unauthorized
        | 403s -> forbidden
        | 404s -> notFound
        | 500s -> internalError
        | _ -> notImplemented

    /// Accepts 4 bytes representing i, i - 1, i - 2 & i - 3.
    /// Then flips them and checks if the are 13,10,13,10 ('\r\n\r\n').
    let checkForSplit index indexMinus1 indexMinus2 indexMinus3 =
        match (indexMinus3, indexMinus2, indexMinus1, index) with
        | (13uy, 10uy, 13uy, 10uy) -> true
        | _ -> false

    /// A recursive function to find the head/body split
    /// in a byte array representing a http message.
    /// If found the index will be that of the last character from the first '\r\n\r\n'.
    let rec findSplitIndex (data: byte array) (i: int) =
        let len = data.Length
        match i with
        | _ when len <= i -> Result.Error "Out of range."
        | _ when i - 3 >= 0 ->
            if checkForSplit data.[i] data.[(i - 1)] data.[(i - 2)] data.[(i - 3)]
            then Ok(i + 1)
            else findSplitIndex data (i + 1)
        | _ -> findSplitIndex data (i + 1)

    /// Get the header/body split index.
    /// If could Some(int) will be returned,
    /// if not the data is not http.
    let getHeaderSplitIndex (data: byte array) =

        let i = findSplitIndex data 0

        match i with
        | Ok i -> Ok i
        | Result.Error message -> Result.Error message

    let createFirstLine (firstLine: string) =
        let split = firstLine.Split(' ')

        if split.Length >= 3 then
            let verb = split.[0]
            let route = split.[1]
            let version = split.[2]
            Ok(verb, route, version)
        else
            Result.Error "Unable to parse first line of request"

    let createHeader (header: string) =
        let split = header.Split(": ")
        if split.Length > 1 then (split.[0], split.[1]) else (split.[0], String.Empty)

    let createHeaders (headers: string list) =
        headers |> List.map createHeader |> Map.ofList

    /// Create a request from a header string and body.
    let createRequest (body: Body option) (text: string) =
        let (firstLine, rest) =
            text.Split "\r\n"
            |> List.ofArray
            |> (fun t -> (t.Head, t.Tail))

        match createFirstLine firstLine with
        | Ok (verb, route, version) ->
            let headers = createHeaders rest
            Ok
                { verb = getVerb verb
                  version = version
                  route = route
                  headers = headers
                  body = body }
        | Result.Error message -> Result.Error message

    let deserializeRequest (data: byte array) =

        let splitIndex = getHeaderSplitIndex data

        match splitIndex with
        | Ok (i) ->
            let (h, b) = Array.splitAt i data

            // If there is some data add a body (as binary for now).
            // The body could be non-text, so it can be handled later.
            let body =
                if (b.Length > 0) then Some(Binary b) else None

            // The header can be converted to text from bytes now.
            // Remove the last bits to get rid of trailing `\r\n\r\n` (5).
            // TODO This feels like a bit of a hack, find a better solution.
            let head = Encoding.Default.GetString h.[0..(h.Length - 5)]

            createRequest body head

        | Result.Error message -> Result.Error message


    /// Get a header value from a request.
    let getHeader (request: Request) key = request.headers.TryFind key


    let createResponse code headers body =

        /// Get the response code and create the response.
        let status = getStatus code

        { code = status
          version = "1.1"
          headers = headers
          body = body }


    let serializeHeader headers key value = sprintf "%s%s: %s\r\n" headers key value

    let (+++) a b =
        Seq.append a b
    
    /// Serialize a response to a byte array.
    let serializeResponse (response: Response) =

        // Bytes for '\r\n\r\n'
        let split = [| 13uy; 10uy |]

        // TODO Make this more efficient!
        let firstLine =
            sprintf "HTTP/%s %i %s\r\n" response.version response.code.code response.code.name

        let headerText =
            response.headers |> Map.fold serializeHeader ""

        let body =
            match response.body with
            | Some b ->
                match b with
                | Binary d -> d
                | Text d -> Encoding.UTF8.GetBytes(d)
            | None -> Array.empty

        let flb = Encoding.UTF8.GetBytes(firstLine)
        let hb = Encoding.UTF8.GetBytes(headerText)

        Seq.append flb hb
        +++ split
        +++ body
        |> Array.ofSeq
        
        


    let requestHandler (logger: Logger) (data: byte array) =

        let request = deserializeRequest data

        match request with
        | Ok request ->
            // If successfully deserialized pass to the router for handling.
            ()
        | Result.Error error ->

            let message =
                sprintf "Could not deserialize request. Error: '%s'" error

            // TODO Post 400 reply?

            logger.Post
                ({ from = "Request Handler"
                   message = message
                   time = DateTime.Now
                   ``type`` = Error })
            ()
        ()