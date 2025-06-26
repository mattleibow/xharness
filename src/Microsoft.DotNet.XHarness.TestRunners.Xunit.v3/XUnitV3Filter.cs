// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DotNet.XHarness.TestRunners.Common;

#nullable enable
namespace Microsoft.DotNet.XHarness.TestRunners.Xunit.v3;

internal class XUnitV3Filter
{
    private string? AssemblyName { get; set; }
    private string? SelectorName { get; set; }
    private string? SelectorValue { get; set; }
    private XUnitV3FilterType FilterType { get; set; }
    private bool Exclude { get; set; }

    public static XUnitV3Filter CreateSingleFilter(string singleTestName, bool exclude)
    {
        if (string.IsNullOrEmpty(singleTestName))
        {
            throw new ArgumentException("must not be null or empty", nameof(singleTestName));
        }

        return new XUnitV3Filter
        {
            AssemblyName = null,
            SelectorName = singleTestName,
            SelectorValue = null,
            FilterType = XUnitV3FilterType.Single,
            Exclude = exclude
        };
    }

    public static XUnitV3Filter CreateClassFilter(string className, bool exclude)
    {
        if (string.IsNullOrEmpty(className))
        {
            throw new ArgumentException("must not be null or empty", nameof(className));
        }

        return new XUnitV3Filter
        {
            AssemblyName = null,
            SelectorName = className,
            SelectorValue = null,
            FilterType = XUnitV3FilterType.Class,
            Exclude = exclude
        };
    }

    public static XUnitV3Filter CreateNamespaceFilter(string namespaceName, bool exclude)
    {
        if (string.IsNullOrEmpty(namespaceName))
        {
            throw new ArgumentException("must not be null or empty", nameof(namespaceName));
        }

        return new XUnitV3Filter
        {
            AssemblyName = null,
            SelectorName = namespaceName,
            SelectorValue = null,
            FilterType = XUnitV3FilterType.Namespace,
            Exclude = exclude
        };
    }

    public static XUnitV3Filter CreateTraitFilter(string traitName, string? traitValue, bool exclude)
    {
        if (string.IsNullOrEmpty(traitName))
        {
            throw new ArgumentException("must not be null or empty", nameof(traitName));
        }

        return new XUnitV3Filter
        {
            AssemblyName = null,
            SelectorName = traitName,
            SelectorValue = traitValue ?? string.Empty,
            FilterType = XUnitV3FilterType.Trait,
            Exclude = exclude
        };
    }

    private bool ApplyTraitFilter(Xunit.v3.ITestCase testCase, Func<bool, bool>? reportFilteredTest = null)
    {
        Func<bool, bool> log = (result) => reportFilteredTest?.Invoke(result) ?? result;

        var traits = testCase.Traits;
        if (traits == null || traits.Count == 0)
        {
            return log(!Exclude);
        }

        if (traits.TryGetValue(SelectorName!, out var values))
        {
            if (values == null || values.Count == 0)
            {
                // We have no values and the filter doesn't specify one - that means we match on
                // the trait name only.
                if (string.IsNullOrEmpty(SelectorValue))
                {
                    return log(Exclude);
                }

                return log(!Exclude);
            }

            return values.Any(value => value.Equals(SelectorValue, StringComparison.InvariantCultureIgnoreCase)) ?
                log(Exclude) : log(!Exclude);
        }

        // no traits found, that means that we return the opposite of the setting of the filter
        return log(!Exclude);
    }

    private bool ApplyTypeNameFilter(Xunit.v3.ITestCase testCase, Func<bool, bool>? reportFilteredTest = null)
    {
        Func<bool, bool> log = (result) => reportFilteredTest?.Invoke(result) ?? result;

        var className = testCase.TestClass?.Class?.Name;
        if (string.IsNullOrEmpty(className))
        {
            return log(!Exclude);
        }

        var match = className.Equals(SelectorName, StringComparison.Ordinal);
        return match ? log(Exclude) : log(!Exclude);
    }

