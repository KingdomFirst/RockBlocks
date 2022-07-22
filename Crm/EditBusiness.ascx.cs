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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

/*******************************************************************************************************************************
 * NOTE: The Security/AccountEdit.ascx block has very similar functionality.  If updating this block, make sure to check
 * that block also.  It may need the same updates.
 *******************************************************************************************************************************/

namespace RockWeb.Plugins.rocks_kfs.Crm
{
    /// <summary>
    /// The main Person Profile block the main information about a person
    /// </summary>
    [DisplayName( "Edit Business" )]
    [Category( "KFS > CRM" )]
    [Description( "Allows you to edit a business." )]

    [SecurityAction( SecurityActionKey.EditFinancials, "The roles and/or users that can edit financial information for the selected person." )]
    [SecurityAction( SecurityActionKey.EditSMS, "The roles and/or users that can edit the SMS Enabled properties for the selected person." )]
    [SecurityAction( SecurityActionKey.EditConnectionStatus, "The roles and/or users that can edit the connection status for the selected person." )]
    [SecurityAction( SecurityActionKey.EditRecordStatus, "The roles and/or users that can edit the record status for the selected person." )]

    #region Block Attributes

    [BooleanField(
        "Hide Grade",
        Key = AttributeKey.HideGrade,
        Description = "Should the Grade (and Graduation Year) fields be hidden?",
        DefaultBooleanValue = false,
        Order = 0 )]

    [BooleanField(
        "Hide Anniversary Date",
        Key = AttributeKey.HideAnniversaryDate,
        Description = "Should the Anniversary Date field be hidden?",
        DefaultBooleanValue = false,
        Order = 1 )]

    [CustomEnhancedListField(
        "Search Key Types",
        Key = AttributeKey.SearchKeyTypes,
        Description = "Optional list of search key types to limit the display in search keys grid. No selection will show all.",
        ListSource = ListSource.SearchKeyTypes,
        IsRequired = false,
        Order = 2 )]

    #endregion Block Attributes

    public partial class EditBusiness : Rock.Web.UI.PersonBlock
    {

        #region Attribute Keys and Values

        private static class AttributeKey
        {
            public const string HideGrade = "HideGrade";
            public const string HideAnniversaryDate = "HideAnniversaryDate";
            public const string SearchKeyTypes = "SearchKeyTypes";
        }

        private static class ListSource
        {
            public const string SearchKeyTypes = @"
        DECLARE @AttributeId int = (
	        SELECT [Id]
	        FROM [Attribute]
	        WHERE [Guid] = '15C419AA-76A9-4105-AB99-8384AB0E9B44'
        )
        SELECT
	        CAST( V.[Guid] as varchar(40) ) AS [Value],
	        V.[Value] AS [Text]
        FROM [DefinedType] T
        INNER JOIN [DefinedValue] V ON V.[DefinedTypeId] = T.[Id]
        LEFT OUTER JOIN [AttributeValue] AV
	        ON AV.[EntityId] = V.[Id]
	        AND AV.[AttributeId] = @AttributeId
	        AND AV.[Value] = 'False'
        WHERE T.[Guid] = '61BDD0E3-173D-45AB-9E8C-1FBB9FA8FDF3'
        AND AV.[Id] IS NULL
        ORDER BY V.[Order]
";
        }

        #endregion Attribute Keys and Values

        #region Security Actions

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        private static class SecurityActionKey
        {
            public const string EditFinancials = "EditFinancials";
            public const string EditSMS = "EditSMS";
            public const string EditConnectionStatus = "EditConnectionStatus";
            public const string EditRecordStatus = "EditRecordStatus";
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Can the current user edit the SMS Enabled property?
        /// </summary>
        public bool CanEditSmsStatus { get; set; }

        #endregion

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            dvpRecordStatus.DefinedTypeId = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS ) ).Id;
            dvpReason.DefinedTypeId = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS_REASON ) ).Id;

            bool canEditRecordStatus = UserCanAdministrate || IsUserAuthorized( SecurityActionKey.EditRecordStatus );
            dvpRecordStatus.Visible = canEditRecordStatus;

            this.CanEditSmsStatus = UserCanAdministrate || IsUserAuthorized( SecurityActionKey.EditSMS );

            string smsScript = @"
    $('.js-sms-number').on('click', function () {
        if ($(this).is(':checked')) {
            $('.js-sms-number').not($(this)).prop('checked', false);
        }
    });
