#I "packages/Suave/lib/net40"
#r "packages/Suave/lib/net40/Suave.dll"

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Files
open Suave.Http.Successful
open Suave.Types
open Suave.Web
open System
open System.Net
open System.IO

let challenges = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "challenges")).GetFiles()
                 |> Seq.filter (fun f -> f.Extension = ".html")
                 |> Seq.mapi (fun i c -> path (sprintf "/issue-%d" (i+1)) >>= file (sprintf "Challenges/%s" c.Name))
                 |> Seq.toList

let config = 
    let port = System.Environment.GetEnvironmentVariable("PORT")
    { defaultConfig with 
        logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Verbose
        bindings=[ (if port = null then HttpBinding.mk' HTTP  "127.0.0.1" 8083
                    else HttpBinding.mk' HTTP  "0.0.0.0" (int32 port)) ] }

let app : WebPart =
  choose [
    GET >>= choose (challenges |> List.append [
      path "/styles.css" >>= file "template/fsharp-style.css" ;
      path "/" >>= file "index.html" 
      ] )
  ]


startWebServer defaultConfig app