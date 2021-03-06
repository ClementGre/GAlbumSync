using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading;

namespace GAlbumSync
{
    public sealed class Auth
    {

        public static string tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GAlbumSync.credential");
        public UserCredential credential;

        public async Task auth(){
            
            //using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read)){
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    //GoogleClientSecrets.Load(stream).Secrets,
                    new Google.Apis.Auth.OAuth2.ClientSecrets{
                        ClientId = ClientSecrets.CLIENT_ID,
                        ClientSecret = ClientSecrets.CLIENT_SECRET
                    },
                    new string[] { "https://www.googleapis.com/auth/photoslibrary.readonly", "https://www.googleapis.com/auth/photoslibrary.appendonly"},
                    "user", CancellationToken.None,
                    new FileDataStore(tokenPath));
            //}

            Console.WriteLine("Token : " + getToken());

        }

        public void resetConexion(){

            string[] files = Directory.GetFiles(tokenPath);    
            foreach(string file in files){    
                File.Delete(file);
            }
        }

        public string getToken(){
            return credential.Token.AccessToken;
        }
    }
}