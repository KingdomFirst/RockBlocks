// <copyright>
// Copyright 2022 by Kingdom First Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NuGet;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Logging;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using rocks.kfs.StepsToCare.Model;

namespace RockWeb.Plugins.rocks_kfs.StepsToCare
{
    #region Block Attributes

    [DisplayName( "Care Entry" )]
    [Category( "KFS > Steps To Care" )]
    [Description( "Care entry block for KFS Steps to Care package. Used for adding and editing care needs " )]

    #endregion

    #region Block Settings

    [BooleanField( "Allow New Person Entry",
        Description = "Should you be able to enter a new person from the care entry form and use person matching?",
        DefaultBooleanValue = false,
        Key = AttributeKey.AllowNewPerson )]

    [GroupRoleField( null, "Group Type and Role",
        Description = "Select the group Type and Role of the leader you would like auto assigned to care need. If none are selected it will not auto assign the small group member to the need. ",
        IsRequired = false,
        Key = AttributeKey.GroupTypeAndRole )]

    [BooleanField( "Auto Assign Worker with Geofence",
        Description = "Care Need Workers can have Geofence locations assigned to them, if there are workers with geofences and this block setting is enabled it will auto assign workers to this need on new entries based on the requester home being in the geofence.",
        DefaultBooleanValue = true,
        Key = AttributeKey.AutoAssignWorkerGeofence )]

    [BooleanField( "Auto Assign Worker (load balanced)",
        Description = "Use intelligent load balancing to auto assign care workers to a care need based on their workload and other parameters?",
        DefaultBooleanValue = true,
        Key = AttributeKey.AutoAssignWorker )]

    [SystemCommunicationField( "Newly Assigned Need Notification",
        Description = "Select the system communication template for the new assignment notification.",
        DefaultSystemCommunicationGuid = rocks.kfs.StepsToCare.SystemGuid.SystemCommunication.CARE_NEED_ASSIGNED,
        Key = AttributeKey.NewAssignmentNotification )]

    [BooleanField( "Verbose Logging",
        Description = "Enable verbose Logging to help in determining issues with adding needs or auto assigning workers. Not recommended for normal use.",
        DefaultBooleanValue = false,
        Key = AttributeKey.VerboseLogging )]

    [CustomDropdownListField( "Load Balanced Workers assignment type",
        Description = "How should the auto assign worker load balancing work? Default: Exclusive. \"Prioritize\", it will prioritize the workers being assigned based on campus, category and any other parameters on the worker but still assign to any worker if their workload matches. \"Exclusive\", if there are workers with matching campus, category or other parameters it will only load balance between those workers.",
        ListSource = "Prioritize,Exclusive",
        DefaultValue = "Exclusive",
        Key = AttributeKey.LoadBalanceWorkersType )]

    [BooleanField( "Enable Family Needs",
        Description = "Show a checkbox to 'Include Family' which will create duplicate Care Needs for each family member with their own workers.",
        DefaultBooleanValue = false,
        Key = AttributeKey.EnableFamilyNeeds,
        Category = AttributeCategory.FamilyNeeds )]

    [CustomDropdownListField( "Adults in Family Worker Assignment",
        Description = "How should workers be assigned to spouses and other adults in the family when using 'Family Needs'. Normal behavior, use the same settings as a normal Care Need (Group Leader, Geofence and load balanced), or assign to Care Workers Only (load balanced).",
        ListSource = "Normal,Workers Only",
        DefaultValue = "Normal",
        Key = AttributeKey.AdultFamilyWorkers,
        Category = AttributeCategory.FamilyNeeds )]

    [SecurityAction(
        SecurityActionKey.UpdateStatus,
        "The roles and/or users that have access to update the status of Care Needs." )]
    #endregion

    public partial class CareEntry : Rock.Web.UI.RockBlock
    {

        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string AllowNewPerson = "AllowNewPerson";
            public const string GroupTypeAndRole = "GroupTypeAndRole";
            public const string AutoAssignWorkerGeofence = "AutoAssignWorkerGeofence";
            public const string AutoAssignWorker = "AutoAssignWorker";
            public const string NewAssignmentNotification = "NewAssignmentNotification";
            public const string VerboseLogging = "VerboseLogging";
            public const string EnableFamilyNeeds = "EnableFamilyNeeds";
            public const string AdultFamilyWorkers = "AdultFamilyWorkers";
            public const string LoadBalanceWorkersType = "LoadBalanceWorkersType";
        }
        private static class PageParameterKey
        {
            public const string PersonId = "PersonId";
            public const string DateEntered = "DateEntered";
            public const string Status = "Status";
            public const string CampusId = "CampusId";
            public const string Category = "Category";
            public const string Details = "Details";
            public const string CareNeedId = "CareNeedId";
        }

        private static class AttributeCategory
        {
            public const string FamilyNeeds = "Family Needs";
        }

        private static class SecurityActionKey
        {
            public const string UpdateStatus = "UpdateStatus";

        }

        private bool _allowNewPerson = false;

