using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopView.Models;

namespace DesktopView.Services;

/// <summary>
/// Result of a single benchmark sample: the input size and the measured average time in
/// milliseconds over the configured number of repeats.
/// </summary>
public sealed class BenchmarkSample
{
    public int N { get; init; }
    public double AverageMilliseconds { get; init; }
}

/// <summary>Performs empirical timing of algorithm execution over a range of input sizes.</summary>
public interface IBenchmarkService
{
    /// <summary>Runs a benchmark for the given algorithm, returning one sample per <c>n</c>.</summary>
    Task<IReadOnlyList<BenchmarkSample>> RunAsync(
        AlgorithmDefinition algorithm,
        int minN,
        int maxN,
        int step,
        int repeats,
        bool useCaching,
        CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IBenchmarkService"/>. Because the <c>algorithms</c> directory is out of
/// scope for now, this service derives a synthetic workload from the algorithm's declared
/// theoretical complexity via <see cref="ComplexityParser"/>. The structure (async, cancellable,
/// repeat-based averaging) mirrors what a real implementation against concrete algorithms would
/// use, so the call site does not need to change when real algorithms are introduced.
/// </summary>
public sealed class BenchmarkService : IBenchmarkService
{
    public async Task<IReadOnlyList<BenchmarkSample>> RunAsync(
        AlgorithmDefinition algorithm,
        int minN,
        int maxN,
        int step,
        int repeats,
        bool useCaching,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        repeats = Math.Max(1, repeats);
        var complexity = ComplexityParser.Resolve(algorithm.TheoreticalComplexity);

        // Choose a per-sweep divisor so the largest sample in the sweep performs a bounded
        // number of iterations (MaxIterations) while preserving the curve *shape* (dividing by
        // a constant only scales the curve). WorkScale amplifies the complexity so even
        // sub-linear complexities produce enough iterations to be measurable; it is kept
        // small enough that complexity(maxN) * WorkScale fits in a long for all supported
        // complexities on this sweep.
        const double WorkScale = 1e8;
        const long MaxIterations = 6_000_000;
        double peakWork = Math.Max(1, complexity(maxN)) * WorkScale;
        long divisor = Math.Max(1, (long)(peakWork / MaxIterations));

        var samples = new List<BenchmarkSample>();
        for (int n = minN; n <= maxN; n += step)
        {
            ct.ThrowIfCancellationRequested();
            double totalMs = 0;
            for (int r = 0; r < repeats; r++)
            {
                totalMs += await TimeWorkloadAsync(n, complexity, divisor, ct);
            }

            samples.Add(new BenchmarkSample
            {
                N = n,
                AverageMilliseconds = totalMs / repeats,
            });
        }

        return samples;
    }

    /// <summary>Performs a bounded number of iterations proportional to
    /// <c>complexity(n) * WorkScale / divisor</c>, so the elapsed time follows the theoretical
    /// curve shape while staying within a fixed iteration budget.</summary>
    private static async Task<double> TimeWorkloadAsync(
        int n,
        Func<double, double> complexity,
        long divisor,
        CancellationToken ct)
    {
        const double WorkScale = 1e8;
        double work = Math.Max(0, complexity(n)) * WorkScale;
        long target = (long)(work / divisor);
        if (target < 1)
        {
            target = 1;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        long acc = 0;
        for (long i = 0; i < target; i++)
        {
            acc += i;
            if ((i & 0xFFF) == 0)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        // Prevent the compiler from eliding the loop.
        _ = acc;
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }
}
