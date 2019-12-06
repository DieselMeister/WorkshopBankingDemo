module App.View

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open BankingDemo


importAll "../sass/main.sass"

open Fable.Helpers.React
open Fable.Helpers.React.Props

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
        { model with CashDepositForm = CashTransaction.Empty }, Cmd.ofMsg GotoMainForm
    | CashWithdrawSend ->
        { model with CashWithdrawnFrom = CashTransaction.Empty }, Cmd.ofMsg GotoMainForm
    | SepaTransferSend ->
        { model with SepaTransactionForm = SepaTransaction.Empty }, Cmd.ofMsg GotoMainForm

    | OnError msg ->
        Fable.Import.Browser.window.alert msg        
        { model with IsLoading = false} ,Cmd.none




let view model dispatch =
    div [ ]  [
        
        Forms.mainWindow [
            if model.IsLoading then
                yield Forms.isLoading ()
            else
                match model.CurrentForm with
                | LoginForm ->
                    yield  Forms.loginForm model dispatch
                | ConnectionForm ->
                    yield  Forms.connecting model dispatch
                | MainForm ->
                    yield  Forms.mainSite model dispatch
                | SepaTransactionForm ->
                    yield  Forms.sepaTransfer model dispatch
                | CashDepositForm ->
                    yield Forms.depositCash model dispatch
                | CashWithdrawnForm ->
                    yield  Forms.withdrawCash model dispatch
        ]
    ]

open Elmish.React
open Elmish.Debug
open Elmish.HMR

// App
Program.mkProgram init update view
#if DEBUG
|> Program.withDebugger
#endif
|> Program.withReact "elmish-app"
|> Program.run
