namespace SFS.Test.UnitTests

open System.Text
open Microsoft.VisualStudio.TestTools.UnitTesting
open SFS.Core.Http

module Http =
    
    

    [<TestClass>]
    type TestClass () =
//
//           type Request =
//        { verb: Verb
//          route: string
//          version: string
//          headers: Map<string, string>
//          body: Body option }
//
//    and Response =
//        { code: StatusCode
//          version: string
//          headers: Map<string, string>
//          body: Body option }
        
        
        let requestString = "GET /hello.htm HTTP/1.1\r\nUser-Agent: Mozilla/4.0 (compatible; MSIE5.01; Windows NT)\r\nHost: www.tutorialspoint.com\r\nAccept-Language: en-us\r\nAccept-Encoding: gzip, deflate\r\nConnection: Keep-Alive\r\n\r\n" 
    
        let requestBytes = Encoding.UTF8.GetBytes requestString
        
        let expectedRequest =  {
            verb = Get
            route = "/hello.htm"
            version = "HTTP/1.1"
            headers = seq {
                "User-Agent", "Mozilla/4.0 (compatible; MSIE5.01; Windows NT)"
                "Host", "www.tutorialspoint.com"
                "Accept-Language", "en-us"
                "Accept-Encoding", "gzip, deflate"
                "Connection", "Keep-Alive"
            } |> Map.ofSeq
            body = None
        }
        
        let responseString = "HTTP/1.1 200 Ok\r\nConnection: Closed\r\nContent-Length: 20\r\nContent-Type: text/html\r\nServer: SFS\r\n\r\n<p>Hello, World!</p>"

        let responseActual = {
            code = ok
            version = "1.1"
            headers = seq {
                "Connection", "Closed"
                "Content-Length", "20"
                "Content-Type", "text/html"
                "Server", "SFS"
            } |> Map.ofSeq
            body = Some(Text "<p>Hello, World!</p>")
        }
        
        [<TestMethod>]
        member this.``deserialize request successfully`` () =
            let expected = expectedRequest
            
            let result = deserializeRequest requestBytes
            
            match result with
            | Ok actual -> Assert.AreEqual(expected, actual)
            | Error e -> Assert.Fail e
            
        [<TestMethod>]
        member this.``deserialize invalid request return error`` () =
            
            let result = deserializeRequest [|0uy;0uy;0uy;0uy;0uy;0uy;0uy;0uy;0uy;|]
            
            match result with
            | Ok _ -> Assert.Fail "Should return Error<string>."
            | Error _ -> ()
            
            
        [<TestMethod>]
        member this.``deserialize empty request return error`` () =
            
            let result = deserializeRequest [||]
            
            match result with
            | Ok _ -> Assert.Fail "Should return Error<string>."
            | Error _ -> ()
            
        [<TestMethod>]            
        member this.``serialize response successfully`` () =
            let expected = Encoding.UTF8.GetBytes responseString
            
            let actual = serializeResponse responseActual
            
            // ~~Not sure why this fails, the strings and arrays are the same?~~
            // Was failing because of capital `k` in actual -> "OK".
            // Changed now, this shouldn't actually affect the system.
            CollectionAssert.AreEqual(expected, actual)