module CypherCLI.Fs.Model


type ItemType =
    | Cypher
    | Plain

type Action =
    | Create
    | Reveal
    | Update
    | Delete
    | Exit

let ActionFromString (s: string) : Action =
    match s with
    | "Create" -> Create
    | "Reveal" -> Reveal
    | "Update" -> Update
    | "Delete" -> Delete
    | "Exit" -> Exit
    | _ -> failwith $"{s} is not a valid action"

type Item =
    { Path: string
      Name: string
      Content: string
      Type: ItemType }
