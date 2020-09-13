namespace SFS.Core

open System.Text
open SFS.Core.Utilities

module WebSockets =

    open Hashing

    let wsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"



    let createHandshake wsKey =
        let key = sprintf "%s%s" wsKey wsGuid
        Encoding.UTF8.GetBytes key
        |> sha1
        |> System.Convert.ToBase64String
