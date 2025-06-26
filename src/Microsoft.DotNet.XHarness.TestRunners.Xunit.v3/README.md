# Microsoft.DotNet.XHarness.TestRunners.Xunit.v3

This package provides xUnit v3 test runner support for XHarness, offering an alternative to the existing xUnit v2 implementation.

## Overview

xUnit v3 is a major rewrite of the xUnit testing framework with new APIs and improved performance. This package allows you to run xUnit v3 tests using the XHarness test execution framework on various platforms including Android, iOS, and WebAssembly.

## Key Features

- **xUnit v3 Support**: Uses the latest xUnit v3 framework APIs
- **Cross-Platform**: Supports Android, iOS, and WASM platforms
- **Filtering**: Advanced test filtering by class, method, namespace, and traits
- **Multiple Output Formats**: Supports xUnit, NUnit v2, and NUnit v3 XML output formats
- **Parallel Execution**: Optimized for xUnit v3's improved parallel execution model

## Usage

### Android

```csharp
public class TestsEntryPoint : AndroidApplicationEntryPoint
{
    protected override int? MaxParallelThreads => System.Environment.ProcessorCount;
}
```

### iOS

```csharp
public class TestsEntryPoint : iOSApplicationEntryPoint
{
    protected override int? MaxParallelThreads => System.Environment.ProcessorCount;
}
```

### WASM

```csharp
public class TestsEntryPoint : WasmApplicationEntryPoint
{
    protected override int? MaxParallelThreads => 1; // WASM typically single-threaded
}
```

## Migration from v2

If you're migrating from the `Microsoft.DotNet.XHarness.TestRunners.Xunit` (v2) package:

1. Update your project reference to use `Microsoft.DotNet.XHarness.TestRunners.Xunit.v3`
2. Update your test framework package references to use xUnit v3 packages
3. Change your entry point namespace from `Microsoft.DotNet.XHarness.TestRunners.Xunit` to `Microsoft.DotNet.XHarness.TestRunners.Xunit.v3`
4. The API surface is largely compatible, but some xUnit v3-specific features may be available

## Current Implementation Status

This is an initial implementation that provides the framework for xUnit v3 support. The current version includes:

- ✅ Project structure and basic test runner framework
- ✅ Platform-specific entry points
- ✅ Test filtering and configuration
- ✅ XML result output in multiple formats
- 🚧 Full xUnit v3 test discovery and execution (placeholder implementation)

The full test discovery and execution implementation will be completed as xUnit v3 stabilizes and becomes widely available.

## Package Dependencies

- `xunit.v3.core`: Core xUnit v3 testing framework
- `xunit.v3.runner.utility`: xUnit v3 runner utilities for test discovery and execution
- `Microsoft.DotNet.XHarness.TestRunners.Common`: Common test runner infrastructure

## Contributing

This package is part of the XHarness project. Contributions and feedback are welcome to help complete the xUnit v3 implementation.