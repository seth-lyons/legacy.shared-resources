using EllieMae.Encompass.Client;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharedResources
{
    internal static class SecretCDOEncryption
    {
        private static byte[] TransformBytes(byte[] data, string encryptionKey, byte[] salt, bool decrypt = true)
        {
            byte[] bytes = null;
            var key = new Rfc2898DeriveBytes(encryptionKey, salt, 32768);

            using (Aes aes = new AesManaged() { KeySize = 256 })
            using (MemoryStream ms = new MemoryStream())
            {
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (CryptoStream cs = new CryptoStream(ms, decrypt ? aes.CreateDecryptor() : aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Close();
                }
                bytes = ms.ToArray();
            }
            return bytes;
        }

        private static (byte[] Data, byte[] Salt) EncryptBytes(byte[] data, string encryptionKey)
        {
            var salt = new byte[24];
            new RNGCryptoServiceProvider().GetBytes(salt);
            return (TransformBytes(data, encryptionKey, salt, false), salt);
        }

        private static (string Base64Data, string Base64Salt) Encrypt(string data, string encryptionKey, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            var salt = new byte[24];
            new RNGCryptoServiceProvider().GetBytes(salt);
            return (Convert.ToBase64String(TransformBytes(encoding.GetBytes(data), encryptionKey, salt, false)), Convert.ToBase64String(salt));
        }

        private static (byte[] Data, byte[] Salt) EncryptToBytes(string data, string encryptionKey, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            var salt = new byte[24];
            new RNGCryptoServiceProvider().GetBytes(salt);
            return (TransformBytes(encoding.GetBytes(data), encryptionKey, salt, false), salt);
        }

        private static string Decrypt(string base64Data, string base64Salt, string encryptionKey, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            var decryptedData = TransformBytes(Convert.FromBase64String(base64Data), encryptionKey, Convert.FromBase64String(base64Salt));
            return encoding.GetString(decryptedData);
        }

        internal static void SetSecretsGlobalCDO(this Session session, JObject secrets, string encryptionKey)
        {
            var variables = session.GetGlobalCDO("EncompassVariables.json") as JObject;

            var securedVariables = EncryptToBytes(secrets.ToString(), encryptionKey);
            variables["Base64SecretsKey"] = encryptionKey;
            variables["Base64SecretsSalt"] = Convert.ToBase64String(securedVariables.Salt);

            session.SaveGlobalCDO(variables, "EncompassVariables.json");
            session.SaveGlobalCDO(securedVariables.Data, "EncompassVariables.json.sec");
        }

        internal static JObject GetSecretsGlobalCDO(this Session session, bool includePublicVariables = true)
        {
            var variables = session.GetGlobalCDO("EncompassVariables.json") as JObject;
            var securedVariables = session.DataExchange.GetCustomDataObject("EncompassVariables.json.sec")?.Data;

            var key = (string)variables["Base64SecretsKey"];
            var salt = (string)variables["Base64SecretsSalt"];

            var decryptedData = TransformBytes(securedVariables, key, Convert.FromBase64String(salt));

            var secrets = JObject.Parse(Encoding.UTF8.GetString(decryptedData));
            if (includePublicVariables)
            {
                variables.Merge(secrets);
                variables.Remove("Base64SecretsKey");
                variables.Remove("Base64SecretsSalt");
                return variables;
            }
            else
                return secrets;
        }
    }
}
