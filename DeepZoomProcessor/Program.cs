using Microsoft.DeepZoomTools;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerImageProcessor
{
    class Program
    {
        private static readonly string _dbConnection = "";
        private static readonly string _azureStorageAccount = "";
        private static readonly string _azureStorageKey = "";

        static void Main(string[] args)
        {
            try
            {
                // get a connection to the blob storage account
                CloudStorageAccount account = CloudStorageAccount.Parse(
                    String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", _azureStorageAccount, _azureStorageKey));
                CloudBlobClient client = account.CreateCloudBlobClient();

                // create a DeepZoom image creater to generate the tile set for each raw image
                ImageCreator creator = new ImageCreator();
                creator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
                creator.TileOverlap = 1;
                creator.TileSize = 256;

                CollectionCreator ccreator = new CollectionCreator();   
                ccreator.TileFormat = ImageFormat.Jpg; 
                ccreator.TileOverlap = 1; 
                ccreator.TileSize = 256;

                Console.WriteLine("Generating DeepZoom Tiles ...");
                using (SqlConnection conn = new SqlConnection(_dbConnection))
                {
                    string containerID = null;
                    List<string> files = null;
                    conn.Open();
              	    using (SqlCommand command = new SqlCommand("select BlobID,ContainerID from Photos p inner join CameraSites s on p.Site_ID = s.ID order by ContainerID", conn))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (containerID == null || containerID != reader.GetString(1))
                            {
                                if (files != null)
                                {
                                    Console.WriteLine(String.Format("Building DeepZoom Collection for {0}...", containerID));
                                    // generate the collection
                                    ccreator.Create(files, System.IO.Path.Combine(System.IO.Path.GetTempPath(), containerID, "da.dzc")); 
                                }
                                files = new List<string>();
                                containerID = reader.GetString(1);
                            }

                            // create a directory in which to store the DeepZoom tiles for the image
                            string rootPath = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.GetTempPath(), @"dzgen"), reader.GetString(1));
                            Directory.CreateDirectory(rootPath);

                            CloudBlob imageBlob = client.GetBlobReference(reader.GetString(1) + "/" + reader.GetString(0) + "/Image.jpg");
                            string fileName = System.IO.Path.Combine(rootPath, reader.GetString(0)+".jpg");
                            imageBlob.DownloadToFile(fileName);
                            files.Add(fileName);

                            creator.Create(fileName, System.IO.Path.Combine(rootPath, reader.GetString(0)+".dzi")); // create the DeepZoom tileset
                            File.Delete(fileName);
                        }
                    }
                }

                // create a reference to the container for this image site
                CloudBlobContainer container = client.GetContainerReference("dzsource");
                container.CreateIfNotExist(); // create it on the cloud if it isn't already there

                BlobRequestOptions options = new BlobRequestOptions();
                options.UseFlatBlobListing = true;

                // set permissions to be public so anyone can access the image sets
                BlobContainerPermissions blobContainerPermissions = new BlobContainerPermissions();
                blobContainerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
                container.SetPermissions(blobContainerPermissions);

                string processRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), @"dzgen");
                DirectoryInfo rootInfo = new DirectoryInfo(processRoot);

                Console.WriteLine("Uploading data to Azure ...");
                IterateFolders(rootInfo, container, processRoot, processRoot); // upload all of the generated image tiles
                Directory.Delete(processRoot, true); // delete the local DeepZoom tiles
            }
            catch (Exception e) 
            { 
                Console.WriteLine(String.Format("Error: {0}", e.Message));
            }

            Console.Write("Process complete.  Press any key to continue");
            Console.ReadKey();
        }

        // this method was taken from http://blogs.msdn.com/b/jbarnes/archive/2011/12/07/hosting-wp7-deep-zoom-content-in-azure-blob-storage.aspx 
        private static void IterateFolders(DirectoryInfo CurrentDir, CloudBlobContainer TargetContainer, string RootFolderName, string globalRoot)
        {
            DirectoryInfo[] ChildDirectories = CurrentDir.GetDirectories();

            //recurively iterate through all descendants of the source folder
            foreach (DirectoryInfo ChildDir in ChildDirectories)
            {
                IterateFolders(ChildDir, TargetContainer, RootFolderName, globalRoot);
            }

            //get the path name including only the rootfoldername and its decendants; it will be used as part of the filename
            string PreAppendPath = CurrentDir.FullName.Remove(0, CurrentDir.FullName.IndexOf(RootFolderName));

            //get file list
            FileInfo[] FileList = CurrentDir.GetFiles();

            //Iterate through all files in a Folder in PARALLEL
            Parallel.ForEach(FileList, file =>
            {
                //filename + path and use as name in container; path + filename should be unique
                string NewFileName = PreAppendPath + "\\" + file.Name;

                //Change Slash to opposite directon
                string FldrPath = globalRoot;
                FldrPath = FldrPath.Replace(@"\", "/").ToLower();

                //Strip relative leading path
                NewFileName = NewFileName.Replace(@"\", "/").ToLower();
                NewFileName = NewFileName.Replace(FldrPath, "");

                //Strip leading slash for root documents
                if (NewFileName.IndexOf("/") == 0)
                    NewFileName = NewFileName.Remove(0, 1);

                //Upload Blob
                BlobRequestOptions options = new BlobRequestOptions();
                options.AccessCondition = AccessCondition.IfNotModifiedSince(file.LastWriteTimeUtc);
                CloudBlob destBlob = TargetContainer.GetBlobReference(NewFileName);
                destBlob.DeleteIfExists();
                destBlob.UploadFile(file.FullName, options);
            });
        }
    }
}
