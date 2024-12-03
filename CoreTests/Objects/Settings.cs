using SharedResources;

namespace CoreTests.Objects
{
    public class Settings
    {
        public ClientSettings TotalExpert { get; set; }
        public ClientSettings TotalExpert_Dev { get; set; }
        public ClientSettings AzureAD { get; set; }
        public ClientSettings AzureManagement { get; set; }
        public ClientSettings RingCentral { get; set; }
        public EncompassSettings Encompass { get; set; }
        public CredentialSettings Velocify { get; set; }
        public CredentialSettings CreditAPI { get; set; }
        public CredentialSettings CreditAPI_PROD { get; set; }
        public TwilioSettings Twilio { get; set; }
        public WorkdaySoapSettings WorkdaySoap { get; set; }
        public EncryptionSettings PGPKeys { get; set; }
        public EncryptionSettings PGPKeysEncryptor { get; set; }
        public EncryptionSettings PGPKeysSigner { get; set; }
    }
}
