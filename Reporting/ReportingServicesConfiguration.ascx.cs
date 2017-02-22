using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using com.kfs.Reporting.SQLReportingServices;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace Plugins.com_kfs.Reporting
{

    [DisplayName("Reporting Services Configuration")]
    [Category("KFS > Reporting")]
    [BooleanField("Use Separate Content Manager User", "Use separate Content Manager user and Browser user.", false, "", 0, "UseCMUser")]
    
    [Description("SQL Server Reprting Services Setup and Configuration.")]
    public partial class ReportingServicesConfiguration : RockBlock
    {

        #region fields
        bool useCMUser = false;
        #endregion
        #region Page Events
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            DisplayMessage( null );
            LoadBlockSettings();
            if ( !Page.IsPostBack )
            {
                SetAdminFieldVisibility();
                TestConnection();
                
            }
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            SaveCredentials();
        }

        protected void btnConfigure_Click( object sender, EventArgs e )
        {

        }

        protected void btnVerify_Click( object sender, EventArgs e )
        {
            TestConnection();
        }

        #endregion

        #region Private Events
        private void DisplayMessage( string message )
        {
            DisplayMessage( message, NotificationBoxType.Default );
        }

        private void DisplayMessage( string message, NotificationBoxType nbType )
        {
            nbReportingServices.Text = message;
            nbReportingServices.NotificationBoxType = nbType;
            nbReportingServices.Visible = !String.IsNullOrWhiteSpace( message );

        }

        private void LoadBlockSettings()
        {
            useCMUser = GetAttributeValue( "UseCMUser" ).AsBoolean();
        }

        private void LoadCredentials()
        {
            ReportingServicesClient client = new ReportingServicesClient();
            tbReportingServicesURL.Text = client.ServerUrl;
            tbReportRootFolder.Text = client.ReportPath;

            if ( useCMUser )
            {
                tbAdminUserName.Text = client.ContentManagerUser;
                tbAdminUserName.Required = true;
                tbAdminPassword.Text = String.Empty;
                if ( string.IsNullOrWhiteSpace( client.ContentManagerPassword ) )
                {
                    tbAdminPassword.Placeholder = "Stored";
                    tbAdminPassword.Required = false;
                    hfAdminPasswordSet.Value = bool.TrueString;

                }
                else
                {
                    tbAdminPassword.Placeholder = String.Empty;
                    tbAdminPassword.Required = true;
                    hfAdminPasswordSet.Value = bool.FalseString;
                }
            }
            else
            {
                tbAdminUserName.Required = false;
                tbAdminPassword.Required = false;
            }

            tbUserName.Text = client.BrowserUser;

            if ( string.IsNullOrWhiteSpace( client.BrowserPassword ) )
            {
                tbPassword.Placeholder = "Stored";
                tbPassword.Required = false;
                hfPasswordSet.Value = bool.TrueString;
            }
            else
            {
                tbPassword.Placeholder = String.Empty;
                tbPassword.Required = true;
                hfPasswordSet.Value = bool.FalseString;
            }
        }

        private void SetAdminFieldVisibility()
        {
            pnlAdminUser.Visible = useCMUser;
        }

        private void SaveCredentials()
        {
            ReportingServicesClient client = new ReportingServicesClient();
            client.ServerUrl = tbReportingServicesURL.Text.Trim();
            client.ReportPath = tbReportRootFolder.Text.Trim();

            if ( useCMUser )
            {
                client.ContentManagerUser = tbAdminUserName.Text.Trim();
                if ( !hfAdminPasswordSet.Value.AsBoolean() || !String.IsNullOrWhiteSpace( tbAdminPassword.Text ) )
                {
                    client.ContentManagerPassword = tbAdminPassword.Text.Trim();
                }

            }

            client.BrowserUser = tbUserName.Text.Trim();

            if ( !hfPasswordSet.Value.AsBoolean() || !String.IsNullOrWhiteSpace( tbPassword.Text ) )
            {
                client.BrowserPassword = tbPassword.Text;
            }
            string message = String.Empty;
            if ( client.SaveCredentials( out message ) )
            {
                LoadCredentials();
                DisplayMessage( "<strong>Success</strong> - Credentials Saved", NotificationBoxType.Success );
            }
            else
            {
                DisplayMessage( message, NotificationBoxType.Danger );
            }
        }

        private void TestConnection()
        {
            ReportingServicesClient client = new ReportingServicesClient();

            if ( client.Configured )
            {
                string message = String.Empty;

                if ( client.TestConnection( out message ) )
                {
                    DisplayMessage( message, NotificationBoxType.Success );
                }
                else
                {
                    DisplayMessage( message, NotificationBoxType.Danger );
                }
            }

            
        }
        #endregion
    }
}