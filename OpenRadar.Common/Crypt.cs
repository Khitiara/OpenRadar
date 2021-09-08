using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using OpenRadar.Common.Properties;

namespace FreeRadar.Common
{
    public static class Crypt
    {
        private static X509Certificate2? _rootCert;

        private static void EnsureCertInit() {
            LazyInitializer.EnsureInitialized(ref _rootCert, LoadRootCert);
        }

        private static X509Certificate2 LoadRootCert() {
            return new X509Certificate2(Resources.RootCert, "freeradar");
        }

        public static void DisposeRootCert() {
            _rootCert?.Dispose();
        }

        public static bool VerifyRemoteCert(object sender, X509Certificate? cert, X509Chain? chain,
            SslPolicyErrors errors) {
            EnsureCertInit();
            X509Chain newChain = new();
            newChain.ChainPolicy.ExtraStore.Add(_rootCert!);
            newChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            newChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            using X509Certificate2 cert2 = new(cert ?? throw new ArgumentNullException(nameof(cert)));
            newChain.Build(cert2);
            if (newChain.ChainStatus.Length == 0)
                return true;
            return newChain.ChainStatus[0].Status == X509ChainStatusFlags.NoError;
        }
    }
}