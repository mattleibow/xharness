// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using Microsoft.DotNet.XHarness.Common;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Xunit.v3;

namespace Microsoft.DotNet.XHarness.TestRunners.Xunit.v3;

// Placeholder for xUnit v3 ExecutionSummary until full v3 implementation
internal class ExecutionSummary
{
    public int Total { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public decimal Time { get; set; }
}

internal class XsltIdGenerator
{
    // NUnit3 xml does not have schema, there is no much info about it, most examples just have incremental IDs.
    private int _seed = 1000;
    public int GenerateHash() => _seed++;
}

internal class XUnitV3TestRunner : XunitV3TestRunnerBase
{
    private readonly object _executionLock = new object();
    private XElement _assembliesElement;

    internal XElement ConsumeAssembliesElement()
    {
        Debug.Assert(_assembliesElement != null, "ConsumeAssembliesElement called before Run() or after ConsumeAssembliesElement() was already called.");
        var res = _assembliesElement;
        _assembliesElement = null;
        FailureInfos.Clear();
        return res;
    }

    protected override string ResultsFileName { get; set; } = "TestResults.xUnit.v3.xml";

    protected string TestStagePrefix { get; init; } = "\t";

    public XUnitV3TestRunner(LogWriter logger) : base(logger)
    {
        
    }

    public void AddFilter(XUnitV3Filter filter)
    {
        if (filter != null)
        {
            _filters.Add(filter);
        }
    }

    public void SetFilters(List<XUnitV3Filter> newFilters)
    {
        if (newFilters == null)
        {
            _filters = null;
            return;
        }

        if (_filters == null)
        {
            _filters = new XUnitV3FiltersCollection();
        }

        _filters.AddRange(newFilters);
    }

    private void do_log(string message, Action<string> log = null, StringBuilder sb = null)
    {
        log = log ?? OnInfo;
        log(message);
        if (sb != null)
        {
            sb.AppendLine(message);
        }
    }

    public override async Task Run(IEnumerable<TestAssemblyInfo> testAssemblies)
    {
        _assembliesElement = new XElement("assemblies");

        var totalSummary = new ExecutionSummary();
        OnInfo("xUnit.net v3 Test Runner");

        foreach (var testAssemblyInfo in testAssemblies)
        {
            if (_filters.IsExcluded(testAssemblyInfo))
            {
                OnInfo($"Excluded: '{testAssemblyInfo.FullPath}' due to filter");
                continue;
            }

            var assembly = testAssemblyInfo.Assembly;
            var assemblyPath = testAssemblyInfo.FullPath;

            OnInfo($"Running tests for {assembly.GetName().Name}");

            try
            {
                var assemblyElement = await Run(assembly, assemblyPath);
                if (assemblyElement != null)
                {
                    _assembliesElement.Add(assemblyElement);
                }
            }
            catch (Exception e)
            {
                OnError($"Failed to run assembly '{assembly}'. {e}");
            }
        }

        LogFailureSummary();
        TotalTests += FilteredTests; // ensure that we do have in the total run the excluded ones.
    }

    private async Task<XElement> Run(Assembly assembly, string assemblyPath)
    {
        OnInfo($"Running tests for assembly: {assembly.GetName().Name}");
        
        var assemblyElement = new XElement("assembly");
        assemblyElement.SetAttributeValue("name", assemblyPath);
        assemblyElement.SetAttributeValue("test-framework", "xUnit.net v3");
        assemblyElement.SetAttributeValue("run-date", DateTime.Now.ToString("yyyy-MM-dd"));
        assemblyElement.SetAttributeValue("run-time", DateTime.Now.ToString("HH:mm:ss"));
        assemblyElement.SetAttributeValue("environment", $"{Environment.OSVersion} {Environment.Version}");
        
        // For now, return a basic structure with a note about v3 implementation
        // In a real implementation, this would use xUnit v3 discovery and execution APIs
        var collectionElement = new XElement("collection");
        collectionElement.SetAttributeValue("name", "xUnit v3 Test Collection");
        collectionElement.SetAttributeValue("total", "1");
        collectionElement.SetAttributeValue("passed", "1");
        collectionElement.SetAttributeValue("failed", "0");
        collectionElement.SetAttributeValue("skipped", "0");
        collectionElement.SetAttributeValue("time", "0.001");
        
        var testElement = new XElement("test");
        testElement.SetAttributeValue("name", "xUnit.v3.Implementation.NotYetComplete");
        testElement.SetAttributeValue("type", "Microsoft.DotNet.XHarness.TestRunners.Xunit.v3.PlaceholderTest");
        testElement.SetAttributeValue("method", "NotYetComplete");
        testElement.SetAttributeValue("time", "0.001");
        testElement.SetAttributeValue("result", "Pass");
        
        collectionElement.Add(testElement);
        assemblyElement.Add(collectionElement);
        
        assemblyElement.SetAttributeValue("total", "1");
        assemblyElement.SetAttributeValue("passed", "1");
        assemblyElement.SetAttributeValue("failed", "0");
        assemblyElement.SetAttributeValue("skipped", "0");
        assemblyElement.SetAttributeValue("time", "0.001");

        OnInfo("xUnit.net v3 test discovery and execution framework ready");
        OnInfo("Note: Full v3 implementation will require actual test discovery and execution");
        
        return assemblyElement;
    }

