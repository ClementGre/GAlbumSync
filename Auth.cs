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

        public static bool auth()
        {

            try{
                new Auth().authenticate().Wait();
            }
            catch (AggregateException ex){
                foreach (var e in ex.InnerExceptions){
                    System.Diagnostics.Debug.WriteLine("ERROR: " + e.Message);
                    return false;
                }
            }
            return true;

        }

        private async Task authenticate()
        {
            string tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GAlbumSync.credential");
            Console.WriteLine("token file: " + tokenPath);

            UserCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new string[] { "https://www.googleapis.com/auth/photoslibrary.readonly" },
                    "user", CancellationToken.None,
                    new FileDataStore(tokenPath));
            }

            Console.WriteLine("token: " + credential.Token);




        }
    }
}