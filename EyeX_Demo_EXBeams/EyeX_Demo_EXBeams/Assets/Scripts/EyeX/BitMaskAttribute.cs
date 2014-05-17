//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using UnityEngine;

/// <summary>
/// Attribute for marking bit-mask properties so that they can be used with a custom property drawer.
/// </summary>
public class BitMaskAttribute : PropertyAttribute
{
    public Type propertyType;

    public BitMaskAttribute(Type type)
    {
        propertyType = type;
    }
}
