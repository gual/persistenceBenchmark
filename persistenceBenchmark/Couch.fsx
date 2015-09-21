#I "bin/Debug"

#r "DreamSeat.dll"
#r "BowStats.dll"

#load "Utils.fsx"
#load "Connection.fsx"

open System

open DreamSeat

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
            let cl = new CouchClient()
            return Some cl
        with
            | e -> return None
    }

let write recipient (data:byte []) (connection:CouchClient) =
    let key = Guid.NewGuid().ToString()

    printfn "Couchdb Data: %A, %A" key data

    async {
        Stats.Aggregator.run "write" (fun () ->
                let result = { key = key; data = data } |> createDocument server recipient
                printfn "Result %A - %A" result result.id
            )
    } |> ignore
    addToKeys key


let read recipient (keys:string []) (connection:CouchClient) =
    let key = keys |> Seq.nth (rand.Next(0, keys.Length))

    async {
        Stats.Aggregator.run "read" (fun () ->
            let result = getDocument<TRecord> server recipient key
            printfn "Read: %A" result
        )
    }

//Benchmark.Connection.write "couchdb" 2 createConnection write 10
//Benchmark.Utils.writeKeys keys file

//let aeroKeys = Benchmark.Utils.readKeys file
FSharpCouch.
let couchKeys = [|"8dcd8d0c-b332-4c6a-ab70-d4dadf630515"|]
Benchmark.Connection.read "couchdb" 2 createConnection read couchKeys 10