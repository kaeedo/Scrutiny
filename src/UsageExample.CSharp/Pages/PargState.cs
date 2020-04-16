using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

public abstract class PargState<a, b>
{

    public string Name
    {
        get;
    }

    public PargState(string name)
    {
        Name = name;
    }

    public abstract void OnEnter();

    public abstract void OnExit();

    public abstract void ExitAction();

    public abstract IEnumerable<Func<PargState<a, b>>> Transitions();

    public abstract IEnumerable<Action> Actions(b ls);
}
