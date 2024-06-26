﻿using System.Diagnostics.Contracts;

namespace LanguageExt.ClassInstances;

/// <summary>
/// Boolean hash
/// </summary>
public struct HashableBool : Hashable<bool>
{
    /// <summary>
    /// Get hash code of the value
    /// </summary>
    /// <param name="x">Value to get the hash code of</param>
    /// <returns>The hash code of x</returns>
    [Pure]
    public static int GetHashCode(bool x) =>
        x.GetHashCode();
}