";
            btnSave.Visible = IsUserAuthorized( Rock.Security.Authorization.EDIT );

            gAlternateIds.Actions.ShowAdd = true;
            gAlternateIds.Actions.AddClick += gAlternateIds_AddClick;

            gSearchKeys.Actions.ShowAdd = true;
            gSearchKeys.Actions.AddClick += gSearchKeys_AddClick;

        }

        /// <summary>
        /// Gets the family name with first names.
        /// </summary>
        /// <param name="familyName">Name of the family.</param>
        /// <param name="familyMembers">The family members.</param>
        /// <returns></returns>
        private string GetFamilyNameWithFirstNames( string familyName, ICollection<GroupMember> familyMembers )
        {
            var adultFirstNames = familyMembers.Where( a => a.GroupRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).OrderBy( a => a.Person.Gender ).ThenBy( a => a.Person.NickName ).Select( a => a.Person.NickName ?? a.Person.FirstName ).ToList();
            var otherFirstNames = familyMembers.Where( a => a.GroupRole.Guid != Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).OrderBy( a => a.Person.Gender ).ThenBy( a => a.Person.NickName ).Select( a => a.Person.NickName ?? a.Person.FirstName ).ToList();
            var firstNames = new List<string>();
            firstNames.AddRange( adultFirstNames );
            firstNames.AddRange( otherFirstNames );
            string familyNameWithFirstNames;
            if ( firstNames.Any() )
            {
                familyNameWithFirstNames = string.Format( "{0} ({1})", familyName, firstNames.AsDelimited( ", ", " and " ) );
            }
            else
            {
                familyNameWithFirstNames = string.Format( "{0} (no family members)", familyName );
            }
            return familyNameWithFirstNames;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack && Person != null )
            {
                ShowDetails();
            }
        }

        #region View State related stuff

        /// <summary>
        /// Gets or sets the state of the person previous names.
        /// </summary>
        /// <value>
        /// The state of the person previous names.
        /// </value>
        private List<PersonPreviousName> PersonPreviousNamesState { get; set; }

        /// <summary>
        /// Gets or sets the state of the person search keys.
        /// </summary>
        /// <value>
        /// The state of the person search keys.
        /// </value>
        private List<PersonSearchKey> PersonSearchKeysState { get; set; }

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            string json = ViewState["PersonPreviousNamesState"] as string;

            if ( string.IsNullOrWhiteSpace( json ) )
            {
                PersonPreviousNamesState = new List<PersonPreviousName>();
            }
            else
            {
                PersonPreviousNamesState = PersonPreviousName.FromJsonAsList( json ) ?? new List<PersonPreviousName>();
            }

            json = ViewState["PersonSearchKeysState"] as string;

            if ( string.IsNullOrWhiteSpace( json ) )
            {
                PersonSearchKeysState = new List<PersonSearchKey>();
            }
            else
            {
                PersonSearchKeysState = PersonSearchKey.FromJsonAsList( json ) ?? new List<PersonSearchKey>();
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
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            ViewState["PersonPreviousNamesState"] = JsonConvert.SerializeObject( PersonPreviousNamesState, Formatting.None, jsonSetting );
            ViewState["PersonSearchKeysState"] = JsonConvert.SerializeObject( PersonSearchKeysState, Formatting.None, jsonSetting );

            return base.SaveViewState();
        }

        #endregion

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlRecordStatus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void ddlRecordStatus_SelectedIndexChanged( object sender, EventArgs e )
        {
            bool showInactiveReason = ( dvpRecordStatus.SelectedValueAsInt() == DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE ) ).Id );

            bool canEditRecordStatus = UserCanAdministrate || IsUserAuthorized( "EditRecordStatus" );
            dvpReason.Visible = showInactiveReason && canEditRecordStatus;
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            if ( IsUserAuthorized( Rock.Security.Authorization.EDIT ) )
            {
                if ( Page.IsValid )
                {
                    var rockContext = new RockContext();

                    var wrapTransactionResult = rockContext.WrapTransactionIf( () =>
                    {
                        var personService = new PersonService( rockContext );

                        Person business = null;
                        if ( int.Parse( hfBusinessId.Value ) != 0 )
                        {
                            business = personService.Get( int.Parse( hfBusinessId.Value ) );
                        }

                        if ( business == null )
                        {
                            business = new Person();
                            personService.Add( business );
                            tbBusinessName.Text = tbBusinessName.Text.FixCase();
                        }

                        int? orphanedPhotoId = null;
                        if ( business.PhotoId != imgPhoto.BinaryFileId )
                        {
                            orphanedPhotoId = business.PhotoId;
                            business.PhotoId = imgPhoto.BinaryFileId;
                        }

                        // Business Name
                        business.LastName = tbBusinessName.Text;

                        // Phone Number
                        var businessPhoneTypeId = new DefinedValueService( rockContext ).GetByGuid( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK ) ).Id;

                        var phoneNumber = business.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == businessPhoneTypeId );

                        if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbPhone.Number ) ) )
                        {
                            if ( phoneNumber == null )
                            {
                                phoneNumber = new PhoneNumber { NumberTypeValueId = businessPhoneTypeId };
                                business.PhoneNumbers.Add( phoneNumber );
                            }
                            phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode );
                            phoneNumber.Number = PhoneNumber.CleanNumber( pnbPhone.Number );
                            phoneNumber.IsMessagingEnabled = cbSms.Checked;
                            phoneNumber.IsUnlisted = cbUnlisted.Checked;
                        }
                        else
                        {
                            if ( phoneNumber != null )
                            {
                                business.PhoneNumbers.Remove( phoneNumber );
                                new PhoneNumberService( rockContext ).Delete( phoneNumber );
                            }
                        }

                        // Record Type - this is always "business". it will never change.
                        business.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;

                        // Record Status
                        business.RecordStatusValueId = dvpRecordStatus.SelectedValueAsInt();
                        ;

                        // Record Status Reason
                        int? newRecordStatusReasonId = null;
                        if ( business.RecordStatusValueId.HasValue && business.RecordStatusValueId.Value == DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE ) ).Id )
                        {
                            newRecordStatusReasonId = dvpReason.SelectedValueAsInt();
                        }
                        business.RecordStatusReasonValueId = newRecordStatusReasonId;

                        // Email
                        business.IsEmailActive = true;
                        business.Email = tbEmail.Text.Trim();
                        business.EmailPreference = rblEmailPreference.SelectedValue.ConvertToEnum<EmailPreference>();

                        /* 2020-10-06 MDP
                         To help prevent a person from setting their communication preference to SMS, even if they don't have an SMS number,
                          we'll require an SMS number in these situations. The goal is to only enforce if they are able to do something about it.
                          1) The block is configured to show both 'Communication Preference' and 'Phone Numbers'
                          2) Communication Preference is set to SMS

                         Edge cases
                           - Both #1 and #2 are true, but no Phone Types are selected in block settings. In this case, still enforce.
                             Think of this as a block configuration issue (they shouldn't have configured it that way)

                           - Person has an SMS phone number, but the block settings don't show it. We'll see if any of the Person's phone numbers
                             have SMS, including ones that are not shown. So, they can set communication preference to SMS without getting a warning.

                        NOTE: We might have already done a save changes at this point, but we are in a DB Transaction, so it'll get rolled back if
                            we return false, with a warning message.
                         */

                        business.CommunicationPreference = rblCommunicationPreference.SelectedValueAsEnum<CommunicationType>();

                        if ( business.CommunicationPreference == CommunicationType.SMS )
                        {
                            if ( !business.PhoneNumbers.Any( a => a.IsMessagingEnabled ) )
                            {
                                nbCommunicationPreferenceWarning.Text = "A phone number with SMS enabled is required when Communication Preference is set to SMS.";
                                nbCommunicationPreferenceWarning.NotificationBoxType = NotificationBoxType.Warning;
                                nbCommunicationPreferenceWarning.Visible = true;
                                return false;
                            }
                        }

                        var personSearchKeyService = new PersonSearchKeyService( rockContext );

                        var validSearchTypes = GetValidSearchKeyTypes();
                        var databaseSearchKeys = personSearchKeyService.Queryable()
                            .Where( a =>
                                validSearchTypes.Contains( a.SearchTypeValue.Guid ) &&
                                a.PersonAlias.PersonId == business.Id )
                            .ToList();

                        foreach ( var deletedSearchKey in databaseSearchKeys.Where( a => !PersonSearchKeysState.Any( p => p.Guid == a.Guid ) ) )
                        {
                            personSearchKeyService.Delete( deletedSearchKey );
                        }

                        foreach ( var personSearchKey in PersonSearchKeysState.Where( a => !databaseSearchKeys.Any( d => d.Guid == a.Guid ) ) )
                        {
                            personSearchKey.PersonAliasId = business.PrimaryAliasId.Value;
                            personSearchKeyService.Add( personSearchKey );
                        }

                        if ( !business.IsValid )
                        {
                            // Controls will render the error messages
                            return false;
                        }

                        if ( business.IsValid )
                        {
                            var saveChangeResult = rockContext.SaveChanges();

                            // if AttributeValues where loaded and set (for example Giving Envelope Number), Save Attribute Values
                            if ( business.AttributeValues != null )
                            {
                                business.SaveAttributeValues( rockContext );
                            }

                            if ( saveChangeResult > 0 )
                            {
                                if ( orphanedPhotoId.HasValue )
                                {
                                    BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                                    var binaryFile = binaryFileService.Get( orphanedPhotoId.Value );
                                    if ( binaryFile != null )
                                    {
                                        string errorMessage;
                                        if ( binaryFileService.CanDelete( binaryFile, out errorMessage ) )
                                        {
                                            binaryFileService.Delete( binaryFile );
                                            rockContext.SaveChanges();
                                        }
                                    }
                                }

                                // if they used the ImageEditor, and cropped it, the uncropped file is still in BinaryFile. So clean it up
                                if ( imgPhoto.CropBinaryFileId.HasValue )
                                {
                                    if ( imgPhoto.CropBinaryFileId != business.PhotoId )
                                    {
                                        BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                                        var binaryFile = binaryFileService.Get( imgPhoto.CropBinaryFileId.Value );
                                        if ( binaryFile != null && binaryFile.IsTemporary )
                                        {
                                            string errorMessage;
                                            if ( binaryFileService.CanDelete( binaryFile, out errorMessage ) )
                                            {
                                                binaryFileService.Delete( binaryFile );
                                                rockContext.SaveChanges();
                                            }
                                        }
                                    }
                                }

                                // Add/Update Family Group
                                var familyGroupType = GroupTypeCache.GetFamilyGroupType();
                                int adultRoleId = familyGroupType.Roles
                                    .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                                    .Select( r => r.Id )
                                    .FirstOrDefault();
                                var adultFamilyMember = UpdateGroupMember( business.Id, familyGroupType, business.LastName + " Business", ddlCampus.SelectedValueAsInt(), adultRoleId, rockContext );
                                business.GivingGroup = adultFamilyMember.Group;

                                // Add/Update Known Relationship Group Type
                                var knownRelationshipGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid() );
                                int knownRelationshipOwnerRoleId = knownRelationshipGroupType.Roles
                                    .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER.AsGuid() ) )
                                    .Select( r => r.Id )
                                    .FirstOrDefault();
                                var knownRelationshipOwner = UpdateGroupMember( business.Id, knownRelationshipGroupType, "Known Relationship", null, knownRelationshipOwnerRoleId, rockContext );

                                // Add/Update Implied Relationship Group Type
                                var impliedRelationshipGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_PEER_NETWORK.AsGuid() );
                                int impliedRelationshipOwnerRoleId = impliedRelationshipGroupType.Roles
                                    .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_PEER_NETWORK_OWNER.AsGuid() ) )
                                    .Select( r => r.Id )
                                    .FirstOrDefault();
                                var impliedRelationshipOwner = UpdateGroupMember( business.Id, impliedRelationshipGroupType, "Implied Relationship", null, impliedRelationshipOwnerRoleId, rockContext );

                                rockContext.SaveChanges();

                                // Location
                                int workLocationTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK ).Id;

                                var groupLocationService = new GroupLocationService( rockContext );
                                var workLocation = groupLocationService.Queryable( "Location" )
                                    .Where( gl =>
                                        gl.GroupId == adultFamilyMember.Group.Id &&
                                        gl.GroupLocationTypeValueId == workLocationTypeId )
                                    .FirstOrDefault();

                                if ( string.IsNullOrWhiteSpace( acAddress.Street1 ) )
                                {
                                    if ( workLocation != null )
                                    {
                                        groupLocationService.Delete( workLocation );
                                    }
                                }
                                else
                                {
                                    var newLocation = new LocationService( rockContext ).Get(
                                        acAddress.Street1, acAddress.Street2, acAddress.City, acAddress.State, acAddress.PostalCode, acAddress.Country );
                                    if ( workLocation == null )
                                    {
                                        workLocation = new GroupLocation();
                                        groupLocationService.Add( workLocation );
                                        workLocation.GroupId = adultFamilyMember.Group.Id;
                                        workLocation.GroupLocationTypeValueId = workLocationTypeId;
                                    }
                                    workLocation.Location = newLocation;
                                    workLocation.IsMailingLocation = true;
                                }

                                rockContext.SaveChanges();

                                hfBusinessId.Value = business.Id.ToString();
                            }
                        }

                        return true;
                    } );

                    if ( wrapTransactionResult )
                    {
                        Response.Redirect( string.Format( "~/Business/{0}", Person.Id ), false );
                    }
                }
            }
        }

        /// <summary>
        /// Gets the search key types that have been configured or are a system type.
        /// </summary>
        /// <returns></returns>
        private List<Guid> GetValidSearchKeyTypes()
        {
            var searchKeyTypes = new List<Guid> { Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() };

            var dt = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_SEARCH_KEYS );
            if ( dt != null )
            {
                var values = dt.DefinedValues;
                var searchTypesList = this.GetAttributeValue( AttributeKey.SearchKeyTypes ).SplitDelimitedValues().AsGuidList();
                if ( searchTypesList.Any() )
                {
                    values = values.Where( v => searchTypesList.Contains( v.Guid ) ).ToList();
                }

                foreach ( var dv in dt.DefinedValues )
                {
                    if ( dv.GetAttributeValue( "UserSelectable" ).AsBoolean() )
                    {
                        searchKeyTypes.Add( dv.Guid );
                    }
                }
            }

            return searchKeyTypes;

        }
        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            Response.Redirect( string.Format( "~/Person/{0}", Person.Id ), false );
        }

        /// <summary>
        /// Shows the details.
        /// </summary>
        private void ShowDetails()
        {
            var business = Person;

            lTitle.Text = string.Format( "Edit: {0}", Person.FullName ).FormatAsHtmlTitle();
            hfBusinessId.Value = Person.Id.ToString();

            imgPhoto.BinaryFileId = Person.PhotoId;
            imgPhoto.NoPictureUrl = Person.GetPersonNoPictureUrl( this.Person, 400, 400 );

            lTitle.Text = ActionTitle.Edit( business.FullName ).FormatAsHtmlTitle();
            tbBusinessName.Text = business.LastName;

            // address
            Location location = null;
            var workLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid() );
            if ( business.GivingGroup != null )     // Giving group is a shortcut to the family group for business
            {
                ddlCampus.SelectedValue = business.GivingGroup.CampusId.ToString();

                location = business.GivingGroup.GroupLocations
                    .Where( gl => gl.GroupLocationTypeValueId == workLocationType.Id )
                    .Select( gl => gl.Location )
                    .FirstOrDefault();
            }
            acAddress.SetValues( location );

            // Phone Number
            var workPhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );
            PhoneNumber phoneNumber = null;
            if ( workPhoneType != null )
            {
                phoneNumber = business.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == workPhoneType.Id );
            }
            if ( phoneNumber != null )
            {
                pnbPhone.Text = phoneNumber.NumberFormatted;
                cbSms.Checked = phoneNumber.IsMessagingEnabled;
                cbUnlisted.Checked = phoneNumber.IsUnlisted;
            }
            else
            {
                pnbPhone.Text = string.Empty;
                cbSms.Checked = false;
                cbUnlisted.Checked = false;
            }

            tbEmail.Text = business.Email;
            rblEmailPreference.SelectedValue = business.EmailPreference.ToString();

            dvpRecordStatus.SelectedValue = business.RecordStatusValueId.HasValue ? business.RecordStatusValueId.Value.ToString() : string.Empty;
            dvpReason.SelectedValue = business.RecordStatusReasonValueId.HasValue ? business.RecordStatusReasonValueId.Value.ToString() : string.Empty;
            dvpReason.Visible = business.RecordStatusReasonValueId.HasValue &&
                business.RecordStatusValueId.Value == DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE ) ).Id;

            var validSearchTypes = GetValidSearchKeyTypes();
            var searchTypeQry = Person.GetPersonSearchKeys().Where( a => validSearchTypes.Contains( a.SearchTypeValue.Guid ) );
            this.PersonSearchKeysState = searchTypeQry.ToList();

            BindPersonPreviousNamesGrid();
            BindPersonAlternateIdsGrid();
            BindPersonSearchKeysGrid();
        }

        /// <summary>
        /// Binds the person previous names grid.
        /// </summary>
        private void BindPersonPreviousNamesGrid()
        {
            grdPreviousNames.DataKeyNames = new string[] { "Guid" };
            grdPreviousNames.DataSource = this.PersonPreviousNamesState;
            grdPreviousNames.DataBind();
        }

        /// <summary>
        /// Binds the person previous names grid.
        /// </summary>
        private void BindPersonAlternateIdsGrid()
        {
            var values = this.PersonSearchKeysState;
            var dv = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() );
            if ( dv != null )
            {
                values = values.Where( s => s.SearchTypeValueId == dv.Id && !s.IsValuePrivate ).ToList();
            }
            gAlternateIds.DataKeyNames = new string[] { "Guid" };
            gAlternateIds.DataSource = values;
            gAlternateIds.DataBind();
        }

        /// <summary>
        /// Binds the person previous names grid.
        /// </summary>
        private void BindPersonSearchKeysGrid()
        {
            var values = this.PersonSearchKeysState;
            var dv = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() );
            if ( dv != null )
            {
                values = values.Where( s => s.SearchTypeValueId != dv.Id ).ToList();
            }
            gSearchKeys.DataKeyNames = new string[] { "Guid" };
            gSearchKeys.DataSource = values;
            gSearchKeys.DataBind();
        }

        /// <summary>
        /// Handles the AddClick event of the gAlternateIds control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gAlternateIds_AddClick( object sender, EventArgs e )
        {
            tbAlternateId.Text = string.Empty;
            mdAlternateId.Show();
        }

        /// <summary>
        /// Handles the SaveClick event of the mdAlternateId control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdAlternateId_SaveClick( object sender, EventArgs e )
        {
            var dv = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() );
            if ( dv != null )
            {
                this.PersonSearchKeysState.Add( new PersonSearchKey { SearchValue = tbAlternateId.Text, SearchTypeValueId = dv.Id, Guid = Guid.NewGuid() } );
            }
            BindPersonAlternateIdsGrid();
            mdAlternateId.Hide();
        }

        /// <summary>
        /// Handles the AddClick event of the gSearchKeys control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gSearchKeys_AddClick( object sender, EventArgs e )
        {
            tbSearchValue.Text = string.Empty;

            var validSearchTypes = GetValidSearchKeyTypes()
                .Where( t => t != Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() )
                .ToList();

            var searchValueTypes = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_SEARCH_KEYS ).DefinedValues;
            var searchTypesList = searchValueTypes.Where( a => validSearchTypes.Contains( a.Guid ) ).ToList();

            ddlSearchValueType.DataSource = searchTypesList;
            ddlSearchValueType.DataTextField = "Value";
            ddlSearchValueType.DataValueField = "Id";
            ddlSearchValueType.DataBind();
            ddlSearchValueType.Items.Insert( 0, new ListItem() );
            mdSearchKey.Show();
        }


        /// <summary>
        /// Handles the SaveClick event of the mdSearchKey control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdSearchKey_SaveClick( object sender, EventArgs e )
        {
            this.PersonSearchKeysState.Add( new PersonSearchKey { SearchValue = tbSearchValue.Text, SearchTypeValueId = ddlSearchValueType.SelectedValue.AsInteger(), Guid = Guid.NewGuid() } );
            BindPersonSearchKeysGrid();
            mdSearchKey.Hide();
        }

        /// <summary>
        /// Handles the Delete event of the gSearchKeys control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gAlternateIds_Delete( object sender, RowEventArgs e )
        {
            this.PersonSearchKeysState.RemoveEntity( ( Guid ) e.RowKeyValue );
            BindPersonAlternateIdsGrid();
        }

        /// <summary>
        /// Handles the Delete event of the gSearchKeys control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gSearchKeys_Delete( object sender, RowEventArgs e )
        {
            this.PersonSearchKeysState.RemoveEntity( ( Guid ) e.RowKeyValue );
            BindPersonSearchKeysGrid();
        }

        /// <summary>
        /// Handles the ServerValidate event of the cvAlternateIds control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="args">The <see cref="ServerValidateEventArgs"/> instance containing the event data.</param>
        protected void cvAlternateIds_ServerValidate( object source, ServerValidateEventArgs args )
        {
            // Validate that none of the alternate ids are being used already.
            var dv = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() );
            if ( dv != null )
            {
                var invalidIds = new List<string>();
                using ( var rockContext = new RockContext() )
                {
                    var service = new PersonSearchKeyService( rockContext );
                    foreach ( var value in PersonSearchKeysState.Where( s => s.SearchTypeValueId == dv.Id ).ToList() )
                    {
                        if ( service.Queryable().AsNoTracking()
                            .Any( v =>
                                v.SearchTypeValueId == dv.Id &&
                                v.SearchValue == value.SearchValue &&
                                v.Guid != value.Guid ) )
                        {
                            invalidIds.Add( value.SearchValue );
                        }
                    }
                }

                if ( invalidIds.Any() )
                {
                    if ( invalidIds.Count == 1 )
                    {
                        cvAlternateIds.ErrorMessage = string.Format( "The '{0}' alternate id is already being used by another person. Please remove this value and optionally add a new unique alternate id.", invalidIds.First() );
                    }
                    else
                    {
                        cvAlternateIds.ErrorMessage = string.Format( "The '{0}' alternate ids are already being used by another person. Please remove these value and optionally add new unique alternate ids.", invalidIds.AsDelimited( "' and '" ) );
                    }

                    args.IsValid = false;
                }
            }
        }

        /// <summary>
        /// Updates the group member.
        /// </summary>
        /// <param name="businessId">The business identifier.</param>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="groupRoleId">The group role identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private GroupMember UpdateGroupMember( int businessId, GroupTypeCache groupType, string groupName, int? campusId, int groupRoleId, RockContext rockContext )
        {
            var groupMemberService = new GroupMemberService( rockContext );

            GroupMember groupMember = groupMemberService.Queryable( "Group" )
                .Where( m =>
                    m.PersonId == businessId &&
                    m.GroupRoleId == groupRoleId )
                .FirstOrDefault();

            if ( groupMember == null )
            {
                groupMember = new GroupMember();
                groupMember.Group = new Group();
            }

            groupMember.PersonId = businessId;
            groupMember.GroupRoleId = groupRoleId;
            groupMember.GroupMemberStatus = GroupMemberStatus.Active;

            groupMember.Group.GroupTypeId = groupType.Id;
            groupMember.Group.Name = groupName;
            groupMember.Group.CampusId = campusId;

            if ( groupMember.Id == 0 )
            {
                groupMemberService.Add( groupMember );
            }

            return groupMember;
        }
    }
}