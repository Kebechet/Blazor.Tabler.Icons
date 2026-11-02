using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.Loader;
using Blazor.Tabler.Icons;
using Blazor.Tabler.Icons.Svg;
using Xunit;

namespace Blazor.Tabler.Icons.Svg.Tests;

[Collection(RegistryCollection.Name)]
public class RegistryTests
{
    private const int RoundCount = 50;
    private const int IconsPerRound = 1024;
    private const int WriterCount = 16;
    private const int ReaderCount = 8;

    private static readonly TimeSpan _roundTimeout = TimeSpan.FromSeconds(60);

    private static readonly int[] _iconNumbers = Enum.GetValues<TablerIconType>()
        .Select(x => (int)x)
        .Distinct()
        .Take(IconsPerRound)
        .ToArray();

    [Fact]
    public void Register_ThenTryGet_ReturnsStoredEntry()
    {
        // Arrange
        TablerSvgRegistry.Register(TablerIconType.Activity, isFilled: false, inner: "<path d=\"M1 1\" />");

        // Act
        var found = TablerSvgRegistry.TryGet(TablerIconType.Activity, out var entry);

        // Assert
        Assert.True(found);
        Assert.False(entry.IsFilled);
        Assert.Equal("<path d=\"M1 1\" />", entry.Inner);
    }

    [Fact]
    public void TryGet_ForUnregisteredIcon_ReturnsFalse()
    {
        // Act
        var found = TablerSvgRegistry.TryGet(TablerIconType.ZzzOff, out _);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void Register_SameIconTwice_KeepsLastWrite()
    {
        // Arrange
        TablerSvgRegistry.Register(TablerIconType.ZoomCancel, isFilled: false, inner: "<path d=\"first\" />");

        // Act
        TablerSvgRegistry.Register(TablerIconType.ZoomCancel, isFilled: true, inner: "<path d=\"second\" />");
        var found = TablerSvgRegistry.TryGet(TablerIconType.ZoomCancel, out var entry);

        // Assert
        Assert.True(found);
        Assert.True(entry.IsFilled);
        Assert.Equal("<path d=\"second\" />", entry.Inner);
    }

    [Fact]
    public void Register_FromManyThreadsAtOnce_KeepsEveryEntry()
    {
        for (var round = 0; round < RoundCount; round++)
        {
            // Arrange
            var registry = new IsolatedRegistry();

            // Act
            RunConcurrently(WriterCount, writer =>
            {
                for (var index = writer; index < _iconNumbers.Length; index += WriterCount)
                {
                    Register(registry, _iconNumbers[index]);
                }
            });

            // Assert
            foreach (var iconNumber in _iconNumbers)
            {
                AssertRegistered(registry, iconNumber);
            }
        }
    }

    [Fact]
    public void TryGet_WhileOtherThreadsRegister_NeverLosesOrCorruptsEntries()
    {
        var alreadyRegisteredIconNumbers = _iconNumbers.Take(_iconNumbers.Length / 2).ToArray();
        var incomingIconNumbers = _iconNumbers.Skip(_iconNumbers.Length / 2).ToArray();

        for (var round = 0; round < RoundCount; round++)
        {
            // Arrange
            var registry = new IsolatedRegistry();
            foreach (var iconNumber in alreadyRegisteredIconNumbers)
            {
                Register(registry, iconNumber);
            }

            using var writersDone = new CountdownEvent(WriterCount);

            // Act
            RunConcurrently(WriterCount + ReaderCount, worker =>
            {
                if (worker < WriterCount)
                {
                    try
                    {
                        for (var index = worker; index < incomingIconNumbers.Length; index += WriterCount)
                        {
                            Register(registry, incomingIconNumbers[index]);
                        }
                    }
                    finally
                    {
                        writersDone.Signal();
                    }

                    return;
                }

                do
                {
                    // Present before the round started: must stay visible while the table grows.
                    foreach (var iconNumber in alreadyRegisteredIconNumbers)
                    {
                        AssertRegistered(registry, iconNumber);
                    }

                    // Being written right now: either not there yet or complete, never half-written.
                    foreach (var iconNumber in incomingIconNumbers)
                    {
                        var entry = registry.TryGet(iconNumber);
                        if (entry is not null)
                        {
                            Assert.Equal(GetExpectedEntry(iconNumber), entry.Value);
                        }
                    }
                }
                while (!writersDone.IsSet);
            });

            // Assert
            foreach (var iconNumber in incomingIconNumbers)
            {
                AssertRegistered(registry, iconNumber);
            }
        }
    }

    private static void Register(IsolatedRegistry registry, int iconNumber)
    {
        var expected = GetExpectedEntry(iconNumber);
        registry.Register(iconNumber, expected.IsFilled, expected.Inner);
    }

    private static TablerSvgEntry GetExpectedEntry(int iconNumber)
    {
        return new TablerSvgEntry(IsFilled: iconNumber % 2 == 0, Inner: $"<path d=\"M{iconNumber} {iconNumber}\" />");
    }

    private static void AssertRegistered(IsolatedRegistry registry, int iconNumber)
    {
        var entry = registry.TryGet(iconNumber);

        Assert.True(entry is not null, $"Icon {iconNumber} was registered but TryGet cannot find it");
        Assert.Equal(GetExpectedEntry(iconNumber), entry.GetValueOrDefault());
    }

    /// <summary>
    /// Runs <paramref name="body"/> on <paramref name="threadCount"/> dedicated threads released by a
    /// barrier at the same moment, so the work really overlaps. Dedicated threads rather than
    /// <c>Parallel.For</c>: a barrier needs every participant running at once, which the thread pool
    /// does not promise. Throws everything the threads threw, or a timeout if one is stuck, which is
    /// how a corrupted <see cref="Dictionary{TKey, TValue}"/> can end.
    /// </summary>
    private static void RunConcurrently(int threadCount, Action<int> body)
    {
        var exceptions = new ConcurrentQueue<Exception>();
        using var startBarrier = new Barrier(threadCount);

        var threads = Enumerable.Range(0, threadCount)
            .Select(worker => new Thread(() =>
            {
                try
                {
                    startBarrier.SignalAndWait();
                    body(worker);
                }
                catch (Exception exception)
                {
                    exceptions.Enqueue(exception);
                }
            })
            {
                IsBackground = true,
            })
            .ToArray();

        foreach (var thread in threads)
        {
            thread.Start();
        }

        var deadline = DateTime.UtcNow + _roundTimeout;
        foreach (var thread in threads)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero || !thread.Join(remaining))
            {
                throw new TimeoutException($"A thread did not finish within {_roundTimeout}; the registry is probably corrupted.");
            }
        }

