module public App.Core.Addons.Tests.EnginesFinderTests

open App.Core.Addons
open App.Core.Domain
open App.Core.State
open Moq
open NUnit.Framework

[<Test>]
[<Ignore("Test needs internet connection + is OS specific. Manual testing only.")>]
let public ``Godot Engines Can Be Found Online`` () =
    let updateOnlineEngines = UpdateOnlineEnginesAddon.mac // Change addon OS here
    let controller = Mock.Of<IAppStateController>()
    let mutable foundEngines = None
    Mock.Get(controller)
        .Setup(fun c -> c.SetOnlineEngines(It.IsAny<EngineOnline seq>()))
        .Callback(fun (a: EngineOnline seq) -> foundEngines <- a |> Array.ofSeq |> Some)
    |> ignore

    match updateOnlineEngines.AfterInitializeTask.Value with
    | AddonAsync asyncTask ->
        asyncTask controller |> Async.StartChild |> Async.RunSynchronously |> Async.RunSynchronously
    | _ -> ()
    Assert.That(foundEngines.Value.Length, Is.GreaterThan(30))
