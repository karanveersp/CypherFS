open System
open CypherCLI.Fs
open CypherCLI.Fs.Model

[<EntryPoint>]
let main args =
    let appDataDir = if Array.isEmpty args then Util.GetAppDataDir()
                     else args.[0]
    let mutable finished = false
    let mutable itemsMap = Util.MapFromFiles(appDataDir)
//    Console.Clear()

    while not finished do
        if itemsMap.IsEmpty then
            // prompt for creating new item
            printfn "No cypher files exist."

            let createNewItem =
                Util.YesNoPrompt "Create a new entry?" false

            if createNewItem then
                itemsMap <- Util.CreateHandler appDataDir itemsMap
            else
                finished <- true
        else
            let selectedAction = Util.GetUserAction()

            match selectedAction with
            | Exit ->
                let clearConsole = Util.YesNoPrompt "Clear console?" true
                if clearConsole then Console.Clear()
                finished <- true
            | _ ->
                itemsMap <- Util.ProcessAction appDataDir itemsMap selectedAction
                let clearConsole = Util.YesNoPrompt "Clear console?" true
                if clearConsole then Console.Clear()

    0
