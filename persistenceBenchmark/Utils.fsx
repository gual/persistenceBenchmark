module Benchmark.Utils

open System
open System.IO

let maxKeys = 1000

let writeKeys (completeKeys:string list) name =
    let file = sprintf "/tmp/%s.keys" name
    let keys = Seq.take maxKeys completeKeys |> List.ofSeq
    printfn "Writing %A keys: %A to %A" maxKeys keys file

    let wr = new StreamWriter(file, true)
    wr.Write(keys |> List.reduce (+))

    printfn "Write ended"

let readKeys name =
    let file = sprintf "/tmp/%s.keys" name
    printfn "Reading keys from %A" file
    File.ReadAllLines(file)