module AzureFunctions

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Newtonsoft.Json
open Microsoft.Azure.SignalR.Management
open Microsoft.Azure.WebJobs.Extensions.SignalRService


let hubName = "banking"

let private serializationOption = JsonSerializerSettings(TypeNameHandling=TypeNameHandling.All)

let getSignalRBankingHub () =
    async {
        return! 
            StaticServiceHubContextStore.Get().GetAsync(hubName).AsTask() |> Async.AwaitTask
    }


let sendAccountIds () =
    async {
        let accountIds = DataAccess.getAccountIds ()
        let! hub = getSignalRBankingHub ()
        do! hub.Clients.All.SendCoreAsync("accounts",[| accountIds |]) |> Async.AwaitTask
    }


let sendAccountDataToClient accountId =
    async {
        let accountData = DataAccess.getAccount accountId
        let! hub = getSignalRBankingHub ()
        let accountDataJson = JsonConvert.SerializeObject(accountData,serializationOption);
        do! hub.Clients.User(accountId).SendCoreAsync("account", [| accountDataJson |]) |> Async.AwaitTask
    }


[<FunctionName("GetAccount")>]
let getAccount ([<HttpTrigger(AuthorizationLevel.Function, "get",Route = "getaccount/{id}")>] req:HttpRequest, id:string, log:ILogger) =
    async {
        log.LogInformation("C# HTTP trigger function processed a request.")
        let account = DataAccess.getAccount id
        match account with
        | None ->
            return NotFoundResult() :> IActionResult
        | Some account ->
            return (OkObjectResult(account) :> IActionResult)
    } |> Async.StartAsTask


[<FunctionName("DepositCash")>]
let depositCash ([<HttpTrigger(AuthorizationLevel.Function, "post",Route = "depositcash")>] req:HttpRequest, log:ILogger) =
    async {
        log.LogInformation("C# HTTP trigger function processed a request.")

        let! requestBody = (new StreamReader(req.Body)).ReadToEndAsync() |> Async.AwaitTask
        let cashDepositData = JsonConvert.DeserializeObject<DataAccess.Dto.CashDeposit>(requestBody);
        let bankService = Services.createBackAccountService DataAccess.getAccount DataAccess.storeAccount


        let account = bankService.DepositCash cashDepositData.AccountId cashDepositData.Amount

        do! sendAccountIds ()
        do! sendAccountDataToClient cashDepositData.AccountId



        match account with
        | Error e ->
            return BadRequestObjectResult(e) :> IActionResult
        | Ok () ->
            return (OkResult() :> IActionResult)
    } |> Async.StartAsTask


[<FunctionName("WithdrawCash")>]
let withdrawCash ([<HttpTrigger(AuthorizationLevel.Function, "post",Route = "withdrawCash")>] req:HttpRequest, log:ILogger) =
    async {
        log.LogInformation("C# HTTP trigger function processed a request.")

        let! requestBody = (new StreamReader(req.Body)).ReadToEndAsync() |> Async.AwaitTask
        let cashWithdrawData = JsonConvert.DeserializeObject<DataAccess.Dto.CashWithdrawn>(requestBody);
        let bankService = Services.createBackAccountService DataAccess.getAccount DataAccess.storeAccount


        let account = bankService.WithdrawCash cashWithdrawData.AccountId cashWithdrawData.Amount

        do! sendAccountIds ()
        do! sendAccountDataToClient cashWithdrawData.AccountId

        match account with
        | Error e ->
            return BadRequestObjectResult(e) :> IActionResult
        | Ok () ->
            return (OkResult() :> IActionResult)
    } |> Async.StartAsTask


[<FunctionName("SepaTransfer")>]
let sepaTransfer ([<HttpTrigger(AuthorizationLevel.Function, "post",Route = "sepaTransfer")>] req:HttpRequest, log:ILogger) =
    async {
        log.LogInformation("C# HTTP trigger function processed a request.")

        let! requestBody = (new StreamReader(req.Body)).ReadToEndAsync() |> Async.AwaitTask
        let sepaData = JsonConvert.DeserializeObject<DataAccess.Dto.SepaTransaction>(requestBody);
        let bankService = Services.createBackAccountService DataAccess.getAccount DataAccess.storeAccount


        let account = bankService.SepaTransfer sepaData.SourceAccount sepaData.TargetAccount sepaData.Amount

        do! sendAccountIds ()
        do! sendAccountDataToClient sepaData.SourceAccount
        do! sendAccountDataToClient sepaData.TargetAccount

        match account with
        | Error e ->
            return BadRequestObjectResult(e) :> IActionResult
        | Ok () ->
            return (OkResult() :> IActionResult)
    } |> Async.StartAsTask
    



    

