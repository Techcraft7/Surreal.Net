using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading.Channels;

using SurrealDB.Common;

namespace SurrealDB.Ws;

/// <summary>Sends messages from a channel to a websocket server.</summary>
public struct WsRxConsumer : IDisposable {
    private readonly ClientWebSocket _ws;
    private readonly ChannelReader<BufferStreamReader> _in;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _execute;

    private readonly int _blockSize;

    public WsRxConsumer(ClientWebSocket ws, ChannelReader<BufferStreamReader> @in, int blockSize) {
        _ws = ws;
        _in = @in;
        _blockSize = blockSize;
    }

    private async Task Execute(CancellationToken ct) {
        Debug.Assert(ct.CanBeCanceled);
        while (!ct.IsCancellationRequested) {
            await Consume(ct).Inv();
            ct.ThrowIfCancellationRequested();
        }
    }

    private async Task Consume(CancellationToken ct) {
        using var reader = await _in.ReadAsync(ct).Inv();

        bool isFinalBlock = false;
        while (!isFinalBlock && !ct.IsCancellationRequested) {
            var rom = await reader.ReadAsync(_blockSize, ct).Inv();
            isFinalBlock = rom.Length != _blockSize;
            await _ws.SendAsync(rom, WebSocketMessageType.Text, isFinalBlock, ct).Inv();
        }

        if (!isFinalBlock) {
            // ensure that the message is always terminated
            // no not pass a CancellationToken
            await _ws.SendAsync(default, WebSocketMessageType.Text, true, default).Inv();
        }
    }

    [MemberNotNullWhen(true, nameof(_cts)), MemberNotNullWhen(true, nameof(_execute))]
    public bool Connected => _cts is not null & _execute is not null;

    public void Open() {
        lock (_lock) {
            ThrowIfConnected();
            _cts = new();
            _execute = Execute(_cts.Token);
        }
    }

    public Task Close() {
        Task task;
        lock (_lock) {
            ThrowIfDisconnected();
            task = _execute;
            _cts.Cancel();
            _cts.Dispose(); // not relly needed here
            _cts = null;
            _execute = null;
        }
        return task;
    }

    [MemberNotNull(nameof(_cts)), MemberNotNull(nameof(_execute))]
    private void ThrowIfDisconnected() {
        if (!Connected) {
            throw new InvalidOperationException("The connection is not open.");
        }
    }

    private void ThrowIfConnected() {
        if (Connected) {
            throw new InvalidOperationException("The connection is already open");
        }
    }

    public void Dispose() {
        _cts?.Dispose();
    }
}
