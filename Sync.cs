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
using Newtonsoft.Json;
using System.Dynamic;
using Ookii.Dialogs.Wpf;
using System.Windows.Threading;
using Microsoft.CSharp.RuntimeBinder;

namespace GAlbumSync
{
    public sealed class Sync
    {


        Dictionary<String, String> mediaIdByName = new Dictionary<String, String>();

        RestClient client = new RestClient("https://photoslibrary.googleapis.com/v1");
        MainWindow mainWindow;
        
        public Sync(MainWindow mainWindow){
            this.mainWindow = mainWindow;
        }

        public async Task sync(string path, Auth auth){

            Console.WriteLine("Indexing pictures...");

            bool indexOk = await IndexMediasIds(auth);
            if(!indexOk) return;

            Console.WriteLine("Syncing " + path);

            string[] files = Directory.GetDirectories(path);    
            foreach(string file in files){    
                bool dirOk = await processDir(file, auth);
                if(!dirOk) return;
            }
        }



        private async Task<bool> processDir(string path, Auth auth){
            string name = new DirectoryInfo(path).Name;
            Console.WriteLine("Processing dir " + path);
            
            string albumId = await createAlbum(name, auth);

            if(albumId != null){

                List<string> ids50 = new List<string>();

                string[] filesPaths = Directory.GetFiles(path);
                int toMove = filesPaths.Length;
                int moved = 0;
                foreach(string filePath in filesPaths){
                    string fileName = Path.GetFileName(filePath);

                    if(mediaIdByName.ContainsKey(fileName)){
                        ids50.Add(mediaIdByName[fileName]);
                    }
                    

                    if(ids50.Count >= 50){
                        bool addOk = await addMediasToAlbum(albumId, auth, ids50);
                        if(!addOk) return false;
                        moved += ids50.Count;
                        Console.WriteLine("Moving media " + moved + "/" + toMove);
                        ids50.Clear();
                    }

                }

                if(ids50.Count >= 1){
                    bool addOk = await addMediasToAlbum(albumId, auth, ids50);
                    if(!addOk) return false;
                    moved += ids50.Count;
                    Console.WriteLine("Moving media " + moved + "/" + toMove);
                    ids50.Clear();
                }

                return true;
            }
            return false;
        }


        private async Task<bool> addMediasToAlbum(string albumId, Auth auth, List<string> mediaIds){
            
            Console.WriteLine("Adding medias to album " + albumId);

            var request = new RestRequest("albums/" + albumId + ":batchAddMediaItems", Method.POST);
            request.AddHeader("Authorization", "Bearer " + auth.getToken());
            request.AddHeader("content-type", "application/json");

            
            string list = "";
            bool firstLoop = true;
            foreach(string mediaId in mediaIds){
                if(firstLoop){
                    list += mediaId;
                    firstLoop = false;
                }else{
                    list += "," + mediaId;
                }
            }
            Console.WriteLine("{\"mediaItemIds\": [" + list + "] }");
            request.AddJsonBody("{\"mediaItemIds\": [" + list + "] }");
            
            String response = await client.PostAsync<String>(request);
            Console.WriteLine(response);

            dynamic parsedData = JsonConvert.DeserializeObject(response);

            try{
                Console.WriteLine("code : " + parsedData.id);
                showRequestError(response);
                return false;
            }catch(NullReferenceException){
                return true;
            }

        }

        private async Task<bool> IndexMediasIds(Auth auth, string nextPageToken = null){
            
            var request = new RestRequest("mediaItems", Method.GET);
            request.AddHeader("Authorization", "Bearer " + auth.getToken());
            request.AddHeader("content-type", "application/json");

            request.AddQueryParameter("pageSize", "100");
            if(nextPageToken != null) request.AddQueryParameter("pageToken", nextPageToken);

            String response = await client.GetAsync<String>(request);
            dynamic parsedData = JsonConvert.DeserializeObject(response);

            try{
                foreach(dynamic mediaData in parsedData.mediaItems){
                    if(mediaIdByName.ContainsKey((string) mediaData.filename)){
                        Console.WriteLine(mediaData.filename + " exists twice");
                    }else{
                        mediaIdByName.Add((string) mediaData.filename, (string) mediaData.id);
                    } 
                }
            }catch(NullReferenceException){
                Console.WriteLine("code : " + parsedData.id);
                showRequestError(response);
                return false;
            }

            if(!String.IsNullOrEmpty((string) parsedData.nextPageToken)){
                return await IndexMediasIds(auth, (string) parsedData.nextPageToken);
            }

            return true;

        }

        private async Task<String> createAlbum(string name, Auth auth){

            string existingId = await existsAlbum(name, auth);
            if(existingId == null){
                return null;
            }
            if(existingId != ""){
                return existingId;
            }

            Console.WriteLine("Creating album " + name);

            var request = new RestRequest("albums", Method.POST);
            request.AddHeader("Authorization", "Bearer " + auth.getToken());
            request.AddHeader("content-type", "application/json");

            request.AddJsonBody("{\"album\": {\"title\": \"" + name + "\"} }");
            
            String response = await client.PostAsync<String>(request);
            Console.WriteLine(response);

            dynamic parsedData = JsonConvert.DeserializeObject(response);

            try{
                return parsedData.id;
            }catch(NullReferenceException){
                showRequestError(response);
                return null;
            }

        }

        private async Task<String> existsAlbum(string name, Auth auth){
            var request = new RestRequest("albums", Method.GET);
            request.AddHeader("Authorization", "Bearer " + auth.getToken());
            request.AddHeader("content-type", "application/json");

            request.AddQueryParameter("pageSize", "50");
            
            String response = await client.GetAsync<String>(request);
           

            dynamic parsedData = JsonConvert.DeserializeObject(response);

            try{
                foreach(dynamic albumData in parsedData.albums){
                    if(albumData.title == name){
                        Console.WriteLine(name + " already exists");
                        return albumData.id;
                    }
                }
                return "";
            }catch(NullReferenceException){
                showRequestError(response);
                return null;
            }
            

        }

        public void showRequestError(string response){

            var diag = new TaskDialog{
                Buttons = {new TaskDialogButton(ButtonType.Ok)},
                WindowTitle = "Request error",
                MainInstruction = "Unable to send request / to parse response.",
                Content = "Try to reset Google authentification and to re-authentificate.",
                ExpandedInformation = "HTTP response :\n" + response,
                MainIcon = TaskDialogIcon.Error
            };
            diag.Show();
            
        }


    }
}