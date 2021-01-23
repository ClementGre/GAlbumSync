using System.ComponentModel;
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
using System.Web;

namespace GAlbumSync
{
    public sealed class Sync
    {


        Dictionary<String, String> mediaIdByName = new Dictionary<String, String>();
        private int sameNamePictures = 0;

        RestClient client = new RestClient("https://photoslibrary.googleapis.com/v1");
        MainWindow mainWindow;

        ProgressDialog dialog;
        
        public Sync(MainWindow mainWindow){
            this.mainWindow = mainWindow;
            dialog = new ProgressDialog{
                WindowTitle = "Syncing...",
                Text = "Syncyng..."
            };
        }

        public async Task sync(string path, Auth auth){

            dialog.DoWork += delegate(object sender, DoWorkEventArgs e) {
                var task = Task.Run(async () => { await doSyncWork(sender, e, path, auth); });
                task.Wait();
            };
            dialog.Show(mainWindow);
        }

        private async Task doSyncWork(object sender, DoWorkEventArgs e, string path, Auth auth){

            Console.WriteLine("Indexing pictures...");

            dialog.ProgressBarStyle = ProgressBarStyle.MarqueeProgressBar;
            dialog.ReportProgress(0, "Indexing pictures...", mediaIdByName.Count + " indexed pictures.  -  " + sameNamePictures + " same name pictures.");
            
            bool indexOk = await IndexMediasIds(auth);
            if(!indexOk) return;


            Console.WriteLine("Syncing " + path);
            dialog.ProgressBarStyle = ProgressBarStyle.ProgressBar;
            
            string[] files = Directory.GetDirectories(path);
            int i = 0;
            foreach(string file in files){
                if(dialog.CancellationPending){
                    return;
                }
                bool dirOk = await processDir(file, auth, i/files.Length * 100);
                if(!dirOk) return;
                i++;
            }
        }



        private async Task<bool> processDir(string path, Auth auth, int percentage){
            if(dialog.CancellationPending){
                return false;
            }
            string name = new DirectoryInfo(path).Name;
            Console.WriteLine("Processing dir " + path);
            dialog.ReportProgress(percentage, "Managing directory/album " + name + "...", "Creating Album...");
            
            string albumId = await createAlbum(name, auth);

            if(dialog.CancellationPending){
                return false;
            }
            if(albumId != null){

                List<string> ids50 = new List<string>();

                string[] filesPaths = Directory.GetFiles(path);
                int toMove = filesPaths.Length;
                int moved = 0;
                foreach(string filePath in filesPaths){
                    string fileName = Path.GetFileName(filePath);

                    if(dialog.CancellationPending){
                        return false;
                    }
                    dialog.ReportProgress(percentage, "Managing directory/album " + name + "...", "Uploading medias (" + moved + "/" + toMove + ")");
                    bool addOk = await uploadFile(albumId, auth, path);
                    if(!addOk) return false;
                    moved ++;
                    Console.WriteLine("Uploading media " + moved + "/" + toMove);
                    dialog.ReportProgress(percentage, "Managing directory/album " + name + "...", "Uploading medias (" + moved + "/" + toMove + ")");
                    
                }

                if(dialog.CancellationPending){
                    return false;
                }

                // List<string> ids50 = new List<string>();

                // string[] filesPaths = Directory.GetFiles(path);
                // int toMove = filesPaths.Length;
                // int moved = 0;
                // foreach(string filePath in filesPaths){
                //     string fileName = Path.GetFileName(filePath);

                //     if(mediaIdByName.ContainsKey(fileName)){
                //         ids50.Add(mediaIdByName[fileName]);
                //     }
                    

                //     if(ids50.Count >= 50){
                //         if(dialog.CancellationPending){
                //             return false;
                //         }
                //         dialog.ReportProgress(percentage, "Managing directory/album " + name + "...", "Moving pictures (" + moved + "/" + toMove + ")");
                //         bool addOk = await addMediasToAlbum(albumId, auth, ids50);
                //         if(!addOk) return false;
                //         moved += ids50.Count;
                //         Console.WriteLine("Moving media " + moved + "/" + toMove);
                //         dialog.ReportProgress(percentage, "Managing directory/album " + name + "...", "Moving pictures (" + moved + "/" + toMove + ")");
                //         ids50.Clear();
                //     }

                // }

                // if(dialog.CancellationPending){
                //     return false;
                // }

                // if(ids50.Count >= 1){
                //     dialog.ReportProgress(percentage, "Managing directory/album " + name + "...", "Moving pictures (" + moved + "/" + toMove + ")");
                //     bool addOk = await addMediasToAlbum(albumId, auth, ids50);
                //     if(!addOk) return false;
                //     moved += ids50.Count;
                //     Console.WriteLine("Moving media " + moved + "/" + toMove);
                //     dialog.ReportProgress(percentage, "Managing directory/album " + name + "...", "Moving pictures (" + moved + "/" + toMove + ")");
                //     ids50.Clear();
                // }

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
                    list += "\"" + mediaId + "\"";
                    firstLoop = false;
                }else{
                    list += ",\"" + mediaId + "\"";
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

        private async Task<bool> uploadFile(string albumId, Auth auth, string path){
            string fileName = Path.GetFileName(path);
            Console.WriteLine("Uploading picture " + fileName);

            var request = new RestRequest("uploads", Method.POST);
            request.AddHeader("Authorization", "Bearer " + auth.getToken());
            request.AddHeader("content-type", "application/octet-stream");
            //request.AddHeader("X-Goog-Upload-Content-Type", MimeMapping.GetMimeMapping(fileName));
            request.AddHeader("X-Goog-Upload-Protocol", "raw");

            request.AddJsonBody("binary of the file");
            
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
            
            if(dialog.CancellationPending){
                return false;
            }
            dialog.ReportProgress(0, "Indexing pictures...", mediaIdByName.Count + " indexed pictures.  -  " + sameNamePictures + " same name pictures.");

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
                        sameNamePictures++;
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