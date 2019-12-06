module Commands

open Model
open Fable.Import
open BankingDemo.Dtos
open Fable.PowerPack
open Fable.Core
open Elmish


let openSignalRConnectionCmd accountId =
    fun dispatch ->

        let onGetAllAccountIds (accountIds:obj) =
            let accountIds = accountIds :?> array<string>
            dispatch (AllAccountsUpdates (accountIds |> Array.toList))
            


        let onGetAccountData (data:obj) =
            let data = data :?> string
            let res = data |> Thoth.Json.Decode.fromString BankAccount.Decoder                 
            match res with
            | Error e ->
                Fable.Import.Browser.console.log(e)
            | Ok res ->
                dispatch (AccountDataUpdated res)
                
                
        
        promise {
            try
                dispatch GotoConnectionForm
                let! info = Clients.SignalR.getConnectionInfo accountId

                do! Clients.SignalR.openSignalRConnection info onGetAllAccountIds onGetAccountData
                dispatch Connected
            with
            | _ as ex ->
                dispatch (OnError ex.Message)

        } |> Promise.start
        
    |> Cmd.ofSub


let getAccountCmd accountId =
    let promi accountId =
        promise {
            let! result = Clients.Banking.getAccount accountId
            return AccountDataUpdated result 
        }

    Cmd.ofPromise 
        promi
        accountId
        (fun x -> x)
        (fun ex -> OnError ex.Message)

    

let depositCashCmd data =
    let promi data =
        promise {
            let! result = Clients.Banking.sendDepositCash data
            match result with
            | Error r ->
                return (OnError r)
            | Ok _ ->
                return CashDepositSend
        }
    Cmd.ofPromise 
        promi
        data
        (fun x -> x)
        (fun ex -> OnError ex.Message)


let withdrawCashCmd data =
    let promi data =
        promise {
            let! result = Clients.Banking.sendWithdrawCash data
            match result with
            | Error r ->
                return (OnError r)
            | Ok _ ->
                return CashWithdrawSend
        }
    Cmd.ofPromise 
        promi
        data
        (fun x -> x)
        (fun ex -> OnError ex.Message)


let sepaTransferCmd data =
    let promi data =
        promise {
            let! result = Clients.Banking.sendSepaTransfer data
            match result with
            | Error r ->
                return (OnError r)
            | Ok _ ->
                return SepaTransferSend
        }
    Cmd.ofPromise 
        promi
        data
        (fun x -> x)
        (fun ex -> OnError ex.Message)





    

