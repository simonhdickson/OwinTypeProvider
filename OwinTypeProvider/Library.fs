module Owin.TypeProvider 
open System       
open System.Reflection             
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.Owin.Hosting
open Owin
open System.Threading.Tasks

open ProviderImplementation.ProvidedTypes
open FSharp.Net

let getEnviroment (url:string) =
    let enviroment = ref null
    let disposable = WebApp.Start(url, fun app -> app.Use(fun context -> fun next -> enviroment := Collections.Generic.Dictionary(context.Environment);next.Invoke())|> ignore)
    try
        Http.Request(url) |> ignore
    with
    | _ -> () 
    !enviroment |> Seq.where(fun i -> i.Value <> null) 
 
[<TypeProvider>]
type OwinProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces ()

    let ns = "Owin.TypeProvider.Provided"
    let asm = Assembly.GetExecutingAssembly()
                                             
    let createTypes () =
        let myType = ProvidedTypeDefinition(asm, ns, "Enviroment", Some typeof<obj>)

        let enviroment = getEnviroment "http://localhost:54223"
        for pair in enviroment do
            let myProp = ProvidedProperty(pair.Key, pair.Value.GetType(),
                                            GetterCode = fun args -> let value = pair.Value
                                                                     <@@ obj() @@>)
            myType.AddMember(myProp)
 
        let ctor = ProvidedConstructor([], InvokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        [myType]
 
    do
        this.AddNamespace(ns, createTypes())
 
[<assembly:TypeProviderAssembly>]
do ()
