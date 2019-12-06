module Forms

open Terminal.Gui.Elmish
open Model
open Dtos

let logo : Terminal.Gui.View list =
    [
        let labelStyle y = 
            Styles [ Pos (CenterPos,AbsPos y) ] 

        label [ labelStyle 0; Text " ___     _     _  _   _  __         ___          _____   ___    ___    _  _     ___    __     __     __  " ]
        label [ labelStyle 1; Text "| _ )   /_\   | \| | | |/ /  ___   / _ \   ___  |_   _| | _ \  / _ \  | \| |   |_  )  /  \   /  \   /  \ " ]
        label [ labelStyle 2; Text "| _ \  / _ \  | .` | | ' <  |___| | (_) | |___|   | |   |   / | (_) | | .` |    / /  | () | | () | | () |" ]
        label [ labelStyle 3; Text "|___/ /_/ \_\ |_|\_| |_|\_\        \___/          |_|   |_|_\  \___/  |_|\_|   /___|  \__/   \__/   \__/ " ]
    ]


let mainWindow content =
    window [
        Title "Bank-o-tron 2000"
        Styles [
            Pos (Position.AbsPos 0,Position.AbsPos 0)
            Dim (Dimension.Fill, Dimension.Fill)
        ]
        
    ] [
        yield! logo

        yield! content
    ]


let formWindow title content =
    window [
        Title title
        Styles [
            Pos (CenterPos,CenterPos)
            Dim (FillMargin 8,FillMargin 10)
        ]
    ] [
        yield! content
    ]


let errorMessage dispatch okMessage text =
    formWindow "Error!" [
        label [ 
            Text text
            Styles [
                Pos (AbsPos 2,AbsPos 2)
                Colors (Terminal.Gui.Color.Red,Terminal.Gui.Color.BrightYellow)
            ]
        ]

        button [
            Styles [
                Pos (CenterPos,AbsPos 5)
            ]
            Text "Ok"
            OnClicked (fun () -> dispatch okMessage)
        ]
    ]


let isLoading () =
    formWindow "Loading..." [
        label [ 
            Text "Wait until action is finished ..."
            Styles [
                Pos (AbsPos 2,AbsPos 2)
                Colors (Terminal.Gui.Color.Red,Terminal.Gui.Color.BrightYellow)
            ]
        ]

        
    ]


let loginForm (model:Model.Model) dispatch =
    formWindow "Login" [
        label [ 
            Text "Account Id:"
            Styles [
                Pos (AbsPos 2,AbsPos 2)
            ]
        ]

        textField [
            Styles [
                Pos (AbsPos 2,AbsPos 3)
                Dim (FillMargin 2,Dimension.AbsDim 1)
            ]
            Value model.AccountId
            OnChanged (fun text -> dispatch (LoginAccountIdUpdate text))
        ]

        button [
            Styles [
                Pos (CenterPos,AbsPos 5)
            ]
            Text "Login"
            OnClicked (fun () -> dispatch LogIn)
        ]
    ]
    


let mainSite model dispatch =
    formWindow "Account Overview" [
        match model.AccountData with
        | None ->
            ()

        | Some accountData ->
            label [
                Styles [
                    Pos(AbsPos 2,AbsPos 1)
                ]
                Text (sprintf "Account ID: %s" accountData.AccountId)
            ]

            label [
                Styles [
                    Pos(AbsPos 2,AbsPos 2)
                ]
                Text (sprintf "Amount: %M" accountData.Amount)
            ]

            button [
                Styles [
                    Pos(AbsPos 3,AbsPos 3)
                ]
                Text "Deposit Cash"
                OnClicked (fun () -> dispatch GotoCashDepositForm)
            ]

            button [
                Styles [
                    Pos(AbsPos 20,AbsPos 3)
                ]
                Text "Withdraw Cash"
                OnClicked (fun () -> dispatch GotoCashWithdrawnForm)
            ]

            button [
                Styles [
                    Pos(AbsPos 40,AbsPos 3)
                ]
                Text "Send Sepa Transfer"
                OnClicked (fun () -> dispatch GotoSepaTransactionForm)
            ]


            let convertItem (item:ITransaction) =
                match item with
                | :? CashDeposit as cd ->
                    "Cash: Deposit",
                    sprintf "  % 7.2M" cd.Amount,
                    Terminal.Gui.Color.BrightGreen

                | :? CashWithdrawn as cw ->
                    "Cash: Withdraw",
                    sprintf "- % 7.2M" cw.Amount,
                    Terminal.Gui.Color.BrightRed
                    
                | :? SepaTransaction as st ->
                    
                    if (st.SourceAccount = accountData.AccountId) then
                        sprintf "Sepa Outgoing: to %s" st.TargetAccount,
                        sprintf "- % 7.2M" st.Amount,
                        Terminal.Gui.Color.BrightRed
                    else
                        sprintf "Sepa Incomming: from %s" st.SourceAccount,
                        sprintf "  % 7.2M" st.Amount,
                        Terminal.Gui.Color.BrightGreen
                | _ ->
                    "invalid Transaction!",
                    "",
                    Terminal.Gui.Color.BrightRed


            for (idx,item) in accountData.Transactions |> List.indexed do
                label [
                    Styles [
                        Pos (AbsPos 2, AbsPos (5 + idx))
                    ]
                    let (text,_,_) = convertItem item
                    Text text
                ]


                label [

                    let (_,amount,color) = convertItem item

                    Styles [
                        Pos (AbsPos 40, AbsPos (5 + idx))
                        Colors (color,Terminal.Gui.Color.BrightBlue)
                    ]
                    Text amount
                ]
    ]


