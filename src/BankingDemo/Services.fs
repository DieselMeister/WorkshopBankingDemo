module Services



    open Domain
    open Domain.DataTypes
    open Domain.TransactionArgs
    open DataAccess
    open FSharp.Control.Tasks.V2
    open System.Threading.Tasks

    
    
    let private getCurrentAccount dataRepository accountId =
        task {

            let! currentAccount =
                dataRepository.GetAccount accountId
            
            return 
                currentAccount
                |> Option.map (Dtos.bankAccountFromDto)
        }

    let private getCurrentAccountOrDefault dataRepository accountId =
        task {
            let! currentAccount =
                getCurrentAccount dataRepository accountId
            return 
                currentAccount
                |> Option.defaultValue {
                    AccountId = AccountId.create accountId
                    Amount = Money.create 0.0m
                    Transactions = []
                }
        }


    let depositCash (dataRepository:DataRepository) accountId amount =
        task {
            let id = AccountId.create accountId
            
            let cashDeposit = CashDeposit { 
                AccountId = id
                Amount = Money.create amount
            }

            // load account or create an empty one
            let! currentAccount =
                getCurrentAccountOrDefault dataRepository accountId

            
            let newAccountResult = processTransaction currentAccount cashDeposit
            match newAccountResult with
            | Ok newAccount ->
                do! dataRepository.StoreAccount newAccount
                return Ok ()
            | Error e ->
                return Error e
        }
        


    let withdrawCash (dataRepository:DataRepository) accountId amount =
        task {
            let id = AccountId.create accountId
            
            let cashWithdraw =
                { 
                    AccountId = id
                    Amount = Money.create amount
                } |> CashWithdrawn

            let! currentAccount =
                getCurrentAccount dataRepository accountId
            

            match currentAccount with
            | Some account ->
                let newAccountResult = processTransaction account cashWithdraw
                match newAccountResult with
                | Ok newAccount ->
                    do! dataRepository.StoreAccount newAccount
                    return Ok ()
                | Error e ->
                    return Error e

            | None ->
                return Error "insufficent funds"
        }
        


    let sepaTransfer (dataRepository:DataRepository) sourceAccountId targetAccountId amount =
        task {
            let saId = AccountId.create sourceAccountId
            let taId = AccountId.create targetAccountId
            
            let sepaTransfer = SepaTransaction { 
                    SourceAccount = saId
                    TargetAccount = taId
                    Amount = Money.create amount
                }

            let! sourceAccount =
                getCurrentAccount dataRepository sourceAccountId

            // load target account or create empty, because an empty can get funds
            let! targetAccount =
                getCurrentAccountOrDefault dataRepository targetAccountId
            

            match sourceAccount with
            | None ->
                return Error "insufficent funds"
            | Some sourceAccount ->
                let newSourceAccount = processTransaction sourceAccount sepaTransfer

                let newTargetAccount = processTransaction targetAccount sepaTransfer

                match newSourceAccount,newTargetAccount with
                | Ok sa, Ok ta ->
                    // save both
                    do! dataRepository.StoreAccount sa
                    do! dataRepository.StoreAccount ta
                    return Ok ()
                | Error se, Error ta ->
                    return Error (sprintf "%s / %s" se ta)
                | Error se, _ ->
                    return Error se
                | _, Error ta ->
                    return Error ta
        }
        


    type BankAccountService = {
        DepositCash: string -> decimal -> Task<Result<unit,string>>
        WithdrawCash: string -> decimal -> Task<Result<unit,string>>
        SepaTransfer: string -> string -> decimal -> Task<Result<unit,string>>
    }


    let createBackAccountService (dataRepository:DataRepository) =
        {
            DepositCash = depositCash dataRepository
            WithdrawCash = withdrawCash dataRepository
            SepaTransfer = sepaTransfer dataRepository
        }
        
        




            
            
                

            

