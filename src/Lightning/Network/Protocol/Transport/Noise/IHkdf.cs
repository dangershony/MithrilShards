using System;

namespace Network.Protocol.Transport.Noise
{
    internal interface IHkdf
    {
        void ExtractAndExpand2(ReadOnlySpan<byte> chainingKey, ReadOnlySpan<byte> inputKeyMaterial, Span<byte> output);
        void ExtractAndExpand3(ReadOnlySpan<byte> chainingKey, ReadOnlySpan<byte> inputKeyMaterial, Span<byte> output);
    }
}