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


[<FunctionName("GetAccount")>]
let getAccount ([<HttpTrigger(AuthorizationLevel.Function, "get",Route = "/{id}")>] req:HttpRequest, id:string, log:ILogger) =
    async {
        log.LogInformation("C# HTTP trigger function processed a request.")
        let account = DataAccess.getAccount id
        match account with
        | None ->
            return NotFoundResult() :> IActionResult
        | Some account ->
            return (OkObjectResult(account) :> IActionResult)
    } |> Async.StartAsTask
    

