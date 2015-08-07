(**
# Setup
*)
#load "../packages/FsLab/FsLab.fsx"

(**
We'll use 3 parts of FSLab:
1. FSharp.Data for data querying
2. Deedle for data processing and manipulation
3. XPlot for graphing our results
*)

open System
open FSharp.Data
open FSharp.Data.Runtime.BaseTypes
open Deedle
open XPlot.GoogleCharts

(** 
Set some constants up.
*)

let baseUrl = "http://api.nytimes.com/svc/elections/us/v3/finances"
// let apiKey = "3938ef998ede56fd9ba55e3b2ccefa37:16:72625105"
let apiKey = "316ed00bf4b54d8ba9a625e84f78d80a:12:72637909"

let states = ["AL";"AK";"AZ";"AR";"CA";"CO";"CT";"DE";"FL";"GA";"HI";
              "ID";"IL";"IN";"IA";"KS";"KY";"LA";"ME";"MD";"MA";"MI";
              "MN";"MS";"MO";"MT";"NE";"NV";"NH";"NJ";"NM";"NY";"NC";
              "ND";"OH";"OK";"OR";"PA";"RI";"SC";"SD";"TN";"TX";"UT";
              "VT";"VA";"WA";"WV";"WI";"WY"]

(**
# Type providers

Type providers are super-classes that allow types to be generated (and compiled into CLI) using dynamic data.  Examples of data sources can be SQL databases, JSON, or CSV files.  Type providers can be thought of as programattic T4 templates, without the double-compilation overhead that T4s have.

We'll setup 2 schemas (types) to query against: **broad** candidate information, and **detailed** candidate information.
Doing this gives strongly typed intellisense-capable classes which can load information directly from the API.
*)
type candidateCollection = JsonProvider<"""{
  "status": "OK",
  "copyright": "Copyright (c) 2015 The New York Times Company. All Rights Reserved.",
  "cycle": 2016,
  "base_uri": "http://api.nytimes.com/svc/elections/us/v3/finances/2016/",
  "num_results": 10,
  "results": [{
    "candidate": {
      "id": "H6MN01174",
      "relative_uri": "/candidates/H6MN01174.json",
      "name": "WALZ, TIMOTHY J.",
      "party": "DEM"
    },
    "committee": "/committees/C00409409.json",
    "state": "/seats/MN.json",
    "district": "/seats/MN/house/01.json"
  }]
}""">
type candidateDetailsCollection = JsonProvider<"""{
  "status": "OK",
  "copyright": "Copyright (c) 2015 The New York Times Company. All Rights Reserved.",
  "base_uri": "http://api.nytimes.com/svc/elections/us/v3/finances/2012/",
  "cycle": 2012,
  "results": [{
    "id": "S6MN00234",
    "name": "DAVIS, TIM",
    "party": "OTH",
    "district": "/seats/MN/senate.json",
    "fec_uri": "http://docquery.fec.gov/cgi-bin/fecimg/?S6MN00234",
    "committee": null,
    "state": "/seats/MN.json",
    "mailing_address": "4154 VINCENT AVENUE NORTH",
    "mailing_city": "MINNEAPOLIS",
    "mailing_state": "MN",
    "mailing_zip": "55412",
    "status": null,
    "total_receipts": 0,
    "total_from_individuals": 0,
    "total_from_pacs": 0,
    "total_contributions": 0,
    "candidate_loans": 0,
    "total_disbursements": 0,
    "begin_cash": 0,
    "end_cash": 0,
    "total_refunds": 0,
    "debts_owed": 0,
    "date_coverage_from": null,
    "date_coverage_to": null,
    "independent_expenditures": 0,
    "coordinated_expenditures": 0,
    "other_cycles": []
  }]
}""">

(**
# Data querying

Next, let's find all candidates in all states.
*)
let stateCandidates =
    states
    |> Seq.map (fun s -> sprintf "%s/2012/seats/%s.json?api-key=%s" baseUrl s apiKey)
    |> Seq.map candidateCollection.Load
    |> Seq.toList

