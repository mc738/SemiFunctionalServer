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
        
        let responseString = "HTTP/1.1 200 OK\r\nServer: SFS\r\nContent-Length: 88\r\nContent-Type: text/htmlConnection: Closed\r\n\r\n<p>hello world!</p>"

        
        
        [<TestMethod>]
        member this.``deserialize result request successfully`` () =
            let expected = expectedRequest
            
            let result = deserializeRequest requestBytes
            
            match result with
            | Ok actual -> Assert.AreEqual(expected, actual);
            | Error e -> Assert.Fail e