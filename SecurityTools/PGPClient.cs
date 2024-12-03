using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.IO;
using System.Linq;

namespace SharedResources
{
    public class PGPClient
    {
        private readonly byte[] _secretKey;
        private readonly byte[] _publicKey;
        private readonly char[] _keyPass;

        public PGPClient(EncryptionSettings settings)
        {
            _publicKey = string.IsNullOrEmpty(settings.Base64PublicKey) ? null : Convert.FromBase64String(settings.Base64PublicKey);
            _secretKey = string.IsNullOrEmpty(settings.Base64PrivateKey) ? null : Convert.FromBase64String(settings.Base64PrivateKey);
            _keyPass = settings.PrivateKeyPassword?.ToCharArray();
        }

        public Stream Decrypt(byte[] fileData)
        {
            try
            {
                // PgpObjectFactory factory = new PgpObjectFactory(PgpUtilities.GetDecoderStream(inputStream));                
                PgpObjectFactory factory = new PgpObjectFactory(fileData);
                PgpObject obj = null;
                do
                    obj = factory.NextPgpObject();
                while (!(obj is PgpEncryptedDataList) && obj != null);

                PgpPrivateKey pKey = null;
                PgpSecretKeyRingBundle pgpSec = null;
                using (var stream = new MemoryStream(_secretKey))
                    pgpSec = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(stream));
                PgpPublicKeyEncryptedData pbe = ((PgpEncryptedDataList)obj)
                    ?.GetEncryptedDataObjects()
                    ?.Cast<PgpPublicKeyEncryptedData>()
                    ?.FirstOrDefault(pked =>
                    {
                        pKey = pgpSec.GetSecretKey(pked.KeyId)?.ExtractPrivateKey(_keyPass);
                        if (pKey != null)
                            return true;
                        return false;
                    });

                if (pKey == null)
                    throw new ArgumentException("Secret key for message not found.");

                PgpObject message = null;
                using (Stream clear = pbe.GetDataStream(pKey))
                {
                    PgpObjectFactory plainFact = new PgpObjectFactory(clear);
                    message = plainFact.NextPgpObject();

                    if (message is PgpOnePassSignatureList) //IGNORE SIG FOR NOW
                        message = plainFact.NextPgpObject();
                }

                if (message is PgpCompressedData)
                {
                    PgpCompressedData cData = (PgpCompressedData)message;
                    PgpObjectFactory of = null;

                    using (Stream compDataIn = cData.GetDataStream())
                        of = new PgpObjectFactory(compDataIn);

                    message = of.NextPgpObject();
                    PgpLiteralData literalData = message is PgpOnePassSignatureList ? (PgpLiteralData)of.NextPgpObject() : (PgpLiteralData)message; //IGNORE SIG FOR NOW
                    return literalData.GetInputStream();
                }
                else if (message is PgpLiteralData)
                {
                    PgpLiteralData literalData = (PgpLiteralData)message;
                    return literalData.GetInputStream();
                }
                else if (message is PgpOnePassSignatureList)
                {
                    throw new PgpException("Encrypted message contains a signed message - not literal data.");
                }
                else
                    throw new PgpException("Message is not a simple encrypted file - type unknown.");
            }
            catch (PgpException ex)
            {
                throw;
            }
        }
    }
}
