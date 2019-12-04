module SignalrFunctions

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.Azure.WebJobs.Extensions.SignalRService
open System.Web
open Microsoft.AspNetCore.Http
open Microsoft.Azure.SignalR.Management


[<FunctionName("negotiate")>]
let getSignalRInfo ([<HttpTrigger(AuthorizationLevel.Anonymous)>] req: HttpRequest, 
                    [<SignalRConnectionInfo(HubName = "banking", UserId="{headers.x-ms-signalr-userid}")>] connectionInfo:SignalRConnectionInfo) =
        connectionInfo


[<FunctionName("blabla")>]
let timeTriggeredMessage ([<TimerTrigger("*/15 * * * * *")>] timerInfo:TimerInfo) =
    async {
        let! (serviceHubContext:IServiceHubContext) = 
            StaticServiceHubContextStore.Get().GetAsync("banking").AsTask() |> Async.AwaitTask

        do! serviceHubContext.Clients.All.SendCoreAsync("newMessage", [| "time jetriggert!" |> unbox |] ) |> Async.AwaitTask
    
    } |> Async.StartAsTask


