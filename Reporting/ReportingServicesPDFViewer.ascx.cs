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
    [DisplayName( "Reporting Services PDF Viewer" )]
    [Category( "KFS > Reporting" )]
    [TextField( "Report Path", "Relative Path to Reporting Services Report. Used in single report mode, and will overide \"ReportPath\" page parameter.", false, "", "Report Configuration", 0, "ReportPath" )]
    [KeyValueListField( "Report Parameters", "Report Parameters.", false, "", "Name", "Value", Category = "Report Configuration", Order = 1, Key = "ReportParameters" )]

    public partial class ReportingServicesPDFViewer : RockBlock
    {
        #region Page Event
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

        }
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            SetError( null, null );
            if ( !Page.IsPostBack )
            {
                LoadReport();
            }
        }
        #endregion

        #region Private Method

        private string GetReportPath( ReportingServicesProvider provider )
        {
            //get path from attributes
            string reportPath = GetAttributeValue( "ReportPath" );

            //if path not provided attempt to get it from query string
            if ( String.IsNullOrWhiteSpace( reportPath ) )
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



        private void LoadReport()
        {
            phViewer.Controls.Clear();
            ReportingServicesProvider provider = new ReportingServicesProvider();
            string reportPath = GetReportPath( provider );
            lReportTitle.Text = "Report Viewer";

            if ( String.IsNullOrWhiteSpace( reportPath ) )
            {
                pnlPdfViewer.Visible = false;
                SetError( "Report Path Error", "Report Path is required." );
                return;
            }

            var rsItem = ReportingServiceItem.GetItemByPath( reportPath );

            if ( rsItem == null )
            {
                pnlPdfViewer.Visible = false;
                SetError( "Report Path Error", "Report Not Found" );
                return;
            }

            var paramNames = ReportingServiceItem.GetReportParameterList( provider, reportPath );
            lReportTitle.Text = string.Format( "{0} Viewer", rsItem.Name );
            Dictionary<string, string> paramValues = new Dictionary<string, string>();
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

                if ( !String.IsNullOrWhiteSpace( paramValue ) )
                {
                    paramValue = HttpUtility.UrlEncode( paramValue );
                }
                else
                {
                    paramValue = PageParameter( pn );
                }

                if ( !String.IsNullOrWhiteSpace( paramValue ) )
                {
                    paramValues.Add( pn, paramValue );
                }
            }

            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.AppendFormat( "{0}?reportPath={1}",
                ResolveRockUrl( "~/Plugins/KFS/Reporting/GetReportingServicesPDF.ashx" ),
                reportPath );
            foreach ( var param in paramValues )
            {
                urlBuilder.AppendFormat( "&{0}={1}", param.Key, param.Value );
            }

            string iframeTag = string.Format( "<iframe src=\"{0}\" id=\"ifReportPDF\" class=\"col-sm-12\"></iframe>", urlBuilder.ToString() );
            phViewer.Controls.Add( new LiteralControl( iframeTag ) );
            pnlPdfViewer.Visible = true;

        }

        private void SetError( string title, string error )
        {
            nbError.Title = title;
            nbError.Text = error;
            nbError.Visible = !String.IsNullOrWhiteSpace( error );
        }
        #endregion

    }
}