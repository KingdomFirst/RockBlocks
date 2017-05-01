using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using com.kfs.Reporting.SQLReportingServices;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.com_kfs.Reporting
{
    [DisplayName( "Reporting Services Tree" )]
    [Category( "KFS > Reporting" )]
    [Description( "SQL Server Reporting Services Tree View" )]

    [BooleanField( "Show Hidden Items", "Determines if hidden items should be displayed. Default is false.", false, "Configuration", 3, "ShowHiddenItems" )]
    [BooleanField( "Show Child Items", "Determines if child items should be displayed. Default is true", true, "Configuration", 0, "ShowChildItems" )]
    [TextField( "Root Folder", "Root/Base Folder", false, "/", "Configuration", 2, "RootFolder" )]
    [TextField("Header Text", "The text to be displayed in the header when in Standalone Mode", false, "", "", 0, "HeaderText")]
    [CustomRadioListField( "Selection Mode", "Reporting Services Tree selection mode.", "Folder,Report", true, "Folder", "Configuration", 2, "SelectionMode" )]
    [LinkedPage( "Report Viewer Page", "The page that contains the Reporting Services Report Viewer. If populated all report nodes will be clickable.", false, "", "Configuration", 4, "ReportViewerPage" )]
    [BooleanField( "Standalone Mode", "A flag indicating if this block is on a shared page with a report viewer or if it is on it's own page.", false, "Configuration", 4, "StandaloneMode" )]

    public partial class ReportingServicesFolderTree : RockBlock
    {
        bool showHiddenItems = false;
        bool showChildItems = false;
        bool standaloneMode = false;
        string rootFolder = null;
        string headerText = null;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            LoadAttributes();
            ShowError( string.Empty, string.Empty );
            if ( !Page.IsPostBack )
            {

                if ( standaloneMode )
                {
                    pnlFolders.CssClass += " panel panel-block";
                    pnlTree.CssClass += " panel-body";
                    pnlHeader.Visible = true;
                    lTitle.Text = headerText;
                }
                else
                {
                    pnlFolders.CssClass = pnlFolders.CssClass.Replace( "panel panel-block", "" ).Trim();
                    pnlTree.CssClass = pnlTree.CssClass.Replace( "panel-body", "" ).Trim();
                    pnlHeader.Visible = false;
                    lTitle.Text = String.Empty;
                }

            }
            var provider = new ReportingServicesProvider();
            if ( provider.CredentialsStored )
            {
                BuildTree();
            }
            else
            {
                ShowError( "Configuration Error", "Reporting Services has not been configured." );
            }
        }

        private void BuildTree()
        {
            try
            {

                string selectedItemPath = PageParameter( "reportPath" );

                if ( !string.IsNullOrWhiteSpace( selectedItemPath ) )
                {
                    selectedItemPath = HttpUtility.UrlDecode( selectedItemPath );
                    var selectedItem = ReportingServiceItem.GetItemByPath( selectedItemPath );

                    if ( selectedItem != null && selectedItem.Type == ItemType.Report )
                    {
                        hfSelectedItem.Value = string.Concat( "r-", selectedItemPath );
                    }
                    else if ( selectedItem != null && selectedItem.Type == ItemType.Folder )
                    {
                        hfSelectedItem.Value = string.Concat( "f-", selectedItemPath );
                    }

                }
                ReportingServiceItem rsItem = null;
                if ( hfSelectionType.Value.Equals( "Folder" ) )
                {
                    rsItem = ReportingServiceItem.GetFoldersTree( rootFolder, showChildItems, showHiddenItems );
                }
                else if ( hfSelectionType.Value.Equals( "Report" ) )
                {
                    rsItem = ReportingServiceItem.GetReportTree( rootFolder, showChildItems, showHiddenItems );
                }

                var treeBuilder = new StringBuilder();

                treeBuilder.AppendLine( "<ul id=\"treeview\">" );

                BuildTreeNode( rsItem, ref treeBuilder );

                treeBuilder.AppendLine( "</ul>" );
                lFolders.Text = treeBuilder.ToString();
            }
            catch ( System.Net.WebException webEx )
            {
                if ( ( ( System.Net.HttpWebResponse )webEx.Response ).StatusCode == System.Net.HttpStatusCode.Unauthorized )
                {
                    ShowError( "Authorization Error", "Browser User Could not authenticate to Reporting Server." );
                }

                else
                {
                    throw webEx;
                }

            }

            catch ( System.ServiceModel.Security.MessageSecurityException msgEx )
            {
                ShowError( "Authorization Error", "Browser User could not authenticate to Reporting Server." );
            }
            catch ( System.ServiceModel.EndpointNotFoundException ex404 )
            {
                ShowError( "Connection Error", "An error occurred when connecting to the reporting server." );
            }
        }

        private void BuildTreeNode( ReportingServiceItem item, ref StringBuilder treeBuilder )
        {
            string iconClass;
            string nodeIdPrefix;
            string viewerUrl = null;
            switch ( item.Type )
            {
                case ItemType.Folder:
                    iconClass = "fa-folder-o";
                    nodeIdPrefix = "f-";
                    break;
                case ItemType.Report:
                    iconClass = "fa-file-text-o";
                    nodeIdPrefix = "r-";
                    if ( !String.IsNullOrWhiteSpace( GetAttributeValue( "ReportViewerPage" ) ) )
                    {
                        var qsParams = new Dictionary<string, string>();
                        qsParams.Add( "reportPath", HttpUtility.UrlEncode( item.Path ) );
                        viewerUrl = LinkedPageUrl( "ReportViewerPage", qsParams );
                    }
                    break;
                default:
                    return;

            }
            string nodeId = string.Concat( nodeIdPrefix, item.Path );

            treeBuilder.AppendFormat(
                "<li data-expanded=\"{0}\"  data-modal=\"RSItem\" data-id=\"{1}\" data-type=\"{5}\"><span><span class=\"rollover-container\"><i class=\"fa {4}\">&nbsp;</i>{2}</span></span>{3}",
                ( hfSelectedItem.Value.Contains( item.Path ) ).ToString().ToLower(),
                nodeId,
                String.IsNullOrWhiteSpace( viewerUrl ) ? item.Name : string.Format( "<a href=\"{0}\">{1}</a>", viewerUrl, item.Name ),
                Environment.NewLine,
                iconClass,
                item.Type.ToString() );

            if ( item.Children != null && item.Children.Where( c => c.Type != ItemType.DataSource ).Count() > 0 )
            {
                treeBuilder.AppendLine( "<ul>" );
                foreach ( ReportingServiceItem child in item.Children )
                {
                    BuildTreeNode( child, ref treeBuilder );
                }
                treeBuilder.AppendLine( "</ul>" );
            }
            treeBuilder.AppendLine( "</li>" );
        }

        private void LoadAttributes()
        {
            showHiddenItems = GetAttributeValue( "ShowHiddenItems" ).AsBoolean();
            showChildItems = GetAttributeValue( "ShowChildItems" ).AsBoolean();
            rootFolder = GetAttributeValue( "RootFolder" );
            hfSelectionType.Value = GetAttributeValue( "SelectionMode" );
            standaloneMode = GetAttributeValue( "StandaloneMode" ).AsBoolean();
            headerText = GetAttributeValue( "HeaderText" );

        }

        private void ShowError( string title, string message )
        {
            nbRockError.Title = title;
            nbRockError.Text = message;

            nbRockError.Visible = !String.IsNullOrWhiteSpace( message );
        }
    }
}