namespace SFS.Test.UnitTests

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

