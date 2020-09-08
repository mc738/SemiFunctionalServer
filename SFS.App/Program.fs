// Learn more about F# at http://fsharp.org

open System
open SFS.Core

[<EntryPoint>]
let main argv =
    // Start the server.
    Server.start
    0 // return an integer exit code
