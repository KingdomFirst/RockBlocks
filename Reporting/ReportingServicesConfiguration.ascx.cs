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
                LoadCredentials();
                //btnConfigure.Visible = true;
                
            }
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            SaveCredentials();
        }

        protected void btnConfigure_Click( object sender, EventArgs e )
        {
            ReportingServiceItem.GetFoldersTree( "", true, true );
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
            ReportingServicesProvider provider = new ReportingServicesProvider();
            btnVerify.Visible = provider.CredentialsStored;
            tbReportingServicesURL.Text = provider.ServerUrl;
            tbReportRootFolder.Text = provider.ReportPath;

            if ( useCMUser )
            {
                tbAdminUserName.Text = provider.ContentManagerUser;
                tbAdminUserName.Required = true;
                tbAdminPassword.Text = String.Empty;
                if ( !string.IsNullOrWhiteSpace( provider.ContentManagerPassword ) )
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

            tbUserName.Text = provider.BrowserUser;

            if ( !string.IsNullOrWhiteSpace( provider.BrowserPassword ) )
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
            ReportingServicesProvider provider = new ReportingServicesProvider();
            provider.ServerUrl = tbReportingServicesURL.Text.Trim();

            if ( String.IsNullOrWhiteSpace( provider.ServerUrl ) )
            {
                DisplayMessage( "<strong>Error</strong> - Server URL is not formatted properly.", NotificationBoxType.Danger );
                return;
            }
            provider.ReportPath = tbReportRootFolder.Text.Trim();

            provider.BrowserUser = tbUserName.Text.Trim();

            if ( !hfPasswordSet.Value.AsBoolean() || !String.IsNullOrWhiteSpace( tbPassword.Text ) )
            {
                provider.BrowserPassword = tbPassword.Text;
            }

            if ( useCMUser )
            {
                provider.ContentManagerUser = tbAdminUserName.Text.Trim();
                if ( !hfAdminPasswordSet.Value.AsBoolean() || !String.IsNullOrWhiteSpace( tbAdminPassword.Text ) )
                {
                    provider.ContentManagerPassword = tbAdminPassword.Text.Trim();
                }

            }
            else
            {
                provider.ContentManagerUser = tbUserName.Text.Trim();
                if ( !hfPasswordSet.Value.AsBoolean() || !String.IsNullOrWhiteSpace( tbPassword.Text ) )
                {
                    provider.ContentManagerPassword = tbPassword.Text;
                }
            }

            string message = String.Empty;
            if ( provider.SaveCredentials( out message ) )
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
            ReportingServicesProvider provider = new ReportingServicesProvider();

            if ( !provider.CredentialsStored )
            {
                return;
            }
            string message = String.Empty;
            bool connectionSuccess = provider.TestConnection( out message, UserType.Browser  );

            if ( !connectionSuccess )
            {
                DisplayMessage( message, NotificationBoxType.Danger );
                return;
            }

            if ( !provider.TestPath() )
            {
                message = string.Format( "<strong>Warning</strong> - Report Path not found ({0})", provider.ReportPath );
                DisplayMessage( message, NotificationBoxType.Warning );
                //btnConfigure.Visible = true;
                return;
            }

            if ( !provider.TestDataSource( out message ) )
            {
                DisplayMessage( "<strong>Datasource Error</strong> - " + message, NotificationBoxType.Warning );
                //btnConfigure.Visible = true;
                return;
            }

            DisplayMessage( "<strong>Success</strong> - Successfully Configured.", NotificationBoxType.Success );
            
        }
        #endregion
    }
}