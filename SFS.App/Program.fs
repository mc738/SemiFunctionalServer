// Learn more about F# at http://fsharp.org

open System
open SFS.Core

[<EntryPoint>]
let main argv =
    
    Server.start
    
    printfn "Hello World from F#!"
    0 // return an integer exit code
