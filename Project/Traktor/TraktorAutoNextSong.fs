﻿open System.Xml.Linq
open FSharp.Data
open System.Collections.Generic


type Collection = XmlProvider<".\collection.nml">

type Song = { BPM : decimal; Title : string; Artist : string; Key : string }
type Edge = { Weight : int; From : Song; To : Song }

let parseCollection = 
    let parseToSong (i:Collection.Entry) = 
        let title = 
            match i.Title.IsSome with 
            | true -> i.Title.Value
            | _    -> ""
            
        let tempo = 
            match i.Tempo with
            | Some n -> n.Bpm
            | None   -> new decimal()

        let artist = 
            match i.Artist with
            | Some n -> n
            | None   -> ""

        let key = 
            match i.Info.Key with
            | Some n -> n
            | _      -> ""

        { BPM = tempo; Title = title; Artist = artist; Key = key }

    let sample = Collection.GetSample()
    let entries = sample.Collection.Entries2
    let songs = Array.map (fun i -> parseToSong i) entries
    List.ofArray songs

let rec createGraph (original: Song list) (list:Song list) acc = 
    match list with
    | song::ss  -> let otherSongs = List.filter (fun s -> (s <> song)) original
                   let edgesFromSong = List.map (fun s -> { Weight = System.Int32.MaxValue; From = song; To = s }) otherSongs
                   createGraph original ss ((song, edgesFromSong) :: acc)
    | []      -> acc

let weightForKey (key, other) = 
    match (key, other) with //TODO: Define key rules and add more key rules.
            | ("1d", other)      -> match other with
                                    | "1m" | "2d" | "12d" -> 0 //Major to minor, one key up/down.
                                    | "3d" | "8d"         -> 0 //One semitone, two semitones up.
                                    | "10d"               -> 0 //Since major, three keys down.
                                    | _                   -> 20 //Keys do not match up, add 20 to weight.
            | ("1m", other)      -> match other with
                                    | "1d" | "2m" | "12m" -> 0 //Major to minor, one key up/down.
                                    | "3m" | "8m"         -> 0 //One semitone, two semitones up.
                                    | "4m"                -> 0 //Since minor, three keys UP.
                                    | _                   -> 20 //Keys do not match up, add 20 to weight.
            | ("2d", other)      -> match other with
                                    | "2m" | "3d" | "1d"  -> 0
                                    | "4d" | "9d"         -> 0 
                                    | "11d"               -> 0
                                    | _                   -> 20 
            | ("2m", other)      -> match other with
                                    | "2d" | "3m" | "1m"  -> 0 
                                    | "4m" | "9m"         -> 0 
                                    | "5m"                -> 0
                                    | _                   -> 20 
            | _                 -> 10000

let rec calculateWeights list acc = 
    let calculateWeight edge = 
        let bpmWeight = System.Math.Abs (edge.From.BPM - edge.To.BPM)
        let keyWeight = weightForKey (edge.From.Key, edge.To.Key)

        (int) bpmWeight + keyWeight

    let weightForSongEdgeTuple (song, edges) = 
        (song, List.map (fun e -> { Weight = calculateWeight e; From = e.From; To = e.To }) edges)

    match list with 
    | x::xs -> calculateWeights xs ((weightForSongEdgeTuple x) :: acc)
    | []     -> acc


[<EntryPoint>]
let main argv = 
    let songs = parseCollection
    let graph = createGraph songs songs []
    let withWeights = calculateWeights graph []
    
    0