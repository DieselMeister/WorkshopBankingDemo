module Forms

open Terminal.Gui.Elmish
open Model


let loginForm model dispatch =
    window [
        Title "Login"
        Styles [
            Pos (AbsPos 5,AbsPos 5)
            Dim (FillMargin 5,FillMargin 5)
        ]
    ] [
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
    window [
        Title "Login"
        Styles [
            Pos (AbsPos 5,AbsPos 5)
            Dim (FillMargin 5,FillMargin 5)
        ]
    ] [
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



            for item in accountData.Transactions do
                label [
                    
                    Text (sprintf "Transaction: %s" item.EventName)
                ]

    ]
    

