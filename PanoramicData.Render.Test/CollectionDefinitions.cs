namespace PanoramicData.Render.Test;

using Xunit;

/// <summary>
/// Tests that measure memory or timing must run serially, not in parallel with other test classes,
/// to avoid false positives from concurrent allocations skewing measurements.
/// </summary>
[CollectionDefinition("NonParallel", DisableParallelization = true)]
public sealed class NonParallelCollection;
