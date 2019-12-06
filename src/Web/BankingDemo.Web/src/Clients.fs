module Clients

open Fable.PowerPack
open SignalRHelper
open BankingDemo
open BankingDemo.Dtos
open Fable.PowerPack
open Fable.PowerPack.Fetch.Fetch_types



let baseUrl = "http://localhost:7071"





module SignalR =

    open Fable.Core

    type SignalRConnectionInfo = {
        url:string
        accessToken:string
    }        

    let getConnectionInfo accountId =
        promise {
            let url = sprintf "%s/api/negotiate?username=%s&hubname=banking" baseUrl accountId
            let headers = Fetch.requestHeaders [
                HttpRequestHeaders.Custom ("x-ms-signalr-userid", accountId)
            ]
            let! res = Fetch.fetch url [ headers ]
            if not res.Ok then
                return failwith ("error getting signal r service info")
            else
                let! content = res.json<SignalRConnectionInfo>()                
                return content
        }


    

    let openSignalRConnection (info:SignalRConnectionInfo) onGetAllAccountIds onGetAccountData =
        promise {
            
            let connection =
                SignalRHelper.connectionBuilder.Create()
                    .WithUrl(info.url, info.accessToken)
                    .Build()
            
            
            connection.On("accounts",(fun (data:obj) -> onGetAllAccountIds data))
            connection.On("account",(fun (data:obj) -> onGetAccountData data))
            do! connection.Start()
        }
        



module Banking =

    let getAccount accountId =
        promise {
            let url = sprintf "%s/api/getaccount/%s" baseUrl accountId
            let! res = Fetch.fetch url []
            if not res.Ok then
                // Not Found
                if res.Status = 404 then
                    return BankAccount.Empty accountId
                else
                    return failwith ("error getting account data")
            else
                let! content = res.text()
                let res = content |> Thoth.Json.Decode.fromString BankAccount.Decoder                 
                match res with
                | Error e ->
                    Fable.Import.Browser.console.log(e)
                    return BankAccount.Empty accountId
                | Ok res ->
                    return res
        }


    let private sendData (url:string) data =
        promise {

            let! res = Fetch.postRecord url data []
            if not res.Ok then
                let! content = res.text()
                let content =
                    if content = "" then 
                        sprintf "StatusCode: %i" res.Status
                    else
                        content

                return Error content
            else
                return Ok ()
        }


    let sendDepositCash data =
        promise {
            let url = sprintf "%s/api/depositcash" baseUrl
            return! sendData url data
        } 


    let sendWithdrawCash data =
        promise {
            let url = sprintf "%s/api/withdrawcash" baseUrl
            return! sendData url data            
        } 


    let sendSepaTransfer data =
        promise {
            let url = sprintf "%s/api/sepatransfer" baseUrl
            return! sendData url data            
        } 

