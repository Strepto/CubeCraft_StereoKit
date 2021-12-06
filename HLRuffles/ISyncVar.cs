using System;

namespace StereoKitApp.HLRuffles
{
    public interface ISyncVar
    {
        void SetNetworkValue(ArraySegment<byte> newValue);
        void UpdateIfChangedOptional();
    }
}
