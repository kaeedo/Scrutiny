namespace Scrutiny.CSharp

open System

[<AttributeUsage(AttributeTargets.Class)>]
type PageStateAttribute() =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type TransitionToAttribute(name: string) =
    inherit Attribute()

    member val Name = name with get 