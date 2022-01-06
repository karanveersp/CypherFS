module CypherCLI.Fs.Util

open System.IO
open AESLib
open Microsoft.FSharp.Collections
open Sharprompt
open System


open CypherCLI.Fs.Model


let ItemFromFile (path: string) : Item =
    let cypher = File.ReadAllText(path)
    let name = Path.GetFileNameWithoutExtension(path)

    { Path = path
      Name = name
      Content = cypher
      Type = Cypher }

let MapFromFiles (appDataDir: string) : Map<string, Item> =
    Directory.GetFiles(appDataDir, "*.txt")
    |> Seq.map ItemFromFile
    |> Seq.map (fun item -> (item.Name, item))
    |> Map.ofSeq

let UnlockItems (key: string) (itemMap: Map<string, Item>) : Map<string, Item> =
    itemMap.Values
    |> Seq.choose
        (fun item ->
            let plaintext =
                LanguageExt.FSharp.ToFSharp(AES.Decrypt(item.Content, key))

            match plaintext with
            | Some v ->
                Some(
                    item.Name,
                    { Name = item.Name
                      Path = item.Path
                      Content = v
                      Type = Plain }
                )
            | None -> None)
    |> Map.ofSeq

let CreateItem (appDataDir: string) (key: string) (name: string) (value: string) : Item =
    let cypher =
        match LanguageExt.FSharp.ToFSharp(AES.Encrypt(value, key)) with
        | Some v -> v
        | None -> ""

    let path = Path.Join(appDataDir, $"{name}.txt")

    let item =
        { Path = Path.Join(appDataDir, $"{name}.txt")
          Name = name
          Content = cypher
          Type = Cypher }

    File.WriteAllText(path, cypher)
    item

/// GetAppDataDir creates the local app data directory if it doesn't exist
/// and returns the path as a string.
let GetAppDataDir () =
    let dirPath =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify
            ),
            "cypher_fs"
        )

    Directory.CreateDirectory(dirPath) |> ignore
    dirPath

let NamePrompt (message: string) : string =
    Prompt.Input<string>(
        message,
        validators =
            [| Validators.Required()
               Validators.MinLength(1) |]
    )

let KeyPrompt () : string =
    Prompt.Password(
        "Entry secret key",
        validators =
            [| Validators.Required()
               Validators.MinLength(8) |]
    )

let GetMultilineInput () : string =
    let lines =
        Seq.initInfinite (fun _ -> Prompt.Input<string>(""))
        |> Seq.map (fun line -> if line = null then "" else line)
        |> Seq.takeWhile (fun line -> line <> "--done")
        |> Seq.toArray

    String.Join('\n', lines)

let YesNoPrompt (message: string) (defaultVal: bool) : bool = Prompt.Confirm(message, defaultVal)

let SelectionPrompt (message: string) (choices: seq<string>) : string = Prompt.Select(message, choices)

let GetUserAction () : Action =
    let action =
        SelectionPrompt
            "Select action"
            [| "Create"
               "Reveal"
               "Update"
               "Delete"
               "Exit" |]

    ActionFromString action



let CreateHandler (appDataDir: string) (itemsMap: Map<string, Item>) =
    let name = NamePrompt "New entry name"
    let key = KeyPrompt()
    printfn "Enter your content (enter --done to submit):"
    let input = GetMultilineInput()
    let item = CreateItem appDataDir key name input
    Map.add name item itemsMap

let RevealHandler (itemsMap: Map<string, Item>) =
    let key = KeyPrompt()
    let revealedMap = UnlockItems key itemsMap

    if revealedMap.IsEmpty then
        printfn "The secret key did not decrypt anything."
    else
        let entries = revealedMap.Keys

        let selectedItem =
            SelectionPrompt "Select an item to reveal" entries

        printfn $"\n{revealedMap.[selectedItem].Content}\n"

    itemsMap

let UpdateHandler (appDataDir: string) (itemsMap: Map<string, Item>) =
    let key = KeyPrompt()
    let entries = itemsMap.Keys

    let selectedItem =
        SelectionPrompt "Select an item to update" entries

    printfn "Enter your content (enter --done to submit):"
    let content = GetMultilineInput()

    let newItem =
        CreateItem appDataDir key selectedItem content

    Map.add selectedItem newItem itemsMap

let DeleteHandler (itemsMap: Map<string, Item>) =
    let key = KeyPrompt()
    let revealedMap = UnlockItems key itemsMap

    if revealedMap.IsEmpty then
        printfn "The secret key did not decrypt anything."
        itemsMap
    else
        let entries = revealedMap.Keys

        let selectedItem =
            SelectionPrompt "Select an item to delete" entries

        let confirm =
            YesNoPrompt $"Are you sure you want to delete entry ({selectedItem})?" false

        if confirm then
            File.Delete(itemsMap.[selectedItem].Path)
            Map.remove selectedItem itemsMap
        else
            itemsMap

let ProcessAction (appDataDir: string) (itemsMap: Map<string, Item>) (action: Action) =
    match action with
    | Create -> CreateHandler appDataDir itemsMap
    | Reveal -> RevealHandler itemsMap
    | Update -> UpdateHandler appDataDir itemsMap
    | Delete -> DeleteHandler itemsMap
    | Exit -> itemsMap
