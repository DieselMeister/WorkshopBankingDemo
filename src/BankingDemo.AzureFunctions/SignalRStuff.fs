module SignalRStuff

open Microsoft.Azure.WebJobs.Extensions.SignalRService
open Newtonsoft.Json



let hubName = "banking"

let serializationOption = JsonSerializerSettings(TypeNameHandling=TypeNameHandling.All)


let getSignalRBankingHub () =
    async {
        return! 
            StaticServiceHubContextStore.Get().GetAsync(hubName).AsTask() |> Async.AwaitTask
    }


let sendAccountIds (dataRepo:DataAccess.DataRepository) =
    async {
        let accountIds = dataRepo.GetAccountIds ()
        let! hub = getSignalRBankingHub ()
        do! hub.Clients.All.SendCoreAsync("accounts",[| accountIds |]) |> Async.AwaitTask
    }


let sendAccountDataToClient (dataRepo:DataAccess.DataRepository) accountId =
    async {
        let! accountData = dataRepo.GetAccount accountId
        match accountData with
        | None -> ()
        | Some accountData ->
            let! hub = getSignalRBankingHub ()
            let accountDataJson = JsonConvert.SerializeObject(accountData,serializationOption);
            do! hub.Clients.User(accountId).SendCoreAsync("account", [| accountDataJson |]) |> Async.AwaitTask
    }

