using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Infrastructure.Outbox;

namespace ProjectManagementSystem.Infrastructure.Tests.Outbox;

/// <summary>
/// The loop of the processor with a scripted drainer instead of the database and a fake clock. A test waits for each
/// drain it expects; "no drain" is checked after giving the loop a moment to run.
/// </summary>
public sealed class OutboxProcessorTests : IAsyncDisposable
{
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero));
    private readonly OutboxSignal _signal = new();
    private readonly ScriptedDrainer _drainer = new();
    private readonly OutboxProcessor _processor;

    public OutboxProcessorTests()
    {
        _processor = new OutboxProcessor(
            _drainer, _signal, _clock, Options.Create(new OutboxOptions()), NullLogger<OutboxProcessor>.Instance);
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    [Fact]
    public async Task Drains_at_start_and_then_only_on_a_signal()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(returns: null);

        await AdvanceAsync(TimeSpan.FromDays(1));
        await AssertNoDrainAsync();

        _signal.Notify();
        await _drainer.NextDrainAsync(returns: null);
        Assert.Equal(2, _drainer.Calls);
    }

    [Fact]
    public async Task Waits_for_the_next_attempt_of_a_failed_message()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(returns: Now.AddSeconds(10));

        await AdvanceAsync(TimeSpan.FromSeconds(9));
        await AssertNoDrainAsync();

        await AdvanceAsync(TimeSpan.FromSeconds(1));
        await _drainer.NextDrainAsync(returns: null);
    }

    [Fact]
    public async Task A_signal_ends_the_wait_for_the_next_attempt_early()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(returns: Now.AddHours(1));

        _signal.Notify();
        await _drainer.NextDrainAsync(returns: null);
    }

    [Fact]
    public async Task A_signal_sent_during_a_drain_is_not_lost()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(returns: null, during: _signal.Notify);

        await _drainer.NextDrainAsync(returns: null);
    }

    [Fact]
    public async Task A_failed_drain_does_not_stop_the_loop_and_is_retried_by_its_own_backoff()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(fails: true);

        await AdvanceAsync(TimeSpan.FromSeconds(4));
        await AssertNoDrainAsync();
        await AdvanceAsync(TimeSpan.FromSeconds(1));
        await _drainer.NextDrainAsync(fails: true);

        await AdvanceAsync(TimeSpan.FromSeconds(29));
        await AssertNoDrainAsync();
        await AdvanceAsync(TimeSpan.FromSeconds(1));
        await _drainer.NextDrainAsync(returns: null);

        Assert.False(_processor.ExecuteTask!.IsCompleted);
    }

    [Fact]
    public async Task The_failure_count_is_reset_after_a_successful_drain()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(fails: true);
        await AdvanceAsync(TimeSpan.FromSeconds(5));
        await _drainer.NextDrainAsync(fails: true);
        await AdvanceAsync(TimeSpan.FromSeconds(30));
        await _drainer.NextDrainAsync(returns: null);

        _signal.Notify();
        await _drainer.NextDrainAsync(fails: true);

        await AdvanceAsync(TimeSpan.FromSeconds(5));
        await _drainer.NextDrainAsync(returns: null);
    }

    [Fact]
    public async Task Signals_do_not_cut_the_wait_after_a_failed_drain_short_but_are_kept()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(fails: true);

        _signal.Notify();
        await AssertNoDrainAsync();

        await AdvanceAsync(TimeSpan.FromSeconds(5));
        await _drainer.NextDrainAsync(returns: null);
        await _drainer.NextDrainAsync(returns: null);
    }

    [Fact]
    public async Task Stopping_ends_the_loop_without_an_error()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(returns: null);

        await _processor.StopAsync(CancellationToken.None);

        Assert.True(_processor.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Stopping_during_a_drain_ends_the_loop_without_an_error()
    {
        await StartAsync();
        await _drainer.NextDrainAsync(waitsForCancellation: true);

        await _processor.StopAsync(CancellationToken.None);

        Assert.True(_processor.ExecuteTask!.IsCompletedSuccessfully);
    }

    public async ValueTask DisposeAsync()
    {
        await _processor.StopAsync(CancellationToken.None);
        _processor.Dispose();
    }

    private Task StartAsync() => _processor.StartAsync(CancellationToken.None);

    /// <summary>
    /// Lets the loop reach its wait before the time moves, then moves it.
    /// </summary>
    private async Task AdvanceAsync(TimeSpan time)
    {
        await Task.Delay(Settle);
        _clock.Advance(time);
    }

    private async Task AssertNoDrainAsync()
    {
        var calls = _drainer.Calls;
        await Task.Delay(Settle);
        Assert.Equal(calls, _drainer.Calls);
    }

    /// <summary>
    /// Each drain blocks until the test says how it ends, so the test sees every drain one by one.
    /// </summary>
    private sealed class ScriptedDrainer : IOutboxDrainer
    {
        private readonly Channel<Drain> _drains = Channel.CreateUnbounded<Drain>();
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public async Task<DateTime?> DrainAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            var drain = new Drain(cancellationToken);
            await _drains.Writer.WriteAsync(drain, cancellationToken);

            return await drain.Result.Task;
        }

        /// <summary>
        /// Waits for the next drain and ends it as given; with <paramref name="waitsForCancellation"/> the drain keeps
        /// running until the processor is stopped.
        /// </summary>
        public async Task NextDrainAsync(
            DateTime? returns = null,
            bool fails = false,
            Action? during = null,
            bool waitsForCancellation = false)
        {
            using var timeout = new CancellationTokenSource(Timeout);
            var drain = await _drains.Reader.ReadAsync(timeout.Token);

            during?.Invoke();

            if (waitsForCancellation)
            {
                drain.CancellationToken.Register(() => drain.Result.TrySetCanceled(drain.CancellationToken));
                return;
            }

            if (fails)
                drain.Result.SetException(new InvalidOperationException("Database is not available."));
            else
                drain.Result.SetResult(returns);
        }

        private sealed record Drain(CancellationToken CancellationToken)
        {
            public TaskCompletionSource<DateTime?> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
