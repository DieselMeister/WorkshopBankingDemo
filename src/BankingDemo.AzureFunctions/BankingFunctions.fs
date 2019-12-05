module BankingFunctions

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Newtonsoft.Json
open SignalRStuff
open FSharp.Control.Tasks.V2



let dataRepo = lazy(
    let storageConnectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage")
    let dataRepo = DataAccess.createAzureBlobStorageRepository storageConnectionString "bankingData.json" 
    dataRepo
)

let bankService = lazy (
    task {
        let! dataRepo = dataRepo.Force()
        return Services.createBackAccountService dataRepo
    }
)


[<FunctionName("GetAccount")>]
let getAccount ([<HttpTrigger(AuthorizationLevel.Anonymous, "get",Route = "getaccount/{id}")>] req:HttpRequest, id:string, log:ILogger) =
    task {
        log.LogInformation("C# HTTP trigger function processed a request.")

        let! dataRepo = dataRepo.Force()

        let! account = dataRepo.GetAccount id

        match account with
        | None ->
            return NotFoundResult() :> IActionResult
        | Some account ->
            // for the sake of an easy example, I added the type information in the json
            let result = JsonResult(account,SignalRStuff.serializationOption) 
            result.StatusCode <- (StatusCodes.Status200OK |> Nullable)
            return result :> IActionResult
    } 



[<FunctionName("DepositCash")>]
let depositCash ([<HttpTrigger(AuthorizationLevel.Anonymous, "post",Route = "depositcash")>] req:HttpRequest, log:ILogger) =
    task {
        log.LogInformation("C# HTTP trigger function processed a request.")

        let! requestBody = (new StreamReader(req.Body)).ReadToEndAsync()
        let cashDepositData = JsonConvert.DeserializeObject<Dtos.CashDeposit>(requestBody);

        let! dataRepo = dataRepo.Force()
        let! bankService = bankService.Force()
        


        let! account = bankService.DepositCash cashDepositData.AccountId cashDepositData.Amount

        do! sendAccountIds dataRepo
        do! sendAccountDataToClient dataRepo cashDepositData.AccountId

        match account with
        | Error e ->
            return BadRequestObjectResult(e) :> IActionResult
        | Ok () ->
            return (OkResult() :> IActionResult)
    } 


[<FunctionName("WithdrawCash")>]
let withdrawCash ([<HttpTrigger(AuthorizationLevel.Anonymous, "post",Route = "withdrawCash")>] req:HttpRequest, log:ILogger) =
    task {
        log.LogInformation("C# HTTP trigger function processed a request.")

        let! requestBody = (new StreamReader(req.Body)).ReadToEndAsync()
        let cashWithdrawData = JsonConvert.DeserializeObject<Dtos.CashWithdrawn>(requestBody);

        let! dataRepo = dataRepo.Force()
        let! bankService = bankService.Force()


        let! account = bankService.WithdrawCash cashWithdrawData.AccountId cashWithdrawData.Amount

        do! sendAccountIds dataRepo
        do! sendAccountDataToClient dataRepo cashWithdrawData.AccountId

        match account with
        | Error e ->
            return BadRequestObjectResult(e) :> IActionResult
        | Ok () ->
            return (OkResult() :> IActionResult)
    } 


[<FunctionName("SepaTransfer")>]
let sepaTransfer ([<HttpTrigger(AuthorizationLevel.Anonymous, "post",Route = "sepaTransfer")>] req:HttpRequest, log:ILogger) =
    task {
        log.LogInformation("C# HTTP trigger function processed a request.")

        let! requestBody = (new StreamReader(req.Body)).ReadToEndAsync()
        let sepaData = JsonConvert.DeserializeObject<Dtos.SepaTransaction>(requestBody);

        let! dataRepo = dataRepo.Force()
        let! bankService = bankService.Force()


        let! account = bankService.SepaTransfer sepaData.SourceAccount sepaData.TargetAccount sepaData.Amount

        do! sendAccountIds dataRepo
        do! sendAccountDataToClient dataRepo sepaData.SourceAccount
        do! sendAccountDataToClient dataRepo sepaData.TargetAccount

        match account with
        | Error e ->
            return BadRequestObjectResult(e) :> IActionResult
        | Ok () ->
            return (OkResult() :> IActionResult)
    } 
    



    

