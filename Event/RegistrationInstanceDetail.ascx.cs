// KFS Registration Instance Detail

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using com.kfs.EventRegistration.Advanced;
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
using Attribute = Rock.Model.Attribute;

namespace RockWeb.Plugins.com_kfs.Event
{
    /// <summary>
    /// Template block for editing an event registration instance.
    /// </summary>
    [DisplayName( "Advanced Registration Instance Detail" )]
    [Category( "KFS > Advanced Event Registration" )]
    [Description( "Template block for editing an event registration instance." )]
    [AccountField( "Default Account", "The default account to use for new registration instances", false, "2A6F9E5F-6859-44F1-AB0E-CE9CF6B08EE5", "", 0 )]
    [LinkedPage( "Registration Page", "The page for editing registration and registrant information", true, "", "", 1 )]
    [LinkedPage( "Linkage Page", "The page for editing registration linkages", true, "", "", 2 )]
    [LinkedPage( "Calendar Item Page", "The page to view calendar item details", true, "", "", 3 )]
    [LinkedPage( "Group Detail Page", "The page for viewing details about a group", true, "", "", 4 )]
    [LinkedPage( "Group Modal Page", "The modal page to view and edit details for a group", true, "", "", 5 )]
    [LinkedPage( "Content Item Page", "The page for viewing details about a content channel item", true, "", "", 6 )]
    [LinkedPage( "Transaction Detail Page", "The page for viewing details about a payment", true, "", "", 7 )]
    [LinkedPage( "Payment Reminder Page", "The page for manually sending payment reminders.", false, "", "", 8 )]
    [BooleanField( "Display Discount Codes", "Display the discount code used with a payment", false, "", 9 )]
    public partial class KFSRegistrationInstanceDetail : Rock.Web.UI.RockBlock, IDetailBlock
    {
        #region Fields

        private List<FinancialTransactionDetail> RegistrationPayments;
        private List<Registration> PaymentRegistrations;
        private bool _instanceHasCost = false;
        private Dictionary<int, Location> _homeAddresses = new Dictionary<int, Location>();

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

        // registration resource types and instances
        protected List<GroupTypeCache> ResourceGroupTypes { get; set; }

        protected Dictionary<string, AttributeValueCache> ResourceGroups { get; set; }
        protected int? GroupAssignmentsIndex = null;
        protected int? RegistrationInstanceGroupId = null;
        protected int? TemplateGroupTypeId = null;

        // TODO: clean up some of these fields
        private List<string> _expandedGroupPanels = new List<string>();

        private List<Guid> _resourceGroupTypes = new List<Guid>();
        private List<Guid> _resourceGroups = new List<Guid>();
        private List<Guid> _expandedRows = new List<Guid>();
        private Guid? _currentGroupTypeGuid = null;
        private Guid? _currentGroupGuid = null;

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
            TemplateGroupTypeId = ViewState["TemplateGroupTypeId"] as int?;
            RegistrationInstanceGroupId = ViewState["RegistrationInstanceGroupId"] as int?;
            _currentGroupTypeGuid = ViewState["CurrentGroupTypeGuid"] as Guid?;
            _currentGroupGuid = ViewState["CurrentGroupGuid"] as Guid?;

            ResourceGroupTypes = new List<GroupTypeCache>();
            using ( var rockContext = new RockContext() )
            {
                foreach ( int id in ViewState["ResourceGroupTypes"] as List<int> )
                {
                    ResourceGroupTypes.Add( GroupTypeCache.Read( id, rockContext ) );
                }

                Group group = null;
                if ( _currentGroupGuid.HasValue )
                {
                    group = new GroupService( rockContext ).Get( _currentGroupGuid.Value );
                    if ( group != null )
                    {
                        resourceGroupPanel.CreateGroupAttributeControls( group, rockContext );
                    }
                }
            }

            bool setValues = this.Request.Params["__EVENTTARGET"] == null || !this.Request.Params["__EVENTTARGET"].EndsWith( "_lbClearFilter" );

            // Rebuilds dynamic group area controls after postbacks
            BuildResourcesInterface();
            BuildSubGroupTabs();
            AddDynamicControls( setValues );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // NOTE: The 3 Grid Filters had a bug where all were using the same "Date Range" key
            // in a prior version and they didn't really work quite right, so wipe them out
            fRegistrations.SaveUserPreference( "Date Range", null );
            fRegistrants.SaveUserPreference( "Date Range", null );
            fPayments.SaveUserPreference( "Date Range", null );

            if ( Page.IsPostBack )
            {
                var httpForm = Request.Form;
                foreach ( string key in httpForm.AllKeys )
                {
                    if ( httpForm[key] == "True" && key.Contains( "hfExpanded" ) )
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
            ddlInGroup.Items.Add( new ListItem() );
            ddlInGroup.Items.Add( new ListItem( "Yes", "Yes" ) );
            ddlInGroup.Items.Add( new ListItem( "No", "No" ) );

            ddlRegistrantsSignedDocument.Items.Clear();
            ddlRegistrantsSignedDocument.Items.Add( new ListItem() );
            ddlRegistrantsSignedDocument.Items.Add( new ListItem( "Yes", "Yes" ) );
            ddlRegistrantsSignedDocument.Items.Add( new ListItem( "No", "No" ) );

            ddlGroupPlacementsSignedDocument.Items.Clear();
            ddlGroupPlacementsSignedDocument.Items.Add( new ListItem() );
            ddlGroupPlacementsSignedDocument.Items.Add( new ListItem( "Yes", "Yes" ) );
            ddlGroupPlacementsSignedDocument.Items.Add( new ListItem( "No", "No" ) );

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

            fGroupPlacements.ApplyFilterClick += fGroupPlacements_ApplyFilterClick;
            gGroupPlacements.DataKeyNames = new string[] { "Id" };
            gGroupPlacements.Actions.ShowAdd = false;
            gGroupPlacements.RowDataBound += gRegistrants_RowDataBound; //intentionally using same row data bound event as the gRegistrants grid
            gGroupPlacements.GridRebind += gGroupPlacements_GridRebind;

            //fLinkages.ApplyFilterClick += fLinkages_ApplyFilterClick;
            //gVolunteers.DataKeyNames = new string[] { "Id" };
            //gVolunteers.Actions.ShowAdd = true;
            //gVolunteers.Actions.AddClick += AddMemberButton_Click;
            //gVolunteers.RowDataBound += gRegistrants_RowDataBound;
            //gVolunteers.RowCommand += gRegistrants_RowCommand;

            rpResourcePanels.ItemDataBound += rpResourcePanels_ItemDataBound;
            rpResourcePanels.ItemCommand += rpResourcePanels_ItemCommand;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            RegisterScript();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            // Set up associated resources
            var registrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsIntegerOrNull();
            if ( registrationInstanceId.HasValue )
            {
                var instance = GetRegistrationInstance( registrationInstanceId.Value );
                if ( ResourceGroupTypes == null )
                {
                    LoadRegistrationResources( instance );
                }

                BindResourcePanels();
            }

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
                // handle group click events
                var postbackArgs = Request.Params["__EVENTARGUMENT"];
                if ( !string.IsNullOrWhiteSpace( postbackArgs ) )
                {
                    var eventParams = postbackArgs.Split( new char[] { ':' } );
                    if ( eventParams.Length == 2 )
                    {
                        hfAreaGroupClicked.Value = "false";
                        switch ( eventParams[0] )
                        {
                            case "select-area":
                                {
                                    hfAreaGroupClicked.Value = "true";
                                    SelectArea( eventParams[1].AsGuid() );
                                    break;
                                }

                            case "select-group":
                                {
                                    hfAreaGroupClicked.Value = "true";
                                    SelectGroup( eventParams[1].AsGuid() );
                                    break;
                                }

                            case "select-subgroup":
                                {
                                    RenderEditGroupMemberModal( eventParams[1] );
                                    break;
                                }
                        }
                    }
                }

                var groupMember = new GroupMember { Id = hfSubGroupMemberId.ValueAsInt(), GroupId = hfSubGroupId.ValueAsInt() };
                if ( groupMember != null && groupMember.GroupId > 0 )
                {
                    groupMember.LoadAttributes();
                    phAttributes.Controls.Clear();
                    Rock.Attribute.Helper.AddEditControls( groupMember, phAttributes, false, mdlAddSubGroupMember.ValidationGroup );
                }

                SetFollowingOnPostback();
            }
        }

        #region Viewstate

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ResourceGroupTypes = ResourceGroupTypes ?? new List<GroupTypeCache>();
            ViewState["RegistrantFields"] = RegistrantFields;
            ViewState["TemplateGroupTypeId"] = TemplateGroupTypeId;
            ViewState["ResourceGroupTypes"] = ResourceGroupTypes.Select( t => t.Id ).ToList();
            ViewState["RegistrationInstanceGroupId"] = RegistrationInstanceGroupId;
            ViewState["CurrentGroupTypeGuid"] = _currentGroupTypeGuid;
            ViewState["CurrentGroupGuid"] = _currentGroupGuid;
            ViewState["ActiveTab"] = ActiveTab;

            return base.SaveViewState();
        }

