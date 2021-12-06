using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Ruffles.Channeling;
using Ruffles.Configuration;
using Ruffles.Connections;
using Ruffles.Core;
using StereoKit.Framework;

namespace StereoKitApp.HLRuffles
{
    public class RufflesTransport : IStepper
    {
        public static RufflesTransport Singleton { get; private set; } = null!;

        public Action<ulong>? OnPeerConnected = default!;
        public Action<ulong>? OnPeerDisconnected = default!;
        public Action? OnStartingSession = default!;

        public HashSet<ulong> PeerIds { get; } = new HashSet<ulong>();

        private Stopwatch _update = Stopwatch.StartNew();
        private double _lastUpdate = -1000;

        private ulong _receivedMessageCounter = 0;
        private ulong _sentMessageCounter = 0;

        public bool IsRunning => _ruffleSocket?.IsRunning ?? false;
        public bool IsHost { get; private set; }
        public ConnectionState ConnectionState =>
            _hostConnection?.State ?? ConnectionState.Disconnected;

        private RuffleSocket? _ruffleSocket;
        private Dictionary<ulong, Connection> _connections = new Dictionary<ulong, Connection>();

        private Connection? _hostConnection;

        private readonly byte[] _messageBuffer;
        private WeakReference? _temporaryBufferReference;

        private Dictionary<int, ISyncVar> _syncVars = new Dictionary<int, ISyncVar>();

        private bool ShouldTick()
        {
            // TODO: Dont use me. Multiple of these on same frame will not work.
            if (_update.ElapsedMilliseconds > _lastUpdate + 1000 / 60f)
            {
                _lastUpdate = _update.ElapsedMilliseconds;
                return true;
            }

            return false;
        }

        public void RegisterSyncVar(int id, ISyncVar syncVar)
        {
            if (_syncVars.ContainsKey(id))
                throw new ArgumentException($"Network ID {id} is already in use");
            _syncVars.Add(id, syncVar);
        }

        public void UnRegisterSyncVar(int id)
        {
            if (_syncVars.ContainsKey(id))
                _syncVars.Remove(id);
        }

        public RufflesTransport()
        {
            Singleton = this;
            _messageBuffer = new byte[1024 * 8];
        }

        private SocketConfig GetConfig(ushort port)
        {
            SocketConfig config = new SocketConfig();
            config.ChannelTypes = new[]
            {
                ChannelType.Reliable,
                ChannelType.Unreliable,
                ChannelType.UnreliableOrdered,
                ChannelType.ReliableSequenced,
                ChannelType.UnreliableRaw,
                ChannelType.ReliableSequencedFragmented,
                ChannelType.ReliableOrdered,
                ChannelType.ReliableFragmented
            };
            config.DualListenPort = port;
            config.EnablePacketMerging = false;

            return config;
        }

        public bool CreateSession(ushort port)
        {
            _ruffleSocket?.Shutdown();
            _ruffleSocket = new RuffleSocket(GetConfig(port));

            if (!_ruffleSocket.Start())
                return false;

            IsHost = true;
            OnStartingSession?.Invoke();
            return true;
        }

        public void JoinSession(IPEndPoint endPoint)
        {
            IsHost = false;
            _ruffleSocket = new RuffleSocket(GetConfig(0));
            _ruffleSocket.Start();
            _hostConnection = _ruffleSocket.Connect(endPoint);
            OnStartingSession?.Invoke();
        }

        public void Stop()
        {
            IsHost = false;
            _ruffleSocket?.Shutdown();
            PeerIds.Clear();
            _connections.Clear();
            _hostConnection = null;
        }

