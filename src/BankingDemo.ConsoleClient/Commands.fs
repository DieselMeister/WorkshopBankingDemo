module Commands

open Model
open System
open System.Net.Http
open Newtonsoft.Json
open Microsoft.Azure.WebJobs.Extensions.SignalRService
open Microsoft.AspNetCore.SignalR.Client
open System.Threading.Tasks
open DataAccess.Dto
open Terminal.Gui.Elmish


let baseUrl = "http://localhost:7071"
let private serializationOption = JsonSerializerSettings(TypeNameHandling=TypeNameHandling.All)


let getConnectionInfo accountId =
    async {
        use client = new HttpClient()
        let url = sprintf "%s/api/negotiate?username=%s&hubname=banking" baseUrl accountId
        client.DefaultRequestHeaders.Add("x-ms-signalr-userid", accountId)
        let! res = client.PostAsync(url,null) |> Async.AwaitTask
        if not res.IsSuccessStatusCode then
            return failwith ("error getting signal r service info")
        else
            let! content = res.Content.ReadAsStringAsync() |> Async.AwaitTask
            let info = JsonConvert.DeserializeObject<SignalRConnectionInfo>(content)
            return info
    }


let openSignalRConnectionCmd accountId =
    fun dispatch ->
        async {
            dispatch GotoConnectionForm
            let! info = getConnectionInfo accountId
            let connection = HubConnectionBuilder()
                                .WithUrl(Uri(info.Url),
                                    (fun (options) ->
                                        options.AccessTokenProvider <- (fun () -> Task.FromResult(info.AccessToken))
                                        ()
                                    )
                                )
                                .Build()
            
            connection.On("newMessage",(fun (data:string) -> ())) |> ignore
            connection.On("accounts",
                (fun (data:string []) -> 
                    dispatch (AllAccountsUpdates (data |> Array.toList))
                )
            ) |> ignore
            connection.On("account",
                (fun (data:string) -> 
                    let accountData = JsonConvert.DeserializeObject<BankAccount option>(data,serializationOption)
                    dispatch (AccountDataUpdated accountData)
                )
            ) |> ignore
            
            do! connection.StartAsync() |> Async.AwaitTask

            dispatch GotoMainForm
        } |> Async.StartImmediate
    |> Cmd.ofSub

