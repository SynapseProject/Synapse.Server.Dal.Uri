using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Utilities.Encryption;

namespace Synapse.Server.Dal.Uri.Encryption
{
    public class EncryptionHelper
    {
        private static string publicKeyName;
        private static string privateKeyName;
        public static string vector;
        private static bool EncryptionEnabled  { get; set; }

        public static void Configure(UriDalConfig config)
        {
            publicKeyName = config.PublicKey;
            privateKeyName = config.PrivateKey;
            vector = config.Vector;
            EncryptionEnabled = config.EncryptionEnabled;
        }

        private static string __private;
        private static string _private
        {
            get
            {
                string key = new SimpleAES().Decrypt( privateKeyName );
                var base64EncodedBytes = System.Convert.FromBase64String( key );
                __private = new string( System.Text.Encoding.UTF8.GetString( base64EncodedBytes ).ToArray() );
                __private = __private.Replace( "&gt;", ">" ).Replace( "&lt;", "<" );

                return __private;
            }
        }

        private static string __public;
        private static string _public
        {
            get
            {
                var base64EncodedBytes = System.Convert.FromBase64String( publicKeyName );
                __public = System.Text.Encoding.UTF8.GetString( base64EncodedBytes );
                __public = __public.Replace( "&gt;", ">" ).Replace( "&lt;", "<" );

                return __public;
            }
        }

        /// <summary>
        /// Decrypt:  Asymmetrical decryption of a string using private RSA key - 
        /// INTERNAL ONLY, NEVER EXPOSE THIS METHOD EXTERNAL TO THIS ASSEMBLY EXCEPT BY PENALTY OF PROGRAMMER FLOGGING
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Decrypt(string data)
        {
            try
            {
                if( EncryptionEnabled )
                {

                    if( string.IsNullOrEmpty( _private ) )
                    {
                        return "You need to set the private key in the configuration for decryption to work correctly. [Missing AppSetting.RSA_PRIVATE_KEY]";
                    }

                    return RSAEncryption.Decrypt( data, _private );
                }
                return data;
            }
            catch( Exception exception )
            {
                // ExceptionHelper.Capture(exception);
                return "Decryption failed. " + exception.Message;
            }
        }

        /// <summary>
        /// Encrypt:  Asymmetrical encryption of a string using public RSA key
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Encrypt(string data)
        {
            try
            {
                if( EncryptionEnabled )
                {
                    if( string.IsNullOrEmpty( _public ) )
                    {
                        return "You need to set the public key in the configuration for encryption to work correctly. [Missing AppSetting.RSA_PUBLIC_KEY]";
                    }

                    return RSAEncryption.Encrypt( data, _public );
                }
                return data;
            }
            catch( Exception exception )
            {
                // ExceptionHelper.Capture(exception);
                return "Encryption failed. " + exception.Message;
            }
        }

        public static string EncryptAES(string data)
        {
            return new SimpleAES().Encrypt( data );
        }
        public static string EncryptAES(string data, string key)
        {
            return new SimpleAES( key ).Encrypt( data );
        }

        #region Legacy AES for key security

        public class SimpleAES
        {
            private ICryptoTransform encryptor, decryptor;
            private UTF8Encoding encoder;
            private string keyVectorRaw = EncryptionHelper.vector;
            private bool userKey = false;
            private byte[] key;
            private byte[] vector;

            public SimpleAES() { }

            public SimpleAES(string key)
            {
                keyVectorRaw = key;
                userKey = true;
            }

            private void GenerateCryptors()
            {
                RijndaelManaged rm = new RijndaelManaged();

                string[] keyVector = keyVectorRaw.Split( '~' );

                foreach( string kv in keyVector )
                {
                    string[] items = kv.Split( ',' );

                    if( items.Count() < 20 )
                    {
                        FillVector( items );
                    }
                    else
                    {
                        FillKey( items );
                    }
                }

                encryptor = rm.CreateEncryptor( key, vector );
                decryptor = rm.CreateDecryptor( key, vector );
                encoder = new UTF8Encoding();
            }

            private void ConvertUserKey()
            {
                string convertedKey = string.Empty;

                // align vector - 16 character codes from reversed backend of key
                string keyReverse = new String( keyVectorRaw.Reverse().ToArray() );
                byte[] bytes = Encoding.ASCII.GetBytes( keyReverse.Substring( 0, 16 ) );
                foreach( byte b in bytes )
                {
                    convertedKey += b.ToString() + ",";
                }
                convertedKey = convertedKey.Substring( 0, convertedKey.Length - 1 ) + "~";

                bytes = Encoding.ASCII.GetBytes( keyVectorRaw );
                foreach( byte b in bytes )
                {
                    convertedKey += b.ToString() + ",";
                }
                // align key - all 32 character codes
                keyVectorRaw = convertedKey.Substring( 0, convertedKey.Length - 1 );
            }


            public string Encrypt(string unencrypted)
            {
                if( userKey )
                {
                    if( keyVectorRaw.Length < 32 )
                    {
                        return "Key provided should be at least 32 characters in length to perform secure encryption.";
                    }
                    ConvertUserKey();
                }

                GenerateCryptors();
                return Convert.ToBase64String( Encrypt( encoder.GetBytes( unencrypted ) ) );
            }

            public string Decrypt(string encrypted)
            {
                if( userKey )
                {
                    if( keyVectorRaw.Length < 32 )
                    {
                        return "Key provided should be at least 32 characters in length to perform secure encryption.";
                    }
                    ConvertUserKey();
                }

                GenerateCryptors();
                return encoder.GetString( Decrypt( Convert.FromBase64String( encrypted ) ) );
            }

            protected byte[] Encrypt(byte[] buffer)
            {
                return Transform( buffer, encryptor );
            }

            protected byte[] Decrypt(byte[] buffer)
            {
                return Transform( buffer, decryptor );
            }

            protected byte[] Transform(byte[] buffer, ICryptoTransform transform)
            {
                MemoryStream stream = new MemoryStream();
                using( CryptoStream cs = new CryptoStream( stream, transform, CryptoStreamMode.Write ) )
                {
                    cs.Write( buffer, 0, buffer.Length );
                }
                return stream.ToArray();
            }

            private void FillVector(string[] items)
            {
                vector = items.Select( p => Convert.ToByte( p ) ).ToArray();
            }
            private void FillKey(string[] items)
            {
                key = items.Select( p => Convert.ToByte( p ) ).ToArray();
            }
        }

        #endregion
    }
}
