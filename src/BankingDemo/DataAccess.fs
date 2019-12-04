module DataAccess
    
    open Newtonsoft.Json

    module Dto =
        
        type ITransaction = 
            abstract EventName:string

        type CashDeposit = {
                AccountId:string
                Amount:decimal
                EventName:string
            }
            with 
                interface ITransaction with
                    member this.EventName = this.EventName
                


        type CashWithdrawn = {
            AccountId:string
            Amount:decimal
            EventName:string
            }
            with 
                interface ITransaction with
                    member this.EventName = this.EventName


        type SepaTransaction = {
            SourceAccount:string            
            TargetAccount:string
            Amount:decimal
            EventName:string
            }
            with 
                interface ITransaction with
                    member this.EventName = this.EventName

        

        type BankAccount = {
            AccountId:string
            Amount:decimal
            Transactions: ITransaction list
        }
                with 
                    static member Empty accountId = {
                            AccountId = accountId
                            Amount = 0.0m
                            Transactions = []
                        }
                    

        open Domain.DataTypes
        open Domain.TransactionArgs


        let private transactionFromDto (dto:ITransaction) =
            match dto with
            | :? CashDeposit as d -> 
                {
                    AccountId = AccountId.create d.AccountId
                    Amount = Money.create d.Amount
                } |> Domain.CashDeposit
            | :? CashWithdrawn as w ->
                {
                    AccountId = AccountId.create w.AccountId
                    Amount = Money.create w.Amount
                } |> Domain.CashWithdrawn
            | :? SepaTransaction as s ->
                Domain.SepaTransaction {
                    SourceAccount = AccountId.create s.SourceAccount
                    TargetAccount = AccountId.create s.TargetAccount
                    Amount =Money.create s.Amount
                }
            | _ -> failwith "unknow transaction type"

        let private transactionFromDomain transaction =
            match transaction with
            | Domain.CashDeposit d ->
                {
                    CashDeposit.AccountId = AccountId.value d.AccountId
                    Amount = Money.value d.Amount
                    EventName="CashDeposit"
                } :> ITransaction

            | Domain.CashWithdrawn w ->
                {
                    CashWithdrawn.AccountId = AccountId.value w.AccountId
                    Amount = Money.value w.Amount
                    EventName="CashWithdrawn"
                } :> ITransaction

            | Domain.SepaTransaction s ->
                {
                    SourceAccount = AccountId.value s.SourceAccount
                    TargetAccount = AccountId.value s.TargetAccount
                    Amount =Money.value s.Amount
                    EventName="SepaTransaction"
                } :> ITransaction


        let bankAccountFromDto (ba:BankAccount) : Domain.BankAccount =
            {
                AccountId = AccountId.create ba.AccountId
                Amount = Money.create ba.Amount
                Transactions = ba.Transactions |> List.map (transactionFromDto)
            }

        let bankAccountFromDomain (ba:Domain.BankAccount) : BankAccount =
            {
                AccountId = AccountId.value ba.AccountId
                Amount = Money.value ba.Amount
                Transactions = ba.Transactions |> List.map (transactionFromDomain)
            }
                


    open Dto

    type AccountStore = {
        Accounts:BankAccount list
    }



    let private filename = "bankaccounts.json"
    let private serializationOption = JsonSerializerSettings(TypeNameHandling=TypeNameHandling.All)


    open System
    open System.IO


    let loadAccounts () =
        if File.Exists(filename) then
            let storeJson = File.ReadAllText(filename)
            let store = JsonConvert.DeserializeObject<AccountStore>(storeJson,serializationOption)
            store.Accounts
        else
            []
        

    let getAccount accountId =
        let accounts = loadAccounts ()
        accounts |> List.tryFind (fun i -> i.AccountId = accountId)



    let getAccountIds () =
        let accounts = loadAccounts ()
        accounts 
        |> List.map (fun i -> i.AccountId) 
        |> List.distinct


    let storeAccount (account:Domain.BankAccount) =
        let domainAccounts =
            loadAccounts ()
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


        

