using System;
using System.Collections.Generic;

namespace StereoKitApp.HLRuffles
{
    public class SyncVar<T> : ISyncVar, IDisposable where T : struct
    {
        public Action<T>? ValueReceived;

        private T _value;
        private readonly IEqualityComparer<T> _equalityComparer;

        /// <summary>
        /// Set the synced value. It will be automatically synced
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (_equalityComparer.Equals(_value, value))
                    return;

                _value = value;
                SendNetworkUpdate();
            }
        }

        public int Id { get; }

        public SyncVar(int id, T? startValue = null, IEqualityComparer<T>? equalityComparer = null)
        {
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default; // Comparing structs might be slow or inaccurate. A separate Equality Comparer can be added for fast comparisons.
            Id = id;
            if (startValue != null)
                _value = startValue.Value;
            RegisterSelf();
        }

        private void RegisterSelf()
        {
            RufflesTransport.Singleton.RegisterSyncVar(Id, this);
        }

        public void SetNetworkValue(ArraySegment<byte> newValue)
        {
            var newValueParsed = NetworkReader.ReadData<T>(newValue, 4);
            if (!_equalityComparer.Equals(_value, newValueParsed))
            {
                _value = newValueParsed;
                ValueReceived?.Invoke(_value);
            }
        }

        public void UpdateIfChangedOptional()
        {
            // Not implemented for SyncVar
        }

        private void SendNetworkUpdate()
        {
            RufflesTransport.Singleton.SendData(NetworkWriter.WriteData(_value, Id));
        }

        public void Dispose()
        {
            RufflesTransport.Singleton.UnRegisterSyncVar(Id);
        }
    }
}
