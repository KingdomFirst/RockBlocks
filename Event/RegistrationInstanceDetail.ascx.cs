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
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Event
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "KFS Registration Instance Detail" )]
    [Category( "KFS > Event" )]
    [Description( "Template block for editing an event registration instance." )]
    [AccountField( "Default Account", "The default account to use for new registration instances", false, "2A6F9E5F-6859-44F1-AB0E-CE9CF6B08EE5", "", 0 )]
    [LinkedPage( "Registration Page", "The page for editing registration and registrant information", true, "", "", 1 )]
    [LinkedPage( "Linkage Page", "The page for editing registration linkages", true, "", "", 2 )]
    [LinkedPage( "Calendar Item Page", "The page to view calendar item details", true, "", "", 3 )]
    [LinkedPage( "Group Detail Page", "The page for viewing details about a group", true, "", "", 4 )]
    [LinkedPage( "Content Item Page", "The page for viewing details about a content channel item", true, "", "", 5 )]
    [LinkedPage( "Transaction Detail Page", "The page for viewing details about a payment", true, "", "", 6 )]
    [LinkedPage( "Payment Reminder Page", "The page for manually sending payment reminders.", false, "", "", 7 )]
    [BooleanField( "Display Discount Codes", "Display the discount code used with a payment", false, "", 8 )]

    [GroupTypeField( "Associated Group Type - Parent", "Select a Group Type to trigger the creation of a new group of selected type upon creating a new Registration Instance. The new Instances's 'AssociatedGroupParent' attribute will be set to the new Group.", false, "", "", 0, "GroupTypeParentSetting", "" )]
    [GroupTypesField( "Associated Group Types - SubGroups", "Select Group Types to dynamically link to Registration Instances", false, key: "GroupTypesSetting" )]
    public partial class KFSRegistrationInstanceDetail : Rock.Web.UI.RockBlock, IDetailBlock
    {
        #region Fields

        private List<FinancialTransactionDetail> RegistrationPayments;
        private List<Registration> PaymentRegistrations;
        private List<string> _expandedGroupPanels = new List<string>();
        private bool _instanceHasCost = false;
        private Dictionary<int, Location>  _homeAddresses = new Dictionary<int, Location>();
        private List<GroupType> _associatedGroupsUsed = new List<GroupType>();
        private List<GroupType> _associatedGroupsAvailable = new List<GroupType>();
        string _templateAttributeKey = "AssociatedGroup";
        string _attributeKeyParent = "AssociatedGroupParent";
        string _attributeNameParent = "Parent Group";
        int _registrantGridColumnCount = 0;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the registrant fields.
        /// </summary>
        /// <value>
        /// The registrant fields.
        /// </value>
        public List<RegistrantFormField> RegistrantFields { get; set; }

        /// <summary>
        /// Gets or sets the Associated GroupTypes.
        /// </summary>
        /// <value>
        /// The associated GroupTypes.
        /// </value>
        public List<GroupType> AssociatedGroupTypes { get; set; }

        /// <summary>
        /// Gets or sets the Grid Associated GroupTypes.
        /// </summary>
        /// <value>
        /// The grid associated GroupTypes.
        /// </value>
        public List<GroupType> AssociatedGroupTypesGrid { get; set; }

        /// <summary>
        /// Gets or sets the person campus ids.
        /// </summary>
        /// <value>
        /// The person campus ids.
        /// </value>
        private Dictionary<int, List<int>> PersonCampusIds { get; set; }

        /// <summary>
        /// Gets or sets the signed person ids.
        /// </summary>
        /// <value>
        /// The signed person ids.
        /// </value>
        private List<int> Signers { get; set; }

        /// <summary>
        /// Gets or sets the group links.
        /// </summary>
        /// <value>
        /// The group links.
        /// </value>
        private Dictionary<int, string> GroupLinks { get; set; }

        /// <summary>
        /// Gets or sets the active tab.
        /// </summary>
        /// <value>
        /// The active tab.
        /// </value>
        protected string ActiveTab { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            ActiveTab = ( ViewState["ActiveTab"] as string ) ?? "";
            RegistrantFields = ViewState["RegistrantFields"] as List<RegistrantFormField>;
            var gs = new GroupTypeService( new RockContext() );
            AssociatedGroupTypes = new List<GroupType>();
            AssociatedGroupTypesGrid = new List<GroupType>();
            foreach ( int id in ViewState["AssociatedGroupTypes"] as List<int> )
            {
                AssociatedGroupTypes.Add( gs.Get( id ) );
            }
            foreach ( int id in ViewState["AssociatedGroupTypesGrid"] as List<int> )
            {
                AssociatedGroupTypesGrid.Add( gs.Get( id ) );
            }

            AddDynamicControls();
            BindRegistrantsGrid();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( Page.IsPostBack )
            {
                var httpForm = Request.Form;
                foreach ( string key in httpForm.AllKeys )
                {
                    if ( httpForm[key] == "True" && key.Contains( "hfExpanded") )
                    {
                        _expandedGroupPanels.Add( key );
                    }

                }
            }


            fRegistrations.ApplyFilterClick += fRegistrations_ApplyFilterClick;
            gRegistrations.DataKeyNames = new string[] { "Id" };
            gRegistrations.Actions.ShowAdd = true;
            gRegistrations.Actions.AddClick += gRegistrations_AddClick;
            gRegistrations.RowDataBound += gRegistrations_RowDataBound;
            gRegistrations.GridRebind += gRegistrations_GridRebind;
            gRegistrations.ShowConfirmDeleteDialog = false;

            ddlInGroup.Items.Clear();
            ddlInGroup.Items.Add( new ListItem());
            ddlInGroup.Items.Add( new ListItem( "Yes", "Yes" ) );
            ddlInGroup.Items.Add( new ListItem( "No", "No" ) );

            ddlSignedDocument.Items.Clear();
            ddlSignedDocument.Items.Add( new ListItem() );
            ddlSignedDocument.Items.Add( new ListItem( "Yes", "Yes" ) );
            ddlSignedDocument.Items.Add( new ListItem( "No", "No" ) );

            fRegistrants.ApplyFilterClick += fRegistrants_ApplyFilterClick;
            gRegistrants.DataKeyNames = new string[] { "Id" };
            gRegistrants.Actions.ShowAdd = true;
            gRegistrants.Actions.AddClick += gRegistrants_AddClick;
            gRegistrants.RowDataBound += gRegistrants_RowDataBound;
            gRegistrants.GridRebind += gRegistrants_GridRebind;
            gRegistrants.RowCommand += gRegistrants_RowCommand;

            fPayments.ApplyFilterClick += fPayments_ApplyFilterClick;
            gPayments.DataKeyNames = new string[] { "Id" };
            gPayments.Actions.ShowAdd = false;
            gPayments.RowDataBound += gPayments_RowDataBound;
            gPayments.GridRebind += gPayments_GridRebind;

            fLinkages.ApplyFilterClick += fLinkages_ApplyFilterClick;
            gLinkages.DataKeyNames = new string[] { "Id" };
            gLinkages.Actions.ShowAdd = true;
            gLinkages.Actions.AddClick += gLinkages_AddClick;
            gLinkages.RowDataBound += gLinkages_RowDataBound;
            gLinkages.GridRebind += gLinkages_GridRebind;

            gGroupPlacements.DataKeyNames = new string[] { "Id" };
            gGroupPlacements.Actions.ShowAdd = false;
            gGroupPlacements.RowDataBound += gRegistrants_RowDataBound; //intentionally using same row data bound event as the gRegistrants grid
            gGroupPlacements.GridRebind += gGroupPlacements_GridRebind;

            rpGroupPanels.ItemDataBound += rpGroupPanels_ItemDataBound;
            rpGroupPanels.ItemCommand += RpGroupPanels_ItemCommand;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            var deleteScript = @"
    $('a.js-delete-instance').click(function( e ){
        e.preventDefault();
        Rock.dialogs.confirm('Are you sure you want to delete this registration instance? All of the registrations and registrants will also be deleted!', function (result) {
            if (result) {
                if ( $('input.js-instance-has-payments').val() == 'True' ) {
                    Rock.dialogs.confirm('This registration instance also has registrations with payments. Are you really sure that you want to delete the instance?<br/><small>(Payments will not be deleted, but they will no longer be associated with a registration.)</small>', function (result) {
                        if (result) {
                            window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                        }
                    });
                } else {
                    window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                }
            }
        });
    });
    $('a.js-delete-subGroup').click(function( e ){
        e.preventDefault();
        Rock.dialogs.confirm('Are you sure you want to delete this Group?', function (result) {
            if (result) {
                    window.location = e.target.href ? e.target.href : e.target.parentElement.href;
            }
        });
    });

    $('table.js-grid-registration a.grid-delete-button').click(function( e ){
        e.preventDefault();
        var $hfHasPayments = $(this).closest('tr').find('input.js-has-payments').first();
        Rock.dialogs.confirm('Are you sure you want to delete this registration? All of the registrants will also be deleted!', function (result) {
            if (result) {
                if ( $hfHasPayments.val() == 'True' ) {
                    Rock.dialogs.confirm('This registration also has payments. Are you really sure that you want to delete the registration?<br/><small>(Payments will not be deleted, but they will no longer be associated with a registration.)</small>', function (result) {
                        if (result) {
                            window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                        }
                    });
                } else {
                    window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                }
            }
        });
    });

    $('table.js-grid-group-members a.grid-delete-button').on( 'click', function( e ){
        var $btn = $(this);
        e.preventDefault();
        var name = e.currentTarget.parentElement.parentElement.children[1].innerHTML;
        var pnlSection = $(e.currentTarget).closest('section');
        var groupName = $( pnlSection ).find('.span-panel-heading').text();
        Rock.dialogs.confirm('Are you sure you want to remove ' + name + ' from ' + groupName + '?', function (result) {
            if (result) {
                if ( $btn.closest('tr').hasClass('js-has-registration') ) {
                    Rock.dialogs.confirm('This group member was added through a registration. Are you really sure that you want to delete this group member and remove the link from the registration? ', function (result) {
                        if (result) {
                            window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                        }
                    });
                } else {
                    window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                }
            }
        });
    });

    //$('.rock-panel-widget > header').click(function (e) {
    //    $expanded = $(this).children('input.filter-expanded');
    //    if (window.sessionStorage) {
    //        sessionStorage.setItem('dropselvalue', dropselvalue);
    //    }
    //    $hfexpandedgroups = $('.hf-expanded-groups');
    //    var groupid = $(this).parent().siblings('.panel-widget-groupid').val();
    //    if( $expanded.val() == 'True') {
    //        $hfexpandedgroups.val() = $hfexpandedgroups.val().split(',').filter(function(elem) {
    //            return elem != groupid;
    //        });
    //    }
    //    if( $expanded.val() == 'False' ) {
    //        var groupsarray = $hfexpandedgroups.val().split(',').push( groupid );
    //        $hfexpandedgroups.val( groupsarray.join(',') );
    //    }
    //    alert( $hfexpandedgroups.val() );
    //    //if ( $(this).find('.js-header-controls').length ) {
    //    //    $(this).find('.js-header-title').slideToggle();
    //    //    $(this).find('.js-header-controls').slideToggle();
    //    //}

    //    //$expanded = $(this).children('input.filter-expanded');
    //    //$expanded.val($expanded.val() == 'True' ? 'False' : 'True');
    //});
";
            ScriptManager.RegisterStartupScript( btnDelete, btnDelete.GetType(), "deleteInstanceScript", deleteScript, true );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            // Set up associated group types

            var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            var ri = new RegistrationInstanceService( new RockContext() ).Get( RegistrationInstanceId );
            if ( ri == null )
            {
                ri = new RegistrationInstance();
            }
            LoadAssociatedGroupTypes( ri );
            pnlSubGroups.Visible = _associatedGroupsAvailable.Count > 0;
            SetupGroupPanel( ri, false );

            BuildSubGroupTabs();
            rpGroupPanels.DataSource = AssociatedGroupTypes;
            rpGroupPanels.DataBind();
            if ( !Page.IsPostBack )
            {
                var tab = PageParameter( "Tab" ).AsIntegerOrNull();
                if ( tab.HasValue )
                {
                    switch ( tab.Value )
                    {
                        case 1:
                            ActiveTab = "lbRegistrations";
                            break;

                        case 2:
                            ActiveTab = "lbRegistrants";
                            break;

                        case 3:
                            ActiveTab = "lbPayments";
                            break;

                        case 4:
                            ActiveTab = "lbLinkage";
                            break;

                        case 5:
                            ActiveTab = "lbGroupPlacement";
                            break;
                    }
                }

                ShowDetail();
            }
            else
            {
                SetFollowingOnPostback();
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
            ViewState["RegistrantFields"] = RegistrantFields;
            ViewState["AssociatedGroupTypes"] = AssociatedGroupTypes.Select( t => t.Id ).ToList();
            ViewState["AssociatedGroupTypesGrid"] = AssociatedGroupTypesGrid.Select( t => t.Id ).ToList();
            ViewState["ActiveTab"] = ActiveTab;

            return base.SaveViewState();
        }

        /// <summary>
        /// Gets the bread crumbs.
        /// </summary>
        /// <param name="pageReference">The page reference.</param>
        /// <returns></returns>
        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            var registrationInstanceId = PageParameter( pageReference, "RegistrationInstanceId" ).AsIntegerOrNull();
            if ( registrationInstanceId.HasValue )
            {
                var registrationInstance = GetRegistrationInstance( registrationInstanceId.Value );
                if ( registrationInstance != null )
                {
                    breadCrumbs.Add( new BreadCrumb( registrationInstance.ToString(), pageReference ) );
                    return breadCrumbs;
                }
            }

            breadCrumbs.Add( new BreadCrumb( "New Registration Instance", pageReference ) );
            return breadCrumbs;
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
        }

        #endregion

        #region Events

        #region Main Form Events

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var registrationInstance = new RegistrationInstanceService( rockContext ).Get( hfRegistrationInstanceId.Value.AsInteger() );

                ShowEditDetails( registrationInstance, rockContext );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnDelete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDelete_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var service = new RegistrationInstanceService( rockContext );
                var registrationInstance = service.Get( hfRegistrationInstanceId.Value.AsInteger() );

                if ( registrationInstance != null )
                {
                    var registrationTemplateId = registrationInstance.RegistrationTemplateId;

                    if ( UserCanEdit ||
                         registrationInstance.IsAuthorized( Authorization.EDIT, CurrentPerson ) ||
                         registrationInstance.IsAuthorized( Authorization.ADMINISTRATE, this.CurrentPerson ) )
                    {
                        rockContext.WrapTransaction( () =>
                        {
                            new RegistrationService( rockContext ).DeleteRange( registrationInstance.Registrations );
                            service.Delete( registrationInstance );
                            rockContext.SaveChanges();
                        } );

                        var qryParams = new Dictionary<string, string> { { "RegistrationTemplateId", registrationTemplateId.ToString() } };
                        NavigateToParentPage( qryParams );
                    }
                    else
                    {
                        mdDeleteWarning.Show( "You are not authorized to delete this registration instance.", ModalAlertType.Information );
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnPreview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPreview_Click( object sender, EventArgs e )
        {
        }

        /// <summary>
        /// Handles the Click event of the btnSendPaymentReminder control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendPaymentReminder_Click( object sender, EventArgs e )
        {
            var queryParms = new Dictionary<string, string>();
            queryParms.Add( "RegistrationInstanceId", PageParameter( "RegistrationInstanceId" ) );
            NavigateToLinkedPage( "PaymentReminderPage", queryParms );
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            RegistrationInstance instance = null;

            var newInstance = false;

            using ( var rockContext = new RockContext() )
            {
                var service = new RegistrationInstanceService( rockContext );

                var RegistrationInstanceId = hfRegistrationInstanceId.Value.AsIntegerOrNull();
                if ( RegistrationInstanceId.HasValue )
                {
                    instance = service.Get( RegistrationInstanceId.Value );
                }

                if ( instance == null )
                {
                    instance = new RegistrationInstance
                    {
                        RegistrationTemplateId = PageParameter( "RegistrationTemplateId" ).AsInteger()
                    };
                    service.Add( instance );
                    newInstance = true;
                }

                rieDetails.GetValue( instance );

                if ( !Page.IsValid )
                {
                    return;
                }

                rockContext.SaveChanges();

                instance = new RegistrationInstanceService( new RockContext() ).Get( instance.Id );

                Guid parentGroupTypeGuid;
                if ( Guid.TryParse( this.GetAttributeValue( "GroupTypeParentSetting" ), out parentGroupTypeGuid ) )
                {
                    Group templateGroup = null;
                    var parentGroupId = 0;
                    instance.LoadAttributes();
                    var groupService = new GroupService( rockContext );
                    if ( instance.GetAttributeValue( _attributeKeyParent ) == null )
                    {
                        var template = instance.RegistrationTemplate;
                        if ( template != null )
                        {
                            template.LoadAttributes();
                            if ( template.GetAttributeValue( _templateAttributeKey ) != null )
                            {
                                templateGroup = groupService.Get( Guid.Parse( template.GetAttributeValue( _templateAttributeKey ) ) );
                            }
                        }
                        if ( templateGroup != null )
                        {
                            parentGroupId = CreateGroup( instance, rockContext, parentGroupTypeGuid, templateGroup.Id, _attributeKeyParent, instance.Name, groupService, false);
                        }
                    }
                    else
                    {
                        parentGroupId = groupService.Get( Guid.Parse( instance.GetAttributeValue( _attributeKeyParent ) ) ).Id;
                    }
                    if ( parentGroupId > 0 )
                    {
                        var associatedGroupTypeId = 0;
                        instance.LoadAttributes();
                        //SetupGroupPanel( instance, false );
                        foreach ( Control control in pnlAssociatedGroupTypes.Controls )
                        {
                            if ( control.GetType() == typeof( RockRadioButtonList ) )
                            {
                                var rbl = ( RockRadioButtonList )control;
                                associatedGroupTypeId = rbl.ID.Substring( rbl.ID.LastIndexOf( '_' ) + 1 ).AsInteger();  // get GroupType id out of radio button list id
                                if ( rbl.SelectedValue.AsInteger() > 0 )
                                {
                                    if ( instance.GetAttributeValue( rbl.Label ) == null )
                                    {
                                        var groupTypeService = new GroupTypeService( rockContext );
                                        var groupTypeGuid = groupTypeService.Get( associatedGroupTypeId ).Guid;

                                        // Check for existing group
                                        var grp = groupService.Queryable()
                                            .Where( g => g.ParentGroupId == parentGroupId )
                                            .Where( g => g.GroupType.Guid == groupTypeGuid )
                                            .FirstOrDefault( g => g.IsActive );
                                        var showOnList = rbl.SelectedValue == "2";
                                        if ( grp == null )
                                        {
                                            CreateGroup( instance, rockContext, groupTypeGuid, parentGroupId, rbl.Label, rbl.Label, groupService, showOnList );
                                        }
                                        else
                                        {
                                            var showOnListInt = showOnList ? 1 : 0;
                                            instance.AttributeValues[rbl.Label].Value = string.Format( "{0}^{1}", grp.Guid.ToString(), showOnListInt );
                                            instance.SaveAttributeValues();
                                        }
                                        AssociatedGroupTypes.Add( groupTypeService.Get( groupTypeGuid ) );
                                        AssociatedGroupTypes = AssociatedGroupTypes.OrderBy( t => t.Name ).ToList();
                                        if ( showOnList )
                                        {
                                            AssociatedGroupTypesGrid.Add( groupTypeService.Get( groupTypeGuid ) );
                                            AssociatedGroupTypesGrid = AssociatedGroupTypesGrid.OrderBy( t => t.Name ).ToList();
                                        }
                                    }
                                    else if ( !string.IsNullOrWhiteSpace( instance.GetAttributeValue( rbl.Label ) ) )
                                    {
                                        var showOnListInt = 0;
                                        if ( rbl.SelectedValue == "2" )
                                        {
                                            showOnListInt = 1;
                                        }
                                        var val = instance.GetAttributeValue( rbl.Label ).Split('^').ToList();
                                        instance.AttributeValues[rbl.Label].Value = string.Format( "{0}^{1}", val[0], showOnListInt );   
                                        if( showOnListInt == 1 && !AssociatedGroupTypesGrid.Select( t => t.Id ).Contains( associatedGroupTypeId ) )
                                        {
                                            AssociatedGroupTypesGrid.Add( new GroupTypeService( new RockContext() ).Get( associatedGroupTypeId ) );
                                            AssociatedGroupTypesGrid = AssociatedGroupTypesGrid.OrderBy( t => t.Name ).ToList();
                                        }    
                                        if( showOnListInt == 0 && AssociatedGroupTypesGrid.Select( t => t.Id ).Contains( associatedGroupTypeId ) )
                                        {
                                            AssociatedGroupTypesGrid = AssociatedGroupTypesGrid.Where( t => t.Id != associatedGroupTypeId ).OrderBy( t => t.Name ).ToList();
                                        }                 
                                    }
                                }
                                else if( !string.IsNullOrWhiteSpace( instance.GetAttributeValue( rbl.Label ) ) )
                                {
                                    instance.AttributeValues[rbl.Label].Value = null;
                                    var gt = new GroupTypeService( rockContext ).Get( associatedGroupTypeId );
                                    if( gt != null )
                                    {
                                        AssociatedGroupTypes = AssociatedGroupTypes.Where( t => t.Guid != gt.Guid ).ToList();
                                        AssociatedGroupTypesGrid = AssociatedGroupTypesGrid.Where( t => t.Guid != gt.Guid ).ToList();
                                    }
                                }
                            }
                        }
                        rockContext.SaveChanges();
                        instance.SaveAttributeValues();
                    }
                }
            }

            if ( newInstance )
            {
                var qryParams = new Dictionary<string, string>();
                qryParams.Add( "RegistrationTemplateId", PageParameter( "RegistrationTemplateId" ) );
                qryParams.Add( "RegistrationInstanceId", instance.Id.ToString() );
                NavigateToCurrentPage( qryParams );
            }
            else
            {
                // Reload instance and show readonly view
                using ( var rockContext = new RockContext() )
                {
                    instance = new RegistrationInstanceService( rockContext ).Get( instance.Id );
                    ShowReadonlyDetails( instance );
                    
                }

                // show send payment reminder link
                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "PaymentReminderPage" ) ) && ( ( instance.RegistrationTemplate.SetCostOnInstance.HasValue && instance.RegistrationTemplate.SetCostOnInstance == true && instance.Cost.HasValue && instance.Cost.Value > 0 ) || instance.RegistrationTemplate.Cost > 0 ) )
                {
                    btnSendPaymentReminder.Visible = true;
                }
                else
                {
                    btnSendPaymentReminder.Visible = false;
                }
            }
            BuildSubGroupTabs();
            AddDynamicControls();
            BindRegistrantsGrid();
        }

        protected void mdlAddSubGroupMember_SaveClick( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                var rockContext = new RockContext();
                // Verify valid group
                var groupService = new GroupService( rockContext );
                var group = groupService.Get( hfSubGroupId.ValueAsInt() );
                if( group == null && ddlSubGroup.Visible )
                {
                    group = groupService.Get( ddlSubGroup.SelectedValue.AsInteger() );
                }
                if ( group != null )
                {
                    var p = new PersonService( rockContext ).Get( Guid.Parse( ddlRegistrantList.SelectedValue ) ) ;
                    int? personId = p.Id;

                    var role = new GroupTypeRoleService( rockContext ).Get( ddlGroupRole.SelectedValueAsInt() ?? 0 );

                    var groupMemberService = new GroupMemberService( rockContext );
                    GroupMember groupMember;

                    var groupMemberId = int.Parse( hfSubGroupMemberId.Value );

                    // if adding a new group member 
                    if ( groupMemberId.Equals( 0 ) )
                    {
                        groupMember = new GroupMember
                        {
                            Id = 0,
                            GroupId = group.Id,
                            PersonId = personId.Value
                        };
                    }
                    else
                    {
                        // load existing group member
                        groupMember = groupMemberService.Get( groupMemberId );
                        groupMember.GroupId = ddlSubGroup.SelectedValue.AsInteger();
                    }

                    groupMember.GroupRoleId = role.Id;
                    groupMember.Note = tbNote.Text;
                    groupMember.GroupMemberStatus = rblStatus.SelectedValueAsEnum<GroupMemberStatus>();

                    groupMember.LoadAttributes();

                    Rock.Attribute.Helper.GetEditValues( phAttributes, groupMember );

                    if ( !Page.IsValid )
                    {
                        return;
                    }

                    // using WrapTransaction because there are three Saves
                    rockContext.WrapTransaction( ( ) =>
                    {
                        if ( groupMember.Id.Equals( 0 ) )
                        {
                            groupMemberService.Add( groupMember );
                        }

                        rockContext.SaveChanges();
                        groupMember.SaveAttributeValues( rockContext );
                    } );

                    rpGroupPanels.DataSource = AssociatedGroupTypes;
                    rpGroupPanels.DataBind();
                    if ( hfRegistrationInstanceId.Value.AsInteger() > 0 )
                    {
                        BindRegistrantsFilter( new RegistrationInstanceService( rockContext ).Get( hfRegistrationInstanceId.Value.AsInteger() ) );
                    }
                    BindRegistrantsGrid();
                    mdlAddSubGroupMember.Hide();
                }
            }
        }

        private int CreateGroup( RegistrationInstance instance, RockContext rockContext, Guid groupTypeGuid, int parentGroupId, string attributeKey, string groupName, GroupService groupService, bool showOnList )
        {
            var result = 0;
            var subGroup = new Group();
            if ( parentGroupId > 0  )
            {
                var groupTypeService = new GroupTypeService( rockContext );
                subGroup.Name = groupName;
                subGroup.ParentGroupId = parentGroupId;
                var subGroupType = groupTypeService.Get( groupTypeGuid );
                subGroup.GroupType = subGroupType;
                groupService.Add( subGroup );
                rockContext.SaveChanges();

                subGroup = new GroupService( new RockContext() ).Get( subGroup.Guid );
                var showOnListInt = showOnList ? 1 : 0;
                instance.AttributeValues[attributeKey].Value = string.Format( "{0}^{1}", subGroup.Guid.ToString(), showOnListInt );
                instance.SaveAttributeValues();
                result = subGroup.Id;
            }
            return result;
        }

        private static void VerifyEntityAttribute( RockContext rockContext, string attributeKey, string attributeName, Object entity )
        {
            int? entityTypeId = null;
            var attributeService = new AttributeService( rockContext );
            Rock.Model.Attribute attribute = null;
            entityTypeId = EntityTypeCache.Read( entity.GetType() ).Id;
            IQueryable<Rock.Model.Attribute> attributeQuery = null;
            if ( entityTypeId != null )
            {
                attributeQuery = attributeService.Get( entityTypeId, string.Empty, string.Empty );
                attributeQuery = attributeQuery.Where( a => a.Key == attributeKey );
            }
            if ( attributeQuery.Count() == 0 )
            {
                var edtAttribute = new Rock.Model.Attribute
                {
                    FieldTypeId = FieldTypeCache.Read( Rock.SystemGuid.FieldType.KEY_VALUE_LIST ).Id,
                    Name = attributeName,
                    Key = attributeKey
                };
                attribute = Rock.Attribute.Helper.SaveAttributeEdits( edtAttribute, entityTypeId, string.Empty, string.Empty );

                AttributeCache.FlushEntityAttributes();
            }
            attribute.LoadAttributes();
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( hfRegistrationInstanceId.Value.Equals( "0" ) )
            {
                var qryParams = new Dictionary<string, string>();

                var parentTemplateId = PageParameter( "RegistrationTemplateId" ).AsIntegerOrNull();
                if ( parentTemplateId.HasValue )
                {
                    qryParams["RegistrationTemplateId"] = parentTemplateId.ToString();
                }

                // Cancelling on Add.  Return to Grid
                NavigateToParentPage( qryParams );
            }
            else
            {
                // Cancelling on Edit.  Return to Details
                using ( var rockContext = new RockContext() )
                {
                    var service = new RegistrationInstanceService( rockContext );
                    var item = service.Get( int.Parse( hfRegistrationInstanceId.Value ) );
                    ShowReadonlyDetails( item );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbTab control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbTab_Click( object sender, EventArgs e )
        {
            var lb = sender as LinkButton;
            if ( lb != null )
            {
                ActiveTab = lb.ID;
                ShowTab();
            }
        }

        protected void lbTemplate_Click( object sender, EventArgs e )
        {
            var qryParams = new Dictionary<string, string>();
            using ( var rockContext = new RockContext() )
            {
                var service = new RegistrationInstanceService( rockContext );
                var registrationInstance = service.Get( hfRegistrationInstanceId.Value.AsInteger() );
                if ( registrationInstance != null )
                {
                    qryParams.Add( "RegistrationTemplateId", registrationInstance.RegistrationTemplateId.ToString() );
                }
            }
            NavigateToParentPage( qryParams );
        }

        #endregion

        #region Registration Tab Events

        /// <summary>
        /// Handles the ApplyFilterClick event of the fRegistrations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fRegistrations_ApplyFilterClick( object sender, EventArgs e )
        {
            fRegistrations.SaveUserPreference( "Date Range", drpRegistrationDateRange.DelimitedValues );
            fRegistrations.SaveUserPreference( "Payment Status", ddlRegistrationPaymentStatus.SelectedValue );
            fRegistrations.SaveUserPreference( "RegisteredBy First Name", tbRegistrationRegisteredByFirstName.Text );
            fRegistrations.SaveUserPreference( "RegisteredBy Last Name", tbRegistrationRegisteredByLastName.Text );
            fRegistrations.SaveUserPreference( "Registrant First Name", tbRegistrationRegistrantFirstName.Text );
            fRegistrations.SaveUserPreference( "Registrant Last Name", tbRegistrationRegistrantLastName.Text );

            BindRegistrationsGrid();
        }

        /// <summary>
        /// Fs the registrations_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void fRegistrations_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Date Range":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }
                case "Payment Status":
                case "RegisteredBy First Name":
                case "RegisteredBy Last Name":
                case "Registrant First Name":
                case "Registrant Last Name":
                    {
                        break;
                    }
                default:
                    {
                        e.Value = string.Empty;
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gRegistrations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gRegistrations_GridRebind( object sender, EventArgs e )
        {
            BindRegistrationsGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gRegistrations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gRegistrations_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var registration = e.Row.DataItem as Registration;
            if ( registration != null )
            {
                // Set the processor value
                var lRegisteredBy = e.Row.FindControl( "lRegisteredBy" ) as Literal;
                if ( lRegisteredBy != null )
                {
                    if ( registration.PersonAlias != null && registration.PersonAlias.Person != null )
                    {
                        lRegisteredBy.Text = registration.PersonAlias.Person.FullNameReversed;
                    }
                    else
                    {
                        lRegisteredBy.Text = string.Format( "{0}, {1}", registration.LastName, registration.FirstName );
                    }
                }

                var registrantNames = string.Empty;
                if ( registration.Registrants != null && registration.Registrants.Any() )
                {
                    var registrants = registration.Registrants
                        .Where( r =>
                            r.PersonAlias != null &&
                            r.PersonAlias.Person != null )
                        .OrderBy( r => r.PersonAlias.Person.NickName )
                        .ThenBy( r => r.PersonAlias.Person.LastName )
                        .ToList();

                    registrantNames = registrants
                        .Select( r => r.PersonAlias.Person.NickName + " " + r.PersonAlias.Person.LastName )
                        .ToList()
                        .AsDelimited( "<br/>" );
                }

                // Set the Registrants
                var lRegistrants = e.Row.FindControl( "lRegistrants" ) as Literal;
                if ( lRegistrants != null )
                {
                    lRegistrants.Text = registrantNames;
                }

                var payments = RegistrationPayments.Where( p => p.EntityId == registration.Id );
                var hasPayments = payments.Any();
                var totalPaid = hasPayments ? payments.Select( p => p.Amount ).DefaultIfEmpty().Sum() : 0.0m;

                var hfHasPayments = e.Row.FindControl( "hfHasPayments" ) as HiddenFieldWithClass;
                if ( hfHasPayments != null )
                {
                    hfHasPayments.Value = hasPayments.ToString();
                }

                // Set the Cost
                var discountedCost = registration.DiscountedCost;
                var lCost = e.Row.FindControl( "lCost" ) as Label;
                if ( lCost != null )
                {
                    lCost.Visible = _instanceHasCost || discountedCost > 0.0M;
                    lCost.Text = discountedCost.FormatAsCurrency();
                }

                var discountCode = registration.DiscountCode;
                var lDiscount = e.Row.FindControl( "lDiscount" ) as Label;
                if ( lDiscount != null )
                {
                    lDiscount.Visible = _instanceHasCost && !string.IsNullOrEmpty( discountCode );
                    lDiscount.Text = discountCode;
                }

                var lBalance = e.Row.FindControl( "lBalance" ) as Label;
                if ( lBalance != null )
                {
                    var balanceDue = registration.DiscountedCost - totalPaid;
                    lBalance.Visible = _instanceHasCost || discountedCost > 0.0M;
                    lBalance.Text = balanceDue.FormatAsCurrency();
                    if ( balanceDue > 0.0m )
                    {
                        lBalance.AddCssClass( "label-danger" );
                        lBalance.RemoveCssClass( "label-warning" );
                        lBalance.RemoveCssClass( "label-success" );
                    }
                    else if ( balanceDue < 0.0m )
                    {
                        lBalance.RemoveCssClass( "label-danger" );
                        lBalance.AddCssClass( "label-warning" );
                        lBalance.RemoveCssClass( "label-success" );
                    }
                    else
                    {
                        lBalance.RemoveCssClass( "label-danger" );
                        lBalance.RemoveCssClass( "label-warning" );
                        lBalance.AddCssClass( "label-success" );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the AddClick event of the gRegistrations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gRegistrations_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "RegistrationPage", "RegistrationId", 0, "RegistrationInstanceId", hfRegistrationInstanceId.ValueAsInt() );
        }

        /// <summary>
        /// Handles the Delete event of the gRegistrations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gRegistrations_Delete( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var registrationService = new RegistrationService( rockContext );
                var registration = registrationService.Get( e.RowKeyId );
                if ( registration != null )
                {
                    var registrationInstanceId = registration.RegistrationInstanceId;

                    if ( !UserCanEdit &&
                        !registration.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) &&
                        !registration.IsAuthorized( Authorization.ADMINISTRATE, this.CurrentPerson ) )
                    {
                        mdDeleteWarning.Show( "You are not authorized to delete this registration.", ModalAlertType.Information );
                        return;
                    }

                    string errorMessage;
                    if ( !registrationService.CanDelete( registration, out errorMessage ) )
                    {
                        mdRegistrationsGridWarning.Show( errorMessage, ModalAlertType.Information );
                        return;
                    }

                    var changes = new List<string>();
                    changes.Add( "Deleted registration" );

                    // remove registrants from any associated groups
                    var memberService = new GroupMemberService( rockContext );
                    var deleteMembers = new List<GroupMember>();
                    foreach ( GroupType groupType in AssociatedGroupTypes )
                    {
                        foreach ( RegistrationRegistrant registrant in registration.Registrants )
                        {
                            deleteMembers.AddRange( GetDeleteGroupMembers( rockContext, memberService, groupType, registrant ) );
                        }
                    }


                    rockContext.WrapTransaction( () =>
                    {
                        HistoryService.SaveChanges(
                            rockContext,
                            typeof( Registration ),
                            Rock.SystemGuid.Category.HISTORY_EVENT_REGISTRATION.AsGuid(),
                            registration.Id,
                            changes );

                        registrationService.Delete( registration );
                        foreach( GroupMember member in deleteMembers )
                        {
                            memberService.Delete( member );
                        }
                        rockContext.SaveChanges();
                    } );

                    SetHasPayments( registrationInstanceId, rockContext );
                }
            }

            BindRegistrationsGrid();
        }

        private List<GroupMember> GetDeleteGroupMembers( RockContext rockContext, GroupMemberService memberService, GroupType groupType, RegistrationRegistrant registrant )
        {
            var groupService = new GroupService( rockContext );
            var deleteMembers = new List<GroupMember>();
            var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            var ri = new RegistrationInstanceService( new RockContext() ).Get( RegistrationInstanceId );
            ri.LoadAttributes();
            var parentGroup = groupService.Get( Guid.Parse( ri.AttributeValues[groupType.Name].Value ) );
            var groupIds = parentGroup.Groups.Select( g => g.Id ).ToList();
            deleteMembers.AddRange( memberService.Queryable()
                                        .Where( m => groupIds.Contains( m.GroupId ) )
                                        .Where( m => m.PersonId == registrant.PersonId ).ToList() );
            return deleteMembers;
        }

        /// <summary>
        /// Handles the RowSelected event of the gRegistrations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gRegistrations_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "RegistrationPage", "RegistrationId", e.RowKeyId );
        }

        #endregion

        #region Registrant Tab Events

        /// <summary>
        /// Handles the ApplyFilterClick event of the fRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fRegistrants_ApplyFilterClick( object sender, EventArgs e )
        {
            fRegistrants.SaveUserPreference( "Date Range", drpRegistrantDateRange.DelimitedValues );
            fRegistrants.SaveUserPreference( "First Name", tbRegistrantFirstName.Text );
            fRegistrants.SaveUserPreference( "Last Name", tbRegistrantLastName.Text );
            fRegistrants.SaveUserPreference( "In Group", ddlInGroup.SelectedValue );
            fRegistrants.SaveUserPreference( "Signed Document", ddlSignedDocument.SelectedValue );

            if ( RegistrantFields != null )
            {
                foreach ( var field in RegistrantFields )
                {
                    if ( field.FieldSource == RegistrationFieldSource.PersonField && field.PersonFieldType.HasValue )
                    {
                        switch ( field.PersonFieldType.Value )
                        {
                            case RegistrationPersonFieldType.Campus:
                                {
                                    var ddlCampus = phRegistrantFormFieldFilters.FindControl( "ddlCampus" ) as RockDropDownList;
                                    if ( ddlCampus != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Home Campus", ddlCampus.SelectedValue );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Email:
                                {
                                    var tbEmailFilter = phRegistrantFormFieldFilters.FindControl( "tbEmailFilter" ) as RockTextBox;
                                    if ( tbEmailFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Email", tbEmailFilter.Text );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Birthdate:
                                {
                                    var drpBirthdateFilter = phRegistrantFormFieldFilters.FindControl( "drpBirthdateFilter" ) as DateRangePicker;
                                    if ( drpBirthdateFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Birthdate Range", drpBirthdateFilter.DelimitedValues );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Grade:
                                {
                                    var gpGradeFilter = phRegistrantFormFieldFilters.FindControl( "gpGradeFilter" ) as GradePicker;
                                    if ( gpGradeFilter != null )
                                    {
                                        var gradeOffset = gpGradeFilter.SelectedValueAsInt( false );
                                        fRegistrants.SaveUserPreference( "Grade", gradeOffset.HasValue ? gradeOffset.Value.ToString() : "" );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.Gender:
                                {
                                    var ddlGenderFilter = phRegistrantFormFieldFilters.FindControl( "ddlGenderFilter" ) as RockDropDownList;
                                    if ( ddlGenderFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Gender", ddlGenderFilter.SelectedValue );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.MaritalStatus:
                                {
                                    var ddlMaritalStatusFilter = phRegistrantFormFieldFilters.FindControl( "ddlMaritalStatusFilter" ) as RockDropDownList;
                                    if ( ddlMaritalStatusFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Marital Status", ddlMaritalStatusFilter.SelectedValue );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.MobilePhone:
                                {
                                    var tbPhoneFilter = phRegistrantFormFieldFilters.FindControl( "tbPhoneFilter" ) as RockTextBox;
                                    if ( tbPhoneFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Phone", tbPhoneFilter.Text );
                                    }

                                    break;
                                }
                        }
                    }

                    if ( field.Attribute != null )
                    {
                        var attribute = field.Attribute;
                        var filterControl = phRegistrantFormFieldFilters.FindControl( "filter_" + attribute.Id.ToString() );
                        if ( filterControl != null )
                        {
                            try
                            {
                                var values = attribute.FieldType.Field.GetFilterValues( filterControl, field.Attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                fRegistrants.SaveUserPreference( attribute.Key, attribute.Name, attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter ).ToJson() );
                            }
                            catch { }
                        }
                    }
                }
            }

            BindRegistrantsGrid();
        }

        /// <summary>
        /// Fs the registrants_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void fRegistrants_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            if ( RegistrantFields != null )
            {
                var attribute = RegistrantFields
                    .Where( a =>
                        a.Attribute != null &&
                        a.Attribute.Key == e.Key )
                    .Select( a => a.Attribute )
                    .FirstOrDefault();

                if ( attribute != null )
                {
                    try
                    {
                        var values = JsonConvert.DeserializeObject<List<string>>( e.Value );
                        e.Value = attribute.FieldType.Field.FormatFilterValues( attribute.QualifierValues, values );
                        return;
                    }
                    catch { }
                }
            }

            switch ( e.Key )
            {
                case "Date Range":
                case "Birthdate Range":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }
                case "Grade":
                    {
                        e.Value = Person.GradeFormattedFromGradeOffset( e.Value.AsIntegerOrNull() );
                        break;
                    }
                case "First Name":
                case "Last Name":
                case "Email":
                case "Phone":
                case "Signed Document":
                    {
                        break;
                    }
                case "Gender":
                    {
                        var gender = e.Value.ConvertToEnumOrNull<Gender>();
                        e.Value = gender.HasValue ? gender.ConvertToString() : string.Empty;
                        break;
                    }
                case "Campus":
                    {
                        var campusId = e.Value.AsIntegerOrNull();
                        if ( campusId.HasValue )
                        {
                            var campus = CampusCache.Read( campusId.Value );
                            e.Value = campus != null ? campus.Name : string.Empty;
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }
                        break;
                    }
                case "Marital Status":
                    {
                        var dvId = e.Value.AsIntegerOrNull();
                        if ( dvId.HasValue )
                        {
                            var maritalStatus = DefinedValueCache.Read( dvId.Value );
                            e.Value = maritalStatus != null ? maritalStatus.Value : string.Empty;
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }
                        break;
                    }
                case "In Group":
                    {
                        e.Value = e.Value;
                        break;
                    }
                default:
                    {
                        e.Value = string.Empty;
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gRegistrants_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindRegistrantsGrid( e.IsExporting );
        }

        /// <summary>
        /// Handles the RowDataBound event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        private void gRegistrants_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var registrant = e.Row.DataItem as RegistrationRegistrant;
            if ( registrant != null )
            {
                // Set the registrant name value
                var lRegistrant = e.Row.FindControl( "lRegistrant" ) as Literal;
                if ( lRegistrant != null )
                {
                    if ( registrant.PersonAlias != null && registrant.PersonAlias.Person != null )
                    {
                        lRegistrant.Text = registrant.PersonAlias.Person.FullNameReversed +
                            ( Signers != null && !Signers.Contains( registrant.PersonAlias.PersonId ) ?
                                " <i class='fa fa-pencil-square-o text-danger'></i>" :
                                string.Empty  );
                    }
                    else
                    {
                        lRegistrant.Text = string.Empty;
                    }
                }

                // Set the Group Name
                if ( registrant.GroupMember != null && GroupLinks.ContainsKey( registrant.GroupMember.GroupId ) )
                {
                    var lGroup = e.Row.FindControl( "lGroup" ) as Literal;
                    if ( lGroup != null )
                    {
                        lGroup.Text = GroupLinks[registrant.GroupMember.GroupId];
                    }
                }

                // Set the campus
                var lCampus = e.Row.FindControl( "lCampus" ) as Literal;
                if ( lCampus != null && PersonCampusIds != null )
                {
                    if ( registrant.PersonAlias != null )
                    {
                        if ( PersonCampusIds.ContainsKey( registrant.PersonAlias.PersonId ) )
                        {
                            var campusIds = PersonCampusIds[registrant.PersonAlias.PersonId];
                            if ( campusIds.Any() )
                            {
                                var campusNames = new List<string>();
                                foreach ( int campusId in campusIds )
                                {
                                    var campus = CampusCache.Read( campusId );
                                    if ( campus != null )
                                    {
                                        campusNames.Add( campus.Name );
                                    }
                                }

                                lCampus.Text = campusNames.AsDelimited( "<br/>" );
                            }
                        }
                    }
                }

                // Set the Fees
                var lFees = e.Row.FindControl( "lFees" ) as Literal;
                if ( lFees != null )
                {
                    if ( registrant.Fees != null && registrant.Fees.Any() )
                    {
                        var feeDesc = new List<string>();
                        foreach ( var fee in registrant.Fees )
                        {
                            feeDesc.Add( string.Format( "{0}{1} ({2})",
                                fee.Quantity > 1 ? fee.Quantity.ToString( "N0" ) + " " : "",
                                fee.Quantity > 1 ? fee.RegistrationTemplateFee.Name.Pluralize() : fee.RegistrationTemplateFee.Name,
                                fee.Cost.FormatAsCurrency() ) );
                        }
                        lFees.Text = feeDesc.AsDelimited( "<br/>" );
                    }
                }

                // Set SubGroup values
                var rockContext = new RockContext();
                var groupService = new GroupService( rockContext );
                var memberService = new GroupMemberService( rockContext );
                var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
                var ri = new RegistrationInstanceService( new RockContext() ).Get( RegistrationInstanceId );
                ri.LoadAttributes();
                var columnIndex = _registrantGridColumnCount;
                if ( AssociatedGroupTypesGrid != null )
                {
                    foreach ( GroupType groupType in AssociatedGroupTypesGrid )
                    {
                        var attributeValSplit = ri.AttributeValues[groupType.Name].Value.Split( '^' ).ToList();
                        var parentGroup = groupService.Get( Guid.Parse( attributeValSplit[0] ) );

                        var changeGroup = e.Row.Cells[columnIndex].Controls[0] as LinkButton;
                        var parentGroupColumn = e.Row.FindControl( "lSubGroup_" + parentGroup.Id ) as Literal;
                        if ( changeGroup != null )
                        {
                            var subGroupIds = parentGroup.Groups.Select( g => g.Id ).ToList();
                            var member = memberService.Queryable()
                                                    .AsNoTracking()
                                                    .Where( m => m.PersonId == registrant.PersonId )
                                                    .FirstOrDefault( m => subGroupIds.Contains( m.GroupId ) );
                            if ( member != null )
                            {
                                changeGroup.Text = member.Group.Name;
                                changeGroup.CommandName = "ChangeSubGroup";
                                changeGroup.CommandArgument = member.Id.ToString();

                            }
                            else
                            {
                                changeGroup.CssClass = "btn-add btn btn-default btn-sm";
                                if ( parentGroup.Groups.Count() > 0 )
                                {
                                    using ( var literalControl = new LiteralControl( "<i class='fa fa-plus-circle'></i>" ) )
                                    {
                                        changeGroup.Controls.Add( literalControl );
                                    }
                                    using ( var literalControl = new LiteralControl( "<span class='grid-btn-assign-text'> Assign</span>" ) )
                                    {
                                        changeGroup.Controls.Add( literalControl );
                                    }
                                    changeGroup.CommandName = "AssignSubGroup";
                                    changeGroup.CommandArgument = string.Format( "{0}|{1}", parentGroup.Id.ToString(), registrant.Id.ToString() );
                                }
                                else
                                {
                                    using ( var literalControl = new LiteralControl( "<i class='fa fa-minus-circle'></i>" ) )
                                    {
                                        changeGroup.Controls.Add( literalControl );
                                    }
                                    using ( var literalControl = new LiteralControl( string.Format( "<span class='grid-btn-assign-text'> No {0}</span>", parentGroup.GroupType.GroupTerm.Pluralize() ) ) )
                                    {
                                        changeGroup.Controls.Add( literalControl );
                                    }
                                    changeGroup.Enabled = false;
                                }
                            }
                            columnIndex++;
                        }
                    }
                }
                // add addresses if exporting
                if ( _homeAddresses.Count > 0 )
                {
                    var lStreet1 = e.Row.FindControl( "lStreet1" ) as Literal;
                    var lStreet2 = e.Row.FindControl( "lStreet2" ) as Literal;
                    var lCity = e.Row.FindControl( "lCity" ) as Literal;
                    var lState = e.Row.FindControl( "lState" ) as Literal;
                    var lPostalCode = e.Row.FindControl( "lPostalCode" ) as Literal;
                    var lCountry = e.Row.FindControl( "lCountry" ) as Literal;

                    var location = _homeAddresses[registrant.PersonId.Value];
                    if (location != null )
                    {
                        lStreet1.Text = location.Street1;
                        lStreet2.Text = location.Street2;
                        lCity.Text = location.City;
                        lState.Text = location.State;
                        lPostalCode.Text = location.PostalCode;
                        lCountry.Text = location.Country;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the AddClick event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void gRegistrants_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "RegistrationPage", "RegistrationId", 0, "RegistrationInstanceId", hfRegistrationInstanceId.ValueAsInt() );
        }

        /// <summary>
        /// Handles the RowSelected event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gRegistrants_RowSelected( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var registrantService = new RegistrationRegistrantService( rockContext );
                var registrant = registrantService.Get( e.RowKeyId );
                if ( registrant != null )
                {
                    var qryParams = new Dictionary<string, string>();
                    qryParams.Add( "RegistrationId", registrant.RegistrationId.ToString() );
                    var url = LinkedPageUrl( "RegistrationPage", qryParams );
                    url += "#" + e.RowKeyValue;
                    Response.Redirect( url, false );
                }
            }
        }

        /// <summary>
        /// Handles the RowCommand event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gRegistrants_RowCommand( object sender, GridViewCommandEventArgs e )
        {
            if( e.CommandName == "AssignSubGroup" )
            {
                var argument = e.CommandArgument.ToString().Split( '|' ).ToList();
                var parentGroupId = 0;
                var registrantId = 0;
                int.TryParse( argument[0], out parentGroupId );
                int.TryParse( argument[1], out registrantId );
                if( parentGroupId > 0 && registrantId > 0 )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var groupService = new GroupService( rockContext );
                        var registrantService = new RegistrationRegistrantService( rockContext );
                        var parentGroup = groupService.Get( parentGroupId );
                        var registrant = registrantService.Get( registrantId );
                        if ( registrant != null )
                        {
                            RenderMemberModal( rockContext, parentGroup, null, null, registrant );
                        }
                    }
                }
            }
            if( e.CommandName == "ChangeSubGroup" )
            {
                var subGroupMemberId = 0;
                if ( int.TryParse( e.CommandArgument.ToString(), out subGroupMemberId ) && subGroupMemberId > 0 )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var groupMemberService = new GroupMemberService( rockContext );
                        var groupMember = groupMemberService.Get( subGroupMemberId );
                        RenderMemberModal( rockContext, groupMember.Group.ParentGroup, groupMember.Group, groupMember, null );
                    }
                }
            }
            if ( hfRegistrationInstanceId.Value.AsInteger() > 0 )
            {
                BindRegistrantsFilter( new RegistrationInstanceService( new RockContext() ).Get( hfRegistrationInstanceId.Value.AsInteger() ) );
            }
            BindRegistrantsGrid();
        }

        /// <summary>
        /// Handles the Delete event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gRegistrants_Delete( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var registrantService = new RegistrationRegistrantService( rockContext );
                var registrant = registrantService.Get( e.RowKeyId );
                if ( registrant != null )
                {
                    string errorMessage;
                    if ( !registrantService.CanDelete( registrant, out errorMessage ) )
                    {
                        mdRegistrantsGridWarning.Show( errorMessage, ModalAlertType.Information );
                        return;
                    }

                    // remove registrant from any associated groups
                    var memberService = new GroupMemberService( rockContext );
                    var deleteMembers = new List<GroupMember>();
                    foreach ( GroupType groupType in AssociatedGroupTypes )
                    {
                        deleteMembers.AddRange( GetDeleteGroupMembers( rockContext, memberService, groupType, registrant ) );
                    }

                    registrantService.Delete( registrant );
                    foreach ( GroupMember member in deleteMembers )
                    {
                        memberService.Delete( member );
                    }
                    rockContext.SaveChanges();
                }
            }

            BindRegistrantsGrid();
        }

        #endregion

        #region Payment Tab Events

        /// <summary>
        /// Handles the ApplyFilterClick event of the fPayments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fPayments_ApplyFilterClick( object sender, EventArgs e )
        {
            fPayments.SaveUserPreference( "Date Range", drpPaymentDateRange.DelimitedValues );

            BindPaymentsGrid();
        }

        /// <summary>
        /// Fs the payments_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void fPayments_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Date Range":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }
                default:
                    {
                        e.Value = string.Empty;
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the RowSelected event of the gPayments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gPayments_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "TransactionDetailPage", "transactionId", e.RowKeyId );
        }

        /// <summary>
        /// Handles the GridRebind event of the gPayments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gPayments_GridRebind( object sender, EventArgs e )
        {
            BindPaymentsGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gPayments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        private void gPayments_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var transaction = e.Row.DataItem as FinancialTransaction;
            var lRegistrar = e.Row.FindControl( "lRegistrar" ) as Literal;
            var lRegistrants = e.Row.FindControl( "lRegistrants" ) as Literal;

            if ( transaction != null && lRegistrar != null && lRegistrants != null )
            {
                var registrars = new List<string>();
                var registrants = new List<string>();

                var registrationIds = transaction.TransactionDetails.Select( d => d.EntityId ).ToList();
                foreach ( var registration in PaymentRegistrations
                    .Where( r => registrationIds.Contains( r.Id ) ) )
                {
                    if ( registration.PersonAlias != null && registration.PersonAlias.Person != null )
                    {
                        var qryParams = new Dictionary<string, string>();
                        qryParams.Add( "RegistrationId", registration.Id.ToString() );
                        var url = LinkedPageUrl( "RegistrationPage", qryParams );
                        registrars.Add( string.Format( "<a href='{0}'>{1}</a>", url, registration.PersonAlias.Person.FullName ) );

                        foreach ( var registrant in registration.Registrants )
                        {
                            if ( registrant.PersonAlias != null && registrant.PersonAlias.Person != null )
                            {
                                registrants.Add( registrant.PersonAlias.Person.FullName );
                            }
                        }
                    }
                }

                lRegistrar.Text = registrars.AsDelimited( "<br/>" );
                lRegistrants.Text = registrants.AsDelimited( "<br/>" );
            }
        }

        #endregion

        #region Linkage Tab Events

        /// <summary>
        /// Handles the ApplyFilterClick event of the fLinkages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fLinkages_ApplyFilterClick( object sender, EventArgs e )
        {
            fLinkages.SaveUserPreference( "Campus", cblCampus.SelectedValues.AsDelimited( ";" ) );

            BindLinkagesGrid();
        }

        /// <summary>
        /// Fs the campusEventItems_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void fLinkages_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Campus":
                    {
                        var values = new List<string>();
                        foreach ( string value in e.Value.Split( ';' ) )
                        {
                            var item = cblCampus.Items.FindByValue( value );
                            if ( item != null )
                            {
                                values.Add( item.Text );
                            }
                        }
                        e.Value = values.AsDelimited( ", " );
                        break;
                    }
                default:
                    {
                        e.Value = string.Empty;
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gLinkages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gLinkages_GridRebind( object sender, EventArgs e )
        {
            BindLinkagesGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gLinkages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gLinkages_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var eventItemOccurrenceGroupMap = e.Row.DataItem as EventItemOccurrenceGroupMap;
                if ( eventItemOccurrenceGroupMap != null && eventItemOccurrenceGroupMap.EventItemOccurrence != null )
                {
                    if ( eventItemOccurrenceGroupMap.EventItemOccurrence.EventItem != null )
                    {
                        var lCalendarItem = e.Row.FindControl( "lCalendarItem" ) as Literal;
                        if ( lCalendarItem != null )
                        {
                            var calendarItems = new List<string>();
                            foreach ( var calendarItem in eventItemOccurrenceGroupMap.EventItemOccurrence.EventItem.EventCalendarItems )
                            {
                                if ( calendarItem.EventItem != null && calendarItem.EventCalendar != null )
                                {
                                    var qryParams = new Dictionary<string, string>();
                                    qryParams.Add( "EventCalendarId", calendarItem.EventCalendarId.ToString() );
                                    qryParams.Add( "EventItemId", calendarItem.EventItem.Id.ToString() );
                                    calendarItems.Add( string.Format( "<a href='{0}'>{1}</a> ({2})",
                                        LinkedPageUrl( "CalendarItemPage", qryParams ),
                                        calendarItem.EventItem.Name,
                                        calendarItem.EventCalendar.Name ) );
                                }
                            }
                            lCalendarItem.Text = calendarItems.AsDelimited( "<br/>" );
                        }

                        if ( eventItemOccurrenceGroupMap.EventItemOccurrence.ContentChannelItems.Any() )
                        {
                            var lContentItem = e.Row.FindControl( "lContentItem" ) as Literal;
                            if ( lContentItem != null )
                            {
                                var contentItems = new List<string>();
                                foreach ( var contentItem in eventItemOccurrenceGroupMap.EventItemOccurrence.ContentChannelItems
                                    .Where( c => c.ContentChannelItem != null )
                                    .Select( c => c.ContentChannelItem ) )
                                {
                                    var qryParams = new Dictionary<string, string>();
                                    qryParams.Add( "ContentItemId", contentItem.Id.ToString() );
                                    contentItems.Add( string.Format( "<a href='{0}'>{1}</a>",
                                        LinkedPageUrl( "ContentItemPage", qryParams ),
                                        contentItem.Title ) );
                                }
                                lContentItem.Text = contentItems.AsDelimited( "<br/>" );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the AddClick event of the gLinkages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gLinkages_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "LinkagePage", "LinkageId", 0, "RegistrationInstanceId", hfRegistrationInstanceId.ValueAsInt() );
        }

        /// <summary>
        /// Handles the Edit event of the gLinkages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gLinkages_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "LinkagePage", "LinkageId", e.RowKeyId, "RegistrationInstanceId", hfRegistrationInstanceId.ValueAsInt() );
        }

        /// <summary>
        /// Handles the Delete event of the gLinkages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gLinkages_Delete( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var campusEventItemService = new EventItemOccurrenceGroupMapService( rockContext );
                var campusEventItem = campusEventItemService.Get( e.RowKeyId );
                if ( campusEventItem != null )
                {
                    string errorMessage;
                    if ( !campusEventItemService.CanDelete( campusEventItem, out errorMessage ) )
                    {
                        mdLinkagesGridWarning.Show( errorMessage, ModalAlertType.Information );
                        return;
                    }

                    campusEventItemService.Delete( campusEventItem );
                    rockContext.SaveChanges();
                }
            }

            BindLinkagesGrid();
        }

        #endregion

        #region Group Placement Tab Events

        /// <summary>
        /// Handles the GridRebind event of the gGroupPlacements control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGroupPlacements_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGroupPlacementGrid( e.IsExporting );
        }

        /// <summary>
        /// Handles the SelectItem event of the gpGroupPlacementParentGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gpGroupPlacementParentGroup_SelectItem( object sender, EventArgs e )
        {
            var parentGroupId = gpGroupPlacementParentGroup.SelectedValueAsInt();

            SetUserPreference( string.Format( "ParentGroup_{0}_{1}", BlockId, hfRegistrationInstanceId.Value ),
                parentGroupId.HasValue ? parentGroupId.Value.ToString() : "", true );

            var groupPickerField = gGroupPlacements.Columns.OfType<GroupPickerField>().FirstOrDefault();
            if ( groupPickerField != null )
            {
                groupPickerField.RootGroupId = parentGroupId;
            }

            BindGroupPlacementGrid();
        }

        /// <summary>
        /// Handles the Click event of the lbPlaceInGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbPlaceInGroup_Click( object sender, EventArgs e )
        {
            var col = gGroupPlacements.Columns.OfType<GroupPickerField>().FirstOrDefault();
            if ( col != null )
            {
                var placements = new Dictionary<int, List<int>>();

                var colIndex = gGroupPlacements.Columns.IndexOf( col ).ToString();
                foreach ( GridViewRow row in gGroupPlacements.Rows )
                {
                    var gp = row.FindControl( "groupPicker_" + colIndex.ToString() ) as GroupPicker;
                    if ( gp != null )
                    {
                        var groupId = gp.SelectedValueAsInt();
                        if ( groupId.HasValue )
                        {
                            var registrantId = (int)gGroupPlacements.DataKeys[row.RowIndex].Value;
                            placements.AddOrIgnore( groupId.Value, new List<int>() );
                            placements[groupId.Value].Add( registrantId );
                        }
                    }
                }

                using ( var rockContext = new RockContext() )
                {
                    var groupMemberService = new GroupMemberService( rockContext );

                    // Get all the registrants that were selected
                    var registrantIds = placements.SelectMany( p => p.Value ).ToList();
                    var registrants = new RegistrationRegistrantService( rockContext )
                        .Queryable( "PersonAlias" ).AsNoTracking()
                        .Where( r => registrantIds.Contains( r.Id ) )
                        .ToList();

                    // Get any groups that were selected
                    var groupIds = placements.Keys.ToList();
                    foreach ( var group in new GroupService( rockContext )
                        .Queryable( "GroupType" ).AsNoTracking()
                        .Where( g => groupIds.Contains( g.Id ) ) )
                    {
                        foreach ( int registrantId in placements[group.Id] )
                        {
                            var roleId = group.GroupType.DefaultGroupRoleId;
                            if ( !roleId.HasValue )
                            {
                                roleId = group.GroupType.Roles
                                    .OrderBy( r => r.Order )
                                    .Select( r => r.Id )
                                    .FirstOrDefault();
                            }

                            var registrant = registrants.FirstOrDefault( r => r.Id == registrantId );
                            if ( registrant != null && roleId.HasValue && roleId.Value > 0 )
                            {
                                var groupMember = new GroupMember
                                {
                                    PersonId = registrant.PersonAlias.PersonId,
                                    GroupId = group.Id,
                                    GroupRoleId = roleId.Value,
                                    GroupMemberStatus = GroupMemberStatus.Active
                                };
                                groupMemberService.Add( groupMember );
                            }
                        }
                    }

                    rockContext.SaveChanges();
                }
            }

            BindGroupPlacementGrid();
        }

        #endregion

        #endregion

        #region Methods

        #region Main Form Methods

        /// <summary>
        /// Gets the registration instance.
        /// </summary>
        /// <param name="registrationInstanceId">The registration instance identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private RegistrationInstance GetRegistrationInstance( int registrationInstanceId, RockContext rockContext = null )
        {
            var key = string.Format( "RegistrationInstance:{0}", registrationInstanceId );
            var registrationInstance = RockPage.GetSharedItem( key ) as RegistrationInstance;
            if ( registrationInstance == null )
            {
                rockContext = rockContext ?? new RockContext();
                registrationInstance = new RegistrationInstanceService( rockContext )
                    .Queryable( "RegistrationTemplate,Account,RegistrationTemplate.Forms.Fields" )
                    .AsNoTracking()
                    .FirstOrDefault( i => i.Id == registrationInstanceId );
                RockPage.SaveSharedItem( key, registrationInstance );
            }

            return registrationInstance;
        }

        public void ShowDetail( int itemId )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail()
        {
            var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsIntegerOrNull();
            var parentTemplateId = PageParameter( "RegistrationTemplateId" ).AsIntegerOrNull();

            if ( !RegistrationInstanceId.HasValue )
            {
                pnlDetails.Visible = false;
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                RegistrationInstance registrationInstance = null;
                if ( RegistrationInstanceId.HasValue )
                {
                    registrationInstance = GetRegistrationInstance( RegistrationInstanceId.Value, rockContext );
                }

                if ( registrationInstance == null )
                {
                    registrationInstance = new RegistrationInstance
                    {
                        Id = 0,
                        IsActive = true,
                        RegistrationTemplateId = parentTemplateId ?? 0
                    };

                    var accountGuid = GetAttributeValue( "DefaultAccount" ).AsGuidOrNull();
                    if ( accountGuid.HasValue )
                    {
                        var account = new FinancialAccountService( rockContext ).Get( accountGuid.Value );
                        registrationInstance.AccountId = account != null ? account.Id : 0;
                    }
                }

                if ( registrationInstance.RegistrationTemplate == null && registrationInstance.RegistrationTemplateId > 0 )
                {
                    registrationInstance.RegistrationTemplate = new RegistrationTemplateService( rockContext )
                        .Get( registrationInstance.RegistrationTemplateId );
                }

                hlType.Visible = registrationInstance.RegistrationTemplate != null;
                hlType.Text = registrationInstance.RegistrationTemplate != null ? registrationInstance.RegistrationTemplate.Name : string.Empty;

                lWizardTemplateName.Text = hlType.Text;

                pnlDetails.Visible = true;
                hfRegistrationInstanceId.Value = registrationInstance.Id.ToString();
                SetHasPayments( registrationInstance.Id, rockContext );

                FollowingsHelper.SetFollowing( registrationInstance, pnlFollowing, this.CurrentPerson );

                // render UI based on Authorized
                var readOnly = false;

                var canEdit = UserCanEdit ||
                    registrationInstance.IsAuthorized( Authorization.EDIT, CurrentPerson ) ||
                    registrationInstance.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );

                nbEditModeMessage.Text = string.Empty;

                // User must have 'Edit' rights to block, or 'Edit' or 'Administrate' rights to instance
                if ( !canEdit )
                {
                    readOnly = true;
                    nbEditModeMessage.Heading = "Information";
                    nbEditModeMessage.Text = EditModeMessage.NotAuthorizedToEdit( RegistrationInstance.FriendlyTypeName );
                }

                if ( readOnly )
                {
                    btnEdit.Visible = false;
                    btnDelete.Visible = false;
                    gRegistrations.Actions.ShowAdd = false;
                    gRegistrations.IsDeleteEnabled = false;
                    ShowReadonlyDetails( registrationInstance );
                }
                else
                {
                    btnEdit.Visible = true;
                    btnDelete.Visible = true;

                    if ( registrationInstance.Id > 0 )
                    {
                        ShowReadonlyDetails( registrationInstance );
                    }
                    else
                    {
                        ShowEditDetails( registrationInstance, rockContext );
                    }
                }

                // show send payment reminder link
                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "PaymentReminderPage" ) ) && ( ( registrationInstance.RegistrationTemplate.SetCostOnInstance.HasValue && registrationInstance.RegistrationTemplate.SetCostOnInstance == true && registrationInstance.Cost.HasValue && registrationInstance.Cost.Value > 0 ) || registrationInstance.RegistrationTemplate.Cost > 0 ) )
                {
                    btnSendPaymentReminder.Visible = true;
                }
                else
                {
                    btnSendPaymentReminder.Visible = false;
                }
                BindRegistrationsFilter();
                BindRegistrantsFilter( registrationInstance );
                BindLinkagesFilter();
                AddDynamicControls();
            }
        }

        /// <summary>
        /// Sets the following on postback.
        /// </summary>
        private void SetFollowingOnPostback()
        {
            var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsIntegerOrNull();
            if ( RegistrationInstanceId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var registrationInstance = GetRegistrationInstance( RegistrationInstanceId.Value, rockContext );
                    if ( registrationInstance != null )
                    {
                        FollowingsHelper.SetFollowing( registrationInstance, pnlFollowing, this.CurrentPerson );
                    }
                }
            }
        }

        /// <summary>
        /// Shows the edit details.
        /// </summary>
        /// <param name="RegistrationTemplate">The registration template.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="instance">The registration instance</param>
        private void ShowEditDetails( RegistrationInstance instance, RockContext rockContext )
        {
            if ( instance.Id == 0 )
            {
                lReadOnlyTitle.Text = ActionTitle.Add( RegistrationInstance.FriendlyTypeName ).FormatAsHtmlTitle();
                hlInactive.Visible = false;
                lWizardInstanceName.Text = "New Instance";
            }
            else
            {
                lWizardInstanceName.Text = instance.Name;
            }

            SetEditMode( true, instance );

            rieDetails.SetValue( instance );
        }

        /// <summary>
        /// Shows the readonly details.
        /// </summary>
        /// <param name="RegistrationInstance">The registration template.</param>
        private void ShowReadonlyDetails( RegistrationInstance RegistrationInstance )
        {
            SetEditMode( false, RegistrationInstance );

            hfRegistrationInstanceId.SetValue( RegistrationInstance.Id );

            lReadOnlyTitle.Text = RegistrationInstance.Name.FormatAsHtmlTitle();
            hlInactive.Visible = RegistrationInstance.IsActive == false;

            lWizardInstanceName.Text = RegistrationInstance.Name;
            lName.Text = RegistrationInstance.Name;

            if ( RegistrationInstance.RegistrationTemplate.SetCostOnInstance ?? false )
            {
                lCost.Text = RegistrationInstance.Cost.FormatAsCurrency();
                lMinimumInitialPayment.Visible = RegistrationInstance.MinimumInitialPayment.HasValue;
                lMinimumInitialPayment.Text = RegistrationInstance.MinimumInitialPayment.HasValue ? RegistrationInstance.MinimumInitialPayment.Value.FormatAsCurrency() : "";
            }
            else
            {
                lCost.Visible = false;
                lMinimumInitialPayment.Visible = false;
            }

            lAccount.Visible = RegistrationInstance.Account != null;
            lAccount.Text = RegistrationInstance.Account != null ? RegistrationInstance.Account.Name : "";

            lMaxAttendees.Visible = RegistrationInstance.MaxAttendees > 0;
            lMaxAttendees.Text = RegistrationInstance.MaxAttendees.ToString( "N0" );
            lWorkflowType.Text = RegistrationInstance.RegistrationWorkflowType != null ?
                RegistrationInstance.RegistrationWorkflowType.Name : string.Empty;
            lWorkflowType.Visible = !string.IsNullOrWhiteSpace( lWorkflowType.Text );

            lDetails.Visible = !string.IsNullOrWhiteSpace( RegistrationInstance.Details );
            lDetails.Text = RegistrationInstance.Details;

            liGroupPlacement.Visible = RegistrationInstance.RegistrationTemplate.AllowGroupPlacement;

            var groupId = GetUserPreference( string.Format( "ParentGroup_{0}_{1}", BlockId, RegistrationInstance.Id ) ).AsIntegerOrNull();
            if ( groupId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var group = new GroupService( rockContext ).Get( groupId.Value );
                    if ( group != null )
                    {
                        gpGroupPlacementParentGroup.SetValue( group );
                    }
                }
            }

            ShowTab();
        }

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        /// <param name="instance">The registration instance</param>
        private void SetEditMode( bool editable, RegistrationInstance instance )
        {
            pnlEditDetails.Visible = editable;
            fieldsetViewDetails.Visible = !editable;
            pnlTabs.Visible = !editable;
            SetGroupPanelRblValues( instance, true );
        }

        private void SetupGroupPanel( RegistrationInstance instance, bool setValues )
        {
            if ( !string.IsNullOrWhiteSpace( this.GetAttributeValue( "GroupTypeParentSetting" ) ) )
            {
                var rockContext = new RockContext();
                instance.LoadAttributes();
                foreach ( GroupType groupType in _associatedGroupsAvailable )
                {
                    var groupTypeRbl = new RockRadioButtonList
                    {
                        Label = groupType.Name,
                        RepeatDirection = RepeatDirection.Horizontal,
                        ID = string.Format( "rblGroupType_{0}", groupType.Id.ToString() )
                    };
                    groupTypeRbl.Items.Add( new ListItem( "Show on Grid", "2" ) );
                    groupTypeRbl.Items.Add( new ListItem( "Hide on Grid", "1" ) );
                    groupTypeRbl.Items.Add( new ListItem( "Not Used", "0" ) );
                    pnlAssociatedGroupTypes.Controls.Add( groupTypeRbl );
                }
            }
            pnlSubGroups.Visible = pnlAssociatedGroupTypes.Controls.Count > 0;
        }

        private void SetGroupPanelRblValues( RegistrationInstance instance, bool setValues)
        {
            foreach ( Control control in pnlAssociatedGroupTypes.Controls )
            {
                if ( control.GetType() == typeof( RockRadioButtonList ) )
                {
                    instance.LoadAttributes();
                    var rbl = ( RockRadioButtonList )control;
                    if ( setValues && !string.IsNullOrWhiteSpace( instance.AttributeValues[rbl.Label].Value ) )
                    {
                        var val = instance.GetAttributeValue( rbl.Label ).Split( '^' ).ToList();
                        if ( val[1] == "1" )
                        {
                            rbl.SelectedValue = "2";
                        }
                        else
                        {
                            rbl.SelectedValue = "1";
                        }
                    }
                    else
                    {
                        rbl.SelectedValue = "0";
                    }
                }
            }
        }

        private void BuildSubGroupTabs()
        {
            phGroupTabs.Controls.Clear();
            var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            var instance = new RegistrationInstanceService( new RockContext() ).Get( RegistrationInstanceId );
            instance.LoadAttributes();
            if ( AssociatedGroupTypes != null )
            {
                foreach ( GroupType groupType in AssociatedGroupTypes )
                {
                    var item = new HtmlGenericControl( "li" )
                    {
                        ID = "li" + groupType.Name
                    };
                    var lb = new LinkButton
                    {
                        ID = "lb" + groupType.Name,
                        Text = groupType.Name
                    };
                    lb.Click += lbTab_Click;
                    item.Controls.Add( lb );
                    phGroupTabs.Controls.Add( item );
                }
            }
        }

        /// <summary>
        /// Shows the tab.
        /// </summary>
        private void ShowTab( )
        {
            var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            var instance = new RegistrationInstanceService( new RockContext() ).Get( RegistrationInstanceId );
            instance.LoadAttributes();
            var groupService = new GroupService( new RockContext() );
            Group parentGroup;
            liRegistrations.RemoveCssClass( "active" );
            pnlRegistrations.Visible = false;

            liRegistrants.RemoveCssClass( "active" );
            pnlRegistrants.Visible = false;

            liPayments.RemoveCssClass( "active" );
            pnlPayments.Visible = false;

            liLinkage.RemoveCssClass( "active" );
            pnlLinkages.Visible = false;

            liGroupPlacement.RemoveCssClass( "active" );
            pnlGroupPlacement.Visible = false;

            HtmlGenericControl liAssociatedGroup;
            if ( AssociatedGroupTypes != null )
            {
                foreach ( GroupType groupType in AssociatedGroupTypes )
                {
                    liAssociatedGroup = new HtmlGenericControl();
                    liAssociatedGroup = ( HtmlGenericControl )ulTabs.FindControl( "li" + groupType.Name );
                    if ( liAssociatedGroup != null )
                    {
                        liAssociatedGroup.RemoveCssClass( "active" );
                    }
                    if ( ActiveTab == "lb" + groupType.Name )
                    {
                        liAssociatedGroup.AddCssClass( "active" );
                        parentGroup = groupService.Get( Guid.Parse( instance.AttributeValues[groupType.Name].Value.Split( '^' )[0] ) );
                        hfActiveTabParentGroup.Value = parentGroup.Guid.ToString();
                    }
                }
            }
            rpGroupPanels.DataSource = AssociatedGroupTypes;
            rpGroupPanels.DataBind();

            switch ( ActiveTab ?? string.Empty )
            {
                case "lbRegistrants":
                    {
                        liRegistrants.AddCssClass( "active" );
                        pnlRegistrants.Visible = true;
                        BindRegistrantsGrid();
                        break;
                    }

                case "lbPayments":
                    {
                        liPayments.AddCssClass( "active" );
                        pnlPayments.Visible = true;
                        BindPaymentsGrid();
                        break;
                    }

                case "lbLinkage":
                    {
                        liLinkage.AddCssClass( "active" );
                        pnlLinkages.Visible = true;
                        BindLinkagesGrid();
                        break;
                    }

                case "lbGroupPlacement":
                    {
                        liGroupPlacement.AddCssClass( "active" );
                        pnlGroupPlacement.Visible = true;
                        BindGroupPlacementGrid();
                        break;
                    }

                case "lbRegistrations":
                    {
                        liRegistrations.AddCssClass( "active" );
                        pnlRegistrations.Visible = true;
                        BindRegistrationsGrid();
                        break;
                    }

                case "":
                    goto case "lbRegistrations";
            }
        }

        /// <summary>
        /// Sets whether the registration has payments.
        /// </summary>
        /// <param name="registrationInstanceId">The registration instance identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        private void SetHasPayments( int registrationInstanceId, RockContext rockContext )
        {
            var registrationIdQry = new RegistrationService( rockContext )
                .Queryable().AsNoTracking()
                .Where( r =>
                    r.RegistrationInstanceId == registrationInstanceId &&
                    !r.IsTemporary )
                .Select( r => r.Id );

            var registrationEntityType = EntityTypeCache.Read( typeof( Rock.Model.Registration ) );
            hfHasPayments.Value = new FinancialTransactionDetailService( rockContext )
                .Queryable().AsNoTracking()
                .Any( d =>
                                    d.EntityTypeId.HasValue &&
                                    d.EntityId.HasValue &&
                                    d.EntityTypeId.Value == registrationEntityType.Id &&
                                    registrationIdQry.Contains( d.EntityId.Value ) )
                .ToString();
        }

        #endregion

        #region Registration Tab

        /// <summary>
        /// Binds the registrations filter.
        /// </summary>
        private void BindRegistrationsFilter()
        {
            drpRegistrationDateRange.DelimitedValues = fRegistrations.GetUserPreference( "Date Range" );
            ddlRegistrationPaymentStatus.SetValue( fRegistrations.GetUserPreference( "Payment Status" ) );
            tbRegistrationRegisteredByFirstName.Text = fRegistrations.GetUserPreference( "Registered By First Name" );
            tbRegistrationRegisteredByLastName.Text = fRegistrations.GetUserPreference( "Registered By Last Name" );
            tbRegistrationRegistrantFirstName.Text = fRegistrations.GetUserPreference( "Registrant First Name" );
            tbRegistrationRegistrantLastName.Text = fRegistrations.GetUserPreference( "Registrant Last Name" );
        }

        /// <summary>
        /// Binds the registrations grid.
        /// </summary>
        private void BindRegistrationsGrid()
        {
            var instanceId = hfRegistrationInstanceId.Value.AsIntegerOrNull();
            if ( instanceId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var registrationEntityType = EntityTypeCache.Read( typeof( Rock.Model.Registration ) );

                    var instance = new RegistrationInstanceService( rockContext ).Get( instanceId.Value );
                    var cost = instance.RegistrationTemplate.Cost;
                    if ( instance.RegistrationTemplate.SetCostOnInstance ?? false )
                    {
                        cost = instance.Cost ?? 0.0m;
                    }
                    _instanceHasCost = cost > 0.0m;

                    var qry = new RegistrationService( rockContext )
                        .Queryable( "PersonAlias.Person,Registrants.PersonAlias.Person,Registrants.Fees.RegistrationTemplateFee" )
                        .AsNoTracking()
                        .Where( r =>
                            r.RegistrationInstanceId == instanceId.Value &&
                            !r.IsTemporary );

                    if ( drpRegistrationDateRange.LowerValue.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value >= drpRegistrationDateRange.LowerValue.Value );
                    }
                    if ( drpRegistrationDateRange.UpperValue.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value <= drpRegistrationDateRange.UpperValue.Value );
                    }

                    if ( !string.IsNullOrWhiteSpace( tbRegistrationRegisteredByFirstName.Text ) )
                    {
                        var pfname = tbRegistrationRegisteredByFirstName.Text;
                        qry = qry.Where( r =>
                            r.FirstName.StartsWith( pfname ) ||
                            r.PersonAlias.Person.NickName.StartsWith( pfname ) ||
                            r.PersonAlias.Person.FirstName.StartsWith( pfname ) );
                    }

                    if ( !string.IsNullOrWhiteSpace( tbRegistrationRegisteredByLastName.Text ) )
                    {
                        var plname = tbRegistrationRegisteredByLastName.Text;
                        qry = qry.Where( r =>
                            r.LastName.StartsWith( plname ) ||
                            r.PersonAlias.Person.LastName.StartsWith( plname ) );
                    }

                    if ( !string.IsNullOrWhiteSpace( tbRegistrationRegistrantFirstName.Text ) )
                    {
                        var rfname = tbRegistrationRegistrantFirstName.Text;
                        qry = qry.Where( r =>
                            r.Registrants.Any( p =>
                                p.PersonAlias.Person.NickName.StartsWith( rfname ) ||
                                p.PersonAlias.Person.FirstName.StartsWith( rfname ) ) );
                    }

                    if ( !string.IsNullOrWhiteSpace( tbRegistrationRegistrantLastName.Text ) )
                    {
                        var rlname = tbRegistrationRegistrantLastName.Text;
                        qry = qry.Where( r =>
                            r.Registrants.Any( p =>
                                p.PersonAlias.Person.LastName.StartsWith( rlname ) ) );
                    }

                    // If filtering on payment status, need to do some sub-querying...
                    if ( ddlRegistrationPaymentStatus.SelectedValue != "" && registrationEntityType != null )
                    {
                        // Get all the registrant costs
                        var rCosts = new Dictionary<int, decimal>();
                        qry.ToList()
                            .Select( r => new
                            {
                                RegistrationId = r.Id,
                                DiscountCosts = r.Registrants.Sum( p => (decimal?)( p.DiscountedCost( r.DiscountPercentage, r.DiscountAmount) ) ) ?? 0.0m,
                            } ).ToList()
                            .ForEach( c =>
                                rCosts.AddOrReplace( c.RegistrationId, c.DiscountCosts ) );

                        var rPayments = new Dictionary<int, decimal>();
                        new FinancialTransactionDetailService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( d =>
                                d.EntityTypeId.HasValue &&
                                d.EntityId.HasValue &&
                                d.EntityTypeId.Value == registrationEntityType.Id &&
                                rCosts.Keys.Contains( d.EntityId.Value ) )
                            .Select( d => new
                            {
                                RegistrationId = d.EntityId.Value,
                                Payment = d.Amount
                            } )
                            .ToList()
                            .GroupBy( d => d.RegistrationId )
                            .Select( d => new
                            {
                                RegistrationId = d.Key,
                                Payments = d.Sum( p => p.Payment )
                            } )
                            .ToList()
                            .ForEach( p =>
                                rPayments.AddOrReplace( p.RegistrationId, p.Payments ) );

                        var rPmtSummary = rCosts
                            .Join( rPayments, c => c.Key, p => p.Key, ( c, p ) => new
                            {
                                RegistrationId = c.Key,
                                Costs = c.Value,
                                Payments = p.Value
                            } )
                            .ToList();

                        var ids = new List<int>();

                        if ( ddlRegistrationPaymentStatus.SelectedValue == "Paid in Full" )
                        {
                            ids = rPmtSummary
                                .Where( r => r.Costs <= r.Payments )
                                .Select( r => r.RegistrationId )
                                .ToList();
                        }
                        else
                        {
                            ids = rPmtSummary
                                .Where( r => r.Costs > r.Payments )
                                .Select( r => r.RegistrationId )
                                .ToList();
                        }

                        qry = qry.Where( r => ids.Contains( r.Id ) );
                    }

                    var sortProperty = gRegistrations.SortProperty;
                    if ( sortProperty != null )
                    {
                        // If sorting by Total Cost or Balance Due, the database query needs to be run first without ordering,
                        // and then ordering needs to be done in memory since TotalCost and BalanceDue are not databae fields.
                        if ( sortProperty.Property == "TotalCost" )
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                gRegistrations.SetLinqDataSource( qry.ToList().OrderBy( r => r.TotalCost ).AsQueryable() );
                            }
                            else
                            {
                                gRegistrations.SetLinqDataSource( qry.ToList().OrderByDescending( r => r.TotalCost ).AsQueryable() );
                            }
                        }
                        else if ( sortProperty.Property == "BalanceDue" )
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                gRegistrations.SetLinqDataSource( qry.ToList().OrderBy( r => r.BalanceDue ).AsQueryable() );
                            }
                            else
                            {
                                gRegistrations.SetLinqDataSource( qry.ToList().OrderByDescending( r => r.BalanceDue ).AsQueryable() );
                            }
                        }
                        else
                        {
                            gRegistrations.SetLinqDataSource( qry.Sort( sortProperty ) );
                        }
                    }
                    else
                    {
                        gRegistrations.SetLinqDataSource( qry.OrderByDescending( r => r.CreatedDateTime ) );
                    }

                    // Get all the payments for any registrations being displayed on the current page.
                    // This is used in the RowDataBound event but queried now so that each row does
                    // not have to query for the data.
                    var currentPageRegistrations = gRegistrations.DataSource as List<Registration>;
                    if ( currentPageRegistrations != null && registrationEntityType != null )
                    {
                        var registrationIds = currentPageRegistrations
                            .Select( r => r.Id )
                            .ToList();

                        RegistrationPayments = new FinancialTransactionDetailService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( d =>
                                d.EntityTypeId.HasValue &&
                                d.EntityId.HasValue &&
                                d.EntityTypeId.Value == registrationEntityType.Id &&
                                registrationIds.Contains( d.EntityId.Value ) )
                            .ToList();
                    }

                    var discountCodeHeader = gRegistrations.Columns.GetColumnByHeaderText( "Discount Code" );
                    if ( discountCodeHeader != null )
                    {
                        discountCodeHeader.Visible = GetAttributeValue( "DisplayDiscountCodes" ).AsBoolean();
                    }

                    gRegistrations.DataBind();
                }
            }
        }

        #endregion

        #region Registrants Tab

        /// <summary>
        /// Binds the registrants filter.
        /// </summary>
        /// <param name="instance">The registration instance.</param>
        private void BindRegistrantsFilter( RegistrationInstance instance )
        {
            drpRegistrantDateRange.DelimitedValues = fRegistrants.GetUserPreference( "Date Range" );
            tbRegistrantFirstName.Text = fRegistrants.GetUserPreference( "First Name" );
            tbRegistrantLastName.Text = fRegistrants.GetUserPreference( "Last Name" );
            ddlInGroup.SetValue( fRegistrants.GetUserPreference( "In Group" ) );

            ddlSignedDocument.SetValue( fRegistrants.GetUserPreference( "Signed Document" ) );
            ddlSignedDocument.Visible = instance != null && instance.RegistrationTemplate != null && instance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue;
        }

        /// <summary>
        /// Binds the registrants grid.
        /// </summary>
        /// <param name="isExporting">if set to <c>true</c> [isExporting].</param>
        private void BindRegistrantsGrid( bool isExporting = false )
        {
            var instanceId = hfRegistrationInstanceId.Value.AsIntegerOrNull();
            if ( instanceId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var registrationInstance = new RegistrationInstanceService( rockContext ).Get( instanceId.Value );

                    if ( registrationInstance != null &&
                        registrationInstance.RegistrationTemplate != null &&
                        registrationInstance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue )
                    {
                        Signers = new SignatureDocumentService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( d =>
                                d.SignatureDocumentTemplateId == registrationInstance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.Value &&
                                d.Status == SignatureDocumentStatus.Signed &&
                                d.BinaryFileId.HasValue &&
                                d.AppliesToPersonAlias != null )
                            .OrderByDescending( d => d.LastStatusDate )
                            .Select( d => d.AppliesToPersonAlias.PersonId )
                            .ToList();
                    }

                    // Start query for registrants
                    var qry = new RegistrationRegistrantService( rockContext )
                    .Queryable( "PersonAlias.Person.PhoneNumbers.NumberTypeValue,Fees.RegistrationTemplateFee,GroupMember.Group" ).AsNoTracking()
                    .Where( r =>
                        r.Registration.RegistrationInstanceId == instanceId.Value &&
                        r.PersonAlias != null &&
                        r.PersonAlias.Person != null );

                    // Filter by daterange
                    if ( drpRegistrantDateRange.LowerValue.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value >= drpRegistrantDateRange.LowerValue.Value );
                    }
                    if ( drpRegistrantDateRange.UpperValue.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value <= drpRegistrantDateRange.UpperValue.Value );
                    }

                    // Filter by first name
                    if ( !string.IsNullOrWhiteSpace( tbRegistrantFirstName.Text ) )
                    {
                        var rfname = tbRegistrantFirstName.Text;
                        qry = qry.Where( r =>
                            r.PersonAlias.Person.NickName.StartsWith( rfname ) ||
                            r.PersonAlias.Person.FirstName.StartsWith( rfname ) );
                    }

                    // Filter by last name
                    if ( !string.IsNullOrWhiteSpace( tbRegistrantLastName.Text ) )
                    {
                        var rlname = tbRegistrantLastName.Text;
                        qry = qry.Where( r =>
                            r.PersonAlias.Person.LastName.StartsWith( rlname ) );
                    }

                    // Filter by signed documents
                    if ( Signers != null )
                    {
                        if ( ddlSignedDocument.SelectedValue.AsBooleanOrNull() == true )
                        {
                            qry = qry.Where( r => Signers.Contains( r.PersonAlias.PersonId ) );
                        }
                        else if ( ddlSignedDocument.SelectedValue.AsBooleanOrNull() == false )
                        {
                            qry = qry.Where( r => !Signers.Contains( r.PersonAlias.PersonId ) );
                        }
                    }

                    if ( ddlInGroup.SelectedValue.AsBooleanOrNull() == true )
                    {
                        qry = qry.Where( r => r.GroupMemberId.HasValue );
                    }
                    else if ( ddlInGroup.SelectedValue.AsBooleanOrNull() == false )
                    {
                        qry = qry.Where( r => !r.GroupMemberId.HasValue );
                    }

                    var preloadCampusValues = false;
                    var registrantAttributes = new List<AttributeCache>();
                    var personAttributes = new List<AttributeCache>();
                    var groupMemberAttributes = new List<AttributeCache>();
                    var registrantAttributeIds = new List<int>();
                    var personAttributesIds = new List<int>();
                    var groupMemberAttributesIds = new List<int>();

                    if ( isExporting )
                    {
                        // get list of home addresses
                        var personIds = qry.Select( r => r.PersonAlias.PersonId ).ToList();
                        _homeAddresses = Person.GetHomeLocations( personIds );
                    }

                    if ( RegistrantFields != null )
                    {
                        // Filter by any selected
                        foreach ( var personFieldType in RegistrantFields
                            .Where( f =>
                                f.FieldSource == RegistrationFieldSource.PersonField &&
                                f.PersonFieldType.HasValue )
                            .Select( f => f.PersonFieldType.Value ) )
                        {
                            switch ( personFieldType )
                            {
                                case RegistrationPersonFieldType.Campus:
                                    {
                                        preloadCampusValues = true;

                                        var ddlCampus = phRegistrantFormFieldFilters.FindControl( "ddlCampus" ) as RockDropDownList;
                                        if ( ddlCampus != null )
                                        {
                                            var campusId = ddlCampus.SelectedValue.AsIntegerOrNull();
                                            if ( campusId.HasValue )
                                            {
                                                var familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid();
                                                qry = qry.Where( r =>
                                                    r.PersonAlias.Person.Members.Any( m =>
                                                        m.Group.GroupType.Guid == familyGroupTypeGuid &&
                                                        m.Group.CampusId.HasValue &&
                                                        m.Group.CampusId.Value == campusId ) );
                                            }
                                        }

                                        break;
                                    }

                                case RegistrationPersonFieldType.Email:
                                    {
                                        var tbEmailFilter = phRegistrantFormFieldFilters.FindControl( "tbEmailFilter" ) as RockTextBox;
                                        if ( tbEmailFilter != null && !string.IsNullOrWhiteSpace( tbEmailFilter.Text ) )
                                        {
                                            qry = qry.Where( r =>
                                                r.PersonAlias.Person.Email != null &&
                                                r.PersonAlias.Person.Email.Contains( tbEmailFilter.Text ) );
                                        }

                                        break;
                                    }

                                case RegistrationPersonFieldType.Birthdate:
                                    {
                                        var drpBirthdateFilter = phRegistrantFormFieldFilters.FindControl( "drpBirthdateFilter" ) as DateRangePicker;
                                        if ( drpBirthdateFilter != null )
                                        {
                                            if ( drpBirthdateFilter.LowerValue.HasValue )
                                            {
                                                qry = qry.Where( r =>
                                                    r.PersonAlias.Person.BirthDate.HasValue &&
                                                    r.PersonAlias.Person.BirthDate.Value >= drpBirthdateFilter.LowerValue.Value );
                                            }
                                            if ( drpBirthdateFilter.UpperValue.HasValue )
                                            {
                                                qry = qry.Where( r =>
                                                    r.PersonAlias.Person.BirthDate.HasValue &&
                                                    r.PersonAlias.Person.BirthDate.Value <= drpBirthdateFilter.UpperValue.Value );
                                            }
                                        }
                                        break;
                                    }

                                case RegistrationPersonFieldType.Grade:
                                    {
                                        var gpGradeFilter = phRegistrantFormFieldFilters.FindControl( "gpGradeFilter" ) as GradePicker;
                                        if ( gpGradeFilter != null )
                                        {
                                            var graduationYear = Person.GraduationYearFromGradeOffset( gpGradeFilter.SelectedValueAsInt( false ) );
                                            if ( graduationYear.HasValue )
                                            {
                                                qry = qry.Where( r =>
                                                    r.PersonAlias.Person.GraduationYear.HasValue &&
                                                    r.PersonAlias.Person.GraduationYear == graduationYear.Value );
                                            }
                                        }
                                        break;
                                    }

                                case RegistrationPersonFieldType.Gender:
                                    {
                                        var ddlGenderFilter = phRegistrantFormFieldFilters.FindControl( "ddlGenderFilter" ) as RockDropDownList;
                                        if ( ddlGenderFilter != null )
                                        {
                                            var gender = ddlGenderFilter.SelectedValue.ConvertToEnumOrNull<Gender>();
                                            if ( gender.HasValue )
                                            {
                                                qry = qry.Where( r =>
                                                    r.PersonAlias.Person.Gender == gender );
                                            }
                                        }

                                        break;
                                    }

                                case RegistrationPersonFieldType.MaritalStatus:
                                    {
                                        var ddlMaritalStatusFilter = phRegistrantFormFieldFilters.FindControl( "ddlMaritalStatusFilter" ) as RockDropDownList;
                                        if ( ddlMaritalStatusFilter != null )
                                        {
                                            var maritalStatusId = ddlMaritalStatusFilter.SelectedValue.AsIntegerOrNull();
                                            if ( maritalStatusId.HasValue )
                                            {
                                                qry = qry.Where( r =>
                                                    r.PersonAlias.Person.MaritalStatusValueId.HasValue &&
                                                    r.PersonAlias.Person.MaritalStatusValueId.Value == maritalStatusId.Value );
                                            }
                                        }

                                        break;
                                    }
                                case RegistrationPersonFieldType.MobilePhone:
                                    {
                                        var tbPhoneFilter = phRegistrantFormFieldFilters.FindControl( "tbPhoneFilter" ) as RockTextBox;
                                        if ( tbPhoneFilter != null && !string.IsNullOrWhiteSpace( tbPhoneFilter.Text ) )
                                        {
                                            var numericPhone = tbPhoneFilter.Text.AsNumeric();

                                            qry = qry.Where( r =>
                                                r.PersonAlias.Person.PhoneNumbers != null &&
                                                r.PersonAlias.Person.PhoneNumbers.Any( n => n.Number.Contains( numericPhone ) ) );
                                        }

                                        break;
                                    }
                            }
                        }

                        // Get all the registrant attributes selected to be on grid
                        registrantAttributes = RegistrantFields
                            .Where( f =>
                                f.Attribute != null &&
                                f.FieldSource == RegistrationFieldSource.RegistrationAttribute )
                            .Select( f => f.Attribute )
                            .ToList();
                        registrantAttributeIds = registrantAttributes.Select( a => a.Id ).Distinct().ToList();

                        // Filter query by any configured registrant attribute filters
                        if ( registrantAttributes != null && registrantAttributes.Any() )
                        {
                            var attributeValueService = new AttributeValueService( rockContext );
                            var parameterExpression = attributeValueService.ParameterExpression;
                            foreach ( var attribute in registrantAttributes )
                            {
                                var filterControl = phRegistrantFormFieldFilters.FindControl( "filter_" + attribute.Id.ToString() );
                                if ( filterControl != null )
                                {
                                    var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                    var expression = attribute.FieldType.Field.AttributeFilterExpression( attribute.QualifierValues, filterValues, parameterExpression );
                                    if ( expression != null )
                                    {
                                        var attributeValues = attributeValueService
                                            .Queryable()
                                            .Where( v => v.Attribute.Id == attribute.Id );
                                        attributeValues = attributeValues.Where( parameterExpression, expression, null );
                                        qry = qry
                                            .Where( r => attributeValues.Select( v => v.EntityId ).Contains( r.Id ) );
                                    }
                                }
                            }
                        }

                        // Get all the person attributes selected to be on grid
                        personAttributes = RegistrantFields
                            .Where( f =>
                                f.Attribute != null &&
                                f.FieldSource == RegistrationFieldSource.PersonAttribute )
                            .Select( f => f.Attribute )
                            .ToList();
                        personAttributesIds = personAttributes.Select( a => a.Id ).Distinct().ToList();

                        // Filter query by any configured person attribute filters
                        if ( personAttributes != null && personAttributes.Any() )
                        {
                            var attributeValueService = new AttributeValueService( rockContext );
                            var parameterExpression = attributeValueService.ParameterExpression;
                            foreach ( var attribute in personAttributes )
                            {
                                var filterControl = phRegistrantFormFieldFilters.FindControl( "filter_" + attribute.Id.ToString() );
                                if ( filterControl != null )
                                {
                                    var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                    var expression = attribute.FieldType.Field.AttributeFilterExpression( attribute.QualifierValues, filterValues, parameterExpression );
                                    if ( expression != null )
                                    {
                                        var attributeValues = attributeValueService
                                            .Queryable()
                                            .Where( v => v.Attribute.Id == attribute.Id );
                                        attributeValues = attributeValues.Where( parameterExpression, expression, null );
                                        qry = qry
                                            .Where( r => attributeValues.Select( v => v.EntityId ).Contains( r.PersonAlias.PersonId ) );
                                    }
                                }
                            }
                        }

                        // Get all the group member attributes selected to be on grid
                        groupMemberAttributes = RegistrantFields
                            .Where( f =>
                                f.Attribute != null &&
                                f.FieldSource == RegistrationFieldSource.GroupMemberAttribute )
                            .Select( f => f.Attribute )
                            .ToList();
                        groupMemberAttributesIds = groupMemberAttributes.Select( a => a.Id ).Distinct().ToList();

                        // Filter query by any configured person attribute filters
                        if ( groupMemberAttributes != null && groupMemberAttributes.Any() )
                        {
                            var attributeValueService = new AttributeValueService( rockContext );
                            var parameterExpression = attributeValueService.ParameterExpression;
                            foreach ( var attribute in groupMemberAttributes )
                            {
                                var filterControl = phRegistrantFormFieldFilters.FindControl( "filter_" + attribute.Id.ToString() );
                                if ( filterControl != null )
                                {
                                    var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                    var expression = attribute.FieldType.Field.AttributeFilterExpression( attribute.QualifierValues, filterValues, parameterExpression );
                                    if ( expression != null )
                                    {
                                        var attributeValues = attributeValueService
                                            .Queryable()
                                            .Where( v => v.Attribute.Id == attribute.Id );
                                        attributeValues = attributeValues.Where( parameterExpression, expression, null );
                                        qry = qry
                                            .Where( r => r.GroupMemberId.HasValue &&
                                            attributeValues.Select( v => v.EntityId ).Contains( r.GroupMemberId.Value ) );
                                    }
                                }
                            }
                        }
                    }

                    // Sort the query
                    IOrderedQueryable<RegistrationRegistrant> orderedQry = null;
                    var sortProperty = gRegistrants.SortProperty;
                    if ( sortProperty != null )
                    {
                        orderedQry = qry.Sort( sortProperty );
                    }
                    else
                    {
                        orderedQry = qry
                            .OrderBy( r => r.PersonAlias.Person.LastName )
                            .ThenBy( r => r.PersonAlias.Person.NickName );
                    }

                    // increase the timeout just in case. A complex filter on the grid might slow things down
                    rockContext.Database.CommandTimeout = 180;

                    // Set the grids LinqDataSource which will run query and set results for current page
                    gRegistrants.SetLinqDataSource<RegistrationRegistrant>( orderedQry );

                    if ( RegistrantFields != null )
                    {
                        // Get the query results for the current page
                        var currentPageRegistrants = gRegistrants.DataSource as List<RegistrationRegistrant>;
                        if ( currentPageRegistrants != null )
                        {
                            // Get all the registrant ids in current page of query results
                            var registrantIds = currentPageRegistrants
                                .Select( r => r.Id )
                                .Distinct()
                                .ToList();

                            // Get all the person ids in current page of query results
                            var personIds = currentPageRegistrants
                                .Select( r => r.PersonAlias.PersonId )
                                .Distinct()
                                .ToList();

                            // Get all the group member ids and the group id in current page of query results
                            var groupMemberIds = new List<int>();
                            GroupLinks = new Dictionary<int, string>();
                            foreach ( var groupMember in currentPageRegistrants
                                .Where( m =>
                                    m.GroupMember != null &&
                                    m.GroupMember.Group != null )
                                .Select( m => m.GroupMember ) )
                            {
                                groupMemberIds.Add( groupMember.Id );
                                GroupLinks.AddOrIgnore( groupMember.GroupId,
                                    isExporting ? groupMember.Group.Name :
                                        string.Format( "<a href='{0}'>{1}</a>",
                                            LinkedPageUrl( "GroupDetailPage", new Dictionary<string, string> { { "GroupId", groupMember.GroupId.ToString() } } ),
                                            groupMember.Group.Name ) );
                            }

                            // If the campus column was selected to be displayed on grid, preload all the people's
                            // campuses so that the databind does not need to query each row
                            if ( preloadCampusValues )
                            {
                                PersonCampusIds = new Dictionary<int, List<int>>();

                                var familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid();
                                foreach ( var personCampusList in new GroupMemberService( rockContext )
                                    .Queryable().AsNoTracking()
                                    .Where( m =>
                                        m.Group.GroupType.Guid == familyGroupTypeGuid &&
                                        personIds.Contains( m.PersonId ) )
                                    .GroupBy( m => m.PersonId )
                                    .Select( m => new
                                    {
                                        PersonId = m.Key,
                                        CampusIds = m
                                            .Where( g => g.Group.CampusId.HasValue )
                                            .Select( g => g.Group.CampusId.Value )
                                            .ToList()
                                    } ) )
                                {
                                    PersonCampusIds.Add( personCampusList.PersonId, personCampusList.CampusIds );
                                }
                            }

                            // If there are any attributes that were selected to be displayed, we're going
                            // to try and read all attribute values in one query and then put them into a
                            // custom grid ObjectList property so that the AttributeField columns don't need
                            // to do the LoadAttributes and querying of values for each row/column
                            if ( personAttributesIds.Any() || groupMemberAttributesIds.Any() || registrantAttributeIds.Any() )
                            {
                                // Query the attribute values for all rows and attributes
                                var attributeValues = new AttributeValueService( rockContext )
                                    .Queryable( "Attribute" ).AsNoTracking()
                                    .Where( v =>
                                        v.EntityId.HasValue &&
                                        (
                                            (
                                                personAttributesIds.Contains( v.AttributeId ) &&
                                                personIds.Contains( v.EntityId.Value )
                                            ) ||
                                            (
                                                groupMemberAttributesIds.Contains( v.AttributeId ) &&
                                                groupMemberIds.Contains( v.EntityId.Value )
                                            ) ||
                                            (
                                                registrantAttributeIds.Contains( v.AttributeId ) &&
                                                registrantIds.Contains( v.EntityId.Value )
                                            )
                                        )
                                    )
                                    .ToList();

                                // Get the attributes to add to each row's object
                                var attributes = new Dictionary<string, AttributeCache>();
                                RegistrantFields
                                        .Where( f => f.Attribute != null )
                                        .Select( f => f.Attribute )
                                        .ToList()
                                    .ForEach( a => attributes
                                        .Add( a.Id.ToString() + a.Key, a ) );

                                // Initialize the grid's object list
                                gRegistrants.ObjectList = new Dictionary<string, object>();

                                // Loop through each of the current page's registrants and build an attribute
                                // field object for storing attributes and the values for each of the registrants
                                foreach ( var registrant in currentPageRegistrants )
                                {
                                    // Create a row attribute object
                                    var attributeFieldObject = new AttributeFieldObject
                                    {

                                        // Add the attributes to the attribute object
                                        Attributes = attributes
                                    };

                                    // Add any person attribute values to object
                                    attributeValues
                                        .Where( v =>
                                            personAttributesIds.Contains( v.AttributeId ) &&
                                            v.EntityId.Value == registrant.PersonAlias.PersonId )
                                        .ToList()
                                        .ForEach( v => attributeFieldObject.AttributeValues
                                            .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );

                                    // Add any group member attribute values to object
                                    if ( registrant.GroupMemberId.HasValue )
                                    {
                                        attributeValues
                                            .Where( v =>
                                                groupMemberAttributesIds.Contains( v.AttributeId ) &&
                                                v.EntityId.Value == registrant.GroupMemberId.Value )
                                            .ToList()
                                            .ForEach( v => attributeFieldObject.AttributeValues
                                                .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );
                                    }

                                    // Add any registrant attribute values to object
                                    attributeValues
                                        .Where( v =>
                                            registrantAttributeIds.Contains( v.AttributeId ) &&
                                            v.EntityId.Value == registrant.Id )
                                        .ToList()
                                        .ForEach( v => attributeFieldObject.AttributeValues
                                            .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );

                                    // Add row attribute object to grid's object list
                                    gRegistrants.ObjectList.Add( registrant.Id.ToString(), attributeFieldObject );
                                }
                            }
                        }
                    }

                    gRegistrants.DataBind();
                }
            }
        }

        /// <summary>
        /// Gets all of the form fields that were configured as 'Show on Grid' for the registration template
        /// </summary>
        /// <param name="registrationInstance">The registration instance.</param>
        private void LoadRegistrantFormFields( RegistrationInstance registrationInstance )
        {
            RegistrantFields = new List<RegistrantFormField>();

            if ( registrationInstance != null )
            {
                foreach ( var form in registrationInstance.RegistrationTemplate.Forms )
                {
                    foreach ( var formField in form.Fields
                        .Where( f => f.IsGridField )
                        .OrderBy( f => f.Order ) )
                    {
                        if ( formField.FieldSource == RegistrationFieldSource.PersonField )
                        {
                            if ( formField.PersonFieldType != RegistrationPersonFieldType.FirstName &&
                                formField.PersonFieldType != RegistrationPersonFieldType.LastName )
                            {
                                RegistrantFields.Add(
                                    new RegistrantFormField
                                    {
                                        FieldSource = formField.FieldSource,
                                        PersonFieldType = formField.PersonFieldType
                                    } );
                            }
                        }
                        else
                        {
                            RegistrantFields.Add(
                                new RegistrantFormField
                                {
                                    FieldSource = formField.FieldSource,
                                    Attribute = AttributeCache.Read( formField.AttributeId.Value )
                                } );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets all of the group types that are associated with the registration template, as well as those marked as 'Show on Grid'.
        /// </summary>
        /// <param name="registrationInstance">The registration instance.</param>
        private void LoadAssociatedGroupTypes( RegistrationInstance registrationInstance )
        {
            AssociatedGroupTypes = new List<GroupType>();
            AssociatedGroupTypesGrid = new List<GroupType>();

            if ( registrationInstance != null )
            {
                var rockContext = new RockContext();
                var groupTypeService = new GroupTypeService( rockContext );
                if ( !string.IsNullOrWhiteSpace( this.GetAttributeValue( "GroupTypeParentSetting" ) ) )
                {
                    if ( !string.IsNullOrWhiteSpace( this.GetAttributeValue( "GroupTypesSetting" ) ) )
                    {
                        foreach ( string groupGuid in this.GetAttributeValue( "GroupTypesSetting" ).Split( ',' ) )
                        {
                            Guid guid;
                            if ( Guid.TryParse( groupGuid, out guid ) )
                            {
                                _associatedGroupsAvailable.Add( groupTypeService.Get( guid ) );
                            }
                        }
                    }
                    VerifyEntityAttribute( rockContext, _attributeKeyParent, _attributeNameParent, registrationInstance );
                    registrationInstance.LoadAttributes();

                    if ( _associatedGroupsAvailable.Count > 0 )
                    {
                        foreach ( GroupType groupType in _associatedGroupsAvailable )
                        {
                            VerifyEntityAttribute( rockContext, groupType.Name, groupType.Name, registrationInstance );
                            registrationInstance.LoadAttributes();
                            if ( !string.IsNullOrWhiteSpace( registrationInstance.AttributeValues[groupType.Name].Value ) )
                            {
                                AssociatedGroupTypes.Add( groupType );
                                var attributeValSplit = registrationInstance.AttributeValues[groupType.Name].Value.Split( '^' ).ToList();
                                if ( attributeValSplit != null && attributeValSplit[1] == "1" )
                                {
                                    AssociatedGroupTypesGrid.Add( groupType );
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the filter controls and grid columns for all of the registration template's form fields
        /// that were configured to 'Show on Grid'
        /// </summary>
        private void AddDynamicControls()
        {
            phRegistrantFormFieldFilters.Controls.Clear();
            _registrantGridColumnCount = gRegistrants.Columns.Count;

            // Remove the registrant field
            foreach ( var column in gRegistrants.Columns
                .OfType<TemplateField>()
                .Where( c => c.HeaderText == "Registrant" )
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
                _registrantGridColumnCount--;
            }

            // Remove any of the dynamic person fields
            var dynamicColumns = new List<string> {
                "PersonAlias.Person.BirthDate"
            };
            foreach ( var column in gRegistrants.Columns
                .OfType<BoundField>()
                .Where( c => dynamicColumns.Contains( c.DataField ) )
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
                _registrantGridColumnCount--;
            }

            // Remove any of the dynamic attribute fields
            foreach ( var column in gRegistrants.Columns
                .OfType<AttributeField>()
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
                _registrantGridColumnCount--;
            }

            // Remove the fees field
            foreach ( var column in gRegistrants.Columns
                .OfType<TemplateField>()
                .Where( c => c.HeaderText == "Fees" )
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
                _registrantGridColumnCount--;
            }

            // Remove the delete field
            foreach ( var column in gRegistrants.Columns
                .OfType<DeleteField>()
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
                _registrantGridColumnCount--;
            }

            // Remove Sub Group fields
            foreach ( var column in gRegistrants.Columns
                .OfType<LinkButtonField>()
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
                _registrantGridColumnCount--;
            }

            // Remove any of the dynamic attribute fields on group placements grid
            foreach ( var column in gGroupPlacements.Columns
                .OfType<AttributeField>()
                .ToList() )
            {
                gGroupPlacements.Columns.Remove( column );
            }

            // Remove the delete field
            foreach ( var column in gRegistrants.Columns
                .OfType<GroupPickerField>()
                .ToList() )
            {
                gGroupPlacements.Columns.Remove( column );
            }

            // Add registrant column
            var registrantField = new RockLiteralField
            {
                ID = "lRegistrant",
                HeaderText = "Registrant",
                SortExpression = "PersonAlias.Person.LastName, PersonAlias.Person.NickName",
                ExcelExportBehavior = ExcelExportBehavior.NeverInclude
            };
            gRegistrants.Columns.Insert( 1, registrantField );
            _registrantGridColumnCount++;

            if ( RegistrantFields != null )
            {
                foreach ( var field in RegistrantFields )
                {
                    if ( field.FieldSource == RegistrationFieldSource.PersonField && field.PersonFieldType.HasValue )
                    {
                        switch ( field.PersonFieldType.Value )
                        {
                            case RegistrationPersonFieldType.Campus:
                                {
                                    var ddlCampus = new RockDropDownList
                                    {
                                        ID = "ddlCampus",
                                        Label = "Home Campus",
                                        DataValueField = "Id",
                                        DataTextField = "Name",
                                        DataSource = CampusCache.All()
                                    };
                                    ddlCampus.DataBind();
                                    ddlCampus.Items.Insert( 0, new ListItem( "", "" ) );
                                    ddlCampus.SetValue( fRegistrants.GetUserPreference( "Home Campus" ) );
                                    phRegistrantFormFieldFilters.Controls.Add( ddlCampus );

                                    var templateField = new RockLiteralField
                                    {
                                        ID = "lCampus",
                                        HeaderText = "Campus"
                                    };
                                    gRegistrants.Columns.Add( templateField );
                                    _registrantGridColumnCount++;

                                    var templateField2 = new RockLiteralField
                                    {
                                        ID = "lCampus",
                                        HeaderText = "Campus"
                                    };
                                    gGroupPlacements.Columns.Add( templateField2 );

                                    break;
                                }

                            case RegistrationPersonFieldType.Email:
                                {
                                    var tbEmailFilter = new RockTextBox
                                    {
                                        ID = "tbEmailFilter",
                                        Label = "Email",
                                        Text = fRegistrants.GetUserPreference( "Email" )
                                    };
                                    phRegistrantFormFieldFilters.Controls.Add( tbEmailFilter );

                                    var dataFieldExpression = "PersonAlias.Person.Email";
                                    var emailField = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Email",
                                        SortExpression = dataFieldExpression
                                    };
                                    gRegistrants.Columns.Add( emailField );
                                    _registrantGridColumnCount++;

                                    var emailField2 = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Email",
                                        SortExpression = dataFieldExpression
                                    };
                                    gGroupPlacements.Columns.Add( emailField2 );

                                    break;
                                }

                            case RegistrationPersonFieldType.Birthdate:
                                {
                                    var drpBirthdateFilter = new DateRangePicker
                                    {
                                        ID = "drpBirthdateFilter",
                                        Label = "Birthdate Range",
                                        DelimitedValues = fRegistrants.GetUserPreference( "Birthdate Range" )
                                    };
                                    phRegistrantFormFieldFilters.Controls.Add( drpBirthdateFilter );

                                    var dataFieldExpression = "PersonAlias.Person.BirthDate";
                                    var birthdateField = new DateField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Birthdate",
                                        SortExpression = dataFieldExpression
                                    };
                                    gRegistrants.Columns.Add( birthdateField );
                                    _registrantGridColumnCount++;

                                    var birthdateField2 = new DateField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Birthdate",
                                        SortExpression = dataFieldExpression
                                    };
                                    gGroupPlacements.Columns.Add( birthdateField2 );

                                    break;
                                }

                            case RegistrationPersonFieldType.Grade:
                                {
                                    var gpGradeFilter = new GradePicker
                                    {
                                        ID = "gpGradeFilter",
                                        Label = "Grade",
                                        UseAbbreviation = true,
                                        UseGradeOffsetAsValue = true,
                                        CssClass = "input-width-md"
                                    };
                                    gpGradeFilter.SetValue( fRegistrants.GetUserPreference( "Grade" ).AsIntegerOrNull() );
                                    phRegistrantFormFieldFilters.Controls.Add( gpGradeFilter );

                                    var dataFieldExpression = "PersonAlias.Person.GraduationYear";
                                    var gradeField = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Graduation Year",
                                        SortExpression = dataFieldExpression
                                    };
                                    gRegistrants.Columns.Add( gradeField );
                                    _registrantGridColumnCount++;

                                    var gradeField2 = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Graduation Year",
                                        SortExpression = dataFieldExpression
                                    };
                                    gGroupPlacements.Columns.Add( gradeField2 );

                                    break;
                                }

                            case RegistrationPersonFieldType.Gender:
                                {
                                    var ddlGenderFilter = new RockDropDownList();
                                    ddlGenderFilter.BindToEnum<Gender>( true );
                                    ddlGenderFilter.ID = "ddlGenderFilter";
                                    ddlGenderFilter.Label = "Gender";
                                    ddlGenderFilter.SetValue( fRegistrants.GetUserPreference( "Gender" ) );
                                    phRegistrantFormFieldFilters.Controls.Add( ddlGenderFilter );

                                    var dataFieldExpression = "PersonAlias.Person.Gender";
                                    var genderField = new EnumField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Gender",
                                        SortExpression = dataFieldExpression
                                    };
                                    gRegistrants.Columns.Add( genderField );
                                    _registrantGridColumnCount++;

                                    var genderField2 = new EnumField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Gender",
                                        SortExpression = dataFieldExpression
                                    };
                                    gGroupPlacements.Columns.Add( genderField2 );
                                    break;
                                }

                            case RegistrationPersonFieldType.MaritalStatus:
                                {
                                    var ddlMaritalStatusFilter = new RockDropDownList();
                                    ddlMaritalStatusFilter.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ), true );
                                    ddlMaritalStatusFilter.ID = "ddlMaritalStatusFilter";
                                    ddlMaritalStatusFilter.Label = "Marital Status";
                                    ddlMaritalStatusFilter.SetValue( fRegistrants.GetUserPreference( "Marital Status" ) );
                                    phRegistrantFormFieldFilters.Controls.Add( ddlMaritalStatusFilter );

                                    var dataFieldExpression = "PersonAlias.Person.MaritalStatusValue.Value";
                                    var maritalStatusField = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "MaritalStatus",
                                        SortExpression = dataFieldExpression
                                    };
                                    gRegistrants.Columns.Add( maritalStatusField );
                                    _registrantGridColumnCount++;

                                    var maritalStatusField2 = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "MaritalStatus",
                                        SortExpression = dataFieldExpression
                                    };
                                    gGroupPlacements.Columns.Add( maritalStatusField2 );

                                    break;
                                }

                            case RegistrationPersonFieldType.MobilePhone:
                                {
                                    var tbPhoneFilter = new RockTextBox
                                    {
                                        ID = "tbPhoneFilter",
                                        Label = "Phone",
                                        Text = fRegistrants.GetUserPreference( "Phone" )
                                    };
                                    phRegistrantFormFieldFilters.Controls.Add( tbPhoneFilter );

                                    var phoneNumbersField = new PhoneNumbersField
                                    {
                                        DataField = "PersonAlias.Person.PhoneNumbers",
                                        HeaderText = "Phone(s)"
                                    };
                                    gRegistrants.Columns.Add( phoneNumbersField );
                                    _registrantGridColumnCount++;

                                    var phoneNumbersField2 = new PhoneNumbersField
                                    {
                                        DataField = "PersonAlias.Person.PhoneNumbers",
                                        HeaderText = "Phone(s)"
                                    };
                                    gGroupPlacements.Columns.Add( phoneNumbersField2 );

                                    break;
                                }
                        }
                    }
                    else if ( field.Attribute != null )
                    {
                        var attribute = field.Attribute;
                        var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                        if ( control != null )
                        {
                            if ( control is IRockControl )
                            {
                                var rockControl = ( IRockControl )control;
                                rockControl.Label = attribute.Name;
                                rockControl.Help = attribute.Description;
                                phRegistrantFormFieldFilters.Controls.Add( control );
                            }
                            else
                            {
                                var wrapper = new RockControlWrapper
                                {
                                    ID = control.ID + "_wrapper",
                                    Label = attribute.Name
                                };
                                wrapper.Controls.Add( control );
                                phRegistrantFormFieldFilters.Controls.Add( wrapper );
                            }

                            var savedValue = fRegistrants.GetUserPreference( attribute.Key );
                            if ( !string.IsNullOrWhiteSpace( savedValue ) )
                            {
                                try
                                {
                                    var values = JsonConvert.DeserializeObject<List<string>>( savedValue );
                                    attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, values );
                                }
                                catch { }
                            }
                        }

                        var dataFieldExpression = attribute.Id.ToString() + attribute.Key;
                        var columnExists = gRegistrants.Columns.OfType<AttributeField>().FirstOrDefault( a => a.DataField.Equals( dataFieldExpression ) ) != null;
                        if ( !columnExists )
                        {
                            var boundField = new AttributeField
                            {
                                DataField = dataFieldExpression,
                                AttributeId = attribute.Id,
                                HeaderText = attribute.Name
                            };

                            var boundField2 = new AttributeField
                            {
                                DataField = dataFieldExpression,
                                AttributeId = attribute.Id,
                                HeaderText = attribute.Name
                            };

                            var attributeCache = Rock.Web.Cache.AttributeCache.Read( attribute.Id );
                            if ( attributeCache != null )
                            {
                                boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                                boundField2.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                            }

                            gRegistrants.Columns.Add( boundField );
                            _registrantGridColumnCount++;
                            gGroupPlacements.Columns.Add( boundField2 );
                        }
                    }
                }
            }

            //// Add dynamic columns for sub groups
            if ( AssociatedGroupTypesGrid != null )
            {
                var groupService = new GroupService( new RockContext() );
                var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
                var ri = new RegistrationInstanceService( new RockContext() ).Get( RegistrationInstanceId );
                ri.LoadAttributes();
                foreach ( var groupType in AssociatedGroupTypesGrid )
                {
                    var attributeValSplit = ri.AttributeValues[groupType.Name].Value.Split( '^' ).ToList();
                    var parentGroup = groupService.Get( Guid.Parse( attributeValSplit[0] ) );
                    var subGroupColumn = new LinkButtonField();
                    subGroupColumn.HeaderStyle.CssClass = "";
                    subGroupColumn.HeaderText = parentGroup.Name;
                    gRegistrants.Columns.Add( subGroupColumn );
                }
            }

            // Add fee column
            var feeField = new RockLiteralField
            {
                ID = "lFees",
                HeaderText = "Fees"
            };
            gRegistrants.Columns.Add( feeField );

            var deleteField = new DeleteField();
            gRegistrants.Columns.Add( deleteField );
            deleteField.Click += gRegistrants_Delete;

            var groupPickerField = new GroupPickerField
            {
                HeaderText = "Group",
                RootGroupId = gpGroupPlacementParentGroup.SelectedValueAsInt()
            };
            gGroupPlacements.Columns.Add( groupPickerField );
        }

        #endregion

        #region Payments Tab

        /// <summary>
        /// Binds the payments filter.
        /// </summary>
        private void BindPaymentsFilter()
        {
            drpPaymentDateRange.DelimitedValues = fPayments.GetUserPreference( "Date Range" );
        }

        /// <summary>
        /// Binds the payments grid.
        /// </summary>
        private void BindPaymentsGrid()
        {
            var instanceId = hfRegistrationInstanceId.Value.AsIntegerOrNull();
            if ( instanceId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var currencyTypes = new Dictionary<int, string>();
                    var creditCardTypes = new Dictionary<int, string>();

                    // If configured for a registration and registration is null, return
                    var registrationEntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Registration ) ).Id;

                    // Get all the registrations for this instance
                    PaymentRegistrations = new RegistrationService( rockContext )
                        .Queryable( "PersonAlias.Person,Registrants.PersonAlias.Person" ).AsNoTracking()
                        .Where( r =>
                            r.RegistrationInstanceId == instanceId.Value &&
                            !r.IsTemporary )
                        .ToList();

                    // Get the Registration Ids
                    var registrationIds = PaymentRegistrations
                        .Select( r => r.Id )
                        .ToList();

                    // Get all the transactions relate to these registrations
                    var qry = new FinancialTransactionService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( t => t.TransactionDetails
                            .Any( d =>
                                d.EntityTypeId.HasValue &&
                                d.EntityTypeId.Value == registrationEntityTypeId &&
                                d.EntityId.HasValue &&
                                registrationIds.Contains( d.EntityId.Value ) ) );

                    // Date Range
                    var drp = new DateRangePicker
                    {
                        DelimitedValues = fPayments.GetUserPreference( "Date Range" )
                    };
                    if ( drp.LowerValue.HasValue )
                    {
                        qry = qry.Where( t => t.TransactionDateTime >= drp.LowerValue.Value );
                    }

                    if ( drp.UpperValue.HasValue )
                    {
                        var upperDate = drp.UpperValue.Value.Date.AddDays( 1 );
                        qry = qry.Where( t => t.TransactionDateTime < upperDate );
                    }

                    var sortProperty = gPayments.SortProperty;
                    if ( sortProperty != null )
                    {
                        if ( sortProperty.Property == "TotalAmount" )
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                qry = qry.OrderBy( t => t.TransactionDetails.Sum( d => ( decimal? )d.Amount ) ?? 0.00M );
                            }
                            else
                            {
                                qry = qry.OrderByDescending( t => t.TransactionDetails.Sum( d => ( decimal? )d.Amount ) ?? 0.0M );
                            }
                        }
                        else
                        {
                            qry = qry.Sort( sortProperty );
                        }
                    }
                    else
                    {
                        qry = qry.OrderByDescending( t => t.TransactionDateTime ).ThenByDescending( t => t.Id );
                    }

                    gPayments.SetLinqDataSource( qry.AsNoTracking() );
                    gPayments.DataBind();
                }
            }
        }

        #endregion

        #region Linkages Tab

        /// <summary>
        /// Binds the registrations filter.
        /// </summary>
        private void BindLinkagesFilter()
        {
            cblCampus.DataSource = CampusCache.All();
            cblCampus.DataBind();
            var campusValue = fLinkages.GetUserPreference( "Campus" );
            if ( !string.IsNullOrWhiteSpace( campusValue ) )
            {
                cblCampus.SetValues( campusValue.Split( ';' ).ToList() );
            }
        }

        /// <summary>
        /// Binds the registrations grid.
        /// </summary>
        private void BindLinkagesGrid()
        {
            var instanceId = hfRegistrationInstanceId.Value.AsIntegerOrNull();
            if ( instanceId.HasValue )
            {
                var groupCol = gLinkages.Columns[2] as HyperLinkField;
                groupCol.DataNavigateUrlFormatString = LinkedPageUrl( "GroupDetailPage" ) + "?GroupID={0}";

                using ( var rockContext = new RockContext() )
                {
                    var qry = new EventItemOccurrenceGroupMapService( rockContext )
                        .Queryable( "EventItemOccurrence.EventItem.EventCalendarItems.EventCalendar,EventItemOccurrence.ContentChannelItems.ContentChannelItem,Group" )
                        .AsNoTracking()
                        .Where( r => r.RegistrationInstanceId == instanceId.Value );

                    var campusIds = cblCampus.SelectedValuesAsInt;
                    if ( campusIds.Any() )
                    {
                        qry = qry
                            .Where( l =>
                                l.EventItemOccurrence != null &&
                                (
                                    !l.EventItemOccurrence.CampusId.HasValue ||
                                    campusIds.Contains( l.EventItemOccurrence.CampusId.Value )
                                ) );
                    }

                    IOrderedQueryable<EventItemOccurrenceGroupMap> orderedQry = null;
                    var sortProperty = gLinkages.SortProperty;
                    if ( sortProperty != null )
                    {
                        orderedQry = qry.Sort( sortProperty );
                    }
                    else
                    {
                        orderedQry = qry.OrderByDescending( r => r.CreatedDateTime );
                    }

                    gLinkages.SetLinqDataSource( orderedQry );
                    gLinkages.DataBind();
                }
            }
        }

        #endregion

        #region Group Placement Tab

        /// <summary>
        /// Binds the group placement grid.
        /// </summary>
        /// <param name="isExporting">if set to <c>true</c> [is exporting].</param>
        private void BindGroupPlacementGrid( bool isExporting = false )
        {
            var groupId = gpGroupPlacementParentGroup.SelectedValueAsInt();
            var instanceId = hfRegistrationInstanceId.Value.AsIntegerOrNull();
            if ( instanceId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    // Start query for registrants
                    var qry = new RegistrationRegistrantService( rockContext )
                        .Queryable( "PersonAlias.Person.PhoneNumbers.NumberTypeValue,Fees.RegistrationTemplateFee,GroupMember.Group" ).AsNoTracking()
                        .Where( r =>
                            r.Registration.RegistrationInstanceId == instanceId.Value &&
                            r.PersonAlias != null &&
                            r.PersonAlias.Person != null );

                    if ( groupId.HasValue )
                    {
                        var validGroupIds = new GroupService( rockContext ).GetAllDescendents( groupId.Value )
                            .Select( g => g.Id )
                            .ToList();

                        var existingPeopleInGroups = new GroupMemberService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( m => validGroupIds.Contains( m.GroupId ) )
                            .Select( m => m.PersonId )
                            .ToList();

                        qry = qry.Where( r => !existingPeopleInGroups.Contains( r.PersonAlias.PersonId ) );
                    }

                    var preloadCampusValues = false;
                    var registrantAttributeIds = new List<int>();
                    var personAttributesIds = new List<int>();
                    var groupMemberAttributesIds = new List<int>();

                    if ( RegistrantFields != null )
                    {
                        // Check if campus is used
                        preloadCampusValues = RegistrantFields
                            .Any( f =>
                                f.FieldSource == RegistrationFieldSource.PersonField &&
                                f.PersonFieldType.HasValue &&
                                f.PersonFieldType.Value == RegistrationPersonFieldType.Campus );

                        // Get all the registrant attributes selected
                        var registrantAttributes = RegistrantFields
                            .Where( f =>
                                f.Attribute != null &&
                                f.FieldSource == RegistrationFieldSource.RegistrationAttribute )
                            .Select( f => f.Attribute )
                            .ToList();
                        registrantAttributeIds = registrantAttributes.Select( a => a.Id ).Distinct().ToList();

                        // Get all the person attributes selected
                        var personAttributes = RegistrantFields
                            .Where( f =>
                                f.Attribute != null &&
                                f.FieldSource == RegistrationFieldSource.PersonAttribute )
                            .Select( f => f.Attribute )
                            .ToList();
                        personAttributesIds = personAttributes.Select( a => a.Id ).Distinct().ToList();

                        // Get all the group member attributes selected to be on grid
                        var groupMemberAttributes = RegistrantFields
                            .Where( f =>
                                f.Attribute != null &&
                                f.FieldSource == RegistrationFieldSource.GroupMemberAttribute )
                            .Select( f => f.Attribute )
                            .ToList();
                        groupMemberAttributesIds = groupMemberAttributes.Select( a => a.Id ).Distinct().ToList();
                    }

                    // Sort the query
                    IOrderedQueryable<RegistrationRegistrant> orderedQry = null;
                    var sortProperty = gGroupPlacements.SortProperty;
                    if ( sortProperty != null )
                    {
                        orderedQry = qry.Sort( sortProperty );
                    }
                    else
                    {
                        orderedQry = qry
                            .OrderBy( r => r.PersonAlias.Person.LastName )
                            .ThenBy( r => r.PersonAlias.Person.NickName );
                    }

                    // Set the grids LinqDataSource which will run query and set results for current page
                    gGroupPlacements.SetLinqDataSource<RegistrationRegistrant>( orderedQry );

                    if ( RegistrantFields != null )
                    {
                        // Get the query results for the current page
                        var currentPageRegistrants = gGroupPlacements.DataSource as List<RegistrationRegistrant>;
                        if ( currentPageRegistrants != null )
                        {
                            // Get all the registrant ids in current page of query results
                            var registrantIds = currentPageRegistrants
                                .Select( r => r.Id )
                                .Distinct()
                                .ToList();

                            // Get all the person ids in current page of query results
                            var personIds = currentPageRegistrants
                                .Select( r => r.PersonAlias.PersonId )
                                .Distinct()
                                .ToList();

                            // Get all the group member ids and the group id in current page of query results
                            var groupMemberIds = new List<int>();
                            GroupLinks = new Dictionary<int, string>();
                            foreach ( var groupMember in currentPageRegistrants
                                .Where( m =>
                                    m.GroupMember != null &&
                                    m.GroupMember.Group != null )
                                .Select( m => m.GroupMember ) )
                            {
                                groupMemberIds.Add( groupMember.Id );
                                GroupLinks.AddOrIgnore( groupMember.GroupId,
                                    isExporting ? groupMember.Group.Name :
                                        string.Format( "<a href='{0}'>{1}</a>",
                                            LinkedPageUrl( "GroupDetailPage", new Dictionary<string, string> { { "GroupId", groupMember.GroupId.ToString() } } ),
                                            groupMember.Group.Name ) );
                            }

                            // If the campus column was selected to be displayed on grid, preload all the people's
                            // campuses so that the databind does not need to query each row
                            if ( preloadCampusValues )
                            {
                                PersonCampusIds = new Dictionary<int, List<int>>();

                                var familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid();
                                foreach ( var personCampusList in new GroupMemberService( rockContext )
                                    .Queryable().AsNoTracking()
                                    .Where( m =>
                                        m.Group.GroupType.Guid == familyGroupTypeGuid &&
                                        personIds.Contains( m.PersonId ) )
                                    .GroupBy( m => m.PersonId )
                                    .Select( m => new
                                    {
                                        PersonId = m.Key,
                                        CampusIds = m
                                            .Where( g => g.Group.CampusId.HasValue )
                                            .Select( g => g.Group.CampusId.Value )
                                            .ToList()
                                    } ) )
                                {
                                    PersonCampusIds.Add( personCampusList.PersonId, personCampusList.CampusIds );
                                }
                            }

                            // If there are any attributes that were selected to be displayed, we're going
                            // to try and read all attribute values in one query and then put them into a
                            // custom grid ObjectList property so that the AttributeField columns don't need
                            // to do the LoadAttributes and querying of values for each row/column
                            if ( personAttributesIds.Any() || groupMemberAttributesIds.Any() || registrantAttributeIds.Any() )
                            {
                                // Query the attribute values for all rows and attributes
                                var attributeValues = new AttributeValueService( rockContext )
                                    .Queryable( "Attribute" ).AsNoTracking()
                                    .Where( v =>
                                        v.EntityId.HasValue &&
                                        (
                                            (
                                                personAttributesIds.Contains( v.AttributeId ) &&
                                                personIds.Contains( v.EntityId.Value )
                                            ) ||
                                            (
                                                groupMemberAttributesIds.Contains( v.AttributeId ) &&
                                                groupMemberIds.Contains( v.EntityId.Value )
                                            ) ||
                                            (
                                                registrantAttributeIds.Contains( v.AttributeId ) &&
                                                registrantIds.Contains( v.EntityId.Value )
                                            )
                                        )
                                    )
                                    .ToList();

                                // Get the attributes to add to each row's object
                                var attributes = new Dictionary<string, AttributeCache>();
                                RegistrantFields
                                        .Where( f => f.Attribute != null )
                                        .Select( f => f.Attribute )
                                        .ToList()
                                    .ForEach( a => attributes
                                        .Add( a.Id.ToString() + a.Key, a ) );

                                // Initialize the grid's object list
                                gGroupPlacements.ObjectList = new Dictionary<string, object>();

                                // Loop through each of the current page's registrants and build an attribute
                                // field object for storing attributes and the values for each of the registrants
                                foreach ( var registrant in currentPageRegistrants )
                                {
                                    // Create a row attribute object
                                    var attributeFieldObject = new AttributeFieldObject
                                    {

                                        // Add the attributes to the attribute object
                                        Attributes = attributes
                                    };

                                    // Add any person attribute values to object
                                    attributeValues
                                        .Where( v =>
                                            personAttributesIds.Contains( v.AttributeId ) &&
                                            v.EntityId.Value == registrant.PersonAlias.PersonId )
                                        .ToList()
                                        .ForEach( v => attributeFieldObject.AttributeValues
                                            .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );

                                    // Add any group member attribute values to object
                                    if ( registrant.GroupMemberId.HasValue )
                                    {
                                        attributeValues
                                            .Where( v =>
                                                groupMemberAttributesIds.Contains( v.AttributeId ) &&
                                                v.EntityId.Value == registrant.GroupMemberId.Value )
                                            .ToList()
                                            .ForEach( v => attributeFieldObject.AttributeValues
                                                .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );
                                    }

                                    // Add any registrant attribute values to object
                                    attributeValues
                                        .Where( v =>
                                            registrantAttributeIds.Contains( v.AttributeId ) &&
                                            v.EntityId.Value == registrant.Id )
                                        .ToList()
                                        .ForEach( v => attributeFieldObject.AttributeValues
                                            .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );

                                    // Add row attribute object to grid's object list
                                    gGroupPlacements.ObjectList.Add( registrant.Id.ToString(), attributeFieldObject );
                                }
                            }
                        }
                    }

                    gGroupPlacements.DataBind();
                }
            }
        }

        #endregion

        #region Group Association Tabs

        protected void rpGroupPanels_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var groupType = ( GroupType )e.Item.DataItem;
            var pnlAssociatedGroup = ( Panel )e.Item.FindControl( "pnlAssociatedGroup" );
            var pnlGroupBody = ( Panel )e.Item.FindControl( "pnlGroupBody" );
            var pnlGroupHeading = ( Panel )e.Item.FindControl( "pnlGroupHeading" );
            var phGroupHeading = ( PlaceHolder )e.Item.FindControl( "phGroupHeading" );
            var lbAddSubGroup = ( LinkButton )e.Item.FindControl( "lbAddSubGroup" );
            var hfParentGroupId = ( HiddenField )e.Item.FindControl( "hfParentGroupId" );
            var phGroupControl = ( PlaceHolder )e.Item.FindControl( "phGroupControl" );

            var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            var ri = new RegistrationInstanceService( new RockContext() ).Get( RegistrationInstanceId );
            ri.LoadAttributes();
            var groupTypeGroupTerm = "Group";
            if ( !string.IsNullOrWhiteSpace( groupType.GroupTerm ) )
            {
                groupTypeGroupTerm = groupType.GroupTerm;
            }
            lbAddSubGroup.Text += string.Format( "Add {0}", groupTypeGroupTerm );
            var attributeValSplit = ri.AttributeValues[groupType.Name].Value.Split( '^' ).ToList();
            var parentGroup = new GroupService( new RockContext() ).Get( Guid.Parse( attributeValSplit[0] ) );
            var subGroups = new List<Group>( parentGroup.Groups );
            hfParentGroupId.Value = parentGroup.Guid.ToString();
            lbAddSubGroup.CommandName = "AddSubGroup";
            lbAddSubGroup.CommandArgument = parentGroup.Guid.ToString();
            BuildSubGroupPanels( phGroupControl, subGroups );

            pnlAssociatedGroup.Visible = ActiveTab == ("lb" + groupType.Name);
            var header = new HtmlGenericControl( "h1" );
            header.Attributes.Add( "class", "panel-title" );
            var modalIconString = string.Empty;
            if ( !string.IsNullOrWhiteSpace( groupType.IconCssClass ) )
            {
                var faIcon = new HtmlGenericControl( "i" );
                faIcon.Attributes.Add( "class", groupType.IconCssClass );
                header.Controls.Add( faIcon );
            }
            header.Controls.Add( new LiteralControl( groupType.Name ) );
            phGroupHeading.Controls.Add( header );
        }

        private void BuildSubGroupPanels( PlaceHolder phGroupControl, List<Group> subGroups )
        {
            foreach ( Group g in subGroups )
            {
                var gp = ( KFSGroupPanel )LoadControl( "~/Plugins/KFS/Event/GroupPanel.ascx" );
                gp.ID = string.Format( "groupPanel_{0}", g.Id );
                foreach ( string control in _expandedGroupPanels )
                {
                    if ( control.Contains( gp.ID ) )
                    {
                        gp.Expanded = true;
                        break;
                    }
                }
                gp.AddButtonClick += Button_Click;
                gp.EditMemberButtonClick += EditMemberButton_Click;
                gp.BuildControl( g );
                phGroupControl.Controls.Add( gp );
            }
        }

        private void RpGroupPanels_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            if ( e.CommandName == "AddSubGroup" )
            {
                hfEditGroup.Value = string.Empty;
                tbName.Text = string.Empty;
                tbDescription.Text = string.Empty;
                nbGroupCapacity.Text = string.Empty;
                var parentGroup = new GroupService( new RockContext() ).Get( Guid.Parse( hfActiveTabParentGroup.Value ) );
                var groupTypeGroupTerm = "Group";
                if ( !string.IsNullOrWhiteSpace( parentGroup.GroupType.GroupTerm ) )
                {
                    groupTypeGroupTerm = parentGroup.GroupType.GroupTerm;
                }
                var modalIconString = string.Empty;
                if ( !string.IsNullOrWhiteSpace( parentGroup.GroupType.IconCssClass ) )
                {
                    modalIconString = string.Format( "<i class='{0}'></i> ", parentGroup.GroupType.IconCssClass );
                }
                rpGroupPanels.DataSource = AssociatedGroupTypes;
                rpGroupPanels.DataBind();
                mdlAddSubGroup.Title = string.Format( "{0} Add New {1}", modalIconString, groupTypeGroupTerm );
                mdlAddSubGroup.Show();
            }
            if ( e.CommandName == "EditSubGroup" )
            {
                var group = new GroupService( new RockContext() ).Get( int.Parse( e.CommandArgument.ToString() ) );
                hfEditGroup.Value = group.Guid.ToString();
                tbName.Text = group.Name;
                tbDescription.Text = group.Description;
                nbGroupCapacity.Text = group.GroupCapacity.ToString();
                var modalIconString = string.Empty;
                if ( !string.IsNullOrWhiteSpace( group.ParentGroup.GroupType.IconCssClass ) )
                {
                    modalIconString = string.Format( "<i class='{0}'></i> ", group.ParentGroup.GroupType.IconCssClass );
                }
                rpGroupPanels.DataSource = AssociatedGroupTypes;
                rpGroupPanels.DataBind();
                mdlAddSubGroup.Title = string.Format( "{0} Edit {1}", modalIconString, group.Name );
                mdlAddSubGroup.Show();
            }
            if ( e.CommandName == "DeleteSubGroup" )
            {
                var rockContext = new RockContext();
                var groupService = new GroupService( rockContext );
                var authService = new AuthService( rockContext );
                var group = groupService.Get( e.CommandArgument.ToString().AsInteger() );
                hfEditGroup.Value = string.Empty;

                int? parentGroupId = null;

                if ( group != null )
                {
                    if ( !group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) )
                    {
                        mdDeleteWarning.Show( "You are not authorized to delete this group.", ModalAlertType.Information );
                        return;
                    }

                    parentGroupId = group.ParentGroupId;
                    string errorMessage;
                    if ( !groupService.CanDelete( group, out errorMessage ) )
                    {
                        mdDeleteWarning.Show( errorMessage, ModalAlertType.Information );
                        return;
                    }

                    var isSecurityRoleGroup = group.IsActive && (group.IsSecurityRole || group.GroupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() ));
                    if ( isSecurityRoleGroup )
                    {
                        Rock.Security.Role.Flush( group.Id );
                        foreach ( var auth in authService.Queryable().Where( a => a.GroupId == group.Id ).ToList() )
                        {
                            authService.Delete( auth );
                        }
                    }

                    // If group has a non-named schedule, delete the schedule record.
                    if ( group.ScheduleId.HasValue )
                    {
                        var scheduleService = new ScheduleService( rockContext );
                        var schedule = scheduleService.Get( group.ScheduleId.Value );
                        if ( schedule != null && schedule.ScheduleType != ScheduleType.Named )
                        {
                            scheduleService.Delete( schedule );
                        }
                    }

                    groupService.Delete( group );

                    rockContext.SaveChanges();

                    if ( isSecurityRoleGroup )
                    {
                        Rock.Security.Authorization.Flush();
                    }
                }
                rpGroupPanels.DataSource = AssociatedGroupTypes;
                rpGroupPanels.DataBind();
            }
        }

        private void Button_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var panel = ( KFSGroupPanel )sender;
            var group = panel.Group;
            RenderMemberModal( rockContext, group.ParentGroup, group, null, null );
        }

        private void EditMemberButton_Click( object sender, EventArgs e )
        {
            var re = ( RowEventArgs )e;
            var groupMemberId = re.RowKeyId;
            if ( groupMemberId > 0 )
            {
                var rockContext = new RockContext();
                var memberService = new GroupMemberService( rockContext );
                var member = memberService.Queryable().Where( m => m.Id == groupMemberId ).FirstOrDefault();
                var group = member.Group;
                RenderMemberModal( rockContext, group.ParentGroup, group, member, null );
            }
        }

        private void RenderMemberModal( RockContext rockContext, Group parentGroup, Group group, GroupMember groupMember, RegistrationRegistrant registrant )
        {
            // Clear modal controls
            ddlRegistrantList.Items.Clear();
            ddlGroupRole.Items.Clear();
            tbNote.Text = string.Empty;
            tbDescription.Text = string.Empty;
            nbGroupCapacity.Text = string.Empty;

            var RegistrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            var ris = new RegistrationInstanceService( new RockContext() );
            rblStatus.BindToEnum<GroupMemberStatus>();
            var groupMemberTerm = "Member";
            if( group != null)
            {
                hfSubGroupId.Value = group.Id.ToString();
                if ( !string.IsNullOrWhiteSpace( group.GroupType.GroupMemberTerm ) )
                {
                    groupMemberTerm = group.GroupType.GroupMemberTerm;
                }
                parentGroup = group.ParentGroup;
            }
            else if( parentGroup != null )
            {
                if ( !string.IsNullOrWhiteSpace( parentGroup.GroupType.GroupMemberTerm ) )
                {
                    groupMemberTerm = parentGroup.GroupType.GroupMemberTerm;
                }
            }
            ddlGroupRole.DataSource = parentGroup.GroupType.Roles.OrderBy( a => a.Order ).ToList();
            ddlGroupRole.DataBind();
            hfSubGroupMemberId.Value = groupMember != null ? groupMember.Id.ToString() : "0";
            if ( groupMember == null )
            {
                ddlSubGroup.Visible = false;
                ddlRegistrantList.Enabled = true;
                if ( group == null && registrant != null )
                {
                    ddlRegistrantList.Help = null;
                    ddlRegistrantList.Items.Add( new ListItem( registrant.Person.FullNameReversed, registrant.Person.Guid.ToString() ) );
                    ddlRegistrantList.Enabled = false;
                    mdlAddSubGroupMember.Title = string.Format( "Add New {0}", groupMemberTerm );
                    ddlSubGroup.Visible = true;
                    ddlSubGroup.DataSource = parentGroup.Groups;
                    ddlSubGroup.DataTextField = "Name";
                    ddlSubGroup.DataValueField = "Id";
                    ddlSubGroup.DataBind();
                    ddlSubGroup.Label = !string.IsNullOrWhiteSpace( parentGroup.GroupType.GroupTerm ) ? parentGroup.GroupType.GroupTerm : "Sub-Group";
                }
                else if ( group != null )
                {
                    var placedMembers = new List<int>();
                    foreach ( Group g in group.ParentGroup.Groups )
                    {
                        placedMembers.AddRange( g.Members.Select( m => m.Person.Id ).Where( m => !placedMembers.Contains( m ) ) );
                    }
                    var qry = new RegistrationRegistrantService( rockContext )
                     .Queryable().AsNoTracking()
                     .Where( r =>
                         r.Registration.RegistrationInstanceId == RegistrationInstanceId &&
                         r.PersonAlias != null &&
                         r.PersonAlias.Person != null &&
                         !placedMembers.Contains( r.PersonAlias.Person.Id ) );
                    ddlRegistrantList.Help = string.Format( "Choose from a list of Registrants who have not yet been assigned to a {0} {1}", group.ParentGroup.Name, group.GroupType.GroupTerm );
                    foreach ( var reg in qry.ToList() )
                    {
                        ddlRegistrantList.Items.Add( new ListItem( reg.PersonAlias.Person.FullNameReversed, reg.PersonAlias.Person.Guid.ToString() ) );
                    }
                    mdlAddSubGroupMember.Title = string.Format( "Add New {0} to {1}", groupMemberTerm, group.Name );
                }
                rblStatus.SelectedIndex = 1;
            }
            else
            {
                ddlRegistrantList.Help = null;
                ddlRegistrantList.Items.Add( new ListItem( groupMember.Person.FullNameReversed, groupMember.Person.Guid.ToString() ) );
                ddlRegistrantList.Enabled = false;
                ddlGroupRole.SelectedValue = groupMember.GroupRoleId.ToString();
                rblStatus.SelectedValue = ( (int)groupMember.GroupMemberStatus ).ToString();
                tbNote.Text = groupMember.Note;
                mdlAddSubGroupMember.Title = string.Format( "Edit {0} in {1}", groupMemberTerm, group.Name );
                ddlSubGroup.Visible = true;
                
                ddlSubGroup.DataSource = group.ParentGroup.Groups;
                ddlSubGroup.DataTextField = "Name";
                ddlSubGroup.DataValueField = "Id";
                ddlSubGroup.DataBind();
                ddlSubGroup.Items.Insert( 0, Rock.Constants.None.ListItem );
                ddlSubGroup.Label = !string.IsNullOrWhiteSpace( group.GroupType.GroupTerm ) ? group.GroupType.GroupTerm : "Sub-Group";
                ddlSubGroup.SelectedValue = group.Id.ToString();
            }
            mdlAddSubGroupMember.Show();
        }

        #endregion

        #endregion

        #region Helper Classes

        /// <summary>
        /// Helper class for tracking registration form fields
        /// </summary>
        [Serializable]
        public class RegistrantFormField
        {
            /// <summary>
            /// Gets or sets the field source.
            /// </summary>
            /// <value>
            /// The field source.
            /// </value>
            public RegistrationFieldSource FieldSource { get; set; }

            /// <summary>
            /// Gets or sets the type of the person field.
            /// </summary>
            /// <value>
            /// The type of the person field.
            /// </value>
            public RegistrationPersonFieldType? PersonFieldType { get; set; }

            /// <summary>
            /// Gets or sets the attribute.
            /// </summary>
            /// <value>
            /// The attribute.
            /// </value>
            public AttributeCache Attribute { get; set; }
        }

        #endregion

        protected void mdlAddSubGroup_SaveClick( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var parentGroup = new GroupService( rockContext ).Get( Guid.Parse( hfActiveTabParentGroup.Value ) );
            Group group;
            var groupService = new GroupService( rockContext );
            Guid groupGuid;
            var isNew = false;

            if ( !Guid.TryParse( hfEditGroup.Value, out groupGuid ) )
            {
                group = new Group
                {
                    IsSystem = false,
                    Name = string.Empty,
                    GroupType = parentGroup.GroupType,
                    CampusId = parentGroup.CampusId,
                    ParentGroupId = parentGroup.Id
                };
                isNew = true;
            }
            else
            {
                group = groupService.Get( groupGuid );
            }
            group.Name = tbName.Text;
            group.Description = tbDescription.Text;
            group.GroupCapacity = nbGroupCapacity.Text.AsIntegerOrNull();
            group.IsActive = cbIsActive.Checked;
            if ( isNew )
            {
                groupService.Add( group );
            }
            rockContext.SaveChanges();
            mdlAddSubGroup.Hide();
            rpGroupPanels.DataSource = AssociatedGroupTypes;
            rpGroupPanels.DataBind();
        }
    }
}