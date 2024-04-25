// <copyright>
// Copyright 2024 by Kingdom First Solutions
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
using System.Web.UI;
using System.Web.UI.WebControls;
using NuGet;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using rocks.kfs.StepsToCare;
using rocks.kfs.StepsToCare.Model;

namespace RockWeb.Plugins.rocks_kfs.StepsToCare
{
    #region Block Attributes

    [DisplayName( "Care Entry" )]
    [Category( "KFS > Steps To Care" )]
    [Description( "Care entry block for KFS Steps to Care package. Used for adding and editing care needs " )]

    #endregion Block Attributes

    #region Block Settings

    [BooleanField( "Allow New Person Entry",
        Description = "Should you be able to enter a new person from the care entry form and use person matching?",
        DefaultBooleanValue = false,
        Key = AttributeKey.AllowNewPerson )]

    [CustomEnhancedListField( "Group Type Roles",
        Description = "Select the Group Type > Roles for the group members you would like auto assigned to Care Needs created for people who are in groups of these types. If none are selected it will not auto assign the group member with the appropriate role to the need. ",
        IsRequired = false,
        ListSource = "SELECT gtr.[Guid] as [Value], CONCAT(gt.[Name],' > ',gtr.[Name]) as [Text] FROM GroupTypeRole gtr JOIN GroupType gt ON gtr.GroupTypeId = gt.Id ORDER BY gt.[Name], gtr.[Order]",
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

    [IntegerField( "Threshold of Days before Assignment",
        Description = "The number of days before a scheduled need is assigned to workers. Default: 3",
        IsRequired = true,
        DefaultIntegerValue = 3,
        Key = AttributeKey.FutureThresholdDays )]

    [BooleanField( "Preview Assigned People",
        Description = "Should you see a preview of who is going to be assigned before the care entry is saved? This will add a bit of processing up front compared to on save. Workers may be duplicated if multiple people are entering care needs at the same time.",
        DefaultBooleanValue = false,
        Key = AttributeKey.PreviewAssignedPeople )]

    [BooleanField( "Complete Child Needs on Parent Completion",
        DefaultBooleanValue = true,
        Key = AttributeKey.CompleteChildNeeds,
        Category = AttributeCategory.FamilyNeeds )]

    [BooleanField( "Snooze Child Needs when Parent Need is Snoozed",
        DefaultBooleanValue = true,
        Key = AttributeKey.SnoozeChildNeeds,
        Category = AttributeCategory.FamilyNeeds )]

    [TextField( "Snooze Button Text",
        Description = "Customize the button text to use for moving a need from 'Follow Up' to 'Snoozed' status.",
        DefaultValue = "Snooze",
        Key = AttributeKey.SnoozedButtonText )]

    [TextField( "Complete Button Text",
        Description = "Text to use on button to move from any status to 'Closed' status quickly.",
        DefaultValue = "Complete Need",
        Key = AttributeKey.CompleteButtonText )]

    [BooleanField( "Enable Custom Follow Up",
        Description = "Enable the ability to set Custom Follow Up settings per need.",
        DefaultBooleanValue = false,
        Key = AttributeKey.EnableCustomFollowUp )]

    [SecurityAction(
        SecurityActionKey.UpdateStatus,
        "The roles and/or users that have access to update the status of Care Needs." )]

    [SecurityAction(
        SecurityActionKey.CompleteNeeds,
        "The roles and/or users that have access to Complete Needs." )]
    #endregion Block Settings
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
            public const string FutureThresholdDays = "FutureThresholdDays";
            public const string PreviewAssignedPeople = "PreviewAssignedPeople";
            public const string CompleteChildNeeds = "CompleteChildNeeds";
            public const string SnoozeChildNeeds = "SnoozeChildNeeds";
            public const string SnoozedButtonText = "SnoozedButtonText";
            public const string CompleteButtonText = "CompleteButtonText";
            public const string EnableCustomFollowUp = "EnableCustomFollowUp";
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
            public const string CompleteNeeds = "CompleteNeeds";
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

        #endregion Properties

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

            gAssignedPersons.DataKeyNames = new string[] { "PersonAliasId" };
            gAssignedPersons.GridRebind += gAssignedPersons_GridRebind;
            gAssignedPersons.Actions.ShowAdd = false;
            gAssignedPersons.ShowActionRow = false;

            _allowNewPerson = GetAttributeValue( AttributeKey.AllowNewPerson ).AsBoolean();
            btnComplete.Text = btnCompleteFtr.Text = GetAttributeValue( AttributeKey.CompleteButtonText );
            btnSnooze.Text = btnSnoozeFtr.Text = GetAttributeValue( AttributeKey.SnoozedButtonText );
            cbCustomFollowUp.Visible = GetAttributeValue( AttributeKey.EnableCustomFollowUp ).AsBoolean();
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

        #endregion Base Control Methods

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

                var previewAssignedPeople = GetAttributeValue( AttributeKey.PreviewAssignedPeople ).AsBoolean();
                var snoozedValueId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_SNOOZED.AsGuid() ).Id;
                var futureThresholdDays = GetAttributeValue( AttributeKey.FutureThresholdDays ).AsDouble();

                double dateDifference = 0;

                CareNeed careNeed = null;
                int careNeedId = hfCareNeedId.ValueAsInt();
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

                if ( careNeed.StatusValueId != dvpStatus.SelectedDefinedValueId && dvpStatus.SelectedDefinedValueId == snoozedValueId )
                {
                    careNeed.SnoozeDate = RockDateTime.Now;
                }

                careNeed.StatusValueId = dvpStatus.SelectedDefinedValueId;

                careNeed.CategoryValueId = dvpCategory.SelectedValue.AsIntegerOrNull();

                if ( dpDate.SelectedDateTime.HasValue )
                {
                    careNeed.DateEntered = dpDate.SelectedDateTime.Value;
                }

                if ( careNeed.DateEntered.HasValue )
                {
                    dateDifference = ( careNeed.DateEntered.Value - DateTime.Now ).TotalDays;
                }

                careNeed.WorkersOnly = cbWorkersOnly.Checked;

                careNeed.CustomFollowUp = cbCustomFollowUp.Checked;
                careNeed.RenewPeriodDays = numbRepeatDays.IntegerValue;
                careNeed.RenewMaxCount = numbRepeatTimes.IntegerValue;

                var enableLogging = GetAttributeValue( AttributeKey.VerboseLogging ).AsBoolean();
                var newlyAssignedPersons = new List<AssignedPerson>();
                if ( careNeed.AssignedPersons != null || ( previewAssignedPeople && dateDifference <= futureThresholdDays ) )
                {
                    if ( AssignedPersons.Any() )
                    {
                        if ( enableLogging )
                        {
                            CareUtilities.LogEvent( null, "UpdateAssignedPersons", string.Format( "Care Need Guid: {0}, AssignedPersons Count: {1} careNeed.AssignedPersons Count: {2}", careNeed.Guid, AssignedPersons.Count(), careNeed.AssignedPersons?.Count() ), "Assigned Persons Edit Start" );
                        }

                        var assignedPersonsLookup = AssignedPersons;

                        foreach ( var existingAssigned in assignedPersonsLookup )
                        {
                            if ( careNeed.AssignedPersons == null || !careNeed.AssignedPersons.Any( ap => ap.PersonAliasId == existingAssigned.PersonAliasId ) )
                            {
                                if ( careNeed.AssignedPersons == null )
                                {
                                    careNeed.AssignedPersons = new List<AssignedPerson>();
                                }
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
                                    WorkerId = existingAssigned.WorkerId,
                                    Type = existingAssigned.Type,
                                    TypeQualifier = existingAssigned.TypeQualifier,
                                    CareNeed = careNeed
                                };
                                careNeed.AssignedPersons.Add( assignedPerson );
                                newlyAssignedPersons.Add( assignedPerson );
                            }
                        }
                        var removePersons = careNeed.AssignedPersons.Where( ap => !assignedPersonsLookup.Select( apl => apl.Id ).ToList().Contains( ap.Id ) ).ToList();
                        assignedPersonService.DeleteRange( removePersons );
                        careNeed.AssignedPersons.RemoveAll( removePersons );

                        if ( enableLogging )
                        {
                            CareUtilities.LogEvent( null, "UpdateAssignedPersons", string.Format( "Care Need Guid: {0}, AssignedPersons Count: {1} careNeed.AssignedPersons Count: {2} removePersons Count {3} Edited By AliasId: {4}", careNeed.Guid, AssignedPersons.Count(), careNeed.AssignedPersons.Count(), removePersons.Count(), CurrentPersonAlias.Id ), "Assigned Persons Edit End" );
                        }
                    }
                    else if ( careNeed.AssignedPersons != null && careNeed.AssignedPersons.Any() )
                    {
                        assignedPersonService.DeleteRange( careNeed.AssignedPersons );
                        careNeed.AssignedPersons.Clear();
                        if ( enableLogging )
                        {
                            CareUtilities.LogEvent( null, "UpdateAssignedPersons", string.Format( "Care Need Guid: {0}, AssignedPersons Count: {1} careNeed.AssignedPersons Count: {2}", careNeed.Guid, AssignedPersons.Count(), careNeed.AssignedPersons.Count() ), "Assigned Persons Edit Else If" );
                        }
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

                    var autoAssignWorker = GetAttributeValue( AttributeKey.AutoAssignWorker ).AsBoolean();
                    var autoAssignWorkerGeofence = GetAttributeValue( AttributeKey.AutoAssignWorkerGeofence ).AsBoolean();
                    var loadBalanceType = GetAttributeValue( AttributeKey.LoadBalanceWorkersType );
                    var leaderRoleGuids = GetAttributeValues( AttributeKey.GroupTypeAndRole ).AsGuidList();

                    if ( isNew && dateDifference <= futureThresholdDays && !previewAssignedPeople )
                    {
                        CareUtilities.AutoAssignWorkers( careNeed, careNeed.WorkersOnly, autoAssignWorker: autoAssignWorker, autoAssignWorkerGeofence: autoAssignWorkerGeofence, loadBalanceType: loadBalanceType, enableLogging: enableLogging, leaderRoleGuids: leaderRoleGuids );
                    }

                    if ( childNeedsCreated && careNeed.ChildNeeds != null && careNeed.ChildNeeds.Any() && dateDifference <= futureThresholdDays )
                    {
                        var familyGroupType = GroupTypeCache.GetFamilyGroupType();
                        var adultRoleId = familyGroupType.Roles.FirstOrDefault( a => a.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).Id;
                        foreach ( var need in careNeed.ChildNeeds )
                        {
                            if ( need.PersonAlias != null && need.PersonAlias.Person.GetFamilyRole().Id != adultRoleId )
                            {
                                CareUtilities.AutoAssignWorkers( need, true, true, autoAssignWorker: autoAssignWorker, autoAssignWorkerGeofence: autoAssignWorkerGeofence, loadBalanceType: loadBalanceType, enableLogging: enableLogging, leaderRoleGuids: leaderRoleGuids );
                            }
                            else
                            {
                                var adultFamilyWorkers = GetAttributeValue( AttributeKey.AdultFamilyWorkers );
                                CareUtilities.AutoAssignWorkers( need, adultFamilyWorkers == "Workers Only" || careNeed.WorkersOnly, autoAssignWorker: autoAssignWorker, autoAssignWorkerGeofence: autoAssignWorkerGeofence, loadBalanceType: loadBalanceType, enableLogging: enableLogging, leaderRoleGuids: leaderRoleGuids );
                            }
                        }
                    }

                    if ( dateDifference <= futureThresholdDays )
                    {
                        // if a NewAssignmentNotificationEmailTemplate is configured, send an email
                        var assignmentEmailTemplateGuid = GetAttributeValue( AttributeKey.NewAssignmentNotification ).AsGuidOrNull();

                        CareUtilities.SendWorkerNotification( rockContext, careNeed, isNew, newlyAssignedPersons, assignmentEmailTemplateGuid, RockPage );
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

                    int requestId = hfCareNeedId.ValueAsInt();

                    if ( !cpCampus.SelectedCampusId.HasValue && ( e != null || requestId == 0 ) )
                    {
                        var personCampus = person.GetCampus();
                        cpCampus.SelectedCampusId = personCampus != null ? personCampus.Id : ( int? ) null;
                    }

                    PreviewAssignedPeople( person );
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
                AssignedPersons.RemoveAll( a => selectedIds.Contains( a.PersonAliasId.Value ) );
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
            var remove = this.AssignedPersons.FirstOrDefault( ap => ap.PersonAliasId == e.RowKeyId );
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
                        NeedId = hfCareNeedId.Value.AsInteger(),
                        Type = AssignedType.Manual
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
                    WorkerId = selectedVal[1].AsIntegerOrNull(),
                    Type = AssignedType.Worker
                };
                AssignedPersons.Add( addPerson );
                BindAssignedPersonsGrid();
            }

            bddlAddWorker.ClearSelection();
        }

