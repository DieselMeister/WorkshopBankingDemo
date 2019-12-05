module SignalRStuff

open Microsoft.Azure.WebJobs.Extensions.SignalRService
open Newtonsoft.Json
open FSharp.Control.Tasks.V2



let hubName = "banking"

let serializationOption = JsonSerializerSettings(TypeNameHandling=TypeNameHandling.All)


let getSignalRBankingHub () =
    task {
        return! 
            StaticServiceHubContextStore.Get().GetAsync(hubName).AsTask()
    }


let sendAccountIds (dataRepo:DataAccess.DataRepository) =
    task {
        let accountIds = dataRepo.GetAccountIds ()
        let! hub = getSignalRBankingHub ()
        do! hub.Clients.All.SendCoreAsync("accounts",[| accountIds |])
    }


let sendAccountDataToClient (dataRepo:DataAccess.DataRepository) accountId =
    task {
        let! accountData = dataRepo.GetAccount accountId
        match accountData with
        | None -> ()
        | Some accountData ->
            let! hub = getSignalRBankingHub ()
            let accountDataJson = JsonConvert.SerializeObject(accountData,serializationOption);
            do! hub.Clients.User(accountId).SendCoreAsync("account", [| accountDataJson |])
    }

