module Forms

open Terminal.Gui.Elmish
open Model
open DataAccess.Dto


let formWindow title content =
    window [
        Title title
        Styles [
            Pos (CenterPos,CenterPos)
            Dim (FillMargin 3,FillMargin 3)
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
                    Pos(AbsPos 2,AbsPos 2)
                ]
                Text (sprintf "Amount: %A" accountData.Amount)
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
                    sprintf "  % 7M" cd.Amount,
                    Terminal.Gui.Color.BrightGreen

                | :? CashWithdrawn as cw ->
                    "Cash: Withdraw",
                    sprintf "- % 7M" cw.Amount,
                    Terminal.Gui.Color.BrightRed
                    
                | :? SepaTransaction as st ->
                    
                    if (st.SourceAccount = accountData.AccountId) then
                        sprintf "Sepa Outgoing: to %s" st.TargetAccount,
                        sprintf "  % 7M" st.Amount,
                        Terminal.Gui.Color.BrightGreen
                    else
                        sprintf "Sepa Incomming: from %s" st.SourceAccount,
                        sprintf "- % 7M" st.Amount,
                        Terminal.Gui.Color.BrightRed
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
            Text "Amount in Euro"
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
            Text "Target Account"
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
    
    

