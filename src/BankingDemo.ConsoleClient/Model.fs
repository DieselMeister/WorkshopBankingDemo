module Model

open DataAccess.Dto


type Form =
    | LoginForm
    | ConnectionForm
    | MainForm
    | SepaTransactionForm
    | CashDepositForm
    | CashWithdrawnForm


type Model = {
    AccountId:string
    AllAccountIds:string list
    AccountData:BankAccount option

    CurrentForm: Form

    SepaTransactionForm:SepaTransaction option
    CashDepositForm:CashDeposit option
    CashWithdrawnFrom:CashWithdrawn option
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
    | AccountDataUpdated of accountData:BankAccount option

    | LoginAccountIdUpdate of accountId:string

