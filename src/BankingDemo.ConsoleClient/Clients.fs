module Clients


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
open FSharp.Control.Tasks.V2


let baseUrl = "http://localhost:7071"


let private serializationOption = JsonSerializerSettings(TypeNameHandling=TypeNameHandling.All)



module SignalR =


    let getConnectionInfo accountId =
        task {
            use client = new HttpClient()
            let url = sprintf "%s/api/negotiate?username=%s&hubname=banking" baseUrl accountId
            client.DefaultRequestHeaders.Add("x-ms-signalr-userid", accountId)
            let! res = client.PostAsync(url,null)
            if not res.IsSuccessStatusCode then
                return failwith ("error getting signal r service info")
            else
                let! content = res.Content.ReadAsStringAsync()
                let info = JsonConvert.DeserializeObject<SignalRConnectionInfo>(content)
                return info
        }


    let openSignalRConnection (info:SignalRConnectionInfo) onGetAllAccountIds onGetAccountData =
        async {
            let connection =
                HubConnectionBuilder()
                    .WithUrl(Uri(info.Url),
                        fun (options) ->
                            options.AccessTokenProvider <- (fun () -> Task.FromResult(info.AccessToken))
                    )
                    .WithAutomaticReconnect([| TimeSpan.Zero; TimeSpan.Zero; TimeSpan.FromSeconds(10.0) |])
                    .Build()
            
            connection.On("accounts", fun (data:string[]) -> onGetAllAccountIds data; ()) |> ignore
            connection.On("account", fun (data:string) -> onGetAccountData data; ()) |> ignore
            do! connection.StartAsync() |> Async.AwaitTask
        }
        



module Banking =

    let getAccount accountId =
        task {
            use client = new HttpClient()
            let url = sprintf "%s/api/getaccount/%s" baseUrl accountId
            let! res = client.GetAsync(url)
            if not res.IsSuccessStatusCode then
                if res.StatusCode = HttpStatusCode.NotFound then
                    return BankAccount.Empty accountId
                else
                    return failwith ("error getting account data")
            else
                let! content = res.Content.ReadAsStringAsync()
                let info = JsonConvert.DeserializeObject<BankAccount>(content,serializationOption)
                return info
        }


    let private sendData (url:string) data =
        task {
            use client = new HttpClient()
            let dataJson = JsonConvert.SerializeObject(data)
            let content = new StringContent(dataJson,Encoding.UTF8,"application/json")
            let! res = client.PostAsync(url,content)
            if not res.IsSuccessStatusCode then
                let! content = res.Content.ReadAsStringAsync()
                let content =
                    if content = "" then 
                        sprintf "StatusCode: %A" res.StatusCode
                    else
                        content

                return Error content
            else
                return Ok ()
        }


    let sendDepositCash data =
        task {
            let url = sprintf "%s/api/depositcash" baseUrl
            return! sendData url data
        } 


    let sendWithdrawCash data =
        task {
            let url = sprintf "%s/api/withdrawcash" baseUrl
            return! sendData url data            
        } 


    let sendSepaTransfer data =
        task {
            let url = sprintf "%s/api/sepatransfer" baseUrl
            return! sendData url data            
        } 