        /// <summary>
        /// Check for new events waiting.
        /// If we found data, returns true
        /// If not returns false.
        /// </summary>
        /// <returns></returns>
        public bool PollEvent()
        {
            if (_ruffleSocket == null || !IsRunning)
                return false;

            NetworkEvent networkEvent = _ruffleSocket.Poll();

            try
            {
                if (networkEvent.Type == NetworkEventType.Nothing)
                {
                    // We have no more data in the event buffer
                    return false;
                }

                byte[] dataBuffer = _messageBuffer;

                if (networkEvent.Type == NetworkEventType.Data)
                {
                    _receivedMessageCounter++;
                    if (networkEvent.Data.Count > _messageBuffer.Length)
                    {
                        if (
                            _temporaryBufferReference != null
                            && _temporaryBufferReference.IsAlive
                            && ((byte[])_temporaryBufferReference.Target!).Length
                                >= networkEvent.Data.Count
                        )
                        {
                            dataBuffer = (byte[])_temporaryBufferReference.Target;
                        }
                        else
                        {
                            dataBuffer = new byte[networkEvent.Data.Count];
                            _temporaryBufferReference = new WeakReference(dataBuffer);
                        }
                    }

                    Buffer.BlockCopy(
                        networkEvent.Data.Array!,
                        networkEvent.Data.Offset,
                        dataBuffer,
                        0,
                        networkEvent.Data.Count
                    );

                    var payload = new ArraySegment<byte>(dataBuffer, 0, networkEvent.Data.Count);

                    if (IsHost)
                        SendDataWithExcludedId(payload, networkEvent.Connection.Id);

                    _syncVars.TryGetValue(NetworkReader.ReadData<int>(payload, 0), out var syncVar);
                    syncVar?.SetNetworkValue(payload);
                    return true;
                }

                switch (networkEvent.Type)
                {
                    case NetworkEventType.Data:
                        break;
                    case NetworkEventType.Connect:
                        _connections.Add(networkEvent.Connection.Id, networkEvent.Connection);
                        PeerIds.Add(networkEvent.Connection.Id);
                        OnPeerConnected?.Invoke(networkEvent.Connection.Id);
                        break;
                    case NetworkEventType.Timeout:
                    case NetworkEventType.Disconnect:
                        OnPeerDisconnected?.Invoke(networkEvent.Connection.Id);
                        _connections.Remove(networkEvent.Connection.Id);
                        PeerIds.Remove(networkEvent.Connection.Id);
                        break;
                    default:
                        break;
                }

                return networkEvent.Type != NetworkEventType.Nothing;
            }
            finally
            {
                networkEvent.Recycle();
            }
        }

        public void SendData(ArraySegment<byte> data)
        {
            foreach (var connection in _connections.Values)
            {
                connection.Send(
                    data,
                    (byte)ChannelType.ReliableFragmented,
                    false,
                    _sentMessageCounter++
                );
            }
        }

        public void SendDataToId(ArraySegment<byte> data, ulong clientId)
        {
            _connections[clientId].Send(
                data,
                (byte)ChannelType.ReliableFragmented,
                false,
                _sentMessageCounter++
            );
        }

        public void SendDataWithExcludedId(ArraySegment<byte> data, ulong exludeId)
        {
            foreach (var connection in _connections.Values)
            {
                if (connection.Id == exludeId)
                    continue;

                connection.Send(
                    data,
                    (byte)ChannelType.ReliableFragmented,
                    false,
                    _sentMessageCounter++
                );
            }
        }

        public bool Initialize()
        {
            return true;
        }

        private readonly Stopwatch _frameTimer = new Stopwatch();

        public void Step()
        {
            while (PollEvent())
            {
                // Poll! :)
            }

            if (ShouldTick())
            {
                _frameTimer.Restart();
                foreach (var value in _syncVars.Values)
                {
                    value.UpdateIfChangedOptional();
                }

                _frameTimer.Stop();
                if (_frameTimer.ElapsedMilliseconds > 1)
                {
                    Console.WriteLine(
                        "Long running frame: " + _frameTimer.ElapsedMilliseconds + "ms"
                    );
                }
            }

            if (_hostConnection?.State == ConnectionState.Disconnected)
            {
                Stop();
            }
        }

        public void Shutdown() { }

        public bool Enabled { get; }
    }
}
