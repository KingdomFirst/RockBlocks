// <copyright>
// Copyright 2021 by Kingdom First Solutions
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
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
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
        }
        private static class PageParameterKey
        {
            public const string PersonId = "PersonId";
            public const string DateEntered = "DateEntered";
            public const string Status = "Status";
            public const string CampusId = "CampusId";
            public const string Category = "Category";
            public const string Details = "Details";
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

            _allowNewPerson = GetAttributeValue( "AllowNewPerson" ).AsBoolean();
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
                ShowDetail( PageParameter( "CareNeedId" ).AsInteger() );
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
            ShowDetail( PageParameter( "CareNeedId" ).AsInteger() );
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
                int careNeedId = PageParameter( "CareNeedId" ).AsInteger();
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

                if ( _allowNewPerson && ppPerson.PersonId == null )
                {
                    var personService = new PersonService( new RockContext() );
                    var person = personService.FindPerson( new PersonService.PersonMatchQuery( dtbFirstName.Text, dtbLastName.Text, ebEmail.Text, pnbCellPhone.Number ), false, true, false );

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

                        foreach ( var alias in assignedPersonsLookup )
                        {
                            if ( !careNeed.AssignedPersons.Any( ap => ap.PersonAliasId == alias.PersonAliasId ) )
                            {
                                var personAlias = alias.PersonAlias;
                                if ( personAlias == null )
                                {
                                    personAlias = new PersonAliasService( rockContext ).Get( alias.PersonAliasId.Value );
                                }
                                var assignedPerson = new AssignedPerson
                                {
                                    PersonAlias = personAlias,
                                    PersonAliasId = personAlias.Id,
                                    FollowUpWorker = alias.FollowUpWorker,
                                    WorkerId = alias.WorkerId
                                };
                                careNeed.AssignedPersons.Add( assignedPerson );
                                newlyAssignedPersons.Add( assignedPerson );
                            }
                        }
                        var removePersons = careNeed.AssignedPersons.Where( ap => !assignedPersonsLookup.Select( apl => apl.PersonAliasId ).ToList().Contains( ap.PersonAliasId.Value ) ).ToList();
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
                    if ( careNeed.Id.Equals( 0 ) )
                    {
                        careNeedService.Add( careNeed );
                    }

                    // get attributes
                    careNeed.LoadAttributes();
                    Helper.GetEditValues( phAttributes, careNeed );

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        careNeed.SaveAttributeValues( rockContext );
                    } );

                    if ( isNew )
                    {
                        AutoAssignWorkers( careNeed );
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
                    }
                    if ( assignedPersons != null && assignedPersons.Any() && assignmentEmailTemplateGuid.HasValue )
                    {
                        Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                        linkedPages.Add( "CareDetail", CurrentPageReference.BuildUrl() );
                        linkedPages.Add( "CareDashboard", GetParentPage().BuildUrl() );

                        var emailMessage = new RockEmailMessage( assignmentEmailTemplateGuid.Value );
                        emailMessage.AppRoot = ResolveRockUrl( "~/" );
                        emailMessage.ThemeRoot = ResolveRockUrl( "~~/" );
                        foreach ( var assignee in assignedPersons )
                        {
                            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                            mergeFields.Add( "CareNeed", careNeed );
                            mergeFields.Add( "LinkedPages", linkedPages );
                            mergeFields.Add( "AssignedPerson", assignee );
                            mergeFields.Add( "Person", assignee.PersonAlias.Person );

                            emailMessage.AddRecipient( new RockEmailMessageRecipient( assignee.PersonAlias.Person, mergeFields ) );
                        }
                        emailMessage.Send();
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

            pnlNewPersonFields.Visible = _allowNewPerson;
            ppPerson.Required = !_allowNewPerson;

            dtbDetailsText.Text = ( careNeed.Details.IsNotNullOrWhiteSpace() ) ? careNeed.Details : PageParameter( PageParameterKey.Details ).ToString();
            dpDate.SelectedDateTime = careNeed.DateEntered ?? PageParameter( PageParameterKey.DateEntered ).AsDateTime();

            cbWorkersOnly.Checked = careNeed.WorkersOnly;

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

        private void AutoAssignWorkers( CareNeed careNeed )
        {
            var rockContext = new RockContext();

            var autoAssignWorker = GetAttributeValue( AttributeKey.AutoAssignWorker ).AsBoolean();
            var autoAssignWorkerGeofence = GetAttributeValue( AttributeKey.AutoAssignWorkerGeofence ).AsBoolean();

            var careNeedService = new CareNeedService( rockContext );
            var careWorkerService = new CareWorkerService( rockContext );
            var careAssigneeService = new AssignedPersonService( rockContext );

            // reload careNeed to fully populate child properties
            careNeed = careNeedService.Get( careNeed.Guid );

            var careWorkers = careWorkerService.Queryable().AsNoTracking().Where( cw => cw.IsActive );

            var addedWorkerAliasIds = new List<int?>();

            // auto assign Deacon/Worker by Geofence
            if ( autoAssignWorkerGeofence )
            {
                var careWorkersWithFence = careWorkers.Where( cw => cw.GeoFenceId != null );
                foreach ( var worker in careWorkersWithFence )
                {
                    var geofenceLocation = new LocationService( rockContext ).Get( worker.GeoFenceId.Value );
                    var homeLocation = careNeed.PersonAlias.Person.GetHomeLocation();
                    if ( homeLocation != null )
                    {
                        var geofenceIntersect = homeLocation.GeoPoint.Intersects( geofenceLocation.GeoFence );
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
                }
            }

            //auto assign worker/pastor by load balance assignment
            if ( autoAssignWorker )
            {
                var careWorkersNoFence = careWorkers.Where( cw => cw.GeoFenceId == null );
                var workerAssigned = false;
                var closedId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_CLOSED ).Id;
                var careWorkerCount1 = careWorkersNoFence
                    .Where( cw => cw.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) && cw.Campuses.Contains( careNeed.CampusId.ToString() ) )
                    .Select( cw => new
                    {
                        Count = cw.AssignedPersons.Where( ap => ap.CareNeed != null && ap.CareNeed.StatusValueId != closedId ).Count(),
                        Worker = cw,
                        HasCategoryAndCampus = true,
                        HasCategory = false,
                        HasCampus = false
                    }
                    )
                    .OrderBy( cw => cw.Count )
                    .ThenBy( cw => cw.Worker.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) )
                    .ThenBy( cw => cw.Worker.Campuses.Contains( careNeed.CampusId.ToString() ) );

                var careWorkerCount2 = careWorkersNoFence
                    .Where( cw => cw.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) && !cw.Campuses.Contains( careNeed.CampusId.ToString() ) )
                    .Select( cw => new
                    {
                        Count = cw.AssignedPersons.Where( ap => ap.CareNeed != null && ap.CareNeed.StatusValueId != closedId ).Count(),
                        Worker = cw,
                        HasCategoryAndCampus = false,
                        HasCategory = true,
                        HasCampus = false
                    }
                    )
                    .OrderBy( cw => cw.Count )
                    .ThenBy( cw => cw.Worker.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) )
                    .ThenBy( cw => cw.Worker.Campuses.Contains( careNeed.CampusId.ToString() ) );

                var careWorkerCount3 = careWorkersNoFence
                    .Where( cw => !cw.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) && cw.Campuses.Contains( careNeed.CampusId.ToString() ) )
                    .Select( cw => new
                    {
                        Count = cw.AssignedPersons.Where( ap => ap.CareNeed != null && ap.CareNeed.StatusValueId != closedId ).Count(),
                        Worker = cw,
                        HasCategoryAndCampus = false,
                        HasCategory = false,
                        HasCampus = true
                    }
                    )
                    .OrderBy( cw => cw.Count )
                    .ThenBy( cw => cw.Worker.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) )
                    .ThenBy( cw => cw.Worker.Campuses.Contains( careNeed.CampusId.ToString() ) );

                var careWorkerCount4 = careWorkersNoFence
                    .Where( cw => !cw.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) && !cw.Campuses.Contains( careNeed.CampusId.ToString() ) )
                    .Select( cw => new
                    {
                        Count = cw.AssignedPersons.Where( ap => ap.CareNeed != null && ap.CareNeed.StatusValueId != closedId ).Count(),
                        Worker = cw,
                        HasCategoryAndCampus = false,
                        HasCategory = false,
                        HasCampus = false
                    }
                    )
                    .OrderBy( cw => cw.Count )
                    .ThenBy( cw => cw.Worker.CategoryValues.Contains( careNeed.CategoryValueId.ToString() ) )
                    .ThenBy( cw => cw.Worker.Campuses.Contains( careNeed.CampusId.ToString() ) );

                var careWorkerCounts = careWorkerCount1
                    .Concat( careWorkerCount2 )
                    .Concat( careWorkerCount3 )
                    .Concat( careWorkerCount4 )
                    .OrderBy( ct => ct.Count )
                    .ThenByDescending( ct => ct.HasCategoryAndCampus )
                    .ThenByDescending( ct => ct.HasCategory )
                    .ThenByDescending( ct => ct.HasCampus );

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
                    }
                }
            }

            // auto assign Small Group Leader by Role
            var leaderRoleGuid = GetAttributeValue( AttributeKey.GroupTypeAndRole ).AsGuidOrNull() ?? Guid.Empty;
            var leaderRole = new GroupTypeRoleService( rockContext ).Get( leaderRoleGuid );
            if ( leaderRole != null )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var inGroups = groupMemberService.GetByPersonId( careNeed.PersonAlias.PersonId ).Where( gm => gm.Group != null && gm.Group.IsActive && !gm.Group.IsArchived && gm.Group.GroupTypeId == leaderRole.GroupTypeId && !gm.IsArchived && gm.GroupMemberStatus == GroupMemberStatus.Active ).Select( gm => gm.GroupId );

                if ( inGroups.Any() )
                {
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

                }
            }
            rockContext.SaveChanges();
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

        #endregion
    }
}
