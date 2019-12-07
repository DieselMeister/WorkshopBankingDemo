module Commands

open Model
open System
open System.Net.Http
open Newtonsoft.Json
open Microsoft.Azure.WebJobs.Extensions.SignalRService
open Microsoft.AspNetCore.SignalR.Client
open System.Threading.Tasks
open Dtos
open Terminal.Gui.Elmish
open System.Net
open System.Text
open FSharp.Control.Tasks.V2.ContextSensitive

let private serializationOption = JsonSerializerSettings(TypeNameHandling=TypeNameHandling.All)


let openSignalRConnectionCmd accountId =
    fun dispatch ->

        let onGetAllAccountIds (accountIds:string []) =
            dispatch (AllAccountsUpdates (accountIds |> Array.toList))
            


        let onGetAccountData (data:string) =
            let accountData = JsonConvert.DeserializeObject<BankAccount>(data,serializationOption)
            dispatch (AccountDataUpdatedFromRemote accountData)
        
        
        async {
            try
                dispatch GotoConnectionForm
                let! info = Clients.SignalR.getConnectionInfo accountId |> Async.AwaitTask

                do! Clients.SignalR.openSignalRConnection info onGetAllAccountIds onGetAccountData
                dispatch Connected
            with
            | _ as ex ->
                dispatch (OnError ex.Message)

        } |> Async.Start
        
    |> Cmd.ofSub


let getAccountCmd accountId =
    task {
        let! result = Clients.Banking.getAccount accountId
        return AccountDataUpdated result
    } |> Cmd.OfTask.result
    

let depositCashCmd data =
    task {
        let! result = Clients.Banking.sendDepositCash data
        match result with
        | Error r ->
            return (OnError r)
        | Ok _ ->
            return CashDepositSend
    } |> Cmd.OfTask.result


let withdrawCashCmd data =
    task {
        let! result = Clients.Banking.sendWithdrawCash data
        match result with
        | Error r ->
            return (OnError r)
        | Ok _ ->
            return CashWithdrawSend
    } |> Cmd.OfTask.result


let sepaTransferCmd data =
    task {
        let! result = Clients.Banking.sendSepaTransfer data
        match result with
        | Error r ->
            return (OnError r)
        | Ok _ ->
            return SepaTransferSend
    } |> Cmd.OfTask.result





    

