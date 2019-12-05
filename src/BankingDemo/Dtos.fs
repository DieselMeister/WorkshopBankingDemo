module Dtos


type ITransaction = interface end


type CashDeposit = {
        AccountId:string
        Amount:decimal
}
    with 
        interface ITransaction
        


type CashWithdrawn = {
    AccountId:string
    Amount:decimal
}
    with 
        interface ITransaction 


type SepaTransaction = {
    SourceAccount:string            
    TargetAccount:string
    Amount:decimal
}
    with 
        interface ITransaction 




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
        } :> ITransaction

    | Domain.CashWithdrawn w ->
        {
            CashWithdrawn.AccountId = AccountId.value w.AccountId
            Amount = Money.value w.Amount
        } :> ITransaction

    | Domain.SepaTransaction s ->
        {
            SepaTransaction.SourceAccount = AccountId.value s.SourceAccount
            TargetAccount = AccountId.value s.TargetAccount
            Amount =Money.value s.Amount
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

