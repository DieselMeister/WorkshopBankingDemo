// Learn more about F# at http://fsharp.org

open Terminal.Gui.Elmish
open Model



let init () =
    let model = {
        AccountId = ""
        AllAccountIds = []
        AccountData = None
        CurrentForm= LoginForm
        SepaTransactionForm = SepaTransaction.Empty
        CashDepositForm = CashTransaction.Empty
        CashWithdrawnFrom = CashTransaction.Empty
        IsLoading = false
    }
    model, Cmd.none





let update msg (model:Model) =
    match msg with
    | LogIn  ->
        // open SignalR connection
        let cmdOpenSignalR = Commands.openSignalRConnectionCmd model.AccountId
        
        model, Cmd.batch [ Cmd.ofMsg GotoConnectionForm; cmdOpenSignalR ]

    | Connected ->
        let cmdLoadAccountData = Commands.getAccountCmd model.AccountId
        { model with IsLoading = true }, Cmd.batch [ cmdLoadAccountData; Cmd.ofMsg GotoMainForm ]

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
        { model with AccountData = Some accountData; IsLoading = false }, Cmd.none

    | AccountDataUpdatedFromRemote accountData ->
        { model with AccountData = Some accountData }, Cmd.none

    | LoginAccountIdUpdate accountId ->
        { model with AccountId = accountId }, Cmd.none


    | CashDepositChangeAmount amount ->
        { model with CashDepositForm = { model.CashDepositForm with Amount = amount }}, Cmd.none
    | CashWithdrawChangeAmount amount ->
        { model with CashWithdrawnFrom = { model.CashWithdrawnFrom with Amount = amount }}, Cmd.none
    | SepaTransferChangeAmount amount ->
        { model with SepaTransactionForm = { model.SepaTransactionForm with Amount = amount }}, Cmd.none
    | SepaTransferChangeTargetAccount accountId ->
        { model with SepaTransactionForm = { model.SepaTransactionForm with TargetAccount = accountId }}, Cmd.none


    | SendCashDeposit ->
        
        let (isValid,amount) = System.Decimal.TryParse(model.CashDepositForm.Amount)
        if not isValid then
            model, Cmd.ofMsg (OnError "invalid amount - not a number!")
        else
            let data:Dtos.CashDeposit = {
                AccountId = model.AccountId
                Amount = amount
            }

            { model with IsLoading = true}, Commands.depositCashCmd data
    | SendCashWithdraw ->
        let (isValid,amount) = System.Decimal.TryParse(model.CashWithdrawnFrom.Amount)
        if not isValid then
            model, Cmd.ofMsg (OnError "invalid amount - not a number!")
        else
            let data:Dtos.CashWithdrawn = {
                AccountId = model.AccountId
                Amount = amount
            }

            { model with IsLoading = true}, Commands.withdrawCashCmd data
    | SendSepaTransfer ->
        let (isValid,amount) = System.Decimal.TryParse(model.SepaTransactionForm.Amount)
        if not isValid then
            model, Cmd.ofMsg (OnError "sepa: invalid amount - not a number!")
        else
            let data:Dtos.SepaTransaction = {
                SourceAccount = model.AccountId
                TargetAccount = model.SepaTransactionForm.TargetAccount
                Amount = amount
            }
            { model with IsLoading = true}, Commands.sepaTransferCmd data

    | CashDepositSend ->
        { model with CashDepositForm = CashTransaction.Empty; IsLoading = false }, Cmd.ofMsg GotoMainForm

    | CashWithdrawSend ->
        { model with CashWithdrawnFrom = CashTransaction.Empty; IsLoading = false }, Cmd.ofMsg GotoMainForm

    | SepaTransferSend ->
        { model with SepaTransactionForm = SepaTransaction.Empty; IsLoading = false }, Cmd.ofMsg GotoMainForm

    | OnError msg ->
        messageBox 60 10 "Error" msg [ "OK" ] |> ignore
        { model with IsLoading = false} ,Cmd.none




let view model dispatch =
    page [
        
        Forms.mainWindow [
            if model.IsLoading then
                Forms.isLoading ()
            else
                match model.CurrentForm with
                | LoginForm ->
                    Forms.loginForm model dispatch
                | ConnectionForm ->
                    Forms.connecting model dispatch
                | MainForm ->
                    Forms.mainSite model dispatch
                | SepaTransactionForm ->
                    Forms.sepaTransfer model dispatch
                | CashDepositForm ->
                    Forms.depositCash model dispatch
                | CashWithdrawnForm ->
                    Forms.withdrawCash model dispatch
        ]
    ]







open System.Globalization


[<EntryPoint>]
let main argv =

    let cultureInfo = new CultureInfo("en-US");
    cultureInfo.NumberFormat.CurrencySymbol <- "€";
    
    CultureInfo.DefaultThreadCurrentCulture <- cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture <- cultureInfo;

    Program.mkProgram init update view
    |> Program.withTrace (fun msg model -> 
        let text = sprintf "Msg: %A - Model: %A" msg model 

        System.Diagnostics.Debug.WriteLine(text)
    )
    |> Program.run
    0 // return an integer exit code
