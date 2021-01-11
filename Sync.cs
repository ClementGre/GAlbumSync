using System.Net;
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
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Validation;
using RestSharp.Serializers;
using RestSharp.Serialization;
using RestSharp.Deserializers;
using RestSharp.Extensions;

namespace GAlbumSync
{
    public sealed class Sync
    {

        RestClient client = new RestClient("https://photoslibrary.googleapis.com/v1");

        public async Task sync(string path, Auth auth){
            Console.WriteLine("Syncing " + path);

            string[] files = Directory.GetDirectories(path);    
            foreach(string file in files){    
                await processDir(file, auth);
            }
        }



        private async Task processDir(string path, Auth auth){
            string name = Path.GetDirectoryName(path);
            Console.WriteLine("Processing dir " + path);

            await createAlbum(name, auth);

            string[] files = Directory.GetFiles(path);    
            foreach(string file in files){    
                
            }

        }


        private async Task createAlbum(string name, Auth auth){
            Console.WriteLine("Creating album " + name);

            var request = new RestRequest("albums", Method.POST);

            request.AddJsonBody("");
            request.AddParameter("Authorization", "Bearer " + auth.credential.Token, ParameterType.HttpHeader);

            String response = await client.PostAsync<String>(request);

            Console.WriteLine(response);

        }


    }
}