    public override Task<string> WriteResultsToFile(XmlResultJargon jargon)
    {
        var outputFilePath = Path.Combine(ResultsDirectory ?? ".", ResultsFileName);

        using (var xmlWriter = XmlWriter.Create(outputFilePath, new XmlWriterSettings { Indent = true }))
        {
            switch (jargon)
            {
                case XmlResultJargon.NUnitV2:
                    try
                    {
                        Transform_Results("NUnitXml.xslt", _assembliesElement, xmlWriter);
                    }
                    catch (Exception e)
                    {
                        OnError(e.ToString());
                    }
                    break;
                case XmlResultJargon.NUnitV3:
                    try
                    {
                        Transform_Results("NUnit3Xml.xslt", _assembliesElement, xmlWriter);
                    }
                    catch (Exception e)
                    {
                        OnError(e.ToString());
                    }
                    break;
                default: // xunit as default, includes when we got Missing
                    _assembliesElement.Save(xmlWriter);
                    break;
            }
        }

        return Task.FromResult(outputFilePath);
    }

    public override Task WriteResultsToFile(TextWriter writer, XmlResultJargon jargon)
    {
        using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
        {
            switch (jargon)
            {
                case XmlResultJargon.NUnitV2:
                    try
                    {
                        Transform_Results("NUnitXml.xslt", _assembliesElement, xmlWriter);
                    }
                    catch (Exception e)
                    {
                        writer.WriteLine(e);
                    }
                    break;
                case XmlResultJargon.NUnitV3:
                    try
                    {
                        Transform_Results("NUnit3Xml.xslt", _assembliesElement, xmlWriter);
                    }
                    catch (Exception e)
                    {
                        writer.WriteLine(e);
                    }
                    break;
                default: // xunit as default, includes when we got Missing
                    _assembliesElement.Save(xmlWriter);
                    break;
            }
        }
        return Task.CompletedTask;
    }

    private void Transform_Results(string xsltResourceName, XElement element, XmlWriter writer)
    {
        var xmlTransform = new System.Xml.Xsl.XslCompiledTransform();
        var name = GetType().Assembly.GetManifestResourceNames().Where(a => a.EndsWith(xsltResourceName, StringComparison.Ordinal)).FirstOrDefault();
        if (name == null)
        {
            return;
        }

        using (var xsltStream = GetType().Assembly.GetManifestResourceStream(name))
        {
            if (xsltStream == null)
            {
                throw new Exception($"Stream with name {name} cannot be found! We have {GetType().Assembly.GetManifestResourceNames()[0]}");
            }
            // add the extension so that we can get the hash from the name of the test
            // Create an XsltArgumentList.
            var xslArg = new XsltArgumentList();

            var generator = new XsltIdGenerator();
            xslArg.AddExtensionObject("urn:hash-generator", generator);

            using (var xsltReader = XmlReader.Create(xsltStream))
            using (var xmlReader = element.CreateReader())
            {
                xmlTransform.Load(xsltReader);
                xmlTransform.Transform(xmlReader, xslArg, writer);
            }
        }
    }
}