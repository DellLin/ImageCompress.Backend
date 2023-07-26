using Google.Cloud.Kms.V1;
using Google.Protobuf;
public class KmsHelper
{
    public CryptoKey CreateKeySymmetricEncryptDecrypt(
        string projectId, string locationId, string keyRingId,
        string id)
    {
        // Create the client.
        KeyManagementServiceClient client = KeyManagementServiceClient.Create();

        // Build the parent key ring name.
        KeyRingName keyRingName = new KeyRingName(projectId, locationId, keyRingId);

        // Build the key.
        CryptoKey key = new CryptoKey
        {
            Purpose = CryptoKey.Types.CryptoKeyPurpose.EncryptDecrypt,
            VersionTemplate = new CryptoKeyVersionTemplate
            {
                Algorithm = CryptoKeyVersion.Types.CryptoKeyVersionAlgorithm.GoogleSymmetricEncryption,
            }
        };

        // Call the API.
        CryptoKey result = client.CreateCryptoKey(keyRingName, id, key);

        // Return the result.
        return result;
    }
    public string EncryptText(string projectId, string locationId, string keyRingId,
        string cryptoKeyId, string plaintext)
    {
        KeyManagementServiceClient client = KeyManagementServiceClient.Create();
        CryptoKeyName keyName = new CryptoKeyName(projectId, locationId, keyRingId, cryptoKeyId);
        ByteString plaintextBytes = ByteString.CopyFromUtf8(plaintext);
        EncryptResponse response = client.Encrypt(keyName, plaintextBytes);
        return response.Ciphertext.ToBase64();
    }

    public string DecryptText(string projectId, string locationId, string keyRingId,
            string cryptoKeyId, string ciphertext)
    {
        KeyManagementServiceClient client = KeyManagementServiceClient.Create();
        CryptoKeyName keyName = new CryptoKeyName(projectId, locationId, keyRingId, cryptoKeyId);
        ByteString ciphertextBytes = ByteString.FromBase64(ciphertext);
        DecryptResponse response = client.Decrypt(keyName, ciphertextBytes);
        return response.Plaintext.ToStringUtf8();
    }

}
