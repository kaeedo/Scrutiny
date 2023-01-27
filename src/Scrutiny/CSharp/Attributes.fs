namespace Scrutiny.CSharp

open System

[<AttributeUsage(AttributeTargets.Class)>]
type PageStateAttribute() =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type TransitionToAttribute(name: string) =
    inherit Attribute()

    member val Name = name

[<AttributeUsage(AttributeTargets.Method)>]
type OnEnterAttribute() =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type OnExitAttribute() =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type ActionAttribute() =
    inherit Attribute()

    member val IsExit = false with get, set

[<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
type DependantActionAttribute(name: string) =
    inherit Attribute()

    member val Name = name
