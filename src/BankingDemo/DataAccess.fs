module DataAccess
    
    open Newtonsoft.Json
    open Dtos
    open FSharp.Control.Tasks.V2


    type AccountStore = {
        Accounts:BankAccount list
    }

       
    let private serializationOption = JsonSerializerSettings(TypeNameHandling=TypeNameHandling.All)

    
       
    module private FileStorage =

        open System
        open System.IO

        let loadAccounts filename =
            if File.Exists(filename) then
                let storeJson = File.ReadAllText(filename)
                let store = JsonConvert.DeserializeObject<AccountStore>(storeJson,serializationOption)
                store.Accounts
            else
                []
        

        let getAccount filename accountId =
            let accounts = loadAccounts filename
            accounts |> List.tryFind (fun i -> i.AccountId = accountId)



        let getAccountIds filename =
            let accounts = loadAccounts filename
            accounts 
            |> List.map (fun i -> i.AccountId) 
            |> List.distinct


        let storeAccount filename (account:Domain.BankAccount) =
            let domainAccounts =
                loadAccounts filename
                |> List.map (bankAccountFromDto)

            let newAccountsList =
                if domainAccounts |> List.exists (fun i -> i.AccountId = account.AccountId) then
                    // update accounts
                    domainAccounts
                    |> List.map (fun i -> 
                        if (i.AccountId = account.AccountId) then
                            account
                        else
                            i
                    )
                else
                    account :: domainAccounts

            let newStore =
                {
                    Accounts = newAccountsList |> List.map (bankAccountFromDomain)
                }

            let newStoreJson = JsonConvert.SerializeObject(newStore, serializationOption)
            File.WriteAllText(filename,newStoreJson)



    module private AzureBlobStorage =

        open Microsoft.WindowsAzure.Storage
        open Microsoft.WindowsAzure.Storage.Blob

        let createStorageAccount connectionString =
            let (isValid,storageAccount) = CloudStorageAccount.TryParse(connectionString)
            if not isValid then 
                failwith ("error connection storage account")
            else
                storageAccount


        let createBlob filename (storageAccount:CloudStorageAccount) =
            task {
                let client = storageAccount.CreateCloudBlobClient()
                let container = client.GetContainerReference("bankingdata")
                let! _ = container.CreateIfNotExistsAsync()
                let blob = container.GetBlockBlobReference(filename);
                blob.Properties.ContentType <- "application/json";
                return blob
            }
            



        let loadAccounts (blob:CloudBlockBlob) =
            task {
                let! existsBlob = blob.ExistsAsync()
                if existsBlob then
                    let! storeJson = blob.DownloadTextAsync()
                    let store = JsonConvert.DeserializeObject<AccountStore>(storeJson,serializationOption)
                    return store.Accounts
                else
                    return []
            }
            
        

        let getAccount blob accountId =
            task {
                let! accounts = loadAccounts blob
                return accounts |> List.tryFind (fun i -> i.AccountId = accountId)
            }
            



        let getAccountIds blob =
            task {
                let! accounts = loadAccounts blob
                return
                    accounts 
                    |> List.map (fun i -> i.AccountId) 
                    |> List.distinct
            }
            


        let storeAccount blob (account:Domain.BankAccount) =
            task {
                let! domainAccounts =
                    loadAccounts blob
                    
                let domainAccounts =
                    domainAccounts
                    |> List.map (bankAccountFromDto)

                let newAccountsList =
                    if domainAccounts |> List.exists (fun i -> i.AccountId = account.AccountId) then
                        // update accounts
                        domainAccounts
                        |> List.map (fun i -> 
                            if (i.AccountId = account.AccountId) then
                                account
                            else
                                i
                        )
                    else
                        account :: domainAccounts

                let newStore = {
                    Accounts = newAccountsList |> List.map (bankAccountFromDomain)
                }

                let newStoreJson = JsonConvert.SerializeObject(newStore, serializationOption)
                do! blob.UploadTextAsync(newStoreJson)
                
            }
            
        
    open System.Threading.Tasks

    type DataRepository = {
        LoadAccounts: unit -> Task<BankAccount list>
        GetAccount: string -> Task<BankAccount option>
        GetAccountIds: unit -> Task<string list>
        StoreAccount: Domain.BankAccount -> Task<unit>
    }


    let createFileStorageRepository filename = {
        LoadAccounts = fun () -> FileStorage.loadAccounts filename |> Task.FromResult
        GetAccount = fun id -> FileStorage.getAccount filename id |> Task.FromResult
        GetAccountIds = fun () -> FileStorage.getAccountIds filename |> Task.FromResult
        StoreAccount = fun id -> FileStorage.storeAccount filename id |> Task.FromResult
    }


    let createAzureBlobStorageRepository connectionString filename = 
        task {
            let storageAccount = AzureBlobStorage.createStorageAccount connectionString
            let! blob = AzureBlobStorage.createBlob filename storageAccount

            return {
                LoadAccounts = fun () -> AzureBlobStorage.loadAccounts blob
                GetAccount = AzureBlobStorage.getAccount blob
                GetAccountIds = fun () -> AzureBlobStorage.getAccountIds blob
                StoreAccount = AzureBlobStorage.storeAccount blob
            }
        }
    


        

