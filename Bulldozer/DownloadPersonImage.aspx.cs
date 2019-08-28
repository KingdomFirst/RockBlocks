// <copyright>
// Copyright 2019 by Kingdom First Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace RockWeb.Plugins.rocks_kfs.Bulldozer
{
    using System;
    using System.IO;

    public partial class DownloadPersonImage : System.Web.UI.Page
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var bulldozerDirectory = Server.MapPath( "~/App_Data/Bulldozer/" );
            var zippedPersonImages = bulldozerDirectory + "PersonImage.zip";

            if ( File.Exists( zippedPersonImages ) )
            {
                FileInfo file = new FileInfo( zippedPersonImages );
                if ( !IsFileLocked( file ) )
                {
                    try
                    {
                        Response.ClearContent();
                        Response.AddHeader( "Content-Disposition", String.Format( "attachment; filename={0}", file.Name ) );
                        Response.AddHeader( "Content-Length", file.Length.ToString() );
                        Response.ContentType = "application/zip";
                        Response.TransmitFile( file.FullName );
                        Response.Flush();
                        File.Delete( zippedPersonImages );
                        Response.End();
                    }
                    catch { }
                }
            }
        }

        protected virtual bool IsFileLocked( FileInfo file )
        {
            FileStream stream = null;

            try
            {
                stream = file.Open( FileMode.Open, FileAccess.Read, FileShare.None );
            }
            catch ( IOException )
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if ( stream != null )
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}