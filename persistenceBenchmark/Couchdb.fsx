#I "bin/Debug"

#r "FSharpCouch.dll"
#r "BowStats.dll"

#load "Utils.fsx"
#load "Connection.fsx"

open System

open FSharpCouch

let server = "http://192.168.3.76:5984"
let mutable keys = List.Empty
let file = "couchdb"
let rand = Random(file.GetHashCode())

type TRecord = {
    key : string
    data : byte []
}

Stats.Publisher.start "benchmark.db.couchdb" "monitoring.beyondgames.io" 2003 60

let addToKeys (key:string) =
    let value = key + "\n"
    keys <- List.append keys [value]

let createConnection () = 
    async {
        try
            return Some ()
        with
            | e -> return None
    }

let write recipient (data:byte []) _conn =
    let key = Guid.NewGuid().ToString()

    //printfn "Couchdb Data: %A, %A" key data

    async {
        let result = Stats.Aggregator.run "write" (fun () -> 
                { key = key; data = data } |> createDocument server recipient
            )
        addToKeys result.id
    }

let read recipient (keys:string []) _conn =
    let key = keys |> Seq.nth (rand.Next(0, keys.Length))

    async {
        Stats.Aggregator.run "read" (fun () ->
            getDocument<TRecord> server recipient key |> ignore
        )
    }

//Benchmark.Connection.write "couchdb" 10 createConnection write 100
//Benchmark.Utils.writeKeys keys file

let couchKeys = Benchmark.Utils.readKeys file
//let couchKeys = [|"8dcd8d0c-b332-4c6a-ab70-d4dadf630515"|]
Benchmark.Connection.read "couchdb" 10 createConnection read couchKeys 100