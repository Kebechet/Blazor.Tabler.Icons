using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
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
    public void RegisterRange_ThenTryGet_ReturnsEveryStoredEntry()
    {
        // Arrange
        var registrations = GetBrandRegistrations(skip: 50, take: 3, inner: "<path d=\"M2 2\" />");

        // Act
        TablerSvgRegistry.RegisterRange(registrations);

        // Assert
        foreach (var registration in registrations)
        {
            Assert.True(TablerSvgRegistry.TryGet(registration.Type, out var entry));
            Assert.Equal(new TablerSvgEntry(registration.IsFilled, registration.Inner), entry);
        }
    }

    [Fact]
    public void RegisterRange_SameIconTwiceInOneBatch_KeepsLastWrite()
    {
        // Arrange
        var brandIcon = GetBrandRegistrations(skip: 54, take: 1, inner: string.Empty).Single().Type;
        TablerSvgRegistration[] registrations =
        [
            new(brandIcon, IsFilled: false, Inner: "<path d=\"first\" />"),
            new(brandIcon, IsFilled: true, Inner: "<path d=\"second\" />"),
        ];

        // Act
        TablerSvgRegistry.RegisterRange(registrations);

        // Assert
        Assert.True(TablerSvgRegistry.TryGet(brandIcon, out var entry));
        Assert.Equal(new TablerSvgEntry(IsFilled: true, Inner: "<path d=\"second\" />"), entry);
    }

    [Fact]
    public void RegisterRange_NotifiesOnce_ForTheWholeBatch()
    {
        // Arrange
        var registrations = GetBrandRegistrations(skip: 55, take: 20, inner: "<path d=\"M3 3\" />");

        // Act
        var notificationCount = CountNotificationsDuring(() => TablerSvgRegistry.RegisterRange(registrations));

        // Assert
        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public void RegisterRange_NotifiesNothing_WhenNothingChanges()
    {
        // Arrange
        var registrations = GetBrandRegistrations(skip: 75, take: 5, inner: "<path d=\"M4 4\" />");
        TablerSvgRegistry.RegisterRange(registrations);

        // Act
        var unchangedNotificationCount = CountNotificationsDuring(() => TablerSvgRegistry.RegisterRange(registrations));
        var emptyNotificationCount = CountNotificationsDuring(() => TablerSvgRegistry.RegisterRange([]));

        // Assert
        Assert.Equal(0, unchangedNotificationCount);
        Assert.Equal(0, emptyNotificationCount);
    }

    [Fact]
    public void Register_NotifiesOnce_AndOnlyWhenTheEntryChanges()
    {
        // Arrange
        var brandIcon = GetBrandRegistrations(skip: 80, take: 1, inner: string.Empty).Single().Type;

        // Act
        var firstNotificationCount = CountNotificationsDuring(() => TablerSvgRegistry.Register(brandIcon, isFilled: false, inner: "<path d=\"M5 5\" />"));
        var repeatedNotificationCount = CountNotificationsDuring(() => TablerSvgRegistry.Register(brandIcon, isFilled: false, inner: "<path d=\"M5 5\" />"));
        var changedNotificationCount = CountNotificationsDuring(() => TablerSvgRegistry.Register(brandIcon, isFilled: false, inner: "<path d=\"M6 6\" />"));

        // Assert
        Assert.Equal(1, firstNotificationCount);
        Assert.Equal(0, repeatedNotificationCount);
        Assert.Equal(1, changedNotificationCount);
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

    [Fact]
    public void GetEntries_WhileOtherThreadsRegister_ReturnsAConsistentSnapshot()
    {
        for (var round = 0; round < RoundCount; round++)
        {
            // Arrange
            var registry = new IsolatedRegistry();
            Register(registry, _iconNumbers[0]);

            using var writerDone = new ManualResetEventSlim();

            // Act - the sprite sheet reads every entry while a module initializer is still registering
            RunConcurrently(2, worker =>
            {
                if (worker == 0)
                {
                    try
                    {
                        foreach (var iconNumber in _iconNumbers)
                        {
                            Register(registry, iconNumber);
                        }
                    }
                    finally
                    {
                        writerDone.Set();
                    }

                    return;
                }

                do
                {
                    var snapshot = registry.GetEntries();

                    // Present before the round started: must stay in every snapshot.
                    Assert.Contains(snapshot, x => x.IconNumber == _iconNumbers[0]);

                    var snapshotIconNumbers = snapshot
                        .Select(x => x.IconNumber)
                        .ToList();
                    Assert.Equal(snapshotIconNumbers.Distinct().OrderBy(x => x), snapshotIconNumbers);

                    // Written while the snapshot was taken: whatever is in it is complete.
                    foreach (var (iconNumber, entry) in snapshot)
                    {
                        Assert.Equal(GetExpectedEntry(iconNumber), entry);
                    }
                }
                while (!writerDone.IsSet);
            });

            // Assert
            Assert.Equal(_iconNumbers, registry.GetEntries().Select(x => x.IconNumber));
        }
    }

    [Fact]
    public void GetEntries_ReturnsEverySortedEntry_AsASnapshotThatLaterRegistrationsDoNotChange()
    {
        // Arrange
        var registry = new IsolatedRegistry();
        foreach (var iconNumber in _iconNumbers.Take(3).Reverse())
        {
            Register(registry, iconNumber);
        }

        // Act
        var snapshot = registry.GetEntries();
        Register(registry, _iconNumbers[3]);

        // Assert
        Assert.Equal(_iconNumbers.Take(3), snapshot.Select(x => x.IconNumber));
        Assert.All(snapshot, x => Assert.Equal(GetExpectedEntry(x.IconNumber), x.Entry));
        Assert.Equal(_iconNumbers.Take(4), registry.GetEntries().Select(x => x.IconNumber));
    }

    /// <summary>
    /// Brand icons no other test reads as unregistered. The slices the tests take are disjoint, and
    /// disjoint from the ones <see cref="TablerIconSpriteTests"/> takes, so registering them
    /// process-wide disturbs nothing.
    /// </summary>
    private static TablerSvgRegistration[] GetBrandRegistrations(int skip, int take, string inner)
    {
        return Enum.GetValues<TablerIconType>()
            .Where(x => x.ToString().StartsWith("Brand", StringComparison.Ordinal))
            .Skip(skip)
            .Take(take)
            .Select(x => new TablerSvgRegistration(x, IsFilled: false, Inner: inner))
            .ToArray();
    }

    private static int CountNotificationsDuring(Action registration)
    {
        var notificationCount = 0;
        Action onIconRegistered = () => notificationCount++;
        TablerSvgRegistry.IconRegistered += onIconRegistered;
        try
        {
            registration();
        }
        finally
        {
            TablerSvgRegistry.IconRegistered -= onIconRegistered;
        }

        return notificationCount;
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
        private readonly Func<Array> _getEntries;
        private readonly PropertyInfo _pairIcon;
        private readonly PropertyInfo _pairEntry;
        private readonly PropertyInfo _entryIsFilled;
        private readonly PropertyInfo _entryInner;

        public IsolatedRegistry()
        {
            var assembly = new AssemblyLoadContext(nameof(IsolatedRegistry))
                .LoadFromAssemblyPath(typeof(TablerSvgRegistry).Assembly.Location);
            Assert.NotSame(typeof(TablerSvgRegistry).Assembly, assembly);
            var registryType = assembly.GetType(typeof(TablerSvgRegistry).FullName!)!;
            var iconType = assembly.GetType(typeof(TablerIconType).FullName!)!;
            var entryType = assembly.GetType(typeof(TablerSvgEntry).FullName!)!;

            _getEntries = registryType
                .GetMethod(nameof(TablerSvgRegistry.GetEntries), BindingFlags.NonPublic | BindingFlags.Static)!
                .CreateDelegate<Func<Array>>();
            var pairType = typeof(KeyValuePair<,>).MakeGenericType(iconType, entryType);
            _pairIcon = pairType.GetProperty(nameof(KeyValuePair<int, int>.Key))!;
            _pairEntry = pairType.GetProperty(nameof(KeyValuePair<int, int>.Value))!;
            _entryIsFilled = entryType.GetProperty(nameof(TablerSvgEntry.IsFilled))!;
            _entryInner = entryType.GetProperty(nameof(TablerSvgEntry.Inner))!;

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

        public IReadOnlyList<(int IconNumber, TablerSvgEntry Entry)> GetEntries()
        {
            return _getEntries()
                .Cast<object>()
                .Select(pair =>
                {
                    var entry = _pairEntry.GetValue(pair)!;
                    return (
                        Convert.ToInt32(_pairIcon.GetValue(pair)),
                        new TablerSvgEntry((bool)_entryIsFilled.GetValue(entry)!, (string)_entryInner.GetValue(entry)!));
                })
                .ToList();
        }

        public TablerSvgEntry? TryGet(int iconNumber)
        {
            return _tryGet(iconNumber);
        }
    }
}
