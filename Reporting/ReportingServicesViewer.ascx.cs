using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Reporting.WebForms;

using com.kfs.Reporting.SQLReportingServices;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Reporting
{
    [DisplayName ("Reporting Services Viewer")]
    [Category( "KFS > Reporting" )]
    [TextField("Report Path", "Relative Path to Reporting Services Report. Used in single report mode, and will overide \"ReportPath\" page parameter.", false, "", "Report Configuration", 0, "ReportPath")]
    [KeyValueListField("Report Parameters", "Report Parameters.", false, "", "Name", "Value", Category = "Report Configuration", Order = 1, Key ="ReportParameters")]
    public partial class ReportingServicesViewer : RockBlock
    {
        string reportPathAttribute = null;
        Dictionary<string, string> reportParamAttributes = null;
        string pageTitleFormat = "{0} Report Viewer";
        protected override void OnLoad( EventArgs e )
        {

            base.OnLoad( e );
            LoadAttributes();
            if ( !Page.IsPostBack )
            {
                LoadReport();
            }

        }

        private void LoadAttributes()
        {
            reportPathAttribute = GetAttributeValue( "ReportPath" );
            reportParamAttributes = GetAttributeValue( "ReportParameters" ).AsDictionaryOrNull();
        }


        private void LoadReport()
        {

            
            string reportPath = null;

            if ( !String.IsNullOrWhiteSpace( reportPathAttribute ) )
            {
                reportPath = reportPathAttribute;
            }
            else if ( !String.IsNullOrWhiteSpace( PageParameter( "reportPath" ) ) )
            {

                reportPath = PageParameter( "reportPath" );
                reportPath = Server.UrlDecode( reportPath );
            }
            else
            {
                pnlReportViewer.Visible = false;
                ShowError( "Report Path Required" );
                return;
            }
            var rsItem = ReportingServiceItem.GetItemByPath( reportPath );
            if ( rsItem == null )
            {
                ShowError( "Report Not Found" );
                pnlReportViewer.Visible = false;
                lReportTitle.Text = string.Format( pageTitleFormat, String.Empty );
                return;
               
            }

            lReportTitle.Text = string.Format( pageTitleFormat, rsItem.Name );

            ReportingServicesProvider provider = new ReportingServicesProvider();
            rsViewer.ProcessingMode = ProcessingMode.Remote;
            rsViewer.Height = Unit.Percentage( 98 );
            rsViewer.Width = Unit.Percentage( 100 );
            ServerReport report = rsViewer.ServerReport;
            report.ReportServerUrl = new Uri( provider.ServerUrl );
            report.ReportPath = reportPath;
            report.ReportServerCredentials = provider.GetBrowserCredentials();
           


            List<ReportParameter> reportParams = new List<ReportParameter>();

            foreach ( ReportParameterInfo parameter in report.GetParameters() )
            {
                string paramValue = null;
                if ( reportParamAttributes.ContainsKey( parameter.Name ) )
                {
                    paramValue = reportParamAttributes[parameter.Name];
                }
                else if ( !String.IsNullOrWhiteSpace( PageParameter( parameter.Name ) ) )
                {
                    paramValue = PageParameter( parameter.Name );
                }

                if ( !String.IsNullOrWhiteSpace( paramValue ) )
                {
                    reportParams.Add( new ReportParameter( parameter.Name, paramValue ) );
                }
            }
            if ( reportParams.Count > 0 )
            {
                report.SetParameters( reportParams.ToArray() );
                report.Refresh();
            }
            pnlReportViewer.Visible = true;

        }

        private void ShowError( string errorText )
        {
            nbError.Title = "Error";
            nbError.Text = errorText;

            nbError.Visible = !String.IsNullOrWhiteSpace( errorText );
        }
       


    }
}