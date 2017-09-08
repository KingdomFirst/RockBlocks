using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using com.kfs.Reporting.SQLReportingServices;
using Microsoft.Reporting.WebForms;
using Rock;
using Rock.Attribute;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Reporting
{
    /// <summary>
    /// KFS Reporting Services Viewer Block
    /// </summary>
    /// <seealso cref="Rock.Web.UI.RockBlock" />
    [DisplayName( "Reporting Services Viewer" )]
    [Category( "KFS > Reporting" )]
    [TextField( "Report Path", "Relative Path to Reporting Services Report. Used in single report mode, and will overide ReportPath page parameter.", false, "", "Report Configuration", 0, "ReportPath" )]
    [KeyValueListField( "Report Parameters", "Report Parameters.", false, "", "Name", "Value", Category = "Report Configuration", Order = 1, Key = "ReportParameters" )]
    public partial class ReportingServicesViewer : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
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
                        ShowError( "Authorization Error", "Browser User Could not authenticate to Reporting Server." );
                    }
                }
                catch ( System.ServiceModel.Security.MessageSecurityException )
                {
                    ShowError( "Authorization Error", "Browser User could not authenticate to Reporting Server." );
                }
                catch ( System.ServiceModel.EndpointNotFoundException )
                {
                    ShowError( "Connection Error", "An error occurred when connecting to the reporting server." );
                }
            }
        }

        /// <summary>
        /// Loads the report.
        /// </summary>
        private void LoadReport()
        {
            string reportPath = null;

            const string pageTitleFormat = "{0} Report Viewer";
            var reportPathAttribute = GetAttributeValue( "ReportPath" );
            var reportParamAttributes = GetAttributeValue( "ReportParameters" ).AsDictionaryOrNull();

            if ( !string.IsNullOrWhiteSpace( reportPathAttribute ) )
            {
                reportPath = reportPathAttribute;
            }
            else if ( !string.IsNullOrWhiteSpace( PageParameter( "reportPath" ) ) )
            {
                reportPath = PageParameter( "reportPath" );
                reportPath = Server.UrlDecode( reportPath );
            }
            else
            {
                pnlReportViewer.Visible = false;
                lReportTitle.Text = string.Format( pageTitleFormat, string.Empty ).Trim();
                return;
            }
            var rsItem = ReportingServiceItem.GetItemByPath( reportPath );
            if ( rsItem == null )
            {
                ShowError( "Error", "Report Not Found" );
                pnlReportViewer.Visible = false;
                lReportTitle.Text = string.Format( pageTitleFormat, string.Empty ).Trim();
                return;
            }

            lReportTitle.Text = string.Format( pageTitleFormat, rsItem.Name );

            var provider = new ReportingServicesProvider();
            rsViewer.ProcessingMode = ProcessingMode.Remote;
            rsViewer.Height = Unit.Percentage( 98 );
            rsViewer.Width = Unit.Percentage( 100 );
            var report = rsViewer.ServerReport;
            report.ReportServerUrl = new Uri( provider.ServerUrl );
            report.ReportPath = reportPath;
            report.ReportServerCredentials = provider.GetBrowserCredentials();

            var reportParams = new List<ReportParameter>();

            foreach ( ReportParameterInfo parameter in report.GetParameters() )
            {
                string paramValue = null;
                if ( reportParamAttributes != null && reportParamAttributes.ContainsKey( parameter.Name ) )
                {
                    paramValue = reportParamAttributes[parameter.Name];
                }
                else if ( !string.IsNullOrWhiteSpace( PageParameter( parameter.Name ) ) )
                {
                    paramValue = PageParameter( parameter.Name );
                }

                if ( !string.IsNullOrWhiteSpace( paramValue ) )
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

        /// <summary>
        /// Shows the error.
        /// </summary>
        /// <param name="errorTitle">The error title.</param>
        /// <param name="errorText">The error text.</param>
        private void ShowError( string errorTitle, string errorText )
        {
            nbError.Title = errorTitle;
            nbError.Text = errorText;

            nbError.Visible = !string.IsNullOrWhiteSpace( errorText );
        }
    }
}
