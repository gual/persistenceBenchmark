#I "bin/Debug"

#r "RiakClient.dll"
#r "BowStats.dll"

#load "Utils.fsx"
#load "Connection.fsx"

open System

open RiakClient
open RiakClient.Models
open RiakClient.Config

let mutable keys = List.Empty
let file = "riak"
let rand = Random(file.GetHashCode())

Stats.Publisher.start "benchmark.db.riak" "monitoring.beyondgames.io" 2003 60

let cluster = RiakCluster.FromConfig("riakConfig", "App.config")

let addToKeys (key:string) =
    let value = key + "\n"
    keys <- List.append keys [value]

let createConnection () = 
    async {
        try
            let cl = cluster.CreateClient()
            let pingResult = cl.Ping()

            match pingResult.IsSuccess with
            | true ->
                return Some cl
            | _ ->
                printfn "Conn couldn't be stablished: %A - %A" pingResult.ResultCode pingResult.ErrorMessage
                return None
        with
            | e -> return None
    }

let write recipient (data:byte []) (connection:IRiakClient) =
    let key = Guid.NewGuid().ToString()
    addToKeys key


    let key = Guid.NewGuid().ToString()
    addToKeys key

    let record = RiakObject(recipient, key, data)
    //printfn "Riak Data: %A, %A" key data

    async {
        Stats.Aggregator.run "write" (fun () -> connection.Put(record) |> ignore)
    }

let read recipient (keys:string []) (connection:IRiakClient) =
    let key = keys |> Seq.nth (rand.Next(0, keys.Length))
    let record = RiakObjectId(recipient, key)

    async {
        Stats.Aggregator.run "read" (fun () -> connection.Get(record) |> ignore)
    }

//Benchmark.Connection.write "riak" 10 createConnection write 1000
//Benchmark.Utils.writeKeys keys file

let riakKeys = Benchmark.Utils.readKeys file
//let riakKeys = [|"b9975850-f6e4-488b-9325-49c6068d2a20"|]
Benchmark.Connection.read "riak" 10 createConnection read riakKeys 1000

