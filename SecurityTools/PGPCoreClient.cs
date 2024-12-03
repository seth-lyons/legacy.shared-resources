using PgpCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SharedResources
{
    public class PGPCoreClient : IDisposable
    {
        private readonly EncryptionSettings _encryptionSettings;

        private readonly Stream _publicKeyStream;
        private readonly Stream _privateKeyStream;
        private readonly PGP _pgp;

        public PGPCoreClient(EncryptionSettings encryptionSettings)
        {
            _encryptionSettings = encryptionSettings;
            _publicKeyStream = _encryptionSettings?.Base64PublicKey == null ? null : new MemoryStream(Convert.FromBase64String(_encryptionSettings.Base64PublicKey));
            _privateKeyStream = _encryptionSettings?.Base64PrivateKey == null ? null : new MemoryStream(Convert.FromBase64String(_encryptionSettings.Base64PrivateKey));

            var keys = new EncryptionKeys(_publicKeyStream, _privateKeyStream, _encryptionSettings?.PrivateKeyPassword ?? "");

            _pgp = new PGP(keys);
            _encryptionSettings = encryptionSettings;
        }

        public async Task<Stream> Decrypt(Stream file, bool verify = false)
        {
            var outstream = new MemoryStream();
            if (verify)
                await _pgp.DecryptStreamAndVerifyAsync(file, outstream);
            else
                await _pgp.DecryptStreamAsync(file, outstream);
            return outstream;
        }

        public async Task<byte[]> Decrypt(byte[] file, bool verify = false)
        {
            using (var instream = new MemoryStream(file))
            using (var outstream = new MemoryStream())
            {
                if (verify)
                    await _pgp.DecryptStreamAndVerifyAsync(instream, outstream);
                else
                    await _pgp.DecryptStreamAsync(instream, outstream);
                return outstream.ToArray();
            }
        }

        public async Task<Stream> Encrypt(Stream file, bool sign = false, bool armor = true, bool withIntegrityCheck = true, string name = "name")
        {
            var outstream = new MemoryStream();
            if (sign)
                await _pgp.EncryptStreamAndSignAsync(file, outstream, armor, withIntegrityCheck, name);
            else
                await _pgp.EncryptStreamAsync(file, outstream, armor, withIntegrityCheck, name);
            return outstream;
        }

        public async Task<byte[]> Encrypt(byte[] file, bool sign = false, bool armor = true, bool withIntegrityCheck = true, string name = "name")
        {
            using (var instream = new MemoryStream(file))
            using (var outstream = new MemoryStream())
            {
                if (sign)
                    await _pgp.EncryptStreamAndSignAsync(instream, outstream, armor, withIntegrityCheck, name);
                else
                    await _pgp.EncryptStreamAsync(instream, outstream, armor, withIntegrityCheck, name);
                return outstream.ToArray();
            }
        }

        public void Decrypt(Stream input, Stream output)
        {
            _pgp.DecryptStream(input, output);
        }

        public void Dispose()
        {
            _publicKeyStream.Dispose();
            _privateKeyStream.Dispose();
            _pgp.Dispose();
        }
    }
}
