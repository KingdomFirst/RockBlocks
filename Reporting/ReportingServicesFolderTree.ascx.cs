using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using com.kfs.Reporting.SQLReportingServices;
using Rock;
using Rock.Attribute;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Reporting
{
    /// <summary>
    /// KFS Reporting Services Tree View
    /// </summary>
    /// <seealso cref="Rock.Web.UI.RockBlock" />
    [DisplayName( "Reporting Services Tree" )]
    [Category( "KFS > Reporting" )]
    [Description( "SQL Server Reporting Services Tree View" )]
    [BooleanField( "Show Hidden Items", "Determines if hidden items should be displayed. Default is false.", false, "Configuration", 3, "ShowHiddenItems" )]
    [BooleanField( "Show Child Items", "Determines if child items should be displayed. Default is true", true, "Configuration", 0, "ShowChildItems" )]
    [TextField( "Root Folder", "Root/Base Folder", false, "/", "Configuration", 2, "RootFolder" )]
    [TextField( "Header Text", "The text to be displayed in the header when in Standalone Mode", false, "", "", 0, "HeaderText" )]
    [CustomRadioListField( "Selection Mode", "Reporting Services Tree selection mode.", "Report,Folder", true, "Report", "Configuration", 2, "SelectionMode" )]
    [LinkedPage( "Report Viewer Page", "The page that contains the Reporting Services Report Viewer. If populated all report nodes will be clickable.", false, "", "Configuration", 4, "ReportViewerPage" )]
    [BooleanField( "Standalone Mode", "A flag indicating if this block is on a shared page with a report viewer or if it is on it's own page.", false, "Configuration", 4, "StandaloneMode" )]
    public partial class ReportingServicesFolderTree : RockBlock
    {
        private bool showHiddenItems = false;
        private bool showChildItems = false;
        private bool standaloneMode = false;
        private string rootFolder = null;
        private string headerText = null;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upMain );
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            NavigateToPage( this.RockPage.Guid, new Dictionary<string, string>() );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            RegisterScript();
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
                    lTitle.Text = string.Empty;
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

        /// <summary>
        /// Builds the tree.
        /// </summary>
        private void BuildTree()
        {
            try
            {
                var expandedItems = new List<string>();
                var selectedItemPath = PageParameter( "ReportPath" );
                
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

                ReportingServiceItem reportItem = null;
                if ( hfSelectionType.Value.Equals( "Folder" ) )
                {
                    reportItem = ReportingServiceItem.GetFoldersTree( rootFolder, showChildItems, showHiddenItems );
                }
                else if ( hfSelectionType.Value.Equals( "Report" ) )
                {
                    reportItem = ReportingServiceItem.GetReportTree( rootFolder, showChildItems, showHiddenItems );
                }
                
                // also get any additional expanded nodes
                var expandedItemParams = PageParameter( "ExpandedItems" );
                if ( !string.IsNullOrWhiteSpace( expandedItemParams ) )
                {
                    foreach ( var id in expandedItemParams.Split( ',' ).ToList() )
                    {
                        if ( !expandedItems.Contains( id ) )
                        {
                            expandedItems.Add( id );
                        }
                    }
                }

                hfExpandedItems.Value = expandedItems.AsDelimited( "," );

                var treeBuilder = new StringBuilder();

                treeBuilder.AppendLine( "<ul id=\"treeview\">" );

                BuildTreeNode( reportItem, expandedItems.Select( s => s.Substring( 2 ) ).ToList(), ref treeBuilder );
                
                treeBuilder.AppendLine( "</ul>" );
                lFolders.Text = treeBuilder.ToString();
            }
            catch ( System.Net.WebException webEx )
            {
                if ( ( (System.Net.HttpWebResponse)webEx.Response ).StatusCode == System.Net.HttpStatusCode.Unauthorized )
                {
                    ShowError( "Authorization Error", "Browser User Could not authenticate to Reporting Server." );
                }
            }
            catch ( System.ServiceModel.Security.MessageSecurityException ex )
            {
                ShowError( "Authorization Error", "Browser User could not authenticate to Reporting Server." );
            }
            catch ( System.ServiceModel.EndpointNotFoundException ex )
            {
                ShowError( "Connection Error", "An error occurred when connecting to the reporting server." );
            }
        }

        /// <summary>
        /// Builds the tree node.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="expandedItems">The expanded items.</param>
        /// <param name="treeBuilder">The tree builder.</param>
        private void BuildTreeNode( ReportingServiceItem item, List<string> expandedItems, ref StringBuilder treeBuilder )
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
                    if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "ReportViewerPage" ) ) )
                    {
                        var qsParams = new Dictionary<string, string>();
                        qsParams.Add( "ReportPath", HttpUtility.UrlEncode( item.Path ) );
                        viewerUrl = LinkedPageUrl( "ReportViewerPage", qsParams );
                    }
                    break;

                default:
                    return;
            }

            var nodeId = string.Concat( nodeIdPrefix, item.Path );
            var listItemFormat = "<li data-expanded=\"{0}\" data-id=\"{1}\" data-type=\"{5}\"><span><span class=\"rollover-container\"><i class=\"fa {4}\"></i> {2} </span></span>{3}";
            var reportItemLink = string.IsNullOrWhiteSpace( viewerUrl ) ? item.Name : string.Format( "<a href=\"{0}\">{1}</a>", viewerUrl, item.Name );

            treeBuilder.AppendFormat( listItemFormat,
                expandedItems.Any( s => item.Path.Equals( s ) ).ToString().ToLower(),
                nodeId,
                reportItemLink,
                Environment.NewLine,
                iconClass,
                item.Type.ToString() );

            if ( item.Children != null && item.Children.Count( c => c.Type != ItemType.DataSource ) > 0 )
            {
                treeBuilder.AppendLine( "<ul>" );
                foreach ( ReportingServiceItem child in item.Children )
                {
                    BuildTreeNode( child, expandedItems, ref treeBuilder );
                }

                treeBuilder.AppendLine( "</ul>" );
            }

            treeBuilder.AppendLine( "</li>" );
        }

        /// <summary>
        /// Loads the attributes.
        /// </summary>
        private void LoadAttributes()
        {
            showHiddenItems = GetAttributeValue( "ShowHiddenItems" ).AsBoolean();
            showChildItems = GetAttributeValue( "ShowChildItems" ).AsBoolean();
            rootFolder = GetAttributeValue( "RootFolder" );
            hfSelectionType.Value = GetAttributeValue( "SelectionMode" );
            standaloneMode = GetAttributeValue( "StandaloneMode" ).AsBoolean();
            headerText = GetAttributeValue( "HeaderText" );
        }

        /// <summary>
        /// Shows the error.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        private void ShowError( string title, string message )
        {
            nbRockError.Title = title;
            nbRockError.Text = message;
            nbRockError.Visible = !string.IsNullOrWhiteSpace( message );
        }

        /// <summary>
        /// Registers the script.
        /// </summary>
        private void RegisterScript()
        {
            var script = string.Format( @"
Sys.Application.add_load(function () {{
    
    var $selectedItem = $('#{0}'), $expandedItems = $('#{2}');
    if ( $expandedItems.val() )
    {{
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function (e) {{
            var arr = $expandedItems.val().split(',');
            for ( var i=0, l=arr.length; i < l; i++ )
            {{
                // fixes report viewer postback destroying the expanded state
                $('li[data-id=""' + arr[i] + '""]' ).find( '.rocktree-children' ).first().css('display', 'block');  
            }}
        }});
    }}

    $('#folders').on( 'rockTree:selected', function( e, id ) {{
        if ( id != $selectedItem.val() )
        {{
            var expandedItems = $( e.currentTarget ).find( '.rocktree-children' ).filter( ':visible' ).closest( '.rocktree-item' ).map( function() {{
                return $( this ).attr( 'data-id' )
            }}).get().join( ',' );

            var selectionMode = $( '#{1}' ).val().toLowerCase();
            var validSelection = false;
            var selectedItemType = id.substring( 0, 1 );
            if ( selectionMode === 'folder' )
            {{
                if ( selectedItemType === 'f' )
                {{
                    validSelection = true;
                }}
            }}
            else if ( selectionMode === 'report' )
            {{
                if ( selectedItemType == 'r' )
                {{
                    validSelection = true;
                }}
            }}

            if ( validSelection == true )
            {{
                $( '#{0}' ).val( id );
                $( '#{2}' ).val( expandedItems );
                var action = $( '.selected' ).find( 'a' ).first().attr( 'href' );
                if ( action != null )
                {{
                    window.location = action + '&ExpandedItems=' + encodeURIComponent(expandedItems);
                }}
            }}
        }}
    }})

    .rockTree({{
        multiSelect: false,
        selectedIds: $selectedItem.val() ? $selectedItem.val().split(',') : null,
        expandedIds: $expandedItems.val() ? $expandedItems.val().split(',') : null
    }});

    $( '#folders' ).show();
}});    
", hfSelectedItem.ClientID, hfSelectionType.ClientID, hfExpandedItems.ClientID );
            ScriptManager.RegisterStartupScript( upMain, this.GetType(), "treeViewScript", script, true );
            
        }
    }
}
