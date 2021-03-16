// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
// <notice>
// This file contains modifications by Kingdom First Solutions
// and is a derivative work.
//
// Modification (including but not limited to):
// * Displays search filters as drop downs even if multiple mode enabled on attribute
// * Added capability to search summary and details with the name filter and customize the label for the textbox.
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
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
using Rock.Store;
using Rock.Security;

namespace RockWeb.Plugins.rocks_kfs.Connection
{
    [DisplayName( "Connection Opportunity Search" )]
    [Category( "KFS > Connection" )]
    [Description( "Allows users to search for an opportunity to join. " )]

    #region Block Attributes

    [CodeEditorField(
        "Lava Template",
        Description = "Lava template to use to display the list of opportunities.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 400,
        IsRequired = true,
        DefaultValue = @"{% include '~~/Assets/Lava/OpportunitySearch.lava' %}",
        Order = 0,
        Key = AttributeKey.LavaTemplate )]
    [BooleanField(
        "Enable Campus Context",
        Description = "If the page has a campus context its value will be used as a filter",
        DefaultBooleanValue = true,
        Order = 1,
        Key = AttributeKey.EnableCampusContext )]
    [BooleanField(
        "Set Page Title",
        Description = "Determines if the block should set the page title with the connection type name.",
        DefaultBooleanValue = false,
        Order = 2,
        Key = AttributeKey.SetPageTitle )]
    [BooleanField(
        "Display Name Filter",
        Description = "Display the name filter",
        DefaultBooleanValue = false,
        Order = 3,
        Key = AttributeKey.DisplayNameFilter )]
    [BooleanField(
        "Display Campus Filter",
        Description = "Display the campus filter",
        DefaultBooleanValue = true,
        Order = 4,
        Key = AttributeKey.DisplayCampusFilter )]
    [BooleanField(
        "Display Inactive Campuses",
        Description = "Include inactive campuses in the Campus Filter",
        DefaultBooleanValue = true,
        Order = 5,
        Key = AttributeKey.DisplayInactiveCampuses )]
    [BooleanField(
        "Display Attribute Filters",
        Description = "Display the attribute filters",
        DefaultBooleanValue = true,
        Order = 6,
        Key = AttributeKey.DisplayAttributeFilters )]
    [LinkedPage(
        "Detail Page",
        Description = "The page used to view a connection opportunity.",
        Order = 7,
        Key = AttributeKey.DetailPage )]
    [IntegerField(
        "Connection Type Id",
        Description = "The Id of the connection type whose opportunities are displayed.",
        IsRequired = true,
        DefaultIntegerValue = 1,
        Order = 8,
        Key = AttributeKey.ConnectionTypeId )]
    [BooleanField(
        "Show Search",
        Description = "Determines if the search fields should be displayed. Sometimes listing all the options is enough.",
        DefaultBooleanValue = true,
        Order = 9,
        Key = AttributeKey.ShowSearch )]
    [TextField(
        "Campus Label",
        IsRequired = true,
        DefaultValue = "Campuses",
        Order = 10,
        Key = AttributeKey.CampusLabel )]
    [TextField( "Dropdown Attribute One Key",
        Description = "Specify an attribute to use for as a Dropdown List after the search panel. You should use this in combination with 'Show Search = false'. ",
        IsRequired = false,
        DefaultValue = "",
        Order = 11,
        Key = AttributeKey.AttributeOneKey )]
    [TextField( "Dropdown Attribute Two Key",
        Description = "Specify an additional attribute to use for as a Dropdown List after the search panel. You should use this in combination with 'Show Search = false'. ",
        IsRequired = false,
        DefaultValue = "",
        Order = 12,
        Key = AttributeKey.AttributeTwoKey )]
    [TextField(
        "Name Filter Label",
        IsRequired = true,
        DefaultValue = "Name",
        Order = 13,
        Key = AttributeKey.NameLabel )]
    [BooleanField(
        "Search Summary and Details with Name Filter",
        Description = "Determines if the 'Name Filter' also searches the summary and details of the opportunities.",
        DefaultBooleanValue = false,
        Order = 14,
        Key = AttributeKey.SearchDescription )]

    #endregion Block Attributes
    public partial class OpportunitySearch : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        private static class AttributeKey
        {
            public const string LavaTemplate = "LavaTemplate";
            public const string EnableCampusContext = "EnableCampusContext";
            public const string SetPageTitle = "SetPageTitle";
            public const string DisplayNameFilter = "DisplayNameFilter";
            public const string DisplayCampusFilter = "DisplayCampusFilter";
            public const string DisplayInactiveCampuses = "DisplayInactiveCampuses";
            public const string DisplayAttributeFilters = "DisplayAttributeFilters";
            public const string DetailPage = "DetailPage";
            public const string ConnectionTypeId = "ConnectionTypeId";
            public const string ShowSearch = "ShowSearch";
            public const string CampusLabel = "CampusLabel";
            public const string AttributeOneKey = "AttributeOneKey";
            public const string AttributeTwoKey = "AttributeTwoKey";
            public const string NameLabel = "NameLabel";
            public const string SearchDescription = "SearchDescription";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the available attributes.
        /// </summary>
        /// <value>
        /// The available attributes.
        /// </value>
        public AttributeCache AttributeOne { get; set; }

