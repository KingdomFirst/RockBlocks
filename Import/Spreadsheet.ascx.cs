using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;

using com.kfs.Import;
using CsvHelper;
using OfficeOpenXml;

namespace RockWeb.Plugins.com_kfs.Import
{
    #region Block Attributes

    [DisplayName( "Spreadsheet Import" )]
    [Category( "KFS > Spreadsheet Import" )]
    [Description( "Block to import spreadsheet data and optionally run a stored procedure." )]

    [CustomDropdownListField( "Default Stored Procedure", "Select the default stored procedure to run when importing a spreadsheet.", "SELECT ROUTINE_NAME AS Text, ROUTINE_NAME AS Value FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' ORDER BY ROUTINE_NAME", false, "", "", 0 )]
    [BooleanField( "Cleanup Table Parameter", "Select this if the SP has a @CleanupTable parameter.", true )]

    #endregion

    public partial class Spreadsheet : RockBlock
    {
        public LogTrigger _trigger;

        /// <summary>
        /// Triggers the log event handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="message">The message.</param>
        private void LogEvent( object sender, string message )
        {
            if ( !string.IsNullOrWhiteSpace( message ) )
            {
                var isEmpty = Session["log"] == null;
                var log = string.Format( "{0}{1}{2}"
                    , Session["log"] ?? message
                    , isEmpty ? string.Empty : "|"
                    , isEmpty ? string.Empty : message
                );

                Session["log"] = log;
            }
        }