    private bool ApplySingleFilter(Xunit.v3.ITestCase testCase, Func<bool, bool>? reportFilteredTest = null)
    {
        Func<bool, bool> log = (result) => reportFilteredTest?.Invoke(result) ?? result;

        var testMethodName = testCase.TestMethod?.Method?.Name;
        if (string.IsNullOrEmpty(testMethodName))
        {
            return log(!Exclude);
        }

        var match = testMethodName.Equals(SelectorName, StringComparison.Ordinal);
        return match ? log(Exclude) : log(!Exclude);
    }

    private bool ApplyNamespaceFilter(Xunit.v3.ITestCase testCase, Func<bool, bool>? reportFilteredTest = null)
    {
        Func<bool, bool> log = (result) => reportFilteredTest?.Invoke(result) ?? result;

        var namespaceName = testCase.TestClass?.Class?.Name;
        if (string.IsNullOrEmpty(namespaceName))
        {
            return log(!Exclude);
        }

        var lastDotIndex = namespaceName.LastIndexOf('.');
        if (lastDotIndex == -1)
        {
            return log(!Exclude);
        }

        var namespaceOnly = namespaceName.Substring(0, lastDotIndex);
        var match = namespaceOnly.Equals(SelectorName, StringComparison.Ordinal);
        return match ? log(Exclude) : log(!Exclude);
    }

    public bool IsExcluded(TestAssemblyInfo assembly, Action<string>? reportFilteredAssembly = null)
    {
        if (string.IsNullOrEmpty(AssemblyName))
        {
            return false;
        }

        var assemblyName = Path.GetFileNameWithoutExtension(assembly.FullPath);
        if (assemblyName.Equals(AssemblyName, StringComparison.Ordinal))
        {
            reportFilteredAssembly?.Invoke($"Excluded '{assembly.FullPath}' due to filter");
            return Exclude;
        }

        return !Exclude;
    }

    public bool IsExcluded(Xunit.v3.ITestCase testCase, Action<string>? log = null)
    {
        return FilterType switch
        {
            XUnitV3FilterType.Single => ApplySingleFilter(testCase, (result) => ReportFilteredTest(testCase, result, log)),
            XUnitV3FilterType.Class => ApplyTypeNameFilter(testCase, (result) => ReportFilteredTest(testCase, result, log)),
            XUnitV3FilterType.Namespace => ApplyNamespaceFilter(testCase, (result) => ReportFilteredTest(testCase, result, log)),
            XUnitV3FilterType.Trait => ApplyTraitFilter(testCase, (result) => ReportFilteredTest(testCase, result, log)),
            _ => false,
        };
    }

    private bool ReportFilteredTest(Xunit.v3.ITestCase testCase, bool excluded, Action<string>? log = null)
    {
        if (log == null)
            return excluded;

        string? message = null;
        switch (FilterType)
        {
            case XUnitV3FilterType.Single:
                message = $"[FILTER] {(excluded ? "Excluded" : "Included")} test '{testCase.DisplayName}' due to single filter '{SelectorName}'.";
                break;
            case XUnitV3FilterType.Class:
                message = $"[FILTER] {(excluded ? "Excluded" : "Included")} test '{testCase.DisplayName}' due to class filter '{SelectorName}'.";
                break;
            case XUnitV3FilterType.Namespace:
                message = $"[FILTER] {(excluded ? "Excluded" : "Included")} test '{testCase.DisplayName}' due to namespace filter '{SelectorName}'.";
                break;
            case XUnitV3FilterType.Trait:
                message = $"[FILTER] {(excluded ? "Excluded" : "Included")} test '{testCase.DisplayName}' due to trait filter '{SelectorName}={SelectorValue}'.";
                break;
        }

        if (message != null)
            log(message);

        return excluded;
    }
}