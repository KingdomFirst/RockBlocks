using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Microsoft.Web.XmlTransform;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.rocks_kfs.Utility
{
    /// <summary>
    /// Block that imports plugin files.
    /// </summary>

    #region Block Attributes

    [DisplayName( "Plugin Import" )]
    [Category( "KFS > Utility" )]
    [Description( "This block imports a .plugin file and processes it as if installed from the Rock Shop." )]

    #endregion

    public partial class PluginImport : RockBlock
    {
        #region Fields

        private const string _xdtExtension = ".rock.xdt";

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Handles the FileUploaded event of the fupPlugin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.FileUploaderEventArgs"/> instance containing the event data.</param>
        protected void fupPlugin_FileUploaded( object sender, Rock.Web.UI.Controls.FileUploaderEventArgs e )
        {
            var allowedExtensions = new List<string> { ".plugin", ".zip" };
            var physicalFile = this.Request.MapPath( fupPlugin.UploadedContentFilePath );
            if ( File.Exists( physicalFile ) )
            {
                var fileInfo = new FileInfo( physicalFile );
                if ( allowedExtensions.Contains( fileInfo.Extension ) )
                {
                    nbWarning.Text = string.Empty;
                    hfPluginFileName.Value = fupPlugin.UploadedContentFilePath;
                    btnImport.Enabled = hfPluginFileName.Value != string.Empty;
                }
                else
                {
                    nbWarning.Text = "Could not process this file.  Please select a valid Plugin file.";
                    File.Delete( physicalFile );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnImport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnImport_Click( object sender, EventArgs e )
        {
            // wait a little so the browser can render and start listening to events
            Thread.Sleep( 1000 );

            var allowedExtensions = new List<string> { ".plugin", ".zip" };
            var physicalFile = this.Request.MapPath( fupPlugin.UploadedContentFilePath );
            if ( File.Exists( physicalFile ) )
            {
                var fileInfo = new FileInfo( physicalFile );
                if ( allowedExtensions.Contains( fileInfo.Extension ) )
                {
                    ProcessPackage( physicalFile );
                }
                else
                {
                    nbWarning.Text = "Could not process this file.  Please select a valid Plugin file.";
                    File.Delete( physicalFile );
                }
            }
        }

        /// <summary>
        /// Processes the package.
        /// </summary>
        /// <param name="destinationFile">The destination file.</param>
        private void ProcessPackage( string destinationFile )
        {
            string appRoot = Server.MapPath( "~/" );

            // process zip folder
            try
            {
                using ( ZipArchive packageZip = ZipFile.OpenRead( destinationFile ) )
                {
                    // unzip content folder and process xdts
                    foreach ( ZipArchiveEntry entry in packageZip.Entries.Where( e => e.FullName.StartsWith( "content/", StringComparison.OrdinalIgnoreCase ) ) )
                    {
                        if ( entry.FullName.EndsWith( _xdtExtension, StringComparison.OrdinalIgnoreCase ) )
                        {
                            // process xdt
                            string filename = entry.FullName.Replace( "content/", "" );
                            string transformTargetFile = appRoot + filename.Substring( 0, filename.LastIndexOf( _xdtExtension ) );

                            // process transform
                            using ( XmlTransformableDocument document = new XmlTransformableDocument() )
                            {
                                document.PreserveWhitespace = true;
                                document.Load( transformTargetFile );

                                using ( XmlTransformation transform = new XmlTransformation( entry.Open(), null ) )
                                {
                                    if ( transform.Apply( document ) )
                                    {
                                        document.Save( transformTargetFile );
                                    }
                                }
                            }
                        }
                        else
                        {
                            // process all content files
                            string fullpath = Path.Combine( appRoot, entry.FullName.Replace( "content/", "" ) );
                            string directory = Path.GetDirectoryName( fullpath ).Replace( "content/", "" );

                            // if entry is a directory ignore it
                            if ( entry.Length != 0 )
                            {
                                if ( !Directory.Exists( directory ) )
                                {
                                    Directory.CreateDirectory( directory );
                                }

                                entry.ExtractToFile( fullpath, true );
                            }
                        }
                    }

                    // process install.sql
                    try
                    {
                        var sqlInstallEntry = packageZip.Entries.Where( e => e.FullName == "install/run.sql" ).FirstOrDefault();
                        if ( sqlInstallEntry != null )
                        {
                            string sqlScript = System.Text.Encoding.Default.GetString( sqlInstallEntry.Open().ReadBytesToEnd() );

                            if ( !string.IsNullOrWhiteSpace( sqlScript ) )
                            {
                                using ( var context = new RockContext() )
                                {
                                    context.Database.ExecuteSqlCommand( sqlScript );
                                }
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        nbWarning.Text = string.Format( "<strong>Error Updating Database</strong> An error occurred while updating the database. <br><em>Error: {0}</em>", ex.Message );
                        return;
                    }

                    // process deletefile.lst
                    try
                    {
                        var deleteListEntry = packageZip.Entries.Where( e => e.FullName == "install/deletefile.lst" ).FirstOrDefault();
                        if ( deleteListEntry != null )
                        {
                            string deleteList = System.Text.Encoding.Default.GetString( deleteListEntry.Open().ReadBytesToEnd() );

                            string[] itemsToDelete = deleteList.Split( new string[] { Environment.NewLine }, StringSplitOptions.None );

                            foreach ( string deleteItem in itemsToDelete )
                            {
                                if ( !string.IsNullOrWhiteSpace( deleteItem ) )
                                {
                                    string deleteItemFullPath = appRoot + deleteItem;

                                    if ( Directory.Exists( deleteItemFullPath ) )
                                    {
                                        Directory.Delete( deleteItemFullPath, true );
                                    }

                                    if ( File.Exists( deleteItemFullPath ) )
                                    {
                                        File.Delete( deleteItemFullPath );
                                    }
                                }
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        nbWarning.Text = string.Format( "<strong>Error Modifying Files</strong> An error occurred while modifying files. <br><em>Error: {0}</em>", ex.Message );
                        return;
                    }
                }
            }
            catch ( Exception ex )
            {
                nbWarning.Text = string.Format( "<strong>Error Extracting Package</strong> An error occurred while extracting the contents of the package. <br><em>Error: {0}</em>", ex.Message );
                return;
            }

            // cleanup whether we could read the file or not
            File.Delete( destinationFile );

            // Clear all cached items
            RockCache.ClearAllCachedItems();

            // show result message
            nbWarning.Text = "<strong>Package Installed</strong>";
        }

        #endregion
    }
}
