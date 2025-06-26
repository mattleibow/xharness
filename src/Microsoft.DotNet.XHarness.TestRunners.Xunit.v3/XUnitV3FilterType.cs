// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.DotNet.XHarness.TestRunners.Xunit.v3;

internal enum XUnitV3FilterType
{
    /// <summary>
    /// Filter to apply to a class
    /// </summary>
    Class,
    /// <summary>
    /// Filter to apply to a trait
    /// </summary>
    Trait,
    /// <summary>
    /// Filter to apply to a single test method.
    /// </summary>
    Single,
    /// <summary>
    /// Filter to apply to namespace.
    /// </summary>
    Namespace,
}