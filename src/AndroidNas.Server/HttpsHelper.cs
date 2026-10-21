using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AndroidNas.Server;

public static class HttpsHelper
{
    public static X509Certificate2 GenerateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("cn=AndroidNAS", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        sanBuilder.AddIpAddress(System.Net.IPAddress.Any);
        req.CertificateExtensions.Add(sanBuilder.Build());

        var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(10));
        return new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }
}
