namespace Criipto.React

open Feliz
open Feliz.Bulma
open Fable.Core.JsInterop

module FileUpload = 
    
    type File = {
        Content : string
        Name : string
        Size : int
        Rank : int
    }

    type FileUploadOptions<'manager> = {
        Manager : Types.IDataManager<'manager,Map<string,File>>
        InputFieldLabel : string
        IsFullWidth : bool
    }
    
    [<ReactComponent>]
    let FileUpload<'err,'view,'user> (options : FileUploadOptions<Types.IManager<'err,'view,'user>>) = 
        let files = 
            options.Manager.Data
            |> Map.toList
            |> List.map snd
            |> List.sortBy (fun f -> f.Rank)
  
        let rec readFile (files : Browser.Types.File list) (readFiles : File list)=
            match files with
            [] -> 
                options.Manager.Data <-
                    readFiles
                    |> List.fold(fun m f ->
                        m.Add(f.Name,f)
                    ) options.Manager.Data
            | file::files -> 
                let reader = Browser.Dom.FileReader.Create()
                reader.onload <- fun evt ->
                    let content = 
                        (string evt.target?result).Split("base64,",2).[1].Trim()
                    readFile files ({Content = content;Name = file.name;Size = file.size;Rank = readFiles.Length}::readFiles)
                reader.onerror <- fun _ ->
                    options.Manager.SystemManager.ErrorManager.AddError reader?error
                    readFile files readFiles
                reader.readAsDataURL(file)
        let fileSection = 
            let dragDropProps = 
                [
                    prop.onDrop (fun (ev : Browser.Types.DragEvent) ->
                        ev.preventDefault()
                        let newFiles = 
                            if ev.dataTransfer.items <> Fable.Core.JS.undefined then
                                [ for i in 0..ev.dataTransfer.items.length - 1 do
                                    let item = ev.dataTransfer.items.[i]
                                    if item.kind = "file" then yield item.getAsFile() ]
                            else
                                [ for i in 0..ev.dataTransfer.files.length - 1 ->
                                    ev.dataTransfer.files.[i] ]
                        readFile newFiles files
                    )
                    prop.onDragOver (fun ev ->
                        ev.preventDefault()
                    )
                ]
            let props = 
                match files with
                [] ->
                    prop.className "file-drop-zone"
                    ::dragDropProps
                | _ ->
                    let swap i j =
                        if i > (files.Length - 1) then 
                            failwithf "Index too high (%d)" i
                        elif i < 0 then
                            failwithf "Index mustbe positive (%d)" i
                        elif j = -1 then
                            fun _ -> () //no-op it's the first element being moved up
                        elif j = files.Length then
                            fun _ -> () //no-op it's the last element being moved down
                        else
                            fun _ ->
                                options.Manager.Data <- 
                                    files
                                    |> List.map(fun f -> 
                                        f.Name,
                                            if j = f.Rank then
                                                { f with Rank = i}
                                            elif i = f.Rank then
                                                { f with Rank = j}
                                            else
                                                f
                                    )
                                    |> Map.ofList
                    let removeAt index = 
                        if index > files.Length then
                            failwithf "Index too high (%d)" index
                        elif index < 0 then
                            failwithf "Index mustbe positive (%d)" index
                        else
                            fun _ -> 
                                options.Manager.Data <-
                                    files
                                    |> List.indexed
                                    |> List.fold(fun m (i,f) -> 
                                        if i <> index then 
                                            m.Add(f.Name,f)
                                        else
                                            m
                                    ) Map.empty
                                
                    (files
                    |> List.mapi(fun i file -> 
                        let iconName = 
                            match (file.Name.Split(".") |> Array.last).ToLower() with
                            "jpg" | "png" | "jpeg" | "gif" -> "fa-file-image"
                            | "pdf" -> "fa-file-pdf"
                            | "doc" | "docx" -> "fa-file-word"
                            | _  -> "fa-file-check"
                        Html.div [
                            prop.className "file-upload-item"
                            prop.children [
                                Bulma.icon [
                                    prop.children [
                                        Html.i [
                                            sprintf "fas %s" iconName |> prop.className
                                        ]
                                    ]
                                ]
                                Html.span file.Name
                                Bulma.icon [
                                    prop.children [
                                        Html.i [
                                            if i = 0 then yield prop.className "action-disabled"
                                            yield "fas fa-angle-up" |> prop.className
                                        ]
                                    ]
                                    prop.onClick(i - 1 |> swap i )
                                ]
                                Bulma.icon [
                                    prop.children [
                                        Html.i [
                                            if i = files.Length - 1 then yield prop.className "action-disabled"
                                            yield "fas fa-angle-down" |> prop.className
                                        ]
                                    ]
                                    prop.onClick(i + 1 |> swap i)
                                ]
                                Bulma.icon [
                                    prop.children [
                                        Html.i [
                                            "fas fa-times" |> prop.className
                                        ]
                                    ]
                                    prop.onClick(removeAt i)
                                ]
                            ]
                        ]
                    ) |> prop.children)
                    ::dragDropProps
            Html.div props

        Bulma.section [
            fileSection
            Html.div [ 
                "file " + if options.IsFullWidth then "is-fullwidth" else ""
                |> prop.className
                prop.children [
                    Html.label [ 
                        prop.className "file-label"
                        prop.children [
                            Html.input [
                                prop.className "file-input" 
                                prop.type' "file" 
                                prop.name "resume"
                                prop.onInput (fun ev -> 
                                    let files = 
                                        let files : Browser.Types.FileList = ev.target?files
                                        [for i in 0..files.length - 1 -> files.Item i]  
                                    let files =   
                                        files
                                        |> List.filter(fun file -> 
                                            options.Manager.Data |> Map.tryFind file.name |> Option.isNone
                                        )
                                    readFile files []
                                )
                            ]
                            Html.span [
                                prop.className"file-cta"
                                prop.children [
                                    Html.span [
                                        prop.className"file-icon"
                                        prop.children [
                                            Html.i [ prop.className"fas fa-upload"]
                                        ]
                                    ]
                                    Html.span [
                                        prop.className"file-label"
                                        prop.text options.InputFieldLabel
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