        protected void dvpCategory_SelectedIndexChanged( object sender, EventArgs e )
        {
            PreviewAssignedPeople( null );
            dtbDetailsText.Focus();
        }

        protected void gAssignedPersons_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var phCountOrRole = e.Row.ControlsOfTypeRecursive<PlaceHolder>().FirstOrDefault();
            var assignedPerson = e.Row.DataItem as AssignedPerson;

            if ( phCountOrRole == null || assignedPerson == null )
            {
                return;
            }
            var returnStr = assignedPerson.Type.ToString();
            if ( assignedPerson.TypeQualifier.IsNotNullOrWhiteSpace() )
            {
                var typeQualifierArray = assignedPerson.TypeQualifier.Split( '^' );
                if ( assignedPerson.Type == AssignedType.Worker )
                {
                    //Format is [0]Count^[1]HasAgeRange^[2]HasCampus^[3]HasCategory^[4]HasGender
                    returnStr = $"Worker ({typeQualifierArray[0]})";
                }
                else if ( assignedPerson.Type == AssignedType.GroupRole && typeQualifierArray.Length > 2 )
                {
                    //Format is [0]GroupRoleId^[1]GroupTypeId^[2]Group Type > Group Role
                    returnStr = typeQualifierArray[2];
                }
            }
            phCountOrRole.Controls.Add( new LiteralControl( returnStr ) );

        }

        protected void cbCustomFollowUp_CheckedChanged( object sender, EventArgs e )
        {
            pnlRecurrenceOptions.Visible = cbCustomFollowUp.Checked;
            BindAssignedPersonsGrid();
        }

        protected void dvpStatus_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindAssignedPersonsGrid();
        }

        protected void cpCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            PreviewAssignedPeople( null );
        }

        protected void btnSnooze_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            CareNeedService careNeedService = new CareNeedService( rockContext );

            CareNeed careNeed = null;
            int careNeedId = hfCareNeedId.ValueAsInt();

            if ( !careNeedId.Equals( 0 ) )
            {
                careNeed = careNeedService.Get( careNeedId );
            }

            if ( careNeed != null )
            {
                var snoozeValueId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_SNOOZED.AsGuid() ).Id;
                careNeed.StatusValueId = snoozeValueId;
                careNeed.SnoozeDate = RockDateTime.Now;

                var snoozeChildNeeds = GetAttributeValue( AttributeKey.SnoozeChildNeeds ).AsBoolean();

                if ( snoozeChildNeeds && careNeed.ChildNeeds.Any() )
                {
                    foreach ( var childneed in careNeed.ChildNeeds )
                    {
                        childneed.StatusValueId = snoozeValueId;
                        childneed.SnoozeDate = RockDateTime.Now;
                    }
                }

                rockContext.WrapTransaction( () =>
                {
                    rockContext.SaveChanges();
                } );

                createNote( rockContext, careNeedId, GetAttributeValue( AttributeKey.SnoozedButtonText ) );

                if ( snoozeChildNeeds && careNeed.ChildNeeds.Any() )
                {
                    foreach ( var childneed in careNeed.ChildNeeds )
                    {
                        createNote( rockContext, childneed.Id, $"Marked \"{GetAttributeValue( AttributeKey.SnoozedButtonText )}\" from Parent Need" );
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

        }
        protected void btnComplete_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            CareNeedService careNeedService = new CareNeedService( rockContext );

            CareNeed careNeed = null;
            int careNeedId = hfCareNeedId.ValueAsInt();

            if ( !careNeedId.Equals( 0 ) )
            {
                careNeed = careNeedService.Get( careNeedId );
            }

            if ( careNeed != null )
            {
                var completeChildNeeds = GetAttributeValue( AttributeKey.CompleteChildNeeds ).AsBoolean();
                var completeValueId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_CLOSED ).Id;
                careNeed.StatusValueId = completeValueId;

                if ( completeChildNeeds && careNeed.ChildNeeds.Any() )
                {
                    foreach ( var childneed in careNeed.ChildNeeds )
                    {
                        childneed.StatusValueId = completeValueId;
                    }
                }

                rockContext.WrapTransaction( () =>
                {
                    rockContext.SaveChanges();
                } );

                createNote( rockContext, careNeedId, GetAttributeValue( AttributeKey.CompleteButtonText ) );

                if ( completeChildNeeds && careNeed.ChildNeeds.Any() )
                {
                    foreach ( var childneed in careNeed.ChildNeeds )
                    {
                        createNote( rockContext, childneed.Id, $"Marked \"{GetAttributeValue( AttributeKey.CompleteButtonText )}\" from Parent Need" );
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
        }

        #endregion Events

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
            cbCustomFollowUp.Checked = careNeed.CustomFollowUp;
            pnlRecurrenceOptions.Visible = cbCustomFollowUp.Checked;
            numbRepeatDays.IntegerValue = careNeed.RenewPeriodDays;
            numbRepeatTimes.IntegerValue = careNeed.RenewMaxCount;

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

                updateStatusLabel( careNeed );
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
                BindAssignedPersonsGrid( true );
                pwAssigned.Visible = UserCanAdministrate;
            }
            else
            {
                pwAssigned.Visible = false;
            }

            careNeed.LoadAttributes();
            Helper.AddEditControls( careNeed, phAttributes, true, BlockValidationGroup, 2 );

            hfCareNeedId.Value = careNeed.Id.ToString();

            ppPerson_SelectPerson( null, null );
        }

        private void updateStatusLabel( CareNeed careNeed )
        {
            careNeed.Status.LoadAttributes();

            hlStatus.Text = careNeed.Status.Value;
            hlStatus.LabelType = LabelType.Custom;
            hlStatus.CustomClass = careNeed.Status.GetAttributeValue( "CssClass" );

            if ( careNeed.CustomFollowUp && ( !careNeed.RenewMaxCount.HasValue || careNeed.RenewCurrentCount <= careNeed.RenewMaxCount.Value ) && ( careNeed.Status.Guid == rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_OPEN.AsGuid() || careNeed.Status.Guid == rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_FOLLOWUP.AsGuid() ) )
            {
                btnSnooze.Visible = btnSnoozeFtr.Visible = true;
            }
            else
            {
                btnSnooze.Visible = btnSnoozeFtr.Visible = false;
            }

            if ( careNeed.Status.Guid != rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_CLOSED.AsGuid() && ( IsUserAuthorized( SecurityActionKey.UpdateStatus ) || IsUserAuthorized( SecurityActionKey.CompleteNeeds ) ) )
            {
                btnComplete.Visible = btnCompleteFtr.Visible = true;
            }
            else
            {
                btnComplete.Visible = btnCompleteFtr.Visible = false;
            }
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

        /// <summary>
        /// Binds the Assigned Persons grid.
        /// </summary>
        private void BindAssignedPersonsGrid( bool initialLoad = false )
        {
            if ( !initialLoad )
            {
                GenerateTempNeed( null );
            }

            var reloadList = AssignedPersons
                .Select( ap => new AssignedPerson
                {
                    Id = ap.Id,
                    PersonAlias = new PersonAliasService( new RockContext() ).Get( ap.PersonAliasId.Value ),
                    PersonAliasId = ap.PersonAliasId,
                    NeedId = ap.NeedId,
                    FollowUpWorker = ap.FollowUpWorker,
                    WorkerId = ap.WorkerId,
                    Type = ap.Type,
                    TypeQualifier = ap.TypeQualifier
                } )
                .OrderBy( ap => ap.PersonAlias.Person.LastName )
                .ThenBy( ap => ap.PersonAlias.Person.NickName );
            // Bind the list items to the grid.
            gAssignedPersons.DataSource = reloadList;
            gAssignedPersons.DataBind();

            btnDeleteSelectedAssignedPersons.Visible = AssignedPersons.Any();
        }

        private void PreviewAssignedPeople( Person person )
        {
            var previewAssignedPeople = GetAttributeValue( AttributeKey.PreviewAssignedPeople ).AsBoolean();
            var categoryId = dvpCategory.SelectedValue.AsIntegerOrNull();
            var needId = hfCareNeedId.ValueAsInt();
            var futureThresholdDays = GetAttributeValue( AttributeKey.FutureThresholdDays ).AsDouble();
            double dateDifference = 0;
            if ( dpDate.SelectedDateTime.HasValue )
            {
                dateDifference = ( dpDate.SelectedDateTime.Value - RockDateTime.Now ).TotalDays;
            }

            if ( person == null && ppPerson.PersonId != null )
            {
                person = new PersonService( new RockContext() ).Get( ppPerson.PersonId.Value );
            }
            else if ( _allowNewPerson )
            {
                var personService = new PersonService( new RockContext() );
                person = personService.FindPerson( new PersonService.PersonMatchQuery( dtbFirstName.Text, dtbLastName.Text, ebEmail.Text, pnbCellPhone.Number ), false, true, false );

                if ( person == null && dtbFirstName.Text.IsNotNullOrWhiteSpace() && dtbLastName.Text.IsNotNullOrWhiteSpace() && ( !string.IsNullOrWhiteSpace( ebEmail.Text ) || !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbHomePhone.Number ) ) || !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbCellPhone.Number ) ) ) )
                {
                    var personRecordTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    var personStatusPending = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

                    var randomId = new Random().Next();
                    randomId *= -1;

                    person = new Person();
                    person.Id = randomId;
                    person.IsSystem = false;
                    person.RecordTypeValueId = personRecordTypeId;
                    person.RecordStatusValueId = personStatusPending;
                    person.FirstName = dtbFirstName.Text;
                    person.LastName = dtbLastName.Text;
                    person.Gender = Gender.Unknown;

                    var personAlias = new PersonAlias();
                    personAlias.Person = person;
                    personAlias.PersonId = randomId;
                    personAlias.AliasPersonId = randomId;
                    person.Aliases.Add( personAlias );
                }
            }
            if ( categoryId.HasValue )
            {
                var category = DefinedValueCache.Get( categoryId.Value );
                var categoryFollowUpAfter = category.GetAttributeValue( "FollowUpAfter" ).AsIntegerOrNull();
                var categoryTimesToRepeat = category.GetAttributeValue( "TimesToRepeat" ).AsIntegerOrNull();
                if ( categoryFollowUpAfter.HasValue && categoryFollowUpAfter > 0 )
                {
                    cbCustomFollowUp.Checked = true;
                    numbRepeatDays.IntegerValue = categoryFollowUpAfter;
                    numbRepeatTimes.IntegerValue = categoryTimesToRepeat;
                    pnlRecurrenceOptions.Visible = true;
                }
            }
            if ( previewAssignedPeople && categoryId.HasValue && person != null && needId == 0 && dateDifference <= futureThresholdDays )
            {
                var autoAssignWorker = GetAttributeValue( AttributeKey.AutoAssignWorker ).AsBoolean();
                var autoAssignWorkerGeofence = GetAttributeValue( AttributeKey.AutoAssignWorkerGeofence ).AsBoolean();
                var loadBalanceType = GetAttributeValue( AttributeKey.LoadBalanceWorkersType );
                var enableLogging = GetAttributeValue( AttributeKey.VerboseLogging ).AsBoolean();
                var leaderRoleGuids = GetAttributeValues( AttributeKey.GroupTypeAndRole ).AsGuidList();

                CareNeed careNeed = GenerateTempNeed( person, categoryId );

                AssignedPersons = CareUtilities.AutoAssignWorkers( careNeed, cbWorkersOnly.Checked, autoAssignWorker: autoAssignWorker, autoAssignWorkerGeofence: autoAssignWorkerGeofence, loadBalanceType: loadBalanceType, enableLogging: enableLogging, leaderRoleGuids: leaderRoleGuids, previewAssigned: previewAssignedPeople );
                pwAssigned.Visible = UserCanAdministrate;
                BindAssignedPersonsGrid();
            }
            else if ( needId == 0 )
            {
                GenerateTempNeed( null, categoryId );

                pwAssigned.Visible = false;
                AssignedPersons = null;
            }
            else
            {
                GenerateTempNeed( null, categoryId );
            }
        }

        private CareNeed GenerateTempNeed( Person person, int? categoryId = null )
        {
            if ( categoryId == null )
            {
                categoryId = dvpCategory.SelectedValue.AsIntegerOrNull();
            }
            var careNeed = new CareNeed { Id = 0 };
            careNeed.CampusId = cpCampus.SelectedCampusId;
            careNeed.PersonAlias = person?.PrimaryAlias;
            careNeed.PersonAliasId = person?.PrimaryAliasId;
            careNeed.StatusValueId = dvpStatus.SelectedValue.AsIntegerOrNull();
            careNeed.CategoryValueId = categoryId;

            careNeed.LoadAttributes();

            phAttributes.Controls.Clear();
            Helper.AddEditControls( careNeed, phAttributes, false, BlockValidationGroup, 2 );

            return careNeed;
        }

        private bool createNote( RockContext rockContext, int entityId, string noteText, bool countsForTouch = false )
        {
            var careNeedNoteTypes = NoteTypeCache.GetByEntity( EntityTypeCache.Get( typeof( CareNeed ) ).Id, "", "", true );

            var noteService = new NoteService( rockContext );
            var noteType = careNeedNoteTypes.FirstOrDefault();
            var retVal = false;

            if ( noteType != null )
            {
                var note = new Note
                {
                    Id = 0,
                    IsSystem = false,
                    IsAlert = false,
                    NoteTypeId = noteType.Id,
                    EntityId = entityId,
                    Text = noteText,
                    EditedByPersonAliasId = CurrentPersonAliasId,
                    EditedDateTime = RockDateTime.Now,
                    NoteUrl = this.RockBlock()?.CurrentPageReference?.BuildUrl(),
                    Caption = !countsForTouch ? "Action" : string.Empty
                };
                if ( noteType.RequiresApprovals )
                {
                    if ( note.IsAuthorized( Authorization.APPROVE, CurrentPerson ) )
                    {
                        note.ApprovalStatus = NoteApprovalStatus.Approved;
                        note.ApprovedByPersonAliasId = CurrentPersonAliasId;
                        note.ApprovedDateTime = RockDateTime.Now;
                    }
                    else
                    {
                        note.ApprovalStatus = NoteApprovalStatus.PendingApproval;
                    }
                }
                else
                {
                    note.ApprovalStatus = NoteApprovalStatus.Approved;
                }

                if ( note.IsValid )
                {
                    noteService.Add( note );

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        note.SaveAttributeValues( rockContext );
                    } );

                    retVal = true;
                }
            }
            return retVal;
        }

        #endregion Methods
    }
}