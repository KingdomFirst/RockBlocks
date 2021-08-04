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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
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

    [BooleanField( "Allow New Person Entry", "Should you be able to enter a new person from the care entry form and use person matching?", false, key: "AllowNewPerson" )]

    #endregion

    public partial class CareEntry : Rock.Web.UI.RockBlock
    {

        bool _allowNewPerson = false;

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

                confirmExit.Enabled = true;
            }
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

                CareNeed careNeed = null;
                int careNeedId = PageParameter( "CareNeedId" ).AsInteger();

                if ( !careNeedId.Equals( 0 ) )
                {
                    careNeed = careNeedService.Get( careNeedId );
                }

                if ( careNeed == null )
                {
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
                    careNeed.PersonAliasId = ( person != null ) ? person.PrimaryAliasId : null;

                    if ( careNeed.PersonAliasId == null )
                    {
                        cvPersonValidation.IsValid = false;
                        cvPersonValidation.ErrorMessage = "A Person must be selected or First Name, Last Name and Email and/or Phone number must be filled out to proceed.";
                        return;
                    }
                }

                careNeed.SubmitterAliasId = ppSubmitter.PersonAliasId;

                careNeed.StatusValueId = dvpStatus.SelectedValue.AsIntegerOrNull();
                careNeed.CategoryValueId = dvpCategory.SelectedValue.AsIntegerOrNull();

                if ( dpDate.SelectedDate.HasValue )
                {
                    careNeed.DateEntered = dpDate.SelectedDate.Value;
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
                var personId = this.PageParameter( "PersonId" ).AsIntegerOrNull();
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

            dtbDetailsText.Text = careNeed.Details;
            dpDate.SelectedDate = careNeed.DateEntered;

            if ( careNeed.Campus != null )
            {
                cpCampus.SelectedCampusId = careNeed.CampusId;
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
                    hlStatus.LabelType = LabelType.Warning;
                }

                if ( careNeed.Status.Value == "Closed" )
                {
                    hlStatus.Text = "Closed";
                    hlStatus.LabelType = LabelType.Danger;
                }
            }

            if ( careNeed.CategoryValueId != null )
            {
                dvpCategory.SetValue( careNeed.CategoryValueId );
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

            ppSubmitter.Visible = true;
        }

        #endregion
    }
}
