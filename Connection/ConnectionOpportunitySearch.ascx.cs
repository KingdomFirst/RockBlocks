using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Connection
{
    #region Block Attributes

    [DisplayName( "Connection Opportunity Search" )]
    [Category( "KFS > Connection" )]
    [Description( "Allows users to search for an opportunity to join.  Attribute keys need to be for drop down list attriute types." )]

    [CodeEditorField( "Lava Template", "Lava template to use to display the list of opportunities.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"{% include '~~/Assets/Lava/OpportunitySearch.lava' %}", "", 0 )]
    [BooleanField( "Enable Campus Context", "If the page has a campus context it's value will be used as a filter", true, order: 1 )]
    [BooleanField( "Set Page Title", "Determines if the block should set the page title with the connection type name.", false, order: 2 )]
    [LinkedPage( "Detail Page", "The page used to view a connection opportunity.", order: 7 )]
    [IntegerField( "Connection Type Id", "The Id of the connection type whose opportunities are displayed.", true, 1, order: 8 )]
    [TextField( "Attribute One Key", "", false, "", order: 10 )]
    [TextField( "Attribute Two Key", "", false, "", order: 11 )]

    #endregion

    public partial class OpportunitySearch : Rock.Web.UI.RockBlock
    {
        #region Properties

        /// <summary>
        /// Gets or sets the available attributes.
        /// </summary>
        /// <value>
        /// The available attributes.
        /// </value>
        public AttributeCache AttributeOne { get; set; }

        public AttributeCache AttributeTwo { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AttributeOne = ViewState["AttributeOne"] as AttributeCache;
            AttributeTwo = ViewState["AttributeTwo"] as AttributeCache;

            SetFilters( false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                SetFilters( true );
                UpdateList();
            }
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["AttributeOne"] = AttributeOne;
            ViewState["AttributeTwo"] = AttributeTwo;

            return base.SaveViewState();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            UpdateList();
        }

        /// <summary>
        /// Handles the Click event of the btnSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSearch_Click( object sender, EventArgs e )
        {
            UpdateList();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Updates the list.
        /// </summary>
        private void UpdateList()
        {
            using ( var rockContext = new RockContext() )
            {
                var searchSelections = new Dictionary<string, string>();

                var connectionTypeId = GetAttributeValue( "ConnectionTypeId" ).AsInteger();
                var connectionType = new ConnectionTypeService( rockContext ).Get( connectionTypeId );
                var connectionOpportunityService = new ConnectionOpportunityService( rockContext );

                var qrySearch = connectionOpportunityService.Queryable().Where( a => a.ConnectionTypeId == connectionTypeId && a.IsActive == true ).ToList();

                if ( GetAttributeValue( "EnableCampusContext" ).AsBoolean() && !GetAttributeValue( "DisplayCampusFilter" ).AsBoolean() )
                {
                    var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
                    var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                    if ( contextCampus != null )
                    {
                        var campusId = contextCampus.Id;
                        qrySearch = qrySearch.Where( o => o.ConnectionOpportunityCampuses.Any( c => c.CampusId.Equals( campusId ) ) ).ToList();
                    }
                }

                if ( AttributeOne != null )
                {
                    var control = phAttributeOne.Controls[0] as DropDownList;
                    if ( control != null )
                    {
                        searchSelections.Add( "ddlAttributeOne", control.SelectedValue );

                        var attributeValueService = new AttributeValueService( rockContext );
                        var parameterExpression = attributeValueService.ParameterExpression;

                        var value = AttributeOne.FieldType.Field.GetEditValue( control, AttributeOne.QualifierValues );
                        if ( !string.IsNullOrWhiteSpace( value ) )
                        {
                            var attributeValues = attributeValueService
                                .Queryable()
                                .Where( v => v.Attribute.Id == AttributeOne.Id )
                                .Where( v => v.Value.Equals( value ) );
                            qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) ).ToList();
                        }
                    }
                    else
                    {
                        var newDdlOne = phAttributeOne.Controls[2] as DropDownList;

                        searchSelections.Add( "ddlAttributeOne", newDdlOne.SelectedValue );

                        var attributeValueService = new AttributeValueService( rockContext );
                        var parameterExpression = attributeValueService.ParameterExpression;

                        var value = newDdlOne.SelectedValue;
                        if ( !string.IsNullOrWhiteSpace( value ) )
                        {
                            var attributeValues = attributeValueService
                                .Queryable()
                                .Where( v => v.Attribute.Id == AttributeOne.Id )
                                .Where( v => v.Value.Contains( value ) );
                            qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) ).ToList();
                        }
                    }
                }

                if ( AttributeTwo != null )
                {
                    var control = phAttributeTwo.Controls[0] as DropDownList;
                    if ( control != null )
                    {
                        searchSelections.Add( "ddlAttributeTwo", control.SelectedValue );

                        var attributeValueService = new AttributeValueService( rockContext );
                        var parameterExpression = attributeValueService.ParameterExpression;

                        var value = AttributeTwo.FieldType.Field.GetEditValue( control, AttributeTwo.QualifierValues );
                        if ( !string.IsNullOrWhiteSpace( value ) )
                        {
                            var attributeValues = attributeValueService
                                .Queryable()
                                .Where( v => v.Attribute.Id == AttributeTwo.Id )
                                .Where( v => v.Value.Equals( value ) );
                            qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) ).ToList();
                        }
                    }
                    else
                    {
                        var newDdlTwo = phAttributeTwo.Controls[2] as DropDownList;

                        searchSelections.Add( "ddlAttributeTwo", newDdlTwo.SelectedValue );

                        var attributeValueService = new AttributeValueService( rockContext );
                        var parameterExpression = attributeValueService.ParameterExpression;

                        var value = newDdlTwo.SelectedValue;
                        if ( !string.IsNullOrWhiteSpace( value ) )
                        {
                            var attributeValues = attributeValueService
                                .Queryable()
                                .Where( v => v.Attribute.Id == AttributeTwo.Id )
                                .Where( v => v.Value.Contains( value ) );
                            qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) ).ToList();
                        }
                    }
                }

                string sessionKey = string.Format( "ConnectionSearch_{0}", this.BlockId );
                Session[sessionKey] = searchSelections;

                var opportunities = qrySearch.OrderBy( s => s.PublicName ).ToList();

                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "CurrentPerson", CurrentPerson );
                mergeFields.Add( "CampusContext", RockPage.GetCurrentContext( EntityTypeCache.Get( "Rock.Model.Campus" ) ) as Campus );
                var pageReference = new PageReference( GetAttributeValue( "DetailPage" ), null );
                mergeFields.Add( "DetailPage", BuildDetailPageUrl( pageReference.BuildUrl() ) );

                // iterate through the opportunities and lava merge the summaries and descriptions
                foreach ( var opportunity in opportunities )
                {
                    opportunity.Summary = opportunity.Summary.ResolveMergeFields( mergeFields );
                    opportunity.Description = opportunity.Description.ResolveMergeFields( mergeFields );
                }

                mergeFields.Add( "Opportunities", opportunities );

                lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );

                if ( GetAttributeValue( "SetPageTitle" ).AsBoolean() )
                {
                    string pageTitle = "Connection";
                    RockPage.PageTitle = pageTitle;
                    RockPage.BrowserTitle = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                    RockPage.Header.Title = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                }
            }
        }

        /// <summary>
        /// Builds the detail page URL. This is needed so that it can pass along any url paramters that are in the
        /// query string.
        /// </summary>
        /// <param name="detailPage">The detail page.</param>
        /// <returns></returns>
        private string BuildDetailPageUrl( string detailPage )
        {
            StringBuilder sbUrlParms = new StringBuilder();
            foreach ( var parm in this.RockPage.PageParameters() )
            {
                if ( parm.Key != "PageId" )
                {
                    if ( sbUrlParms.Length > 0 )
                    {
                        sbUrlParms.Append( string.Format( "&{0}={1}", parm.Key, parm.Value.ToString() ) );
                    }
                    else
                    {
                        sbUrlParms.Append( string.Format( "?{0}={1}", parm.Key, parm.Value.ToString() ) );
                    }
                }
            }

            return detailPage + sbUrlParms.ToString();
        }

        /// <summary>
        /// Sets the filters.
        /// </summary>
        private void SetFilters( bool setValues )
        {
            using ( var rockContext = new RockContext() )
            {
                string sessionKey = string.Format( "ConnectionSearch_{0}", this.BlockId );
                var searchSelections = Session[sessionKey] as Dictionary<string, string>;
                setValues = setValues && searchSelections != null;

                var connectionType = new ConnectionTypeService( rockContext ).Get( GetAttributeValue( "ConnectionTypeId" ).AsInteger() );

                if ( connectionType != null )
                {
                    int entityTypeId = new ConnectionOpportunity().TypeId;
                    var attributeOneKey = GetAttributeValue( "AttributeOneKey" );
                    var attributeTwoKey = GetAttributeValue( "AttributeTwoKey" );

                    if ( !string.IsNullOrWhiteSpace( attributeOneKey ) )
                    {
                        if ( !string.IsNullOrWhiteSpace( attributeTwoKey ) )
                        {
                            pnlAttributeOne.CssClass = "col-sm-6";
                        }
                        else
                        {
                            pnlAttributeOne.CssClass = "col-sm-12";
                        }

                        AttributeOne = AttributeCache.Get( new AttributeService( new RockContext() ).Queryable()
                            .FirstOrDefault( a =>
                                a.EntityTypeId == entityTypeId &&
                                a.EntityTypeQualifierColumn.Equals( "ConnectionTypeId", StringComparison.OrdinalIgnoreCase ) &&
                                a.EntityTypeQualifierValue.Equals( connectionType.Id.ToString() ) &&
                                a.Key == attributeOneKey ) );
                    }

                    if ( !string.IsNullOrWhiteSpace( attributeTwoKey ) )
                    {
                        if ( !string.IsNullOrWhiteSpace( attributeOneKey ) )
                        {
                            pnlAttributeTwo.CssClass = "col-sm-6";
                        }
                        else
                        {
                            pnlAttributeTwo.CssClass = "col-sm-12";
                        }

                        AttributeTwo = AttributeCache.Get( new AttributeService( new RockContext() ).Queryable()
                            .FirstOrDefault( a =>
                                a.EntityTypeId == entityTypeId &&
                                a.EntityTypeQualifierColumn.Equals( "ConnectionTypeId", StringComparison.OrdinalIgnoreCase ) &&
                                a.EntityTypeQualifierValue.Equals( connectionType.Id.ToString() ) &&
                                a.Key == attributeTwoKey ) );
                    }
                }

                if ( AttributeOne != null)
                {
                    phAttributeOne.Controls.Clear();
                    AttributeOne.AddControl( phAttributeOne.Controls, string.Empty, string.Empty, true, true, false );
                    var control = phAttributeOne.Controls[0] as DropDownList;
                    if ( control != null )
                    {
                        control.EnableViewState = true;
                        control.AutoPostBack = true;
                        control.SelectedIndexChanged += new EventHandler( ddlAttributeOne_SelectedIndexChanged );
                    }
                    else
                    {
                        var theseControls = phAttributeOne.Controls[0] as DropDownList;
                        DropDownList ddl = new DropDownList();
                        string[] theseValues = AttributeOne.QualifierValues.Values.ElementAt( 0 ).Value.Split( ',' );

                        foreach ( string nameValue in theseValues )
                        {
                            string[] nameAndValue = nameValue.Split( new char[] { '^' } );

                            if ( nameAndValue.Length == 2 )
                            {
                                ddl.Items.Add( new ListItem( nameAndValue[1].Trim(), nameAndValue[0].Trim() ) );
                            }
                        }

                        ddl.Items.Insert( 0, new ListItem( String.Empty, String.Empty ) );
                        ddl.SelectedIndex = 0;
                        ddl.CssClass = "form-control";
                        var attributeOneLabel = new HtmlGenericContainer( "label" );
                        attributeOneLabel.InnerHtml = AttributeOne.Name;
                        attributeOneLabel.CssClass = "control-label";

                        phAttributeOne.Controls.Clear();
                        phAttributeOne.Controls.Add( new Literal() { Text = "<div class='form-group rock-drop-down-list'>" } );
                        phAttributeOne.Controls.Add( attributeOneLabel );
                        phAttributeOne.Controls.Add( ddl );
                        phAttributeOne.Controls.Add( new Literal() { Text = "</div>" } );

                        var newDdl = phAttributeOne.Controls[2] as DropDownList;

                        newDdl.EnableViewState = true;
                        newDdl.AutoPostBack = true;
                        newDdl.SelectedIndexChanged += new EventHandler( ddlAttributeOne_SelectedIndexChanged );
                    }
                }

                if ( AttributeTwo != null )
                {
                    phAttributeTwo.Controls.Clear();
                    AttributeTwo.AddControl( phAttributeTwo.Controls, string.Empty, string.Empty, true, true, false );
                    var control = phAttributeTwo.Controls[0] as DropDownList;
                    if ( control != null )
                    {
                        control.EnableViewState = true;
                        control.AutoPostBack = true;
                        control.SelectedIndexChanged += new EventHandler( ddlAttributeTwo_SelectedIndexChanged );
                    }
                    else
                    {
                        var theseControls = phAttributeTwo.Controls[0] as DropDownList;
                        DropDownList ddl = new DropDownList();
                        string[] theseValues = AttributeTwo.QualifierValues.Values.ElementAt( 0 ).Value.Split( ',' );

                        foreach ( string nameValue in theseValues )
                        {
                            string[] nameAndValue = nameValue.Split( new char[] { '^' } );

                            if ( nameAndValue.Length == 2 )
                            {
                                ddl.Items.Add( new ListItem( nameAndValue[1].Trim(), nameAndValue[0].Trim() ) );
                            }
                        }

                        ddl.Items.Insert( 0, new ListItem( String.Empty, String.Empty ) );
                        ddl.SelectedIndex = 0;
                        ddl.CssClass = "form-control";
                        var attributeTwoLabel = new HtmlGenericContainer("label");
                        attributeTwoLabel.InnerHtml = AttributeTwo.Name;
                        attributeTwoLabel.CssClass = "control-label";

                        phAttributeTwo.Controls.Clear();
                        phAttributeTwo.Controls.Add( new Literal() { Text="<div class='form-group rock-drop-down-list'>" } );
                        phAttributeTwo.Controls.Add( attributeTwoLabel );
                        phAttributeTwo.Controls.Add( ddl );
                        phAttributeTwo.Controls.Add( new Literal() { Text = "</div>" } );

                        var newDdl = phAttributeTwo.Controls[2] as DropDownList;

                        newDdl.EnableViewState = true;
                        newDdl.AutoPostBack = true;
                        newDdl.SelectedIndexChanged += new EventHandler( ddlAttributeTwo_SelectedIndexChanged );
                    }
                }
            }
        }

        #endregion

        protected void ddlAttributeOne_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( phAttributeTwo.Controls.Count > 0 )
            {
                var control = phAttributeTwo.Controls[0] as DropDownList;
                if ( control != null )
                {
                    control.SelectedIndex = 0;
                }
                else
                {
                    control = phAttributeTwo.Controls[2] as DropDownList;
                    control.SelectedIndex = 0;
                }
            }
            UpdateList();
        }

        protected void ddlAttributeTwo_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( phAttributeOne.Controls.Count > 0 )
            {
                var control = phAttributeOne.Controls[0] as DropDownList;
                if ( control != null )
                {
                    control.SelectedIndex = 0;
                }
                else
                {
                    control = phAttributeOne.Controls[2] as DropDownList;
                    control.SelectedIndex = 0;
                }
            }
            UpdateList();
        }
    }
}
