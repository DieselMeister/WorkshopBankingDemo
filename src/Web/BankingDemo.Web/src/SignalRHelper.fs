module SignalRHelper

open System
open Fable.Core
open Fable.Import.JS

type SignalRResult = {
    Result: obj
}

type IHubConnection =
    [<Emit("$0.on($1,$2)")>]
    abstract On:string * (obj -> unit) -> unit
    
    
    [<Emit("$0.start()")>]
    abstract Start:unit -> Promise<unit>


type IConnectionBuilder =
    [<Emit("new signalR.HubConnectionBuilder()")>]
    abstract Create: unit->IConnectionBuilder
    [<Emit("$0.withUrl($1,{ accessTokenFactory: function() { return $2; } })")>]
    abstract WithUrl: string * string -> IConnectionBuilder
    [<Emit("$0.build()")>]
    abstract Build: unit-> IHubConnection


[<Import("default", from="@aspnet/signalr")>]
let connectionBuilder:IConnectionBuilder = jsNative




