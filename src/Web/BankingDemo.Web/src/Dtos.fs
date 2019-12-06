namespace BankingDemo

module Dtos =

    open Thoth.Json
    

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


    let transactionDecoder :Thoth.Json.Decode.Decoder<ITransaction> =
        Decode.object
            (fun get ->
                let t = get.Required.Field "$type" Decode.string
                
                if (t.Contains("CashDeposit")) then
                    let cashDecoder = Decode.Auto.generateDecoder<CashDeposit>()
                    let v = get.Required.Raw cashDecoder
                    v :> ITransaction
                    
                else if (t.Contains("CashWithdrawn")) then
                    let cashwithdrawDecoder = Decode.Auto.generateDecoder<CashWithdrawn>()
                    let v = get.Required.Raw cashwithdrawDecoder
                    v :> ITransaction
                else if (t.Contains("SepaTransaction")) then
                    let sepaDecoder = Decode.Auto.generateDecoder<SepaTransaction>()
                    let v = get.Required.Raw sepaDecoder
                    v :> ITransaction
                else
                   failwith "unknown type" 

            )

    let transactionListDecoder : Thoth.Json.Decode.Decoder<ITransaction list> =
        Decode.object
            (fun get ->
                let t = get.Required.Field "$type" Decode.string                
                let values = get.Required.Field "$values" (Decode.list transactionDecoder)
                values
            )


    open Thoth.Json

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

                static member Decoder : Decode.Decoder<BankAccount> =
                    Decode.object
                        (fun get ->

                            let accountId = get.Required.Field "AccountId" Decode.string
                            let amount = get.Required.Field "Amount" Decode.decimal
                            let transactions =  get.Required.Field "Transactions" transactionListDecoder
                            {
                                AccountId = accountId
                                Amount = amount
                                Transactions = transactions
                            }
                        )

