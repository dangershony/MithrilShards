using Network.Protocol.Transport.Noise;

namespace Network.Test.Protocol.Transport.Noise
{
    public static class Bolt8TestVectorParameters
    {
        private const string InitiatorPrivateKeyHex = "0x1111111111111111111111111111111111111111111111111111111111111111";
        private const string InitiatorPublicKeyHex = "0x034f355bdcb7cc0af728ef3cceb9615d90684bb5b2ca5f859ab0f0b704075871aa";

        private const string ReceiverPrivateKeyHex = "0x2121212121212121212121212121212121212121212121212121212121212121";
        private const string ReceiverPublicKeyHex = "0x028d7500dd4c12685d1f568b4c2b5048e8534b873319f3a8daa612b469132ec7f7";
        
        private const string InitiatorEphemeralPrivateKeyHex = "0x1212121212121212121212121212121212121212121212121212121212121212";
        private const string InitiatorEphemeralPublicKeyHex = "0x036360e856310ce5d294e8be33fc807077dc56ac80d95d9cd4ddbd21325eff73f7";

        private const string ReceiverEphemeralPrivateKeyHex = "0x2222222222222222222222222222222222222222222222222222222222222222";
        private const string ReceiverEphemeralPublicKeyHex = "0x02466d7fcae563e5cb09a0d1870bb580344804617879a14949cf22285f1bae3f27";

        public static KeyPair Initiator =>
                new KeyPair(InitiatorPrivateKeyHex.ToByteArray(),
                InitiatorPublicKeyHex.ToByteArray());

        public static KeyPair InitiatorEphemeralKeyPair =>
            new KeyPair(InitiatorEphemeralPrivateKeyHex.ToByteArray(),
                InitiatorEphemeralPublicKeyHex.ToByteArray());
        
        public static KeyPair Receiver =>
            new KeyPair(ReceiverPrivateKeyHex.ToByteArray(),
                ReceiverPublicKeyHex.ToByteArray());
        
        public static KeyPair ReceiverEphemeralKeyPair =>
            new KeyPair(ReceiverEphemeralPrivateKeyHex.ToByteArray(),
                ReceiverEphemeralPublicKeyHex.ToByteArray());

        public static class ActOne
        {
            public const string EndStateHash =
                "0x9d1ffbb639e7e20021d9259491dc7b160aab270fb1339ef135053f6f2cebe9ce";

            public const string InitiatorOutput =
                "0x00036360e856310ce5d294e8be33fc807077dc56ac80d95d9cd4ddbd21325eff73f70df6086551151f58b8afe6c195782c6a";
        }

        public static class ActTwo
        {
            public const string EndStateHash =
                "0x90578e247e98674e661013da3c5c1ca6a8c8f48c90b485c0dfa1494e23d56d72";

            public const string ResponderOutput =
                "0x0002466d7fcae563e5cb09a0d1870bb580344804617879a14949cf22285f1bae3f276e2470b93aac583c9ef6eafca3f730ae";
        }

        public static class ActThree
        {
            public const string EndStateHash =
                "0x5dcb5ea9b4ccc755e0e3456af3990641276e1d5dc9afd82f974d90a47c918660";

            public const string InitiatorOutput =
                "0x00b9e3a702e93e3a9948c2ed6e5fd7590a6e1c3a0344cfc9d5b57357049aa22355361aa02e55a8fc28fef5bd6d71ad0c38228dc68b1c466263b47fdf31e560e139ba";
        }
    }
}