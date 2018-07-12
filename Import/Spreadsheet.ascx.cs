using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using CsvHelper;
using OfficeOpenXml;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Import
{
    [DisplayName( "Spreadsheet Import" )]
    [Category( "Spreadsheet Import" )]
    [Description( "Block to import spreadsheet data and optionally run a stored procedure." )]
    [CustomDropdownListField( "Default Stored Procedure", "Select the default stored procedure to run when importing a spreadsheet.", "com_kfs_spTransactionImport", false, "", "", 0 )]
    [BooleanField( "Cleanup Table Parameter", "Select this if the SP has a @CleanupTable parameter.", true )]
    public partial class Spreadsheet : RockBlock
    {
        //SELECT ROUTINE_NAME, ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'

        public AsyncTrigger _trigger;

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
                _trigger = (AsyncTrigger)Session["trigger"];
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
                foreach ( string s in Session["log"].ToString().Split( new char[] { '|' } ) )
                {
                    if ( !string.IsNullOrWhiteSpace( s ) )
                    {
                        lblSqlStatus.Text += s + "<br>";
                    }
                }

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
                FileInfo fileInfo = new FileInfo( physicalFile );
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

            // get SP name
            var storedProcedure = GetAttributeValue( "DefaultStoredProcedure" );

            // create parameters
            var paramsList = new List<SqlParameter>
            {
                // TODO clean this up to read parameters from SP
                new SqlParameter { ParameterName = "@TransactionTable", SqlDbType = SqlDbType.NVarChar, Value = tableName }
                //new SqlParameter { ParameterName = "@BatchPrefix", SqlDbType = SqlDbType.NVarChar, Value = GetAttributeValue( "BatchPrefix" ) }
            };

            var cleanupTable = GetAttributeValue( "CleanupTableParameter" ).AsBoolean();
            if ( cleanupTable  )
            {
                paramsList.Add( new SqlParameter { ParameterName = "@CleanupTable", SqlDbType = SqlDbType.NVarChar, Value = 1 } );
            }

            RunSQL( storedProcedure, paramsList, true, out statusText );
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
                var stream = File.OpenRead( filePath );

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

                if ( tableToUpload != null )
                {
                    // generate the table creation
                    tableToUpload.TableName = fileInfo.Name.Replace( fileInfo.Extension, "" );
                    tableToUpload.TableName = tableToUpload.TableName.RemoveSpecialCharacters();
                    tableToUpload.TableName = "_com_kfs_" + tableToUpload.TableName;
                    var sb = new System.Text.StringBuilder( string.Format("DROP TABLE IF EXISTS [{0}]; CREATE TABLE [{0}] (", tableToUpload.TableName ) );
                    foreach ( DataColumn column in tableToUpload.Columns )
                    {
                        sb.Append( " [" + column.ColumnName.RemoveSpecialCharacters() + "] " + SpreadsheetExtensions.GetSQLType( column ) + "," );
                    }

                    var createTableScript = sb.ToString().TrimEnd( new char[] { ',' } ) + ")";
                    RunSQL( createTableScript, new List<SqlParameter>(), false, out errors );

                    // wait for the table to be created
                    Thread.Sleep( 1000 );

                    string conn = ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString;
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
                _trigger = new AsyncTrigger();
                _trigger.LogEvent += new AsyncTrigger.InfoMessage( LogEvent );
                _trigger.connection = new SqlConnection( GetConnectionString() );
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

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <returns></returns>
        private static string GetConnectionString()
        {
            var builder = new SqlConnectionStringBuilder( ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString )
            {
                AsynchronousProcessing = true,
                MultipleActiveResultSets = true,
                ConnectTimeout = 5
            };

            return builder.ConnectionString;
        }
    }

    #region Async Triggers

    /// <summary>
    /// Async Trigger to track SQL progress
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class AsyncTrigger : System.Object, IDisposable
    {
        public SqlCommand command;
        public SqlConnection connection;
        public bool isExecuting = true;

        protected Thread bgThread;

        public AsyncTrigger()
        {
        }

        public AsyncTrigger( SqlConnection conn, SqlCommand comm )
        {
            connection = conn;
            command = comm;
        }

        public delegate void InfoMessage( object sender, string Message );

        public event InfoMessage LogEvent;

        public void Dispose()
        {
            try
            {
                if ( connection != null ) connection.Dispose();
                if ( command != null ) command.Dispose();
            }
            catch { }
        }

        public void ExecSql()
        {
            if ( connection == null || command == null )
            {
                return;
            }

            if ( LogEvent != null )
            {
                connection.FireInfoMessageEventOnUserErrors = true;
                connection.InfoMessage += new SqlInfoMessageEventHandler( SqlEventTrigger );
            }

            connection.Open();
            command.Connection = connection;
            command.CommandTimeout = 0;
            command.ExecuteNonQuery();
            connection.Close();

            isExecuting = false;
        }

        public void SqlEventTrigger( object sender, SqlInfoMessageEventArgs e )
        {
            if ( LogEvent != null )
            {
                LogEvent( sender, e.Message );
            }
        }

        public void Join()
        {
            if ( bgThread != null && bgThread.IsAlive )
            {
                bgThread.Join();
            }
        }

        public void Start()
        {
            bgThread = new Thread( new ThreadStart( ExecSql ) );
            bgThread.Start();
        }

        public void Stop()
        {
            if ( bgThread != null && bgThread.IsAlive )
            {
                bgThread.Abort();
            }

            Join();
        }
    }

    #endregion

    #region Spreadsheet extensions

    public static class SpreadsheetExtensions
    {
        /// <summary>
        /// Transforms the CSV into a table.
        /// </summary>
        /// <param name="csv">The CSV.</param>
        /// <returns></returns>
        public static DataTable TransformTable( this CsvReader csv )
        {
            var table = new DataTable();

            csv.Read();
            foreach ( var header in csv.FieldHeaders.Where( h => !string.IsNullOrWhiteSpace( h ) ) )
            {
                table.Columns.Add( header );
            }

            do
            {
                var row = table.NewRow();
                foreach ( DataColumn column in table.Columns )
                {
                    row[column.ColumnName] = csv.GetField( column.DataType, column.ColumnName );
                    if ( column.DataType == typeof( string ) )
                    {
                        column.MaxLength = Math.Max( column.MaxLength, row[column.ColumnName].ToString().Length );
                    }
                }

                table.Rows.Add( row );
            }
            while ( csv.Read() );

            return table;
        }

        /// <summary>
        /// Transforms the Excel file into a table.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns></returns>
        public static DataTable TransformTable( this ExcelPackage package )
        {
            if ( !package.Workbook.Worksheets.Any() )
            {
                return null;
            }

            int i = 0;
            var table = new DataTable();
            var skippedColumns = new List<int>();
            var sheet = package.Workbook.Worksheets.First();
            foreach ( var headers in sheet.Cells[1, 1, 1, sheet.Dimension.End.Column] )
            {
                if ( !string.IsNullOrWhiteSpace( headers.Text ))
                {
                    table.Columns.Add( headers.Text );
                }
                else
                {
                    skippedColumns.Add( i );
                }

                i++;
            }

            for ( var currentRow = 2; currentRow <= sheet.Dimension.End.Row; currentRow++ )
            {
                var numSkippedColumns = 0;
                var row = sheet.Cells[currentRow, 1, currentRow, sheet.Dimension.End.Column];
                var newRow = table.NewRow();
                foreach ( var cell in row )
                {
                    var spreadsheetIndex = cell.Start.Column - 1;
                    if ( !skippedColumns.Contains( spreadsheetIndex ) )
                    {
                        var importIndex = spreadsheetIndex - numSkippedColumns;
                        newRow[importIndex] = cell.Text;
                        table.Columns[importIndex].MaxLength = Math.Max( cell.Text.Length, table.Columns[importIndex].MaxLength );
                    }
                    else
                    {
                        numSkippedColumns++;
                    }
                }

                table.Rows.Add( newRow );
            }

            return table;
        }

        /// <summary>
        /// Gets the column type for SQL
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="columnSize">Size of the column.</param>
        /// <param name="numericPrecision">The numeric precision.</param>
        /// <param name="numericScale">The numeric scale.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetSQLType( object type, int columnSize, int numericPrecision, int numericScale )
        {
            switch ( type.ToString() )
            {
                case "System.String":
                    return "NVARCHAR(" + ( columnSize > 50 ? columnSize : 50 ) + ")";

                case "System.Decimal":
                    if ( numericScale > 0 )
                        return "REAL";
                    else if ( numericPrecision > 10 )
                        return "BIGINT";
                    else
                        return "INT";

                case "System.Double":
                case "System.Single":
                    return "REAL";

                case "System.Int64":
                    return "BIGINT";

                case "System.Int16":
                case "System.Int32":
                    return "INT";

                case "System.DateTime":
                    return "DATETIME";

                default:
                    throw new Exception( type.ToString() + " not implemented." );
            }
        }

        /// <summary>
        /// SQLs the type of the get.
        /// </summary>
        /// <param name="schemaRow">The schema row.</param>
        /// <returns></returns>
        public static string GetSQLType( DataRow schemaRow )
        {
            return GetSQLType( schemaRow["DataType"],
                int.Parse( schemaRow["ColumnSize"].ToString() ),
                int.Parse( schemaRow["NumericPrecision"].ToString() ),
                int.Parse( schemaRow["NumericScale"].ToString() ) );
        }

        // Overload based on DataColumn from DataTable type
        /// <summary>
        /// SQLs the type of the get.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        public static string GetSQLType( DataColumn column )
        {
            return GetSQLType( column.DataType, column.MaxLength, 10, 2 );
        }
    }

    #endregion
}