        public AttributeCache AttributeTwo { get; set; }

        public List<AttributeCache> AvailableAttributes { get; set; }

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
            AvailableAttributes = ViewState["AvailableAttributes"] as List<AttributeCache>;

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

            pnlSearch.Visible = GetAttributeValue( AttributeKey.ShowSearch ).AsBoolean();
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
            ViewState["AvailableAttributes"] = AvailableAttributes;

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

                var connectionTypeId = GetAttributeValue( AttributeKey.ConnectionTypeId ).AsInteger();
                var connectionType = new ConnectionTypeService( rockContext ).Get( connectionTypeId );
                var connectionOpportunityService = new ConnectionOpportunityService( rockContext );

                var qrySearch = connectionOpportunityService.Queryable().Where( a => a.ConnectionTypeId == connectionTypeId && a.IsActive && a.ConnectionType.IsActive );

                if ( GetAttributeValue( AttributeKey.DisplayNameFilter ).AsBoolean() )
                {
                    tbSearchName.Label = GetAttributeValue( AttributeKey.NameLabel );
                    if ( !string.IsNullOrWhiteSpace( tbSearchName.Text ) )
                    {
                        searchSelections.Add( "tbSearchName", tbSearchName.Text );
                        var searchTerms = tbSearchName.Text.ToLower().SplitDelimitedValues( true );
                        if ( GetAttributeValue( AttributeKey.SearchDescription ).AsBoolean() ) {
                            qrySearch = qrySearch.Where( o => searchTerms.Any( t => t.Contains( o.Name.ToLower() ) || o.Name.ToLower().Contains( t ) || t.Contains( o.Summary.ToLower() ) || o.Summary.ToLower().Contains( t ) || t.Contains( o.Description.ToLower() ) || o.Description.ToLower().Contains( t ) ) );
                        }
                        else
                        {
                            qrySearch = qrySearch.Where( o => searchTerms.Any( t => t.Contains( o.Name.ToLower() ) || o.Name.ToLower().Contains( t ) ) );
                        }
                    }
                }

                if ( GetAttributeValue( AttributeKey.DisplayCampusFilter ).AsBoolean() )
                {
                    cblCampus.Label = GetAttributeValue( AttributeKey.CampusLabel );
                    var searchCampuses = cblCampus.SelectedValuesAsInt;
                    if ( searchCampuses.Count > 0 )
                    {
                        searchSelections.Add( "cblCampus", searchCampuses.AsDelimited( "|" ) );
                        qrySearch = qrySearch.Where( o => o.ConnectionOpportunityCampuses.Any( c => searchCampuses.Contains( c.CampusId ) ) );
                    }
                }