        /// <summary>
        /// Handles the PreRender event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Page_PreRender( object sender, EventArgs e )
        {
            if ( _trigger != null && !_trigger.isExecuting )
            {
                _trigger.Dispose();
                _trigger = null;
                Session.Remove( "trigger" );
                tmrSyncSQL.Enabled = false;
                btnImport.Enabled = true;
            }
            else if ( _trigger == null )
            {
                Session.Remove( "log" );
                Session.Remove( "trigger" );
            }
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load( object sender, System.EventArgs e )
        {
            if ( Session["trigger"] != null )
            {
                _trigger = (LogTrigger)Session["trigger"];
            }
        }

        /// <summary>
        /// Handles the Tick event of the tmrSyncSQL control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tmrSyncSQL_Tick( object sender, EventArgs e )
        {
            // output log events to pnlSqlStatus
            if ( Session["log"] != null )
            {
                var builder = new System.Text.StringBuilder();
                builder.Append( lblSqlStatus.Text );
                foreach ( string s in Session["log"].ToString().Split( new char[] { '|' } ) )
                {
                    if ( !string.IsNullOrWhiteSpace( s ) )
                    {
                        builder.Append( s + "<br>" );
                    }
                }
                lblSqlStatus.Text = builder.ToString();

                Session["log"] = string.Empty;
            }
        }

        /// <summary>
        /// Handles the FileUploaded event of the fupSpreadsheet control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.FileUploaderEventArgs"/> instance containing the event data.</param>
        protected void fupSpreadsheet_FileUploaded( object sender, Rock.Web.UI.Controls.FileUploaderEventArgs e )
        {
            var allowedExtensions = new List<string> { ".csv", ".xls", ".xlsx" };
            var physicalFile = this.Request.MapPath( fupSpreadsheet.UploadedContentFilePath );
            if ( File.Exists( physicalFile ) )
            {
                var fileInfo = new FileInfo( physicalFile );
                if ( allowedExtensions.Contains( fileInfo.Extension ) )
                {
                    nbWarning.Text = string.Empty;
                    hfSpreadsheetFileName.Value = fupSpreadsheet.UploadedContentFilePath;
                    btnImport.Enabled = hfSpreadsheetFileName.Value != string.Empty;
                }
                else
                {
                    nbWarning.Text = "Could not process this file.  Please select a valid spreadsheet file.";
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

            var statusText = string.Empty;
            var tableName = string.Empty;

            var uploadSuccessful = TransformFile( fupSpreadsheet.UploadedContentFilePath, out tableName, out statusText );
            if ( !uploadSuccessful )
            {
                nbWarning.Text = string.Format( "Could not upload this spreadsheet: {0}", statusText );
                return;
            }

            // create parameters
            var paramsList = new List<SqlParameter>
            {
                // TODO clean this up to read parameters from SP
                new SqlParameter { ParameterName = "@ImportTable", SqlDbType = SqlDbType.NVarChar, Value = tableName }
            };

            var cleanupTable = GetAttributeValue( "CleanupTableParameter" ).AsBoolean();
            if ( cleanupTable )
            {
                paramsList.Add( new SqlParameter { ParameterName = "@CleanupTable", SqlDbType = SqlDbType.NVarChar, Value = 1 } );
            }

            var storedProcedure = GetAttributeValue( "DefaultStoredProcedure" );
            if ( !string.IsNullOrWhiteSpace( storedProcedure ) )
            {
                RunSQL( storedProcedure, paramsList, true, out statusText );
            }
            else
            {
                nbWarning.Text = "Block settings were not configured with a stored procedure.";
                return;
            }
        }

        /// <summary>
        /// Transforms the file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="errors">The errors.</param>
        /// <returns></returns>
        public bool TransformFile( string filePath, out string tableName, out string errors )
        {
            var transformResult = false;
            DataTable tableToUpload = null;
            tableName = string.Empty;
            errors = string.Empty;

            if ( !File.Exists( filePath ) )
            {
                filePath = this.Request.MapPath( filePath );
                var fileInfo = new FileInfo( filePath );

                using ( var stream = File.OpenRead( filePath ) )
                {
                    if ( fileInfo.Extension.Equals( ".csv", StringComparison.CurrentCultureIgnoreCase ) )
                    {
                        using ( var pack = new CsvReader( new StreamReader( stream ) ) )
                        {
                            tableToUpload = pack.TransformTable();
                        }
                    }

                    if ( fileInfo.Extension.Equals( ".xls", StringComparison.CurrentCultureIgnoreCase ) || fileInfo.Extension.Equals( ".xlsx", StringComparison.CurrentCultureIgnoreCase ) )
                    {
                        using ( ExcelPackage pack = new ExcelPackage( stream ) )
                        {
                            tableToUpload = pack.TransformTable();
                        }
                    }
                }

                if ( tableToUpload != null )
                {
                    // generate the table creation
                    tableToUpload.TableName = fileInfo.Name.Replace( fileInfo.Extension, "" );
                    tableToUpload.TableName = tableToUpload.TableName.RemoveSpecialCharacters();
                    tableToUpload.TableName = "_rocks_kfs_" + tableToUpload.TableName;
                    var sb = new System.Text.StringBuilder(
                        string.Format( @"IF EXISTS
                                    ( SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}')
                                    DROP TABLE [{0}];
                                    CREATE TABLE [{0}] (", tableToUpload.TableName ) );
                    foreach ( DataColumn column in tableToUpload.Columns )
                    {
                        sb.Append( " [" + column.ColumnName.RemoveSpecialCharacters() + "] " + TableExtensions.GetSQLType( column ) + "," );
                    }

                    var createTableScript = sb.ToString().TrimEnd( new char[] { ',' } ) + ")";
                    RunSQL( createTableScript, new List<SqlParameter>(), false, out errors );

                    // wait for the table to be created
                    Thread.Sleep( 1000 );

                    var conn = ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString;
                    using ( var bulkCopy = new SqlBulkCopy( conn ) )
                    {
                        bulkCopy.DestinationTableName = string.Format( "[{0}]", tableToUpload.TableName );
                        try
                        {
                            foreach ( var column in tableToUpload.Columns )
                            {
                                // use the original column to map headers, but trim in SQL
                                bulkCopy.ColumnMappings.Add( column.ToString(), column.ToString().RemoveSpecialCharacters() );
                            }
                            bulkCopy.WriteToServer( tableToUpload );
                        }
                        catch ( Exception ex )
                        {
                            errors = ex.Message;
                        }
                    }

                    if ( string.IsNullOrWhiteSpace( errors ) )
                    {
                        transformResult = true;
                        tableName = tableToUpload.TableName;
                    }
                }

                // cleanup whether we could read the file or not
                File.Delete( filePath );
            }

            return transformResult;
        }

        /// <summary>
        /// Runs the SQL.
        /// </summary>
        /// <param name="sqlCommand">The SQL command.</param>
        /// <param name="sqlParams">The SQL parameters.</param>
        /// <param name="storedProcedure">if set to <c>true</c> [stored procedure].</param>
        /// <param name="statusText">The status text.</param>
        public void RunSQL( string sqlCommand, List<SqlParameter> sqlParams, bool storedProcedure, out string statusText )
        {
            statusText = string.Empty;
            try
            {
                // remove any existing commands
                Session.RemoveAll();
                if ( _trigger != null )
                {
                    _trigger.Dispose();
                }

                // create command and assign delegate handlers
                _trigger = new LogTrigger();
                _trigger.LogEvent += new LogTrigger.InfoMessage( LogEvent );
                _trigger.connection = new SqlConnection( TableExtensions.GetConnectionString() );
                _trigger.command = new SqlCommand( sqlCommand );

                if ( storedProcedure )
                {
                    _trigger.command.CommandType = CommandType.StoredProcedure;
                }

                foreach ( var param in sqlParams )
                {
                    _trigger.command.Parameters.Add( param );
                }

                // start the command and the timer
                _trigger.Start();
                tmrSyncSQL.Enabled = true;
                Session["trigger"] = _trigger;
            }
            catch ( Exception ex )
            {
                statusText += string.Format( "Could not connect to the database. {0}<br>", ex.Message );
            }
        }
    }
}
