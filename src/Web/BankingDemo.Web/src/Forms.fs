module Forms

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Model
open BankingDemo.Dtos
open Fable.Import

let logo : React.ReactElement list =
    [
        pre [ Style [ FontFamily "monospace" ] ]  [
            str """
         ___     _     _  _   _  __         ___          _____   ___    ___    _  _     ___    __     __     __  
        | _ )   /_\   | \| | | |/ /  ___   / _ \   ___  |_   _| | _ \  / _ \  | \| |   |_  )  /  \   /  \   /  \ 
        | _ \  / _ \  | .` | | ' <  |___| | (_) | |___|   | |   |   / | (_) | | .` |    / /  | () | | () | | () |
        |___/ /_/ \_\ |_|\_| |_|\_\        \___/          |_|   |_|_\  \___/  |_|\_|   /___|  \__/   \__/   \__/ 
            
                                     _____           _                                  _   
                                    |_   _|         | |                                | |  
                                      | |    _ __   | |_    ___   _ __   _ __     ___  | |_ 
                                      | |   | '_ \  | __|  / _ \ | '__| | '_ \   / _ \ | __|
                                     _| |_  | | | | | |_  |  __/ | |    | | | | |  __/ | |_ 
                                    |_____| |_| |_|  \__|  \___| |_|    |_| |_|  \___|  \__|

            """
        ]
    ]


let mainWindow content =
    div [ ClassName "notification" ] [
        button [ ClassName "delete" ] [ ]
        h1 [] [ str "Bank-o-tron 2000" ]
        
        yield! logo

        yield! content
    ]


let formWindow title content =
    div [ ClassName "notification" ] [
        button [ ClassName "delete" ] [ ]
        yield! content
    ]


let errorMessage dispatch okMessage text =
    formWindow "Error!" [
        p [ ] [ 
            str text
        ]

        button [
            OnClick (fun e -> dispatch okMessage)
        ] [
            str "OK"
        ]
    ]


let isLoading () =
    formWindow "Loading..." [
        p [] [ 
            str "Wait until action is finished ..."
        ]
    ]


let loginForm (model:Model.Model) dispatch =
    formWindow "Login" [
        label [ ] [ 
            Text "Account Id:"            
        ]

        input [
            Type "text"
            Value model.AccountId
            OnChange (fun text -> dispatch (LoginAccountIdUpdate text.Value))
        ]

        button [
            OnClick (fun e -> dispatch LogIn)
        ] [
            str "Login"
            
        ]
    ]
    


let mainSite model dispatch =
    formWindow "Account Overview" [
        match model.AccountData with
        | None ->
            ()

        | Some accountData ->
            yield p [ ] [
                str (sprintf "Account ID: %s" accountData.AccountId)
            ]

            yield p [ ] [
                str (sprintf "Amount: %M" accountData.Amount)
            ]

            yield button [
                OnClick (fun e -> dispatch GotoCashDepositForm)

            ] [
                str "Deposit Cash"
            ]

            yield button [
                OnClick (fun e -> dispatch GotoCashWithdrawnForm)

            ] [
                str "Withdraw Cash"
            ]

            yield button [
                OnClick (fun e -> dispatch GotoSepaTransactionForm)

            ] [
                str "Send Sepa Transfer"
            ]


            let convertItem (item:ITransaction) =
                match item with
                | :? CashDeposit as cd ->
                    "Cash: Deposit",
                    sprintf "  % 7.2M" cd.Amount,
                    "Green"

                | :? CashWithdrawn as cw ->
                    "Cash: Withdraw",
                    sprintf "- % 7.2M" cw.Amount,
                    "Red"
                    
                | :? SepaTransaction as st ->
                    
                    if (st.SourceAccount = accountData.AccountId) then
                        sprintf "Sepa Outgoing: to %s" st.TargetAccount,
                        sprintf "- % 7.2M" st.Amount,
                        "Red"
                    else
                        sprintf "Sepa Incomming: from %s" st.SourceAccount,
                        sprintf "  % 7.2M" st.Amount,
                        "Green"
                | _ ->
                    "invalid Transaction!",
                    "",
                    "Red"


            yield table [
                ClassName "table"
                Style [
                    Width "100%"
                ]
                
            ] [
                tbody [] [
                    for (item) in accountData.Transactions do
                        let (text,amount,color) = convertItem item
                        yield tr [] [
                            td [] [str text]
                            td [
                                Style [
                                    Color color
                                ]
                            ] [str (amount + " €") ]
                        ]
                ]
            ]
            
    ]


let connecting model dispatch =
    formWindow "Connecting ..." [
        p [] [ 
            str "Connecting to Server ... Please wait ..."
        ]
    ]


let depositCash (model:Model.Model) dispatch =
    formWindow "Deposit Cash" [
        label [ ] [ 
            Text "How much do you want to deposit into you account?"            
        ]

        input [
            Type "text"
            Value model.CashDepositForm.Amount
            OnChange (fun text -> dispatch (CashDepositChangeAmount text.Value))
        ]

        button [
            OnClick (fun e -> dispatch SendCashDeposit)
        ] [
            str "Send"
        ]
        
    ]


let withdrawCash (model:Model.Model) dispatch =
    formWindow "Withdraw Cash" [
        label [ ] [ 
            Text "How much do you want to withdraw from your account?"            
        ]

        input [
            Type "text"
            Value model.CashWithdrawnFrom.Amount
            OnChange (fun text -> dispatch (CashWithdrawChangeAmount text.Value))
        ]

        button [
            OnClick (fun e -> dispatch SendCashWithdraw)
        ] [
            str "Send"
        ]
        
    ]


let sepaTransfer (model:Model.Model) dispatch =
    formWindow "Sepa Transfer" [

        label [ ] [ 
            Text "Enter the target account id for the sepa transfer."            
        ]

        input [
            Type "text"
            Value model.SepaTransactionForm.TargetAccount
            OnChange (fun text -> dispatch (SepaTransferChangeTargetAccount text.Value))
        ]

        label [ ] [ 
            Text "Amount:"            
        ]

        input [
            Type "text"
            Value model.SepaTransactionForm.Amount
            OnChange (fun text -> dispatch (SepaTransferChangeAmount text.Value))
        ]

        button [
            OnClick (fun e -> dispatch SendSepaTransfer)
        ] [
            str "Send"
        ]

    ]
    
    

