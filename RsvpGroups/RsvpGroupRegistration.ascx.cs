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
// * Modified the GroupRegistration block for use with RSVP Groups
// * Added support for Group Member Attributes
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.RsvpGroups
{
    #region Block Attributes

    [DisplayName( "RSVP Group Registration" )]
    [Category( "KFS > RSVP Groups" )]
    [Description( "Allows a person to register for an RSVP Group." )]

    #endregion

    #region Block Settings

    [GroupField( "Group", Description = "Optional group to add person to. If omitted, the group's Guid should be passed via the Query string (GroupGuid=).",
        IsRequired = false,
        Order = 0,
        Key = AttributeKey.Group )]

    [BooleanField( "Enable Passing Group Id",
        Description = "If enabled, allows the ability to pass in a group's Id (GroupId=) instead of the Guid.",
        DefaultBooleanValue = true,
        Order = 1,
        Key = AttributeKey.EnablePassingGroupId )]

    [CustomRadioListField( "Mode",
        Description = "The mode to use when displaying registration details.",
        ListSource = "Simple^Simple,Full^Full",
        IsRequired = true,
        DefaultValue = "Simple",
        Order = 2,
        Key = AttributeKey.Mode )]

    [CustomRadioListField( "Group Member Status",
        Description = "The group member status to use when adding person to group (default: 'Pending'.)",
        ListSource = "2^Pending,1^Active,0^Inactive",
        IsRequired = true,
        DefaultValue = "2",
        Order = 3,
        Key = AttributeKey.GroupMemberStatus )]

    [DefinedValueField( "Connection Status",
        Description = "The connection status to use for new individuals (default: 'Web Prospect'.)",
        DefinedTypeGuid = "2E6540EA-63F0-40FE-BE50-F2A84735E600",
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = "368DD475-242C-49C4-A42C-7278BE690CC2",
        Order = 4,
        Key = AttributeKey.ConnectionStatus )]

    [DefinedValueField( "Record Status",
        Description = "The record status to use for new individuals (default: 'Pending'.)",
        DefinedTypeGuid = "8522BADD-2871-45A5-81DD-C76DA07E2E7E",
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = "283999EC-7346-42E3-B807-BCE9B2BABB49",
        Order = 5,
        Key = AttributeKey.RecordStatus )]

    [WorkflowTypeField( "Workflow",
        Description = "An optional workflow to start when registration is created. The GroupMember will set as the workflow 'Entity' when processing is started.",
        IsRequired = false,
        AllowMultiple = false,
        Order = 6,
        Key = AttributeKey.Workflow )]

    [CodeEditorField( "Lava Template",
        Description = "The lava template to use to format the group details.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 400,
        IsRequired = false,
        Order = 7,
        Key = AttributeKey.LavaTemplate )]

    [LinkedPage( "Result Page",
        Description = "An optional page to redirect user to after they have been registered for the group.",
        IsRequired = false,
        Order = 8,
        Key = AttributeKey.ResultPage )]

    [CodeEditorField( "Result Lava Template",
        Description = "The lava template to use to format result message after user has been registered. Will only display if user is not redirected to a Result Page ( previous setting ).",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 400,
        IsRequired = false,
        Order = 9,
        Key = AttributeKey.ResultLavaTemplate )]

    [CustomRadioListField( "Auto Fill Form",
        Description = "If set to FALSE then the form will not load the context of the logged in user (default: 'True'.)",
        ListSource = "true^True,false^False",
        IsRequired = true,
        DefaultValue = "true",
        Order = 10,
        Key = AttributeKey.AutoFillForm )]

    [TextField( "Register Button Alt Text",
        Description = "Alternate text to use for the Register button (default is 'Register').",
        IsRequired = false,
        Order = 11,
        Key = AttributeKey.RegisterButtonAltText )]

    [BooleanField( "Require Email",
        Description = "Should email be required for registration?",
        DefaultBooleanValue = true,
        Order = 12,
        Key = AttributeKey.RequireEmail )]

    [BooleanField( "Require Mobile Phone",
        Description = "Should mobile phone numbers be required for registration?",
        DefaultBooleanValue = false,
        Order = 13,
        Key = AttributeKey.RequiredMobile )]

    [BooleanField( "Show Attributes",
        Description = "Should group member attributes show on the registration? (Only attributes marked as 'Public' will be shown.)",
        DefaultBooleanValue = false,
        Order = 14,
        Key = AttributeKey.ShowAttributes )]

    [IntegerField( "Attribute Columns",
        Description = "How many columns should the attribute editor use.",
        DefaultIntegerValue = 2,
        IsRequired = true,
        Order = 15,
        Key = AttributeKey.AttributeColumns )]

    #endregion

    public partial class RsvpGroupRegistration : RockBlock
    {
        public static class AttributeKey
        {
            public const string Group = "Group";
            public const string EnablePassingGroupId = "EnablePassingGroupId";
            public const string Mode = "Mode";
            public const string GroupMemberStatus = "GroupMemberStatus";
            public const string ConnectionStatus = "ConnectionStatus";
            public const string RecordStatus = "RecordStatus";
            public const string Workflow = "Workflow";
            public const string LavaTemplate = "LavaTemplate";
            public const string ResultPage = "ResultPage";
            public const string ResultLavaTemplate = "ResultLavaTemplate";
            public const string AutoFillForm = "AutoFillForm";
            public const string RegisterButtonAltText = "RegisterButtonAltText";
            public const string RequireEmail = "IsRequireEmail";
            public const string RequiredMobile = "IsRequiredMobile";
            public const string ShowAttributes = "ShowAttributes";
            public const string AttributeColumns = "AttributeColumns";
        }
        #region Fields

        private RockContext _rockContext = null;
        private string _mode = "Simple";
        private Group _group = null;
        private GroupTypeRole _defaultGroupRole = null;
        private DefinedValueCache _dvcConnectionStatus = null;
        private DefinedValueCache _dvcRecordStatus = null;
        private DefinedValueCache _married = null;
        private DefinedValueCache _homeAddressType = null;
        private GroupTypeCache _familyType = null;
        private GroupTypeRoleCache _adultRole = null;
        private bool _autoFill = true;
        private bool _isValidSettings = true;
        private bool _isEditing = false;
        private int _currentRSVP = 1;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is simple.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is simple; otherwise, <c>false</c>.
        /// </value>
        protected bool IsSimple
        {
            get
            {
                return _mode == "Simple";
            }
        }

        #endregion

        #region Control Methods

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

            if ( !CheckSettings() )
            {
                _isValidSettings = false;
                nbNotice.Visible = true;
                pnlView.Visible = false;
            }
            else
            {
                nbNotice.Visible = false;
                pnlView.Visible = true;

                if ( !Page.IsPostBack )
                {
                    ShowDetails();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails();
        }

        /// <summary>
        /// Handles the NumberUpdated event of the numHowMany control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void numHowMany_NumberUpdated( object sender, EventArgs e )
        {
            ShowCapacityNotice();
        }

        /// <summary>
        /// Handles the Click event of the btnRegister control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRegister_Click( object sender, EventArgs e )
        {
            // Check _isValidSettings in case the form was showing and they clicked the visible register button.
            if ( Page.IsValid && _isValidSettings )
            {
                var rockContext = new RockContext();
                var personService = new PersonService( rockContext );

                Person person = null;
                Group family = null;
                GroupLocation homeLocation = null;
                bool isMatch = false;

                // Only use current person if the name entered matches the current person's name and autofill mode is true
                if ( _autoFill )
                {
                    if ( CurrentPerson != null &&
                        tbFirstName.Text.Trim().Equals( CurrentPerson.FirstName.Trim(), StringComparison.OrdinalIgnoreCase ) &&
                        tbLastName.Text.Trim().Equals( CurrentPerson.LastName.Trim(), StringComparison.OrdinalIgnoreCase ) )
                    {
                        person = personService.Get( CurrentPerson.Id );
                        isMatch = true;
                    }
                }

                // Try to find person by name/email
                if ( person == null )
                {
                    var personQuery = new PersonService.PersonMatchQuery( tbFirstName.Text.Trim(), tbLastName.Text.Trim(), tbEmail.Text.Trim(), pnCell.Text.Trim() );
                    person = personService.FindPerson( personQuery, true );
                    if ( person != null )
                    {
                        isMatch = true;
                    }
                }

                // Check to see if this is a new person
                if ( person == null )
                {
                    // If so, create the person and family record for the new person
                    person = new Person();
                    person.FirstName = tbFirstName.Text.Trim();
                    person.LastName = tbLastName.Text.Trim();
                    person.Email = tbEmail.Text.Trim();
                    person.IsEmailActive = true;
                    person.EmailPreference = EmailPreference.EmailAllowed;
                    person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    person.ConnectionStatusValueId = _dvcConnectionStatus.Id;
                    person.RecordStatusValueId = _dvcRecordStatus.Id;
                    person.Gender = Gender.Unknown;

                    family = PersonService.SaveNewPerson( person, rockContext, _group.CampusId, false );
                }
                else
                {
                    // updating current existing person
                    person.Email = tbEmail.Text;

                    // Get the current person's families
                    var families = person.GetFamilies( rockContext );

                    // If address can being entered, look for first family with a home location
                    if ( !IsSimple )
                    {
                        foreach ( var aFamily in families )
                        {
                            homeLocation = aFamily.GroupLocations
                                .Where( l =>
                                    l.GroupLocationTypeValueId == _homeAddressType.Id &&
                                    l.IsMappedLocation )
                                .FirstOrDefault();
                            if ( homeLocation != null )
                            {
                                family = aFamily;
                                break;
                            }
                        }
                    }

                    // If a family wasn't found with a home location, use the person's first family
                    if ( family == null )
                    {
                        family = families.FirstOrDefault();
                    }
                }

                // If using a 'Full' view, save the phone numbers and address
                if ( !IsSimple )
                {
                    if ( !isMatch || !string.IsNullOrWhiteSpace( pnHome.Number ) )
                    {
                        SetPhoneNumber( rockContext, person, pnHome, null, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
                    }
                    if ( !isMatch || !string.IsNullOrWhiteSpace( pnCell.Number ) )
                    {
                        SetPhoneNumber( rockContext, person, pnCell, cbSms, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                    }

                    if ( !isMatch || !string.IsNullOrWhiteSpace( acAddress.Street1 ) )
                    {
                        string oldLocation = homeLocation != null ? homeLocation.Location.ToString() : string.Empty;
                        string newLocation = string.Empty;

                        var location = new LocationService( rockContext ).Get( acAddress.Street1, acAddress.Street2, acAddress.City, acAddress.State, acAddress.PostalCode, acAddress.Country );
                        if ( location != null )
                        {
                            if ( homeLocation == null )
                            {
                                homeLocation = new GroupLocation();
                                homeLocation.GroupLocationTypeValueId = _homeAddressType.Id;
                                family.GroupLocations.Add( homeLocation );
                            }
                            else
                            {
                                oldLocation = homeLocation.Location.ToString();
                            }

                            homeLocation.Location = location;
                            newLocation = location.ToString();
                        }
                        else
                        {
                            if ( homeLocation != null )
                            {
                                homeLocation.Location = null;
                                family.GroupLocations.Remove( homeLocation );
                                new GroupLocationService( rockContext ).Delete( homeLocation );
                            }
                        }
                    }
                }

                // Save the person and change history
                rockContext.SaveChanges();

                // Check to see if a workflow should be launched for each person
                WorkflowTypeCache workflowType = null;
                Guid? workflowTypeGuid = GetAttributeValue( AttributeKey.Workflow ).AsGuidOrNull();
                if ( workflowTypeGuid.HasValue )
                {
                    workflowType = WorkflowTypeCache.Get( workflowTypeGuid.Value );
                }

                // Save the registrations ( and launch workflows )
                var newGroupMembers = new List<GroupMember>();
                AddPersonToGroup( rockContext, person, workflowType, newGroupMembers );

                // Show the results
                pnlView.Visible = false;
                pnlResult.Visible = true;

                // Show lava content
                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "Group", _group );
                mergeFields.Add( "GroupMembers", newGroupMembers );

                string template = GetAttributeValue( AttributeKey.ResultLavaTemplate );
                lResult.Text = template.ResolveMergeFields( mergeFields );

                // Will only redirect if a value is specifed
                NavigateToLinkedPage( "ResultPage" );
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the RSVP notice.
        /// </summary>
        private void ShowCapacityNotice()
        {
            if ( CurrentPersonId.HasValue )
            {
                if ( _group.Members
                .Any( m =>
                    m.PersonId == CurrentPersonId.Value ) )
                {
                    _isEditing = true;
                }
            }

            if ( _isEditing && CurrentPersonId.HasValue )
            {
                var groupMember = _group.Members.Where( m =>
                       m.PersonId == CurrentPersonId &&
                       m.GroupRoleId == _defaultGroupRole.Id ).FirstOrDefault();

                if ( groupMember != null )
                {
                    groupMember.LoadAttributes();
                    _currentRSVP = groupMember.GetAttributeValue( "RSVPCount" ).AsInteger();

                    if ( !Page.IsPostBack )
                    {
                        numHowMany.Value = _currentRSVP;
                    }
                }
            }

            numHowMany.Minimum = 1;

            if ( _group.Attributes == null )
            {
                _group.LoadAttributes();
            }
            var capacity = _group.GetAttributeValue( "MaxRSVP" ).AsInteger();

            if ( capacity > 0 )
            {
                var current = GetCurrentRsvps();
                var remaining = capacity - current;
                numHowMany.Maximum = remaining;

                nbCapacity.Title = string.Format( "{0} Full", _group.GroupType.GroupTerm );

                if ( current >= capacity )
                {
                    nbCapacity.Text = string.Format( "<p>This {0} has reached its capacity.</p>", _group.GroupType.GroupTerm );
                    nbCapacity.Visible = true;
                    numHowMany.Maximum = 0;
                    numHowMany.Value = 0;
                    btnRegister.Enabled = false;
                }
                else
                {
                    if ( numHowMany.Value >= remaining )
                    {
                        nbCapacity.Text = string.Format(
                            "<p>This {0} only has capacity for {1} more {2}.",
                            _group.GroupType.GroupTerm,
                            remaining,
                            _group.GroupType.GroupMemberTerm.PluralizeIf( remaining > 1 ).ToLower() );
                        nbCapacity.Visible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Counts the group member attributes for curent RSVPs.
        /// </summary>
        private int GetCurrentRsvps()
        {
            var count = 0;

            foreach ( var member in _group.Members )
            {
                member.LoadAttributes();
                var rsvp = member.GetAttributeValue( "RSVPCount" ).AsInteger();
                count += rsvp;
            }

            if ( _isEditing )
            {
                count -= _currentRSVP;
            }

            return count;
        }

        /// <summary>
        /// Shows the details.
        /// </summary>
        private void ShowDetails()
        {
            _rockContext = _rockContext ?? new RockContext();

            if ( _group != null )
            {
                ShowCapacityNotice();

                // Show lava content
                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "Group", _group );

                string template = GetAttributeValue( AttributeKey.LavaTemplate );
                lLavaOverview.Text = template.ResolveMergeFields( mergeFields );

                tbEmail.Required = GetAttributeValue( AttributeKey.RequireEmail ).AsBoolean();
                pnCell.Required = GetAttributeValue( AttributeKey.RequiredMobile ).AsBoolean();

                pnlHomePhone.Visible = !IsSimple;
                pnlCellPhone.Visible = !IsSimple;
                acAddress.Visible = !IsSimple;

                string phoneLabel = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Value;
                phoneLabel = phoneLabel.Trim().EndsWith( "Phone" ) ? phoneLabel : phoneLabel + " Phone";
                pnCell.Label = phoneLabel;

                GroupMember groupMember = null;

                if ( CurrentPersonId.HasValue && _autoFill )
                {
                    var personService = new PersonService( _rockContext );
                    Person person = personService
                        .Queryable( "PhoneNumbers.NumberTypeValue" ).AsNoTracking()
                        .FirstOrDefault( p => p.Id == CurrentPersonId.Value );

                    groupMember = new GroupMemberService( _rockContext ).GetByGroupIdAndPersonId( _group.Id, CurrentPersonId.Value ).FirstOrDefault();

                    tbFirstName.Text = CurrentPerson.FirstName;
                    tbLastName.Text = CurrentPerson.LastName;
                    tbEmail.Text = CurrentPerson.Email;

                    tbFirstName.Enabled = !_isEditing;
                    tbLastName.Enabled = !_isEditing;

                    if ( !IsSimple )
                    {
                        Guid homePhoneType = Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid();
                        var homePhone = person.PhoneNumbers
                            .FirstOrDefault( n => n.NumberTypeValue.Guid.Equals( homePhoneType ) );
                        if ( homePhone != null )
                        {
                            pnHome.Text = homePhone.Number;
                        }

                        Guid cellPhoneType = Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid();
                        var cellPhone = person.PhoneNumbers
                            .FirstOrDefault( n => n.NumberTypeValue.Guid.Equals( cellPhoneType ) );
                        if ( cellPhone != null )
                        {
                            pnCell.Text = cellPhone.Number;
                            cbSms.Checked = cellPhone.IsMessagingEnabled;
                        }

                        var homeAddress = person.GetHomeLocation();
                        if ( homeAddress != null )
                        {
                            acAddress.SetValues( homeAddress );
                        }
                    }

                    if ( groupMember == null )
                    {
                        // only create a new one if parent was specified
                        if ( _group != null )
                        {
                            groupMember = new GroupMember { Id = 0 };
                            groupMember.GroupId = _group.Id;
                            groupMember.Group = new GroupService( _rockContext ).Get( groupMember.GroupId );
                            groupMember.GroupRoleId = groupMember.Group.GroupType.DefaultGroupRoleId ?? 0;
                            groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                            groupMember.DateTimeAdded = RockDateTime.Now;
                        }
                    }

                    if ( GetAttributeValue( AttributeKey.ShowAttributes ).AsBoolean() && groupMember != null )
                    {
                        groupMember.LoadAttributes();
                        avcGroupMemberAttributes.Visible = false;
                        avcGroupMemberAttributes.NumberOfColumns = GetAttributeValue( AttributeKey.AttributeColumns ).AsIntegerOrNull() ?? 2;

                        List<string> editableGroupMemberAttributes;

                        if ( _group.IsAuthorized( Authorization.ADMINISTRATE, this.CurrentPerson ) )
                        {
                            editableGroupMemberAttributes = groupMember.Attributes.Where( a => a.Value.IsPublic ).Select( a => a.Key ).ToList();
                        }
                        else
                        {
                            editableGroupMemberAttributes = groupMember.Attributes.Where( a => a.Value.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) && a.Value.IsPublic ).Select( a => a.Key ).ToList();
                        }

                        if ( editableGroupMemberAttributes.Any() )
                        {
                            avcGroupMemberAttributes.Visible = true;
                            avcGroupMemberAttributes.ExcludedAttributes = groupMember.Attributes.Where( a => !editableGroupMemberAttributes.Contains( a.Key ) ).Select( a => a.Value ).ToArray();
                            avcGroupMemberAttributes.AddEditControls( groupMember );
                        }
                    }

                }
            }

            string registerButtonText = GetAttributeValue( AttributeKey.RegisterButtonAltText );
            if ( string.IsNullOrWhiteSpace( registerButtonText ) )
            {
                registerButtonText = "Register";
            }
            if ( _isEditing )
            {
                registerButtonText = "Update";
            }
            btnRegister.Text = registerButtonText;
        }

        /// <summary>
        /// Adds the person to group.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="groupMembers">The group members.</param>
        private void AddPersonToGroup( RockContext rockContext, Person person, WorkflowTypeCache workflowType, List<GroupMember> groupMembers )
        {
            if ( person != null )
            {
                GroupMember groupMember = null;
                if ( !_group.Members
                    .Any( m =>
                        m.PersonId == person.Id &&
                        m.GroupRoleId == _defaultGroupRole.Id ) )
                {
                    var groupMemberService = new GroupMemberService( rockContext );
                    groupMember = new GroupMember();
                    groupMember.PersonId = person.Id;
                    groupMember.GroupRoleId = _defaultGroupRole.Id;
                    groupMember.GroupMemberStatus = ( GroupMemberStatus ) GetAttributeValue( AttributeKey.GroupMemberStatus ).AsInteger();
                    groupMember.GroupId = _group.Id;
                    groupMemberService.Add( groupMember );
                    rockContext.SaveChanges();
                }
                else
                {
                    GroupMemberStatus status = ( GroupMemberStatus ) GetAttributeValue( AttributeKey.GroupMemberStatus ).AsInteger();
                    groupMember = _group.Members.Where( m =>
                       m.PersonId == person.Id &&
                       m.GroupRoleId == _defaultGroupRole.Id ).FirstOrDefault();
                    if ( groupMember.GroupMemberStatus != status )
                    {
                        var groupMemberService = new GroupMemberService( rockContext );

                        // reload this group member in the current context
                        groupMember = groupMemberService.Get( groupMember.Id );
                        groupMember.GroupMemberStatus = status;
                        rockContext.SaveChanges();
                    }
                }

                if ( groupMember != null )
                {
                    groupMember.LoadAttributes();
                    avcGroupMemberAttributes.GetEditValues( groupMember );
                    groupMember.SetAttributeValue( "RSVPCount", numHowMany.Value );
                    groupMember.SaveAttributeValues();

                    SendGroupEmail( groupMember );
                }

                if ( groupMember != null && workflowType != null && ( workflowType.IsActive ?? true ) )
                {
                    try
                    {
                        List<string> workflowErrors;
                        var workflow = Workflow.Activate( workflowType, person.FullName );
                        new WorkflowService( rockContext ).Process( workflow, groupMember, out workflowErrors );
                    }
                    catch ( Exception ex )
                    {
                        ExceptionLogService.LogException( ex, this.Context );
                    }
                }
            }
        }

        /// <summary>
        /// Sends the new group member an email using the group attributes
        /// </summary>
        /// <returns>true if settings are valid; false otherwise</returns>
        protected void SendGroupEmail( GroupMember groupMember )
        {
            var group = groupMember.Group;
            if ( group.Attributes == null )
            {
                group.LoadAttributes();
            }

            var sendEmail = group.GetAttributeValue( "SendEmail" ).AsBoolean();

            if ( sendEmail && !string.IsNullOrWhiteSpace( groupMember.Person.Email ) )
            {
                var message = group.GetAttributeValue( "Message" );

                if ( !string.IsNullOrWhiteSpace( message ) )
                {
                    var mergeFields = new Dictionary<string, object>()
                    {
                        {"Group", group},
                        {"GroupMember", groupMember},
                        {"Person", groupMember.Person}
                    };

                    var fromEmail = group.GetAttributeValue( "FromEmail" );
                    var fromName = group.GetAttributeValue( "FromName" );
                    var subject = group.GetAttributeValue( "Subject" );

                    var emailMessage = new RockEmailMessage();
                    var emailMessageRecipient = new RockEmailMessageRecipient( groupMember.Person, mergeFields );
                    emailMessage.AddRecipient( emailMessageRecipient );

                    emailMessage.FromEmail = fromEmail;
                    emailMessage.FromName = fromName;
                    emailMessage.Subject = subject;
                    emailMessage.Message = message;

                    emailMessage.CreateCommunicationRecord = true;
                    emailMessage.AppRoot = Rock.Web.Cache.GlobalAttributesCache.Get().GetValue( "InternalApplicationRoot" ) ?? string.Empty;

                    emailMessage.Send();
                }
            }
        }

        /// <summary>
        /// Checks the settings.  If false is returned, it's expected that the caller will make
        /// the nbNotice visible to inform the user of the "settings" error.
        /// </summary>
        /// <returns>true if settings are valid; false otherwise</returns>
        private bool CheckSettings()
        {
            _rockContext = _rockContext ?? new RockContext();

            _mode = GetAttributeValue( AttributeKey.Mode );

            _autoFill = GetAttributeValue( AttributeKey.AutoFillForm ).AsBoolean();

            var groupService = new GroupService( _rockContext );
            bool groupIsFromQryString = true;

            Guid? groupGuid = GetAttributeValue( AttributeKey.Group ).AsGuidOrNull();
            if ( groupGuid.HasValue )
            {
                _group = groupService.Get( groupGuid.Value );
                groupIsFromQryString = false;
            }

            if ( _group == null )
            {
                groupGuid = PageParameter( "GroupGuid" ).AsGuidOrNull();
                if ( groupGuid.HasValue )
                {
                    _group = groupService.Get( groupGuid.Value );
                }
            }

            if ( _group == null && GetAttributeValue( AttributeKey.EnablePassingGroupId ).AsBoolean( false ) )
            {
                int? groupId = PageParameter( "GroupId" ).AsIntegerOrNull();
                if ( groupId.HasValue )
                {
                    _group = groupService.Get( groupId.Value );
                }
            }

            if ( _group == null )
            {
                nbNotice.Heading = "Unknown Group";
                nbNotice.Text = "<p>This page requires a valid group identifying parameter and there was not one provided.</p>";
                return false;
            }
            else
            {
                Guid RsvpGroupTypeGuid = "1A082EFF-30DA-44B2-8E48-02385C20828E".AsGuid();
                var groupTypeGuids = new GroupTypeService( _rockContext ).Queryable()
                                        .AsNoTracking()
                                        .Where( g =>
                                            g.Guid == RsvpGroupTypeGuid ||
                                            g.InheritedGroupType.Guid == RsvpGroupTypeGuid )
                                        .Select( g => g.Guid )
                                        .ToList();

                if ( groupIsFromQryString && groupTypeGuids.Any() && !groupTypeGuids.Contains( _group.GroupType.Guid ) )
                {
                    _group = null;
                    nbNotice.Heading = "Invalid Group";
                    nbNotice.Text = "<p>The selected group is a restricted group type therefore this block cannot be used to add people to these groups (unless configured to allow).</p>";
                    return false;
                }
                else
                {
                    _defaultGroupRole = _group.GroupType.DefaultGroupRole;
                }
            }

            _dvcConnectionStatus = DefinedValueCache.Get( GetAttributeValue( AttributeKey.ConnectionStatus ).AsGuid() );
            if ( _dvcConnectionStatus == null )
            {
                nbNotice.Heading = "Invalid Connection Status";
                nbNotice.Text = "<p>The selected Connection Status setting does not exist.</p>";
                return false;
            }

            _dvcRecordStatus = DefinedValueCache.Get( GetAttributeValue( AttributeKey.RecordStatus ).AsGuid() );
            if ( _dvcRecordStatus == null )
            {
                nbNotice.Heading = "Invalid Record Status";
                nbNotice.Text = "<p>The selected Record Status setting does not exist.</p>";
                return false;
            }

            _married = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_MARRIED.AsGuid() );
            _homeAddressType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
            _familyType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() );
            _adultRole = _familyType.Roles.FirstOrDefault( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) );

            if ( _married == null || _homeAddressType == null || _familyType == null || _adultRole == null )
            {
                nbNotice.Heading = "Missing System Value";
                nbNotice.Text = "<p>There is a missing or invalid system value. Check the settings for Marital Status of 'Married', Location Type of 'Home', Group Type of 'Family', and Family Group Role of 'Adult'.</p>";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the phone number.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        /// <param name="pnbNumber">The PNB number.</param>
        /// <param name="cbSms">The cb SMS.</param>
        /// <param name="phoneTypeGuid">The phone type unique identifier.</param>
        private void SetPhoneNumber( RockContext rockContext, Person person, PhoneNumberBox pnbNumber, RockCheckBox cbSms, Guid phoneTypeGuid )
        {
            var phoneType = DefinedValueCache.Get( phoneTypeGuid );
            if ( phoneType != null )
            {
                var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneType.Id );
                string oldPhoneNumber = string.Empty;
                if ( phoneNumber == null )
                {
                    phoneNumber = new PhoneNumber { NumberTypeValueId = phoneType.Id };
                }
                else
                {
                    oldPhoneNumber = phoneNumber.NumberFormattedWithCountryCode;
                }

                phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbNumber.CountryCode );
                phoneNumber.Number = PhoneNumber.CleanNumber( pnbNumber.Number );

                if ( string.IsNullOrWhiteSpace( phoneNumber.Number ) )
                {
                    if ( phoneNumber.Id > 0 )
                    {
                        new PhoneNumberService( rockContext ).Delete( phoneNumber );
                        person.PhoneNumbers.Remove( phoneNumber );
                    }
                }
                else
                {
                    if ( phoneNumber.Id <= 0 )
                    {
                        person.PhoneNumbers.Add( phoneNumber );
                    }
                    if ( cbSms != null && cbSms.Checked )
                    {
                        phoneNumber.IsMessagingEnabled = true;
                        person.PhoneNumbers
                            .Where( n => n.NumberTypeValueId != phoneType.Id )
                            .ToList()
                            .ForEach( n => n.IsMessagingEnabled = false );
                    }
                }
            }
        }

        #endregion
    }
}