        if (!exceptions.IsEmpty)
        {
            throw new AggregateException(exceptions);
        }
    }

    /// <summary>
    /// A private copy of <see cref="TablerSvgRegistry"/>: the assembly is loaded again into an
    /// <see cref="AssemblyLoadContext"/> of its own, so the copy's static table starts empty and nothing
    /// registered through it reaches the process-wide registry that other tests read. That gives every
    /// round a registry that is empty and has to grow, which is when concurrent writes hurt most.
    /// The enum is compiled into the assembly, so the copy has an enum type of its own and icons are
    /// passed as their numbers.
    /// </summary>
    private sealed class IsolatedRegistry
    {
        private readonly Action<int, bool, string> _register;
        private readonly Func<int, TablerSvgEntry?> _tryGet;

        public IsolatedRegistry()
        {
            var assembly = new AssemblyLoadContext(nameof(IsolatedRegistry))
                .LoadFromAssemblyPath(typeof(TablerSvgRegistry).Assembly.Location);
            Assert.NotSame(typeof(TablerSvgRegistry).Assembly, assembly);
            var registryType = assembly.GetType(typeof(TablerSvgRegistry).FullName!)!;
            var iconType = assembly.GetType(typeof(TablerIconType).FullName!)!;
            var entryType = assembly.GetType(typeof(TablerSvgEntry).FullName!)!;

            var iconNumber = Expression.Parameter(typeof(int));
            var icon = Expression.Convert(iconNumber, iconType);

            var isFilled = Expression.Parameter(typeof(bool));
            var inner = Expression.Parameter(typeof(string));
            var register = Expression.Call(registryType.GetMethod(nameof(TablerSvgRegistry.Register))!, icon, isFilled, inner);
            _register = Expression.Lambda<Action<int, bool, string>>(register, iconNumber, isFilled, inner).Compile();

            var entry = Expression.Variable(entryType);
            var tryGet = Expression.Call(registryType.GetMethod(nameof(TablerSvgRegistry.TryGet))!, icon, entry);
            var copiedEntry = Expression.New(
                typeof(TablerSvgEntry).GetConstructor([typeof(bool), typeof(string)])!,
                Expression.Property(entry, nameof(TablerSvgEntry.IsFilled)),
                Expression.Property(entry, nameof(TablerSvgEntry.Inner)));
            var tryGetAndCopy = Expression.Block(
                [entry],
                Expression.Condition(
                    tryGet,
                    Expression.Convert(copiedEntry, typeof(TablerSvgEntry?)),
                    Expression.Default(typeof(TablerSvgEntry?))));
            _tryGet = Expression.Lambda<Func<int, TablerSvgEntry?>>(tryGetAndCopy, iconNumber).Compile();
        }

        public void Register(int iconNumber, bool isFilled, string inner)
        {
            _register(iconNumber, isFilled, inner);
        }

        public TablerSvgEntry? TryGet(int iconNumber)
        {
            return _tryGet(iconNumber);
        }
    }
}
