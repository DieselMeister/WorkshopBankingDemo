// Learn more about F# at http://fsharp.org

open Terminal.Gui.Elmish
open Model





let init () =
    let model = {
        AccountId = ""
        AllAccountIds = []
        AccountData = None
        CurrentForm= LoginForm
        SepaTransactionForm = None
        CashDepositForm = None
        CashWithdrawnFrom = None
    }
    model, Cmd.none





let update msg (model:Model) =
    match msg with
    | LogIn  ->
        // open SignalR connection
        let cmd = Commands.openSignalRConnectionCmd model.AccountId
        model, cmd

    // Goto's

    | GotoMainForm ->
        { model with CurrentForm = MainForm },Cmd.none

    | GotoConnectionForm ->
        let newModel = { model with CurrentForm = ConnectionForm }
        newModel, Cmd.none
    | GotoCashDepositForm ->
        let newModel = { model with CurrentForm = CashDepositForm }
        newModel, Cmd.none
    | GotoCashWithdrawnForm ->
        let newModel = { model with CurrentForm = CashWithdrawnForm }
        newModel, Cmd.none
    | GotoSepaTransactionForm ->
        let newModel = { model with CurrentForm = SepaTransactionForm }
        newModel, Cmd.none


    // Data Update Operations

    | AllAccountsUpdates accountIds ->
        { model with AllAccountIds = accountIds }, Cmd.none

    | AccountDataUpdated accountData ->
        { model with AccountData = accountData }, Cmd.none

    | LoginAccountIdUpdate accountId ->
        { model with AccountId = accountId }, Cmd.none




let view model dispatch =
    page [
        
        window [
            Title "Banking App 2000"
            Styles [
                Pos (Position.AbsPos 0,Position.AbsPos 0)
                Dim (Dimension.Fill, Dimension.Fill)
            ]
            
        ] [
            
            match model.CurrentForm with
            | LoginForm ->
                Forms.loginForm model dispatch
            | ConnectionForm ->
                label [ Text "Connection Please Wait" ]
            | MainForm ->
                Forms.mainSite model dispatch
            | SepaTransactionForm ->
                label [ Text "SepaTransactionForm" ]
            | CashDepositForm ->
                label [ Text "CashDepositForm" ]
            | CashWithdrawnForm ->
                label [ Text "CashWithdrawnForm" ]
            

        ]
    ]









[<EntryPoint>]
let main argv =
    Program.mkProgram init update view
    |> Program.run
    0 // return an integer exit code
