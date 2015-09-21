module Benchmark.Connection

#I "bin/Debug"
#r "Mono.Posix.dll"

open System
open Mono.Unix

let bucket = "benchmark"

let private benchmarkAction numClients clientFactory action printEvery =
    async { 
        let! clients = 
            Array.init numClients (fun _ -> clientFactory ())
            |> Async.Parallel

        let connected = 
            clients |> Array.fold (fun conn cl ->
                match cl with
                | Some ns ->
                    ns :: conn
                | None -> conn
            ) List.empty
            |> Array.ofList

        printfn "%d clients connected" connected.Length

        connected |> Array.iteri (fun i conn ->
            let i = i + 1
            let rec loop n =
                async {
                    if (n % printEvery) = 0 && n > 0 then
                        printfn "%d records" n

                    do! action conn

                    Console.ReadLine() |> ignore

                    return! loop (n + 1)
                }
            loop 0 |> Async.Start
        )

    } |> Async.RunSynchronously

    let signals = [|
        new UnixSignal(Native.Signum.SIGINT)
    |]

    let s = UnixSignal.WaitAny signals
    printfn "Completed, got %O." <| Enum.GetName(typeof<Native.Signum>, signals.[s].Signum)

let write backend clients clientFactory writeFunc printEvery =
    let data = BitConverter.GetBytes(1337)
    let numClients = clients

    printfn "Starting writing benchmark to %s with %d clients" backend numClients
    printfn "Writing to: %s bucket/table/set" bucket

    let writeAction = writeFunc bucket data

    benchmarkAction numClients clientFactory writeAction printEvery
    printfn "Writing benchmark completed, bye"
    
let read backend clients clientFactory readFunc keys printEvery =
    let numClients = clients

    printfn "Starting reading benchmark from %s with %d clients" backend numClients
    printfn "reading from: %s bucket/table/set" bucket

    let readAction = readFunc bucket keys

    benchmarkAction numClients clientFactory readAction printEvery
    printfn "Reading benchmark completed, bye"