using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HappyVoting8
{
	public static class PictureStore
	{
		private static string uploadFile(HttpPostedFile file, string folder_name, string file_name)
		{
			// shrunk image files have file name "blob".
			string ext = Path.GetExtension(file.FileName);		// todo: xss attack ?
			if (ext.Length == 0)
			{
				if (file.ContentType == "image/jpeg")
					ext = ".jpg";
				else if (file.ContentType == "image/png")
					ext = ".png";
			}
			else
				ext = ext.ToLowerInvariant();		// avoid n3 use .JPG and n2 use .jpg.

			string blob_name = file_name + ext;
			blob_name = folder_name + "/" + blob_name;

			CloudBlockBlob block_blob = Warehouse.PictureContainer.GetBlockBlobReference(blob_name);

			block_blob.Properties.ContentType = file.ContentType;
			block_blob.Properties.CacheControl = "public, max-age=2592000";		// 30 days
			block_blob.UploadFromStream(file.InputStream);		// if the same file.InputStream is uploaded twice, the latter one gets zero bytes of data.
			//block_blob.SetProperties();

			string uri = @"http://i.hela.cc" + block_blob.Uri.PathAndQuery;
			return uri;
		}
		public static string Save(HttpFileCollection files)
		{
			string folder_name = Util.DateTimeToString(DateTime.Now, 4);
			string file_name = Util.RandomAlphaNumericString(4);

			HttpPostedFile file = files["picture"];
			string uri = uploadFile(file, folder_name, file_name);

			return uri;
		}
		public static string ProcessUploadFiles(HttpFileCollection files)
		{
			foreach (string key in files.AllKeys)
			{
				string prefix = key.Substring(0, 3/*length of "n1/"*/);
				string prefix_thumbnail = null;
				string key_body = key.Substring(3/*length of "n1/"*/);

				if (prefix == "n3/")
					prefix_thumbnail = "n2/";
				else if (prefix == "n5/")
					prefix_thumbnail = "n4/";
				else if (prefix != "n1/" && prefix != "n6/")
					continue;

				string folder_name = Util.DateTimeToString(DateTime.Now, 4);
				string file_name = Util.RandomAlphaNumericString(6);
				string thumbnail_uri = null;

				if (prefix_thumbnail != null)
				{
					HttpPostedFile thumbnail_file = files[prefix_thumbnail + key_body];
					thumbnail_uri = uploadFile(thumbnail_file, folder_name, prefix_thumbnail + file_name);
				}
				HttpPostedFile file = files[prefix + key_body];
				string uri = uploadFile(file, folder_name, prefix + file_name);

				return uri;
			}
			return null;
		}
	}
}