module Services



    open Domain
    open Domain.DataTypes
    

    let depositCash getAccount storeAccount accountId amount =
        let accountId = AccountId.create accountId
        
        let cashDeposit =
            {| 
                AccountId = accountId
                Amount = Money.create amount
            |} |> CashDeposit

        // load account or create an empty one
        let currentAccount =
            getAccount accountId
            |> Option.map (DataAccess.Dto.bankAccountFromDto)
            |> Option.defaultValue {
                AccountId = accountId
                Amount = Money.create 0.0m
                Transactions = []
            }
        
        let newAccountResult = processTransaction currentAccount cashDeposit
        match newAccountResult with
        | Ok newAccount ->
            storeAccount newAccount
            Ok ()
        | Error e ->
            Error e


    let withdrawCash getAccount storeAccount accountId amount =
        let accountId = AccountId.create accountId
        
        let cashWithdraw =
            {| 
                AccountId = accountId
                Amount = Money.create amount
            |} |> CashWithdrawn

        let currentAccount =
            getAccount accountId
            |> Option.map (DataAccess.Dto.bankAccountFromDto)

        match currentAccount with
        | Some account ->
            let newAccountResult = processTransaction account cashWithdraw
            match newAccountResult with
            | Ok newAccount ->
                storeAccount newAccount
                Ok ()
            | Error e ->
                Error e

        | None ->
            Error "insufficent funds"


    let sepaTransfer getAccount storeAccount sourceAccount targetAccount amount =
        let sourceAccount = AccountId.create sourceAccount
        let targetAccount = AccountId.create targetAccount
        
        let sepaTransfer = SepaTransaction { 
                SourceAccount = sourceAccount
                TargetAccount = targetAccount
                Amount = Money.create amount
            }

        let sourceAccount =
            getAccount sourceAccount
            |> Option.map (DataAccess.Dto.bankAccountFromDto)

        // load target account or create empty, because an empty can get funds
        let targetAccount =
            getAccount targetAccount
            |> Option.map (DataAccess.Dto.bankAccountFromDto)
            |> Option.defaultValue (
                {
                    AccountId = targetAccount
                    Amount = Money.create 0.0m
                    Transactions = []
                }
            )

        match sourceAccount with
        | None ->
            Error "insufficent funds"
        | Some sourceAccount ->
            let newSourceAccount = processTransaction sourceAccount sepaTransfer

            let newTargetAccount = processTransaction targetAccount sepaTransfer

            match newSourceAccount,newTargetAccount with
            | Ok sa, Ok ta ->
                // save both
                storeAccount sa
                storeAccount ta
                Ok ()
            | Error se, Error ta ->
                Error (sprintf "%s / %s" se ta)
            | Error se, _ ->
                Error se
            | _, Error ta ->
                Error ta


    type BankAccountService = {
        DepositCash: string -> decimal -> Result<unit,string>
        WithdrawCash: string -> decimal -> Result<unit,string>
        SepaTransfer: string -> string -> decimal -> Result<unit,string>
    }

    let createBackAccountService getAccount storeAccount =
        
        let getAccount =
            (fun id -> AccountId.value id |> getAccount)
        {
            DepositCash = depositCash getAccount storeAccount
            WithdrawCash = withdrawCash getAccount storeAccount
            SepaTransfer = sepaTransfer getAccount storeAccount
        }




            
            
                

            

