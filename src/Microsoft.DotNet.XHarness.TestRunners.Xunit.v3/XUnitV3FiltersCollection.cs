// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

#nullable enable
namespace Microsoft.DotNet.XHarness.TestRunners.Xunit.v3;

internal class XUnitV3FiltersCollection : List<XUnitV3Filter>
{
    public bool IsExcluded(TestAssemblyInfo assembly, System.Action<string>? reportFilteredAssembly = null)
    {
        if (Count == 0)
        {
            return false;
        }

        foreach (var f in this)
        {
            if (f.IsExcluded(assembly, reportFilteredAssembly))
                return true;
        }
        return false;
    }

    public bool IsExcluded(Xunit.v3.ITestCase testCase, System.Action<string>? log = null)
    {
        if (Count == 0)
        {
            return false;
        }

        foreach (var f in this)
        {
            if (f.IsExcluded(testCase, log))
                return true;
        }
        return false;
    }
}