        #region Properties
        /// <summary>
        /// Gets or sets the Assigned Persons to the list.
        /// </summary>
        /// <value>
        /// The Assigned Person list.
        /// </value>
        protected List<AssignedPerson> AssignedPersons
        {
            get
            {
                var persons = Session["AssignedPersons"] as List<AssignedPerson>;
                if ( persons == null )
                {
                    persons = new List<AssignedPerson>();
                    Session["AssignedPersons"] = persons;
                }

                return persons;
            }

            set
            {
                Session["AssignedPersons"] = value;
            }
        }
        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlCareEntry );

            gAssignedPersons.DataKeyNames = new string[] { "Id" };
            gAssignedPersons.GridRebind += gAssignedPersons_GridRebind;
            gAssignedPersons.Actions.ShowAdd = false;
            gAssignedPersons.ShowActionRow = false;

            _allowNewPerson = GetAttributeValue( AttributeKey.AllowNewPerson ).AsBoolean();
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
                cpCampus.Campuses = CampusCache.All();
                ShowDetail( PageParameter( PageParameterKey.CareNeedId ).AsInteger() );
            }
            else
            {
                var rockContext = new RockContext();
                CareNeed item = new CareNeedService( rockContext ).Get( hfCareNeedId.ValueAsInt() );
                if ( item == null )
                {
                    item = new CareNeed();
                }
                item.LoadAttributes();

                phAttributes.Controls.Clear();
                Helper.AddEditControls( item, phAttributes, false, BlockValidationGroup, 2 );
            }

            confirmExit.Enabled = true;
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
            ShowDetail( PageParameter( PageParameterKey.CareNeedId ).AsInteger() );
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                RockContext rockContext = new RockContext();
                CareNeedService careNeedService = new CareNeedService( rockContext );
                AssignedPersonService assignedPersonService = new AssignedPersonService( rockContext );

                CareNeed careNeed = null;
                int careNeedId = PageParameter( PageParameterKey.CareNeedId ).AsInteger();
                var isNew = false;

                if ( !careNeedId.Equals( 0 ) )
                {
                    careNeed = careNeedService.Get( careNeedId );
                }

                if ( careNeed == null )
                {
                    isNew = true;
                    careNeed = new CareNeed { Id = 0 };
                }

                careNeed.Details = dtbDetailsText.Text;
                careNeed.CampusId = cpCampus.SelectedCampusId;

                careNeed.PersonAliasId = ppPerson.PersonAliasId;

                Person person = null;

                if ( _allowNewPerson && ppPerson.PersonId == null )
                {
                    var personService = new PersonService( new RockContext() );
                    person = personService.FindPerson( new PersonService.PersonMatchQuery( dtbFirstName.Text, dtbLastName.Text, ebEmail.Text, pnbCellPhone.Number ), false, true, false );

                    if ( person == null && dtbFirstName.Text.IsNotNullOrWhiteSpace() && dtbLastName.Text.IsNotNullOrWhiteSpace() && ( !string.IsNullOrWhiteSpace( ebEmail.Text ) || !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbHomePhone.Number ) ) || !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbCellPhone.Number ) ) ) )
                    {
                        var personRecordTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                        var personStatusPending = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

                        person = new Person();
                        person.IsSystem = false;
                        person.RecordTypeValueId = personRecordTypeId;
                        person.RecordStatusValueId = personStatusPending;
                        person.FirstName = dtbFirstName.Text;
                        person.LastName = dtbLastName.Text;
                        person.Gender = Gender.Unknown;

                        if ( !string.IsNullOrWhiteSpace( ebEmail.Text ) )
                        {
                            person.Email = ebEmail.Text;
                            person.IsEmailActive = true;
                            person.EmailPreference = EmailPreference.EmailAllowed;
                        }

                        PersonService.SaveNewPerson( person, rockContext, cpCampus.SelectedCampusId );

                        if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbHomePhone.Number ) ) )
                        {
                            var homePhoneType = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME ) );

                            var phoneNumber = new PhoneNumber { NumberTypeValueId = homePhoneType.Id };
                            phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbHomePhone.CountryCode );
                            phoneNumber.Number = PhoneNumber.CleanNumber( pnbHomePhone.Number );
                            person.PhoneNumbers.Add( phoneNumber );
                        }

                        if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbCellPhone.Number ) ) )
                        {
                            var mobilePhoneType = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ) );

                            var phoneNumber = new PhoneNumber { NumberTypeValueId = mobilePhoneType.Id };
                            phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbCellPhone.CountryCode );
                            phoneNumber.Number = PhoneNumber.CleanNumber( pnbCellPhone.Number );
                            person.PhoneNumbers.Add( phoneNumber );
                        }

                        if ( lapAddress.Location != null )
                        {
                            Guid? familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuidOrNull();
                            if ( familyGroupTypeGuid.HasValue )
                            {
                                var familyGroup = person.GetFamily();
                                if ( familyGroup != null )
                                {
                                    Guid? addressTypeGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuidOrNull();
                                    if ( addressTypeGuid.HasValue )
                                    {
                                        var groupLocationService = new GroupLocationService( rockContext );

                                        var dvHomeAddressType = DefinedValueCache.Get( addressTypeGuid.Value );
                                        var familyAddress = groupLocationService.Queryable().Where( l => l.GroupId == familyGroup.Id && l.GroupLocationTypeValueId == dvHomeAddressType.Id ).FirstOrDefault();
                                        if ( !string.IsNullOrWhiteSpace( lapAddress.Location.Street1 ) )
                                        {
                                            if ( familyAddress == null )
                                            {
                                                familyAddress = new GroupLocation();
                                                groupLocationService.Add( familyAddress );
                                                familyAddress.GroupLocationTypeValueId = dvHomeAddressType.Id;
                                                familyAddress.GroupId = familyGroup.Id;
                                                familyAddress.IsMailingLocation = true;
                                                familyAddress.IsMappedLocation = true;
                                            }

                                            var loc = lapAddress.Location;
                                            familyAddress.Location = new LocationService( rockContext ).Get(
                                                loc.Street1, loc.Street2, loc.City, loc.State, loc.PostalCode, loc.Country, familyGroup, true );

                                        }
                                    }
                                }
                            }
                        }
                    }
                    careNeed.PersonAliasId = person?.PrimaryAliasId;

                    if ( careNeed.PersonAliasId == null )
                    {
                        cvPersonValidation.IsValid = false;
                        cvPersonValidation.ErrorMessage = "Please select a Person or provide First Name, Last Name and Email or Phone number to proceed.";
                        wpRequestor.CssClass += " has-error";
                        return;
                    }
                }
                else if ( ppPerson.PersonId.HasValue )
                {
                    person = new PersonService( rockContext ).Get( ppPerson.PersonId.Value );
                }

                careNeed.SubmitterAliasId = ppSubmitter.PersonAliasId;

                careNeed.StatusValueId = dvpStatus.SelectedValue.AsIntegerOrNull();
                careNeed.CategoryValueId = dvpCategory.SelectedValue.AsIntegerOrNull();

                if ( dpDate.SelectedDateTime.HasValue )
                {
                    careNeed.DateEntered = dpDate.SelectedDateTime.Value;
                }

                careNeed.WorkersOnly = cbWorkersOnly.Checked;

                var newlyAssignedPersons = new List<AssignedPerson>();
                if ( careNeed.AssignedPersons != null )
                {
                    if ( AssignedPersons.Any() )
                    {
                        var assignedPersonsLookup = AssignedPersons;

                        foreach ( var existingAssigned in assignedPersonsLookup )
                        {
                            if ( !careNeed.AssignedPersons.Any( ap => ap.PersonAliasId == existingAssigned.PersonAliasId ) )
                            {
                                var personAlias = existingAssigned.PersonAlias;
                                if ( personAlias == null )
                                {
                                    personAlias = new PersonAliasService( rockContext ).Get( existingAssigned.PersonAliasId.Value );
                                }
                                var assignedPerson = new AssignedPerson
                                {
                                    PersonAlias = personAlias,
                                    PersonAliasId = personAlias.Id,
                                    FollowUpWorker = existingAssigned.FollowUpWorker,
                                    WorkerId = existingAssigned.WorkerId
                                };
                                careNeed.AssignedPersons.Add( assignedPerson );
                                newlyAssignedPersons.Add( assignedPerson );
                            }
                        }
                        var removePersons = careNeed.AssignedPersons.Where( ap => !assignedPersonsLookup.Select( apl => apl.Id ).ToList().Contains( ap.Id ) ).ToList();
                        assignedPersonService.DeleteRange( removePersons );
                        careNeed.AssignedPersons.RemoveAll( removePersons );
                    }
                    else if ( careNeed.AssignedPersons.Any() )
                    {
                        assignedPersonService.DeleteRange( careNeed.AssignedPersons );
                        careNeed.AssignedPersons.Clear();
                    }
                }

                if ( careNeed.IsValid )
                {
                    var childNeedsCreated = false;
                    if ( careNeed.Id.Equals( 0 ) )
                    {
                        careNeedService.Add( careNeed );
                    }

                    // get attributes
                    careNeed.LoadAttributes();
                    Helper.GetEditValues( phAttributes, careNeed );

                    if ( cbIncludeFamily.Visible && cbIncludeFamily.Checked && ( isNew || ( careNeed.ChildNeeds == null || ( careNeed.ChildNeeds != null && !careNeed.ChildNeeds.Any() ) ) ) )
                    {
                        var family = person.GetFamilyMembers( false, rockContext );
                        foreach ( var fm in family )
                        {
                            var copyNeed = ( CareNeed ) careNeed.Clone();
                            copyNeed.Id = 0;
                            copyNeed.Guid = Guid.NewGuid();
                            copyNeed.PersonAliasId = fm.Person.PrimaryAliasId;
                            if ( copyNeed.Campus != null )
                            {
                                copyNeed.Campus = null;
                            }
                            if ( copyNeed.Status != null )
                            {
                                copyNeed.Status = null;
                            }
                            if ( copyNeed.Category != null )
                            {
                                copyNeed.Category = null;
                            }
                            if ( copyNeed.AssignedPersons != null && copyNeed.AssignedPersons.Any() )
                            {
                                copyNeed.AssignedPersons = new List<AssignedPerson>();
                            }

                            if ( careNeed.ChildNeeds == null )
                            {
                                careNeed.ChildNeeds = new List<CareNeed>();
                            }
                            careNeed.ChildNeeds.Add( copyNeed );
                        }
                        childNeedsCreated = true;
                    }

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        careNeed.SaveAttributeValues( rockContext );
                    } );

                    if ( isNew )
                    {
                        AutoAssignWorkers( careNeed );
                    }

                    if ( childNeedsCreated && careNeed.ChildNeeds != null && careNeed.ChildNeeds.Any() )
                    {
                        var familyGroupType = GroupTypeCache.GetFamilyGroupType();
                        var adultRoleId = familyGroupType.Roles.FirstOrDefault( a => a.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).Id;
                        foreach ( var need in careNeed.ChildNeeds )
                        {
                            if ( need.PersonAlias != null && need.PersonAlias.Person.GetFamilyRole().Id != adultRoleId )
                            {
                                AutoAssignWorkers( need, true, true );
                            }
                            else
                            {
                                var adultFamilyWorkers = GetAttributeValue( AttributeKey.AdultFamilyWorkers );
                                AutoAssignWorkers( need, adultFamilyWorkers == "Workers Only" );
                            }
                        }
                    }

                    // if a NewAssignmentNotificationEmailTemplate is configured, send an email
                    var assignmentEmailTemplateGuid = GetAttributeValue( AttributeKey.NewAssignmentNotification ).AsGuidOrNull();

                    var assignedPersons = careNeed.AssignedPersons;
                    if ( newlyAssignedPersons.Any() )
                    {
                        assignedPersons = newlyAssignedPersons;
                    }
                    else
                    {
                        // Reload Care Need after save changes
                        careNeed = new CareNeedService( new RockContext() ).Get( careNeed.Id );
                        assignedPersons = careNeed.AssignedPersons;
                        if ( careNeed.ChildNeeds != null && careNeed.ChildNeeds.Any() )
                        {
                            foreach ( var need in careNeed.ChildNeeds )
                            {
                                assignedPersons.AddRange( need.AssignedPersons );
                            }
                        }
                    }

                    if ( assignedPersons != null && assignedPersons.Any() && assignmentEmailTemplateGuid.HasValue && ( isNew || newlyAssignedPersons.Any() ) )
                    {
                        var errors = new List<string>();
                        var errorsSms = new List<string>();
                        Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                        linkedPages.Add( "CareDetail", CurrentPageReference.BuildUrl() );
                        linkedPages.Add( "CareDashboard", GetParentPage().BuildUrl() );

                        var systemCommunication = new SystemCommunicationService( rockContext ).Get( assignmentEmailTemplateGuid.Value );
                        var emailMessage = new RockEmailMessage( systemCommunication );
                        var smsMessage = new RockSMSMessage( systemCommunication );
                        emailMessage.AppRoot = smsMessage.AppRoot = ResolveRockUrl( "~/" );
                        emailMessage.ThemeRoot = smsMessage.ThemeRoot = ResolveRockUrl( "~~/" );
                        foreach ( var assignee in assignedPersons )
                        {
                            assignee.PersonAlias.Person.LoadAttributes();
                            var smsNumber = assignee.PersonAlias.Person.PhoneNumbers.GetFirstSmsNumber();
                            if ( !assignee.PersonAlias.Person.CanReceiveEmail( false ) && smsNumber.IsNullOrWhiteSpace() )
                            {
                                continue;
                            }

                            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                            mergeFields.Add( "CareNeed", careNeed );
                            mergeFields.Add( "LinkedPages", linkedPages );
                            mergeFields.Add( "AssignedPerson", assignee );
                            mergeFields.Add( "Person", assignee.PersonAlias.Person );

                            var notificationType = assignee.PersonAlias.Person.GetAttributeValue( rocks.kfs.StepsToCare.SystemGuid.PersonAttribute.NOTIFICATION.AsGuid() );

                            if ( notificationType == null || notificationType == "Email" || notificationType == "Both" )
                            {
                                if ( !assignee.PersonAlias.Person.CanReceiveEmail( false ) )
                                {
                                    var emailWarningMessage = string.Format( "{0} does not have a valid email address.", assignee.PersonAlias.Person.FullName );
                                    RockLogger.Log.Warning( "RockWeb.Plugins.rocks_kfs.StepsToCare.CareEntry", emailWarningMessage );
                                    errors.Add( emailWarningMessage );
                                }
                                else
                                {
                                    emailMessage.AddRecipient( new RockEmailMessageRecipient( assignee.PersonAlias.Person, mergeFields ) );
                                }
                            }

                            if ( notificationType == "SMS" || notificationType == "Both" )
                            {
                                if ( string.IsNullOrWhiteSpace( smsNumber ) )
                                {
                                    var smsWarningMessage = string.Format( "No SMS number could be found for {0}.", assignee.PersonAlias.Person.FullName );
                                    RockLogger.Log.Warning( "RockWeb.Plugins.rocks_kfs.StepsToCare.CareEntry", smsWarningMessage );
                                    errorsSms.Add( smsWarningMessage );
                                }

                                smsMessage.AddRecipient( new RockSMSMessageRecipient( assignee.PersonAlias.Person, smsNumber, mergeFields ) );
                            }
                        }
                        if ( emailMessage.GetRecipients().Count > 0 )
                        {
                            emailMessage.Send( out errors );
                        }
                        if ( smsMessage.GetRecipients().Count > 0 )
                        {
                            smsMessage.Send( out errorsSms );
                        }

                        if ( errors.Any() || errorsSms.Any() )
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append( string.Format( "{0} Errors Sending Care Assignment Notification: ", errors.Count + errorsSms.Count ) );
                            errors.ForEach( es => { sb.AppendLine(); sb.Append( es ); } );
                            errorsSms.ForEach( es => { sb.AppendLine(); sb.Append( es ); } );
                            string errorStr = sb.ToString();
                            var exception = new Exception( errorStr );
                            HttpContext context = HttpContext.Current;
                            ExceptionLogService.LogException( exception, context );
                        }
                    }

                    // redirect back to parent
                    var personId = this.PageParameter( "PersonId" ).AsIntegerOrNull();
                    var qryParams = new Dictionary<string, string>();
                    if ( personId.HasValue )
                    {
                        qryParams.Add( "PersonId", personId.ToString() );
                    }

                    NavigateToParentPage( qryParams );
                }
                else
                {
                    cvCareNeed.IsValid = false;
                    cvCareNeed.ErrorMessage = careNeed.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            var personId = this.PageParameter( "PersonId" ).AsIntegerOrNull();
            var qryParams = new Dictionary<string, string>();
            if ( personId.HasValue )
            {
                qryParams.Add( "PersonId", personId.ToString() );
            }

            NavigateToParentPage( qryParams );
        }

        /// <summary>
        /// Handles the SelectPerson event of the ppPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( ppPerson.PersonId != null )
            {
                Person person = new PersonService( new RockContext() ).Get( ppPerson.PersonId.Value );
                if ( person != null )
                {
                    wpRequestor.CssClass = wpRequestor.CssClass.Replace( " has-error", "" );
                    if ( !string.IsNullOrWhiteSpace( person.FirstName ) )
                    {
                        dtbFirstName.Text = person.FirstName;
                    }
                    else if ( !string.IsNullOrWhiteSpace( person.NickName ) )
                    {
                        dtbFirstName.Text = person.NickName;
                    }

                    dtbFirstName.Enabled = string.IsNullOrWhiteSpace( dtbFirstName.Text );

                    dtbLastName.Text = person.LastName;
                    dtbLastName.Enabled = string.IsNullOrWhiteSpace( dtbLastName.Text );

                    var homePhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
                    if ( homePhoneType != null )
                    {
                        var homePhone = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == homePhoneType.Id );
                        if ( homePhone != null )
                        {
                            pnbHomePhone.Text = homePhone.NumberFormatted;
                            pnbHomePhone.Enabled = false;
                        }
                    }

                    var mobilePhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                    if ( mobilePhoneType != null )
                    {
                        var mobileNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == mobilePhoneType.Id );
                        if ( mobileNumber != null )
                        {
                            pnbCellPhone.Text = mobileNumber.NumberFormatted;
                            pnbCellPhone.Enabled = false;
                        }
                    }

                    ebEmail.Text = person.Email;
                    ebEmail.Enabled = false;

                    lapAddress.SetValue( person.GetHomeLocation() );
                    lapAddress.Enabled = false;

                    int? requestId = PageParameter( "CareNeedId" ).AsIntegerOrNull();

                    if ( !cpCampus.SelectedCampusId.HasValue && ( e != null || ( requestId.HasValue && requestId == 0 ) ) )
                    {
                        var personCampus = person.GetCampus();
                        cpCampus.SelectedCampusId = personCampus != null ? personCampus.Id : ( int? ) null;
                    }
                }
            }
            else
            {
                dtbFirstName.Enabled = true;
                dtbLastName.Enabled = true;
                pnbHomePhone.Enabled = true;
                pnbCellPhone.Enabled = true;
                ebEmail.Enabled = true;
                if ( lapAddress.Location != null )
                {
                    lapAddress.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnDeleteSelectedAssignedPersons control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDeleteSelectedAssignedPersons_Click( object sender, EventArgs e )
        {
            // get the selected personIds
            bool removeAll = false;
            var selectField = gAssignedPersons.ColumnsOfType<SelectField>().First();
            if ( selectField != null && selectField.HeaderCheckbox != null )
            {
                // if the 'Select All' checkbox in the header is checked, and they haven't unselected anything, then assume they want to remove all people
                removeAll = selectField.HeaderCheckbox.Checked && gAssignedPersons.SelectedKeys.Count == gAssignedPersons.PageSize;
            }

            if ( removeAll )
            {
                AssignedPersons.Clear();
            }
            else
            {
                var selectedIds = gAssignedPersons.SelectedKeys.OfType<int>().ToList();
                AssignedPersons.RemoveAll( a => selectedIds.Contains( a.Id ) );
            }

            BindAssignedPersonsGrid();

        }

        /// <summary>
        /// Handles the GridRebind event of the gAssignedPersons control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        private void gAssignedPersons_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindAssignedPersonsGrid();
        }

        /// <summary>
        /// Handles the DeleteClick event of the gAssignedPersons control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gAssignedPersons_DeleteClick( object sender, RowEventArgs e )
        {
            var remove = this.AssignedPersons.FirstOrDefault( ap => ap.Id == e.RowKeyId );
            this.AssignedPersons.Remove( remove );
            BindAssignedPersonsGrid();
        }

        /// <summary>
        /// Handles the SelectPerson event of the ppAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppAddPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( ppAddPerson.PersonId.HasValue )
            {
                if ( !AssignedPersons.Any( ap => ap.PersonAliasId == ppAddPerson.PersonAliasId ) )
                {
                    var addPerson = new AssignedPerson
                    {
                        PersonAliasId = ppAddPerson.PersonAliasId,
                        NeedId = hfCareNeedId.Value.AsInteger()
                    };
                    AssignedPersons.Add( addPerson );
                    BindAssignedPersonsGrid();
                }

                // clear out the personpicker and have it say "Add Person" again since they are added to the list
                ppAddPerson.SetValue( null );
                ppAddPerson.PersonName = "Add Person";
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the worker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bddlAddWorker_SelectionChanged( object sender, EventArgs e )
        {
            var selectedVal = bddlAddWorker.SelectedValue.SplitDelimitedValues( "^" );
            if ( selectedVal.IsNotNull() && selectedVal.Length > 1 && !AssignedPersons.Any( ap => ap.PersonAliasId == selectedVal[0].AsIntegerOrNull() ) )
            {
                var addPerson = new AssignedPerson
                {
                    PersonAliasId = selectedVal[0].AsIntegerOrNull() ?? 0,
                    NeedId = hfCareNeedId.Value.AsInteger(),
                    FollowUpWorker = !AssignedPersons.Any( ap => ap.FollowUpWorker.HasValue && ap.FollowUpWorker.Value ),
                    WorkerId = selectedVal[1].AsIntegerOrNull()
                };
                AssignedPersons.Add( addPerson );
                BindAssignedPersonsGrid();
            }

            bddlAddWorker.ClearSelection();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="careNeedId">The care need identifier</param>
        public void ShowDetail( int careNeedId )
        {
            CareNeed careNeed = null;
            var rockContext = new RockContext();
            CareNeedService careNeedService = new CareNeedService( rockContext );
            if ( !careNeedId.Equals( 0 ) )
            {
                careNeed = careNeedService.Get( careNeedId );
                pdAuditDetails.SetEntity( careNeed, ResolveRockUrl( "~" ) );
            }

            if ( careNeed == null )
            {
                careNeed = new CareNeed { Id = 0 };
                careNeed.DateEntered = RockDateTime.Now;
                var personId = this.PageParameter( PageParameterKey.PersonId ).AsIntegerOrNull();
                if ( personId.HasValue )
                {
                    var person = new PersonService( rockContext ).Get( personId.Value );
                    if ( person != null )
                    {
                        careNeed.PersonAliasId = person.PrimaryAliasId;
                        careNeed.PersonAlias = person.PrimaryAlias;
                    }
                }
                pdAuditDetails.Visible = false;
            }

            cbIncludeFamily.Visible = GetAttributeValue( AttributeKey.EnableFamilyNeeds ).AsBoolean();

            pnlNewPersonFields.Visible = _allowNewPerson;
            ppPerson.Required = !_allowNewPerson;

            dtbDetailsText.Text = ( careNeed.Details.IsNotNullOrWhiteSpace() ) ? careNeed.Details : PageParameter( PageParameterKey.Details ).ToString();
            dpDate.SelectedDateTime = careNeed.DateEntered ?? PageParameter( PageParameterKey.DateEntered ).AsDateTime();

            cbWorkersOnly.Checked = careNeed.WorkersOnly;
            cbIncludeFamily.Checked = careNeed.ChildNeeds != null && careNeed.ChildNeeds.Any();

            var paramCampusId = PageParameter( PageParameterKey.CampusId ).AsIntegerOrNull();
            if ( careNeed.Campus != null )
            {
                cpCampus.SelectedCampusId = careNeed.CampusId;
            }
            else if ( paramCampusId != null )
            {
                cpCampus.SelectedCampusId = paramCampusId;
            }
            else
            {
                cpCampus.SelectedIndex = 0;
            }

            if ( careNeed.PersonAlias != null )
            {
                ppPerson.SetValue( careNeed.PersonAlias.Person );
            }
            else
            {
                ppPerson.SetValue( null );
            }

            LoadDropDowns( careNeed );

            var paramStatus = PageParameter( PageParameterKey.Status ).AsIntegerOrNull();
            if ( careNeed.StatusValueId != null )
            {
                dvpStatus.SetValue( careNeed.StatusValueId );

                if ( careNeed.Status.Value == "Open" )
                {
                    hlStatus.Text = "Open";
                    hlStatus.LabelType = LabelType.Success;
                }

                if ( careNeed.Status.Value == "Follow Up" )
                {
                    hlStatus.Text = "Follow Up";
                    hlStatus.LabelType = LabelType.Danger;
                }

                if ( careNeed.Status.Value == "Closed" )
                {
                    hlStatus.Text = "Closed";
                    hlStatus.LabelType = LabelType.Primary;
                }
            }
            else if ( paramStatus != null )
            {
                dvpStatus.SetValue( paramStatus );
            }
            else
            {
                dvpStatus.SelectedDefinedValueId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_OPEN.AsGuid() ).Id;
            }

            var canUpdateStatus = IsUserAuthorized( SecurityActionKey.UpdateStatus );
            if ( !canUpdateStatus )
            {
                dvpStatus.Visible = false;
            }

            var paramCategory = PageParameter( PageParameterKey.Category ).AsIntegerOrNull();
            if ( careNeed.CategoryValueId != null )
            {
                dvpCategory.SetValue( careNeed.CategoryValueId );
            }
            else if ( paramCategory != null )
            {
                dvpCategory.SetValue( paramCategory );
            }

            if ( careNeed.SubmitterPersonAlias != null )
            {
                ppSubmitter.SetValue( careNeed.SubmitterPersonAlias.Person );
            }
            else if ( CurrentPersonAlias != null )
            {
                ppSubmitter.SetValue( CurrentPersonAlias.Person );
            }
            else
            {
                ppSubmitter.SetValue( null );
            }

            if ( careNeed.AssignedPersons != null )
            {
                AssignedPersons = careNeed.AssignedPersons.ToList();
                BindAssignedPersonsGrid();
                pwAssigned.Visible = UserCanAdministrate;
            }
            else
            {
                pwAssigned.Visible = false;
            }


            careNeed.LoadAttributes();
            Helper.AddEditControls( careNeed, phAttributes, true, BlockValidationGroup, 2 );

            ppPerson_SelectPerson( null, null );

            hfCareNeedId.Value = careNeed.Id.ToString();
        }

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns( CareNeed careNeed )
        {
            dvpStatus.DefinedTypeId = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_STATUS ) ).Id;
            dvpCategory.DefinedTypeId = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_CATEGORY ) ).Id;

            using ( var rockContext = new RockContext() )
            {
                var careWorkerService = new CareWorkerService( rockContext );
                var careWorkers = careWorkerService.Queryable()
                    .AsNoTracking()
                    .Where( cw => cw.IsActive && cw.PersonAlias != null && !cw.GeoFenceId.HasValue )
                    .OrderBy( cw => cw.PersonAlias.Person.LastName )
                    .ThenBy( cw => cw.PersonAlias.Person.NickName )
                    .DistinctBy( cw => cw.PersonAliasId )
                    .Select( cw => new
                    {
                        Value = cw.PersonAlias.Id + "^" + cw.Id,
                        Label = cw.PersonAlias.Person.NickName + " " + cw.PersonAlias.Person.LastName
                    } )
                    .ToList();

                bddlAddWorker.DataSource = careWorkers;
                bddlAddWorker.DataBind();
            }
            ppSubmitter.Visible = true;
        }

        private void AutoAssignWorkers( CareNeed careNeed, bool roundRobinOnly = false, bool childAssignment = false )
        {
            var rockContext = new RockContext();

            var autoAssignWorker = GetAttributeValue( AttributeKey.AutoAssignWorker ).AsBoolean();
            var autoAssignWorkerGeofence = GetAttributeValue( AttributeKey.AutoAssignWorkerGeofence ).AsBoolean();
            var loadBalanceType = GetAttributeValue( AttributeKey.LoadBalanceWorkersType );

            var careNeedService = new CareNeedService( rockContext );
            var careWorkerService = new CareWorkerService( rockContext );
            var careAssigneeService = new AssignedPersonService( rockContext );

            // reload careNeed to fully populate child properties
            careNeed = careNeedService.Get( careNeed.Guid );

            var careWorkers = careWorkerService.Queryable().AsNoTracking().Where( cw => cw.IsActive );

            var addedWorkerAliasIds = new List<int?>();

            var enableLogging = GetAttributeValue( AttributeKey.VerboseLogging ).AsBoolean();

            // auto assign Deacon/Worker by Geofence
            if ( autoAssignWorkerGeofence && !roundRobinOnly )
            {
                if ( enableLogging )
                {
                    LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, CareWorkers Count: {1}", careNeed.Guid, careWorkers.Count() ), "Geofence assignment start." );
                }
                var careWorkersWithFence = careWorkers.Where( cw => cw.GeoFenceId != null );
                foreach ( var worker in careWorkersWithFence )
                {
                    var geofenceLocation = new LocationService( rockContext ).Get( worker.GeoFenceId.Value );
                    if ( enableLogging )
                    {
                        LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, geofenceLocation: {1}", careNeed.Guid, geofenceLocation.Id ), "Care Workers with Fence" );
                    }
                    var homeLocation = careNeed.PersonAlias.Person.GetHomeLocation();
                    if ( enableLogging )
                    {
                        LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, geofenceLocation: {1}, homeLocation: {2}", careNeed.Guid, geofenceLocation.Id, ( homeLocation != null ) ? homeLocation.Id.ToString() : "null" ), "Care Workers with Fence" );
                    }
                    if ( homeLocation != null && homeLocation.GeoPoint != null )
                    {
                        var geofenceIntersect = homeLocation.GeoPoint.Intersects( geofenceLocation.GeoFence );
                        if ( enableLogging )
                        {
                            LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, geofenceIntersect: {1}, homeLocation: {2}", careNeed.Guid, geofenceIntersect, homeLocation.Id ), "geofenceIntersect" );
                        }
                        if ( geofenceIntersect )
                        {
                            var careAssignee = new AssignedPerson { Id = 0 };
                            careAssignee.CareNeed = careNeed;
                            careAssignee.PersonAliasId = worker.PersonAliasId;
                            careAssignee.WorkerId = worker.Id;
                            //careAssignee.FollowUpWorker = true;

                            careAssigneeService.Add( careAssignee );
                            addedWorkerAliasIds.Add( careAssignee.PersonAliasId );
                        }
                    }
                    else if ( homeLocation != null && homeLocation.GeoPoint == null )
                    {
                        LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Id: {0}, homeLocation: {1}", careNeed.Id, homeLocation.Id ), "Home Location does not have a valid GeoPoint. Please verify their address and manually assign their geo worker." );
                    }
                }
                if ( enableLogging )
                {
                    LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, careWorkersWithFence Count: {1} addedWorkerAliasIds Count: {2}", careNeed.Guid, careWorkersWithFence.Count(), addedWorkerAliasIds.Count() ), "Geofence assignment end." );
                }
            }

            //auto assign worker/pastor by load balance assignment
            if ( autoAssignWorker )
            {
                if ( enableLogging )
                {
                    LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, CareWorkers Count: {1}", careNeed.Guid, careWorkers.Count() ), "Auto Assign Worker start." );
                }
                var careWorkersNoFence = careWorkers.Where( cw => cw.GeoFenceId == null );
                var workerAssigned = false;
                var closedId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_CLOSED ).Id;

                // Campus, Category, Ignore Age Range and Gender
                var careWorkerCount1 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, false, true, true, true );

                if ( enableLogging )
                {
                    LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, careWorkersNoFence Count: {1}, careWorkerCount1 Count: {2}", careNeed.Guid, careWorkersNoFence.Count(), careWorkerCount1.Count() ), "careWorkerCount1, Category AND Campus" );
                }

                // Category, Ignore Age Range and Gender
                var careWorkerCount2 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, false, false, true, true );

                if ( enableLogging )
                {
                    LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, careWorkerCount2 Count: {1}", careNeed.Guid, careWorkerCount2.Count() ), "careWorkerCount2, Category NOT Campus" );
                }

                // Campus, Ignore Age Range and Gender
                var careWorkerCount3 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, false, true, false, true );

                if ( enableLogging )
                {
                    LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, careWorkerCount3 Count: {1}", careNeed.Guid, careWorkerCount3.Count() ), "careWorkerCount3, Campus NOT Category" );
                }

                // None, doesn't include parameters for other values though.
                var careWorkerCount4 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, false, false, false, true );

                if ( enableLogging )
                {
                    LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, careWorkerCount4 Count: {1}", careNeed.Guid, careWorkerCount4.Count() ), "careWorkerCount4, NOT Campus or Category" );
                }

                IOrderedQueryable<WorkerResult> careWorkersCountChild1 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild2 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild3 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild4 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild5 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild6 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild7 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild8 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild9 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild10 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild11 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild12 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild13 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild14 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild15 = null;
                IOrderedQueryable<WorkerResult> careWorkersCountChild16 = null;
                if ( childAssignment )
                {
                    // AgeRange, Gender, Campus, Category
                    careWorkersCountChild1 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, true, true, true, true );

                    // AgeRange, Gender, Category
                    careWorkersCountChild2 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, true, true, false, true );

                    // AgeRange, Gender, Campus
                    careWorkersCountChild3 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, true, true, true, false );

                    // AgeRange, Campus, Category
                    careWorkersCountChild4 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, true, false, true, true );

                    // Gender, Campus, Category
                    careWorkersCountChild5 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, true, true, true );

                    // AgeRange, Gender
                    careWorkersCountChild6 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, true, true, false, false );

                    // AgeRange, Category
                    careWorkersCountChild7 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, true, false, false, true );

                    // AgeRange, Campus
                    careWorkersCountChild8 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, true, false, true, false );

                    // Gender, Category
                    careWorkersCountChild9 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, true, false, true );

                    // Gender, Campus
                    careWorkersCountChild10 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, true, true, false );

                    // Campus, Category
                    careWorkersCountChild11 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, false, true, true );

                    // AgeRange
                    careWorkersCountChild12 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, true, false, false, false );

                    // Gender 
                    careWorkersCountChild13 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, true, false, false );

                    // Category
                    careWorkersCountChild14 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, false, false, true );

                    // Campus
                    careWorkersCountChild15 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, false, true, false );

                    // None
                    careWorkersCountChild16 = GenerateAgeQuery( careNeed, careWorkersNoFence, closedId, false, false, false, false );

                }

                var careWorkerCounts = careWorkerCount1;
                if ( loadBalanceType == "Prioritize" )
                {
                    if ( childAssignment )
                    {
                        careWorkerCounts = careWorkersCountChild1
                                            .Concat( careWorkersCountChild2 )
                                            .Concat( careWorkersCountChild3 )
                                            .Concat( careWorkersCountChild4 )
                                            .Concat( careWorkersCountChild5 )
                                            .Concat( careWorkersCountChild6 )
                                            .Concat( careWorkersCountChild7 )
                                            .Concat( careWorkersCountChild8 )
                                            .Concat( careWorkersCountChild9 )
                                            .Concat( careWorkersCountChild10 )
                                            .Concat( careWorkersCountChild11 )
                                            .Concat( careWorkersCountChild12 )
                                            .Concat( careWorkersCountChild13 )
                                            .Concat( careWorkersCountChild14 )
                                            .Concat( careWorkersCountChild15 )
                                            .Concat( careWorkersCountChild16 )
                                            .OrderBy( ct => ct.Count )
                                            .ThenByDescending( ct => ct.HasAgeRange && ct.HasGender && ct.HasCampus && ct.HasCategory )         // AgeRange, Gender, Campus, Category
                                            .ThenByDescending( ct => ct.HasAgeRange && ct.HasGender && !ct.HasCampus && ct.HasCategory )        // AgeRange, Gender, Category
                                            .ThenByDescending( ct => ct.HasAgeRange && ct.HasGender && ct.HasCampus && !ct.HasCategory )        // AgeRange, Gender, Campus
                                            .ThenByDescending( ct => ct.HasAgeRange && !ct.HasGender && ct.HasCampus && ct.HasCategory )        // AgeRange, Campus, Category
                                            .ThenByDescending( ct => !ct.HasAgeRange && ct.HasGender && ct.HasCampus && ct.HasCategory )        // Gender, Campus, Category
                                            .ThenByDescending( ct => ct.HasAgeRange && ct.HasGender && !ct.HasCampus && !ct.HasCategory )       // AgeRange, Gender
                                            .ThenByDescending( ct => ct.HasAgeRange && !ct.HasGender && !ct.HasCampus && ct.HasCategory )       // AgeRange, Category
                                            .ThenByDescending( ct => ct.HasAgeRange && !ct.HasGender && ct.HasCampus && !ct.HasCategory )       // AgeRange, Campus
                                            .ThenByDescending( ct => !ct.HasAgeRange && ct.HasGender && !ct.HasCampus && ct.HasCategory )       // Gender, Category
                                            .ThenByDescending( ct => !ct.HasAgeRange && ct.HasGender && ct.HasCampus && !ct.HasCategory )       // Gender, Campus
                                            .ThenByDescending( ct => !ct.HasAgeRange && !ct.HasGender && ct.HasCampus && ct.HasCategory )       // Campus, Category
                                            .ThenByDescending( ct => ct.HasAgeRange && !ct.HasGender && !ct.HasCampus && !ct.HasCategory )      // AgeRange
                                            .ThenByDescending( ct => !ct.HasAgeRange && ct.HasGender && !ct.HasCampus && !ct.HasCategory )      // Gender
                                            .ThenByDescending( ct => !ct.HasAgeRange && !ct.HasGender && !ct.HasCampus && ct.HasCategory )      // Category
                                            .ThenByDescending( ct => !ct.HasAgeRange && !ct.HasGender && ct.HasCampus && !ct.HasCategory )      // Campus
                                            .ThenByDescending( ct => !ct.HasAgeRange && !ct.HasGender && !ct.HasCampus && !ct.HasCategory );    // None
                    }
                    else
                    {
                        careWorkerCounts = careWorkerCount1
                                        .Concat( careWorkerCount2 )
                                        .Concat( careWorkerCount3 )
                                        .Concat( careWorkerCount4 )
                                        .OrderBy( ct => ct.Count )
                                        .ThenByDescending( ct => ct.HasCategory && ct.HasCampus )
                                        .ThenByDescending( ct => ct.HasCategory && !ct.HasCampus )
                                        .ThenByDescending( ct => ct.HasCampus && !ct.HasCategory );
                    }
                }
                else
                {
                    if ( childAssignment )
                    {
                        if ( careWorkersCountChild1.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild1;
                        }
                        else if ( careWorkersCountChild2.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild2;
                        }
                        else if ( careWorkersCountChild3.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild3;
                        }
                        else if ( careWorkersCountChild4.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild4;
                        }
                        else if ( careWorkersCountChild5.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild5;
                        }
                        else if ( careWorkersCountChild6.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild6;
                        }
                        else if ( careWorkersCountChild7.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild7;
                        }
                        else if ( careWorkersCountChild8.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild8;
                        }
                        else if ( careWorkersCountChild9.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild9;
                        }
                        else if ( careWorkersCountChild10.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild10;
                        }
                        else if ( careWorkersCountChild11.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild11;
                        }
                        else if ( careWorkersCountChild12.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild12;
                        }
                        else if ( careWorkersCountChild13.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild13;
                        }
                        else if ( careWorkersCountChild14.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild14;
                        }
                        else if ( careWorkersCountChild15.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild15;
                        }
                        else if ( careWorkersCountChild16.Any() )
                        {
                            careWorkerCounts = careWorkersCountChild16;
                        }
                    }
                    else
                    {
                        if ( careWorkerCount1.Any() )
                        {
                            careWorkerCounts = careWorkerCount1;
                        }
                        else if ( careWorkerCount2.Any() )
                        {
                            careWorkerCounts = careWorkerCount2;
                        }
                        else if ( careWorkerCount3.Any() )
                        {
                            careWorkerCounts = careWorkerCount3;
                        }
                        else if ( careWorkerCount4.Any() )
                        {
                            careWorkerCounts = careWorkerCount4;
                        }
                    }
                }

                if ( enableLogging )
                {
                    LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, careWorkerCounts Count: {1}", careNeed.Guid, careWorkerCounts.Count() ), "Combined careWorkerCounts" );
                }

                foreach ( var workerCount in careWorkerCounts )
                {
                    var worker = workerCount.Worker;
                    if ( !workerAssigned && !addedWorkerAliasIds.Contains( worker.PersonAliasId ) && worker.PersonAlias != null && careAssigneeService.GetByPersonAliasAndCareNeed( worker.PersonAlias.Id, careNeed.Id ) == null )
                    {
                        var careAssignee = new AssignedPerson { Id = 0 };
                        careAssignee.CareNeed = careNeed;
                        careAssignee.PersonAliasId = worker.PersonAliasId;
                        careAssignee.WorkerId = worker.Id;
                        careAssignee.FollowUpWorker = true;

                        careAssigneeService.Add( careAssignee );
                        addedWorkerAliasIds.Add( careAssignee.PersonAliasId );

                        workerAssigned = true;

                        if ( enableLogging )
                        {
                            LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, Worker PersonAliasId: {1}, WorkerId: {2}", careNeed.Guid, worker.PersonAliasId, worker.Id ), "Worker Assigned" );
                        }
                    }
                }
            }

            // auto assign Small Group Leader by Role
            var leaderRoleGuid = GetAttributeValue( AttributeKey.GroupTypeAndRole ).AsGuidOrNull() ?? Guid.Empty;
            var leaderRole = new GroupTypeRoleService( rockContext ).Get( leaderRoleGuid );
            if ( enableLogging )
            {
                LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, Leader Role Guid: {1}, Leader Role: {2}", careNeed.Guid, leaderRoleGuid, leaderRole.Id ), "Get Leader Role" );
            }

            if ( leaderRole != null && !roundRobinOnly )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var inGroups = groupMemberService.GetByPersonId( careNeed.PersonAlias.PersonId ).Where( gm => gm.Group != null && gm.Group.IsActive && !gm.Group.IsArchived && gm.Group.GroupTypeId == leaderRole.GroupTypeId && !gm.IsArchived && gm.GroupMemberStatus == GroupMemberStatus.Active ).Select( gm => gm.GroupId );

                if ( inGroups.Any() )
                {
                    if ( enableLogging )
                    {
                        LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, In Groups Count: {1}, leaderRole.GroupTypeId: {2}", careNeed.Guid, inGroups.Count(), leaderRole.GroupTypeId ), "In Small Groups" );
                    }
                    var groupLeaders = groupMemberService.GetByGroupRoleId( leaderRole.Id ).Where( gm => inGroups.Contains( gm.GroupId ) && !gm.IsArchived && gm.GroupMemberStatus == GroupMemberStatus.Active );
                    foreach ( var member in groupLeaders )
                    {
                        if ( !addedWorkerAliasIds.Contains( member.Person.PrimaryAliasId ) && careAssigneeService.GetByPersonAliasAndCareNeed( member.Person.PrimaryAliasId, careNeed.Id ) == null && member.PersonId != careNeed.PersonAlias.Person.Id )
                        {
                            var careAssignee = new AssignedPerson { Id = 0 };
                            careAssignee.CareNeed = careNeed;
                            careAssignee.PersonAliasId = member.Person.PrimaryAliasId;

                            careAssigneeService.Add( careAssignee );
                            addedWorkerAliasIds.Add( careAssignee.PersonAliasId );
                        }
                    }
                    if ( enableLogging )
                    {
                        LogEvent( null, "AutoAssignWorkers", string.Format( "Care Need Guid: {0}, groupLeaders Count: {1} addedWorkerAliasIds Count: {2}", careNeed.Guid, groupLeaders.Count(), addedWorkerAliasIds.Count() ), "In Small Groups, Leader Count" );
                    }

                }
            }
            rockContext.SaveChanges();
        }

        private static IOrderedQueryable<WorkerResult> GenerateAgeQuery( CareNeed careNeed, IQueryable<CareWorker> careWorkersNoFence, int closedId, bool includeAgeRange, bool includeGender, bool includeCampus, bool includeCategory, bool ignoreAgeRangeAndGender = false )
        {
            var ageAsDecimal = ( decimal? ) careNeed.PersonAlias.Person.AgePrecise;
            var tempQuery = careWorkersNoFence;

            if ( includeAgeRange )
            {
                tempQuery = tempQuery.Where( cw => ageAsDecimal.HasValue && (
                                                    ( cw.AgeRangeMin.HasValue && cw.AgeRangeMax.HasValue && ( ageAsDecimal > cw.AgeRangeMin.Value && ageAsDecimal < cw.AgeRangeMax.Value ) ) ||
                                                    ( !cw.AgeRangeMin.HasValue && cw.AgeRangeMax.HasValue && ageAsDecimal < cw.AgeRangeMax.Value ) ||
                                                    ( cw.AgeRangeMin.HasValue && !cw.AgeRangeMax.HasValue && ageAsDecimal > cw.AgeRangeMin.Value )
                                                   )
                                            );
            }
            else if ( !ignoreAgeRangeAndGender )
            {
                tempQuery = tempQuery.Where( cw => ageAsDecimal.HasValue && !(
                                                     ( cw.AgeRangeMin.HasValue && cw.AgeRangeMax.HasValue && ( ageAsDecimal > cw.AgeRangeMin.Value && ageAsDecimal < cw.AgeRangeMax.Value ) ) ||
                                                     ( !cw.AgeRangeMin.HasValue && cw.AgeRangeMax.HasValue && ageAsDecimal < cw.AgeRangeMax.Value ) ||
                                                     ( cw.AgeRangeMin.HasValue && !cw.AgeRangeMax.HasValue && ageAsDecimal > cw.AgeRangeMin.Value )
                                                    )
                                            );
            }

            if ( includeGender )
            {
                tempQuery = tempQuery.Where( cw => cw.Gender == careNeed.PersonAlias.Person.Gender );
            }
            else if ( !ignoreAgeRangeAndGender )
            {
                tempQuery = tempQuery.Where( cw => cw.Gender != careNeed.PersonAlias.Person.Gender );
            }

            if ( includeCategory )
            {
                tempQuery = tempQuery.Where( cw => cw.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) );
            }
            else
            {
                tempQuery = tempQuery.Where( cw => !cw.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) );
            }

            if ( includeCampus )
            {
                tempQuery = tempQuery.Where( cw => cw.Campuses.Contains( careNeed.CampusId.ToString() ) );
            }
            else
            {
                tempQuery = tempQuery.Where( cw => !cw.Campuses.Contains( careNeed.CampusId.ToString() ) );
            }

            return tempQuery.Select( cw => new WorkerResult
                                        {
                                            Count = cw.AssignedPersons.Where( ap => ap.CareNeed != null && ap.CareNeed.StatusValueId != closedId ).Count(),
                                            Worker = cw,
                                            HasCategory = includeCategory,
                                            HasCampus = includeCampus,
                                            HasAgeRange = includeAgeRange,
                                            HasGender = includeGender
                                        }
                                    )
                                    .OrderBy( cw => cw.Count )
                                    .ThenBy( cw => cw.Worker.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) )
                                    .ThenBy( cw => cw.Worker.Campuses.Contains( careNeed.CampusId.ToString() ) );
        }

        /// <summary>
        /// Binds the Assigned Persons grid.
        /// </summary>
        private void BindAssignedPersonsGrid()
        {
            var reloadList = AssignedPersons
                .Select( ap => new AssignedPerson
                {
                    Id = ap.Id,
                    PersonAlias = new PersonAliasService( new RockContext() ).Get( ap.PersonAliasId.Value ),
                    PersonAliasId = ap.PersonAliasId,
                    NeedId = ap.NeedId,
                    FollowUpWorker = ap.FollowUpWorker,
                    WorkerId = ap.WorkerId
                } )
                .OrderBy( ap => ap.PersonAlias.Person.LastName )
                .ThenBy( ap => ap.PersonAlias.Person.NickName );
            // Bind the list items to the grid.
            gAssignedPersons.DataSource = reloadList;
            gAssignedPersons.DataBind();

            btnDeleteSelectedAssignedPersons.Visible = AssignedPersons.Any();
        }

        private PageReference GetParentPage()
        {
            var pageCache = PageCache.Get( RockPage.PageId );
            if ( pageCache != null )
            {
                var parentPage = pageCache.ParentPage;
                if ( parentPage != null )
                {
                    return new PageReference( parentPage.Guid.ToString() );
                }
            }
            return new PageReference( pageCache.Guid.ToString() );
        }

        private ServiceLog LogEvent( RockContext rockContext, string type, string input, string result )
        {
            if ( rockContext == null )
            {
                rockContext = new RockContext();
            }

            var rockLogger = new ServiceLogService( rockContext );
            ServiceLog serviceLog = new ServiceLog
            {
                Name = "Steps To Care",
                Type = type,
                LogDateTime = RockDateTime.Now,
                Input = input,
                Result = result,
                Success = true
            };
            rockLogger.Add( serviceLog );
            rockContext.SaveChanges();
            return serviceLog;
        }


        #endregion
    }

    public partial class WorkerResult
    {
        public int Count { get; set; }
        public CareWorker Worker { get; set; }
        public bool HasCategory { get; set; } = false;
        public bool HasCampus { get; set; } = false;
        public bool HasAgeRange { get; set; } = false;
        public bool HasGender { get; set; } = false;
    }
}