(**
Then get details for everyone one of these candidates.
*)
let candidateDetails =
    stateCandidates |> Seq.collect (fun c ->
      c.Results |> Seq.map(fun r -> r.Candidate.Id))
    |> Seq.map (fun s ->
      sprintf "%s/2012/candidates/%s.json?api-key=%s" baseUrl s apiKey)
    |> Seq.map candidateDetailsCollection.Load
    |> Seq.toList

(**
Deedle doesn't support expanding JsonValue types by default, so we'll add that into the library.  This is mostly unecessary to read.
*)
let matchColumn (key:string) value =
    seq {
        match value with
        | JsonValue.String (s)  -> yield key,typeof<string>,box s
        | JsonValue.Boolean (b) -> yield key,typeof<bool>,box b
        | JsonValue.Float (f)   -> yield key,typeof<float>,box f
        | JsonValue.Null (_)    -> yield key,typeof<obj>,box null
        | JsonValue.Number (n)  -> yield key,typeof<decimal>,box n
        | JsonValue.Record (r)  -> yield key,typeof<JsonValue>,box r
        | JsonValue.Array (a)   ->
            yield! a
            |> Seq.map (fun item -> (key,typeof<JsonValue>,box item))
    }


Frame.CustomExpanders.Add(typeof<JsonDocument>, fun o -> seq {
    let doc = o :?> FSharp.Data.Runtime.BaseTypes.JsonDocument
    match doc.JsonValue with 
    | JsonValue.Record(map) ->
        for (k,v) in map do yield! (matchColumn k v)
    | _ -> () })

Frame.CustomExpanders.Add(typeof<JsonValue>, fun o -> seq {
    let doc = o :?> JsonValue
    match doc with 
    | JsonValue.Record(map) ->
        for (k,v) in map do yield! (matchColumn k v)
    | _ -> () })

(**
# Deedle for analysis

Deedle is something like an in-memory table structure, but not quite.  The frame concept is present in other languages (R, python [Pandas], and more).  Deedle is F#'s most prevelant implementation of the data frame pattern.

Data frames allow very quick manipulation of data once they are setup.
*)
let dataFrame =
    [for c in candidateDetails
      |> Seq.collect (fun c -> c.Results) -> series ["Candidate" => c] ]
    |> Frame.ofRowsOrdinal
    |> Frame.expandAllCols 10

(**
Group information by parties
*)
let parties =
    dataFrame
    |> Frame.groupRowsByString "Candidate.party"

(**
Get the number of candidates for each party
*)
let numberOfCandidates =
    parties
    |> Frame.nest
    |> Series.mapValues Frame.countRows

(**
Get the sum & standard deviation of various columns - this is done dynamically.  We'll focus on finances when we chart.
*)
let partyFinancesSum = parties |> Frame.applyLevel fst Stats.sum
let partyFinancesStdDev = parties |> Frame.applyLevel fst Stats.stdDev

(**
# Charting!

Chart some interesting things.
 - How many candidates did each party have in 2012?  
 - Which party had the most capital?
 - Which party had the most capital PER candidate?
*)
numberOfCandidates
|> Chart.Pie
|> Chart.WithLegend true
|> Chart.WithTitle "Number of candidates in each party"
(**
Gives the chart
<iframe src="/chart-1" scrolling="no" seamless="seamless"></iframe>
*)
partyFinancesSum?``Candidate.total_contributions``
|> Chart.Bar
|> Chart.WithTitle "Contributions for each party"
(**
Ends up with the following: 
<iframe src="/chart-2" scrolling="no" seamless="seamless"></iframe>
*)

(**
Cool property of data frames: division and other mathematical operatoins are done via index matching.
*)
(partyFinancesSum?``Candidate.total_contributions``) /(numberOfCandidates |> Series.mapValues float) 
|> Chart.Pie 
|> Chart.WithLegend true 
|> Chart.WithTitle "Contributions per candidate"

(**
<iframe src="/chart-3" scrolling="no" seamless="seamless"></iframe>
And it looks like although the republican party had more candidates, democrats raised more money.  Additionally, the WFP party (which was so small as to not even register on our first exposure) raisd a fair amount of money for their one candidate.
*)