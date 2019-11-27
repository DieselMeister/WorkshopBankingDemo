module Domain
    
    // DDD Value Types
    module DataTypes =

        type AccountId = private UserId of string
        module AccountId =
            
            let create uid =
                UserId uid

            let value (UserId id) = id


        type Money = private Money of decimal
        module Money =
            
            let create m =
                Money m

            let value (Money m) = m

            let add (Money a) (Money b) = Money (a + b)

            let subtract (Money a) (Money b) = Money (a - b)


    module TransactionArgs =
        
        open DataTypes

        type SepaTransactionArg = {
            SourceAccount:AccountId            
            TargetAccount:AccountId
            Amount:Money
        }

    open DataTypes
    
    type Transaction =
        | CashDeposit of {| AccountId:AccountId; Amount:Money |}
        | SepaTransaction of TransactionArgs.SepaTransactionArg
        | CashWithdrawn of {| AccountId:AccountId; Amount:Money |}


    // State
    type BankAccount = {
        AccountId:AccountId
        Amount:Money
        Transactions: Transaction list
    }


    
    let processTransaction bankaccount transaction =
        match transaction with
        | CashDeposit cd ->
            if cd.AccountId <> bankaccount.AccountId then
                Error "invalid bank account for this transaction"
            else
                let newAmount = Money.add bankaccount.Amount cd.Amount
                {
                    bankaccount with
                        Amount = newAmount
                        Transactions = transaction :: bankaccount.Transactions
                } |> Ok

        | CashWithdrawn cw ->
            if cw.AccountId <> bankaccount.AccountId then
                Error "invalid bank account for this transaction"
            else
                let newAmount = Money.subtract bankaccount.Amount cw.Amount
                if (Money.value newAmount) < 0.0m then
                    Error "insufficent money on you account!"
                else
                    {
                        bankaccount with
                            Amount = newAmount
                            Transactions = transaction :: bankaccount.Transactions
                    } |> Ok
        | SepaTransaction st ->
            match st.SourceAccount, st.TargetAccount with
            | s,t when s = bankaccount.AccountId && t = bankaccount.AccountId ->
                Error "you can not transfer money to yourself"
            | s,_ when s = bankaccount.AccountId ->
                // here we need to remove the amount from the bank account
                // refactor later while code duplication
                let newAmount = Money.subtract bankaccount.Amount st.Amount
                if (Money.value newAmount) < 0.0m then
                    Error "insufficent money on you account!"
                else
                    {
                        bankaccount with
                            Amount = newAmount
                            Transactions = transaction :: bankaccount.Transactions
                    } |> Ok
            | _,t when t = bankaccount.AccountId ->
                // here web need to depoit the amount from the transaction
                let newAmount = Money.add bankaccount.Amount st.Amount
                {
                    bankaccount with
                        Amount = newAmount
                        Transactions = transaction :: bankaccount.Transactions
                } |> Ok
            | _,_ ->
                Error "invalid acont id for source and target"

                
                





