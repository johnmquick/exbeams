//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using UnityEngine;

/// <summary>
/// Pre-defined interaction behaviors.
/// NOTE: these behaviors can be combined like so: Activatable | GazeAware.
/// </summary>
[Flags]
public enum EyeXBehaviors
{
    None = 0,
    Activatable = 0x01,
    ActivatableWithTentativeFocus = 0x02,
    GazeAware = 0x04,
    GazeAwareWithInertia = 0x08
}
