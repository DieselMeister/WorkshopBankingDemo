module Model

open DataAccess.Dto


type Form =
    | LoginForm
    | ConnectionForm
    | MainForm
    | SepaTransactionForm
    | CashDepositForm
    | CashWithdrawnForm


type CashTransaction = {
    Amount:string
}
        with static member Empty = { Amount = "" }


type SepaTransaction = {
    TargetAccount:string
    Amount:string
}
        with static member Empty = { Amount = ""; TargetAccount="" }



type Model = {
    AccountId:string
    AllAccountIds:string list
    AccountData:BankAccount option

    CurrentForm: Form

    SepaTransactionForm:SepaTransaction
    CashDepositForm:CashTransaction
    CashWithdrawnFrom:CashTransaction
}


type Msg =
    | LogIn
    | Connected

    | GotoMainForm
    | GotoConnectionForm
    | GotoCashDepositForm
    | GotoCashWithdrawnForm
    | GotoSepaTransactionForm

    | AllAccountsUpdates of accountIds:string list
    | AccountDataUpdated of accountData:BankAccount

    | LoginAccountIdUpdate of accountId:string

    | CashDepositChangeAmount of amount:string
    | CashWithdrawChangeAmount of amount:string
    | SepaTransferChangeAmount of amount:string
    | SepaTransferChangeTargetAccount of accountId:string

    | SendCashDeposit
    | SendCashWithdraw
    | SendSepaTransfer

    | CashDepositSend
    | CashWithdrawSend
    | SepaTransferSend

    | OnError of string 





