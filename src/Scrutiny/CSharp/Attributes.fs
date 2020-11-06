namespace Scrutiny.CSharp

open System

[<AttributeUsage(AttributeTargets.Class)>]
type PageStateAttribute() =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type TransitionToAttribute(name: string) =
    inherit Attribute()

    member val Name = name with get 

[<AttributeUsage(AttributeTargets.Method)>]
type OnEnterAttribute() =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type OnExitAttribute() =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type ActionAttribute() =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type ExitActionAttribute() =
    inherit Attribute()
