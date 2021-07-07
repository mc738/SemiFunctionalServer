namespace SFS.Test.UnitTests

open System
open System.Text
open Microsoft.VisualStudio.TestTools.UnitTesting
open SFS.Core.WebSockets 

module WebSockets =

   
        
    [<TestClass>]
    type StandardTests() =
        
        [<TestMethod>]
        member this.``handshake from spec`` () =
                let expected = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="
                let input = "dGhlIHNhbXBsZSBub25jZQ=="
                
                let actual = createHandshake input
                
                Assert.AreEqual(expected, actual)

        [<TestMethod>]
        member this.``decode test message`` () =
            let expected = "Hello, World!"
            let rng = Random()
            
            let mask =  [|0uy;0uy;0uy;0uy|]
            
            rng.NextBytes(mask)
            
            let decoder = decode mask 0
            
            let data = Encoding.UTF8.GetBytes expected
            
            // Call encode 2x - it is a reversible encoding.
            let actual = decoder data |> Array.ofSeq |> decoder |> Array.ofSeq |> Encoding.UTF8.GetString
            
            Assert.AreEqual(expected, actual)