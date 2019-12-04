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
open System.Net
open System.Text


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

        let onGetAllAccountIds (accountIds:string []) =
            dispatch (AllAccountsUpdates (accountIds |> Array.toList))


        let onGetAccountData (data:string) =
            let accountData = JsonConvert.DeserializeObject<BankAccount>(data,serializationOption)
            dispatch (AccountDataUpdated accountData)
        
        async {
            dispatch GotoConnectionForm
            let! info = getConnectionInfo accountId

            let connection = HubConnectionBuilder()
                                .WithUrl(Uri(info.Url),
                                    fun (options) ->
                                        options.AccessTokenProvider <- (fun () -> Task.FromResult(info.AccessToken))
                                )
                                .Build()
            
            connection.On("accounts", fun (data:string[]) -> onGetAllAccountIds data) |> ignore
            connection.On("account", fun (data:string) -> onGetAccountData data) |> ignore
            
            do! connection.StartAsync() |> Async.AwaitTask

            dispatch Connected
        } |> Async.StartImmediate
    |> Cmd.ofSub


let getAccountCmd accountId =
    async {
        use client = new HttpClient()
        let url = sprintf "%s/api/getaccount/%s" baseUrl accountId
        let! res = client.GetAsync(url) |> Async.AwaitTask
        if not res.IsSuccessStatusCode then
            if res.StatusCode = HttpStatusCode.NotFound then
                return AccountDataUpdated (BankAccount.Empty accountId)
            else
                return failwith ("error getting account data")
        else
            let! content = res.Content.ReadAsStringAsync() |> Async.AwaitTask
            let info = JsonConvert.DeserializeObject<BankAccount>(content,serializationOption)
            return AccountDataUpdated (info)
    } |> Cmd.OfAsync.result



let private sendData (url:string) data : Async<Result<unit,string>> =
    async {
        use client = new HttpClient()
        let dataJson = JsonConvert.SerializeObject(data)
        let content = new StringContent(dataJson,Encoding.UTF8,"application/json")
        let! res = client.PostAsync(url,content) |> Async.AwaitTask
        if not res.IsSuccessStatusCode then
            let! content = res.Content.ReadAsStringAsync() |> Async.AwaitTask
            let content =
                if content = "" then 
                    sprintf "StatusCode: %A" res.StatusCode
                else
                    content

            return Error content
        else
            return Ok ()
    }


let depositCashCmd data =
    async {
        let url = sprintf "%s/api/depositcash" baseUrl
        let! result = sendData url data
        match result with
        | Error r ->
            return (OnError r)
        | Ok _ ->
            return CashDepositSend
    } |> Cmd.OfAsync.result


let withdrawCashCmd data =
    async {
        let url = sprintf "%s/api/withdrawcash" baseUrl
        let! result = sendData url data
        match result with
        | Error r ->
            return (OnError r)
        | Ok _ ->
            return CashWithdrawSend
    } |> Cmd.OfAsync.result


let sepaTransferCmd data =
    async {
        let url = sprintf "%s/api/sepatransfer" baseUrl
        let! result = sendData url data
        match result with
        | Error r ->
            return (OnError r)
        | Ok _ ->
            return SepaTransferSend
    } |> Cmd.OfAsync.result
    

