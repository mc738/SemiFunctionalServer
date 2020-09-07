namespace SFS.Core.Logging

open System
open System.ComponentModel
open Microsoft.VisualBasic
open SFS.Core.Utilities

type LogItem =
    { from: string
      message: string
      time: DateTime
      ``type``: ItemType }

and ItemType =
    | Success
    | Error
    | Information
    | Warning
    | Debug

/// A basic logging class.
type Logger() =

    let getCCT itemType =
        match itemType with
        | Success -> (ConsoleColor.Green, "OK")
        | Error -> (ConsoleColor.Red, "ERROR")
        | Information -> (ConsoleColor.White, "INFO")
        | Warning -> (ConsoleColor.Yellow, "WARN")
        | Debug -> (ConsoleColor.DarkGray, "DEBUG")

    let handleItem item = 
        let (color, title) = getCCT item.``type``
        Console.ForegroundColor <- color
        printf "%s\t" title
        Console.ResetColor()
        let time = item.time.ToString()
        printfn "[%s] %s: %s" time item.from item.message
        true
 
    let listener = Threads.createMBP<LogItem> handleItem

    member this.Post item = listener.Post item