﻿// <copyright>
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
// * This adds ability to include person properties as form fields
// * Added Conditional Field/Form Field Filter support to person attributes
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Field;
using Rock.Field.Types;
using Rock.Lava;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Crm
{
    /// <summary>
    /// Block to capture person data from currently logged in user.
    /// </summary>

    #region Block Attributes

    // Block Properties
    [DisplayName( "Person Attribute Forms Advanced" )]
    [Category( "KFS > CRM" )]
    [Description( "Block to capture person data from currently logged in user." )]

    #endregion

    #region Block Settings

    [BooleanField( "Allow Connection Opportunity", "Determines if a url parameter of 'OpportunityId' should be evaluated when complete.  Example: OpportunityId=1 or OpportunityId=1,2,3", false, "Connections", 0 )]
    [BooleanField( "Allow Group Membership", "Determines if a url parameter of 'GroupGuid' or 'GroupId' should be evaluated when complete.", false, "Groups", 0 )]
    [BooleanField( "Enable Passing Group Id", "If enabled, allows the ability to pass in a group's Id (GroupId=) instead of the Guid.", true, "Groups", 1 )]
    [GroupTypesField( "Allowed Group Types", "This setting restricts which types of groups a person can be added to, however selecting a specific group via the Group setting will override this restriction.", true, Rock.SystemGuid.GroupType.GROUPTYPE_SMALL_GROUP, "Groups", 2 )]
    [GroupField( "Group", "Optional group to add person to. If omitted, the group's Guid should be passed via the Query string (GroupGuid=).", false, "", "Groups", 3 )]
    [CustomRadioListField( "Group Member Status", "The group member status to use when adding person to group (default: 'Pending'.)", "2^Pending,1^Active,0^Inactive", true, "2", "Groups", 4 )]
    [BooleanField( "Display SMS Checkbox on Mobile Phone", "Should we show the SMS checkbox when a mobile phone is displayed on the form?", false )]
    [CustomDropdownListField( "Person Mode", "Person selection mode, should we allow family members, any person guid, or logged in user only.", "Family Members,Anyone,Logged in Person only", true, "Family Members" )]
    [BooleanField( "Display Family Member Picker", "Should we show the family member picker on the form? (Note: this will only display in Family Members or Anyone \"Person Mode\".) Default: false", false )]
    [BooleanField( "Display Progress Bar", "Determines if the progress bar should be show if there is more than one form.", true, "CustomSetting" )]
    [CustomDropdownListField( "Save Values", "", "PAGE,END", true, "END", "CustomSetting" )]
    [WorkflowTypeField( "Workflow", "The workflow to be launched when complete.", false, false, "", "CustomSetting" )]
    [CustomDropdownListField( "Workflow Entity", "", "Person,ConnectionRequest,GroupMember", true, "Person", "CustomSetting" )]
    [LinkedPage( "Done Page", "The page to redirect to when done.", false, "", "CustomSetting" )]
    [TextField( "Forms", "The forms to show.", false, "", "CustomSetting" )]
    [CodeEditorField( "Confirmation Text", "", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, "", "CustomSetting" )]

    #endregion

    public partial class PersonAttributeForms : RockBlockCustomSettings
    {
        #region Fields

        private Person _person = null;

        private string _mode = "VIEW";
        private bool _saveNavigationHistory = false;
        public decimal PercentComplete = 0;

        #endregion Fields

        #region Properties

        private List<AttributeForm> FormState { get; set; }

        private Dictionary<PersonFieldType, string> PersonValueState { get; set; }
        private Dictionary<int, string> AttributeValueState { get; set; }

        /// <summary>
        /// Gets the settings tool tip.
        /// </summary>
        /// <value>
        /// The settings tool tip.
        /// </value>
        public override string SettingsToolTip
        {
            get
            {
                return "Edit Forms and Fields";
            }
        }

        // The current page index
        private int CurrentPageIndex { get; set; }

        protected decimal ProgressBarSteps
        {
            get { return ViewState["ProgressBarSteps"] as decimal? ?? 1.0m; }
            set { ViewState["ProgressBarSteps"] = value; }
        }

        #endregion Properties

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            _mode = ViewState["Mode"].ToString();

            string json = ViewState["FormState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                FormState = new List<AttributeForm>();
            }
            else
            {
                FormState = JsonConvert.DeserializeObject<List<AttributeForm>>( json );
            }

            json = ViewState["PersonValueState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                PersonValueState = new Dictionary<PersonFieldType, string>();
            }
            else
            {
                PersonValueState = JsonConvert.DeserializeObject<Dictionary<PersonFieldType, string>>( json );
            }

            json = ViewState["AttributeValueState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                AttributeValueState = new Dictionary<int, string>();
            }
            else
            {
                AttributeValueState = JsonConvert.DeserializeObject<Dictionary<int, string>>( json );
            }

            CurrentPageIndex = ViewState["CurrentPageIndex"] as int? ?? 0;

            if ( _mode == "VIEW" )
            {
                BuildViewControls( false );
            }
            else
            {
                BuildEditControls( false );
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            BlockUpdated += AttributeForm_BlockUpdated;
            AddConfigurationUpdateTrigger( upnlContent );

            RegisterClientScript();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var sm = ScriptManager.GetCurrent( Page );
            sm.Navigate += Sm_Navigate;

            nbMain.Visible = false;

            var personMode = GetAttributeValue( "PersonMode" );
            var errorMsg = "";
            _person = CurrentPerson;

            var paramPersonGuid = PageParameter( "Person" ).AsGuidOrNull();
            Person paramPerson = null;
            if ( paramPersonGuid != null )
            {
                paramPerson = new PersonService( new RockContext() ).Get( paramPersonGuid.Value );

                if ( paramPerson != null )
                {
                    if ( personMode == "Logged in Person only" )
                    {
                        errorMsg = "You are currently in Logged in Person Only mode.";
                    }
                    else if ( personMode == "Family Members" )
                    {
                        var foundCurrentPerson = false;
                        foreach ( var member in paramPerson.GetFamilyMembers( true ) )
                        {
                            if ( member.PersonId == CurrentPerson.Id )
                            {
                                foundCurrentPerson = true;
                            }
                        }
                        if ( foundCurrentPerson )
                        {
                            _person = paramPerson;
                        }
                        else
                        {
                            errorMsg = "You must be a family member to fill out the form for this person.";
                        }
                    }
                    else
                    {
                        _person = paramPerson;
                    }
                }
                else
                {
                    errorMsg = "Person not found with selected Guid.";
                }
            }
            if ( errorMsg != "" )
            {
                nbMain.Title = "Sorry";
                nbMain.Text = string.Format( "{0} Continuing as current logged in person.", errorMsg );
                nbMain.NotificationBoxType = NotificationBoxType.Warning;
                nbMain.Visible = true;
            }
            if ( _person != null )
            {
                if ( !Page.IsPostBack )
                {
                    var displayFamilyMemberPicker = GetAttributeValue( "DisplayFamilyMemberPicker" ).AsBoolean();

                    if ( displayFamilyMemberPicker && personMode != "Logged in Person only" )
                    {
                        var familyMembers = _person.GetFamilyMembers( true )
                                        .Select( m => m.Person )
                                        .ToList();

                        if ( familyMembers.Count() > 1 )
                        {
                            pnlFamilyMembers.Visible = true;
                            ddlFamilyMembers.Items.Add( new ListItem() );

                            foreach ( var familyMember in familyMembers )
                            {
                                ListItem listItem = new ListItem( familyMember.FullName, familyMember.Guid.ToString() );
                                listItem.Selected = familyMember.Id == _person.Id;
                                ddlFamilyMembers.Items.Add( listItem );
                            }
                        }
                    }

                    ShowDetail();
                }
                else
                {
                    pnlFamilyMembers.Visible = false;

                    ShowDialog();

                    if ( _mode == "VIEW" )
                    {
                        ParseViewControls();
                    }

                    if ( _mode == "EDIT" )
                    {
                        string postbackArgs = Request.Params["__EVENTARGUMENT"];
                        if ( !string.IsNullOrWhiteSpace( postbackArgs ) )
                        {
                            string[] nameValue = postbackArgs.Split( new char[] { ':' } );
                            if ( nameValue.Count() == 2 )
                            {
                                string[] values = nameValue[1].Split( new char[] { ';' } );
                                if ( values.Count() == 2 )
                                {
                                    Guid guid = values[0].AsGuid();
                                    int newIndex = values[1].AsInteger();

                                    switch ( nameValue[0] )
                                    {
                                        case "re-order-form":
                                            SortForms( guid, newIndex );
                                            break;
                                        case "cancel-dlg-field":
                                            BuildEditControls( true );
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                nbMain.Title = "Sorry";
                nbMain.Text = "You need to login before entering information on this page.";
                nbMain.NotificationBoxType = NotificationBoxType.Warning;
                nbMain.Visible = true;
            }
        }

        protected override object SaveViewState()
        {
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            ViewState["FormState"] = JsonConvert.SerializeObject( FormState, Formatting.None, jsonSetting );
            ViewState["PersonValueState"] = JsonConvert.SerializeObject( PersonValueState, Formatting.None, jsonSetting );
            ViewState["AttributeValueState"] = JsonConvert.SerializeObject( AttributeValueState, Formatting.None, jsonSetting );
            ViewState["CurrentPageIndex"] = CurrentPageIndex;
            ViewState["Mode"] = _mode;

            return base.SaveViewState();
        }

        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );

            if ( _saveNavigationHistory )
            {
                this.AddHistory( "form", CurrentPageIndex.ToString() );
            }
        }

        #endregion Base Control Methods

        #region Events

        private void Sm_Navigate( object sender, HistoryEventArgs e )
        {
            var state = e.State["form"];

            if ( state != null )
            {
                CurrentPageIndex = state.AsInteger();
            }
            else
            {
                CurrentPageIndex = 0;
            }

            ShowPage();
        }

        protected void lbPrev_Click( object sender, EventArgs e )
        {
            _saveNavigationHistory = true;

            CurrentPageIndex--;

            ShowPage();

            hfTriggerScroll.Value = "true";
        }

        protected void lbNext_Click( object sender, EventArgs e )
        {
            _saveNavigationHistory = true;

            CurrentPageIndex++;

            bool saveEachPage = GetAttributeValue( "SaveValues" ) == "PAGE";
            if ( saveEachPage || CurrentPageIndex >= FormState.Count )
            {
                if ( _person != null && _person.Id > 0 )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var person = new PersonService( rockContext ).Get( _person.Id );
                        if ( person != null )
                        {
                            var pagePersonFields = new List<PersonFieldType>();
                            if ( saveEachPage && CurrentPageIndex > 0 && CurrentPageIndex <= FormState.Count )
                            {
                                pagePersonFields = FormState[CurrentPageIndex - 1].Fields
                                    .Where( f => f.FieldSource == FormFieldSource.PersonField )
                                    .Select( f => f.PersonFieldType )
                                    .ToList();
                            }

                            int? campusId = null;
                            int? locationId = null;

                            foreach ( var keyVal in PersonValueState )
                            {
                                if ( CurrentPageIndex >= FormState.Count || !pagePersonFields.Any() || pagePersonFields.Contains( keyVal.Key ) )
                                {
                                    var fieldValue = keyVal.Value;

                                    switch ( keyVal.Key )
                                    {
                                        case PersonFieldType.FirstName:
                                            {
                                                var newName = fieldValue.ToString() ?? string.Empty;

                                                var updateBoth = false;
                                                if ( person.FirstName == person.NickName || string.IsNullOrWhiteSpace( person.NickName ) )
                                                {
                                                    updateBoth = true;
                                                }

                                                person.FirstName = newName;

                                                if ( updateBoth )
                                                {
                                                    person.NickName = newName;
                                                }

                                                break;
                                            }

                                        case PersonFieldType.LastName:
                                            {
                                                var newLastName = fieldValue.ToString() ?? string.Empty;
                                                person.LastName = newLastName;
                                                break;
                                            }

                                        case PersonFieldType.MiddleName:
                                            {
                                                person.MiddleName = fieldValue.ToString() ?? string.Empty;
                                                break;
                                            }

                                        case PersonFieldType.Campus:
                                            {
                                                if ( fieldValue != null )
                                                {
                                                    campusId = fieldValue.ToString().AsIntegerOrNull();
                                                }
                                                break;
                                            }

                                        case PersonFieldType.Address:
                                            {
                                                locationId = fieldValue.ToString().AsIntegerOrNull();
                                                break;
                                            }

                                        case PersonFieldType.Birthdate:
                                            {
                                                var birthMonth = person.BirthMonth;
                                                var birthDay = person.BirthDay;
                                                var birthYear = person.BirthYear;

                                                person.SetBirthDate( fieldValue.AsDateTime() );

                                                break;
                                            }

                                        case PersonFieldType.Grade:
                                            {
                                                var newGraduationYear = fieldValue.ToString().AsIntegerOrNull();
                                                person.GraduationYear = newGraduationYear;

                                                break;
                                            }

                                        case PersonFieldType.Gender:
                                            {
                                                var newGender = fieldValue.ToString().ConvertToEnumOrNull<Gender>() ?? Gender.Unknown;
                                                person.Gender = newGender;
                                                break;
                                            }

                                        case PersonFieldType.MaritalStatus:
                                            {
                                                if ( fieldValue != null )
                                                {
                                                    int? newMaritalStatusId = fieldValue.ToString().AsIntegerOrNull();
                                                    person.MaritalStatusValueId = newMaritalStatusId;
                                                }
                                                break;
                                            }

                                        case PersonFieldType.AnniversaryDate:
                                            {
                                                person.AnniversaryDate = fieldValue.AsDateTime();
                                                break;
                                            }

                                        case PersonFieldType.MobilePhone:
                                            {
                                                SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                                                break;
                                            }

                                        case PersonFieldType.HomePhone:
                                            {
                                                SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
                                                break;
                                            }

                                        case PersonFieldType.WorkPhone:
                                            {
                                                SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );
                                                break;
                                            }

                                        case PersonFieldType.Email:
                                            {
                                                var newEmail = fieldValue.ToString() ?? string.Empty;
                                                person.Email = newEmail;
                                                break;
                                            }

                                        case PersonFieldType.ConnectionStatus:
                                            {
                                                var newConnectionStatusId = fieldValue.ToString().AsIntegerOrNull() ?? DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT ).Id;
                                                person.ConnectionStatusValueId = newConnectionStatusId;
                                                break;
                                            }
                                    }
                                }
                            }

                            var saveChangeResult = rockContext.SaveChanges();

                            // Set the family guid for any other registrants that were selected to be in the same family
                            var family = person.GetFamilies( rockContext ).FirstOrDefault();
                            if ( family != null )
                            {
                                if ( campusId.HasValue )
                                {
                                    if ( family.CampusId != campusId )
                                    {
                                        family.CampusId = campusId;
                                    }
                                }

                                if ( locationId.HasValue )
                                {
                                    var location = new LocationService( new RockContext() ).Get( ( int ) locationId );
                                    if ( location != null )
                                    {
                                        var homeLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
                                        if ( homeLocationType != null )
                                        {
                                            var familyGroup = new GroupService( rockContext ).Get( family.Id );
                                            if ( familyGroup != null )
                                            {
                                                GroupService.AddNewGroupAddress(
                                                    rockContext,
                                                    familyGroup,
                                                    Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME,
                                                    location.Street1, location.Street2, location.City, location.State, location.PostalCode, location.Country, true );

                                                //
                                                // note v7 will automatically make a new home address mailing and mark as mapping
                                                //
                                                {
                                                    var newLocation = familyGroup.GroupLocations.FirstOrDefault( l => l.LocationId == locationId && l.IsMailingLocation == false );
                                                    if ( newLocation != null )
                                                    {
                                                        newLocation.IsMailingLocation = true;
                                                        newLocation.IsMappedLocation = true;
                                                        rockContext.SaveChanges();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            person.LoadAttributes( rockContext );

                            var pageAttributeIds = new List<int>();

                            if ( saveEachPage && CurrentPageIndex > 0 && ( CurrentPageIndex - 1 ) <= FormState.Count )
                            {
                                pageAttributeIds = FormState[CurrentPageIndex - 1].Fields
                                    .Where( f => f.AttributeId.HasValue )
                                    .Select( f => f.AttributeId.Value )
                                    .ToList();
                            }

                            foreach ( var keyVal in AttributeValueState )
                            {
                                var attribute = AttributeCache.Get( keyVal.Key );
                                if ( attribute != null && ( CurrentPageIndex >= FormState.Count || !pageAttributeIds.Any() || pageAttributeIds.Contains( attribute.Id ) ) )
                                {
                                    person.SetAttributeValue( attribute.Key, keyVal.Value );
                                }
                            }

                            person.SaveAttributeValues( rockContext );

                            if ( CurrentPageIndex >= FormState.Count )
                            {
                                int? connectionRequestId = null;
                                if ( GetAttributeValue( "AllowConnectionOpportunity" ).AsBoolean() )
                                {
                                    var opportunityService = new ConnectionOpportunityService( rockContext );
                                    var connectionRequestService = new ConnectionRequestService( rockContext );

                                    var personCampus = person.GetCampus();
                                    if ( personCampus != null )
                                    {
                                        campusId = personCampus.Id;
                                    }

                                    var opportunities = RockPage.PageParameter( "OpportunityId" ).SplitDelimitedValues().AsIntegerList();
                                    foreach ( var opportunityId in opportunities )
                                    {
                                        var opportunity = opportunityService
                                            .Queryable()
                                            .Where( o => o.Id == opportunityId )
                                            .FirstOrDefault();

                                        int defaultStatusId = opportunity.ConnectionType.ConnectionStatuses
                                            .Where( s => s.IsDefault )
                                            .Select( s => s.Id )
                                            .FirstOrDefault();

                                        // If opportunity is valid and has a default status
                                        if ( opportunity != null && defaultStatusId > 0 )
                                        {
                                            var connectionRequest = new ConnectionRequest();
                                            connectionRequest.PersonAliasId = person.PrimaryAliasId.Value;
                                            connectionRequest.Comments = string.Empty;
                                            connectionRequest.ConnectionOpportunityId = opportunity.Id;
                                            connectionRequest.ConnectionState = ConnectionState.Active;
                                            connectionRequest.ConnectionStatusId = defaultStatusId;
                                            connectionRequest.CampusId = campusId;
                                            connectionRequest.ConnectorPersonAliasId = opportunity.GetDefaultConnectorPersonAliasId( campusId );
                                            if ( campusId.HasValue &&
                                                opportunity != null &&
                                                opportunity.ConnectionOpportunityCampuses != null )
                                            {
                                                var campus = opportunity.ConnectionOpportunityCampuses
                                                    .Where( c => c.CampusId == campusId.Value )
                                                    .FirstOrDefault();
                                                if ( campus != null )
                                                {
                                                    connectionRequest.ConnectorPersonAliasId = campus.DefaultConnectorPersonAliasId;
                                                }
                                            }

                                            if ( !connectionRequest.IsValid )
                                            {
                                                // Controls will show warnings
                                                return;
                                            }

                                            connectionRequestService.Add( connectionRequest );

                                            rockContext.SaveChanges();

                                            // get id for workflow
                                            if ( opportunities.Count == 1 )
                                            {
                                                connectionRequestId = connectionRequest.Id;
                                            }
                                        }
                                    }
                                }

                                var urlConnectionRequestId = PageParameter( "ConnectionRequestId" ).AsIntegerOrNull();
                                if ( urlConnectionRequestId.HasValue && !connectionRequestId.HasValue )
                                {
                                    var request = new ConnectionRequestService( rockContext ).Get( urlConnectionRequestId.Value );

                                    if ( request != null )
                                    {
                                        connectionRequestId = request.Id;
                                    }
                                }

                                int? groupMemberId = null;
                                if ( GetAttributeValue( "AllowGroupMembership" ).AsBoolean() )
                                {
                                    Group group = null;
                                    GroupTypeRole defaultGroupRole = null;
                                    var groupService = new GroupService( rockContext );
                                    bool groupIsFromQryString = true;

                                    Guid? groupGuid = GetAttributeValue( "Group" ).AsGuidOrNull();
                                    if ( groupGuid.HasValue )
                                    {
                                        group = groupService.Get( groupGuid.Value );
                                        groupIsFromQryString = false;
                                    }

                                    if ( group == null )
                                    {
                                        groupGuid = PageParameter( "GroupGuid" ).AsGuidOrNull();
                                        if ( groupGuid.HasValue )
                                        {
                                            group = groupService.Get( groupGuid.Value );
                                        }
                                    }

                                    if ( group == null && GetAttributeValue( "EnablePassingGroupId" ).AsBoolean() )
                                    {
                                        int? groupId = PageParameter( "GroupId" ).AsIntegerOrNull();
                                        if ( groupId.HasValue )
                                        {
                                            group = groupService.Get( groupId.Value );
                                        }
                                    }

                                    if ( group != null )
                                    {
                                        var groupTypeGuids = this.GetAttributeValue( "AllowedGroupTypes" ).SplitDelimitedValues().AsGuidList();

                                        if ( groupIsFromQryString && groupTypeGuids.Any() && !groupTypeGuids.Contains( group.GroupType.Guid ) )
                                        {
                                            group = null;
                                        }
                                        else
                                        {
                                            defaultGroupRole = group.GroupType.DefaultGroupRole;
                                        }

                                        if ( group != null )
                                        {
                                            if ( !group.Members
                                                .Any( m =>
                                                    m.PersonId == person.Id &&
                                                    m.GroupRoleId == defaultGroupRole.Id ) )
                                            {
                                                var groupMemberService = new GroupMemberService( rockContext );
                                                var groupMember = new GroupMember();
                                                groupMember.PersonId = person.Id;
                                                groupMember.GroupRoleId = defaultGroupRole.Id;
                                                groupMember.GroupMemberStatus = ( GroupMemberStatus ) GetAttributeValue( "GroupMemberStatus" ).AsInteger();
                                                groupMember.GroupId = group.Id;
                                                groupMemberService.Add( groupMember );
                                                rockContext.SaveChanges();

                                                // get id for workflow
                                                groupMemberId = groupMember.Id;
                                            }
                                            else
                                            {
                                                groupMemberId = group.Members
                                                .FirstOrDefault( m =>
                                                    m.PersonId == person.Id &&
                                                    m.GroupRoleId == defaultGroupRole.Id )
                                                .Id;
                                            }
                                        }
                                    }
                                }

                                Guid? workflowTypeGuid = GetAttributeValue( "Workflow" ).AsGuidOrNull();
                                if ( workflowTypeGuid.HasValue )
                                {
                                    var workflowType = WorkflowTypeCache.Get( workflowTypeGuid.Value );
                                    if ( workflowType != null && ( workflowType.IsActive ?? true ) )
                                    {
                                        try
                                        {
                                            var workflowEntity = GetAttributeValue( "WorkflowEntity" );

                                            if ( workflowEntity.Equals( "ConnectionRequest" ) && connectionRequestId.HasValue )
                                            {
                                                ConnectionRequest connectionRequest = null;
                                                connectionRequest = new ConnectionRequestService( rockContext ).Get( connectionRequestId.Value );
                                                if ( connectionRequest != null )
                                                {
                                                    var workflow = Workflow.Activate( workflowType, person.FullName );
                                                    List<string> workflowErrors;
                                                    new WorkflowService( rockContext ).Process( workflow, connectionRequest, out workflowErrors );
                                                }
                                            }
                                            else if ( workflowEntity.Equals( "GroupMember" ) && groupMemberId.HasValue )
                                            {
                                                GroupMember groupMember = null;
                                                groupMember = new GroupMemberService( rockContext ).Get( groupMemberId.Value );
                                                if ( groupMember != null )
                                                {
                                                    var workflow = Workflow.Activate( workflowType, person.FullName );
                                                    List<string> workflowErrors;
                                                    new WorkflowService( rockContext ).Process( workflow, groupMember, out workflowErrors );
                                                }
                                            }
                                            else
                                            {
                                                var workflow = Workflow.Activate( workflowType, person.FullName );
                                                List<string> workflowErrors;
                                                new WorkflowService( rockContext ).Process( workflow, person, out workflowErrors );
                                            }
                                        }
                                        catch ( Exception ex )
                                        {
                                            ExceptionLogService.LogException( ex, this.Context );
                                        }
                                    }
                                }

                                if ( GetAttributeValue( "DonePage" ).AsGuidOrNull().HasValue )
                                {
                                    NavigateToLinkedPage( "DonePage" );
                                }
                                else
                                {
                                    pnlView.Visible = false;
                                    litConfirmationText.Visible = true;
                                    litConfirmationText.Text = GetAttributeValue( "ConfirmationText" );
                                }
                                upnlContent.Update();
                            }
                            else
                            {
                                ShowPage();
                                hfTriggerScroll.Value = "true";
                            }
                        }
                    }
                }
            }
            else
            {
                ShowPage();
                hfTriggerScroll.Value = "true";
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the AttributeForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void AttributeForm_BlockUpdated( object sender, EventArgs e )
        {
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            SetAttributeValue( "DisplayProgressBar", cbDisplayProgressBar.Checked.ToString() );
            SetAttributeValue( "SaveValues", ddlSaveValues.SelectedValue );

            var workflowTypeId = wtpWorkflow.SelectedValueAsInt();
            if ( workflowTypeId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var workflowType = new WorkflowTypeService( rockContext ).Get( workflowTypeId.Value );
                    if ( workflowType != null )
                    {
                        SetAttributeValue( "Workflow", workflowType.Guid.ToString() );
                    }
                    else
                    {
                        SetAttributeValue( "Workflow", "" );
                    }
                }
            }
            else
            {
                SetAttributeValue( "Workflow", "" );
            }
            SetAttributeValue( "WorkflowEntity", ddlWorkflowEntity.SelectedValue );

            var ppFieldType = new PageReferenceFieldType();
            SetAttributeValue( "DonePage", ppFieldType.GetEditValue( ppDonePage, null ) );

            ParseEditControls();
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            string json = JsonConvert.SerializeObject( FormState, Formatting.None, jsonSetting );
            SetAttributeValue( "Forms", json );

            SetAttributeValue( "ConfirmationText", ceConfirmationText.Text );

            SaveAttributeValues();

            mdEdit.Hide();
            pnlEditModal.Visible = false;

            ShowDetail();

            upnlContent.Update();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            ShowDetail();

            upnlContent.Update();
        }

        private void ddlCountry_indexChanged( object sender, EventArgs e )
        {
            upnlContent.Update();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlFamilyMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlFamilyMembers_SelectedIndexChanged( object sender, EventArgs e )
        {
            var qs = new Dictionary<string, string>();
            qs.Add( "Person", ddlFamilyMembers.SelectedValueAsGuid().ToString() );
            NavigateToCurrentPageReference( qs );
        }

        #region Form Control Events

        /// <summary>
        /// Handles the Click event of the lbAddForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddForm_Click( object sender, EventArgs e )
        {
            ParseEditControls();

            var form = new AttributeForm();
            form.Guid = Guid.NewGuid();
            form.Expanded = true;
            form.Order = FormState.Any() ? FormState.Max( a => a.Order ) + 1 : 0;
            FormState.Add( form );

            BuildEditControls( true, form.Guid );
        }

        /// <summary>
        /// Handles the DeleteFormClick event of the tfeForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void tfeForm_DeleteFormClick( object sender, EventArgs e )
        {
            ParseEditControls();

            var attributeFormEditor = sender as PersonAttributeFormEditor;
            if ( attributeFormEditor != null )
            {
                var form = FormState.FirstOrDefault( a => a.Guid == attributeFormEditor.FormGuid );
                if ( form != null )
                {
                    FormState.Remove( form );
                }
            }

            BuildEditControls( true );
        }

        /// <summary>
        /// Tfes the form_ add attribute click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void tfeForm_AddFieldClick( object sender, AttributeFormFieldEventArg e )
        {
            ParseEditControls();

            ShowFormFieldEdit( e.FormGuid, Guid.NewGuid() );
        }

        /// <summary>
        /// Handles the filter field click on the registrationTemplateFormEditor.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void tfeForm_FilterFieldClick( object sender, AttributeFormFieldEventArg e )
        {
            ParseEditControls();

            ShowFormFieldFilter( e.FormGuid, e.FormFieldGuid );

            BuildEditControls( true );
        }

        /// <summary>
        /// Tfes the form_ edit attribute click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void tfeForm_EditFieldClick( object sender, AttributeFormFieldEventArg e )
        {
            ParseEditControls();

            ShowFormFieldEdit( e.FormGuid, e.FormFieldGuid );
        }

        /// <summary>
        /// Tfes the form_ reorder attribute click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void tfeForm_ReorderFieldClick( object sender, AttributeFormFieldEventArg e )
        {
            ParseEditControls();

            var form = FormState.FirstOrDefault( f => f.Guid == e.FormGuid );
            if ( form != null )
            {
                SortFields( form.Fields, e.OldIndex, e.NewIndex );
                ReOrderFields( form.Fields );
            }

            BuildEditControls( true, e.FormGuid );
        }

        /// <summary>
        /// Tfes the form_ delete attribute click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void tfeForm_DeleteFieldClick( object sender, AttributeFormFieldEventArg e )
        {
            ParseEditControls();

            var form = FormState.FirstOrDefault( f => f.Guid == e.FormGuid );
            if ( form != null )
            {
                var field = form.Fields.FirstOrDefault( f => f.Guid == e.FormFieldGuid );
                if ( field != null )
                {
                    /*
                      On Field Delete, we need to also remove all the existing reference of current field in filter rule list
                    */
                    var newFormFieldsWithRules = form.Fields
                        .Where( a => a.FieldVisibilityRules.RuleList.Any()
                         && a.FieldVisibilityRules.RuleList.Any( b =>
                             b.ComparedToFormFieldGuid.HasValue
                             && b.ComparedToFormFieldGuid == e.FormFieldGuid ) );

                    foreach ( var newFormField in newFormFieldsWithRules )
                    {
                        newFormField.FieldVisibilityRules.RuleList
                            .RemoveAll( a => a.ComparedToFormFieldGuid.HasValue
                            && a.ComparedToFormFieldGuid.Value == e.FormFieldGuid );
                    }

                    form.Fields.Remove( field );
                }
            }

            BuildEditControls( true, e.FormGuid );
        }

        /// <summary>
        /// Tfes the form_ rebind attribute click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void tfeForm_RebindFieldClick( object sender, AttributeFormFieldEventArg e )
        {
            ParseEditControls();

            BuildEditControls( true, e.FormGuid );
        }

        #endregion Form Control Events

        #region Field Dialog Events

        /// <summary>
        /// Handles the SaveClick event of the dlgField control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dlgField_SaveClick( object sender, EventArgs e )
        {
            var formGuid = hfFormGuid.Value.AsGuid();
            var attributeGuid = hfAttributeGuid.Value.AsGuid();

            var form = FormState.FirstOrDefault( f => f.Guid == formGuid );

            if ( form == null && formGuid == Guid.Empty )
            {
                form = FormState.FirstOrDefault( f => f.Guid == Guid.Empty );
            }

            if ( form != null )
            {
                var field = form.Fields.FirstOrDefault( a => a.Guid.Equals( attributeGuid ) );
                if ( field == null )
                {
                    field = new AttributeFormField();
                    field.Order = form.Fields.Any() ? form.Fields.Max( a => a.Order ) + 1 : 0;
                    field.Guid = attributeGuid;
                    field.FieldSource = ddlFieldSource.SelectedValueAsEnum<FormFieldSource>();
                    field.RegistrationFieldSource = ddlFieldSource.SelectedValueAsEnum<RegistrationFieldSource>();
                    form.Fields.Add( field );
                }

                field.PreText = ceAttributePreText.Text;
                field.PostText = ceAttributePostText.Text;

                switch ( field.FieldSource )
                {
                    case FormFieldSource.PersonField:
                        {
                            if ( ddlPersonField.Visible )
                            {
                                field.PersonFieldType = ddlPersonField.SelectedValueAsEnum<PersonFieldType>();
                                // Due to our Enum and Core RegistrationPersonFieldType Enum not matching we have 
                                // to manually ensure anniversary date and middle name are the right values.
                                switch ( field.PersonFieldType )
                                {
                                    case PersonFieldType.AnniversaryDate:
                                        field.RegistrationPersonFieldType = RegistrationPersonFieldType.AnniversaryDate;
                                        break;
                                    case PersonFieldType.MiddleName:
                                        field.RegistrationPersonFieldType = RegistrationPersonFieldType.MiddleName;
                                        break;
                                    default:
                                        field.RegistrationPersonFieldType = ddlPersonField.SelectedValueAsEnum<RegistrationPersonFieldType>();
                                        break;
                                }
                            }
                            break;
                        }
                    case FormFieldSource.PersonAttribute:
                        {
                            field.AttributeId = ddlPersonAttributes.SelectedValueAsInt();
                            if ( field.AttributeId.HasValue )
                            {
                                field.Guid = field.Attribute.Guid;
                            }
                            break;
                        }
                }

                field.ShowCurrentValue = cbUsePersonCurrentValue.Checked;
                field.IsRequired = cbRequireInInitialEntry.Checked;
            }

            HideDialog();

            BuildEditControls( true );
        }

        #endregion Field Dialog Events

        #region Registrant Forms/FieldFilter Methods

        /// <summary>
        /// Shows the form field filter.
        /// </summary>
        /// <param name="formGuid">The form unique identifier.</param>
        /// <param name="formFieldGuid">The form field unique identifier.</param>
        private void ShowFormFieldFilter( Guid formGuid, Guid formFieldGuid )
        {

            BuildEditControls( true );

            var form = FormState.FirstOrDefault( f => f.Guid == formGuid );

            if ( form == null && formGuid == Guid.Empty )
            {
                form = FormState.FirstOrDefault( f => f.Guid == Guid.Empty );
            }

            if ( form != null )
            {
                ShowDialog( dlgFieldFilter );

                hfFormGuidFilter.Value = formGuid.ToString();
                hfFormFieldGuidFilter.Value = formFieldGuid.ToString();
                var formField = form.Fields.FirstOrDefault( a => a.Guid == formFieldGuid );
                var otherFormFields = form.Fields.Where( a => a != formField && a.FieldSource != FormFieldSource.PersonField ).ToList();

                fvreFieldVisibilityRulesEditor.ValidationGroup = dlgFieldFilter.ValidationGroup;
                fvreFieldVisibilityRulesEditor.FieldName = formField.ToString();
                fvreFieldVisibilityRulesEditor.ComparableFields = otherFormFields.ToDictionary( aff => aff.Guid, aff => new FieldVisibilityRuleField
                {
                    Guid = aff.Guid,
                    Attribute = aff.AttributeObj,
                    PersonFieldType = aff.RegistrationPersonFieldType,
                    FieldSource = aff.RegistrationFieldSource
                } );
                fvreFieldVisibilityRulesEditor.SetFieldVisibilityRules( formField.FieldVisibilityRules );
            }

            BuildEditControls( true );
        }

        /// <summary>
        /// Handles the SaveClick event of the dlgFieldFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dlgFieldFilter_SaveClick( object sender, EventArgs e )
        {
            Guid formGuid = hfFormGuidFilter.Value.AsGuid();
            Guid formFieldGuid = hfFormFieldGuidFilter.Value.AsGuid();
            var formField = FormState.FirstOrDefault( f => f.Guid == formGuid ).Fields.FirstOrDefault( a => a.Guid == formFieldGuid );
            formField.FieldVisibilityRules = fvreFieldVisibilityRulesEditor.GetFieldVisibilityRules();

            HideDialog();

            BuildEditControls( true );
        }
        #endregion Registrant Forms/FieldFilter Methods

        #endregion Events

        #region Methods

        #region View Mode

        private void ShowDetail()
        {
            _mode = "VIEW";

            pnlEditModal.Visible = false;

            string json = GetAttributeValue( "Forms" );
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                FormState = new List<AttributeForm>();
            }
            else
            {
                FormState = JsonConvert.DeserializeObject<List<AttributeForm>>( json );
            }

            if ( FormState.Count > 0 )
            {
                pnlView.Visible = true;

                PersonValueState = new Dictionary<PersonFieldType, string>();
                AttributeValueState = new Dictionary<int, string>();
                if ( _person != null )
                {
                    if ( _person.Attributes == null )
                    {
                        _person.LoadAttributes();
                    }

                    foreach ( var form in FormState )
                    {
                        foreach ( var field in form.Fields
                            .Where( a =>
                                a.ShowCurrentValue == true ) )
                        {
                            if ( field.FieldSource == FormFieldSource.PersonField )
                            {
                                switch ( field.PersonFieldType )
                                {
                                    case PersonFieldType.FirstName:
                                        {
                                            var value = _person.FirstName;
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.FirstName, value );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.LastName:
                                        {
                                            var value = _person.LastName;
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.LastName, value );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.MiddleName:
                                        {
                                            var value = _person.MiddleName;
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.MiddleName, value );
                                            }
                                            break;
                                        }
                                    case PersonFieldType.Campus:
                                        {
                                            var campus = _person.GetCampus();
                                            if ( campus != null )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.Campus, campus.Id.ToString() );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.Address:
                                        {
                                            var homeLocation = _person.GetHomeLocation();
                                            if ( homeLocation != null )
                                            {
                                                int? locationId = homeLocation.Id;
                                                PersonValueState.AddOrReplace( PersonFieldType.Address, locationId.ToString() );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.Birthdate:
                                        {
                                            var value = _person.BirthDate.ToString();
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.Birthdate, value );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.Grade:
                                        {
                                            var value = _person.GraduationYear.ToString();
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.Grade, value );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.Gender:
                                        {
                                            var value = _person.Gender.ToString();
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.Gender, value );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.MaritalStatus:
                                        {
                                            var value = _person.MaritalStatusValueId.ToString();
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.MaritalStatus, value );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.AnniversaryDate:
                                        {
                                            var value = _person.AnniversaryDate.ToString();
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.AnniversaryDate, value );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.MobilePhone:
                                        {
                                            var phone = _person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValue.Guid.Equals( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ) );
                                            if ( phone != null )
                                            {
                                                var value = phone.Number;
                                                if ( !string.IsNullOrWhiteSpace( value ) )
                                                {
                                                    PersonValueState.AddOrReplace( PersonFieldType.MobilePhone, value + "^" + phone.IsMessagingEnabled.ToString() );
                                                }
                                            }
                                            break;
                                        }

                                    case PersonFieldType.HomePhone:
                                        {
                                            var phone = _person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValue.Guid.Equals( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() ) );
                                            if ( phone != null )
                                            {
                                                var value = phone.Number;
                                                if ( !string.IsNullOrWhiteSpace( value ) )
                                                {
                                                    PersonValueState.AddOrReplace( PersonFieldType.HomePhone, value );
                                                }
                                            }
                                            break;
                                        }

                                    case PersonFieldType.WorkPhone:
                                        {
                                            var phone = _person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValue.Guid.Equals( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() ) );
                                            if ( phone != null )
                                            {
                                                var value = phone.Number;
                                                if ( !string.IsNullOrWhiteSpace( value ) )
                                                {
                                                    PersonValueState.AddOrReplace( PersonFieldType.WorkPhone, value );
                                                }
                                            }
                                            break;
                                        }

                                    case PersonFieldType.Email:
                                        {
                                            var value = _person.Email;
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.Email, value );
                                            }
                                            break;
                                        }

                                    case PersonFieldType.ConnectionStatus:
                                        {
                                            var value = _person.ConnectionStatusValueId.ToString();
                                            if ( !string.IsNullOrWhiteSpace( value ) )
                                            {
                                                PersonValueState.AddOrReplace( PersonFieldType.ConnectionStatus, value );
                                            }
                                            break;
                                        }
                                }
                            }
                            else if ( field.AttributeId.HasValue && field.FieldSource == FormFieldSource.PersonAttribute )
                            {
                                var attributeCache = AttributeCache.Get( field.AttributeId.Value );
                                if ( attributeCache != null )
                                {
                                    AttributeValueState.AddOrReplace( field.AttributeId.Value, _person.GetAttributeValue( attributeCache.Key ) );
                                }
                            }
                        }
                    }
                }

                ProgressBarSteps = FormState.Count();
                CurrentPageIndex = 0;
                ShowPage();
            }
            else
            {
                nbMain.Title = "No Forms/Fields";
                nbMain.Text = "No forms or fields have been configured. Use the Block Configuration to add new forms and fields.";
                nbMain.NotificationBoxType = NotificationBoxType.Warning;
                nbMain.Visible = true;
            }
        }

        private void ShowPage()
        {
            decimal currentStep = CurrentPageIndex + 1;
            PercentComplete = ( currentStep / ProgressBarSteps ) * 100.0m;
            pnlProgressBar.Visible = GetAttributeValue( "DisplayProgressBar" ).AsBoolean() && ( FormState.Count > 1 );

            BuildViewControls( true );

            lbPrev.Visible = CurrentPageIndex > 0;
            lbNext.Visible = CurrentPageIndex < FormState.Count;
            lbNext.Text = CurrentPageIndex < FormState.Count() - 1 ? "Next" : "Finish";

            upnlContent.Update();
        }

        private void BuildViewControls( bool setValues )
        {
            phContent.Controls.Clear();

            if ( FormState != null )
            {
                if ( CurrentPageIndex >= FormState.Count )
                {
                    lTitle.Text = "Done!";
                    lHeader.Text = string.Empty;
                    lFooter.Text = string.Empty;
                }
                else
                {
                    var form = FormState[CurrentPageIndex];

                    var mergeFields = LavaHelper.GetCommonMergeFields( RockPage, _person );
                    lTitle.Text = form.Name.ResolveMergeFields( mergeFields );
                    lHeader.Text = form.Header.ResolveMergeFields( mergeFields );
                    lFooter.Text = form.Footer.ResolveMergeFields( mergeFields );

                    foreach ( var field in form.Fields
                        .OrderBy( f => f.Order ) )
                    {
                        bool hasDependantVisibilityRule = form.Fields.Any( a => a.FieldVisibilityRules.RuleList.Any( r => r.ComparedToFormFieldGuid == field.Guid ) );
                        string value = null;
                        if ( field.FieldSource == FormFieldSource.PersonField )
                        {
                            string personFieldValue = null;
                            if ( PersonValueState.ContainsKey( field.PersonFieldType ) )
                            {
                                personFieldValue = PersonValueState[field.PersonFieldType];
                            }

                            CreatePersonField( field, setValues, personFieldValue, hasDependantVisibilityRule );
                        }
                        else if ( field.AttributeId.HasValue && field.FieldSource == FormFieldSource.PersonAttribute )
                        {
                            if ( AttributeValueState.ContainsKey( field.AttributeId.Value ) )
                            {
                                value = AttributeValueState[field.AttributeId.Value];
                            }

                            var attribute = AttributeCache.Get( field.AttributeId.Value );
                            if ( attribute != null )
                            {
                                FieldVisibilityWrapper fieldVisibilityWrapper = new FieldVisibilityWrapper
                                {
                                    ID = "_fieldVisibilityWrapper_attribute_" + attribute.Id.ToString(),
                                    FormFieldId = field.AttributeId.Value,
                                    FieldVisibilityRules = field.FieldVisibilityRules
                                };

                                fieldVisibilityWrapper.EditValueUpdated += ( object sender, FieldVisibilityWrapper.FieldEventArgs args ) =>
                                {
                                    FieldVisibilityWrapper.ApplyFieldVisibilityRules( phContent );
                                    upnlContent.Update();
                                };

                                phContent.Controls.Add( fieldVisibilityWrapper );

                                if ( !string.IsNullOrWhiteSpace( field.PreText ) )
                                {
                                    fieldVisibilityWrapper.Controls.Add( new LiteralControl( field.PreText ) );
                                }

                                var editControl = attribute.AddControl( fieldVisibilityWrapper.Controls, value, BlockValidationGroup, setValues, true, field.IsRequired, null, string.Empty );
                                fieldVisibilityWrapper.EditControl = editControl;

                                if ( !string.IsNullOrWhiteSpace( field.PostText ) )
                                {
                                    fieldVisibilityWrapper.Controls.Add( new LiteralControl( field.PostText ) );
                                }

                                if ( hasDependantVisibilityRule && attribute.FieldType.Field.HasChangeHandler( editControl ) )
                                {
                                    attribute.FieldType.Field.AddChangeHandler( editControl, () =>
                                    {
                                        fieldVisibilityWrapper.TriggerEditValueUpdated( editControl, new FieldVisibilityWrapper.FieldEventArgs( attribute, editControl ) );
                                    } );
                                }

                                if ( attribute.FieldType.Field is AddressFieldType )
                                {
                                    foreach ( var ctrl in phContent.Controls )
                                    {
                                        if ( ctrl is AddressControl )
                                        {
                                            var ac = ( AddressControl ) ctrl;
                                            var ddlCountry = ac.FindControl( "ddlCountry" ) as RockDropDownList;
                                            if ( ddlCountry != null )
                                            {
                                                ddlCountry.SelectedIndexChanged += ddlCountry_indexChanged;
                                            }
                                        }
                                    }
                                }
                            }

                        }

                    }

                    FieldVisibilityWrapper.ApplyFieldVisibilityRules( phContent );
                }
            }
        }

        private void ParseViewControls()
        {
            if ( FormState != null && FormState.Count > CurrentPageIndex )
            {
                var form = FormState[CurrentPageIndex];
                foreach ( var field in form.Fields
                    .OrderBy( f => f.Order ) )
                {
                    if ( field.FieldSource == FormFieldSource.PersonField )
                    {
                        string value = null;
                        switch ( field.PersonFieldType )
                        {
                            case PersonFieldType.FirstName:
                                {
                                    Control control = phContent.FindControl( "tbFirstName" );
                                    if ( control != null )
                                    {
                                        value = ( ( RockTextBox ) control ).Text;
                                    }
                                    break;
                                }

                            case PersonFieldType.LastName:
                                {
                                    Control control = phContent.FindControl( "tbLastName" );
                                    if ( control != null )
                                    {
                                        value = ( ( RockTextBox ) control ).Text;
                                    }
                                    break;
                                }

                            case PersonFieldType.MiddleName:
                                {
                                    Control control = phContent.FindControl( "tbMiddleName" );
                                    if ( control != null )
                                    {
                                        value = ( ( RockTextBox ) control ).Text;
                                    }
                                    break;
                                }

                            case PersonFieldType.Campus:
                                {
                                    Control control = phContent.FindControl( "cpHomeCampus" );
                                    if ( control != null )
                                    {
                                        value = ( ( CampusPicker ) control ).SelectedValue;
                                    }
                                    break;
                                }

                            case PersonFieldType.Address:
                                {
                                    Control control = phContent.FindControl( "acAddress" );
                                    if ( control != null )
                                    {
                                        var address = new AddressFieldType();
                                        var location = new LocationService( new RockContext() ).Get( address.GetEditValue( control, null ).AsGuid() );
                                        if ( location != null )
                                        {
                                            value = location.Id.ToString();
                                        }
                                    }
                                    break;
                                }

                            case PersonFieldType.Email:
                                {
                                    Control control = phContent.FindControl( "tbEmail" );
                                    if ( control != null )
                                    {
                                        value = ( ( EmailBox ) control ).Text;
                                    }
                                    break;
                                }

                            case PersonFieldType.Birthdate:
                                {
                                    Control control = phContent.FindControl( "bpBirthday" );
                                    if ( control != null )
                                    {
                                        value = ( ( BirthdayPicker ) control ).SelectedDate.ToString();
                                    }
                                    break;
                                }

                            case PersonFieldType.Grade:
                                {
                                    Control control = phContent.FindControl( "gpGrade" );
                                    if ( control != null )
                                    {
                                        value = Person.GraduationYearFromGradeOffset( ( ( GradePicker ) control ).SelectedValue.AsIntegerOrNull() ).ToString();
                                    }
                                    break;
                                }

                            case PersonFieldType.Gender:
                                {
                                    Control control = phContent.FindControl( "ddlGender" );
                                    if ( control != null )
                                    {
                                        value = ( ( RockDropDownList ) control ).SelectedValue;
                                    }
                                    break;
                                }

                            case PersonFieldType.MaritalStatus:
                                {
                                    Control control = phContent.FindControl( "dvpMaritalStatus" );
                                    if ( control != null )
                                    {
                                        value = ( ( RockDropDownList ) control ).SelectedValue;
                                    }
                                    break;
                                }

                            case PersonFieldType.AnniversaryDate:
                                {
                                    Control control = phContent.FindControl( "dpAnniversary" );
                                    if ( control != null )
                                    {
                                        value = ( ( DatePicker ) control ).SelectedDate.ToString();
                                    }
                                    break;
                                }

                            case PersonFieldType.MobilePhone:
                                {
                                    var phoneNumber = new PhoneNumber();
                                    var ppMobile = phContent.FindControl( "ppMobile" ) as PhoneNumberBox;
                                    var cbSms = phContent.FindControl( "cbSms" ) as RockCheckBox;
                                    if ( ppMobile != null )
                                    {
                                        phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppMobile.CountryCode );
                                        phoneNumber.Number = PhoneNumber.CleanNumber( ppMobile.Number );
                                        value = phoneNumber.Number;
                                        if ( cbSms != null )
                                        {
                                            value += "^" + cbSms.Checked.ToString();
                                        }
                                    }
                                    break;
                                }
                            case PersonFieldType.HomePhone:
                                {
                                    var phoneNumber = new PhoneNumber();
                                    var ppHome = phContent.FindControl( "ppHome" ) as PhoneNumberBox;
                                    if ( ppHome != null )
                                    {
                                        phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppHome.CountryCode );
                                        phoneNumber.Number = PhoneNumber.CleanNumber( ppHome.Number );
                                        value = phoneNumber.Number;
                                    }
                                    break;
                                }

                            case PersonFieldType.WorkPhone:
                                {
                                    var phoneNumber = new PhoneNumber();
                                    var ppWork = phContent.FindControl( "ppWork" ) as PhoneNumberBox;
                                    if ( ppWork != null )
                                    {
                                        phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppWork.CountryCode );
                                        phoneNumber.Number = PhoneNumber.CleanNumber( ppWork.Number );
                                        value = phoneNumber.Number;
                                    }
                                    break;
                                }

                            case PersonFieldType.ConnectionStatus:
                                {
                                    Control control = phContent.FindControl( "dvpConnectionStatus" );
                                    if ( control != null )
                                    {
                                        value = ( ( RockDropDownList ) control ).SelectedValue;
                                    }
                                    break;
                                }
                        }

                        if ( !string.IsNullOrWhiteSpace( value ) )
                        {
                            PersonValueState.AddOrReplace( field.PersonFieldType, value );
                        }
                    }
                    else if ( field.AttributeId.HasValue && field.FieldSource == FormFieldSource.PersonAttribute )
                    {
                        var attribute = AttributeCache.Get( field.AttributeId.Value );
                        if ( attribute != null )
                        {
                            string fieldId = "attribute_field_" + attribute.Id.ToString();

                            Control control = phContent.FindControl( fieldId );
                            if ( control != null )
                            {
                                string value = attribute.FieldType.Field.GetEditValue( control, attribute.QualifierValues );
                                AttributeValueState.AddOrReplace( attribute.Id, value );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates the person field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="setValue">if set to <c>true</c> [set value].</param>
        /// <param name="fieldValue">The field value.</param>
        private void CreatePersonField( AttributeFormField field, bool setValue, string fieldValue, bool hasDependantVisibilityRule )
        {
            Control personFieldControl = null;

            switch ( field.PersonFieldType )
            {
                case PersonFieldType.FirstName:
                    {
                        var tbFirstName = new RockTextBox();
                        tbFirstName.ID = "tbFirstName";
                        tbFirstName.Label = "First Name";
                        tbFirstName.Required = field.IsRequired;
                        tbFirstName.ValidationGroup = BlockValidationGroup;
                        tbFirstName.AddCssClass( "js-first-name" );
                        personFieldControl = tbFirstName;

                        if ( setValue && fieldValue != null )
                        {
                            tbFirstName.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case PersonFieldType.LastName:
                    {
                        var tbLastName = new RockTextBox();
                        tbLastName.ID = "tbLastName";
                        tbLastName.Label = "Last Name";
                        tbLastName.Required = field.IsRequired;
                        tbLastName.ValidationGroup = BlockValidationGroup;
                        personFieldControl = tbLastName;

                        if ( setValue && fieldValue != null )
                        {
                            tbLastName.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case PersonFieldType.MiddleName:
                    {
                        var tbMiddleName = new RockTextBox();
                        tbMiddleName.ID = "tbMiddleName";
                        tbMiddleName.Label = "Middle Name";
                        tbMiddleName.Required = field.IsRequired;
                        tbMiddleName.ValidationGroup = BlockValidationGroup;
                        personFieldControl = tbMiddleName;

                        if ( setValue && fieldValue != null )
                        {
                            tbMiddleName.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case PersonFieldType.Campus:
                    {
                        var cpHomeCampus = new CampusPicker();
                        cpHomeCampus.ID = "cpHomeCampus";
                        cpHomeCampus.Label = "Campus";
                        cpHomeCampus.Required = field.IsRequired;
                        cpHomeCampus.ValidationGroup = BlockValidationGroup;
                        cpHomeCampus.Campuses = CampusCache.All( false );

                        personFieldControl = cpHomeCampus;

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().AsIntegerOrNull();
                            cpHomeCampus.SelectedCampusId = value;
                        }
                        break;
                    }

                case PersonFieldType.Address:
                    {
                        var acAddress = new AddressControl();
                        acAddress.ID = "acAddress";
                        acAddress.Label = "Address";
                        acAddress.UseStateAbbreviation = true;
                        acAddress.UseCountryAbbreviation = false;
                        acAddress.Required = field.IsRequired;
                        acAddress.ValidationGroup = BlockValidationGroup;

                        var ctrlDDL = acAddress.FindControl( "ddlCountry" ) as RockDropDownList;
                        if ( ctrlDDL != null )
                        {
                            ctrlDDL.SelectedIndexChanged += ddlCountry_indexChanged;
                        }

                        personFieldControl = acAddress;

                        if ( setValue && fieldValue != null )
                        {
                            var locationId = fieldValue.ToString().AsIntegerOrNull();
                            if ( locationId.HasValue )
                            {
                                var location = new LocationService( new RockContext() ).Get( ( int ) locationId );
                                acAddress.SetValues( location );
                            }
                        }

                        break;
                    }

                case PersonFieldType.Email:
                    {
                        var tbEmail = new EmailBox();
                        tbEmail.ID = "tbEmail";
                        tbEmail.Label = "Email";
                        tbEmail.Required = field.IsRequired;
                        tbEmail.ValidationGroup = BlockValidationGroup;
                        personFieldControl = tbEmail;

                        if ( setValue && fieldValue != null )
                        {
                            tbEmail.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case PersonFieldType.Birthdate:
                    {
                        var bpBirthday = new BirthdayPicker();
                        bpBirthday.ID = "bpBirthday";
                        bpBirthday.Label = "Birthday";
                        bpBirthday.Required = field.IsRequired;
                        bpBirthday.ValidationGroup = BlockValidationGroup;
                        personFieldControl = bpBirthday;

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.AsDateTime();
                            bpBirthday.SelectedDate = value;
                        }

                        break;
                    }

                case PersonFieldType.Grade:
                    {
                        var gpGrade = new GradePicker();
                        gpGrade.ID = "gpGrade";
                        gpGrade.Label = "Grade";
                        gpGrade.Required = field.IsRequired;
                        gpGrade.ValidationGroup = BlockValidationGroup;
                        gpGrade.UseAbbreviation = true;
                        gpGrade.UseGradeOffsetAsValue = true;
                        gpGrade.CssClass = "input-width-md";
                        personFieldControl = gpGrade;

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().AsIntegerOrNull();
                            gpGrade.SetValue( Person.GradeOffsetFromGraduationYear( value ) );
                        }

                        break;
                    }

                case PersonFieldType.Gender:
                    {
                        var ddlGender = new RockDropDownList();
                        ddlGender.ID = "ddlGender";
                        ddlGender.Label = "Gender";
                        ddlGender.Required = field.IsRequired;
                        ddlGender.ValidationGroup = BlockValidationGroup;
                        ddlGender.BindToEnum<Gender>( false );

                        // change the 'Unknown' value to be blank instead
                        ddlGender.Items.FindByValue( "0" ).Text = string.Empty;

                        personFieldControl = ddlGender;

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().ConvertToEnumOrNull<Gender>() ?? Gender.Unknown;
                            ddlGender.SetValue( value.ConvertToInt() );
                        }

                        break;
                    }

                case PersonFieldType.MaritalStatus:
                    {
                        var dvpMaritalStatus = new DefinedValuePicker();
                        dvpMaritalStatus.ID = "dvpMaritalStatus";
                        dvpMaritalStatus.Label = "Marital Status";
                        dvpMaritalStatus.Required = field.IsRequired;
                        dvpMaritalStatus.ValidationGroup = BlockValidationGroup;
                        dvpMaritalStatus.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ).Id;
                        personFieldControl = dvpMaritalStatus;

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().AsInteger();
                            dvpMaritalStatus.SetValue( value );
                        }

                        break;
                    }

                case PersonFieldType.AnniversaryDate:
                    {
                        var dpAnniversary = new DatePicker();
                        dpAnniversary.ID = "dpAnniversary";
                        dpAnniversary.Label = "Anniversary Date";
                        dpAnniversary.Required = field.IsRequired;
                        dpAnniversary.ValidationGroup = BlockValidationGroup;
                        personFieldControl = dpAnniversary;

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.AsDateTime();
                            dpAnniversary.SelectedDate = value;
                        }
                        break;
                    }

                case PersonFieldType.MobilePhone:
                    {
                        var dv = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );
                        if ( dv != null )
                        {
                            var pnlFrmGroup = new Panel { CssClass = "form-group phonegroup clearfix" };
                            personFieldControl = pnlFrmGroup;

                            var lblMobile = new Label { CssClass = "control-label phonegroup-label" };
                            lblMobile.Text = dv.Value;
                            pnlFrmGroup.Controls.Add( lblMobile );

                            var displaySmsCheckbox = GetAttributeValue( "DisplaySMSCheckboxonMobilePhone" ).AsBoolean();

                            var pnlPhoneGroup = new Panel { CssClass = string.Format( "controls col-sm-12 pl-0 phonegroup-number {0}", displaySmsCheckbox ? "" : " pr-0" ) };
                            pnlFrmGroup.Controls.Add( pnlPhoneGroup );

                            var pnlRow = new Panel { CssClass = string.Format( "form-row {0}", displaySmsCheckbox ? "" : "mr-0" ) };
                            pnlPhoneGroup.Controls.Add( pnlRow );

                            var pnlCol1 = new Panel { CssClass = displaySmsCheckbox ? "col-sm-11" : "col-sm-12 pr-0" };
                            pnlRow.Controls.Add( pnlCol1 );

                            var ppMobile = new PhoneNumberBox
                            {
                                ID = "ppMobile",
                                Required = field.IsRequired,
                                ValidationGroup = BlockValidationGroup,
                                CountryCode = PhoneNumber.DefaultCountryCode()
                            };
                            lblMobile.AssociatedControlID = ppMobile.ClientID;
                            pnlCol1.Controls.Add( ppMobile );

                            var splitFieldValue = fieldValue.SplitDelimitedValues( "^" );
                            if ( setValue && fieldValue != null )
                            {
                                ppMobile.Number = PhoneNumber.FormattedNumber( PhoneNumber.DefaultCountryCode(), fieldValue.Contains( "^" ) ? splitFieldValue[0] : fieldValue );
                            }

                            var pnlCol2 = new Panel { CssClass = string.Format( "col-sm-1 form-align {0}", displaySmsCheckbox ? "" : "hidden" ) };
                            pnlRow.Controls.Add( pnlCol2 );

                            var cbSms = new RockCheckBox
                            {
                                ID = "cbSms",
                                Required = field.IsRequired,
                                ValidationGroup = BlockValidationGroup,
                                Text = "SMS",
                                DisplayInline = true,
                                ContainerCssClass = "checkbox-inline pt-0 my-2"
                            };
                            pnlCol2.Controls.Add( cbSms );

                            if ( setValue && fieldValue != null && fieldValue.Contains( "^" ) )
                            {
                                cbSms.Checked = splitFieldValue[1].AsBoolean();
                            }
                        }

                        break;
                    }
                case PersonFieldType.HomePhone:
                    {
                        var dv = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME );
                        if ( dv != null )
                        {
                            var ppHome = new PhoneNumberBox();
                            ppHome.ID = "ppHome";
                            ppHome.Label = dv.Value;
                            ppHome.Required = field.IsRequired;
                            ppHome.ValidationGroup = BlockValidationGroup;
                            ppHome.CountryCode = PhoneNumber.DefaultCountryCode();

                            personFieldControl = ppHome;

                            if ( setValue && fieldValue != null )
                            {
                                ppHome.Number = PhoneNumber.FormattedNumber( PhoneNumber.DefaultCountryCode(), fieldValue );
                            }
                        }

                        break;
                    }

                case PersonFieldType.WorkPhone:
                    {
                        var dv = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK );
                        if ( dv != null )
                        {
                            var ppWork = new PhoneNumberBox();
                            ppWork.ID = "ppWork";
                            ppWork.Label = dv.Value;
                            ppWork.Required = field.IsRequired;
                            ppWork.ValidationGroup = BlockValidationGroup;
                            ppWork.CountryCode = PhoneNumber.DefaultCountryCode();

                            personFieldControl = ppWork;

                            if ( setValue && fieldValue != null )
                            {
                                ppWork.Number = PhoneNumber.FormattedNumber( PhoneNumber.DefaultCountryCode(), fieldValue );
                            }
                        }

                        break;
                    }
                case PersonFieldType.ConnectionStatus:
                    {
                        var dvpConnectionStatus = new DefinedValuePicker();
                        dvpConnectionStatus.ID = "dvpConnectionStatus";
                        dvpConnectionStatus.Label = "Connection Status";
                        dvpConnectionStatus.Required = field.IsRequired;
                        dvpConnectionStatus.ValidationGroup = BlockValidationGroup;
                        dvpConnectionStatus.DefinedTypeId = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS ) ).Id;

                        personFieldControl = dvpConnectionStatus;

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().AsInteger();
                            dvpConnectionStatus.SetValue( value );
                        }

                        break;
                    }
            }

            var fieldVisibilityWrapper = new FieldVisibilityWrapper
            {
                ID = "_fieldVisibilityWrapper_field_" + field.Guid.ToString( "N" ),
                FormFieldId = ( ( int ) field.PersonFieldType ),
                FieldVisibilityRules = field.FieldVisibilityRules
            };

            fieldVisibilityWrapper.EditValueUpdated += ( object sender, FieldVisibilityWrapper.FieldEventArgs args ) =>
            {
                FieldVisibilityWrapper.ApplyFieldVisibilityRules( phContent );
            };

            phContent.Controls.Add( fieldVisibilityWrapper );

            if ( !string.IsNullOrWhiteSpace( field.PreText ) )
            {
                fieldVisibilityWrapper.Controls.Add( new LiteralControl( field.PreText ) );
            }

            fieldVisibilityWrapper.Controls.Add( personFieldControl );
            fieldVisibilityWrapper.EditControl = personFieldControl;

            if ( !string.IsNullOrWhiteSpace( field.PostText ) )
            {
                fieldVisibilityWrapper.Controls.Add( new LiteralControl( field.PostText ) );
            }

            if ( hasDependantVisibilityRule && FieldVisibilityRules.IsFieldSupported( field.RegistrationPersonFieldType ) )
            {
                var fieldType = FieldVisibilityRules.GetSupportedFieldTypeCache( field.RegistrationPersonFieldType ).Field;

                if ( fieldType.HasChangeHandler( personFieldControl ) )
                {
                    fieldType.AddChangeHandler( personFieldControl, () =>
                    {
                        fieldVisibilityWrapper.TriggerEditValueUpdated( personFieldControl, new FieldVisibilityWrapper.FieldEventArgs( null, personFieldControl ) );
                    } );
                }
            }
        }

        #endregion View Mode

        #region Edit Mode

        /// <summary>
        /// Shows the settings.
        /// </summary>
        protected override void ShowSettings()
        {
            //NOTE: This isn't shown in a modal :(

            cbDisplayProgressBar.Checked = GetAttributeValue( "DisplayProgressBar" ).AsBoolean();
            ddlSaveValues.SetValue( GetAttributeValue( "SaveValues" ) );

            Guid? wtGuid = GetAttributeValue( "Workflow" ).AsGuidOrNull();
            if ( wtGuid.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    wtpWorkflow.SetValue( new WorkflowTypeService( rockContext ).Get( wtGuid.Value ) );
                }
            }
            else
            {
                wtpWorkflow.SetValue( null );
            }
            ddlWorkflowEntity.SetValue( GetAttributeValue( "WorkflowEntity" ) );

            var ppFieldType = new PageReferenceFieldType();
            ppFieldType.SetEditValue( ppDonePage, null, GetAttributeValue( "DonePage" ) );

            string json = GetAttributeValue( "Forms" );
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                FormState = new List<AttributeForm>();
                FormState.Add( new AttributeForm { Expanded = true } );
            }
            else
            {
                FormState = JsonConvert.DeserializeObject<List<AttributeForm>>( json );
            }

            ceConfirmationText.Text = GetAttributeValue( "ConfirmationText" );

            BuildEditControls( true );

            pnlEditModal.Visible = true;
            pnlView.Visible = false;
            mdEdit.Show();

            _mode = "EDIT";

            upnlContent.Update();
        }

        /// <summary>
        /// Builds the controls.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        /// <param name="activeFormGuid">The active form unique identifier.</param>
        private void BuildEditControls( bool setValues = false, Guid? activeFormGuid = null )
        {
            ddlFieldSource.BindToEnum<FormFieldSource>();
            ddlPersonField.BindToEnum<PersonFieldType>( sortAlpha: true );

            phForms.Controls.Clear();

            if ( FormState != null )
            {
                foreach ( var form in FormState.OrderBy( f => f.Order ) )
                {
                    BuildFormControl( phForms, setValues, form, activeFormGuid );
                }
            }

            upnlContent.Update();
        }

        /// <summary>
        /// Builds the form control.
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        /// <param name="form">The form.</param>
        /// <param name="activeFormGuid">The active form unique identifier.</param>
        /// <param name="showInvalid">if set to <c>true</c> [show invalid].</param>
        private void BuildFormControl( Control parentControl, bool setValues, AttributeForm form,
            Guid? activeFormGuid = null, bool showInvalid = false )
        {
            var control = new PersonAttributeFormEditor();
            control.ID = form.Guid.ToString( "N" );
            parentControl.Controls.Add( control );

            control.ValidationGroup = mdEdit.ValidationGroup;

            control.DeleteFieldClick += tfeForm_DeleteFieldClick;
            control.ReorderFieldClick += tfeForm_ReorderFieldClick;
            control.FilterFieldClick += tfeForm_FilterFieldClick;
            control.EditFieldClick += tfeForm_EditFieldClick;
            control.RebindFieldClick += tfeForm_RebindFieldClick;
            control.DeleteFormClick += tfeForm_DeleteFormClick;
            control.AddFieldClick += tfeForm_AddFieldClick;

            control.SetForm( form );

            control.BindFieldsGrid( form.Fields );

            if ( setValues )
            {
                if ( !control.Expanded )
                {
                    control.Expanded = activeFormGuid.HasValue && activeFormGuid.Equals( form.Guid );
                }
            }
        }

        /// <summary>
        /// Parses the controls.
        /// </summary>
        private void ParseEditControls()
        {
            int order = 0;
            foreach ( var formEditor in phForms.Controls.OfType<PersonAttributeFormEditor>() )
            {
                var form = FormState.FirstOrDefault( f => f.Guid == formEditor.FormGuid );
                if ( form != null )
                {
                    form.Order = order++;
                    form.Name = formEditor.Name;
                    form.Header = formEditor.Header;
                    form.Footer = formEditor.Footer;
                    form.Expanded = formEditor.Expanded;
                }
            }
        }

        /// <summary>
        /// Shows the form field edit.
        /// </summary>
        /// <param name="formGuid">The form unique identifier.</param>
        /// <param name="formFieldGuid">The form field unique identifier.</param>
        private void ShowFormFieldEdit( Guid formGuid, Guid formFieldGuid )
        {
            BuildEditControls( true );

            var form = FormState.FirstOrDefault( f => f.Guid == formGuid );

            if ( form == null && formGuid == Guid.Empty )
            {
                form = FormState.FirstOrDefault( f => f.Guid == Guid.Empty );
            }

            if ( form != null )
            {
                var field = form.Fields.FirstOrDefault( a => a.Guid.Equals( formFieldGuid ) );
                if ( field == null )
                {
                    lFieldSource.Visible = false;
                    ddlFieldSource.Visible = true;
                    ddlPersonAttributes.Visible = true;
                    ddlPersonField.Visible = false;
                    field = new AttributeFormField();
                    field.Guid = formFieldGuid;
                    field.ShowCurrentValue = true;
                    field.IsRequired = false;
                    field.FieldSource = FormFieldSource.PersonAttribute;
                }
                else
                {
                    lFieldSource.Text = field.FieldSource.ConvertToString();
                    lFieldSource.Visible = true;
                    ddlFieldSource.SetValue( field.FieldSource.ConvertToInt() );
                    ddlFieldSource.Visible = false;
                }

                ceAttributePreText.Text = field.PreText;
                ceAttributePostText.Text = field.PostText;

                ddlPersonAttributes.Items.Clear();
                var person = new Person();
                person.LoadAttributes();
                foreach ( var attr in person.Attributes
                    .OrderBy( a => a.Value.Categories.FirstOrDefault()?.Name )
                    .ThenBy( a => a.Value.Name )
                    .Select( a => a.Value ) )
                {
                    if ( attr.IsAuthorized( Authorization.VIEW, _person ) )
                    {
                        var li = new ListItem( attr.Name, attr.Id.ToString() );
                        li.Attributes.Add( "optiongroup", attr.Categories.AsDelimited( ", " ) );
                        li.Attributes.Add( "title", attr.Description );
                        ddlPersonAttributes.Items.Add( li );
                    }
                }

                var attribute = new Rock.Model.Attribute();
                attribute.FieldTypeId = FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT ).Id;

                if ( field.FieldSource == FormFieldSource.PersonAttribute )
                {
                    ddlPersonAttributes.SetValue( field.AttributeId );
                    ddlPersonAttributes.Visible = true;
                    ddlPersonField.Visible = false;
                }
                else if ( field.FieldSource == FormFieldSource.PersonField )
                {
                    ddlPersonField.SetValue( field.PersonFieldType.ConvertToInt() );
                    ddlPersonField.Visible = true;
                    ddlPersonAttributes.Visible = false;
                }

                cbRequireInInitialEntry.Checked = field.IsRequired;
                cbUsePersonCurrentValue.Checked = field.ShowCurrentValue;

                hfFormGuid.Value = formGuid.ToString();
                hfAttributeGuid.Value = formFieldGuid.ToString();

                ShowDialog( dlgField );
            }

            BuildEditControls( true );
        }

        /// <summary>
        /// Sorts the forms.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <param name="newIndex">The new index.</param>
        private void SortForms( Guid guid, int newIndex )
        {
            ParseEditControls();

            Guid? activeFormGuid = null;

            var form = FormState.FirstOrDefault( a => a.Guid.Equals( guid ) );
            if ( form != null )
            {
                activeFormGuid = form.Guid;

                FormState.Remove( form );
                if ( newIndex >= FormState.Count() )
                {
                    FormState.Add( form );
                }
                else
                {
                    FormState.Insert( newIndex, form );
                }
            }

            int order = 0;
            foreach ( var item in FormState )
            {
                item.Order = order++;
            }

            BuildEditControls( true );
        }

        /// <summary>
        /// Sorts the fields.
        /// </summary>
        /// <param name="fieldList">The field list.</param>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        private void SortFields( List<AttributeFormField> fieldList, int oldIndex, int newIndex )
        {
            var movedItem = fieldList.Where( a => a.Order == oldIndex ).FirstOrDefault();
            if ( movedItem != null )
            {
                if ( newIndex < oldIndex )
                {
                    // Moved up
                    foreach ( var otherItem in fieldList.Where( a => a.Order < oldIndex && a.Order >= newIndex ) )
                    {
                        otherItem.Order = otherItem.Order + 1;
                    }
                }
                else
                {
                    // Moved Down
                    foreach ( var otherItem in fieldList.Where( a => a.Order > oldIndex && a.Order <= newIndex ) )
                    {
                        otherItem.Order = otherItem.Order - 1;
                    }
                }

                movedItem.Order = newIndex;
            }
        }

        /// <summary>
        /// Reorder fields.
        /// </summary>
        /// <param name="fieldList">The field list.</param>
        private void ReOrderFields( List<AttributeFormField> fieldList )
        {
            fieldList = fieldList.OrderBy( a => a.Order ).ToList();
            int order = 0;
            fieldList.ForEach( a => a.Order = order++ );
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlFieldSource control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlFieldSource_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetFieldDisplay();
        }

        /// <summary>
        /// Sets the field display.
        /// </summary>
        protected void SetFieldDisplay()
        {
            var fieldSource = ddlFieldSource.SelectedValueAsEnum<FormFieldSource>();
            ddlPersonField.Visible = fieldSource == FormFieldSource.PersonField;
            ddlPersonAttributes.Visible = fieldSource == FormFieldSource.PersonAttribute;
            cbUsePersonCurrentValue.Visible =
                fieldSource == FormFieldSource.PersonAttribute ||
                fieldSource == FormFieldSource.PersonField;
        }

        /// <summary>
        /// Saves the phone.
        /// </summary>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="person">The person.</param>
        /// <param name="phoneTypeGuid">The phone type unique identifier.</param>
        private void SavePhone( string cleanNumber, Person person, Guid phoneTypeGuid )
        {
            if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( cleanNumber ) ) )
            {
                var numberType = DefinedValueCache.Get( phoneTypeGuid );
                if ( numberType != null )
                {
                    var phone = person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValueId == numberType.Id );
                    string oldPhoneNumber = string.Empty;
                    if ( phone == null )
                    {
                        phone = new PhoneNumber();
                        person.PhoneNumbers.Add( phone );
                        phone.NumberTypeValueId = numberType.Id;
                    }
                    else
                    {
                        oldPhoneNumber = phone.NumberFormattedWithCountryCode;
                    }

                    if ( cleanNumber.Contains( "^" ) )
                    {
                        var splitNum = cleanNumber.SplitDelimitedValues( "^" );
                        cleanNumber = splitNum[0];

                        var isSms = splitNum[1].AsBoolean();
                        if ( isSms )
                        {
                            // only allow one sms enabled number on the person record
                            foreach ( var phoneNum in person.PhoneNumbers.Where( p => p.IsMessagingEnabled && p.Number != cleanNumber ) )
                            {
                                phoneNum.IsMessagingEnabled = false;
                            }
                        }
                        phone.IsMessagingEnabled = isSms;
                    }

                    phone.CountryCode = PhoneNumber.CleanNumber( PhoneNumber.DefaultCountryCode() );
                    phone.Number = cleanNumber;
                }
            }
        }

        #endregion Edit Mode

        #region Dialog Methods

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( ModalDialog dialog, bool setValues = false )
        {
            hfActiveDialog.Value = dialog.ID;
            ShowDialog( setValues );
        }

        /// <summary>
        /// Shows the active dialog.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( bool setValues = false )
        {
            var activeDialog = this.ControlsOfTypeRecursive<ModalDialog>().FirstOrDefault( a => a.ID == hfActiveDialog.Value );
            if ( activeDialog != null )
            {
                activeDialog.Show();
            }
        }

        /// <summary>
        /// Hides the active dialog.
        /// </summary>
        private void HideDialog()
        {
            var activeDialog = this.ControlsOfTypeRecursive<ModalDialog>().FirstOrDefault( a => a.ID == hfActiveDialog.Value );
            if ( activeDialog != null )
            {
                activeDialog.Hide();
            }

            hfActiveDialog.Value = string.Empty;
        }

        #endregion Dialog Methods

        /// <summary>
        /// Registers the client script.
        /// </summary>
        private void RegisterClientScript()
        {
            RockPage.AddScriptLink( ResolveUrl( "~/Scripts/jquery.creditCardTypeDetector.js" ) );

            string script = string.Format( @"

    if ( $('#{0}').val() == 'true' ) {{
        setTimeout('window.scrollTo(0,0)',0);
        $('#{0}').val('')
    }}

",
            hfTriggerScroll.ClientID       // {0}
            );

            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "PersonAttributeForms", script, true );
        }

        #endregion Methods
    }

    #region Helper Classes

    [ToolboxData( "<{0}:PersonAttributeFormEditor runat=server></{0}:PersonAttributeFormEditor>" )]
    public class PersonAttributeFormEditor : CompositeControl, IHasValidationGroup
    {
        private HiddenFieldWithClass _hfExpanded;
        private HiddenField _hfFormGuid;
        private Label _lblFormName;

        private RockTextBox _tbFormName;
        private CodeEditor _tbFormHeader;
        private CodeEditor _tbFormFooter;

        private LinkButton _lbDeleteForm;

        private Grid _gFields;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PersonAttributeFormEditor"/> is expanded.
        /// </summary>
        public bool Expanded
        {
            get
            {
                EnsureChildControls();
                return _hfExpanded.Value.AsBooleanOrNull() ?? false;
            }

            set
            {
                EnsureChildControls();
                _hfExpanded.Value = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>
        /// The validation group.
        /// </value>
        public string ValidationGroup
        {
            get
            {
                return ViewState["ValidationGroup"] as string;
            }

            set
            {
                ViewState["ValidationGroup"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the activity type unique identifier.
        /// </summary>
        /// <value>
        /// The activity type unique identifier.
        /// </value>
        public Guid FormGuid
        {
            get
            {
                EnsureChildControls();
                return _hfFormGuid.Value.AsGuid();
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get
            {
                EnsureChildControls();
                return _tbFormName.Text;
            }
        }

        /// <summary>
        /// Gets the header.
        /// </summary>
        /// <value>
        /// The header.
        /// </value>
        public string Header
        {
            get
            {
                EnsureChildControls();
                return _tbFormHeader.Text;
            }
        }

        /// <summary>
        /// Gets the footer.
        /// </summary>
        /// <value>
        /// The footer.
        /// </value>
        public string Footer
        {
            get
            {
                EnsureChildControls();
                return _tbFormFooter.Text;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            string script = @"
// activity animation
$('.template-form > header').click(function () {
    $(this).siblings('.panel-body').slideToggle();

    $expanded = $(this).children('input.filter-expanded');
    $expanded.val($expanded.val() == 'True' ? 'False' : 'True');

    $('i.template-form-state', this).toggleClass('fa-chevron-down');
    $('i.template-form-state', this).toggleClass('fa-chevron-up');
});

// fix so that the Remove button will fire its event, but not the parent event
$('.template-form a.js-activity-delete').click(function (event) {
    event.stopImmediatePropagation();
});

// fix so that the Reorder button will fire its event, but not the parent event
$('.template-form a.template-form-reorder').click(function (event) {
    event.stopImmediatePropagation();
});

$('.template-form > .panel-body').on('validation-error', function() {
    var $header = $(this).siblings('header');
    $(this).slideDown();

    $expanded = $header.children('input.filter-expanded');
    $expanded.val('True');

    $('i.template-form-state', $header).removeClass('fa-chevron-down');
    $('i.template-form-state', $header).addClass('fa-chevron-up');

    return false;
});

";

            ScriptManager.RegisterStartupScript( this, this.GetType(), "PersonAttributeFormEditorScript", script, true );
        }

        /// <summary>
        /// Sets the type of the workflow activity.
        /// </summary>
        /// <param name="value">The value.</param>
        public void SetForm( AttributeForm value )
        {
            EnsureChildControls();
            _hfFormGuid.Value = value.Guid.ToString();
            _tbFormName.Text = value.Name;
            _tbFormHeader.Text = value.Header;
            _tbFormFooter.Text = value.Footer;
            Expanded = value.Expanded;
        }

        /// <summary>
        /// The filterable fields count. If there is less than 2, don't show the FilterButton (since there would be no other fields to use as criteria)
        /// </summary>
        private int _filterableFieldsCount = 0;

        /// <summary>
        /// Binds the fields grid.
        /// </summary>
        /// <param name="formFields">The fields.</param>
        public void BindFieldsGrid( List<AttributeFormField> formFields )
        {
            _filterableFieldsCount = formFields.Where( a => a.FieldSource != FormFieldSource.PersonField ).Count();
            _gFields.DataSource = formFields
                .OrderBy( a => a.Order )
                .ToList();
            _gFields.DataBind();
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            Controls.Clear();

            _hfExpanded = new HiddenFieldWithClass();
            Controls.Add( _hfExpanded );
            _hfExpanded.ID = this.ID + "_hfExpanded";
            _hfExpanded.CssClass = "filter-expanded";
            _hfExpanded.Value = "False";

            _hfFormGuid = new HiddenField();
            Controls.Add( _hfFormGuid );
            _hfFormGuid.ID = this.ID + "_hfFormGuid";

            _lblFormName = new Label();
            Controls.Add( _lblFormName );
            _lblFormName.ClientIDMode = ClientIDMode.Static;
            _lblFormName.ID = this.ID + "_lblFormName";

            _lbDeleteForm = new LinkButton();
            Controls.Add( _lbDeleteForm );
            _lbDeleteForm.CausesValidation = false;
            _lbDeleteForm.ID = this.ID + "_lbDeleteForm";
            _lbDeleteForm.CssClass = "btn btn-xs btn-square btn-danger js-activity-delete";
            _lbDeleteForm.Click += lbDeleteForm_Click;
            _lbDeleteForm.Controls.Add( new LiteralControl { Text = "<i class='fa fa-times'></i>" } );

            _tbFormName = new RockTextBox();
            Controls.Add( _tbFormName );
            _tbFormName.ID = this.ID + "_tbFormName";
            _tbFormName.Label = "Form Title";
            _tbFormName.Help = "Title of the form <span class='tip tip-lava'></span>.";
            _tbFormName.Attributes["onblur"] = string.Format( "javascript: $('#{0}').text($(this).val());", _lblFormName.ID );

            _tbFormHeader = new CodeEditor();
            Controls.Add( _tbFormHeader );
            _tbFormHeader.ID = this.ID + "_tbFormHeader";
            _tbFormHeader.Label = "Form Header";
            _tbFormHeader.Help = "HTML to display above the fields <span class='tip tip-lava'></span>.";
            _tbFormHeader.EditorMode = CodeEditorMode.Html;
            _tbFormHeader.EditorTheme = CodeEditorTheme.Rock;
            _tbFormHeader.EditorHeight = "100";

            _tbFormFooter = new CodeEditor();
            Controls.Add( _tbFormFooter );
            _tbFormFooter.ID = this.ID + "_tbFormFooter";
            _tbFormFooter.Label = "Form Footer";
            _tbFormFooter.Help = "HTML to display below the fields <span class='tip tip-lava'></span>.";
            _tbFormFooter.EditorMode = CodeEditorMode.Html;
            _tbFormFooter.EditorTheme = CodeEditorTheme.Rock;
            _tbFormFooter.EditorHeight = "100";

            _gFields = new Grid();
            Controls.Add( _gFields );
            _gFields.ID = this.ID + "_gFields";
            _gFields.AllowPaging = false;
            _gFields.DisplayType = GridDisplayType.Light;
            _gFields.RowItemText = "Field";
            _gFields.AddCssClass( "field-grid" );
            _gFields.DataKeyNames = new string[] { "Guid" };
            _gFields.Actions.ShowAdd = true;
            _gFields.Actions.AddClick += gFields_Add;
            _gFields.GridRebind += gFields_Rebind;
            _gFields.GridReorder += gFields_Reorder;

            var reorderField = new ReorderField();
            _gFields.Columns.Add( reorderField );

            var nameField = new RockLiteralField();
            nameField.HeaderText = "Field";
            nameField.DataBound += NameField_DataBound;
            _gFields.Columns.Add( nameField );

            var fieldSource = new RockLiteralField();
            fieldSource.HeaderText = "Source";
            fieldSource.DataBound += SourceField_DataBound;
            _gFields.Columns.Add( fieldSource );

            var typeField = new RockLiteralField();
            typeField.HeaderText = "Type";
            typeField.DataBound += TypeField_DataBound;
            _gFields.Columns.Add( typeField );

            var showCurrentValueField = new BoolField();
            showCurrentValueField.DataField = "ShowCurrentValue";
            showCurrentValueField.HeaderText = "Use Current Value";
            _gFields.Columns.Add( showCurrentValueField );

            var isRequiredField = new BoolField();
            isRequiredField.DataField = "IsRequired";
            isRequiredField.HeaderText = "Required";
            _gFields.Columns.Add( isRequiredField );

            var btnFieldFilterField = new LinkButtonField();
            btnFieldFilterField.CssClass = "btn btn-default btn-sm attribute-criteria";
            btnFieldFilterField.Text = "<i class='fa fa-filter'></i>";
            btnFieldFilterField.Click += gFields_btnFieldFilterField_Click;
            btnFieldFilterField.DataBound += btnFieldFilterField_DataBound;
            _gFields.Columns.Add( btnFieldFilterField );

            var editField = new EditField();
            editField.Click += gFields_Edit;
            _gFields.Columns.Add( editField );

            var delField = new DeleteField();
            delField.Click += gFields_Delete;
            _gFields.Columns.Add( delField );
        }

        /// <summary>
        /// Writes the <see cref="T:System.Web.UI.WebControls.CompositeControl" /> content to the specified <see cref="T:System.Web.UI.HtmlTextWriter" /> object, for display on the client.
        /// </summary>
        /// <param name="writer">An <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel panel-widget template-form" );

            writer.AddAttribute( "data-key", _hfFormGuid.Value );
            writer.AddAttribute( HtmlTextWriterAttribute.Id, this.ID + "_section" );
            writer.RenderBeginTag( "section" );

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel-heading clearfix clickable" );
            writer.RenderBeginTag( "header" );

            // Hidden Field to track expansion
            _hfExpanded.RenderControl( writer );

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "filter-toggle pull-left" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            writer.AddAttribute( "class", "panel-title" );
            writer.RenderBeginTag( HtmlTextWriterTag.H3 );
            _lblFormName.Text = _tbFormName.Text;
            _lblFormName.RenderControl( writer );

            // H3 tag
            writer.RenderEndTag();

            // Name div
            writer.RenderEndTag();

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "pull-right" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            writer.WriteLine( "<a class='btn btn-xs btn-link form-reorder'><i class='fa fa-bars'></i></a>" );
            writer.WriteLine( string.Format( "<a class='btn btn-xs btn-link'><i class='form-state fa {0}'></i></a>", Expanded ? "fa fa-chevron-up" : "fa fa-chevron-down" ) );

            _lbDeleteForm.RenderControl( writer );

            // Add/ChevronUpDown/Delete div
            writer.RenderEndTag();

            // header div
            writer.RenderEndTag();

            if ( !Expanded )
            {
                // hide details if the activity and actions are valid
                writer.AddStyleAttribute( "display", "none" );
            }

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel-body" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            // activity edit fields
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "row" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-md-6" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            _hfFormGuid.RenderControl( writer );
            _tbFormName.ValidationGroup = ValidationGroup;
            _tbFormName.RenderControl( writer );
            writer.RenderEndTag();

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-md-6" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            writer.RenderEndTag();

            writer.RenderEndTag();

            _tbFormHeader.RenderControl( writer );

            _gFields.RenderControl( writer );

            _tbFormFooter.RenderControl( writer );

            // widget-content div
            writer.RenderEndTag();

            // section tag
            writer.RenderEndTag();
        }

        /// <summary>
        /// Handles the Click event of the lbDeleteForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbDeleteForm_Click( object sender, EventArgs e )
        {
            if ( DeleteFormClick != null )
            {
                DeleteFormClick( this, e );
            }
        }

        /// <summary>
        /// Handles the Rebind event of the gFields control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gFields_Rebind( object sender, EventArgs e )
        {
            if ( RebindFieldClick != null )
            {
                var eventArg = new AttributeFormFieldEventArg( FormGuid );
                RebindFieldClick( this, eventArg );
            }
        }

        /// <summary>
        /// Handles the Add event of the gFields control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gFields_Add( object sender, EventArgs e )
        {
            if ( AddFieldClick != null )
            {
                var eventArg = new AttributeFormFieldEventArg( FormGuid, Guid.Empty );
                AddFieldClick( this, eventArg );
            }
        }

        /// <summary>
        /// Handles the Click event of the gFields_btnFieldFilterField control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gFields_btnFieldFilterField_Click( object sender, RowEventArgs e )
        {
            if ( FilterFieldClick != null )
            {
                var eventArg = new AttributeFormFieldEventArg( FormGuid, ( Guid ) e.RowKeyValue );
                FilterFieldClick( this, eventArg );
            }
        }

        /// <summary>
        /// Handles the Edit event of the gFields control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gFields_Edit( object sender, RowEventArgs e )
        {
            if ( EditFieldClick != null )
            {
                var eventArg = new AttributeFormFieldEventArg( FormGuid, ( Guid ) e.RowKeyValue );
                EditFieldClick( this, eventArg );
            }
        }

        /// <summary>
        /// Handles the Reorder event of the gFields control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridReorderEventArgs"/> instance containing the event data.</param>
        protected void gFields_Reorder( object sender, GridReorderEventArgs e )
        {
            if ( ReorderFieldClick != null )
            {
                var eventArg = new AttributeFormFieldEventArg( FormGuid, e.OldIndex, e.NewIndex );
                ReorderFieldClick( this, eventArg );
            }
        }

        /// <summary>
        /// Handles the Delete event of the gFields control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gFields_Delete( object sender, RowEventArgs e )
        {
            if ( DeleteFieldClick != null )
            {
                var eventArg = new AttributeFormFieldEventArg( FormGuid, ( Guid ) e.RowKeyValue );
                DeleteFieldClick( this, eventArg );
            }
        }


        /// <summary>
        /// Handles the DataBound event of the btnFieldFilterField control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        private void btnFieldFilterField_DataBound( object sender, RowEventArgs e )
        {
            LinkButton linkButton = sender as LinkButton;
            AttributeFormField field = e.Row.DataItem as AttributeFormField;

            if ( field != null && linkButton != null )
            {
                if ( ( field.FieldSource == FormFieldSource.PersonField ) || _filterableFieldsCount < 2 )
                {
                    linkButton.Visible = false;
                }
                else
                {
                    if ( field.FieldVisibilityRules.RuleList.Any() )
                    {
                        linkButton.RemoveCssClass( "btn-default" );
                        linkButton.AddCssClass( "btn-warning" );
                        linkButton.AddCssClass( "criteria-exists" );
                    }
                    else
                    {
                        linkButton.AddCssClass( "btn-default" );
                        linkButton.RemoveCssClass( "btn-warning" );
                        linkButton.RemoveCssClass( "criteria-exists" );
                    }

                    linkButton.Visible = true;
                }
            }
        }
        /// <summary>
        /// Handles the DataBound event of the TypeField control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        private void TypeField_DataBound( object sender, RowEventArgs e )
        {
            Literal literal = sender as Literal;
            AttributeFormField field = e.Row.DataItem as AttributeFormField;
            if ( field != null && literal != null )
            {
                if ( field.FieldSource != FormFieldSource.PersonField && field.AttributeId.HasValue )
                {
                    var attribute = field.Attribute;
                    if ( attribute != null )
                    {
                        literal.Text = attribute.FieldType.Name;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the DataBound event of the SourceField control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        private void SourceField_DataBound( object sender, RowEventArgs e )
        {
            Literal literal = sender as Literal;
            AttributeFormField field = e.Row.DataItem as AttributeFormField;
            if ( field != null && literal != null )
            {
                literal.Text = field.FieldSource.ConvertToString();
            }
        }

        /// <summary>
        /// Handles the DataBound event of the NameField control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        private void NameField_DataBound( object sender, RowEventArgs e )
        {
            Literal literal = sender as Literal;
            AttributeFormField field = e.Row.DataItem as AttributeFormField;
            if ( field != null && literal != null )
            {
                if ( field.FieldSource != FormFieldSource.PersonField && field.Attribute != null )
                {
                    // Update Guid for Conditional Field support to match actual attribute Guid, for backwards compatibility without having to re-add/edit every field on the form.
                    if ( field.Guid != field.Attribute.Guid )
                    {
                        field.Guid = field.Attribute.Guid;
                    }

                    literal.Text = field.Attribute.Name;
                }
                else
                {
                    literal.Text = field.PersonFieldType.ConvertToString();
                }
            }
        }

        /// <summary>
        /// Occurs when [delete activity type click].
        /// </summary>
        public event EventHandler DeleteFormClick;

        /// <summary>
        /// Occurs when [add field click].
        /// </summary>
        public event EventHandler<AttributeFormFieldEventArg> RebindFieldClick;

        /// <summary>
        /// Occurs when [add field click].
        /// </summary>
        public event EventHandler<AttributeFormFieldEventArg> AddFieldClick;

        /// <summary>
        /// Occurs when [filter field click].
        /// </summary>
        public event EventHandler<AttributeFormFieldEventArg> FilterFieldClick;

        /// <summary>
        /// Occurs when [edit field click].
        /// </summary>
        public event EventHandler<AttributeFormFieldEventArg> EditFieldClick;

        /// <summary>
        /// Occurs when [edit field click].
        /// </summary>
        public event EventHandler<AttributeFormFieldEventArg> ReorderFieldClick;

        /// <summary>
        /// Occurs when [delete field click].
        /// </summary>
        public event EventHandler<AttributeFormFieldEventArg> DeleteFieldClick;
    }

    /// <summary>
    ///
    /// </summary>
    public class AttributeFormFieldEventArg : EventArgs
    {
        /// <summary>
        /// Gets or sets the activity type unique identifier.
        /// </summary>
        /// <value>
        /// The activity type unique identifier.
        /// </value>
        public Guid FormGuid { get; set; }

        /// <summary>
        /// Gets or sets the field unique identifier.
        /// </summary>
        /// <value>
        /// The field unique identifier.
        /// </value>
        public Guid FormFieldGuid { get; set; }

        /// <summary>
        /// Gets or sets the old index.
        /// </summary>
        /// <value>
        /// The old index.
        /// </value>
        public int OldIndex { get; set; }

        /// <summary>
        /// Gets or sets the new index.
        /// </summary>
        /// <value>
        /// The new index.
        /// </value>
        public int NewIndex { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeFormFieldEventArg"/> class.
        /// </summary>
        /// <param name="activityTypeGuid">The activity type unique identifier.</param>
        public AttributeFormFieldEventArg( Guid activityTypeGuid )
        {
            FormGuid = activityTypeGuid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeFormFieldEventArg"/> class.
        /// </summary>
        /// <param name="formGuid">The form unique identifier.</param>
        /// <param name="formFieldGuid">The form field unique identifier.</param>
        public AttributeFormFieldEventArg( Guid formGuid, Guid formFieldGuid )
        {
            FormGuid = formGuid;
            FormFieldGuid = formFieldGuid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeFormFieldEventArg" /> class.
        /// </summary>
        /// <param name="formGuid">The form unique identifier.</param>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        public AttributeFormFieldEventArg( Guid formGuid, int oldIndex, int newIndex )
        {
            FormGuid = formGuid;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }

    [Serializable]
    public class AttributeForm : IOrdered
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Header { get; set; }
        public string Footer { get; set; }
        public int Order { get; set; }
        public bool Expanded { get; set; }
        public virtual List<AttributeFormField> Fields { get; set; }

        public AttributeForm()
        {
            Fields = new List<AttributeFormField>();

        }

        public override string ToString()
        {
            return Name;
        }
    }

    [Serializable]
    public partial class AttributeFormField : IOrdered
    {
        public Guid Guid { get; set; }

        public int? AttributeId { get; set; }

        public bool ShowCurrentValue { get; set; }

        public bool IsRequired { get; set; }

        public virtual Rock.Field.FieldVisibilityRules FieldVisibilityRules { get; set; } = new Rock.Field.FieldVisibilityRules();

        public int Order { get; set; }

        public string PreText { get; set; }

        public string PostText { get; set; }

        public FormFieldSource FieldSource { get; set; }

        public RegistrationFieldSource RegistrationFieldSource { get; set; }

        public PersonFieldType PersonFieldType { get; set; }

        public RegistrationPersonFieldType RegistrationPersonFieldType { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public AttributeCache Attribute
        {
            get
            {
                if ( AttributeId.HasValue && FieldSource == FormFieldSource.PersonAttribute )
                {
                    return AttributeCache.Get( AttributeId.Value );
                }

                return null;
            }
        }

        public Rock.Model.Attribute AttributeObj
        {
            get
            {
                if ( AttributeId.HasValue && FieldSource == FormFieldSource.PersonAttribute )
                {
                    var rockContext = new RockContext();
                    return new AttributeService( rockContext ).Get( AttributeId.Value );
                }

                return null;
            }
        }

        public override string ToString()
        {
            if ( FieldSource == FormFieldSource.PersonField )
            {
                return PersonFieldType.ConvertToString();
            }

            var attributeCache = this.Attribute;
            if ( attributeCache != null )
            {
                return attributeCache.Name;
            }

            return base.ToString();
        }
    }

    public enum FormFieldSource
    {
        PersonAttribute,
        PersonField
    }

    /// <summary>
    ///
    /// </summary>
    public enum PersonFieldType
    {
        /// <summary>
        /// The first name
        /// </summary>
        FirstName = 0,

        /// <summary>
        /// The last name
        /// </summary>
        LastName = 1,

        /// <summary>
        /// The person's campus
        /// </summary>
        Campus = 2,

        /// <summary>
        /// The Address
        /// </summary>
        Address = 3,

        /// <summary>
        /// The email
        /// </summary>
        Email = 4,

        /// <summary>
        /// The birthdate
        /// </summary>
        Birthdate = 5,

        /// <summary>
        /// The gender
        /// </summary>
        Gender = 6,

        /// <summary>
        /// The marital status
        /// </summary>
        MaritalStatus = 7,

        /// <summary>
        /// The mobile phone
        /// </summary>
        MobilePhone = 8,

        /// <summary>
        /// The home phone
        /// </summary>
        HomePhone = 9,

        /// <summary>
        /// The work phone
        /// </summary>
        WorkPhone = 10,

        /// <summary>
        /// The grade
        /// </summary>
        Grade = 11,

        /// <summary>
        /// The connection status
        /// </summary>
        ConnectionStatus = 12,

        /// <summary>
        /// The anniversary date
        /// </summary>
        AnniversaryDate = 13,

        /// <summary>
        /// The middle name
        /// </summary>
        MiddleName = 14
    }

    public class FieldVisibilityWrapper : DynamicPlaceholder
    {
        /// <summary>
        /// Gets or sets the field identifier
        /// </summary>
        /// <value>
        /// The field identifier.
        /// </value>
        public int FormFieldId
        {
            get
            {
                return ViewState["FormFieldId"] as int? ?? 0;
            }

            set
            {
                ViewState["FormFieldId"] = value;
            }
        }

        /// <summary>
        /// Get the form field
        /// </summary>
        /// <returns></returns>
        public RegistrationTemplateFormFieldCache GetRegistrationTemplateFormField()
        {
            return null;
        }

        /// <summary>
        /// Gets the form field.
        /// </summary>
        /// <returns></returns>
        public AttributeCache GetFormField()
        {
            return AttributeCache.Get( FormFieldId );
        }

        /// <summary>
        /// Get the attribute id
        /// </summary>
        /// <returns></returns>
        public AttributeCache GetAttributeCache()
        {
            var field = GetRegistrationTemplateFormField();
            var id = field?.AttributeId;

            if ( id.HasValue )
            {
                return AttributeCache.Get( id.Value );
            }

            return null;
        }

        /// <summary>
        /// Sets the visibility based on the value of other attributes
        /// </summary>
        /// <param name="attributeValues">The attribute values.</param>
        /// <param name="personFieldValues">The person field values.</param>
        public void UpdateVisibility( Dictionary<int, AttributeValueCache> attributeValues, Dictionary<RegistrationPersonFieldType, string> personFieldValues )
        {
            var visible = FieldVisibilityRules.Evaluate( attributeValues, personFieldValues );
            if ( visible == false && this.Visible )
            {
                // if hiding this field, set the value to null since we don't want to save values that aren't shown
                this.EditValue = null;
            }

            this.Visible = visible;
        }

        /// <summary>
        /// Gets or sets the edit control for the Attribute
        /// </summary>
        /// <value>
        /// The edit control.
        /// </value>
        public Control EditControl { get; set; }

        /// <summary>
        /// Gets the edit value from the <see cref="EditControl"/> associated with <see cref="FormFieldId"/>
        /// </summary>
        /// <value>
        /// The edit value.
        /// </value>
        public string EditValue
        {
            get
            {
                var field = GetRegistrationTemplateFormField();
                var attribute = GetAttributeCache() ?? GetFormField();

                if ( attribute != null )
                {
                    return attribute.FieldType.Field.GetEditValue( this.EditControl, attribute.QualifierValues );
                }
                else if ( field != null && FieldVisibilityRules.IsFieldSupported( field.PersonFieldType ) )
                {
                    var fieldType = FieldVisibilityRules.GetSupportedFieldTypeCache( field.PersonFieldType );
                    return fieldType.Field.GetEditValue( this.EditControl, null );
                }
                else
                {
                    throw new NotImplementedException( "The field type and source are not supported" );
                }
            }

            private set
            {
                var field = GetRegistrationTemplateFormField();
                var attribute = GetAttributeCache() ?? GetFormField();

                if ( attribute != null )
                {
                    attribute.FieldType.Field.SetEditValue( this.EditControl, attribute.QualifierValues, value );
                }
                else if ( field != null && FieldVisibilityRules.IsFieldSupported( field.PersonFieldType ) )
                {
                    var fieldType = FieldVisibilityRules.GetSupportedFieldTypeCache( field.PersonFieldType );
                    fieldType.Field.SetEditValue( this.EditControl, null, value );
                }
                else
                {
                    throw new NotImplementedException( "The field type and source are not supported" );
                }
            }
        }

        /// <summary>
        /// Gets or sets the field visibility rules.
        /// </summary>
        /// <value>
        /// The field visibility rules.
        /// </value>
        public FieldVisibilityRules FieldVisibilityRules { get; set; }

        #region Event Handlers

        /// <summary>
        /// Gets called when an attributes edit control fires a EditValueUpdated
        /// </summary>
        public void TriggerEditValueUpdated( Control editControl, FieldEventArgs args )
        {
            EditValueUpdated?.Invoke( editControl, args );
        }

        /// <summary>
        /// Occurs when [edit value updated].
        /// </summary>
        public event EventHandler<FieldEventArgs> EditValueUpdated;

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="System.EventArgs" />
        public class FieldEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FieldEventArgs" /> class.
            /// </summary>
            /// <param name="attribute">The attribute.</param>
            /// <param name="editControl">The edit control.</param>
            public FieldEventArgs( AttributeCache attribute, Control editControl )
            {
                this.AttributeId = attribute?.Id;
                this.EditControl = editControl;
            }

            /// <summary>
            /// Gets or sets the attribute identifier.
            /// </summary>
            /// <value>
            /// The attribute identifier.
            /// </value>
            public int? AttributeId { get; set; }

            /// <summary>
            /// Gets the edit control.
            /// </summary>
            /// <value>
            /// The edit control.
            /// </value>
            public Control EditControl { get; private set; }
        }

        /// <summary>
        /// Applies the field visibility rules for all FieldVisibilityWrappers contained in the parentControl
        /// </summary>
        public static void ApplyFieldVisibilityRules( Control parentControl )
        {
            var fieldVisibilityWrappers = parentControl.ControlsOfTypeRecursive<FieldVisibilityWrapper>().ToDictionary( k => k.FormFieldId, v => v );
            var attributeValues = new Dictionary<int, AttributeValueCache>();
            var personFieldValues = new Dictionary<RegistrationPersonFieldType, string>();

            foreach ( var fieldVisibilityWrapper in fieldVisibilityWrappers.Values )
            {
                var field = fieldVisibilityWrapper.GetRegistrationTemplateFormField();
                var fieldAttribute = fieldVisibilityWrapper.GetFormField();

                var fieldAttributeId = field?.AttributeId ?? fieldAttribute?.Id;
                if ( fieldAttributeId.HasValue )
                {
                    var attributeId = fieldAttributeId.Value;
                    attributeValues.Add( attributeId, new AttributeValueCache { AttributeId = attributeId, Value = fieldVisibilityWrapper.EditValue } );
                }
                else if ( field != null && FieldVisibilityRules.IsFieldSupported( field.PersonFieldType ) )
                {
                    personFieldValues[field.PersonFieldType] = fieldVisibilityWrapper.EditValue;
                }
            }

            // This needs to be done AFTER all of the attributeValuse for each fieldVisibilityWrapper are collected in order to work correctly.
            foreach ( var fieldVisibilityWrapper in fieldVisibilityWrappers.Values )
            {
                fieldVisibilityWrapper.UpdateVisibility( attributeValues, personFieldValues );
            }
        }

        #endregion
    }

    #endregion Helper Classes
}