                if ( GetAttributeValue( AttributeKey.DisplayAttributeFilters ).AsBoolean() )
                {
                    // Filter query by any configured attribute filters
                    if ( AvailableAttributes != null && AvailableAttributes.Any() )
                    {
                        foreach ( var attribute in AvailableAttributes )
                        {
                            string filterControlId = "filter_" + attribute.Id.ToString();
                            var filterControl = phAttributeFilters.FindControl( filterControlId );
                            qrySearch = attribute.FieldType.Field.ApplyAttributeQueryFilter( qrySearch, filterControl, attribute, connectionOpportunityService, Rock.Reporting.FilterMode.SimpleFilter );
                        }
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
                            qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) );
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
                            qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) );
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
                            qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) );
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
                            qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) );
                        }
                    }
                }

                string sessionKey = string.Format( "ConnectionSearch_{0}", this.BlockId );
                Session[sessionKey] = searchSelections;

                var opportunities = qrySearch.OrderBy( s => s.PublicName ).ToList();

                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "CurrentPerson", CurrentPerson );
                mergeFields.Add( "CampusContext", RockPage.GetCurrentContext( EntityTypeCache.Get( "Rock.Model.Campus" ) ) as Campus );
                var pageReference = new PageReference( GetAttributeValue( AttributeKey.DetailPage ), null );
                mergeFields.Add( "DetailPage", BuildDetailPageUrl( pageReference.BuildUrl() ) );

                // iterate through the opportunities and lava merge the summaries and descriptions
                foreach ( var opportunity in opportunities )
                {
                    opportunity.Summary = opportunity.Summary.ResolveMergeFields( mergeFields );
                    opportunity.Description = opportunity.Description.ResolveMergeFields( mergeFields );
                }

                mergeFields.Add( "Opportunities", opportunities );

                lOutput.Text = GetAttributeValue( AttributeKey.LavaTemplate ).ResolveMergeFields( mergeFields );

                if ( GetAttributeValue( AttributeKey.SetPageTitle ).AsBoolean() )
                {
                    string pageTitle = "Connection";
                    RockPage.PageTitle = pageTitle;
                    RockPage.BrowserTitle = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                    RockPage.Header.Title = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                }
            }
        }

        /// <summary>
        /// Builds the detail page URL. This is needed so that it can pass along any url parameters that are in the
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

                var connectionType = new ConnectionTypeService( rockContext ).Get( GetAttributeValue( AttributeKey.ConnectionTypeId ).AsInteger() );

                if ( !GetAttributeValue( AttributeKey.DisplayNameFilter ).AsBoolean() )
                {
                    tbSearchName.Visible = false;
                }

                if ( GetAttributeValue( AttributeKey.DisplayCampusFilter ).AsBoolean() )
                {
                    cblCampus.Visible = true;
                    cblCampus.DataSource = CampusCache.All( GetAttributeValue( AttributeKey.DisplayInactiveCampuses ).AsBoolean() );
                    cblCampus.DataBind();
                }
                else
                {
                    cblCampus.Visible = false;
                }

                if ( setValues )
                {
                    if ( searchSelections.ContainsKey( "tbSearchName" ) )
                    {
                        tbSearchName.Text = searchSelections["tbSearchName"];
                    }
                    if ( searchSelections.ContainsKey( "cblCampus" ) )
                    {
                        var selectedItems = searchSelections["cblCampus"].SplitDelimitedValues().AsIntegerList();
                        cblCampus.SetValues( selectedItems );
                    }
                }
                else if ( GetAttributeValue( AttributeKey.EnableCampusContext ).AsBoolean() )
                {
                    var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
                    var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                    if ( contextCampus != null )
                    {
                        cblCampus.SetValue( contextCampus.Id.ToString() );
                    }
                }

                if ( GetAttributeValue( AttributeKey.DisplayAttributeFilters ).AsBoolean() )
                {
                    // Parse the attribute filters 
                    AvailableAttributes = new List<AttributeCache>();
                    if ( connectionType != null )
                    {
                        int entityTypeId = new ConnectionOpportunity().TypeId;
                        foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                            .Where( a =>
                                a.EntityTypeId == entityTypeId &&
                                a.EntityTypeQualifierColumn.Equals( "ConnectionTypeId", StringComparison.OrdinalIgnoreCase ) &&
                                a.EntityTypeQualifierValue.Equals( connectionType.Id.ToString() ) &&
                                a.AllowSearch == true )
                            .OrderBy( a => a.Order )
                            .ThenBy( a => a.Name ) )
                        {
                            AvailableAttributes.Add( AttributeCache.Get( attributeModel ) );
                        }
                    }

                    // Clear the filter controls
                    phAttributeFilters.Controls.Clear();

                    if ( AvailableAttributes != null )
                    {
                        foreach ( var attribute in AvailableAttributes )
                        {
                            string controlId = "filter_" + attribute.Id.ToString();
                            var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, controlId, false, Rock.Reporting.FilterMode.SimpleFilter );
                            if ( control != null )
                            {
                                if ( control is IRockControl )
                                {
                                    var rockControl = ( IRockControl ) control;
                                    rockControl.Label = attribute.Name;
                                    rockControl.Help = attribute.Description;
                                    phAttributeFilters.Controls.Add( control );
                                }
                                else
                                {
                                    var wrapper = new RockControlWrapper();
                                    wrapper.ID = control.ID + "_wrapper";
                                    wrapper.Label = attribute.Name;
                                    wrapper.Controls.Add( control );
                                    phAttributeFilters.Controls.Add( wrapper );
                                }

                                if ( setValues && searchSelections.ContainsKey( controlId ) )
                                {
                                    var values = searchSelections[controlId].FromJsonOrNull<List<string>>();
                                    attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, values );
                                }
                            }
                        }
                    }
                }
                else
                {
                    phAttributeFilters.Visible = false;
                }

                if ( connectionType != null )
                {
                    int entityTypeId = new ConnectionOpportunity().TypeId;
                    var attributeOneKey = GetAttributeValue( AttributeKey.AttributeOneKey );
                    var attributeTwoKey = GetAttributeValue( AttributeKey.AttributeTwoKey );

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

                if ( AttributeOne != null )
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
                        if ( theseValues.Count() == 1 && theseValues[0] == "rb" )
                        {
                            theseValues = AttributeOne.QualifierValues.Values.ElementAt( 1 ).Value.Split( ',' );
                        }
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
                        if ( theseValues.Count() == 1 && theseValues[0] == "rb" )
                        {
                            theseValues = AttributeTwo.QualifierValues.Values.ElementAt( 1 ).Value.Split( ',' );
                        }

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
                        var attributeTwoLabel = new HtmlGenericContainer( "label" );
                        attributeTwoLabel.InnerHtml = AttributeTwo.Name;
                        attributeTwoLabel.CssClass = "control-label";

                        phAttributeTwo.Controls.Clear();
                        phAttributeTwo.Controls.Add( new Literal() { Text = "<div class='form-group rock-drop-down-list'>" } );
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