        #endregion

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
                var instance = GetRegistrationInstance( registrationInstanceId.Value );
                if ( instance != null )
                {
                    breadCrumbs.Add( new BreadCrumb( instance.ToString(), pageReference ) );
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
                var instance = new RegistrationInstanceService( rockContext ).Get( hfRegistrationInstanceId.Value.AsInteger() );
                BuildResourcesInterface( true ); 
                BuildRegistrationGroupHierarchy( rockContext, instance );
                ShowEditDetails( instance, rockContext );
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
                var instance = service.Get( hfRegistrationInstanceId.Value.AsInteger() );
                if ( instance != null )
                {
                    if ( UserCanEdit || instance.IsAuthorized( Authorization.EDIT, CurrentPerson ) || instance.IsAuthorized( Authorization.ADMINISTRATE, this.CurrentPerson ) )
                    {
                        rockContext.WrapTransaction( () =>
                        {
                            new RegistrationService( rockContext ).DeleteRange( instance.Registrations );
                            service.Delete( instance );

                            // delete all instance groups
                            var groupService = new GroupService( rockContext );
                            var instanceGroups = groupService.GetChildren( (int)RegistrationInstanceGroupId, 0, false, new List<int>(), new List<int>(), true, false );
                            groupService.DeleteRange( instanceGroups );
                            groupService.Delete( new Group { Id = (int)RegistrationInstanceGroupId } );

                            rockContext.SaveChanges();
                        } );

                        var qryParams = new Dictionary<string, string> { { "RegistrationTemplateId", instance.RegistrationTemplateId.ToString() } };
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

                var registrationInstanceId = hfRegistrationInstanceId.Value.AsIntegerOrNull();
                if ( registrationInstanceId.HasValue )
                {
                    instance = service.Get( registrationInstanceId.Value );
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

                if ( resourceAreaPanel.Visible )
                {
                    var groupType = new GroupTypeService( rockContext ).Get( resourceAreaPanel.GroupTypeGuid );
                    if ( groupType != null )
                    {
                        resourceAreaPanel.GetGroupTypeValues( groupType );

                        if ( groupType.IsValid )
                        {
                            rockContext.SaveChanges();
                            groupType.SaveAttributeValues( rockContext );

                            // Make sure default role is set
                            if ( !groupType.DefaultGroupRoleId.HasValue && groupType.Roles.Any() )
                            {
                                groupType.DefaultGroupRoleId = groupType.Roles.First().Id;
                            }

                            GroupTypeCache.Flush( groupType.Id );
                            nbSaveSuccess.Visible = true;
                        }
                        else
                        {
                            ShowInvalidResults( groupType.ValidationResults );
                        }
                    }
                }

                if ( resourceGroupPanel.Visible )
                {
                    var groupService = new GroupService( rockContext );
                    var groupLocationService = new GroupLocationService( rockContext );

                    var group = groupService.Get( resourceGroupPanel.GroupGuid );
                    if ( group != null )
                    {
                        group.LoadAttributes( rockContext );
                        resourceGroupPanel.GetGroupValues( group );

                        // make sure child groups can be created
                        if ( !group.GroupType.ChildGroupTypes.Contains( group.GroupType ) )
                        {
                            group.GroupType.ChildGroupTypes.Add( group.GroupType );
                        }

                        if ( group.IsValid )
                        {
                            rockContext.SaveChanges();
                            group.SaveAttributeValues( rockContext );
                            nbSaveSuccess.Visible = true;
                        }
                        else
                        {
                            ShowInvalidResults( group.ValidationResults );
                        }
                    }
                }

                rockContext.SaveChanges();
                BuildRegistrationGroupHierarchy( rockContext, instance );
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

            BuildSubGroupTabs( instance );
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

        /// <summary>
        /// Handles the Click event of the lbTemplate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbTemplate_Click( object sender, EventArgs e )
        {
            var qryParams = new Dictionary<string, string>();
            using ( var rockContext = new RockContext() )
            {
                var service = new RegistrationInstanceService( rockContext );
                var instance = service.Get( hfRegistrationInstanceId.Value.AsInteger() );
                if ( instance != null )
                {
                    qryParams.Add( "RegistrationTemplateId", instance.RegistrationTemplateId.ToString() );
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
            fRegistrations.SaveUserPreference( "Registrations Date Range", "Registration Date Range", sdrpRegistrationDateRange.DelimitedValues );
            fRegistrations.SaveUserPreference( "Payment Status", ddlRegistrationPaymentStatus.SelectedValue );
            fRegistrations.SaveUserPreference( "RegisteredBy First Name", tbRegistrationRegisteredByFirstName.Text );
            fRegistrations.SaveUserPreference( "RegisteredBy Last Name", tbRegistrationRegisteredByLastName.Text );
            fRegistrations.SaveUserPreference( "Registrant First Name", tbRegistrationRegistrantFirstName.Text );
            fRegistrations.SaveUserPreference( "Registrant Last Name", tbRegistrationRegistrantLastName.Text );

            BindRegistrationsGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the fRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fRegistrants_ClearFilterClick( object sender, EventArgs e )
        {
            fRegistrants.DeleteUserPreferences();

            foreach ( var control in phRegistrantFormFieldFilters.ControlsOfTypeRecursive<Control>().Where( a => a.ID != null && a.ID.StartsWith( "filter" ) && a.ID.Contains( "_" ) ) )
            {
                var attributeId = control.ID.Split( '_' )[1].AsInteger();
                var attribute = AttributeCache.Read( attributeId );
                if ( attribute != null )
                {
                    attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, new List<string>() );
                }
            }

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
                                    var ddlCampus = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsCampus" ) as RockDropDownList;
                                    if ( ddlCampus != null )
                                    {
                                        ddlCampus.SetValue( (Guid?)null );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Email:
                                {
                                    var tbEmailFilter = phRegistrantFormFieldFilters.FindControl( "tbRegistrantsEmailFilter" ) as RockTextBox;
                                    if ( tbEmailFilter != null )
                                    {
                                        tbEmailFilter.Text = string.Empty;
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Birthdate:
                                {
                                    var drpBirthdateFilter = phRegistrantFormFieldFilters.FindControl( "drpRegistrantsBirthdateFilter" ) as DateRangePicker;
                                    if ( drpBirthdateFilter != null )
                                    {
                                        drpBirthdateFilter.LowerValue = null;
                                        drpBirthdateFilter.UpperValue = null;
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Grade:
                                {
                                    var gpGradeFilter = phRegistrantFormFieldFilters.FindControl( "gpRegistrantsGradeFilter" ) as GradePicker;
                                    if ( gpGradeFilter != null )
                                    {
                                        gpGradeFilter.SetValue( (Guid?)null );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.Gender:
                                {
                                    var ddlGenderFilter = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsGenderFilter" ) as RockDropDownList;
                                    if ( ddlGenderFilter != null )
                                    {
                                        ddlGenderFilter.SetValue( (Guid?)null );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.MaritalStatus:
                                {
                                    var ddlMaritalStatusFilter = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsMaritalStatusFilter" ) as RockDropDownList;
                                    if ( ddlMaritalStatusFilter != null )
                                    {
                                        ddlMaritalStatusFilter.SetValue( (Guid?)null );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.MobilePhone:
                                {
                                    var tbPhoneFilter = phRegistrantFormFieldFilters.FindControl( "tbRegistrantsPhoneFilter" ) as RockTextBox;
                                    if ( tbPhoneFilter != null )
                                    {
                                        tbPhoneFilter.Text = string.Empty;
                                    }

                                    break;
                                }
                        }
                    }
                }
            }

            BindRegistrantsFilter( null );
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the fRegistrations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fRegistrations_ClearFilterClick( object sender, EventArgs e )
        {
            fRegistrants.DeleteUserPreferences();
            BindRegistrationsFilter();
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
                case "Registrations Date Range":
                    {
                        e.Value = SlidingDateRangePicker.FormatDelimitedValues( e.Value );
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
                    lDiscount.Visible = _instanceHasCost && !string.IsNullOrWhiteSpace( discountCode );
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
                        !registration.IsAuthorized( "Register", CurrentPerson ) &&
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
                    foreach ( var groupType in ResourceGroupTypes )
                    {
                        foreach ( var registrant in registration.Registrants )
                        {
                            deleteMembers.AddRange( GetDeleteGroupMembers( rockContext, memberService, groupType, registrant, registrationInstanceId ) );
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

                        foreach ( GroupMember member in deleteMembers )
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

        /// <summary>
        /// Gets the group members to delete.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="memberService">The member service.</param>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="registrant">The registrant.</param>
        /// <param name="registrationInstanceId">The registration instance identifier.</param>
        /// <returns></returns>
        private List<GroupMember> GetDeleteGroupMembers( RockContext rockContext, GroupMemberService memberService, GroupTypeCache groupType, RegistrationRegistrant registrant, int? registrationInstanceId )
        {
            var deleteMembers = new List<GroupMember>();
            registrationInstanceId = registrationInstanceId ?? PageParameter( "RegistrationInstanceId" ).AsIntegerOrNull();
            if ( registrationInstanceId.HasValue )
            {
                var instance = GetRegistrationInstance( registrationInstanceId.Value, rockContext );
                var parentGroup = new GroupService( rockContext ).Get( instance.AttributeValues[groupType.Name].Value.AsGuid() );
                if ( parentGroup != null )
                {
                    var groupIds = parentGroup.Groups.Select( g => g.Id ).ToList();

                    deleteMembers.AddRange( memberService.Queryable()
                        .Where( m => groupIds.Contains( m.GroupId ) )
                        .Where( m => m.PersonId == registrant.PersonId )
                        .ToList()
                    );
                }
            }

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
            fRegistrants.SaveUserPreference( "Registrants Date Range", "Registration Date Range", sdrpRegistrantsRegistrantDateRange.DelimitedValues );
            fRegistrants.SaveUserPreference( "First Name", tbRegistrantFirstName.Text );
            fRegistrants.SaveUserPreference( "Last Name", tbRegistrantLastName.Text );
            fRegistrants.SaveUserPreference( "In Group", ddlInGroup.SelectedValue );
            fRegistrants.SaveUserPreference( "Signed Document", ddlRegistrantsSignedDocument.SelectedValue );

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
                                    var ddlCampus = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsCampus" ) as RockDropDownList;
                                    if ( ddlCampus != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Home Campus", ddlCampus.SelectedValue );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Email:
                                {
                                    var tbEmailFilter = phRegistrantFormFieldFilters.FindControl( "tbRegistrantsEmailFilter" ) as RockTextBox;
                                    if ( tbEmailFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Email", tbEmailFilter.Text );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Birthdate:
                                {
                                    var drpBirthdateFilter = phRegistrantFormFieldFilters.FindControl( "drpRegistrantsBirthdateFilter" ) as DateRangePicker;
                                    if ( drpBirthdateFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Birthdate Range", drpBirthdateFilter.DelimitedValues );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Grade:
                                {
                                    var gpGradeFilter = phRegistrantFormFieldFilters.FindControl( "gpRegistrantsGradeFilter" ) as GradePicker;
                                    if ( gpGradeFilter != null )
                                    {
                                        var gradeOffset = gpGradeFilter.SelectedValueAsInt( false );
                                        fRegistrants.SaveUserPreference( "Grade", gradeOffset.HasValue ? gradeOffset.Value.ToString() : "" );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.Gender:
                                {
                                    var ddlGenderFilter = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsGenderFilter" ) as RockDropDownList;
                                    if ( ddlGenderFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Gender", ddlGenderFilter.SelectedValue );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.MaritalStatus:
                                {
                                    var ddlMaritalStatusFilter = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsMaritalStatusFilter" ) as RockDropDownList;
                                    if ( ddlMaritalStatusFilter != null )
                                    {
                                        fRegistrants.SaveUserPreference( "Marital Status", ddlMaritalStatusFilter.SelectedValue );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.MobilePhone:
                                {
                                    var tbPhoneFilter = phRegistrantFormFieldFilters.FindControl( "tbRegistrantsPhoneFilter" ) as RockTextBox;
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

            // save fee filters
            var nreFeeFilter = phRegistrantFormFieldFilters.FindControl( "nreRegistrantsFeeFilter" ) as NumberRangeEditor;
            if ( nreFeeFilter != null )
            {
                fRegistrants.SaveUserPreference( "Fee Amount", nreFeeFilter.DelimitedValues );
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
                case "Registrants Date Range":
                    {
                        e.Value = SlidingDateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }
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
                case "Fee Amount":
                    {
                        e.Value = NumberRangeEditor.FormatDelimitedValues( e.Value, "N2" );
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
                                string.Empty );
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
                    if ( location != null && lStreet1 != null && lStreet2 != null && lCity != null && lPostalCode != null && lCountry != null )
                    {
                        lStreet1.Text = location.Street1;
                        lStreet2.Text = location.Street2;
                        lCity.Text = location.City;
                        lState.Text = location.State;
                        lPostalCode.Text = location.PostalCode;
                        lCountry.Text = location.Country;
                    }
                }
                
                // Build subgroup button grid
                using ( var rockContext = new RockContext() )
                {
                    foreach ( var groupType in ResourceGroupTypes.Where( gt => gt.GetAttributeValue( "ShowOnGrid" ).AsBoolean( true ) ) )
                    {
                        var resourceGroupGuid = ResourceGroups.ContainsKey( groupType.Name ) ? ResourceGroups[groupType.Name] : null;
                        if ( resourceGroupGuid != null && !Guid.Empty.Equals( resourceGroupGuid.Value.AsGuid() ) )
                        {
                            var parentGroup = new GroupService( rockContext ).Get( resourceGroupGuid.Value.AsGuid() );
                            if ( parentGroup != null )
                            {
                                // Use a name match to find the column since there are multiple dynamic columns 
                                var columnIndex = 0;
                                foreach ( DataControlFieldCell cell in e.Row.Cells )
                                {
                                    if ( parentGroup.Name == cell.ContainingField.HeaderText || parentGroup.GroupType.Name == cell.ContainingField.HeaderText )
                                    {
                                        break;
                                    }
                                    columnIndex++;
                                }

                                var groupAssignments = e.Row.Cells[columnIndex];
                                var lGroupExport = e.Row.FindControl( string.Format("lAssignments_{0}", groupType.Id ) ) as Literal;
                                var btnGroupAssignment = groupAssignments.Controls.Count > 0 ? groupAssignments.Controls[0] as LinkButton : null;
                                if ( btnGroupAssignment != null )
                                {
                                    if ( parentGroup.Groups.Any() )
                                    {
                                        if ( parentGroup.GroupType.Attributes == null || !parentGroup.GroupType.Attributes.Any() )
                                        {
                                            parentGroup.GroupType.LoadAttributes( rockContext );
                                        }

                                        var subGroupIds = parentGroup.Groups.Select( g => g.Id ).ToList();
                                        var groupMemberships = new GroupMemberService( rockContext ).Queryable().AsNoTracking()
                                            .Where( m => m.PersonId == registrant.PersonId && subGroupIds.Contains( m.GroupId ) ).ToList();
                                        if ( !groupMemberships.Any() )
                                        {
                                            btnGroupAssignment.CssClass = "btn-add btn btn-default btn-sm";

                                            using ( var literalControl = new LiteralControl( "<i class='fa fa-plus-circle'></i>" ) )
                                            {
                                                btnGroupAssignment.Controls.Add( literalControl );
                                            }
                                            using ( var literalControl = new LiteralControl( "<span class='grid-btn-assign-text'> Assign</span>" ) )
                                            {
                                                btnGroupAssignment.Controls.Add( literalControl );
                                            }

                                            btnGroupAssignment.CommandName = "AssignSubGroup";
                                            btnGroupAssignment.CommandArgument = string.Format( "{0}|{1}", parentGroup.Id.ToString(), registrant.Id.ToString() );
                                        }
                                        else if ( parentGroup.GroupType.GetAttributeValue( "AllowMultipleRegistrations" ).AsBoolean() )
                                        {
                                            var subGroupControls = e.Row.Cells[columnIndex].Controls;
                                            var groupExportText = new List<string>();

                                            // add any group memberships
                                            foreach ( var member in groupMemberships )
                                            {
                                                using ( var subGroupButton = new LinkButton() )
                                                {
                                                    // TODO: Y U NO WORK?
                                                    //subGroupButton.Command += new CommandEventHandler( MultipleSubGroup_Click );
                                                    //btnGroupAssignment.CommandName = "ChangeSubGroup";
                                                    //btnGroupAssignment.CommandArgument = member.Id.ToString();

                                                    subGroupButton.OnClientClick = "javascript: __doPostBack( 'btnMultipleRegistrations', 'select-subgroup:" + member.Id + "' ); return false;";
                                                    subGroupButton.Text = " " + member.Group.Name + "  ";
                                                    subGroupControls.AddAt( 0, subGroupButton );
                                                }
                                                groupExportText.Add( member.Group.Name );
                                            }

                                            if ( lGroupExport != null )
                                            {
                                                lGroupExport.Text = string.Join(",", groupExportText);
                                            }

                                            using ( var literalControl = new LiteralControl( "<i class='fa fa-plus-circle'></i>" ) )
                                            {
                                                btnGroupAssignment.Controls.Add( literalControl );
                                            }

                                            // TODO: figure out why command args aren't passing
                                            btnGroupAssignment.OnClientClick = string.Format( "javascript: __doPostBack( 'btnMultipleRegistrations', 'select-subgroup:{0}|{1}' ); return false;", parentGroup.Id, registrant.Id );
                                            btnGroupAssignment.CssClass = "btn-add btn btn-default btn-sm";
                                            //btnGroupAssignment.CommandName = "AssignSubGroup";
                                            //btnGroupAssignment.CommandArgument = string.Format( "subgroup_{0}|{1}", parentGroup.Id.ToString(), registrant.Id.ToString() );
                                        }
                                        else
                                        {
                                            var groupMember = groupMemberships.FirstOrDefault();
                                            btnGroupAssignment.Text = groupMember.Group.Name;
                                            btnGroupAssignment.CommandName = "ChangeSubGroup";
                                            btnGroupAssignment.CommandArgument = groupMember.Id.ToString();

                                            if ( lGroupExport != null )
                                            {
                                                lGroupExport.Text = groupMember.Group.Name;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using ( var literalControl = new LiteralControl( "<i class='fa fa-minus-circle'></i>" ) )
                                        {
                                            btnGroupAssignment.Controls.Add( literalControl );
                                        }
                                        using ( var literalControl = new LiteralControl( string.Format( "<span class='grid-btn-assign-text'> No {0}</span>", parentGroup.GroupType.GroupTerm.Pluralize() ) ) )
                                        {
                                            btnGroupAssignment.Controls.Add( literalControl );
                                        }
                                        btnGroupAssignment.Enabled = false;
                                    }
                                }
                            }
                        }
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
            if ( e.CommandName == "AssignSubGroup" )
            {
                var argument = e.CommandArgument.ToString().Split( '|' ).ToList();
                var parentGroupId = 0;
                var registrantId = 0;
                int.TryParse( argument[0], out parentGroupId );
                int.TryParse( argument[1], out registrantId );
                if ( parentGroupId > 0 && registrantId > 0 )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var groupService = new GroupService( rockContext );
                        var registrantService = new RegistrationRegistrantService( rockContext );
                        var parentGroup = groupService.Get( parentGroupId );
                        var registrant = registrantService.Get( registrantId );
                        if ( registrant != null )
                        {
                            RenderGroupMemberModal( rockContext, parentGroup, null, null, registrant );
                        }
                    }
                }
            }

            if ( e.CommandName == "ChangeSubGroup" )
            {
                var subGroupMemberId = 0;
                if ( int.TryParse( e.CommandArgument.ToString(), out subGroupMemberId ) && subGroupMemberId > 0 )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var groupMemberService = new GroupMemberService( rockContext );
                        var groupMember = groupMemberService.Get( subGroupMemberId );
                        RenderGroupMemberModal( rockContext, groupMember.Group.ParentGroup, groupMember.Group, groupMember, null );
                    }
                }
            }
            if ( hfRegistrationInstanceId.Value.AsInteger() > 0 )
            {
                BindRegistrantsFilter( new RegistrationInstanceService( new RockContext() ).Get( hfRegistrationInstanceId.Value.AsInteger() ) );
            }
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
                    // TODO: is registration instance id already present?
                    var registrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsIntegerOrNull();

                    string errorMessage;
                    if ( !registrantService.CanDelete( registrant, out errorMessage ) )
                    {
                        mdRegistrantsGridWarning.Show( errorMessage, ModalAlertType.Information );
                        return;
                    }

                    // remove registrant from any associated groups
                    var memberService = new GroupMemberService( rockContext );
                    var deleteMembers = new List<GroupMember>();
                    foreach ( var groupType in ResourceGroupTypes )
                    {
                        deleteMembers.AddRange( GetDeleteGroupMembers( rockContext, memberService, groupType, registrant, registrationInstanceId ) );
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
            fPayments.SaveUserPreference( "Payments Date Range", "Transaction Date Range", sdrpPaymentDateRange.DelimitedValues );

            BindPaymentsGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the fPayments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fPayments_ClearFilterClick( object sender, EventArgs e )
        {
            fPayments.DeleteUserPreferences();
            BindPaymentsFilter();
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
                case "Payments Date Range":
                    {
                        e.Value = SlidingDateRangePicker.FormatDelimitedValues( e.Value );
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
        /// Handles the ClearFilterClick event of the fLinkages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fLinkages_ClearFilterClick( object sender, EventArgs e )
        {
            fLinkages.DeleteUserPreferences();
            BindLinkagesFilter();
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
        protected void lbGroupPlace_Click( object sender, EventArgs e )
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
                        .Where( g => groupIds.Contains( g.Id ) ).ToList() )
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

                                if ( cbSetGroupAttributes.Checked )
                                {
                                    registrant.LoadAttributes( rockContext );
                                    groupMember.LoadAttributes( rockContext );
                                    foreach ( var attr in groupMember.Attributes.Where( m => registrant.Attributes.Keys.Contains( m.Key ) ) )
                                    {
                                        groupMember.SetAttributeValue( attr.Key, registrant.GetAttributeValue( attr.Key ) );
                                    }
                                }

                                rockContext.SaveChanges();
                                groupMember.SaveAttributeValues( rockContext );
                            }
                        }
                    }
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
            RegistrationInstance instance = RockPage.GetSharedItem( key ) as RegistrationInstance;
            if ( instance == null )
            {
                rockContext = rockContext ?? new RockContext();
                instance = new RegistrationInstanceService( rockContext )
                    .Queryable( "RegistrationTemplate,Account,RegistrationTemplate.Forms.Fields" )
                    .AsNoTracking()
                    .FirstOrDefault( i => i.Id == registrationInstanceId );
                instance.LoadAttributes( rockContext );

                // refresh local copy of instance attributes
                ResourceGroups = instance.AttributeValues;

                RockPage.SaveSharedItem( key, instance );
            }

            return instance;
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemId">The item id value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void ShowDetail( int itemId )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail()
        {
            var registrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsIntegerOrNull();
            var parentTemplateId = PageParameter( "RegistrationTemplateId" ).AsIntegerOrNull();

            if ( !registrationInstanceId.HasValue )
            {
                pnlDetails.Visible = false;
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                RegistrationInstance instance = null;
                if ( registrationInstanceId.HasValue )
                {
                    instance = GetRegistrationInstance( registrationInstanceId.Value, rockContext );
                }

                if ( instance == null )
                {
                    instance = new RegistrationInstance
                    {
                        Id = 0,
                        IsActive = true,
                        RegistrationTemplateId = parentTemplateId ?? 0
                    };

                    var accountGuid = GetAttributeValue( "DefaultAccount" ).AsGuidOrNull();
                    if ( accountGuid.HasValue )
                    {
                        var account = new FinancialAccountService( rockContext ).Get( accountGuid.Value );
                        instance.AccountId = account != null ? account.Id : 0;
                    }
                }

                if ( instance.RegistrationTemplate == null && instance.RegistrationTemplateId > 0 )
                {
                    instance.RegistrationTemplate = new RegistrationTemplateService( rockContext )
                        .Get( instance.RegistrationTemplateId );
                }

                hlType.Visible = instance.RegistrationTemplate != null;
                hlType.Text = instance.RegistrationTemplate != null ? instance.RegistrationTemplate.Name : string.Empty;

                lWizardTemplateName.Text = hlType.Text;
                
                pnlDetails.Visible = true;
                hfRegistrationInstanceId.Value = instance.Id.ToString();
                SetHasPayments( instance.Id, rockContext );

                FollowingsHelper.SetFollowing( instance, pnlFollowing, this.CurrentPerson );

                // render UI based on Authorized
                var readOnly = false;

                var canEdit = UserCanEdit ||
                    instance.IsAuthorized( Authorization.EDIT, CurrentPerson ) ||
                    instance.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );

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

                    bool allowRegistrationEdit = instance.IsAuthorized( "Register", CurrentPerson );
                    gRegistrations.Actions.ShowAdd = allowRegistrationEdit;
                    gRegistrations.IsDeleteEnabled = allowRegistrationEdit;
                    ShowReadonlyDetails( instance );
                }
                else
                {
                    btnEdit.Visible = true;
                    btnDelete.Visible = true;

                    if ( instance.Id > 0 )
                    {
                        ShowReadonlyDetails( instance );
                    }
                    else
                    {
                        ShowEditDetails( instance, rockContext );
                    }
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

                BuildRegistrationGroupHierarchy( rockContext, instance );
                BuildSubGroupTabs( instance );

                // TODO: are all of these still necessary?
                LoadRegistrantFormFields( instance );
                BindRegistrationsFilter();
                BindRegistrantsFilter( instance );
                BindGroupPlacementsFilter( instance );
                BindLinkagesFilter();
                AddDynamicControls( true );

                // do the ShowTab now since it may depend on DynamicControls and Filter Bindings
                ShowTab();
            }
        }

        /// <summary>
        /// Sets the following on postback.
        /// </summary>
        private void SetFollowingOnPostback()
        {
            var registrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsIntegerOrNull();
            if ( registrationInstanceId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var instance = GetRegistrationInstance( registrationInstanceId.Value, rockContext );
                    if ( instance != null )
                    {
                        FollowingsHelper.SetFollowing( instance, pnlFollowing, this.CurrentPerson );
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

            pnlSubGroups.Visible = ResourceGroupTypes.Any();

            SetEditMode( true );

            rieDetails.SetValue( instance );
        }

        /// <summary>
        /// Shows the readonly details.
        /// </summary>
        /// <param name="instance">The registration template.</param>
        /// <param name="setTab">if set to <c>true</c> [set tab].</param>
        private void ShowReadonlyDetails( RegistrationInstance instance, bool setTab = true )
        {
            SetEditMode( false );

            hfRegistrationInstanceId.SetValue( instance.Id );

            lReadOnlyTitle.Text = instance.Name.FormatAsHtmlTitle();
            hlInactive.Visible = instance.IsActive == false;

            lWizardInstanceName.Text = instance.Name;
            lName.Text = instance.Name;

            if ( instance.RegistrationTemplate.SetCostOnInstance ?? false )
            {
                lCost.Text = instance.Cost.FormatAsCurrency();
                lMinimumInitialPayment.Visible = instance.MinimumInitialPayment.HasValue;
                lMinimumInitialPayment.Text = instance.MinimumInitialPayment.HasValue ? instance.MinimumInitialPayment.Value.FormatAsCurrency() : "";
            }
            else
            {
                lCost.Visible = false;
                lMinimumInitialPayment.Visible = false;
            }

            lAccount.Visible = instance.Account != null;
            lAccount.Text = instance.Account != null ? instance.Account.Name : "";

            lMaxAttendees.Visible = instance.MaxAttendees > 0;
            lMaxAttendees.Text = instance.MaxAttendees.ToString( "N0" );
            lWorkflowType.Text = instance.RegistrationWorkflowType != null ?
                instance.RegistrationWorkflowType.Name : string.Empty;
            lWorkflowType.Visible = !string.IsNullOrWhiteSpace( lWorkflowType.Text );

            lDetails.Visible = !string.IsNullOrWhiteSpace( instance.Details );
            lDetails.Text = instance.Details;

            liGroupPlacement.Visible = instance.RegistrationTemplate.AllowGroupPlacement;

            var groupId = GetUserPreference( string.Format( "ParentGroup_{0}_{1}", BlockId, instance.Id ) ).AsIntegerOrNull();
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

            if ( setTab )
            {
                ShowTab();
            }
        }

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        private void SetEditMode( bool editable )
        {
            pnlEditDetails.Visible = editable;
            fieldsetViewDetails.Visible = !editable;
            pnlTabs.Visible = !editable;
        }

        /// <summary>
        /// Builds the sub group tabs.
        /// </summary>
        /// <param name="instance">The instance.</param>
        private void BuildSubGroupTabs( RegistrationInstance instance = null )
        {
            phGroupTabs.Controls.Clear();
            instance = instance ?? GetRegistrationInstance( PageParameter( "RegistrationInstanceId" ).AsInteger() );
            if ( instance != null )
            {
                if ( instance.Attributes == null )
                {
                    instance.LoadAttributes();
                }

                foreach ( var groupType in ResourceGroupTypes.Where( gt => instance.AttributeValues.ContainsKey( gt.Name ) ) )
                {
                    var associatedGroupGuid = instance.AttributeValues[groupType.Name].Value.AsGuid();
                    if ( !associatedGroupGuid.Equals( Guid.Empty ) )
                    {
                        var tabName = groupType.Name;

                        var group = new GroupService( new RockContext() ).Get( associatedGroupGuid );
                        if ( group != null )
                        {
                            tabName = group.Name;
                        }

                        var item = new HtmlGenericControl( "li" )
                        {
                            ID = "li" + tabName
                        };
                        var lb = new LinkButton
                        {
                            ID = "lb" + tabName,
                            Text = tabName
                        };

                        lb.Click += lbTab_Click;
                        item.Controls.Add( lb );
                        phGroupTabs.Controls.Add( item );
                    }
                }
            }
        }

        /// <summary>
        /// Shows the tab.
        /// </summary>
        private void ShowTab()
        {
            // only show active tab
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

            // set custom placement groups
            HtmlGenericControl liAssociatedGroup;
            var instance = GetRegistrationInstance( PageParameter( "RegistrationInstanceId" ).AsInteger() );
            foreach ( var groupType in ResourceGroupTypes )
            {
                if ( groupType.GetAttributeValue( "ShowOnGrid" ).AsBoolean( true ) && instance != null && instance.AttributeValues.ContainsKey( groupType.Name ) )
                {
                    var resourceGroupGuid = instance.AttributeValues[groupType.Name];
                    if ( resourceGroupGuid != null && !string.IsNullOrWhiteSpace( resourceGroupGuid.Value ) && !Guid.Empty.Equals( resourceGroupGuid.Value.AsGuid() ) )
                    {
                        var tabName = groupType.Name.RemoveSpecialCharacters();
                        var parentGroup = new GroupService( new RockContext() ).Get( resourceGroupGuid.Value.AsGuid() );
                        if ( parentGroup != null )
                        {
                            hfActiveTabParentGroup.Value = parentGroup.Guid.ToString();
                            tabName = parentGroup.Name;
                        }

                        liAssociatedGroup = new HtmlGenericControl();
                        liAssociatedGroup = (HtmlGenericControl)ulTabs.FindControl( "li" + tabName );
                        if ( liAssociatedGroup != null )
                        {
                            liAssociatedGroup.RemoveCssClass( "active" );
                        }
                        if ( ActiveTab == "lb" + tabName )
                        {
                            liAssociatedGroup.AddCssClass( "active" );
                        }
                    }
                }
            }

            BindResourcePanels();

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
                        cbSetGroupAttributes.Checked = true;
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
            sdrpRegistrationDateRange.DelimitedValues = fRegistrations.GetUserPreference( "Registrations Date Range" );
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
            if ( instanceId.HasValue && instanceId > 0 )
            {
                using ( var rockContext = new RockContext() )
                {
                    var registrationEntityType = EntityTypeCache.Read( typeof( Rock.Model.Registration ) );

                    var instance = new RegistrationInstanceService( rockContext ).Get( instanceId.Value );
                    if ( instance != null )
                    {
                        decimal cost = instance.RegistrationTemplate.Cost;
                        if ( instance.RegistrationTemplate.SetCostOnInstance ?? false )
                        {
                            cost = instance.Cost ?? 0.0m;
                        }
                        _instanceHasCost = cost > 0.0m;
                    }

                    var qry = new RegistrationService( rockContext )
                        .Queryable( "PersonAlias.Person,Registrants.PersonAlias.Person,Registrants.Fees.RegistrationTemplateFee" )
                        .AsNoTracking()
                        .Where( r =>
                            r.RegistrationInstanceId == instanceId.Value &&
                            !r.IsTemporary );

                    var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( sdrpRegistrationDateRange.DelimitedValues );

                    if ( dateRange.Start.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                                                        r.CreatedDateTime.Value >= dateRange.Start.Value );
                    }
                    if ( dateRange.End.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value < dateRange.End.Value );
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
                                DiscountCosts = r.Registrants.Sum( p => (decimal?)( p.DiscountedCost( r.DiscountPercentage, r.DiscountAmount ) ) ) ?? 0.0m,
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
            sdrpRegistrantsRegistrantDateRange.DelimitedValues = fRegistrants.GetUserPreference( "Registrants Date Range" );
            tbRegistrantFirstName.Text = fRegistrants.GetUserPreference( "First Name" );
            tbRegistrantLastName.Text = fRegistrants.GetUserPreference( "Last Name" );
            ddlInGroup.SetValue( fRegistrants.GetUserPreference( "In Group" ) );

            ddlRegistrantsSignedDocument.SetValue( fRegistrants.GetUserPreference( "Signed Document" ) );
            ddlRegistrantsSignedDocument.Visible = instance != null && instance.RegistrationTemplate != null && instance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue;
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
                    var instance = GetRegistrationInstance( instanceId.Value );

                    if ( instance != null &&
                        instance.RegistrationTemplate != null &&
                        instance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue )
                    {
                        Signers = new SignatureDocumentService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( d =>
                                d.SignatureDocumentTemplateId == instance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.Value &&
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
                    var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( sdrpRegistrantsRegistrantDateRange.DelimitedValues );
                    if ( dateRange.Start.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value >= dateRange.Start.Value );
                    }
                    if ( dateRange.End.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value < dateRange.End.Value );
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
                        if ( ddlRegistrantsSignedDocument.SelectedValue.AsBooleanOrNull() == true )
                        {
                            qry = qry.Where( r => Signers.Contains( r.PersonAlias.PersonId ) );
                        }
                        else if ( ddlRegistrantsSignedDocument.SelectedValue.AsBooleanOrNull() == false )
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

                                        var ddlCampus = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsCampus" ) as RockDropDownList;
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
                                        var tbEmailFilter = phRegistrantFormFieldFilters.FindControl( "tbRegistrantsEmailFilter" ) as RockTextBox;
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
                                        var drpBirthdateFilter = phRegistrantFormFieldFilters.FindControl( "drpRegistrantsBirthdateFilter" ) as DateRangePicker;
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
                                        var gpGradeFilter = phRegistrantFormFieldFilters.FindControl( "gpRegistrantsGradeFilter" ) as GradePicker;
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
                                        var ddlGenderFilter = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsGenderFilter" ) as RockDropDownList;
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
                                        var ddlMaritalStatusFilter = phRegistrantFormFieldFilters.FindControl( "ddlRegistrantsMaritalStatusFilter" ) as RockDropDownList;
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
                                        var tbPhoneFilter = phRegistrantFormFieldFilters.FindControl( "tbRegistrantsPhoneFilter" ) as RockTextBox;
                                        if ( tbPhoneFilter != null && !string.IsNullOrWhiteSpace( tbPhoneFilter.Text ) )
                                        {
                                            var numericPhone = tbPhoneFilter.Text.AsNumeric();
                                            if ( !string.IsNullOrEmpty( numericPhone ) )
                                            {
                                                var phoneNumberPersonIdQry = new PhoneNumberService( rockContext ).Queryable().Where( a => a.Number.Contains( numericPhone ) ).
                                                    Select( a => a.PersonId );

                                                qry = qry.Where( r => phoneNumberPersonIdQry.Contains( r.PersonAlias.PersonId ) );
                                            }
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

                    // Filter by fees
                    var nreFeeFilter = phRegistrantFormFieldFilters.FindControl( "nreRegistrantsFeeFilter" ) as NumberRangeEditor;
                    if ( nreFeeFilter != null )
                    {
                        if ( nreFeeFilter.LowerValue.HasValue )
                        {
                            qry = qry.Where( r => r.Fees.Any( f => f.Cost >= nreFeeFilter.LowerValue.Value ) );
                        }

                        if ( nreFeeFilter.UpperValue.HasValue )
                        {
                            qry = qry.Where( r => r.Fees.Any( f => f.Cost <= nreFeeFilter.UpperValue.Value ) );
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
                    gRegistrants.SetLinqDataSource( orderedQry );

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


                    var firstResourceColumn = gRegistrants.Columns.OfType<LinkButtonField>().FirstOrDefault();
                    var columnIndex = gRegistrants.Columns.IndexOf( firstResourceColumn ).ToString().AsInteger();

                    // Build subgroup button grid
                    //BuildGroupAssignmentGrid( gRegistrants, columnIndex );


                    gRegistrants.DataBind();
                }
            }
        }

        /// <summary>
        /// Gets all of the form fields that were configured as 'Show on Grid' for the registration template
        /// </summary>
        /// <param name="instance">The registration instance.</param>
        private void LoadRegistrantFormFields( RegistrationInstance instance )
        {
            RegistrantFields = new List<RegistrantFormField>();

            if ( instance != null )
            {
                foreach ( var form in instance.RegistrationTemplate.Forms )
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
        /// Adds the filter controls and grid columns for all of the registration template's form fields
        /// that were configured to 'Show on Grid'
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        /// <param name="instance"></param>
        private void AddDynamicControls( bool setValues, RegistrationInstance instance = null )
        {
            phRegistrantFormFieldFilters.Controls.Clear();
            phGroupPlacementsFormFieldFilters.Controls.Clear();
            var allGrids = this.ControlsOfTypeRecursive<Grid>().ToList();

            // Remove any of the dynamic person fields
            var dynamicColumns = new List<string> {
                "PersonAlias.Person.BirthDate",
            };
            foreach ( var column in gRegistrants.Columns
                .OfType<BoundField>()
                .Where( c => dynamicColumns.Contains( c.DataField ) )
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
            }

            // Remove any of the dynamic attribute fields
            foreach ( var column in gRegistrants.Columns
                .OfType<AttributeField>()
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
            }

            // Remove the fees field
            foreach ( var column in gRegistrants.Columns
                .OfType<TemplateField>()
                .Where( c => c.HeaderText == "Fees" )
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
            }

            // Remove the delete field
            foreach ( var column in gRegistrants.Columns
                .OfType<DeleteField>()
                .ToList() )
            {
                gRegistrants.Columns.Remove( column );
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
                                    var ddlRegistrantsCampus = new RockDropDownList
                                    {
                                        ID = "ddlRegistrantsCampus",
                                        Label = "Home Campus",
                                        DataValueField = "Id",
                                        DataTextField = "Name",
                                        DataSource = CampusCache.All()
                                    };
                                    ddlRegistrantsCampus.DataBind();
                                    ddlRegistrantsCampus.Items.Insert( 0, new ListItem( "", "" ) );

                                    if ( setValues )
                                    {
                                        ddlRegistrantsCampus.SetValue( fRegistrants.GetUserPreference( "Home Campus" ) );
                                    }

                                    phRegistrantFormFieldFilters.Controls.Add( ddlRegistrantsCampus );

                                    var ddlGroupPlacementsCampus = new RockDropDownList
                                    {
                                        ID = "ddlGroupPlacementsCampus",
                                        Label = "Home Campus",
                                        DataValueField = "Id",
                                        DataTextField = "Name",
                                        DataSource = CampusCache.All()
                                    };
                                    ddlGroupPlacementsCampus.DataBind();
                                    ddlGroupPlacementsCampus.Items.Insert( 0, new ListItem( "", "" ) );

                                    if ( setValues )
                                    {
                                        ddlGroupPlacementsCampus.SetValue( fGroupPlacements.GetUserPreference( "GroupPlacements-Home Campus" ) );
                                    }

                                    phGroupPlacementsFormFieldFilters.Controls.Add( ddlGroupPlacementsCampus );

                                    var lRegistrantsCampus = new RockLiteralField
                                    {
                                        ID = "lRegistrantsCampus",
                                        HeaderText = "Campus"
                                    };
                                    gRegistrants.Columns.Add( lRegistrantsCampus );

                                    var lGroupPlacementsCampus = new RockLiteralField
                                    {
                                        ID = "lGroupPlacementsCampus",
                                        HeaderText = "Campus"
                                    };
                                    gGroupPlacements.Columns.Add( lGroupPlacementsCampus );

                                    break;
                                }

                            case RegistrationPersonFieldType.Email:
                                {
                                    var tbRegistrantsEmailFilter = new RockTextBox
                                    {
                                        ID = "tbRegistrantsEmailFilter",
                                        Label = "Email"
                                    };

                                    if ( setValues )
                                    {
                                        tbRegistrantsEmailFilter.Text = fRegistrants.GetUserPreference( "Email" );
                                    }

                                    phRegistrantFormFieldFilters.Controls.Add( tbRegistrantsEmailFilter );

                                    var tbGroupPlacementsEmailFilter = new RockTextBox
                                    {
                                        ID = "tbGroupPlacementsEmailFilter",
                                        Label = "Email"
                                    };
                                    if ( setValues )
                                    {
                                        tbGroupPlacementsEmailFilter.Text = fGroupPlacements.GetUserPreference( "Email" );
                                    }

                                    phGroupPlacementsFormFieldFilters.Controls.Add( tbGroupPlacementsEmailFilter );

                                    string dataFieldExpression = "PersonAlias.Person.Email";
                                    var emailField = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Email",
                                        SortExpression = dataFieldExpression
                                    };
                                    gRegistrants.Columns.Add( emailField );

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
                                    var drpRegistrantsBirthdateFilter = new DateRangePicker
                                    {
                                        ID = "drpRegistrantsBirthdateFilter",
                                        Label = "Birthdate Range"
                                    };

                                    if ( setValues )
                                    {
                                        drpRegistrantsBirthdateFilter.DelimitedValues = fRegistrants.GetUserPreference( "Birthdate Range" );
                                    }

                                    phRegistrantFormFieldFilters.Controls.Add( drpRegistrantsBirthdateFilter );

                                    var drpGroupPlacementsBirthdateFilter = new DateRangePicker
                                    {
                                        ID = "drpGroupPlacementsBirthdateFilter",
                                        Label = "Birthdate Range"
                                    };

                                    if ( setValues )
                                    {
                                        drpGroupPlacementsBirthdateFilter.DelimitedValues = fGroupPlacements.GetUserPreference( "GroupPlacements-Birthdate Range" );
                                    }

                                    phGroupPlacementsFormFieldFilters.Controls.Add( drpGroupPlacementsBirthdateFilter );

                                    string dataFieldExpression = "PersonAlias.Person.BirthDate";
                                    var birthdateField = new DateField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Birthdate",
                                        SortExpression = dataFieldExpression
                                    };
                                    gRegistrants.Columns.Add( birthdateField );
                                    

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
                                    var gpRegistrantsGradeFilter = new GradePicker
                                    {
                                        ID = "gpRegistrantsGradeFilter",
                                        Label = "Grade",
                                        UseAbbreviation = true,
                                        UseGradeOffsetAsValue = true,
                                        CssClass = "input-width-md"
                                    };
                                    // Since 12th grade is the 0 Value, we need to handle the "no user preference" differently
                                    // by not calling SetValue otherwise it will select 12th grade.
                                    if ( setValues )
                                    {
                                        var registrantsGradeUserPreference = fRegistrants.GetUserPreference( "Grade" ).AsIntegerOrNull();
                                        if ( registrantsGradeUserPreference != null )
                                        {
                                            gpRegistrantsGradeFilter.SetValue( registrantsGradeUserPreference );
                                        }
                                    }

                                    phRegistrantFormFieldFilters.Controls.Add( gpRegistrantsGradeFilter );

                                    var gpGroupPlacementsGradeFilter = new GradePicker
                                    {
                                        ID = "gpGroupPlacementsGradeFilter",
                                        Label = "Grade",
                                        UseAbbreviation = true,
                                        UseGradeOffsetAsValue = true,
                                        CssClass = "input-width-md"
                                    };
                                    // Since 12th grade is the 0 Value, we need to handle the "no user preference" differently
                                    // by not calling SetValue otherwise it will select 12th grade.
                                    if ( setValues )
                                    {
                                        var groupPlacementsGradeUserPreference = fGroupPlacements.GetUserPreference( "GroupPlacements-Grade" ).AsIntegerOrNull();
                                        if ( groupPlacementsGradeUserPreference != null )
                                        {
                                            gpGroupPlacementsGradeFilter.SetValue( groupPlacementsGradeUserPreference );
                                        }
                                    }

                                    phGroupPlacementsFormFieldFilters.Controls.Add( gpGroupPlacementsGradeFilter );

                                    // 2017-01-13 as discussed, changing this to Grade but keeping the sort based on grad year
                                    string dataFieldExpression = "PersonAlias.Person.GradeFormatted";
                                    var gradeField = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Grade",
                                        SortExpression = "PersonAlias.Person.GraduationYear"
                                    };
                                    gRegistrants.Columns.Add( gradeField );
                                    

                                    var gradeField2 = new RockBoundField
                                    {
                                        DataField = dataFieldExpression,
                                        HeaderText = "Grade"
                                    };
                                    gGroupPlacements.Columns.Add( gradeField2 );

                                    break;
                                }

                            case RegistrationPersonFieldType.Gender:
                                {
                                    var ddlRegistrantsGenderFilter = new RockDropDownList();
                                    ddlRegistrantsGenderFilter.BindToEnum<Gender>( true );
                                    ddlRegistrantsGenderFilter.ID = "ddlRegistrantsGenderFilter";
                                    ddlRegistrantsGenderFilter.Label = "Gender";

                                    if ( setValues )
                                    {
                                        ddlRegistrantsGenderFilter.SetValue( fRegistrants.GetUserPreference( "Gender" ) );
                                    }

                                    phRegistrantFormFieldFilters.Controls.Add( ddlRegistrantsGenderFilter );

                                    var ddlGroupPlacementsGenderFilter = new RockDropDownList();
                                    ddlGroupPlacementsGenderFilter.BindToEnum<Gender>( true );
                                    ddlGroupPlacementsGenderFilter.ID = "ddlGroupPlacementsGenderFilter";
                                    ddlGroupPlacementsGenderFilter.Label = "Gender";

                                    if ( setValues )
                                    {
                                        ddlGroupPlacementsGenderFilter.SetValue( fGroupPlacements.GetUserPreference( "GroupPlacements-Gender" ) );
                                    }

                                    phGroupPlacementsFormFieldFilters.Controls.Add( ddlGroupPlacementsGenderFilter );

                                    string dataFieldExpression = "PersonAlias.Person.Gender";
                                    var genderField = new EnumField();
                                    genderField.DataField = dataFieldExpression;
                                    genderField.HeaderText = "Gender";
                                    genderField.SortExpression = dataFieldExpression;
                                    gRegistrants.Columns.Add( genderField );
                                    

                                    var genderField2 = new EnumField();
                                    genderField2.DataField = dataFieldExpression;
                                    genderField2.HeaderText = "Gender";
                                    genderField2.SortExpression = dataFieldExpression;
                                    gGroupPlacements.Columns.Add( genderField2 );

                                    break;
                                }

                            case RegistrationPersonFieldType.MaritalStatus:
                                {
                                    var ddlRegistrantsMaritalStatusFilter = new RockDropDownList();
                                    ddlRegistrantsMaritalStatusFilter.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ), true );
                                    ddlRegistrantsMaritalStatusFilter.ID = "ddlRegistrantsMaritalStatusFilter";
                                    ddlRegistrantsMaritalStatusFilter.Label = "Marital Status";

                                    if ( setValues )
                                    {
                                        ddlRegistrantsMaritalStatusFilter.SetValue( fRegistrants.GetUserPreference( "Marital Status" ) );
                                    }

                                    phRegistrantFormFieldFilters.Controls.Add( ddlRegistrantsMaritalStatusFilter );

                                    var ddlGroupPlacementsMaritalStatusFilter = new RockDropDownList();
                                    ddlGroupPlacementsMaritalStatusFilter.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ), true );
                                    ddlGroupPlacementsMaritalStatusFilter.ID = "ddlGroupPlacementsMaritalStatusFilter";
                                    ddlGroupPlacementsMaritalStatusFilter.Label = "Marital Status";

                                    if ( setValues )
                                    {
                                        ddlGroupPlacementsMaritalStatusFilter.SetValue( fGroupPlacements.GetUserPreference( "GroupPlacements-Marital Status" ) );
                                    }

                                    phGroupPlacementsFormFieldFilters.Controls.Add( ddlGroupPlacementsMaritalStatusFilter );

                                    string dataFieldExpression = "PersonAlias.Person.MaritalStatusValue.Value";
                                    var maritalStatusField = new RockBoundField();
                                    maritalStatusField.DataField = dataFieldExpression;
                                    maritalStatusField.HeaderText = "MaritalStatus";
                                    maritalStatusField.SortExpression = dataFieldExpression;
                                    gRegistrants.Columns.Add( maritalStatusField );
                                    

                                    var maritalStatusField2 = new RockBoundField();
                                    maritalStatusField2.DataField = dataFieldExpression;
                                    maritalStatusField2.HeaderText = "MaritalStatus";
                                    maritalStatusField2.SortExpression = dataFieldExpression;
                                    gGroupPlacements.Columns.Add( maritalStatusField2 );

                                    break;
                                }

                            case RegistrationPersonFieldType.MobilePhone:
                                {
                                    var tbRegistrantsPhoneFilter = new RockTextBox();
                                    tbRegistrantsPhoneFilter.ID = "tbRegistrantsPhoneFilter";
                                    tbRegistrantsPhoneFilter.Label = "Phone";

                                    if ( setValues )
                                    {
                                        tbRegistrantsPhoneFilter.Text = fRegistrants.GetUserPreference( "Phone" );
                                    }

                                    phRegistrantFormFieldFilters.Controls.Add( tbRegistrantsPhoneFilter );

                                    var tbGroupPlacementsPhoneFilter = new RockTextBox();
                                    tbGroupPlacementsPhoneFilter.ID = "tbGroupPlacementsPhoneFilter";
                                    tbGroupPlacementsPhoneFilter.Label = "Phone";

                                    if ( setValues )
                                    {
                                        tbGroupPlacementsPhoneFilter.Text = fGroupPlacements.GetUserPreference( "GroupPlacements-Phone" );
                                    }

                                    phGroupPlacementsFormFieldFilters.Controls.Add( tbGroupPlacementsPhoneFilter );

                                    var phoneNumbersField = new PhoneNumbersField();
                                    phoneNumbersField.DataField = "PersonAlias.Person.PhoneNumbers";
                                    phoneNumbersField.HeaderText = "Phone(s)";
                                    gRegistrants.Columns.Add( phoneNumbersField );
                                    

                                    var phoneNumbersField2 = new PhoneNumbersField();
                                    phoneNumbersField2.DataField = "PersonAlias.Person.PhoneNumbers";
                                    phoneNumbersField2.HeaderText = "Phone(s)";
                                    gGroupPlacements.Columns.Add( phoneNumbersField2 );

                                    break;
                                }
                        }
                    }
                    else if ( field.Attribute != null )
                    {
                        var attribute = field.Attribute;

                        // add dynamic filter to registrant grid
                        var registrantsControl = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                        if ( registrantsControl != null )
                        {
                            if ( registrantsControl is IRockControl )
                            {
                                var rockControl = (IRockControl)registrantsControl;
                                rockControl.Label = attribute.Name;
                                rockControl.Help = attribute.Description;
                                phRegistrantFormFieldFilters.Controls.Add( registrantsControl );
                            }
                            else
                            {
                                var wrapper = new RockControlWrapper
                                {
                                    ID = registrantsControl.ID + "_wrapper",
                                    Label = attribute.Name
                                };
                                wrapper.Controls.Add( registrantsControl );
                                phRegistrantFormFieldFilters.Controls.Add( wrapper );
                            }

                            if ( setValues )
                            {
                                var savedValue = fRegistrants.GetUserPreference( attribute.Key );
                                if ( !string.IsNullOrWhiteSpace( savedValue ) )
                                {
                                    try
                                    {
                                        var values = JsonConvert.DeserializeObject<List<string>>( savedValue );
                                        attribute.FieldType.Field.SetFilterValues( registrantsControl, attribute.QualifierValues, values );
                                    }
                                    catch { }
                                }
                            }
                        }

                        var groupPlacementsControl = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filterGroupPlacements_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                        if ( groupPlacementsControl != null )
                        {
                            if ( groupPlacementsControl is IRockControl )
                            {
                                var rockControl = (IRockControl)groupPlacementsControl;
                                rockControl.Label = attribute.Name;
                                rockControl.Help = attribute.Description;
                                phGroupPlacementsFormFieldFilters.Controls.Add( groupPlacementsControl );
                            }
                            else
                            {
                                var wrapper = new RockControlWrapper()
                                {
                                    ID = groupPlacementsControl.ID + "_wrapper",
                                    Label = attribute.Name
                                };
                                wrapper.Controls.Add( groupPlacementsControl );
                                phGroupPlacementsFormFieldFilters.Controls.Add( wrapper );
                            }

                            if ( setValues )
                            {
                                string savedValue = fRegistrants.GetUserPreference( "GroupPlacements-" + attribute.Key );
                                if ( !string.IsNullOrWhiteSpace( savedValue ) )
                                {
                                    try
                                    {
                                        var values = JsonConvert.DeserializeObject<List<string>>( savedValue );
                                        attribute.FieldType.Field.SetFilterValues( groupPlacementsControl, attribute.QualifierValues, values );
                                    }
                                    catch { }
                                }
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
                            
                            gGroupPlacements.Columns.Add( boundField2 );
                        }
                    }
                }
            }

            //// Add dynamic columns for sub groups
            if ( ResourceGroupTypes.Any() )
            {
                using ( var rockContext = new RockContext() )
                {
                    foreach ( var groupType in ResourceGroupTypes.Where( gt => gt.GetAttributeValue( "ShowOnGrid" ).AsBoolean( true ) ) )
                    {
                        if ( ResourceGroups.ContainsKey( groupType.Name ) )
                        {
                            var resourceGroupGuids = ResourceGroups[groupType.Name];
                            if ( resourceGroupGuids != null && !string.IsNullOrWhiteSpace( resourceGroupGuids.Value ) )
                            {
                                var parentGroup = new GroupService( rockContext ).Get( resourceGroupGuids.Value.AsGuid() );
                                if ( parentGroup != null )
                                {
                                    var groupAssignmentColumn = new LinkButtonField();
                                    groupAssignmentColumn.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                                    groupAssignmentColumn.HeaderStyle.CssClass = "";
                                    groupAssignmentColumn.HeaderText = parentGroup.Name;
                                    gRegistrants.Columns.Add( groupAssignmentColumn );

                                    var groupExportColumn = new RockLiteralField();
                                    groupExportColumn.ID = string.Format( "lAssignments_{0}", groupType.Id ); //"lAssignments_" + groupType.Id;
                                    groupExportColumn.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                                    groupExportColumn.HeaderStyle.CssClass = "";
                                    groupExportColumn.HeaderText = parentGroup.Name;
                                    groupExportColumn.ExcelExportBehavior = ExcelExportBehavior.AlwaysInclude;
                                    groupExportColumn.Visible = false;
                                    gRegistrants.Columns.Add( groupExportColumn );
                                }
                            }
                        }
                    }       

                    // Add dynamic column for volunteer assignments too
                    //if ( groupType.GroupTypePurposeValue != null && groupType.GroupTypePurposeValue.Value == "Serving Area"  )
                    //{
                    //    if ( groupType.GetAttributeValue( "AllowVolunteerAssignment" ).AsBoolean( true ) )
                    //    {
                    //        gVolunteers.Columns.Add( subGroupColumn );
                    //    }
                    //} 
                }
            }

            // Add fee column and filter
            

            var nreFeeFilter = new NumberRangeEditor();
            nreFeeFilter.NumberType = ValidationDataType.Double;
            nreFeeFilter.ID = "nreRegistrantsFeeFilter";
            nreFeeFilter.Label = "Fee Amount";

            if ( setValues )
            {
                nreFeeFilter.DelimitedValues = fRegistrants.GetUserPreference( "Fee Amount" );
            }

            phRegistrantFormFieldFilters.Controls.Add( nreFeeFilter );
            
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

        #region Resource Builder Events

        /// <summary>
        /// Handles the AddGroupClick event of the ResourceArea control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ResourceAreaRow_AddGroupClick( object sender, EventArgs e )
        {
            var parentRow = sender as ResourceAreaRow;
            parentRow.Expanded = true;
            using ( var rockContext = new RockContext() )
            {
                var groupTypeService = new GroupTypeService( rockContext );
                var parentArea = groupTypeService.Get( parentRow.GroupTypeGuid );
                if ( parentArea != null )
                {
                    var checkinGroup = parentArea.Groups.FirstOrDefault( g => g.ParentGroupId == RegistrationInstanceGroupId );
                    if ( checkinGroup == null )
                    {
                        checkinGroup = new Group();
                        checkinGroup.Guid = Guid.NewGuid();
                        checkinGroup.Name = parentArea.Name;
                        checkinGroup.IsActive = true;
                        checkinGroup.IsPublic = true;
                        checkinGroup.IsSystem = false;
                        checkinGroup.Order = 0;
                        checkinGroup.ParentGroupId = RegistrationInstanceGroupId;
                        parentArea.Groups.Add( checkinGroup );
                        rockContext.SaveChanges();

                        GroupTypeCache.Flush( parentArea.Id );
                    }

                    SelectGroup( checkinGroup.Guid );
                }
            }
        }

        /// <summary>
        /// Handles the AddGroupClick event of the ResourceGroupRow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ResourceGroupRow_AddGroupClick( object sender, EventArgs e )
        {
            var parentRow = sender as ResourceGroupRow;
            parentRow.Expanded = true;
            using ( var rockContext = new RockContext() )
            {
                var groupService = new GroupService( rockContext );
                var parentGroup = groupService.Get( parentRow.GroupGuid );
                if ( parentGroup != null )
                {
                    Guid newGuid = Guid.NewGuid();

                    var checkinGroup = new Group();
                    checkinGroup.Guid = newGuid;
                    checkinGroup.GroupTypeId = parentGroup.GroupTypeId;
                    checkinGroup.Name = "New Group";
                    checkinGroup.IsActive = true;
                    checkinGroup.IsPublic = true;
                    checkinGroup.IsSystem = false;
                    checkinGroup.Order = parentGroup.Groups.Any() ? parentGroup.Groups.Max( t => t.Order ) + 1 : 0;
                    checkinGroup.ParentGroupId = parentGroup.Id;
                    groupService.Add( checkinGroup );

                    rockContext.SaveChanges();

                    SelectGroup( newGuid );
                }
            }
        }

        /// <summary>
        /// Handles the DeleteGroupClick event of the ResourceGroupRow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ResourceGroupRow_DeleteGroupClick( object sender, EventArgs e )
        {
            var row = sender as ResourceGroupRow;

            using ( var rockContext = new RockContext() )
            {
                var groupService = new GroupService( rockContext );
                var group = groupService.Get( row.GroupGuid );
                if ( group != null )
                {
                    string errorMessage;
                    if ( !groupService.CanDelete( group, out errorMessage ) )
                    {
                        nbDeleteWarning.Text = "WARNING - Cannot Delete: " + errorMessage;
                        nbDeleteWarning.Visible = true;
                        return;
                    }

                    groupService.Delete( group );
                    rockContext.SaveChanges();

                    SelectGroup( null );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnGroupSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnResourceSave_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                if ( resourceAreaPanel.Visible )
                {
                    var groupTypeService = new GroupTypeService( rockContext );
                    var groupType = groupTypeService.Get( resourceAreaPanel.GroupTypeGuid );
                    if ( groupType != null )
                    {
                        resourceAreaPanel.GetGroupTypeValues( groupType );

                        // make sure child groups can be created
                        if ( !groupType.ChildGroupTypes.Contains( groupType ) )
                        {
                            groupType.ChildGroupTypes.Add( groupType );
                        }

                        if ( groupType.IsValid )
                        {
                            rockContext.SaveChanges();
                            groupType.SaveAttributeValues( rockContext );

                            // Make sure default role is set
                            if ( !groupType.DefaultGroupRoleId.HasValue && groupType.Roles.Any() )
                            {
                                groupType.DefaultGroupRoleId = groupType.Roles.First().Id;
                            }

                            rockContext.SaveChanges();

                            GroupTypeCache.Flush( groupType.Id );
                            nbSaveSuccess.Visible = true;
                        }
                        else
                        {
                            nbSaveSuccess.Visible = false;
                            ShowInvalidResults( groupType.ValidationResults );
                        }
                    }
                }

                if ( resourceGroupPanel.Visible )
                {
                    var groupService = new GroupService( rockContext );
                    var group = groupService.Get( resourceGroupPanel.GroupGuid );
                    if ( group != null )
                    {
                        group.LoadAttributes( rockContext );
                        resourceGroupPanel.GetGroupValues( group );

                        // add requirements to child groups
                        foreach ( var requirement in group.ParentGroup.GroupRequirements )
                        {
                            group.GroupRequirements.Add( new GroupRequirement
                            {
                                GroupId = group.Id,
                                GroupRoleId = group.GroupType.DefaultGroupRoleId,
                                GroupRequirementTypeId = requirement.GroupRequirementTypeId
                            } );
                        }

                        // make sure child groups can be created
                        if ( !group.GroupType.ChildGroupTypes.Contains( group.GroupType ) )
                        {
                            group.GroupType.ChildGroupTypes.Add( group.GroupType );
                        }

                        if ( group.IsValid )
                        {
                            rockContext.SaveChanges();
                            group.SaveAttributeValues( rockContext );
                            nbSaveSuccess.Visible = true;
                        }
                        else
                        {
                            nbSaveSuccess.Visible = false;
                            ShowInvalidResults( group.ValidationResults );
                        }
                    }
                }
            }

            // rebuilds the group display on change
            BuildResourcesInterface();
        }

        /// <summary>
        /// Handles the Click event of the btnResourceDelete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnResourceDelete_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                if ( resourceAreaPanel.Visible )
                {
                    var groupTypeService = new GroupTypeService( rockContext );
                    var groupType = groupTypeService.Get( resourceAreaPanel.GroupTypeGuid );
                    if ( groupType != null )
                    {
                        if ( IsInheritedGroupTypeRecursive( groupType, groupTypeService ) )
                        {
                            nbDeleteWarning.Text = "WARNING - Cannot delete. This group type or one of its child group types is assigned as an inherited group type.";
                            nbDeleteWarning.Visible = true;
                            return;
                        }

                        string errorMessage;
                        if ( !groupTypeService.CanDelete( groupType, out errorMessage ) )
                        {
                            nbDeleteWarning.Text = "WARNING - Cannot Delete: " + errorMessage;
                            nbDeleteWarning.Visible = true;
                            return;
                        }

                        int oldGroupTypeId = groupType.Id;

                        groupType.ParentGroupTypes.Clear();
                        groupType.ChildGroupTypes.Clear();
                        groupTypeService.Delete( groupType );
                        rockContext.SaveChanges();
                        GroupTypeCache.Flush( oldGroupTypeId );
                        Rock.CheckIn.KioskDevice.FlushAll();
                    }
                    SelectArea( null );
                }

                if ( resourceGroupPanel.Visible )
                {
                    var groupService = new GroupService( rockContext );
                    var group = groupService.Get( resourceGroupPanel.GroupGuid );
                    if ( group != null )
                    {
                        string errorMessage;
                        if ( !groupService.CanDelete( group, out errorMessage ) )
                        {
                            nbDeleteWarning.Text = "WARNING - Cannot Delete: " + errorMessage;
                            nbDeleteWarning.Visible = true;
                            return;
                        }

                        groupService.Delete( group ); //Delete if group isn't active
                        rockContext.SaveChanges();
                        SelectGroup( null );
                    }
                }
            }

            // just deleted this item, remove visibility
            btnResourceSave.Visible = false;
            btnResourceDelete.Visible = false;
            BuildResourcesInterface();
        }

        /// <summary>
        /// Determines whether [is inherited group type recursive] [the specified group type].
        /// </summary>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="groupTypeService">The group type service.</param>
        /// <param name="typesChecked">The types checked.</param>
        /// <returns>
        ///   <c>true</c> if [is inherited group type recursive] [the specified group type]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsInheritedGroupTypeRecursive( GroupType groupType, GroupTypeService groupTypeService, List<int> typesChecked = null )
        {
            // Track the groups that have been checked since group types can have themselves as a child
            typesChecked = typesChecked ?? new List<int>();
            if ( !typesChecked.Contains( groupType.Id ) )
            {
                typesChecked.Add( groupType.Id );

                if ( groupTypeService.Queryable().Any( a => a.InheritedGroupType.Guid == groupType.Guid ) )
                {
                    return true;
                }

                foreach ( var childGroupType in groupType.ChildGroupTypes.Where( t => !typesChecked.Contains( t.Id ) ) )
                {
                    if ( IsInheritedGroupTypeRecursive( childGroupType, groupTypeService, typesChecked ) )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Selects the area.
        /// </summary>
        /// <param name="groupTypeGuid">The group type unique identifier.</param>
        private void SelectArea( Guid? groupTypeGuid )
        {
            resourceAreaPanel.Visible = false;
            resourceGroupPanel.Visible = false;
            btnResourceSave.Visible = false;
            nbSaveSuccess.Visible = false;
            nbInvalid.Visible = false;

            if ( groupTypeGuid.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var groupTypeService = new GroupTypeService( rockContext );
                    var groupType = groupTypeService.Get( groupTypeGuid.Value );
                    if ( groupType != null )
                    {
                        _currentGroupTypeGuid = groupType.Guid;
                        resourceAreaPanel.SetGroupType( groupType, rockContext );

                        if ( resourceAreaPanel.EnableCheckinOptions )
                        {
                            resourceAreaPanel.CheckinLabels = new List<ResourceArea.CheckinLabelAttributeInfo>();
                            groupType.LoadAttributes( rockContext );
                            var labelAttributeKeys = ResourceArea.GetCheckinLabelAttributes( groupType.Attributes )
                                .OrderBy( a => a.Value.Order )
                                .Select( a => a.Key ).ToList();
                            var binaryFileService = new BinaryFileService( rockContext );

                            foreach ( string key in labelAttributeKeys )
                            {
                                var attributeValue = groupType.GetAttributeValue( key );
                                var binaryFileGuid = attributeValue.AsGuid();
                                var fileName = binaryFileService.Queryable().Where( a => a.Guid == binaryFileGuid ).Select( a => a.FileName ).FirstOrDefault();
                                if ( fileName != null )
                                {
                                    resourceAreaPanel.CheckinLabels.Add( new ResourceArea.CheckinLabelAttributeInfo { AttributeKey = key, BinaryFileGuid = binaryFileGuid, FileName = fileName } );
                                }
                            }
                        }

                        resourceAreaPanel.Visible = true;
                        btnResourceSave.Text = "Save Area";
                        btnResourceSave.Visible = true;
                        // don't allow delete, this depends on areas generated from grouptype inheritance
                        btnResourceDelete.Visible = false;
                        //btnDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}', '{1}');", "check-in area", "This action cannot be undone." );
                    }
                    else
                    {
                        _currentGroupTypeGuid = null;
                    }
                }
            }
            else
            {
                _currentGroupTypeGuid = null;
            }
        }

        /// <summary>
        /// Selects the group.
        /// </summary>
        /// <param name="groupGuid">The group unique identifier.</param>
        private void SelectGroup( Guid? groupGuid )
        {
            resourceAreaPanel.Visible = false;
            resourceGroupPanel.Visible = false;
            btnResourceSave.Visible = false;
            nbSaveSuccess.Visible = false;
            nbInvalid.Visible = false;

            if ( groupGuid.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var groupService = new GroupService( rockContext );
                    var group = groupService.Get( groupGuid.Value );
                    if ( group != null )
                    {
                        _currentGroupGuid = group.Guid;
                        resourceGroupPanel.SetGroup( group, rockContext );
                        resourceGroupPanel.Visible = true;

                        if ( resourceGroupPanel.EnableAddLocations )
                        {
                            var locationService = new LocationService( rockContext );
                            var locationQry = locationService.Queryable().Select( a => new { a.Id, a.ParentLocationId, a.Name } );

                            resourceGroupPanel.Locations = new List<ResourceGroup.LocationGridItem>();
                            foreach ( var groupLocation in group.GroupLocations.OrderBy( gl => gl.Order ).ThenBy( gl => gl.Location.Name ) )
                            {
                                var location = groupLocation.Location;
                                var gridItem = new ResourceGroup.LocationGridItem();
                                gridItem.LocationId = location.Id;
                                gridItem.Name = location.Name;
                                gridItem.FullNamePath = location.Name;
                                gridItem.ParentLocationId = location.ParentLocationId;
                                gridItem.Order = groupLocation.Order;

                                var parentLocationId = location.ParentLocationId;
                                while ( parentLocationId != null )
                                {
                                    var parentLocation = locationQry.FirstOrDefault( a => a.Id == parentLocationId );
                                    gridItem.FullNamePath = parentLocation.Name + " > " + gridItem.FullNamePath;
                                    parentLocationId = parentLocation.ParentLocationId;
                                }

                                resourceGroupPanel.Locations.Add( gridItem );
                            }
                        }

                        btnResourceSave.Text = "Save Group";
                        btnResourceSave.Visible = true;
                        btnResourceDelete.Visible = true;
                        btnResourceDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}', '{1}');", "resource area", "This action cannot be undone." );
                    }
                    else
                    {
                        _currentGroupGuid = null;
                    }
                }
            }
            else
            {
                _currentGroupGuid = null;
                resourceGroupPanel.CreateGroupAttributeControls( null, null );
            }
        }

        /// <summary>
        /// Loads the registration resources.
        /// </summary>
        /// <param name="instance">The instance.</param>
        private void LoadRegistrationResources( RegistrationInstance instance )
        {
            using ( var rockContext = new RockContext() )
            {
                GroupType templateGroupType = null;
                instance = instance ?? GetRegistrationInstance( PageParameter( "RegistrationInstanceId" ).AsInteger() );
                if ( instance != null )
                {
                    templateGroupType = instance.RegistrationTemplate.GroupType;
                    hfRegistrationInstanceId.Value = instance.Id.ToString();
                }
                else
                {
                    var registrationTemplateId = PageParameter( "RegistrationTemplateId" ).AsInteger();
                    if ( registrationTemplateId > 0 )
                    {
                        var registrationTemplate = new RegistrationTemplateService( rockContext ).Get( registrationTemplateId );
                        if ( registrationTemplate != null && registrationTemplate.GroupType != null )
                        {
                            templateGroupType = registrationTemplate.GroupType;
                        }
                    }
                }

                ResourceGroupTypes = new List<GroupTypeCache>();

                if ( templateGroupType != null )
                {
                    TemplateGroupTypeId = templateGroupType.Id;
                    var groupTypeIds = new GroupTypeService( rockContext ).Queryable().AsNoTracking()
                        .Where( t => t.InheritedGroupTypeId == templateGroupType.Id && t.Id != templateGroupType.Id )
                        .OrderBy( t => t.Order ).ThenBy( t => t.Name ).Select( t => t.Id ).ToList();

                    foreach ( var groupTypeId in groupTypeIds )
                    {
                        ResourceGroupTypes.Add( GroupTypeCache.Read( groupTypeId ) );
                    }
                }
            }
        }

        /// <summary>
        /// Builds the registration group hierarchy.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="instance">The instance.</param>
        private void BuildRegistrationGroupHierarchy( RockContext rockContext, RegistrationInstance instance )
        {
            if ( TemplateGroupTypeId.HasValue )
            {
                if ( instance.Attributes == null || !instance.Attributes.Any() )
                {
                    instance.LoadAttributes();
                }

                if ( instance.RegistrationTemplate == null )
                {
                    instance = GetRegistrationInstance( instance.Id, rockContext );
                }

                int? parentGroupId = null;
                var groupService = new GroupService( rockContext );
                var categoryService = new CategoryService( rockContext );
                var templateGroupType = new GroupTypeService( rockContext ).Get( (int)TemplateGroupTypeId );

                // make sure child groups can be created
                if ( !templateGroupType.ChildGroupTypes.Contains( templateGroupType ) )
                {
                    templateGroupType.ChildGroupTypes.Add( templateGroupType );
                }

                // get the template category group and save the id for use with the template
                var childCategoryGroup = groupService.GetByGroupTypeId( templateGroupType.Id ).FirstOrDefault( g => g.Name.Equals( instance.RegistrationTemplate.Category.Name ) );
                if ( childCategoryGroup == null )
                {
                    childCategoryGroup = CreateGroup( rockContext, templateGroupType, null, instance.RegistrationTemplate.Category.Name );
                }
                parentGroupId = childCategoryGroup.Id;

                // walk up the category tree to create group placeholders
                var parentCategory = instance.RegistrationTemplate.Category;
                while ( parentCategory != null )
                {
                    parentCategory = categoryService.Get( parentCategory.Guid );
                    var parentCategoryGroup = groupService.GetByGroupTypeId( templateGroupType.Id ).FirstOrDefault( g => g.Name.Equals( parentCategory.Name ) );
                    if ( parentCategoryGroup == null )
                    {
                        parentCategoryGroup = CreateGroup( rockContext, templateGroupType, null, parentCategory.Name );
                    }

                    if ( childCategoryGroup.ParentGroup == null && childCategoryGroup != parentCategoryGroup )
                    {
                        childCategoryGroup.ParentGroup = parentCategoryGroup;
                    }

                    // move up a level
                    childCategoryGroup = parentCategoryGroup;
                    parentCategory = parentCategory.ParentCategory;
                }

                // create template group
                var attributeKey = instance.RegistrationTemplate.GetType().GetFriendlyTypeName();
                var templateGroup = groupService.GetByGroupTypeId( templateGroupType.Id ).FirstOrDefault( g => g.Name.Equals( instance.RegistrationTemplate.Name ) );
                if ( templateGroup == null )
                {
                    templateGroup = CreateGroup( rockContext, templateGroupType, parentGroupId, instance.RegistrationTemplate.Name );
                    CreateRegistrationInstanceAttribute( rockContext, instance, attributeKey, templateGroup.Guid.ToString() );
                }
                else
                {
                    instance.AttributeValues[attributeKey].Value = templateGroup.Guid.ToString();
                }

                // create registration instance group
                parentGroupId = templateGroup.Id;
                attributeKey = instance.GetType().GetFriendlyTypeName();
                var instanceGroup = groupService.GetByGroupTypeId( templateGroupType.Id ).FirstOrDefault( g => g.Name.Equals( instance.Name ) && g.ParentGroupId == parentGroupId );
                if ( instanceGroup == null )
                {
                    // this might still be null if the instance doesn't have a name yet
                    instanceGroup = CreateGroup( rockContext, templateGroupType, parentGroupId, instance.Name );
                    if ( instanceGroup != null )
                    {
                        CreateRegistrationInstanceAttribute( rockContext, instance, attributeKey, instanceGroup.Guid.ToString() );
                    }
                }
                else
                {
                    instance.AttributeValues[attributeKey].Value = instanceGroup.Guid.ToString();
                }

                if ( instanceGroup != null )
                {
                    RegistrationInstanceGroupId = instanceGroup.Id;

                    // check if UI groups have been updated
                    var resourceGroups = new List<Group>();
                    foreach ( var groupRow in phRows.ControlsOfTypeRecursive<ResourceGroupRow>() )
                    {
                        resourceGroups.Add( groupService.Get( groupRow.GroupGuid ) );
                    }

                    if ( resourceGroups.Any() )
                    {
                        // assign resource groups for this registration
                        foreach ( var groupType in ResourceGroupTypes )
                        {
                            // assume group resource is unused
                            var resourceGroupGuid = Guid.Empty.ToString();
                            var resourceGroup = resourceGroups.FirstOrDefault( g => g.GroupTypeId == groupType.Id );
                            if ( resourceGroup != null )
                            {
                                // resource group is being used, link it to the instance
                                if ( resourceGroup.ParentGroupId == null )
                                {
                                    resourceGroup.ParentGroupId = RegistrationInstanceGroupId;
                                }

                                resourceGroupGuid = resourceGroup.Guid.ToString();
                            }

                            // add any newly enabled resources for this instance
                            if ( !instance.Attributes.ContainsKey( groupType.Name ) )
                            {
                                CreateRegistrationInstanceAttribute( rockContext, instance, groupType.Name, resourceGroupGuid );
                            }
                            else
                            {
                                instance.AttributeValues[groupType.Name].Value = resourceGroupGuid;
                            }
                        }
                    }
                }      

                rockContext.SaveChanges();
                instance.SaveAttributeValues();
            }
        }

        /// <summary>
        /// Builds the resource group UI.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        /// <param name="instance"></param>
        /// <param name="rInstance">The r instance.</param>
        private void BuildResourcesInterface( bool setValues = true )
        {
            _resourceGroupTypes = new List<Guid>();
            _resourceGroups = new List<Guid>();
            _expandedRows = new List<Guid>();

            foreach ( var groupTypeRow in phRows.ControlsOfTypeRecursive<ResourceAreaRow>() )
            {
                if ( groupTypeRow.Expanded )
                {
                    _expandedRows.Add( groupTypeRow.GroupTypeGuid );
                }
            }

            foreach ( var groupRow in phRows.ControlsOfTypeRecursive<ResourceGroupRow>() )
            {
                if ( groupRow.Expanded )
                {
                    _expandedRows.Add( groupRow.GroupGuid );
                }
            }

            phRows.Controls.Clear();
            using ( var rockContext = new RockContext() )
            {
                var childGroupTypes = new GroupTypeService( rockContext ).Queryable()
                    .Where( t => t.InheritedGroupTypeId == TemplateGroupTypeId && t.Id != TemplateGroupTypeId )
                    .OrderBy( t => t.Order ).ThenBy( t => t.Name ).ToList();

                // load resource groups assigned to this registration instance
                foreach ( var groupType in childGroupTypes )
                {
                    BuildGroupTypeRow( groupType, phRows, setValues );
                }
            }
        }

        /// <summary>
        /// Builds the checkin area row.
        /// </summary>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void BuildGroupTypeRow( GroupType groupType, Control parentControl, bool setValues )
        {
            if ( groupType != null && !_resourceGroupTypes.Contains( groupType.Guid ) )
            {
                _resourceGroupTypes.Add( groupType.Guid );

                var resourceAreaRow = new ResourceAreaRow()
                {
                    ID = "ResourceAreaRow_" + groupType.Guid.ToString( "N" )
                };

                resourceAreaRow.EnableAddAreas = false;
                resourceAreaRow.EnableAddGroups = true;
                resourceAreaRow.SetGroupType( groupType );
                //resourceAreaRow.AddAreaClick += ResourceAreaRow_AddAreaClick;
                resourceAreaRow.AddGroupClick += ResourceAreaRow_AddGroupClick;
                parentControl.Controls.Add( resourceAreaRow );

                if ( setValues )
                {
                    resourceAreaRow.Expanded = true; //_expandedRows.Contains( groupType.Guid );
                    resourceAreaRow.Selected = _currentGroupTypeGuid.HasValue && groupType.Guid.Equals( _currentGroupTypeGuid.Value );
                }

                foreach ( var childGroupType in groupType.ChildGroupTypes
                    .Where( t => t.Id != groupType.Id )
                    .OrderBy( t => t.Order )
                    .ThenBy( t => t.Name ) )
                {
                    BuildGroupTypeRow( childGroupType, resourceAreaRow, setValues );
                }

                // Hydrate the groups of this registration instance and type, or new ones just created
                var allGroupIds = groupType.Groups.Where( g => g.ParentGroupId.Equals( RegistrationInstanceGroupId ) ).Select( g => g.Id ).ToList();
                foreach ( var childGroup in groupType.Groups
                    .Where( g => g.Guid != Guid.Empty && (
                        !g.ParentGroupId.HasValue ||
                        allGroupIds.Contains( g.Id ) )
                    )
                    .OrderBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    BuildGroupRow( childGroup, resourceAreaRow, setValues );
                }
            }
        }

        /// <summary>
        /// Builds the group row.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void BuildGroupRow( Group group, Control parentControl, bool setValues )
        {
            if ( group != null && !_resourceGroups.Contains( group.Guid ) )
            {
                var resourceGroupRow = new ResourceGroupRow
                {
                    ID = "ResourceGroupRow_" + group.Guid.ToString( "N" )
                };

                resourceGroupRow.SetGroup( group );
                resourceGroupRow.AddGroupClick += ResourceGroupRow_AddGroupClick;
                resourceGroupRow.DeleteGroupClick += ResourceGroupRow_DeleteGroupClick;
                parentControl.Controls.Add( resourceGroupRow );

                if ( setValues )
                {
                    resourceGroupRow.Expanded = true; // _expandedRows.Contains( group.Guid );
                    resourceGroupRow.Selected = resourceGroupPanel.Visible && _currentGroupGuid.HasValue && group.Guid.Equals( _currentGroupGuid.Value );
                }

                foreach ( var childGroup in group.Groups
                    .Where( g => g.GroupTypeId == group.GroupTypeId )
                    .OrderBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    BuildGroupRow( childGroup, resourceGroupRow, setValues );
                }
            }
        }

        #endregion

        #region Payments Tab

        /// <summary>
        /// Binds the payments filter.
        /// </summary>
        private void BindPaymentsFilter()
        {
            sdrpPaymentDateRange.DelimitedValues = fPayments.GetUserPreference( "Payments Date Range" );
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
                    var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( sdrpPaymentDateRange.DelimitedValues );

                    if ( dateRange.Start.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.TransactionDateTime >= dateRange.Start.Value );
                    }
                    if ( dateRange.End.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.TransactionDateTime < dateRange.End.Value );
                    }

                    var sortProperty = gPayments.SortProperty;
                    if ( sortProperty != null )
                    {
                        if ( sortProperty.Property == "TotalAmount" )
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                qry = qry.OrderBy( t => t.TransactionDetails.Sum( d => (decimal?)d.Amount ) ?? 0.00M );
                            }
                            else
                            {
                                qry = qry.OrderByDescending( t => t.TransactionDetails.Sum( d => (decimal?)d.Amount ) ?? 0.0M );
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
                            r.OnWaitList == false &&
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

                    // Filter by daterange
                    var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( sdrpRegistrationDateRange.DelimitedValues );

                    if ( dateRange.Start.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value >= dateRange.Start.Value );
                    }

                    if ( dateRange.End.HasValue )
                    {
                        qry = qry.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value < dateRange.End.Value );
                    }

                    // Filter by first name
                    if ( !string.IsNullOrWhiteSpace( tbGroupPlacementsFirstName.Text ) )
                    {
                        var rfname = tbGroupPlacementsFirstName.Text;
                        qry = qry.Where( r =>
                            r.PersonAlias.Person.NickName.StartsWith( rfname ) ||
                            r.PersonAlias.Person.FirstName.StartsWith( rfname ) );
                    }

                    // Filter by last name
                    if ( !string.IsNullOrWhiteSpace( tbGroupPlacementsLastName.Text ) )
                    {
                        var rlname = tbGroupPlacementsLastName.Text;
                        qry = qry.Where( r =>
                            r.PersonAlias.Person.LastName.StartsWith( rlname ) );
                    }

                    // Filter by signed documents
                    if ( Signers != null )
                    {
                        if ( ddlGroupPlacementsSignedDocument.SelectedValue.AsBooleanOrNull() == true )
                        {
                            qry = qry.Where( r => Signers.Contains( r.PersonAlias.PersonId ) );
                        }
                        else if ( ddlGroupPlacementsSignedDocument.SelectedValue.AsBooleanOrNull() == false )
                        {
                            qry = qry.Where( r => !Signers.Contains( r.PersonAlias.PersonId ) );
                        }
                    }

                    var preloadCampusValues = false;
                    var registrantAttributeIds = new List<int>();
                    var personAttributesIds = new List<int>();
                    var groupMemberAttributesIds = new List<int>();

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

                                        var ddlCampus = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsCampus" ) as RockDropDownList;
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
                                        var tbEmailFilter = phGroupPlacementsFormFieldFilters.FindControl( "tbGroupPlacementsEmailFilter" ) as RockTextBox;
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
                                        var drpBirthdateFilter = phGroupPlacementsFormFieldFilters.FindControl( "drpGroupPlacementsBirthdateFilter" ) as DateRangePicker;
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
                                        var gpGradeFilter = phGroupPlacementsFormFieldFilters.FindControl( "gpGroupPlacementsGradeFilter" ) as GradePicker;
                                        if ( gpGradeFilter != null )
                                        {
                                            int? graduationYear = Person.GraduationYearFromGradeOffset( gpGradeFilter.SelectedValueAsInt( false ) );
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
                                        var ddlGenderFilter = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsGenderFilter" ) as RockDropDownList;
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
                                        var ddlMaritalStatusFilter = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsMaritalStatusFilter" ) as RockDropDownList;
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
                                        var tbPhoneFilter = phGroupPlacementsFormFieldFilters.FindControl( "tbGroupPlacementsPhoneFilter" ) as RockTextBox;
                                        if ( tbPhoneFilter != null && !string.IsNullOrWhiteSpace( tbPhoneFilter.Text ) )
                                        {
                                            string numericPhone = tbPhoneFilter.Text.AsNumeric();

                                            if ( !string.IsNullOrEmpty( numericPhone ) )
                                            {
                                                var phoneNumberPersonIdQry = new PhoneNumberService( rockContext ).Queryable().Where( a => a.Number.Contains( numericPhone ) ).
                                                    Select( a => a.PersonId );

                                                qry = qry.Where( r => phoneNumberPersonIdQry.Contains( r.PersonAlias.PersonId ) );
                                            }
                                        }

                                        break;
                                    }
                            }
                        }

                        // Get all the registrant attributes selected to be on grid
                        var registrantAttributes = RegistrantFields
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
                                var filterControl = phGroupPlacementsFormFieldFilters.FindControl( "filterGroupPlacements_" + attribute.Id.ToString() );
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
                        var personAttributes = RegistrantFields
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
                                var filterControl = phGroupPlacementsFormFieldFilters.FindControl( "filterGroupPlacements_" + attribute.Id.ToString() );
                                if ( filterControl != null )
                                {
                                    var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                    var expression = attribute.FieldType.Field.AttributeFilterExpression( attribute.QualifierValues, filterValues, parameterExpression );
                                    if ( expression != null )
                                    {
                                        var attributeValues = attributeValueService.Queryable()
                                            .Where( v => v.Attribute.Id == attribute.Id );
                                        attributeValues = attributeValues.Where( parameterExpression, expression, null );
                                        qry = qry
                                            .Where( r => attributeValues.Select( v => v.EntityId ).Contains( r.PersonAlias.PersonId ) );
                                    }
                                }
                            }
                        }

                        // Get all the group member attributes selected to be on grid
                        var groupMemberAttributes = RegistrantFields
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
                                var filterControl = phGroupPlacementsFormFieldFilters.FindControl( "filterGroupPlacements_" + attribute.Id.ToString() );
                                if ( filterControl != null )
                                {
                                    var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                    var expression = attribute.FieldType.Field.AttributeFilterExpression( attribute.QualifierValues, filterValues, parameterExpression );
                                    if ( expression != null )
                                    {
                                        var attributeValues = attributeValueService.Queryable()
                                            .Where( v => v.Attribute.Id == attribute.Id );
                                        attributeValues = attributeValues.Where( parameterExpression, expression, null );
                                        qry = qry
                                            .Where( r => r.GroupMemberId.HasValue && attributeValues.Select( v => v.EntityId ).Contains( r.GroupMemberId.Value ) );
                                    }
                                }
                            }
                        }
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

        /// <summary>
        /// Handles the ApplyFilterClick event of the fGroupPlacements control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fGroupPlacements_ApplyFilterClick( object sender, EventArgs e )
        {
            fGroupPlacements.SaveUserPreference( "GroupPlacements-Date Range", "Date Range", sdrpGroupPlacementsDateRange.DelimitedValues );
            fGroupPlacements.SaveUserPreference( "GroupPlacements-First Name", "First Name", tbGroupPlacementsFirstName.Text );
            fGroupPlacements.SaveUserPreference( "GroupPlacements-Last Name", "Last Name", tbGroupPlacementsLastName.Text );
            fGroupPlacements.SaveUserPreference( "GroupPlacements-Signed Document", "Signed Document", ddlGroupPlacementsSignedDocument.SelectedValue );

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
                                    var ddlCampus = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsCampus" ) as RockDropDownList;
                                    if ( ddlCampus != null )
                                    {
                                        fGroupPlacements.SaveUserPreference( "GroupPlacements-Home Campus", "Home Campus", ddlCampus.SelectedValue );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Email:
                                {
                                    var tbEmailFilter = phGroupPlacementsFormFieldFilters.FindControl( "tbGroupPlacementsEmailFilter" ) as RockTextBox;
                                    if ( tbEmailFilter != null )
                                    {
                                        fGroupPlacements.SaveUserPreference( "GroupPlacements-Email", "Email", tbEmailFilter.Text );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Birthdate:
                                {
                                    var drpBirthdateFilter = phGroupPlacementsFormFieldFilters.FindControl( "drpGroupPlacementsBirthdateFilter" ) as DateRangePicker;
                                    if ( drpBirthdateFilter != null )
                                    {
                                        fGroupPlacements.SaveUserPreference( "GroupPlacements-Birthdate Range", "Birthdate Range", drpBirthdateFilter.DelimitedValues );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Grade:
                                {
                                    var gpGradeFilter = phGroupPlacementsFormFieldFilters.FindControl( "gpGroupPlacementsGradeFilter" ) as GradePicker;
                                    if ( gpGradeFilter != null )
                                    {
                                        var gradeOffset = gpGradeFilter.SelectedValueAsInt( false );
                                        fGroupPlacements.SaveUserPreference( "GroupPlacements-Grade", "Grade", gradeOffset.HasValue ? gradeOffset.Value.ToString() : "" );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.Gender:
                                {
                                    var ddlGenderFilter = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsGenderFilter" ) as RockDropDownList;
                                    if ( ddlGenderFilter != null )
                                    {
                                        fGroupPlacements.SaveUserPreference( "GroupPlacements-Gender", "Gender", ddlGenderFilter.SelectedValue );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.MaritalStatus:
                                {
                                    var ddlMaritalStatusFilter = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsMaritalStatusFilter" ) as RockDropDownList;
                                    if ( ddlMaritalStatusFilter != null )
                                    {
                                        fGroupPlacements.SaveUserPreference( "GroupPlacements-Marital Status", "Marital Status", ddlMaritalStatusFilter.SelectedValue );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.MobilePhone:
                                {
                                    var tbPhoneFilter = phGroupPlacementsFormFieldFilters.FindControl( "tbGroupPlacementsPhoneFilter" ) as RockTextBox;
                                    if ( tbPhoneFilter != null )
                                    {
                                        fGroupPlacements.SaveUserPreference( "GroupPlacements-Phone", "Phone", tbPhoneFilter.Text );
                                    }

                                    break;
                                }
                        }
                    }

                    if ( field.Attribute != null )
                    {
                        var attribute = field.Attribute;
                        var filterControl = phGroupPlacementsFormFieldFilters.FindControl( "filterGroupPlacements_" + attribute.Id.ToString() );
                        if ( filterControl != null )
                        {
                            try
                            {
                                var values = attribute.FieldType.Field.GetFilterValues( filterControl, field.Attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                fGroupPlacements.SaveUserPreference( "GroupPlacements-" + attribute.Key, attribute.Name, attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter ).ToJson() );
                            }
                            catch { }
                        }
                    }
                }
            }

            BindGroupPlacementGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the fGroupPlacements control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fGroupPlacements_ClearFilterClick( object sender, EventArgs e )
        {
            fGroupPlacements.DeleteUserPreferences();

            foreach ( var control in phGroupPlacementsFormFieldFilters.ControlsOfTypeRecursive<Control>().Where( a => a.ID != null && a.ID.StartsWith( "filter" ) && a.ID.Contains( "_" ) ) )
            {
                var attributeId = control.ID.Split( '_' )[1].AsInteger();
                var attribute = AttributeCache.Read( attributeId );
                if ( attribute != null )
                {
                    attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, new List<string>() );
                }
            }

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
                                    var ddlCampus = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsCampus" ) as RockDropDownList;
                                    if ( ddlCampus != null )
                                    {
                                        ddlCampus.SetValue( (Guid?)null );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Email:
                                {
                                    var tbEmailFilter = phGroupPlacementsFormFieldFilters.FindControl( "tbGroupPlacementsEmailFilter" ) as RockTextBox;
                                    if ( tbEmailFilter != null )
                                    {
                                        tbEmailFilter.Text = string.Empty;
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Birthdate:
                                {
                                    var drpBirthdateFilter = phGroupPlacementsFormFieldFilters.FindControl( "drpGroupPlacementsBirthdateFilter" ) as DateRangePicker;
                                    if ( drpBirthdateFilter != null )
                                    {
                                        drpBirthdateFilter.LowerValue = null;
                                        drpBirthdateFilter.UpperValue = null;
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.Grade:
                                {
                                    var gpGradeFilter = phGroupPlacementsFormFieldFilters.FindControl( "gpGroupPlacementsGradeFilter" ) as GradePicker;
                                    if ( gpGradeFilter != null )
                                    {
                                        gpGradeFilter.SetValue( (Guid?)null );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.Gender:
                                {
                                    var ddlGenderFilter = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsGenderFilter" ) as RockDropDownList;
                                    if ( ddlGenderFilter != null )
                                    {
                                        ddlGenderFilter.SetValue( (Guid?)null );
                                    }

                                    break;
                                }

                            case RegistrationPersonFieldType.MaritalStatus:
                                {
                                    var ddlMaritalStatusFilter = phGroupPlacementsFormFieldFilters.FindControl( "ddlGroupPlacementsMaritalStatusFilter" ) as RockDropDownList;
                                    if ( ddlMaritalStatusFilter != null )
                                    {
                                        ddlMaritalStatusFilter.SetValue( (Guid?)null );
                                    }

                                    break;
                                }
                            case RegistrationPersonFieldType.MobilePhone:
                                {
                                    var tbPhoneFilter = phGroupPlacementsFormFieldFilters.FindControl( "tbGroupPlacementsPhoneFilter" ) as RockTextBox;
                                    if ( tbPhoneFilter != null )
                                    {
                                        tbPhoneFilter.Text = string.Empty;
                                    }

                                    break;
                                }
                        }
                    }
                }
            }

            BindGroupPlacementsFilter( null );
        }

        /// <summary>
        /// fs the group placements display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void fGroupPlacements_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            if ( e.Key.StartsWith( "GroupPlacements-" ) )
            {
                var key = e.Key.Remove( 0, "GroupPlacements-".Length );

                if ( RegistrantFields != null )
                {
                    var attribute = RegistrantFields
                        .Where( a =>
                            a.Attribute != null &&
                            a.Attribute.Key == key )
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

                switch ( key )
                {
                    case "Date Range":
                    case "Birthdate Range":
                        {
                            // The value might either be from a SlidingDateRangePicker or a DateRangePicker, so try both
                            var storedValue = e.Value;
                            e.Value = SlidingDateRangePicker.FormatDelimitedValues( storedValue );
                            if ( string.IsNullOrWhiteSpace( e.Value ) )
                            {
                                e.Value = DateRangePicker.FormatDelimitedValues( storedValue );
                            }

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
                            int? campusId = e.Value.AsIntegerOrNull();
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
                            int? dvId = e.Value.AsIntegerOrNull();
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
            else
            {
                e.Value = "";
            }
        }

        /// <summary>
        /// Binds the group placements filter.
        /// </summary>
        /// <param name="instance">The instance.</param>
        private void BindGroupPlacementsFilter( RegistrationInstance instance )
        {
            sdrpGroupPlacementsDateRange.DelimitedValues = fGroupPlacements.GetUserPreference( "GroupPlacements-Date Range" );
            tbGroupPlacementsFirstName.Text = fGroupPlacements.GetUserPreference( "GroupPlacements-First Name" );
            tbGroupPlacementsLastName.Text = fGroupPlacements.GetUserPreference( "GroupPlacements-Last Name" );

            ddlGroupPlacementsSignedDocument.SetValue( fGroupPlacements.GetUserPreference( "GroupPlacements-Signed Document" ) );
            ddlGroupPlacementsSignedDocument.Visible = instance != null && instance.RegistrationTemplate != null && instance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue;
        }

        #endregion

        #region Group Association Tabs

        /// <summary>
        /// Handles the ItemDataBound event of the rpGroupPanels control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rpResourcePanels_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var groupType = (GroupTypeCache)e.Item.DataItem;
            var pnlAssociatedGroup = (Panel)e.Item.FindControl( "pnlAssociatedGroup" );
            var pnlGroupBody = (Panel)e.Item.FindControl( "pnlGroupBody" );
            var pnlGroupHeading = (Panel)e.Item.FindControl( "pnlGroupHeading" );
            var phGroupHeading = (PlaceHolder)e.Item.FindControl( "phGroupHeading" );
            var lbAddSubGroup = (HtmlAnchor)e.Item.FindControl( "lbAddSubGroup" );
            var hfParentGroupId = (HiddenField)e.Item.FindControl( "hfParentGroupId" );
            var phGroupControl = (PlaceHolder)e.Item.FindControl( "phGroupControl" );

            var groupTypeGroupTerm = "Group";
            if ( !string.IsNullOrWhiteSpace( groupType.GroupTerm ) )
            {
                groupTypeGroupTerm = groupType.GroupTerm;
            }

            lbAddSubGroup.InnerHtml += string.Format( "Add {0}", groupTypeGroupTerm );

            var registrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            var instance = GetRegistrationInstance( registrationInstanceId );

            var tabName = groupType.Name;
            if ( instance != null )
            {
                // get the group placeholder for this grouptype
                if ( instance.AttributeValues.ContainsKey( groupType.Name ) )
                {
                    Group parentGroup = null;
                    var resourceGroupGuids = instance.AttributeValues[groupType.Name];
                    if ( resourceGroupGuids != null && !string.IsNullOrWhiteSpace( resourceGroupGuids.Value ) )
                    {
                        parentGroup = new GroupService( new RockContext() ).Get( resourceGroupGuids.Value.AsGuid() );
                        if ( parentGroup != null )
                        {
                            tabName = parentGroup.Name;
                        }
                    }

                    var modalIconString = string.Empty;
                    //if ( groupType.GroupTypePurposeValue != null && groupType.GroupTypePurposeValue.Value == "Serving Area" )
                    //{
                    //    pnlVolunteers.Visible = true;
                    //}

                    if ( parentGroup != null )
                    {
                        hfParentGroupId.Value = parentGroup.Guid.ToString();
                        phGroupControl.Controls.Clear();

                        // TODO: move this to ShowTab so it fires once per tab
                        BuildSubGroupPanels( phGroupControl, parentGroup.Groups.OrderBy( g => g.Name ).ToList() );

                        var qryParams = new Dictionary<string, string>();
                        qryParams.Add( "t", string.Format( "Add {0}", groupTypeGroupTerm ) );
                        qryParams.Add( "GroupId", "0" );
                        qryParams.Add( "ParentGroupId", parentGroup.Id.ToString() );

                        lbAddSubGroup.HRef = "javascript: Rock.controls.modal.show($(this), '" + LinkedPageUrl( "GroupModalPage", qryParams ) + "')";
                    }
                }
            }

            pnlAssociatedGroup.Visible = ActiveTab == ( "lb" + tabName );

            // build header section
            var header = new HtmlGenericControl( "h1" );
            header.Attributes.Add( "class", "panel-title" );
            if ( !string.IsNullOrWhiteSpace( groupType.IconCssClass ) )
            {
                var faIcon = new HtmlGenericControl( "i" );
                faIcon.Attributes.Add( "class", groupType.IconCssClass );
                header.Controls.Add( faIcon );
            }

            header.Controls.Add( new LiteralControl( string.Format( " {0}", tabName ) ) );
            phGroupHeading.Controls.Add( header );
        }

        /// <summary>
        /// Builds the sub group panels.
        /// </summary>
        /// <param name="phGroupControl">The ph group control.</param>
        /// <param name="subGroups">The sub groups.</param>
        private void BuildSubGroupPanels( PlaceHolder phGroupControl, List<Group> subGroups )
        {
            var registrationInstance = GetRegistrationInstance( PageParameter( "RegistrationInstanceId" ).AsInteger() );

            foreach ( Group group in subGroups )
            {
                var groupPanel = (GroupPanel)LoadControl( "~/Plugins/com_kfs/Event/GroupPanel.ascx");
                //var groupPanel = new GroupPanel();  // doesn't work

                groupPanel.ID = string.Format( "groupPanel_{0}", group.Id );

                foreach ( string control in _expandedGroupPanels )
                {
                    if ( control.Contains( groupPanel.ID ) )
                    {
                        groupPanel.Expanded = true;
                        break;
                    }
                }
                groupPanel.AddButtonClick += AddMemberButton_Click;
                groupPanel.EditMemberButtonClick += EditMemberButton_Click;
                groupPanel.GroupRowDataBound += GroupRowDataBound;
                //groupPanel.AssignGroupButtonClick += ;
                groupPanel.BuildControl( group, ResourceGroupTypes, ResourceGroups );
                phGroupControl.Controls.Add( groupPanel );
            }
        }

        /// <summary>
        /// Handles the ItemCommand event of the RpGroupPanels control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        private void rpResourcePanels_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            if ( e.CommandName == "EditSubGroup" )
            {
                var group = new GroupService( new RockContext() ).Get( int.Parse( e.CommandArgument.ToString() ) );
                hfEditGroup.Value = group.Guid.ToString();

                BindResourcePanels();

                var qryParams = new Dictionary<string, string>();
                qryParams.Add( "t", string.Format( "Edit {0}", group.Name ) );
                qryParams.Add( "GroupId", group.Id.ToString() );

                string script = "Rock.controls.modal.show($(this), '" + LinkedPageUrl( "GroupModalPage", qryParams ) + "');";
                ScriptManager.RegisterClientScriptBlock( Page, Page.GetType(), "editSubGroup" + e.CommandArgument.ToString(), script, true );
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

                    var isSecurityRoleGroup = group.IsActive && ( group.IsSecurityRole || group.GroupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() ) );
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

                BindResourcePanels();
            }
        }

        /// <summary>
        /// Binds the reousrce panels.
        /// </summary>
        private void BindResourcePanels()
        {
            // TODO make sure this doesn't fire all the time
            rpResourcePanels.DataSource = ResourceGroupTypes.OrderBy( g => g.Name );
            rpResourcePanels.DataBind();
        }

        /// <summary>
        /// Rows the data bound.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        private void GroupRowDataBound( object sender, EventArgs e )
        {
            var panel = (GroupPanel)sender;
            var rowEvent = e as GridViewRowEventArgs;
            if ( rowEvent != null )
            {
                var groupMember = rowEvent.Row.DataItem as GroupMember;
                if ( groupMember != null )
                {
                    var campus = groupMember.Person.GetCampus();
                    var lCampus = rowEvent.Row.FindControl( "lFamilyCampus" ) as Literal;
                    if ( lCampus != null && campus != null )
                    {
                        lCampus.Text = campus.Name;
                    }

                    // Build subgroup button grid for Volunteers
                    if ( groupMember.Group.GroupType.GroupTypePurposeValue != null && groupMember.Group.GroupType.GroupTypePurposeValue.Value == "Serving Area" )
                    {
                        BuildGroupAssignmentGrid( rowEvent );
                    }
                }
            }
        }

        /// <summary>
        /// Builds the group assignment grid.
        /// </summary>
        /// <param name="e">The <see cref="GridViewRowEventArgs" /> instance containing the event data.</param>
        /// <param name="columnIndex">Index of the column.</param>
        private void BuildGroupAssignmentGrid( GridViewRowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                int? personId = null;
                if ( e.Row.DataItem is RegistrationRegistrant )
                {
                    personId = ( (RegistrationRegistrant)e.Row.DataItem ).PersonId;
                }
                else if ( e.Row.DataItem is GroupMember )
                {
                    personId = ( (GroupMember)e.Row.DataItem ).PersonId;
                }
                
                foreach ( var groupType in ResourceGroupTypes.Where( gt => gt.GetAttributeValue( "ShowOnGrid" ).AsBoolean( true ) ) )
                {
                    // if ( instance.AttributeValues.ContainsKey( groupType.Name ) )
                    var resourceGroupGuid = ResourceGroups[groupType.Name];
                    if ( resourceGroupGuid != null && !Guid.Empty.Equals( resourceGroupGuid.Value.AsGuid() ) )
                    {
                        var parentGroup = new GroupService( rockContext ).Get( resourceGroupGuid.Value.AsGuid() );
                        if ( parentGroup != null )
                        {
                            // Use a name match to find the column since there are multiple dynamic columns 
                            var columnIndex = 0;
                            LinkButton btnGroupAssignment = null;
                            foreach ( DataControlFieldCell cell in e.Row.Cells )
                            {   
                                if ( parentGroup.Name == cell.ContainingField.HeaderText || parentGroup.GroupType.Name == cell.ContainingField.HeaderText )
                                {
                                    btnGroupAssignment = cell.Controls.Count > 0 ? cell.Controls[0] as LinkButton : null;
                                    break;
                                }
                                columnIndex++;
                            }
                            
                            if ( btnGroupAssignment != null )
                            {
                                if ( parentGroup.Groups.Any() )
                                {
                                    if ( parentGroup.GroupType.Attributes == null || !parentGroup.GroupType.Attributes.Any() )
                                    {
                                        parentGroup.GroupType.LoadAttributes( rockContext );
                                    }

                                    var subGroupIds = parentGroup.Groups.Select( g => g.Id ).ToList();
                                    var groupMemberships = new GroupMemberService( rockContext ).Queryable().AsNoTracking()
                                        .Where( m => m.PersonId == personId && subGroupIds.Contains( m.GroupId ) ).ToList();
                                    if ( !groupMemberships.Any() )
                                    {
                                        btnGroupAssignment.CssClass = "btn-add btn btn-default btn-sm";

                                        using ( var literalControl = new LiteralControl( "<i class='fa fa-plus-circle'></i>" ) )
                                        {
                                            btnGroupAssignment.Controls.Add( literalControl );
                                        }
                                        using ( var literalControl = new LiteralControl( "<span class='grid-btn-assign-text'> Assign</span>" ) )
                                        {
                                            btnGroupAssignment.Controls.Add( literalControl );
                                        }
                                        btnGroupAssignment.CommandName = "AssignSubGroup";
                                        btnGroupAssignment.CommandArgument = string.Format( "{0}|{1}", parentGroup.Id.ToString(), personId.ToString() );
                                    }
                                    else if ( parentGroup.GroupType.GetAttributeValue( "AllowMultipleRegistrations" ).AsBoolean() )
                                    {
                                        var subGroupControls = e.Row.Cells[columnIndex].Controls;

                                        // add any group memberships
                                        foreach ( var member in groupMemberships )
                                        {
                                            using ( var subGroupButton = new LinkButton() )
                                            {
                                                // TODO: Y U NO WORK?
                                                //subGroupButton.Command += new CommandEventHandler( MultipleSubGroup_Click );
                                                //btnGroupAssignment.CommandName = "ChangeSubGroup";
                                                //btnGroupAssignment.CommandArgument = member.Id.ToString();

                                                subGroupButton.OnClientClick = "javascript: __doPostBack( 'btnMultipleRegistrations', 'select-subgroup:" + member.Id + "' ); return false;";
                                                subGroupButton.Text = " " + member.Group.Name + "  ";
                                                subGroupControls.AddAt( 0, subGroupButton );
                                            }
                                        }

                                        using ( var literalControl = new LiteralControl( "<i class='fa fa-plus-circle'></i>" ) )
                                        {
                                            btnGroupAssignment.Controls.Add( literalControl );
                                        }

                                        // TODO: figure out why command args aren't passing
                                        btnGroupAssignment.OnClientClick = string.Format( "javascript: __doPostBack( 'btnMultipleRegistrations', 'select-subgroup:{0}|{1}' ); return false;", parentGroup.Id, personId );
                                        btnGroupAssignment.CssClass = "btn-add btn btn-default btn-sm";
                                        //btnGroupAssignment.CommandName = "AssignSubGroup";
                                        //btnGroupAssignment.CommandArgument = string.Format( "subgroup_{0}|{1}", parentGroup.Id.ToString(), registrant.Id.ToString() );
                                    }
                                    else
                                    {
                                        var groupMember = groupMemberships.FirstOrDefault();
                                        btnGroupAssignment.Text = groupMember.Group.Name;
                                        btnGroupAssignment.CommandName = "ChangeSubGroup";
                                        btnGroupAssignment.CommandArgument = groupMember.Id.ToString();
                                    }
                                }
                                else
                                {
                                    using ( var literalControl = new LiteralControl( "<i class='fa fa-minus-circle'></i>" ) )
                                    {
                                        btnGroupAssignment.Controls.Add( literalControl );
                                    }
                                    using ( var literalControl = new LiteralControl( string.Format( "<span class='grid-btn-assign-text'> No {0}</span>", parentGroup.GroupType.GroupTerm.Pluralize() ) ) )
                                    {
                                        btnGroupAssignment.Controls.Add( literalControl );
                                    }
                                    btnGroupAssignment.Enabled = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the AddMemberButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void AddMemberButton_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var panel = (GroupPanel)sender;
            RenderGroupMemberModal( rockContext, panel.Group.ParentGroup, panel.Group, null, null );
        }

        /// <summary>
        /// Handles the Click event of the EditMemberButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void EditMemberButton_Click( object sender, EventArgs e )
        {
            var groupMemberId = ( (RowEventArgs)e ).RowKeyId;
            if ( groupMemberId > 0 )
            {
                RenderEditGroupMemberModal( groupMemberId.ToString() );
            }
        }

        /// <summary>
        /// Handles the Click event of the AddMemberButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GroupAssignment_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var panel = (GroupPanel)sender;
            RenderGroupMemberModal( rockContext, panel.Group.ParentGroup, panel.Group, null, null );
            //BuildGroupAssignmentGrid( e as GridViewRowEventArgs, panel.Grid.Rows[0] );
        }

        /// <summary>
        /// Renders the edit group member modal.
        /// </summary>
        /// <param name="groupKey">The group key.</param>
        private void RenderEditGroupMemberModal( string groupKey )
        {
            if ( !groupKey.Contains( '|' ) )
            {
                // group member id
                using ( var rockContext = new RockContext() )
                {
                    var member = new GroupMemberService( rockContext ).Get( groupKey.AsInteger() );
                    if ( member != null )
                    {
                        RenderGroupMemberModal( rockContext, member.Group.ParentGroup, member.Group, member, null );
                    }
                }
            }
            else
            {
                // group id | registrant id pair
                var parentGroupId = 0;
                var registrantId = 0;
                int.TryParse( groupKey.Split( '|' )[0], out parentGroupId );
                int.TryParse( groupKey.Split( '|' )[1], out registrantId );
                if ( parentGroupId > 0 && registrantId > 0 )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var groupService = new GroupService( rockContext );
                        var registrantService = new RegistrationRegistrantService( rockContext );
                        var parentGroup = groupService.Get( parentGroupId );
                        var registrant = registrantService.Get( registrantId );
                        if ( registrant != null )
                        {
                            RenderGroupMemberModal( rockContext, parentGroup, null, null, registrant );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Renders the member modal.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="parentGroup">The parent group.</param>
        /// <param name="group">The group.</param>
        /// <param name="groupMember">The group member.</param>
        /// <param name="registrant">The registrant.</param>
        protected void RenderGroupMemberModal( RockContext rockContext, Group parentGroup, Group group, GroupMember groupMember, RegistrationRegistrant registrant )
        {
            // Clear modal controls
            nbErrorMessage.Visible = false;
            ddlRegistrantList.Items.Clear();
            ddlSubGroup.Items.Clear();
            ddlGroupRole.Items.Clear();
            tbNote.Text = string.Empty;

            // registrant may be empty, so get instance from URL
            var registrationGroupGuids = new List<Guid>();
            var registrationInstanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            var registrationInstance = GetRegistrationInstance( registrationInstanceId, rockContext );
            if ( registrationInstance.AttributeValues.Any() )
            {
                registrationGroupGuids = registrationInstance.AttributeValues.Values
                    .Where( v => !string.IsNullOrWhiteSpace( v.Value ) ).Select( v => v.Value.AsGuid() ).ToList();
                hfRegistrationGroupGuid.Value = string.Join( ",", registrationGroupGuids );
            }

            rblStatus.BindToEnum<GroupMemberStatus>();
            rblMoveRegistrants.SelectedValue = "N";
            var groupMemberTerm = "Member";
            if ( group != null )
            {
                hfSubGroupId.Value = group.Id.ToString();
                if ( !string.IsNullOrWhiteSpace( group.GroupType.GroupMemberTerm ) )
                {
                    groupMemberTerm = group.GroupType.GroupMemberTerm;
                }
                parentGroup = group.ParentGroup;
            }
            else if ( parentGroup != null )
            {
                hfSubGroupId.Value = string.Empty;
                if ( !string.IsNullOrWhiteSpace( parentGroup.GroupType.GroupMemberTerm ) )
                {
                    groupMemberTerm = parentGroup.GroupType.GroupMemberTerm;
                }
            }

            ddlGroupRole.DataSource = parentGroup.GroupType.Roles.OrderBy( a => a.Order ).ToList();
            ddlGroupRole.DataBind();

            // TODO refactor this
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
                    ddlSubGroup.Items.Insert( 0, Rock.Constants.None.ListItem );
                    // for consistency with delete member functionality, don't autoselect the first group in the list
                    ddlSubGroup.SelectedIndex = 0; // parentGroup.Groups.Any() ? 1 : 0; 
                    ddlSubGroup.Label = !string.IsNullOrWhiteSpace( parentGroup.GroupType.GroupTerm ) ? parentGroup.GroupType.GroupTerm : parentGroup.Name;
                    group = parentGroup.Groups.Any() ? parentGroup.Groups.FirstOrDefault() : null;
                }
                else if ( group != null )
                {
                    if ( group.GroupType.Attributes == null || !group.GroupType.Attributes.Any() )
                    {
                        group.GroupType.LoadAttributes();
                    }

                    // start a list of available volunteers, will be filtered below
                    var qryAvailableVolunteers = new GroupMemberService( rockContext ).Queryable().Where( g => g.Group.GroupType.GroupTypePurposeValue.Value == "Serving Area" );

                    // dropdown is limited to registrants or non-serving areas
                    if ( group.GroupType.GroupTypePurposeValue == null || group.GroupType.GroupTypePurposeValue.Value != "Serving Area" )
                    {
                        // check if registrants can be assigned to multiple groups
                        var placedMembers = new List<int>();
                        if ( !group.GroupType.GetAttributeValue( "AllowMultipleRegistrations" ).AsBoolean() )
                        {
                            ddlRegistrantList.Help = string.Format( "Choose from a list of Registrants who have not yet been assigned to a {0} {1}", group.ParentGroup.Name, group.GroupType.GroupTerm );
                            foreach ( Group g in group.ParentGroup.Groups )
                            {
                                placedMembers.AddRange(
                                    g.Members.Select( m => m.Person.Id )
                                    .Where( m => !placedMembers.Contains( m ) )
                                );
                            }
                        }
                        else
                        {
                            ddlRegistrantList.Label = groupMemberTerm;
                            ddlRegistrantList.Help = null;
                        }

                        // add registrants who haven't already been placed
                        var allowedRegistrations = new RegistrationRegistrantService( rockContext ).Queryable().AsNoTracking()
                            .Where( r => r.Registration.RegistrationInstanceId == registrationInstanceId && r.PersonAlias != null && r.PersonAlias.Person != null
                                && !placedMembers.Contains( r.PersonAlias.Person.Id ) );
                        foreach ( var allowedRegistrant in allowedRegistrations )
                        {
                            var registrantItem = new ListItem( allowedRegistrant.PersonAlias.Person.FullNameReversed, allowedRegistrant.PersonAlias.Person.Guid.ToString() );
                            registrantItem.Attributes["optiongroup"] = "Registrants";
                            ddlRegistrantList.Items.Add( registrantItem );
                        }

                        // restrict any volunteer lists to this registration's resources
                        if ( registrationInstance.AttributeValues.Any() )
                        {
                            qryAvailableVolunteers = qryAvailableVolunteers.Where( g => registrationGroupGuids.Contains( g.Group.Guid ) || registrationGroupGuids.Contains( g.Group.ParentGroup.Guid ) )
                                .DistinctBy( v => v.PersonId ).AsQueryable();
                        }
                    }
                    // adding a volunteer instead of registrants, show the person picker instead of a dropdown
                    else
                    {
                        ddlRegistrantList.Visible = false;
                        ddlRegistrantList.Required = false;
                        ppVolunteer.Visible = true;
                        ppVolunteer.Required = true;
                    }

                    if ( !ppVolunteer.Visible && group.GroupType.GetAttributeValue( "AllowVolunteerAssignment" ).AsBoolean() && registrationInstanceId > 0 )
                    {
                        // display active volunteers not already in this group
                        foreach ( var volunteer in qryAvailableVolunteers.Where( v => v.GroupMemberStatus == GroupMemberStatus.Active && v.GroupId != group.Id ) )
                        {
                            var volunteerItem = new ListItem( volunteer.Person.FullNameReversed, volunteer.Person.Guid.ToString() );
                            volunteerItem.Attributes["optiongroup"] = "Volunteers"; //group.GroupType.GroupMemberTerm;
                            ddlRegistrantList.Items.Add( volunteerItem );
                        }

                        // since volunteers are allowed, display the move other registrants option
                        //rblMoveRegistrants.Visible = true;
                    }

                    mdlAddSubGroupMember.Title = string.Format( "Add New {0} to {1}", groupMemberTerm, group.Name );
                }

                if ( group != null )
                {
                    // this is a new group member, initialize the model
                    groupMember = new GroupMember { Id = 0 };
                    groupMember.GroupId = group.Id;
                    groupMember.Group = group;
                    groupMember.GroupRoleId = groupMember.Group.GroupType.DefaultGroupRoleId ?? 0;
                    groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                    groupMember.DateTimeAdded = RockDateTime.Now;
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
                ddlSubGroup.Label = !string.IsNullOrWhiteSpace( group.GroupType.GroupTerm ) ? group.GroupType.GroupTerm : group.ParentGroup.Name;
                ddlSubGroup.SelectedValue = group.Id.ToString();
            }

            groupMember.LoadAttributes();
            phAttributes.Controls.Clear();
            Rock.Attribute.Helper.AddEditControls( groupMember, phAttributes, true, string.Empty, true );

            mdlAddSubGroupMember.Show();

            // render dynamic group controls beneath modal
            BindRegistrantsGrid();
        }

        /// <summary>
        /// Handles the SaveClick event of the mdlAddSubGroupMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdlAddSubGroupMember_SaveClick( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                using ( var rockContext = new RockContext() )
                {
                    // add or remove group membership
                    GroupMember groupMember;
                    var originalGroupId = hfSubGroupId.ValueAsInt();
                    var newGroupId = ddlSubGroup.SelectedValue.AsInteger();
                    var groupMemberService = new GroupMemberService( rockContext );
                    var groupMemberId = int.Parse( hfSubGroupMemberId.Value );

                    // Check to see if a registrant or volunteer was selected
                    Person person;
                    if ( ddlRegistrantList.Visible )
                    {
                        person = ddlRegistrantList.SelectedValueAsGuid().HasValue ? new PersonService( rockContext ).Get( (Guid)ddlRegistrantList.SelectedValueAsGuid() ) : null;
                    }
                    else
                    {
                        person = ppVolunteer.SelectedValue.HasValue ? new PersonService( rockContext ).Get( (int)ppVolunteer.SelectedValue ) : null;
                    }
                    
                    if ( person == null )
                    {
                        nbErrorMessage.Title = "Please select a Person";
                        nbErrorMessage.Visible = true;
                        return;
                    }

                    // check to see if the user selected a role
                    var role = new GroupTypeRoleService( rockContext ).Get( ddlGroupRole.SelectedValueAsInt() ?? 0 );
                    if ( role == null )
                    {
                        nbErrorMessage.Title = "Please select a Role";
                        nbErrorMessage.Visible = true;
                        return;
                    }

                    if ( groupMemberId > 0 )
                    {
                        // load existing group member and move if needed
                        groupMember = groupMemberService.Get( groupMemberId );
                        groupMember.GroupId = newGroupId;
                    }
                    else
                    {
                        // if adding a new group member
                        groupMember = new GroupMember
                        {
                            Id = 0,
                            GroupId = originalGroupId > 0 ? originalGroupId : newGroupId
                        };
                    }

                    groupMember.PersonId = person.Id;
                    groupMember.GroupRoleId = role.Id;
                    groupMember.Note = tbNote.Text;
                    groupMember.GroupMemberStatus = rblStatus.SelectedValueAsEnum<GroupMemberStatus>();
                    groupMember.LoadAttributes();
                    Rock.Attribute.Helper.GetEditValues( phAttributes, groupMember );

                    // check for valid group membership
                    if ( groupMember.GroupId != 0 && !groupMember.IsValid )
                    {
                        if ( groupMember.ValidationResults.Any() )
                        {
                            nbErrorMessage.Text = groupMember.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                        }

                        nbErrorMessage.Visible = true;
                        return;
                    }

                    // check for moving other registrants
                    var membersBeingLed = new List<int>();
                    if ( rblMoveRegistrants.Visible && rblMoveRegistrants.SelectedValue.AsBoolean() )
                    {
                        var registrationGroupGuids = hfRegistrationGroupGuid.Value.SplitDelimitedValues()
                            .Select( g => g.AsGuid() ).ToList();

                        // look for a registration group this member is a leader of
                        membersBeingLed = groupMemberService.GetByPersonId( groupMember.PersonId )
                            .Where( gm => gm.GroupRole.IsLeader )
                            .Where( gm => registrationGroupGuids.Contains( gm.Group.Guid ) || registrationGroupGuids.Contains( gm.Group.ParentGroup.Guid ) )
                            .Select( gm => gm.Group ).SelectMany( g => g.Members )
                            .Where( gm => gm.PersonId != groupMember.PersonId )
                            .Select( gm => gm.PersonId ).ToList();
                    }

                    // use WrapTransaction because there are three context writes
                    rockContext.WrapTransaction( () =>
                    {
                        // add any members to the new group
                        if ( groupMember.GroupId > 0 )
                        {
                            if ( groupMember.Id == 0 )
                            {
                                groupMemberService.Add( groupMember );
                            }

                            if ( membersBeingLed.Any() )
                            {
                                var groupToAddTo = new GroupService( rockContext ).Get( groupMember.GroupId );
                                groupMemberService.AddRange( membersBeingLed.Except( groupToAddTo.Members.Select( cm => cm.PersonId ) )
                                    .Select( memberPerson => new GroupMember
                                    {
                                        PersonId = memberPerson,
                                        GroupId = groupMember.GroupId,
                                        GroupRoleId = role.GroupType.DefaultGroupRoleId ?? role.Id,
                                        GroupMemberStatus = groupMember.GroupMemberStatus,
                                    }
                                ) );
                            }
                        }

                        // delete any members from the existing group
                        if ( groupMember.GroupId != originalGroupId )
                        {
                            if ( groupMember.GroupId == 0 && groupMember.Id > 0 )
                            {
                                groupMemberService.Delete( groupMember );
                            }

                            if ( membersBeingLed.Any() )
                            {
                                var groupToRemoveFrom = new GroupService( rockContext ).Get( originalGroupId );
                                groupMemberService.DeleteRange( groupToRemoveFrom.Members
                                    .Where( m => membersBeingLed.Contains( m.PersonId ) )
                                );
                            }
                        }

                        rockContext.SaveChanges();
                        groupMember.SaveAttributeValues( rockContext );
                    } );

                    if ( groupMember != null && groupMember.Id > 0 )
                    {
                        hfSubGroupMemberId.Value = groupMember.Id.ToString();

                        if ( groupMember.GroupId > 0 )
                        {
                            hfSubGroupId.Value = groupMember.GroupId.ToString();
                        }
                    }

                    if ( hfRegistrationInstanceId.Value.AsInteger() > 0 )
                    {
                        BindRegistrantsFilter( new RegistrationInstanceService( rockContext ).Get( hfRegistrationInstanceId.Value.AsInteger() ) );
                    }
                }

                BindResourcePanels();
                BindRegistrantsGrid();
                mdlAddSubGroupMember.Hide();
            }
        }

        #endregion

        #endregion

        #region Helper Methods

        /// <summary>
        /// Shows a summary for invalid results.
        /// </summary>
        /// <param name="validationResults">The validation results.</param>
        private void ShowInvalidResults( List<ValidationResult> validationResults )
        {
            nbInvalid.Text = string.Format( "Please correct the following:<ul><li>{0}</li></ul>", validationResults.AsDelimited( "</li><li>" ) );
            nbInvalid.Visible = true;
        }

        /// <summary>
        /// Creates a group for this registration instance
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="parentGroupId">The parent group identifier.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        private Group CreateGroup( RockContext rockContext, GroupType groupType, int? parentGroupId, string groupName )
        {
            Group newGroup = null;
            if ( !string.IsNullOrWhiteSpace( groupName ) )
            {
                var groupService = new GroupService( rockContext );

                newGroup = new Group();
                newGroup.Name = groupName;
                newGroup.ParentGroupId = parentGroupId;
                newGroup.GroupType = groupType;
                groupService.Add( newGroup );
                rockContext.SaveChanges();

                // TODO: this should already have a guid // reuse connection
                newGroup = new GroupService( new RockContext() ).Get( newGroup.Guid );
            }

            return newGroup;
        }

        /// <summary>
        /// Creates the registration instance attribute.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="instanceAttributeKey">The instance attribute key.</param>
        /// <param name="attributeValue">The attribute value.</param>
        private void CreateRegistrationInstanceAttribute( RockContext rockContext, RegistrationInstance instance, string instanceAttributeKey, string attributeValue = "" )
        {
            if ( !string.IsNullOrWhiteSpace( instanceAttributeKey ) )
            {
                if ( instance.Attributes == null || !instance.Attributes.Any() )
                {
                    instance.LoadAttributes();
                }

                if ( !instance.Attributes.ContainsKey( instanceAttributeKey ) )
                {
                    var newAttribute = new Attribute
                    {
                        FieldTypeId = FieldTypeCache.Read( Rock.SystemGuid.FieldType.KEY_VALUE_LIST ).Id,
                        Name = instanceAttributeKey,
                        Key = instanceAttributeKey,
                        DefaultValue = string.Empty
                    };

                    newAttribute = Helper.SaveAttributeEdits( newAttribute, instance.TypeId, string.Empty, string.Empty, rockContext );
                    AttributeCache.FlushEntityAttributes();
                    instance.LoadAttributes();
                }

                instance.AttributeValues[instanceAttributeKey].Value = attributeValue;
                instance.SaveAttributeValues();
            }
        }

        /// <summary>
        /// Registers the page script.
        /// </summary>
        private void RegisterScript()
        {
            var script = @"
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

    function isDirty() {{
        return false;
    }}
";
            ScriptManager.RegisterStartupScript( upnlContent, this.GetType(), "deleteInstanceScript", script, true );
        }

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
    }
}
