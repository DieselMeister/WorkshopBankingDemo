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




