#I "bin/Debug"

#r "AerospikeClient.dll"
#r "BowStats.dll"

#load "Utils.fsx"
#load "Connection.fsx"

open System

open Aerospike.Client

let host = "122.3.2.2"
let port = 3000
let mutable keys = List.Empty
let file = "aerospike"
let rand = Random(file.GetHashCode())

Stats.Publisher.start "benchmark.db.aerospike" "monitoring.beyondgames.io" 2003 60

let policy = new AsyncClientPolicy()
policy.readPolicyDefault.timeout <- 50
policy.readPolicyDefault.maxRetries <- 3
policy.readPolicyDefault.sleepBetweenRetries <- 10
policy.writePolicyDefault.timeout <- 50
policy.writePolicyDefault.maxRetries <- 3
policy.writePolicyDefault.sleepBetweenRetries <- 10
policy.writePolicyDefault.recordExistsAction <- RecordExistsAction.REPLACE
policy.writePolicyDefault.sendKey <- true

policy.failIfNotConnected <- false
policy.timeout <- 5000

let addToKeys (key:string) =
    let value = key + "\n"
    keys <- List.append keys [value]

let createConnection () = 
    async {
        try
            let asHost = new Host(host, port)
            let cl = new AsyncClient(policy, [| asHost |])
            //let cl = new AerospikeClient(host, port)
            match cl.Connected with
            | true -> 
                return Some cl
            | false -> 
                printfn "Couldn't connect"
                return None
        with
            | e -> 
                printfn "Error: %A" e.Message
                return None
    }

let write recipient (data:byte []) (connection:AsyncClient) =
    let key = Guid.NewGuid().ToString()
    addToKeys key

    let recordKey = new Key("local", "test", key)
    let record = new Bin("name", data)

    printfn "Aerospike Data: %A, %A" recordKey record

    async {
        Stats.Aggregator.run "write" (fun () -> connection.Put(null, recordKey, record))
    }

let read recipient (keys:string []) (connection:AsyncClient) =
    let key = keys |> Seq.nth (rand.Next(0, keys.Length))

    let recordKey = new Key("local", "test", key)

    async {
        Stats.Aggregator.run "read" (fun () -> connection.Get(null, recordKey) |> ignore)
    }

Benchmark.Connection.write "aerospike" 1 createConnection write 1000
Benchmark.Utils.writeKeys keys file

//let aeroKeys = Benchmark.Utils.readKeys file
//let aeroKeys = [|"b6506e16-2d36-47ef-8ceb-323a91faa8b8"; "4e300166-8c3c-4b1d-a1c5-b7b8d443f1f9"|]
//Benchmark.Connection.read "aerospike" 10 createConnection read aeroKeys 1000