let connecting model dispatch =
    formWindow "Connecting ..." [
        label [
            Styles [
                Pos (CenterPos,CenterPos)
                Colors (Terminal.Gui.Color.BrightYellow,Terminal.Gui.Color.Blue)
            ]
            
            Text "Connecting to Server ... Please wait ..."
        ]
    ]


let depositCash (model:Model.Model) dispatch =
    formWindow "Deposit Cash" [
        label [ 
            Text "How much do you want to deposit into you account?"
            Styles [
                Pos (AbsPos 2,AbsPos 2)
            ]
        ]

        textField [
            Styles [
                Pos (AbsPos 2,AbsPos 3)
                Dim (FillMargin 2,Dimension.AbsDim 1)
            ]
            Value model.CashDepositForm.Amount
            OnChanged (fun text -> dispatch (CashDepositChangeAmount text))
        ]

        button [
            Styles [
                Pos (CenterPos,AbsPos 5)
            ]
            Text "Send"
            OnClicked (fun () -> dispatch SendCashDeposit)
        ]
    ]


let withdrawCash (model:Model.Model) dispatch =
    formWindow "Withdraw Cash" [
        label [ 
            Text "How much do you want to withdraw from your account?"
            Styles [
                Pos (AbsPos 2,AbsPos 2)
            ]
        ]

        textField [
            Styles [
                Pos (AbsPos 2,AbsPos 3)
                Dim (FillMargin 2,Dimension.AbsDim 1)
            ]
            Value model.CashWithdrawnFrom.Amount
            OnChanged (fun text -> dispatch (CashWithdrawChangeAmount text))
        ]

        button [
            Styles [
                Pos (CenterPos,AbsPos 5)
            ]
            Text "Send"
            OnClicked (fun () -> dispatch SendCashWithdraw)
        ]
    ]


let sepaTransfer (model:Model.Model) dispatch =
    formWindow "Sepa Transfer" [
        label [ 
            Text "Enter the target account id for the sepa transfer."
            Styles [
                Pos (AbsPos 2,AbsPos 2)
            ]
        ]

        textField [
            Styles [
                Pos (AbsPos 2,AbsPos 3)
                Dim (FillMargin 2,Dimension.AbsDim 1)
            ]
            Value model.SepaTransactionForm.TargetAccount
            OnChanged (fun text -> dispatch (SepaTransferChangeTargetAccount text))
        ]

        label [ 
            Text "Amount:"
            Styles [
                Pos (AbsPos 2,AbsPos 5)
            ]
        ]

        textField [
            Styles [
                Pos (AbsPos 2,AbsPos 6)
                Dim (FillMargin 2,Dimension.AbsDim 1)
            ]
            Value model.SepaTransactionForm.Amount
            OnChanged (fun text -> dispatch (SepaTransferChangeAmount text))
        ]

        button [
            Styles [
                Pos (CenterPos,AbsPos 8)
            ]
            Text "Send"
            OnClicked (fun () -> dispatch SendSepaTransfer)
        ]


        label [ 
            Text "Available Accounts:"
            Styles [
                Pos (CenterPos,AbsPos 10)
            ]
        ]


        for (idx,item) in model.AllAccountIds |> List.indexed do
            label [ 
                Text item
                Styles [
                    Pos (CenterPos,AbsPos (12 + idx))
                ]
            ]
    ]
    
    

