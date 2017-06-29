using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;

namespace com.kfs.Reporting.SQLReportingServices
{
    /// <summary>
    /// KFS Reporting Services PDF Viewer Block
    /// </summary>
    /// <seealso cref="Rock.Web.UI.RockBlock" />
    [DisplayName( "Reporting Services PDF Viewer" )]
    [Category( "KFS > Reporting" )]
    [TextField( "Report Path", "Relative Path to Reporting Services Report. Used in single report mode, and will overide \"ReportPath\" page parameter.", false, "", "Report Configuration", 0, "ReportPath" )]
    [KeyValueListField( "Report Parameters", "Report Parameters.", false, "", "Name", "Value", Category = "Report Configuration", Order = 1, Key = "ReportParameters" )]
    [BooleanField( "Show PDF Viewer", "A flag that determines if the full PDF Viewer block should be rendered or only return the report pdf. Default is true.", true, "Advanced", 0, "ShowReportViewer" )]
    public partial class ReportingServicesPDFViewer : RockBlock
    {
        private bool mRenderPDFOnly = false;

        #region Page Event

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            mRenderPDFOnly = !GetAttributeValue( "ShowReportViewer" ).AsBoolean();
            SetError( null, null );
            if ( !Page.IsPostBack )
            {
                try
                {
                    LoadReport();
                }
                catch ( System.Net.WebException webEx )
                {
                    if ( ( (System.Net.HttpWebResponse)webEx.Response ).StatusCode == System.Net.HttpStatusCode.Unauthorized )
                    {
                        SetError( "Authorization Error", "Browser User Could not authenticate to Reporting Server." );
                    }
                }
                catch ( System.ServiceModel.Security.MessageSecurityException )
                {
                    SetError( "Authorization Error", "Browser User could not authenticate to Reporting Server." );
                }
                catch ( System.ServiceModel.EndpointNotFoundException )
                {
                    SetError( "Connection Error", "An error occurred when connecting to the reporting server." );
                }
            }
        }

        #endregion

        #region Private Method

        /// <summary>
        /// Gets the report path.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <returns></returns>
        private string GetReportPath( ReportingServicesProvider provider )
        {
            //get path from attributes
            var reportPath = GetAttributeValue( "ReportPath" );

            //if path not provided attempt to get it from query string
            if ( string.IsNullOrWhiteSpace( reportPath ) )
            {
                reportPath = HttpUtility.UrlDecode( PageParameter( "reportPath" ) );
            }

            //if report path found, format it
            if ( !string.IsNullOrWhiteSpace( reportPath ) )
            {
                reportPath = provider.GetFolderPath( reportPath );
            }

            return reportPath;
        }

        /// <summary>
        /// Loads the report.
        /// </summary>
        private void LoadReport()
        {
            pnlPdfViewer.Visible = false;
            phViewer.Controls.Clear();
            var provider = new ReportingServicesProvider();
            var reportPath = GetReportPath( provider );
            lReportTitle.Text = "Report Viewer";

            if ( string.IsNullOrWhiteSpace( reportPath ) )
            {
                SetError( "Report Path Error", "Report Path is required." );
                return;
            }

            var rsItem = ReportingServiceItem.GetItemByPath( reportPath );

            if ( rsItem == null )
            {
                SetError( "Report Path Error", "Report Not Found" );
                return;
            }

            var paramNames = ReportingServiceItem.GetReportParameterList( provider, reportPath );
            lReportTitle.Text = string.Format( "{0} Viewer", rsItem.Name );
            var paramValues = new Dictionary<string, string>();
            var paramAttribute = GetAttributeValue( "ReportParameters" ).AsDictionaryOrNull();
            foreach ( var pn in paramNames )
            {
                string paramValue = null;
                if ( paramAttribute != null )
                {
                    paramValue = paramAttribute
                                    .Where( a => a.Key.Equals( pn, StringComparison.InvariantCultureIgnoreCase ) )
                                    .Select( a => a.Value )
                                    .FirstOrDefault();
                }

                if ( !string.IsNullOrWhiteSpace( paramValue ) )
                {
                    paramValue = HttpUtility.UrlEncode( paramValue );
                }
                else
                {
                    paramValue = PageParameter( pn );
                }

                if ( !string.IsNullOrWhiteSpace( paramValue ) )
                {
                    paramValues.Add( pn, paramValue );
                }
            }

            var urlBuilder = new StringBuilder();
            urlBuilder.AppendFormat( "{0}?reportPath={1}",
                ResolveRockUrlIncludeRoot( "~/Plugins/com_kfs/Reporting/GetReportingServicesPDF.ashx" ),
                reportPath );
            foreach ( var param in paramValues )
            {
                urlBuilder.AppendFormat( "&{0}={1}", param.Key, param.Value );
            }

            if ( !mRenderPDFOnly )
            {
                var iframeTag = string.Format( "<iframe src=\"{0}\" id=\"ifReportPDF\" class=\"col-sm-12\"></iframe>", urlBuilder.ToString() );
                phViewer.Controls.Add( new LiteralControl( iframeTag ) );
                pnlPdfViewer.Visible = true;
            }
            else
            {
                var pdfBytes = GetPDFStream( urlBuilder.ToString() );
                base.Response.Clear();
                var dispositionType = string.Empty;

                if ( pdfBytes == null )
                {
                    base.Response.StatusCode = 404;
                }
                else
                {
                    base.Response.AppendHeader( "content-length", ( pdfBytes.Length ).ToString() );
                    //base.Resp
                    base.Response.AppendHeader( "content-disposition", string.Format( "filename={0}.pdf", rsItem.Name ) );
                    base.Response.ContentType = "applicaton/pdf";
                    base.Response.OutputStream.Write( pdfBytes, 0, pdfBytes.Length );
                }
                base.Response.Flush();
                base.Response.Close();
                base.Response.End();
            }
        }

        /// <summary>
        /// Gets the PDF stream.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private byte[] GetPDFStream( string url )
        {
            byte[] pdfBytes = null;

            var req = System.Net.WebRequest.Create( url );
            req.Method = "GET";

            var resp = (System.Net.HttpWebResponse)req.GetResponse();

            if ( resp.StatusCode == System.Net.HttpStatusCode.OK && resp.ContentType.IndexOf( "Application/PDF", StringComparison.InvariantCultureIgnoreCase ) >= 0 )
            {
                pdfBytes = resp.GetResponseStream().ReadBytesToEnd();
            }

            return pdfBytes;
        }

        /// <summary>
        /// Sets the error.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="error">The error.</param>
        /// <exception cref="Exception"></exception>
        private void SetError( string title, string error )
        {
            if ( !mRenderPDFOnly )
            {
                nbError.Title = title;
                nbError.Text = error;
                nbError.Visible = !string.IsNullOrWhiteSpace( error );
            }
            else if ( !string.IsNullOrWhiteSpace( error ) )
            {
                var exMessage = string.Format( "{0}{1}{2}", title, string.IsNullOrWhiteSpace( title ) ? "" : " - ", error );
                throw new Exception( exMessage.Trim() );
            }
        }

        #endregion
    }
}
