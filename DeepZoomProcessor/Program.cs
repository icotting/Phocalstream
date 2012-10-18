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
                string rootPath = "";
                string containerID = null;
                string siteName = null;
                long siteId = -1;

                List<string> files = null;
                List<Tuple<string, string, long, string>> siteInfo = new List<Tuple<string, string, long, string>>();

                using (SqlConnection conn = new SqlConnection(_dbConnection))
                {
                    conn.Open();
                    using (SqlCommand command = new SqlCommand("select BlobID,ContainerID,Site_ID,s.Name from Photos p inner join CameraSites s on p.Site_ID = s.ID where s.ID not in (select Site_ID from Collections where Status = 1) order by Site_ID", conn))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            siteInfo.Add(new Tuple<string, string, long, string>(reader.GetString(0), reader.GetString(1), reader.GetInt64(2), reader.GetString(3)));
                        }
                    }
                    conn.Close();
                }

                foreach (Tuple<string, string, long, string> site in siteInfo)
                {
                    if (containerID == null || containerID != site.Item2)
                    {
                        if (files != null)
                        {
                            // create a reference to the container for this image site
                            CloudBlobContainer container = client.GetContainerReference(String.Format("{0}-dz", containerID));
                            container.CreateIfNotExist(); // create it on the cloud if it isn't already there

                            BlobRequestOptions options = new BlobRequestOptions();
                            options.UseFlatBlobListing = true;

                            // set permissions to be public so anyone can access the image sets
                            BlobContainerPermissions blobContainerPermissions = new BlobContainerPermissions();
                            blobContainerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
                            container.SetPermissions(blobContainerPermissions);

                            using (SqlConnection conn = new SqlConnection(_dbConnection))
                            {
                                conn.Open();
                                Boolean create = false;
                                using (SqlCommand command = new SqlCommand("select ID from Collections where Site_ID = @Site", conn))
                                {
                                    command.Parameters.AddWithValue("@Site", siteId);
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {
                                        if (reader.HasRows == false) // there is no collection for this site
                                        {
                                            create = true;
                                        }
                                    }
                                }

                                if (create)
                                {
                                    Console.WriteLine(String.Format("Building DeepZoom Collection for {0}...", containerID));
                                    // generate the collection
                                    ccreator.Create(files, System.IO.Path.Combine(rootPath, "da.dzc"));

                                    using (SqlCommand insert = new SqlCommand("insert into Collections (Name,Type,Site_ID,Status) values (@Name,@Type,@Site_ID,@Status)", conn))
                                    {
                                        insert.Parameters.AddWithValue("@Name", siteName);
                                        insert.Parameters.AddWithValue("@Type", 0);
                                        insert.Parameters.AddWithValue("@Site_ID", siteId);
                                        insert.Parameters.AddWithValue("@Status", 0);
                                        insert.ExecuteNonQuery();
                                    }
                                }
                                conn.Close();
                            }
                            DirectoryInfo rootInfo = new DirectoryInfo(rootPath);

                            Console.WriteLine("Uploading data to Azure ...");
                            IterateFolders(rootInfo, container, rootPath, rootPath); // upload all of the generated image tiles
                            Console.WriteLine(String.Format("Cleaning up for {0} ...", containerID));

                            using (SqlConnection conn = new SqlConnection(_dbConnection))
                            {
                                conn.Open();
                                using (SqlCommand command = new SqlCommand("update Collections set Status = 1 where Site_ID = @Site", conn))
                                {
                                    command.Parameters.AddWithValue("@Site", siteId);
                                    command.ExecuteNonQuery();
                                }
                                conn.Close();
                            }
                            Directory.Delete(rootPath, true); // delete the local DeepZoom tiles
                        }

                        // create a directory in which to store the DeepZoom tiles for the image
                        rootPath = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.GetTempPath(), @"dzgen"), site.Item2);
                        Directory.CreateDirectory(rootPath);

                        // reset files to begin processing a new site
                        files = new List<string>();
                        containerID = site.Item2;
                        siteId = site.Item3;
                        siteName = site.Item4;

                        Console.WriteLine(String.Format("Starting process in {0}", rootPath));
                    }

                    string fileName = System.IO.Path.Combine(rootPath, site.Item1 + ".jpg");
                    string dziFile = System.IO.Path.Combine(rootPath, site.Item1 + ".dzi");
                    files.Add(dziFile);

                    if (File.Exists(dziFile) == false) // if the file is already there, don't recreate it
                    {
                        CloudBlob imageBlob = client.GetBlobReference(site.Item2 + "/" + site.Item1 + "/Image.jpg");
                        imageBlob.DownloadToFile(fileName);
                        creator.Create(fileName, dziFile); // create the DeepZoom tileset
                        File.Delete(fileName);
                    }
                }
            }
            catch (Exception e) 
            { 
                Console.WriteLine(String.Format("Error: {0}", e.Message));
                Console.WriteLine(e.ToString());   
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
                BlobRequestOptions options = new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(20), AccessCondition = AccessCondition.IfNotModifiedSince(file.LastWriteTimeUtc)};
                CloudBlob destBlob = TargetContainer.GetBlobReference(NewFileName);
                destBlob.UploadFile(file.FullName, options);
                file.Delete();
            });
        }
    }
}
