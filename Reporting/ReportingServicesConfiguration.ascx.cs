using System;
using System.ComponentModel;
using System.Web.UI;
using com.kfs.Reporting.SQLReportingServices;
using Rock;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Reporting
{
    /// <summary>
    /// KFS Reporting Services Configuration Block
    /// </summary>
    /// <seealso cref="Rock.Web.UI.RockBlock" />
    [DisplayName( "Reporting Services Configuration" )]
    [Category( "KFS > Reporting" )]
    [BooleanField( "Use Separate Content Manager User", "Use separate Content Manager user and Browser user.", false, "", 0, "UseCMUser" )]
    [Description( "SQL Server Reporting Services Setup and Configuration." )]
    public partial class ReportingServicesConfiguration : RockBlock
    {
        #region fields

        private bool useCMUser = false;

        #endregion

        #region Page Events

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
            DisplayMessage( null );
            LoadBlockSettings();
            if ( !Page.IsPostBack )
            {
                pnlAdminUser.Visible = useCMUser;

                TestConnection();
                LoadCredentials();
                //btnConfigure.Visible = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            SaveCredentials();
        }

        /// <summary>
        /// Handles the Click event of the btnConfigure control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfigure_Click( object sender, EventArgs e )
        {
            ReportingServiceItem.GetFoldersTree( "", true, true );
        }

        /// <summary>
        /// Handles the Click event of the btnVerify control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnVerify_Click( object sender, EventArgs e )
        {
            TestConnection();
        }

        #endregion

        #region Private Events

        /// <summary>
        /// Displays the message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void DisplayMessage( string message )
        {
            DisplayMessage( message, NotificationBoxType.Default );
        }

        /// <summary>
        /// Displays the message with a custom notification type.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="nbType">Type of the nb.</param>
        private void DisplayMessage( string message, NotificationBoxType nbType )
        {
            nbReportingServices.Text = message;
            nbReportingServices.NotificationBoxType = nbType;
            nbReportingServices.Visible = !string.IsNullOrWhiteSpace( message );
        }

        /// <summary>
        /// Loads the block settings.
        /// </summary>
        private void LoadBlockSettings()
        {
            useCMUser = GetAttributeValue( "UseCMUser" ).AsBoolean();
        }

        /// <summary>
        /// Loads the credentials.
        /// </summary>
        private void LoadCredentials()
        {
            var provider = new ReportingServicesProvider();
            btnVerify.Visible = provider.CredentialsStored;
            tbReportingServicesURL.Text = provider.ServerUrl;
            tbReportRootFolder.Text = provider.ReportPath;

            if ( useCMUser )
            {
                tbAdminUserName.Text = provider.ContentManagerUser;
                tbAdminUserName.Required = true;
                tbAdminPassword.Text = string.Empty;
                if ( !string.IsNullOrWhiteSpace( provider.ContentManagerPassword ) )
                {
                    tbAdminPassword.Placeholder = "Stored";
                    tbAdminPassword.Required = false;
                    hfAdminPasswordSet.Value = bool.TrueString;
                }
                else
                {
                    tbAdminPassword.Placeholder = string.Empty;
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
                tbPassword.Placeholder = string.Empty;
                tbPassword.Required = true;
                hfPasswordSet.Value = bool.FalseString;
            }
        }

        /// <summary>
        /// Saves the credentials.
        /// </summary>
        private void SaveCredentials()
        {
            var provider = new ReportingServicesProvider
            {
                ServerUrl = tbReportingServicesURL.Text.Trim()
            };

            if ( string.IsNullOrWhiteSpace( provider.ServerUrl ) )
            {
                DisplayMessage( "<strong>Error</strong> - Server URL is not formatted properly.", NotificationBoxType.Danger );
                return;
            }
            provider.ReportPath = tbReportRootFolder.Text.Trim();

            provider.BrowserUser = tbUserName.Text.Trim();

            if ( !hfPasswordSet.Value.AsBoolean() || !string.IsNullOrWhiteSpace( tbPassword.Text ) )
            {
                provider.BrowserPassword = tbPassword.Text;
            }

            if ( useCMUser )
            {
                provider.ContentManagerUser = tbAdminUserName.Text.Trim();
                if ( !hfAdminPasswordSet.Value.AsBoolean() || !string.IsNullOrWhiteSpace( tbAdminPassword.Text ) )
                {
                    provider.ContentManagerPassword = tbAdminPassword.Text.Trim();
                }
            }
            else
            {
                provider.ContentManagerUser = tbUserName.Text.Trim();
                if ( !hfPasswordSet.Value.AsBoolean() || !string.IsNullOrWhiteSpace( tbPassword.Text ) )
                {
                    provider.ContentManagerPassword = tbPassword.Text;
                }
            }

            var message = string.Empty;
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

        /// <summary>
        /// Tests the connection.
        /// </summary>
        private void TestConnection()
        {
            var provider = new ReportingServicesProvider();

            if ( !provider.CredentialsStored )
            {
                return;
            }
            var message = string.Empty;
            var connectionSuccess = provider.TestConnection( out message, UserType.Browser );

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
