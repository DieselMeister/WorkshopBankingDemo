module Services



    open Domain
    open Domain.DataTypes
    open Domain.TransactionArgs
    open DataAccess
    open FSharp.Control.Tasks.V2
    open System.Threading.Tasks
    open Common.MoandicTaskHelper
    

    
    
    let private getCurrentAccount dataRepository accountId =
        task {

            let! currentAccount =
                dataRepository.GetAccount accountId
            
            return 
                currentAccount
                |> Option.map (Dtos.bankAccountFromDto)
        }


    let private getCurrentAccountV2 dataRepository accountId =
        accountId
        |> dataRepository.GetAccount
        |> TaskOption.map (Dtos.bankAccountFromDto)




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


    let private getCurrentAccountOrDefaultV2 dataRepository accountId =
        accountId
        |> getCurrentAccountV2 dataRepository
        |> Task.map (
            fun account ->
                account
                |> Option.defaultValue {
                    AccountId = AccountId.create accountId
                    Amount = Money.create 0.0m
                    Transactions = []
                }
            
        )
        


    let depositCash (dataRepository:DataRepository) (command:Dtos.CashDeposit) =
        task {
           
            
            let cashDeposit = Dtos.transactionFromDto command

            // load account or create an empty one
            let! currentAccount =
                getCurrentAccountOrDefault dataRepository command.AccountId

            
            let newAccountResult = processTransaction cashDeposit currentAccount
            match newAccountResult with
            | Ok newAccount ->
                do! dataRepository.StoreAccount newAccount
                return Ok ()
            | Error e ->
                return Error e
        }


    /// monadic version of depositCash
    let depositCashV2 (dataRepository:DataRepository) (command:Dtos.CashDeposit) =
        command.AccountId
        |> getCurrentAccountOrDefault dataRepository
        |> Task.map (fun account -> Dtos.transactionFromDto command, account) 
        |> Task.map (fun (cashDeposit,account) -> processTransaction cashDeposit account)
        |> TaskResult.bind (fun newAccount -> dataRepository.StoreAccount newAccount)


    /// more understandable monadic version of depositCash
    let depositCashV3 (dataRepository:DataRepository) (command:Dtos.CashDeposit) =

        let mapCommandToTransaction account =
            account 
            |> Task.map (fun account -> Dtos.transactionFromDto command, account)

        let processTransaction input =
            input 
            |> Task.map (fun (cashDeposit,account) -> Domain.processTransaction cashDeposit account)

        let storeTransaction newTransaction =
            newTransaction
            |> TaskResult.bind (fun newAccount -> dataRepository.StoreAccount newAccount)

        command.AccountId
        |> getCurrentAccountOrDefault dataRepository
        |> mapCommandToTransaction
        |> processTransaction
        |> storeTransaction


    let withdrawCash (dataRepository:DataRepository) (command:Dtos.CashWithdrawn) =
        task {
            
            let cashWithdraw = Dtos.transactionFromDto command

            let! currentAccount =
                getCurrentAccount dataRepository command.AccountId
            

            match currentAccount with
            | Some account ->
                let newAccountResult = processTransaction cashWithdraw account
                match newAccountResult with
                | Ok newAccount ->
                    do! dataRepository.StoreAccount newAccount
                    return Ok ()
                | Error e ->
                    return Error e

            | None ->
                return Error "insufficent funds"
        }


    /// monadic version of depositCash
    let withdrawCashV2 (dataRepository:DataRepository) (command:Dtos.CashWithdrawn) =
        command.AccountId
        |> getCurrentAccount dataRepository
        |> TaskOption.map (fun account -> Dtos.transactionFromDto command, account)
        |> TaskOption.map (fun (cashWithdraw,account) -> processTransaction cashWithdraw account)
        |> Task.map (Option.defaultValue (Error "insufficent funds"))
        |> TaskResult.bind (fun newAccount -> dataRepository.StoreAccount newAccount)
        

    /// more understandable monadic version of depositCash
    let withdrawCashV3 (dataRepository:DataRepository) (command:Dtos.CashWithdrawn) =

        let mapCommandToTransaction account =
            account 
            |> TaskOption.map (fun account -> Dtos.transactionFromDto command, account)

        let processTransaction input =
            input 
            |> TaskOption.map (fun (cashWithdraw,account) -> Domain.processTransaction cashWithdraw account)

        let storeTransaction newTransaction =
            newTransaction
            |> TaskResult.bind (fun newAccount -> dataRepository.StoreAccount newAccount)

        let leaveWithInsufficentFundsIfNoAccountExists result =
            result 
            |> Task.map (Option.defaultValue (Error "insufficent funds"))


        command.AccountId
        |> getCurrentAccount dataRepository
        |> mapCommandToTransaction
        |> processTransaction
        |> leaveWithInsufficentFundsIfNoAccountExists
        |> storeTransaction


    let sepaTransfer (dataRepository:DataRepository) (command:Dtos.SepaTransaction) =
        task {
            
            let sepaTransfer = Dtos.transactionFromDto command

            let! sourceAccount =
                getCurrentAccount dataRepository command.SourceAccount

            // load target account or create empty, because an empty can get funds
            let! targetAccount =
                getCurrentAccountOrDefault dataRepository command.TargetAccount
            

            match sourceAccount with
            | None ->
                return Error "insufficent funds"
            | Some sourceAccount ->
                let newSourceAccount = processTransaction sepaTransfer sourceAccount
                let newTargetAccount = processTransaction sepaTransfer targetAccount

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


    let sepaTransferV2 (dataRepository:DataRepository) (command:Dtos.SepaTransaction) =
        let newSourceAccount =
            command.SourceAccount
            |> getCurrentAccount dataRepository
            |> TaskOption.map (fun sourceAccount -> Dtos.transactionFromDto command, sourceAccount)
            |> TaskOption.map (fun (sepaTransaction,sourceAccount) -> processTransaction sepaTransaction sourceAccount)
            |> Task.map (Option.defaultValue (Error "insufficent funds"))

        let newTargetAccount =
            command.TargetAccount
            |> getCurrentAccountOrDefault dataRepository
            |> Task.map (fun targetAccount -> Dtos.transactionFromDto command, targetAccount) 
            |> Task.map (fun (sepaTransaction,targetAccount) -> processTransaction sepaTransaction targetAccount)

        (newSourceAccount,newTargetAccount)
        |> TaskResult.map2 (fun (newSourceAccount,newTargetAccount) ->
            Task.WhenAll [
                newSourceAccount |> dataRepository.StoreAccount
                newTargetAccount |> dataRepository.StoreAccount
            ] |> ignore
            ()
        )


    let sepaTransferV3 (dataRepository:DataRepository) (command:Dtos.SepaTransaction) =

        let mapCommandToTransaction account =
            account 
            |> Task.map (fun account -> Dtos.transactionFromDto command, account)

        let mapCommandToTransactionOption account =
            account 
            |> TaskOption.map (fun account -> Dtos.transactionFromDto command, account)

        
        let processTransaction input =
            input 
            |> Task.map (fun (cashWithdraw,account) -> Domain.processTransaction cashWithdraw account)


        let processTransactionOption input =
            input 
            |> TaskOption.map (fun (sepaTransaction,sourceAccount) -> Domain.processTransaction sepaTransaction sourceAccount)


        let leaveWithInsufficentFundsIfNoAccountExists result =
            result 
            |> Task.map (Option.defaultValue (Error "insufficent funds"))


        let storeAccounts (a1,a2) =
            (a1,a2)
            |> TaskResult.map2 (
                fun (newSourceAccount,newTargetAccount) ->
                    Task.WhenAll [
                        newSourceAccount |> dataRepository.StoreAccount
                        newTargetAccount |> dataRepository.StoreAccount
                    ] |> ignore
                    ()
            )


        let newSourceAccount =
            command.SourceAccount
            |> getCurrentAccount dataRepository
            |> mapCommandToTransactionOption
            |> processTransactionOption
            |> leaveWithInsufficentFundsIfNoAccountExists

        let newTargetAccount =
            command.TargetAccount
            |> getCurrentAccountOrDefault dataRepository
            |> mapCommandToTransaction
            |> processTransaction

        (newSourceAccount,newTargetAccount)
        |> storeAccounts


    /// evolve remove double functions
    let sepaTransferV4 (dataRepository:DataRepository) (command:Dtos.SepaTransaction) =

        let mapCommandToTransaction f account =
            account 
            |> Task.map (f (fun account -> Dtos.transactionFromDto command, account))

        
        let processTransaction f input =
            input 
            |> Task.map (f (fun (cashWithdraw,account) -> Domain.processTransaction cashWithdraw account))


        let leaveWithInsufficentFundsIfNoAccountExists result =
            result 
            |> Task.map (Option.defaultValue (Error "insufficent funds"))


        let store2Accounts (a1,a2) =
            (a1,a2)
            |> TaskResult.map2 (
                fun (newSourceAccount,newTargetAccount) ->
                    Task.WhenAll [
                        newSourceAccount |> dataRepository.StoreAccount
                        newTargetAccount |> dataRepository.StoreAccount
                    ] |> ignore
                    ()
            )


        let newSourceAccount =
            command.SourceAccount
            |> getCurrentAccount dataRepository
            |> mapCommandToTransaction Option.map
            |> processTransaction Option.map
            |> leaveWithInsufficentFundsIfNoAccountExists

        let newTargetAccount =
            command.TargetAccount
            |> getCurrentAccountOrDefault dataRepository
            |> mapCommandToTransaction id // id is alias fun x -> x
            |> processTransaction id

        (newSourceAccount,newTargetAccount)
        |> store2Accounts
            
         



    type BankAccountService = {
        DepositCash: Dtos.CashDeposit ->  Task<Result<unit,string>>
        WithdrawCash: Dtos.CashWithdrawn -> Task<Result<unit,string>>
        SepaTransfer: Dtos.SepaTransaction -> Task<Result<unit,string>>
    }


    let createBackAccountService (dataRepository:DataRepository) =
        {
            DepositCash = depositCashV3 dataRepository
            WithdrawCash = withdrawCashV3 dataRepository
            SepaTransfer = sepaTransferV4 dataRepository
        }
        
        




            
            
                

            

