// <copyright>
// Copyright 2025 by Kingdom First Solutions
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
using System.Linq.Dynamic.Core;
using System.Web.UI;
using System.Web.UI.WebControls;

using NuGet;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Cms
{
    #region Block Attributes

    [DisplayName( "Edit Profile" )]
    [Category( "KFS > CMS" )]
    [Description( "Customized Edit Profile block to have more control over the fields and interface." )]

    #endregion Block Attributes

    #region Block Settings

    [BooleanField(
        "Allow Photo Editing",
        Description = "Should you be able to edit the person photo?",
        DefaultBooleanValue = true,
        Key = AttributeKey.AllowPhotoEditing,
        Order = 0 )]

    [BooleanField(
        "Display Buttons in All Panels",
        Description = "Should the Save and Cancel button be added to each panel?",
        DefaultBooleanValue = false,
        Key = AttributeKey.DisplayButtonsInAllPanels,
        Order = 0 )]

    [TextField(
        "Family Member Header",
        Key = AttributeKey.FamilyMemberFieldsHeader,
        Description = "The Header text for the \"Family Fields\" panel.",
        IsRequired = false,
        DefaultValue = "Family Members",
        Order = 1 )]

    [BooleanField(
        "Show Family Members",
        Description = "Should family members be shown to add/edit or not?",
        DefaultBooleanValue = true,
        Key = AttributeKey.ShowFamilyMembers,
        Order = 2 )]

    [TextField(
        "Person Fields Header",
        Key = AttributeKey.PersonFieldsHeader,
        Description = "The Header text for the \"Person Fields\" panel.",
        IsRequired = false,
        DefaultValue = "Profile Information",
        Category = AttributeCategory.PersonFields,
        Order = 3 )]

    [CustomDropdownListField(
        "Title",
        Key = AttributeKey.Title,
        Description = "How should Title be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Optional",
        Order = 4 )]

    [CustomDropdownListField(
        "First Name",
        Key = AttributeKey.FirstName,
        Description = "How should FirstName be displayed?",
        ListSource = ListSource.HIDE_DISABLE_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Required",
        Order = 5 )]

    [CustomDropdownListField(
        "Nick Name",
        Key = AttributeKey.NickName,
        Description = "How should Nick Name be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Hide",
        Order = 6 )]

    [CustomDropdownListField(
        "Last Name",
        Key = AttributeKey.LastName,
        Description = "How should Last Name be displayed?",
        ListSource = ListSource.HIDE_DISABLE_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Required",
        Order = 7 )]

    [CustomDropdownListField(
        "Suffix",
        Key = AttributeKey.Suffix,
        Description = "How should Suffix be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Optional",
        Order = 8 )]

    [CustomDropdownListField(
        "Birthday",
        Key = AttributeKey.Birthday,
        Description = "How should Birthday be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Required",
        Order = 9 )]

    [CustomDropdownListField(
        "Grade",
        Key = AttributeKey.Grade,
        Description = "How should Grade be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Optional",
        Order = 9 )]

    [CustomDropdownListField(
        "Role",
        Key = AttributeKey.Role,
        Description = "How should Role be displayed?",
        ListSource = "Required",
        Category = "CustomSetting",
        IsRequired = false,
        DefaultValue = "Required",
        Order = 9 )]

    [CustomDropdownListField(
        "Gender",
        Key = AttributeKey.Gender,
        Description = "How should Gender be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Required",
        Order = 10 )]

    [CustomDropdownListField(
        "Race",
        Key = AttributeKey.Race,
        Description = "How should Race be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Hide",
        Order = 11 )]

    [CustomDropdownListField(
        "Ethnicity",
        Key = AttributeKey.Ethnicity,
        Description = "How should Ethnicity be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Hide",
        Order = 12 )]

    [CustomDropdownListField(
        "Marital Status",
        Key = AttributeKey.MaritalStatus,
        Description = "How should Marital Status be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        Category = AttributeCategory.PersonFields,
        IsRequired = false,
        DefaultValue = "Optional",
        Order = 13 )]

    [TextField(
        "Address Field Header",
        Key = AttributeKey.AddressFieldsHeader,
        Description = "The Header text for the \"Address\" panel.",
        IsRequired = false,
        DefaultValue = "{{ Type }} Address",
        Category = AttributeCategory.ContactFields,
        Order = 14 )]

    [CustomDropdownListField(
        "Addresses",
        Key = AttributeKey.Address,
        Description = "How should Address be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIRED,
        IsRequired = false,
        DefaultValue = "Optional",
        Category = AttributeCategory.ContactFields,
        Order = 15 )]

    [GroupLocationTypeField(
        "Address Type",
        Key = AttributeKey.AddressType,
        Description = "The type of address to be displayed / edited.",
        GroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY,
        IsRequired = false,
        DefaultValue = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME,
        Category = AttributeCategory.ContactFields,
        Order = 16 )]

    [TextField(
        "Contact Fields Header",
        Key = AttributeKey.ContactFieldsHeader,
        Description = "The Header text for the \"Contact Fields\" panel.",
        IsRequired = false,
        DefaultValue = "Contact Information",
        Category = AttributeCategory.ContactFields,
        Order = 17 )]

    [CustomDropdownListField(
        "Email",
        Key = AttributeKey.Email,
        Description = "How should Email be displayed?",
        ListSource = ListSource.HIDE_OPTIONAL_REQUIREDADULT_REQUIREDBOTH,
        Category = AttributeCategory.ContactFields,
        IsRequired = false,
        DefaultValue = "Optional",
        Order = 18 )]

    [DefinedValueField(
        "Phone Types",
        Key = AttributeKey.PhoneTypes,
        Description = "The types of phone numbers to display / edit.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE,
        IsRequired = false,
        AllowMultiple = true,
        Category = AttributeCategory.ContactFields,
        Order = 19 )]

    [DefinedValueField(
        "Required Adult Phone Types",
        Key = AttributeKey.RequiredAdultPhoneTypes,
        Description = "The phone numbers that are required when editing an adult record.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE,
        IsRequired = false,
        AllowMultiple = true,
        Category = AttributeCategory.ContactFields,
        Order = 20 )]

    [BooleanField( "Show SMS Enable message",
        Description = "Should the \"Mobile\" phone type display a message to enable SMS?",
        DefaultBooleanValue = true,
        Key = AttributeKey.ShowSMSEnable,
        Category = AttributeCategory.ContactFields,
        Order = 21 )]

    [TextField(
        "SMS Enable Label",
        Key = AttributeKey.SMSEnableLabel,
        Description = "The label for the SMS Enable checkbox",
        IsRequired = false,
        DefaultValue = "I would like to receive important text messages",
        Category = AttributeCategory.ContactFields,
        Order = 22 )]

    [CustomDropdownListField(
        "Email Preference",
        Key = AttributeKey.EmailPreference,
        Description = "Should Email Preference be displayed?",
        ListSource = "Hide,Show on Adult,Show on All",
        Category = AttributeCategory.ContactFields,
        IsRequired = false,
        DefaultValue = "Hide",
        Order = 23 )]

    [CustomDropdownListField(
        "Communication Preference",
        Key = AttributeKey.CommunicationPreference,
        Description = "Should Communication Preference be displayed?",
        ListSource = "Hide,Show on Adult,Show on All",
        Category = AttributeCategory.ContactFields,
        IsRequired = false,
        DefaultValue = "Hide",
        Order = 24 )]

    [TextField(
        "Family Field Header",
        Key = AttributeKey.FamilyFieldsHeader,
        Description = "The Header text for the \"Family\" panel (primarily for family attributes, but can also contain campus).",
        IsRequired = false,
        DefaultValue = "Family Information",
        Category = AttributeCategory.Attributes,
        Order = 24 )]

    [AttributeField(
        "Family Attributes",
        Key = AttributeKey.FamilyAttributes,
        EntityTypeGuid = Rock.SystemGuid.EntityType.GROUP,
        EntityTypeQualifierColumn = "GroupTypeId",
        EntityTypeQualifierValue = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY,
        Description = "The family attributes that should be displayed / edited.",
        IsRequired = false,
        AllowMultiple = true,
        Category = AttributeCategory.Attributes,
        Order = 25 )]

    [AttributeField(
        "Person Attributes (adults)",
        Key = AttributeKey.PersonAttributesAdults,
        EntityTypeGuid = Rock.SystemGuid.EntityType.PERSON,
        Description = "The person attributes that should be displayed / edited for adults.",
        IsRequired = false,
        AllowMultiple = true,
        Category = AttributeCategory.Attributes,
        Order = 26 )]

    [AttributeField(
        "Person Attributes (children)",
        Key = AttributeKey.PersonAttributesChildren,
        EntityTypeGuid = Rock.SystemGuid.EntityType.PERSON,
        Description = "The person attributes that should be displayed / edited for children.",
        IsRequired = false,
        AllowMultiple = true,
        Category = AttributeCategory.Attributes,
        Order = 27 )]

    [CustomDropdownListField(
        "Campus Selector",
        Key = AttributeKey.CampusSelector,
        Description = "Should Campus Selector be displayed and where?",
        ListSource = "Hide,Show with Person,Show with Family",
        Category = AttributeCategory.Campus,
        IsRequired = false,
        DefaultValue = "Hide",
        Order = 28 )]

    [TextField(
        "Campus Selector Label",
        Key = AttributeKey.CampusSelectorLabel,
        Description = "The label for the campus selector (only effective when \"Show Campus Selector\" is enabled).",
        IsRequired = false,
        DefaultValue = "Campus",
        Category = AttributeCategory.Campus,
        Order = 29 )]

    [DefinedValueField(
        "Campus Types",
        Key = AttributeKey.CampusTypes,
        Description = "This setting filters the list of campuses by type that are displayed in the campus drop-down.",
        IsRequired = false,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_TYPE,
        AllowMultiple = true,
        Category = AttributeCategory.Campus,
        Order = 30 )]

    [DefinedValueField(
        "Campus Statuses",
        Key = AttributeKey.CampusStatuses,
        Description = "This setting filters the list of campuses by statuses that are displayed in the campus drop-down.",
        IsRequired = false,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_STATUS,
        AllowMultiple = true,
        Category = AttributeCategory.Campus,
        Order = 31 )]

    [ValueListField(
        name: "Panel Order",
        description: "Set the order of the panels, valid panel names: Person, Contact, Family, FamilyMember, Address",
        key: AttributeKey.PanelOrder,
        required: false,
        customValues: ListSource.Panels,
        order: 32 )]

    [ValueListField(
        name: "Person Fields Order",
        description: "Set the order of the person fields.",
        key: AttributeKey.PersonFieldsOrder,
        required: false,
        customValues: ListSource.PersonFields + ",Spacer",
        order: 33 )]

    [ValueListField(
        name: "Contact Fields Order",
        description: "Set the order of the contact fields.",
        key: AttributeKey.ContactFieldsOrder,
        required: false,
        customValues: ListSource.ContactFields + ",Spacer",
        order: 34 )]

    [ValueListField(
        name: "Family Member Fields Order",
        description: "Set the order of the person fields.",
        key: AttributeKey.FamilyMemberFieldsOrder,
        required: false,
        customValues: ListSource.PersonFields + ",PersonSpacer," + ListSource.ContactFields + ",ContactSpacer",
        order: 35 )]

    [BooleanField(
        "Match Person Fields on Family Members",
        Description = "Should all the person and contact fields that are displayed on the Current Person be displayed on all Family Members? If no, it will use the 'Family Member Fields Order' to also determine what fields are displayed.",
        DefaultBooleanValue = true,
        Key = AttributeKey.MatchPersonFieldsFamilyMember,
        Order = 36 )]

    [BooleanField(
        "Allow Adding Family Members?",
        Description = "Should this block allow the ability to add new Family Members?",
        DefaultBooleanValue = true,
        Key = AttributeKey.AllowAddingFamilyMembers,
        Order = 37 )]

    [LinkedPage(
        "Redirect Page",
        Description = "Page to redirect on Save or Cancel. By Default it will use a returnUrl page parameter if provided or display a modal stating your profile has been updated.",
        IsRequired = false,
        Key = AttributeKey.RedirectPage,
        Order = 38 )]

    #endregion Block Settings
    public partial class ProfileBioEdit : Rock.Web.UI.RockBlock
    {
        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string AllowPhotoEditing = "AllowPhotoEditing";
            public const string DisplayButtonsInAllPanels = "DisplayButtonsInAllPanels";
            public const string FamilyMemberFieldsHeader = "FamilyMemberFieldsHeader";
            public const string ShowFamilyMembers = "ShowFamilyMembers";
            public const string PersonFieldsHeader = "PersonFieldsHeader";
            public const string Title = "Title";
            public const string FirstName = "FirstName";
            public const string NickName = "NickName";
            public const string LastName = "LastName";
            public const string Suffix = "Suffix";
            public const string Birthday = "Birthday";
            public const string Gender = "Gender";
            public const string Grade = "Grade";
            public const string Role = "Role";
            public const string Race = "Race";
            public const string Ethnicity = "Ethnicity";
            public const string MaritalStatus = "MaritalStatus";
            public const string AddressFieldsHeader = "AddressFieldsHeader";
            public const string Address = "Address";
            public const string AddressType = "AddressType";
            public const string ContactFieldsHeader = "ContactFieldsHeader";
            public const string Email = "Email";
            public const string PhoneTypes = "PhoneTypes";
            public const string RequiredAdultPhoneTypes = "RequiredAdultPhoneTypes";
            public const string SMSEnableLabel = "SMSEnableLabel";
            public const string ShowSMSEnable = "ShowSMSEnable";
            public const string EmailPreference = "EmailPreference";
            public const string CommunicationPreference = "CommunicationPreference";
            public const string FamilyFieldsHeader = "FamilyFieldsHeader";
            public const string FamilyAttributes = "FamilyAttributes";
            public const string PersonAttributesAdults = "PersonAttributesAdults";
            public const string PersonAttributesChildren = "PersonAttributesChildren";
            public const string CampusSelector = "Campus";
            public const string CampusSelectorLabel = "CampusSelectorLabel";
            public const string CampusTypes = "CampusTypes";
            public const string CampusStatuses = "CampusStatuses";
            public const string PanelOrder = "PanelOrder";
            public const string PersonFieldsOrder = "PersonFieldsOrder";
            public const string ContactFieldsOrder = "ContactFieldsOrder";
            public const string FamilyMemberFieldsOrder = "FamilyMemberFieldsOrder";
            public const string MatchPersonFieldsFamilyMember = "MatchPersonFieldsFamilyMember";
            public const string AllowAddingFamilyMembers = "AllowAddingFamilyMembers";
            public const string RedirectPage = "RedirectPage";
        }

        private static class PageParameterKey
        {
            public const string ReturnUrl = "ReturnUrl";
        }

        private static class ListSource
        {
            public const string HIDE_OPTIONAL_REQUIREDADULT_REQUIREDBOTH = "Hide,Optional,RequiredAdult^Required Adult,Required^Required Adult and Child";
            public const string HIDE_OPTIONAL_REQUIRED = "Hide,Optional,Required";
            public const string HIDE_DISABLE_REQUIRED = "Hide,Disable,Required";
            public const string Panels = "Person,Contact,Family,FamilyMember,Address";
            public const string PersonFields = "Photo,Title,FirstName,NickName,LastName,Suffix,Birthday,Graduation,Grade,Role,Gender,Race,Ethnicity,MaritalStatus,Campus,PersonAttributes";
            public const string ContactFields = "Phone,Email,EmailPreference,CommunicationPreference";
        }

        private static class AttributeCategory
        {
            public const string PersonFields = "Person Fields";
            public const string ContactFields = "Contact Fields";
            public const string Campus = "Campus";
            public const string Attributes = "Attributes";
        }

        private List<PersonFamilyMember> FamilyMembers
        {
            get { return Session["FamilyMembers"] as List<PersonFamilyMember>; }
            set { Session["FamilyMembers"] = value; }
        }

        #region Properties


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

            BuildForm();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            //confirmExit.Enabled = true;

            lbSave.ValidationGroup = BlockValidationGroup;

            lbCancel.ValidationGroup = BlockValidationGroup;
            lbCancel.CausesValidation = false;

            var displayButtonsInPanels = GetAttributeValue( AttributeKey.DisplayButtonsInAllPanels ).AsBoolean();

            lbSave.Visible = !displayButtonsInPanels;
            lbCancel.Visible = !displayButtonsInPanels;

            pnlProfilePanels.DefaultButton = lbSave.ID;

            // Initialize year picker javascript per grade ddl
            //if ( ddlGrade.Visible )
            //{
            //    //ScriptManager.RegisterStartupScript( ddlGrade, ddlGrade.GetType(), "grade-selection-" + BlockId.ToString(), ddlGrade.GetJavascriptForYearPicker( ypGraduation ), true );
            //}


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
            BuildForm();
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
                var rockContext = new RockContext();
                var personGuid = hfPrimaryPersonGuid.Value.AsGuid();
                var groupId = hfGroupId.Value.AsIntegerOrNull();

                if ( !groupId.HasValue )
                {
                    // GroupId wasn't specified due to invalid situation
                    // Return and report nothing.
                    return;
                }

                var group = new GroupService( rockContext ).Get( groupId.Value );
                if ( group == null )
                {
                    return;
                }

                var pnlAddress = pnlProfilePanels.FindControl( "pnlAddress" ) as Panel;
                var acAddress = pnlAddress.FindControl( "acAddress" ) as AddressControl;
                var cbIsMailingAddress = pnlAddress.FindControl( "cbIsMailingAddress" ) as RockCheckBox;

                var avcFamilyAttributes = pnlProfilePanels.FindControl( "avcFamilyAttributes" ) as AttributeValuesContainer;

                var wrapTransactionResult = rockContext.WrapTransactionIf( () =>
                {
                    Person person = null;
                    var returnVal = SavePerson( rockContext, ref personGuid, groupId, group, pnlProfilePanels, out person );

                    if ( returnVal )
                    {
                        var pnlFamilyMemberBody = pnlProfilePanels.FindControl( "pnlFamilyMemberBody" );
                        if ( pnlFamilyMemberBody != null )
                        {
                            foreach ( var fm in FamilyMembers )
                            {
                                var familyMemberGuid = fm.Guid;
                                var pwFamilyMember = pnlFamilyMemberBody.FindControl( $"pwFamilyMember_{fm.PersonId}" ) as PanelWidget;
                                var fmPerson = fm.Person;
                                if ( pwFamilyMember != null )
                                {
                                    var pnlFamilyMemberPerson = pwFamilyMember.FindControl( "pnlFamilyMemberPerson" ) as Panel;
                                    returnVal = SavePerson( rockContext, ref familyMemberGuid, groupId, group, pnlFamilyMemberPerson, out fmPerson );
                                    if ( returnVal )
                                    {
                                        fm.Person = fmPerson;
                                        pwFamilyMember.Expanded = false;
                                        pwFamilyMember.ShowDeleteButton = false;
                                        var rblRole = pnlFamilyMemberPerson.FindControl( "rblRole" ) as RockRadioButtonList;
                                        var roleText = fm.Person.AgeClassification != AgeClassification.Unknown ? fm.Person.GetFamilyRole().ToString() : rblRole?.SelectedItem?.Text;
                                        pwFamilyMember.Title = $"<h6>{fm.Person.FullName}<br><small>{roleText}</small></h6>";
                                    }
                                }
                            }
                        }
                    }

                    var familyGroup = new GroupService( rockContext )
                        .Queryable()
                        .Where( f =>
                            f.Id == groupId.Value &&
                            f.Members.Any( m => m.Person.Guid == personGuid ) )
                        .FirstOrDefault();
                    if ( familyGroup != null )
                    {
                        // save family information
                        if ( pnlAddress != null && pnlAddress.Visible && acAddress != null )
                        {
                            Guid? addressTypeGuid = GetAttributeValue( AttributeKey.AddressType ).AsGuidOrNull();
                            if ( addressTypeGuid.HasValue )
                            {
                                var groupLocationService = new GroupLocationService( rockContext );

                                var dvAddressType = DefinedValueCache.Get( addressTypeGuid.Value );
                                var familyAddress = groupLocationService.Queryable().Where( l => l.GroupId == familyGroup.Id && l.GroupLocationTypeValueId == dvAddressType.Id ).FirstOrDefault();
                                if ( familyAddress != null && string.IsNullOrWhiteSpace( acAddress.Street1 ) )
                                {
                                    // delete the current address
                                    groupLocationService.Delete( familyAddress );
                                    rockContext.SaveChanges();
                                }
                                else
                                {
                                    if ( !string.IsNullOrWhiteSpace( acAddress.Street1 ) )
                                    {
                                        if ( familyAddress == null )
                                        {
                                            familyAddress = new GroupLocation();
                                            groupLocationService.Add( familyAddress );
                                            familyAddress.GroupLocationTypeValueId = dvAddressType.Id;
                                            familyAddress.GroupId = familyGroup.Id;
                                            familyAddress.IsMailingLocation = true;
                                            familyAddress.IsMappedLocation = true;
                                        }

                                        familyAddress.IsMailingLocation = cbIsMailingAddress.Checked;

                                        var loc = new Location();
                                        acAddress.GetValues( loc );

                                        familyAddress.Location = new LocationService( rockContext ).Get(
                                            loc.Street1, loc.Street2, loc.City, loc.State, loc.PostalCode, loc.Country, familyGroup, true );

                                        // since there can only be one mapped location, set the other locations to not mapped
                                        if ( familyAddress.IsMappedLocation )
                                        {
                                            var groupLocations = groupLocationService.Queryable()
                                                .Where( l => l.GroupId == familyGroup.Id && l.Id != familyAddress.Id ).ToList();

                                            foreach ( var groupLocation in groupLocations )
                                            {
                                                groupLocation.IsMappedLocation = false;
                                            }
                                        }

                                        rockContext.SaveChanges();
                                    }
                                }
                            }

                            if ( avcFamilyAttributes != null )
                            {
                                familyGroup.LoadAttributes();
                                avcFamilyAttributes.GetEditValues( familyGroup );
                                familyGroup.SaveAttributeValues();
                            }
                        }
                    }

                    return returnVal;
                } );

                if ( wrapTransactionResult )
                {
                    //confirmExit.Enabled = false;

                    // When in EditOnly mode if there's a ReturnUrl specified navigate to that page.
                    // Otherwise stay on the page, but show a saved success message.
                    var returnUrl = PageParameter( PageParameterKey.ReturnUrl );

                    if ( returnUrl.IsNotNullOrWhiteSpace() )
                    {
                        string redirectUrl = Server.UrlDecode( returnUrl );

                        string queryString = string.Empty;
                        if ( redirectUrl.Contains( "?" ) )
                        {
                            queryString = redirectUrl.Split( '?' ).Last();
                        }
                        Context.Response.Redirect( redirectUrl );
                    }
                    else
                    {
                        NavigateToLinkedPage( AttributeKey.RedirectPage );
                    }

                    //hlblSuccess.Visible = true;

                    maAlert.Show( "Your profile has been updated!", ModalAlertType.None );
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
            var redirect = NavigateToLinkedPage( AttributeKey.RedirectPage );
            if ( !redirect )
            {
                NavigateToParentPage();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbAddFamilyMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddFamilyMember_Click( object sender, EventArgs e )
        {
            var pnlFamilyMemberBody = pnlProfilePanels.FindControl( "pnlFamilyMemberBody" );
            if ( pnlFamilyMemberBody != null )
            {
                var lastPersonId = FamilyMembers.OrderByDescending( fm => fm.PersonId ).Select( fm => fm.PersonId ).LastOrDefault();
                if ( lastPersonId > 0 )
                {
                    lastPersonId = 0;
                }
                lastPersonId--;

                var newFamilyMember = new PersonFamilyMember
                {
                    Guid = Guid.Empty,
                    PersonId = lastPersonId,
                    Person = new Person()
                };
                newFamilyMember.Person.LastName = "New Family Member";

                FamilyMembers.Add( newFamilyMember );

                AddFamilyMember( pnlFamilyMemberBody, newFamilyMember, true );
            }
        }

        /// <summary>
        /// Handles the DeleteClick event of the pwFamilyMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void pwFamilyMember_DeleteClick( object sender, EventArgs e )
        {
            PanelWidget panelWidget = sender as PanelWidget;
            var pnlFamilyMemberBody = pnlProfilePanels.FindControl( "pnlFamilyMemberBody" );
            if ( panelWidget != null && pnlFamilyMemberBody != null )
            {
                var personId = panelWidget.ID.Replace( "pwFamilyMember_", string.Empty ).AsInteger();
                pnlFamilyMemberBody.Controls.Remove( panelWidget );
                var familyMember = FamilyMembers.First( a => a.PersonId == personId );
                FamilyMembers.Remove( familyMember );
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblRole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void rblRole_SelectedIndexChanged( object sender, EventArgs e )
        {
            var rblRole = sender as RockRadioButtonList;
            if ( rblRole != null )
            {
                var roleTypeId = rblRole.SelectedValueAsId();
                var parentPanelWidget = rblRole.FirstParentControlOfType<PanelWidget>();
                if ( parentPanelWidget != null && roleTypeId.HasValue )
                {
                    var childGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid();
                    var groupTypeFamily = GroupTypeCache.GetFamilyGroupType();

                    var ypGraduation = parentPanelWidget.FindControl( "ypGraduation" ) as YearPicker;
                    var ddlGrade = parentPanelWidget.FindControl( "ddlGrade" ) as RockDropDownList;

                    var dvpMaritalStatus = parentPanelWidget.FindControl( "dvpMaritalStatus" ) as DefinedValuePicker;
                    var avcPersonAttributes = parentPanelWidget.FindControl( "avcPersonAttributes" ) as AttributeValuesContainer;
                    var avcChildPersonAttributes = parentPanelWidget.FindControl( "avcChildPersonAttributes" ) as AttributeValuesContainer;
                    var ebEmail = parentPanelWidget.FindControl( "ebEmail" ) as EmailBox;
                    var rblEmailPreference = parentPanelWidget.FindControl( "rblEmailPreference" ) as RockRadioButtonList;
                    var rblCommunicationPreference = parentPanelWidget.FindControl( "rblCommunicationPreference" ) as RockRadioButtonList;
                    var emailRequired = GetAttributeValue( AttributeKey.Email );

                    var requiredPhoneTypes = GetAttributeValues( AttributeKey.RequiredAdultPhoneTypes ).AsGuidList();
                    var requiredPhoneTypeControls = new List<PhoneNumberBox>();
                    if ( requiredPhoneTypes.Any() )
                    {
                        var phoneTypeValueIds = new DefinedValueService( new RockContext() )
                            .GetByGuids( requiredPhoneTypes ).Select( v => v.Id );

                        foreach ( var typeValueId in phoneTypeValueIds )
                        {
                            var pnbPhone = parentPanelWidget.FindControl( $"pnbPhone{typeValueId}" ) as PhoneNumberBox;
                            if ( pnbPhone != null )
                            {
                                requiredPhoneTypeControls.Add( pnbPhone );
                            }
                        }
                    }

                    var displayEmailPreference = GetAttributeValue( AttributeKey.EmailPreference );
                    var displayCommunicationPreference = GetAttributeValue( AttributeKey.CommunicationPreference );


                    if ( groupTypeFamily.Roles.Where( gr => gr.Guid == childGuid && gr.Id == roleTypeId ).Any() )
                    {
                        if ( ddlGrade != null )
                        {
                            ddlGrade.Visible = true;
                            var parentControl = ddlGrade.Parent as WebControl;
                            if ( parentControl != null )
                            {
                                parentControl.CssClass = "col-md-6";
                            }
                        }
                        if ( dvpMaritalStatus != null )
                        {
                            dvpMaritalStatus.Visible = false;
                            var parentControl = dvpMaritalStatus.Parent as WebControl;
                            if ( parentControl != null )
                            {
                                parentControl.CssClass = "";
                            }
                        }
                        if ( avcPersonAttributes != null )
                        {
                            avcPersonAttributes.Visible = false;
                        }
                        if ( avcChildPersonAttributes != null )
                        {
                            avcChildPersonAttributes.Visible = true;
                        }
                        if ( ebEmail != null )
                        {
                            ebEmail.Required = emailRequired == "Required";
                        }
                        foreach ( var pnbPhone in requiredPhoneTypeControls )
                        {
                            pnbPhone.Required = false;
                            var parentContainer = parentPanelWidget.FindControl( $"pnlContainer{pnbPhone.ID.Replace( "pnbPhone", "" )}_Phone" ) as WebControl;
                            if ( parentContainer != null )
                            {
                                parentContainer.RemoveCssClass( "required" );
                            }
                        }
                        if ( rblEmailPreference != null )
                        {
                            rblEmailPreference.Visible = displayEmailPreference == "Show on All";
                        }
                        if ( rblCommunicationPreference != null )
                        {
                            rblCommunicationPreference.Visible = displayCommunicationPreference == "Show on All";
                        }
                    }
                    else
                    {
                        if ( ddlGrade != null )
                        {
                            ddlGrade.Visible = false;
                            var parentControl = ddlGrade.Parent as WebControl;
                            if ( parentControl != null )
                            {
                                parentControl.CssClass = "";
                            }
                        }
                        if ( dvpMaritalStatus != null )
                        {
                            dvpMaritalStatus.Visible = true;
                            var parentControl = dvpMaritalStatus.Parent as WebControl;
                            if ( parentControl != null )
                            {
                                parentControl.CssClass = "col-md-6";
                            }
                        }
                        if ( avcPersonAttributes != null )
                        {
                            avcPersonAttributes.Visible = true;
                        }
                        if ( avcChildPersonAttributes != null )
                        {
                            avcChildPersonAttributes.Visible = false;
                        }
                        if ( ebEmail != null )
                        {
                            ebEmail.Required = emailRequired.Contains( "Required" );
                        }
                        foreach ( var pnbPhone in requiredPhoneTypeControls )
                        {
                            pnbPhone.Required = true;
                            var parentContainer = parentPanelWidget.FindControl( $"pnlContainer{pnbPhone.ID.Replace( "pnbPhone", "" )}_Phone" ) as WebControl;
                            if ( parentContainer != null )
                            {
                                parentContainer.AddCssClass( "required" );
                            }
                        }
                        if ( rblEmailPreference != null )
                        {
                            rblEmailPreference.Visible = displayEmailPreference != "Hide";
                        }
                        if ( rblCommunicationPreference != null )
                        {
                            rblCommunicationPreference.Visible = displayCommunicationPreference != "Hide";
                        }
                    }

                }
            }
        }
        #endregion Events

        #region Methods

        public void BuildForm()
        {
            pnlProfilePanels.Controls.Clear();

            var panels = ListSource.Panels.SplitDelimitedValues( false ).ToList();
            var panelOrder = GetAttributeValue( AttributeKey.PanelOrder ).SplitDelimitedValues( false ).ToList();

            var missingPanels = panels.Except( panelOrder ).ToList();
            panelOrder.AddRange( missingPanels );

            var showFamilyMember = GetAttributeValue( AttributeKey.ShowFamilyMembers ).AsBoolean();
            var showAddress = GetAttributeValue( AttributeKey.Address );

            Panel pnlPerson = GeneratePanel( "Person" );
            Panel pnlContact = GeneratePanel( "Contact" );
            Panel pnlFamily = GeneratePanel( "Family" );
            Panel pnlFamilyMember = GeneratePanel( "FamilyMember" );

            var addressMergeFields = new Dictionary<string, object>();
            addressMergeFields.Add( "Type", "" );
            Panel pnlAddress = GeneratePanel( "Address", GetAttributeValue( AttributeKey.AddressFieldsHeader ).ResolveMergeFields( addressMergeFields ) );
            pnlAddress.Visible = false;

            var pnlControls = new List<Panel> { pnlPerson, pnlContact, pnlFamily, pnlFamilyMember, pnlAddress };

            foreach ( var panel in panelOrder )
            {
                var panelById = pnlControls.FirstOrDefault( p => p.ID == $"pnl{panel}" );
                if ( panelById != null )
                {
                    pnlProfilePanels.Controls.Add( panelById );
                }
            }
            pnlFamilyMember.Visible = showFamilyMember;

            var selectedFamily = CurrentPerson.GetFamily();
            hfGroupId.Value = selectedFamily.Id.ToString();

            if ( !Page.IsPostBack )
            {
                FamilyMembers = new List<PersonFamilyMember>();
                foreach ( var groupMember in selectedFamily.Members.Where( gm => gm.PersonId != CurrentPerson.Id ) )
                {
                    FamilyMembers.Add( new PersonFamilyMember { Guid = groupMember.Person.Guid, PersonId = groupMember.Person.Id, Person = groupMember.Person } );
                }
            }

            #region Person Fields

            Control pnlFamilyBody = pnlFamily.FindControl( "pnlFamilyBody" );

            GeneratePersonFields( CurrentPerson.Guid, pnlPerson, pnlFamilyBody );

            #endregion

            #region Family Fields

            var rockContext = new RockContext();
            var groupId = hfGroupId.Value.AsIntegerOrNull();

            if ( !groupId.HasValue )
            {
                groupId = CurrentPerson.GetFamily().Id;

                if ( groupId == 0 )
                {
                    return;
                }
                hfGroupId.Value = groupId.ToString();
            }

            var group = new GroupService( rockContext ).Get( groupId.Value );
            if ( group == null )
            {
                // invalid situation; return and report nothing.
                return;
            }

            var avcFamilyAttributes = new AttributeValuesContainer { ID = "avcFamilyAttributes" };
            var familyAttributesGuidList = GetAttributeValue( AttributeKey.FamilyAttributes ).SplitDelimitedValues().AsGuidList();

            avcFamilyAttributes.IncludedAttributes = familyAttributesGuidList.Select( a => AttributeCache.Get( a ) ).ToArray();
            avcFamilyAttributes.AddEditControls( group, true );

            pnlFamilyBody.Controls.Add( avcFamilyAttributes );

            pnlFamily.Visible = familyAttributesGuidList.Any() || GetAttributeValue( AttributeKey.CampusSelector ) == "Show with Family";

            #endregion

            Guid? locationTypeGuid = GetAttributeValue( AttributeKey.AddressType ).AsGuidOrNull();
            var acAddress = new AddressControl { ID = "acAddress", Required = showAddress == "Required", ValidationGroup = BlockValidationGroup };
            if ( locationTypeGuid.HasValue )
            {
                pnlAddress.Visible = showAddress != "Hide";

                var addressTypeDv = DefinedValueCache.Get( locationTypeGuid.Value );

                addressMergeFields = new Dictionary<string, object>();
                addressMergeFields.Add( "Type", addressTypeDv.Value ); // make this dynamic

                var hdrAddress = pnlAddress.FindControl( "hdrAddress" ) as WebControl;
                if ( hdrAddress != null && acAddress.Required )
                {
                    hdrAddress.Controls.Clear();
                    hdrAddress.Controls.Add( new LiteralControl( GetAttributeValue( AttributeKey.AddressFieldsHeader ).ResolveMergeFields( addressMergeFields ) ) );
                    hdrAddress.AddCssClass( "required-indicator" );
                }

                var cbIsMailingAddress = new RockCheckBox { ID = "cbIsMailingAddress", Text = "This is my mailing address" };
                var familyAddress = new GroupLocationService( rockContext ).Queryable()
                                    .Where( l => l.GroupId == groupId.Value
                                            && l.GroupLocationTypeValueId == addressTypeDv.Id
                                            && l.Group.Members.Any( m => m.PersonId == CurrentPerson.Id ) )
                                    .FirstOrDefault();
                if ( familyAddress != null )
                {
                    acAddress.SetValues( familyAddress.Location );
                    cbIsMailingAddress.Checked = familyAddress.IsMailingLocation;
                }

                var pnlAddressBody = pnlAddress.FindControl( "pnlAddressBody" );
                if ( pnlAddressBody != null )
                {
                    pnlAddressBody.Controls.Add( acAddress );
                    pnlAddressBody.Controls.Add( cbIsMailingAddress );
                }
            }

            #region Contact Fields

            GenerateContactFields( CurrentPerson.Guid, pnlContact );

            #endregion

            #region Build Family Member Panel

            if ( pnlFamilyMember.Visible )
            {
                var pnlFamilyMemberBody = pnlFamilyMember.FindControl( "pnlFamilyMemberBody" );
                if ( pnlFamilyMemberBody != null )
                {
                    var pnlFamilyMemberHeader = pnlFamilyMember.FindControl( "pnlFamilyMemberHeader" );
                    var lbAddFamilyMember = new LinkButton
                    {
                        ID = "lbAddFamilyMember",
                        CssClass = "btn btn-primary btn-sm mt-2 pull-right",
                        CausesValidation = false
                    };
                    lbAddFamilyMember.Click += lbAddFamilyMember_Click;
                    lbAddFamilyMember.Controls.Add( new LiteralControl( "<i class='fa fa-plus'></i>" ) );
                    lbAddFamilyMember.Visible = GetAttributeValue( AttributeKey.AllowAddingFamilyMembers ).AsBoolean();
                    if ( pnlFamilyMemberHeader != null )
                    {
                        pnlFamilyMemberHeader.Controls.AddAt( 0, lbAddFamilyMember );
                    }
                    else
                    {
                        pnlFamilyMemberBody.Controls.AddAt( 0, lbAddFamilyMember );
                    }

                    var pnlFamilyMemberWidgets = new Panel { ID = "pnlFamilyMemberWidgets" };
                    pnlFamilyMemberBody.Controls.Add( pnlFamilyMemberWidgets );

                    foreach ( var fm in FamilyMembers )
                    {
                        AddFamilyMember( pnlFamilyMemberWidgets, fm );
                    }
                }
            }
            #endregion

        }

        private void AddFamilyMember( Control pnlFamilyMemberBody, PersonFamilyMember fm, bool expanded = false )
        {
            var pwFamilyMember = new PanelWidget
            {
                ID = $"pwFamilyMember_{fm.PersonId}",
                Title = $"<h6>{fm.Person.FullName}<br><small>{fm.Person.GetFamilyRole()}</small></h6>",
                CssClass = "family-member",
                Expanded = expanded,
                ShowDeleteButton = fm.Guid == Guid.Empty
            };
            pwFamilyMember.DeleteClick += pwFamilyMember_DeleteClick;

            var pnlFamilyMemberPerson = new Panel { ID = "pnlFamilyMemberPerson", CssClass = "d-flex flex-wrap" };
            pwFamilyMember.Controls.Add( pnlFamilyMemberPerson );

            GeneratePersonFields( fm.Guid, pnlFamilyMemberPerson, null, true );
            GenerateContactFields( fm.Guid, pnlFamilyMemberPerson, true );

            pnlFamilyMemberBody.Controls.Add( pwFamilyMember );

            if ( expanded )
            {
                pwFamilyMember.Focus();
                ScriptManager.RegisterStartupScript( pwFamilyMember, pwFamilyMember.GetType(), $"focus-{pwFamilyMember.ClientID}", $"$('html, body').animate({{scrollTop: $('#{pwFamilyMember.ClientID}').offset().top }}, 1000);", true );
            }
        }

        private void GeneratePersonFields( Guid personGuid, Panel pnlPerson, Control pnlFamilyBody, bool familyMember = false )
        {
            WebControl lSpacer = new WebControl( HtmlTextWriterTag.Span ) { ID = "PersonSpacer" };
            var childGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid();

            RockContext rockContext = new RockContext();

            var groupId = hfGroupId.Value.AsIntegerOrNull();

            if ( !groupId.HasValue )
            {
                groupId = CurrentPerson.GetFamily().Id;

                if ( groupId == 0 )
                {
                    return;
                }

                hfGroupId.Value = groupId.ToString();
            }

            if ( hfPrimaryPersonGuid.Value.IsNullOrWhiteSpace() && personGuid != Guid.Empty )
            {
                hfPrimaryPersonGuid.Value = personGuid.ToString();
            }

            var group = new GroupService( rockContext ).Get( groupId.Value );
            if ( group == null )
            {
                return;
            }

            var personFields = ListSource.PersonFields.SplitDelimitedValues( false ).ToList();
            var personFieldsOrder = GetAttributeValue( AttributeKey.PersonFieldsOrder ).SplitDelimitedValues( false ).ToList();

            var matchPersonFieldsFamilyMember = GetAttributeValue( AttributeKey.MatchPersonFieldsFamilyMember ).AsBoolean();
            if ( familyMember )
            {
                personFields = ( ListSource.PersonFields + "," + ListSource.ContactFields ).Replace( ",Spacer", "" ).SplitDelimitedValues( false ).ToList();
                personFieldsOrder = GetAttributeValue( AttributeKey.FamilyMemberFieldsOrder ).SplitDelimitedValues( false ).ToList();
                if ( personFieldsOrder.Any() && !matchPersonFieldsFamilyMember )
                {
                    personFields = personFieldsOrder;
                }
            }

            var missingPersonFields = personFields.Except( personFieldsOrder ).ToList();
            personFieldsOrder.AddRange( missingPersonFields );

            var imagePhotoEditor = GenerateControl( "Photo", typeof( ImageEditor ), "" ) as ImageEditor;
            var dvpTitle = GenerateControl( "Title", typeof( DefinedValuePicker ), "Title", "input-width-md", DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_TITLE ) ).Id ) as DefinedValuePicker;
            var tbFirstName = GenerateControl( "FirstName", typeof( RockTextBox ), "First Name" ) as RockTextBox;
            var tbNickName = GenerateControl( "NickName", typeof( RockTextBox ), "Nick Name" ) as RockTextBox;
            var tbLastName = GenerateControl( "LastName", typeof( RockTextBox ), "Last Name" ) as RockTextBox;
            var dvpSuffix = GenerateControl( "Suffix", typeof( DefinedValuePicker ), "Suffix", "input-width-md", DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_SUFFIX ) ).Id ) as DefinedValuePicker;
            var bpBirthday = GenerateControl( "Birthday", typeof( BirthdayPicker ), "Birthday" ) as BirthdayPicker;
            var ypGraduation = GenerateControl( "Graduation", typeof( YearPicker ), "", "hide" ) as YearPicker;
            var ddlGrade = GenerateControl( "Grade", typeof( GradePicker ), "Grade" ) as GradePicker;
            var ddlGender = GenerateControl( "Gender", typeof( RockDropDownList ), "Gender" ) as RockDropDownList;
            var rblRole = GenerateControl( "Role", typeof( RockRadioButtonList ), "Role" ) as RockRadioButtonList;
            var rpRace = GenerateControl( "Race", typeof( RacePicker ), null ) as RacePicker;
            var epEthnicity = GenerateControl( "Ethnicity", typeof( EthnicityPicker ), null ) as EthnicityPicker;
            var dvpMaritalStatus = GenerateControl( "MaritalStatus", typeof( DefinedValuePicker ), "Marital Status", "input-width-md", DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS ) ).Id ) as DefinedValuePicker;
            var cpCampus = GenerateControl( "Campus", typeof( CampusPicker ), GetAttributeValue( AttributeKey.CampusSelectorLabel ) ) as CampusPicker;
            var avcPersonAttributes = new AttributeValuesContainer { ID = "avcPersonAttributes", NumberOfColumns = 2 };
            var avcChildPersonAttributes = new AttributeValuesContainer { ID = "avcChildPersonAttributes", NumberOfColumns = 2 };

            var personFieldControls = new List<Control> { imagePhotoEditor, dvpTitle, tbFirstName, tbNickName, tbLastName, dvpSuffix, bpBirthday, ypGraduation, ddlGrade, rblRole, ddlGender, rpRace, epEthnicity, dvpMaritalStatus, avcPersonAttributes, avcChildPersonAttributes, lSpacer };

            var person = new Person();

            if ( personGuid == Guid.Empty )
            {
                tbFirstName.Visible = true;
                tbLastName.Visible = true;
                tbFirstName.Enabled = true;
                tbLastName.Enabled = true;
                tbFirstName.Required = true;
                tbLastName.Required = true;
            }
            else
            {
                rblRole.Visible = false;
                person = new PersonService( rockContext ).Get( personGuid );
            }

            if ( person == null )
            {
                return;
            }

            var selectedFamily = person.GetFamily();
            if ( selectedFamily == null && groupId.HasValue )
            {
                var personFamilies = CurrentPerson.GetFamilies();
                selectedFamily = personFamilies.FirstOrDefault( a => a.Id == groupId );
            }

            if ( selectedFamily == null )
            {
                selectedFamily = CurrentPerson.GetFamily();
            }

            // Setup specific control settings

            imagePhotoEditor.ButtonText = "<i class='fa fa-camera'></i>";
            imagePhotoEditor.BinaryFileTypeGuid = new Guid( Rock.SystemGuid.BinaryFiletype.PERSON_IMAGE );
            imagePhotoEditor.Visible = GetAttributeValue( AttributeKey.AllowPhotoEditing ).AsBoolean();
            imagePhotoEditor.BinaryFileId = person.PhotoId;
            imagePhotoEditor.NoPictureUrl = Person.GetPersonNoPictureUrl( person, 200, 200 );

            dvpTitle.SetValue( person.TitleValueId );
            tbFirstName.Text = person.FirstName;
            tbNickName.Text = person.NickName;
            tbLastName.Text = person.LastName;
            dvpSuffix.SetValue( person.SuffixValueId );
            bpBirthday.SelectedDate = person.BirthDate;

            ddlGrade.UseAbbreviation = true;
            ddlGrade.UseGradeOffsetAsValue = true;

            ddlGender.Items.Add( new ListItem() );
            ddlGender.Items.Add( new ListItem( "Male" ) );
            ddlGender.Items.Add( new ListItem( "Female" ) );
            ddlGender.SelectedValue = person.Gender == Gender.Unknown ? string.Empty : person.Gender.ConvertToString();

            rblRole.SelectedIndexChanged += rblRole_SelectedIndexChanged;
            rblRole.AutoPostBack = true;
            rblRole.RepeatDirection = RepeatDirection.Horizontal;
            rblRole.Items.Clear();
            var familyRoles = selectedFamily.GroupType.Roles.OrderBy( r => r.Order ).ToList();
            foreach ( var role in familyRoles )
            {
                rblRole.Items.Add( new ListItem( role.Name, role.Id.ToString() ) );
            }

            if ( personGuid == Guid.Empty )
            {
                rblRole.SelectedValue = null;
            }
            else
            {
                rblRole.SetValue( person.GetFamilyRole() );
            }

            var childRoleId = familyRoles.FirstOrDefault( r => r.Guid == childGuid )?.Id;

            if ( group.Members.Where( gm => gm.PersonId == person.Id && gm.GroupRole.Guid == childGuid ).Any() || rblRole.SelectedValueAsInt() == childRoleId )
            {
                cpCampus.Visible = false;
                dvpMaritalStatus.Visible = false;

                if ( person.GraduationYear.HasValue )
                {
                    ypGraduation.SelectedYear = person.GraduationYear.Value;
                }
                else
                {
                    ypGraduation.SelectedYear = null;
                }

                ddlGrade.Visible = GetAttributeValue( AttributeKey.Grade ) != "Hide";
                if ( !person.HasGraduated ?? false )
                {
                    int gradeOffset = person.GradeOffset.Value;
                    var maxGradeOffset = ddlGrade.MaxGradeOffset;

                    // keep trying until we find a Grade that has a gradeOffset that includes the Person's gradeOffset (for example, there might be combined grades)
                    while ( !ddlGrade.Items.OfType<ListItem>().Any( a => a.Value.AsInteger() == gradeOffset ) && gradeOffset <= maxGradeOffset )
                    {
                        gradeOffset++;
                    }

                    ddlGrade.SetValue( gradeOffset );
                }
                else
                {
                    ddlGrade.SelectedIndex = 0;
                }
            }
            else
            {
                ddlGrade.Visible = false;
                dvpMaritalStatus.SetValue( person.MaritalStatusValueId );
            }

            rpRace.SetValue( person.RaceValueId );
            epEthnicity.SetValue( person.EthnicityValueId );

            if ( familyMember )
            {
                cpCampus.Visible = false;
            }

            switch ( GetAttributeValue( AttributeKey.CampusSelector ) )
            {
                case "Show with Person":
                    personFieldControls.Add( cpCampus );
                    break;
                case "Show with Family":
                    if ( pnlFamilyBody != null )
                    {
                        pnlFamilyBody.Controls.Add( cpCampus );
                    }
                    break;
            }

            if ( cpCampus.Visible )
            {
                cpCampus.Campuses = CampusCache.All( false );

                var selectedCampusTypeIds = GetAttributeValue( AttributeKey.CampusTypes )
                    .SplitDelimitedValues( true )
                    .AsGuidList()
                    .Select( a => DefinedValueCache.Get( a ) )
                    .Where( a => a != null )
                    .Select( a => a.Id )
                    .ToList();

                if ( selectedCampusTypeIds.Any() )
                {
                    cpCampus.CampusTypesFilter = selectedCampusTypeIds;
                }

                var selectedCampusStatusIds = GetAttributeValue( AttributeKey.CampusStatuses )
                    .SplitDelimitedValues( true )
                    .AsGuidList()
                    .Select( a => DefinedValueCache.Get( a ) )
                    .Where( a => a != null )
                    .Select( a => a.Id )
                    .ToList();

                if ( selectedCampusStatusIds.Any() )
                {
                    cpCampus.CampusStatusFilter = selectedCampusStatusIds;
                }

                cpCampus.SelectedCampusId = person.PrimaryCampusId;
            }

            var personChildAttributeGuidList = GetAttributeValue( AttributeKey.PersonAttributesChildren ).SplitDelimitedValues().AsGuidList();
            avcChildPersonAttributes.IncludedAttributes = personChildAttributeGuidList.Select( a => AttributeCache.Get( a ) ).ToArray();
            avcChildPersonAttributes.ShowCategoryLabel = false;
            avcChildPersonAttributes.NumberOfColumns = 1;
            avcChildPersonAttributes.AddEditControls( person, true );

            var personAttributeGuidList = GetAttributeValue( AttributeKey.PersonAttributesAdults ).SplitDelimitedValues().AsGuidList();
            avcPersonAttributes.IncludedAttributes = personAttributeGuidList.Select( a => AttributeCache.Get( a ) ).ToArray();
            avcPersonAttributes.ShowCategoryLabel = false;
            avcPersonAttributes.NumberOfColumns = 1;
            avcPersonAttributes.AddEditControls( person, true );

            if ( rblRole.SelectedValueAsInt() == childRoleId )
            {
                avcPersonAttributes.Visible = false;
                avcChildPersonAttributes.Visible = true;
            }
            else
            {
                avcPersonAttributes.Visible = true;
                avcChildPersonAttributes.Visible = false;
            }

            // End Setup Specific Control Settings


            // Add Controls to Panel in order in rows and columns

            var pnlBody = pnlPerson.FindControl( "pnlPersonBody" );
            var pnlFields = new Panel { ID = $"pnlFields_{person.Id}", CssClass = "d-flex flex-wrap" };

            if ( pnlBody == null )
            {
                pnlFields = pnlPerson;
            }
            else
            {
                pnlBody.Controls.Add( pnlFields );
            }

            var visibleControlCount = 0;
            foreach ( var ctrl in personFieldsOrder )
            {
                var actualCtrls = personFieldControls.Where( f => f.ID.EndsWith( ctrl ) );
                foreach ( var actualCtrl in actualCtrls )
                {
                    var actualWebCtrl = actualCtrl as WebControl;
                    if ( actualCtrl != null )
                    {
                        var personFieldCol = new Panel { CssClass = actualCtrl.Visible ? "col-md-6" : "" };
                        if ( actualCtrl.Visible )
                        {
                            visibleControlCount++;
                        }
                        if ( actualWebCtrl != null && !actualWebCtrl.CssClass.Contains( "hide" ) )
                        {
                            pnlFields.Controls.Add( personFieldCol );
                            personFieldCol.Controls.Add( actualCtrl );
                        }
                        else
                        {
                            visibleControlCount--;
                            pnlFields.Controls.Add( actualCtrl );
                        }
                    }
                }
            }

            if ( pnlFields != null && visibleControlCount < 1 )
            {
                pnlPerson.Visible = false;
            }
        }


        private void GenerateContactFields( Guid personGuid, Panel pnlContact, bool familyMember = false )
        {
            var childGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid();
            var groupTypeFamily = GroupTypeCache.GetFamilyGroupType();
            var isChild = false;

            var rockContext = new RockContext();

            var person = new Person();

            if ( personGuid != Guid.Empty )
            {
                person = new PersonService( rockContext ).Get( personGuid );
            }

            if ( person == null )
            {
                return;
            }

            WebControl lSpacer = new WebControl( HtmlTextWriterTag.Span ) { ID = "ContactSpacer" };

            var contactFields = ListSource.ContactFields.SplitDelimitedValues( false ).ToList();
            var contactFieldsOrder = GetAttributeValue( AttributeKey.ContactFieldsOrder ).SplitDelimitedValues( false ).ToList();

            var matchPersonFieldsFamilyMember = GetAttributeValue( AttributeKey.MatchPersonFieldsFamilyMember ).AsBoolean();
            if ( familyMember )
            {
                contactFields = ( ListSource.PersonFields + "," + ListSource.ContactFields ).Replace( ",Spacer", "" ).SplitDelimitedValues( false ).ToList();
                contactFieldsOrder = GetAttributeValue( AttributeKey.FamilyMemberFieldsOrder ).SplitDelimitedValues( false ).ToList();
                if ( contactFieldsOrder.Any() && !matchPersonFieldsFamilyMember )
                {
                    contactFields = contactFieldsOrder;
                }
            }

            var missingContactFields = contactFields.Except( contactFieldsOrder ).ToList();
            contactFieldsOrder.AddRange( missingContactFields );

            var rblRole = pnlContact.FindControl( "rblRole" ) as RockRadioButtonList;

            var ebEmail = GenerateControl( "Email", typeof( EmailBox ), "Email" ) as EmailBox;
            var rblCommunicationPreference = GenerateControl( "CommunicationPreference", typeof( RockRadioButtonList ), "Communication Preference" ) as RockRadioButtonList;
            var rblEmailPreference = GenerateControl( "EmailPreference", typeof( RockRadioButtonList ), "Email Preference" ) as RockRadioButtonList;

            var contactFieldsControls = new List<Control> { ebEmail, rblCommunicationPreference, rblEmailPreference, lSpacer };

            ebEmail.Text = person.Email;
            if ( rblRole != null && groupTypeFamily.Roles.Where( gr => gr.Guid == childGuid && gr.Id == rblRole.SelectedValueAsInt() ).Any() )
            {
                isChild = true;
                ebEmail.Required = GetAttributeValue( AttributeKey.Email ) == "Required";
            }

            var phoneNumbers = new List<PhoneNumber>();
            var phoneNumberTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE ) );
            var mobilePhoneType = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ) );
            var selectedPhoneTypeGuids = GetAttributeValues( AttributeKey.PhoneTypes ).AsGuidList();
            var requiredPhoneTypes = GetAttributeValues( AttributeKey.RequiredAdultPhoneTypes ).AsGuidList();

            if ( phoneNumberTypes.DefinedValues.Where( pnt => selectedPhoneTypeGuids.Contains( pnt.Guid ) ).Any() )
            {
                foreach ( var phoneNumberType in phoneNumberTypes.DefinedValues.Where( pnt => selectedPhoneTypeGuids.Contains( pnt.Guid ) ) )
                {
                    var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneNumberType.Id );
                    if ( phoneNumber == null )
                    {
                        var numberType = new DefinedValue
                        {
                            Id = phoneNumberType.Id,
                            Value = phoneNumberType.Value,
                            Guid = phoneNumberType.Guid
                        };

                        phoneNumber = new PhoneNumber
                        {
                            NumberTypeValueId = numberType.Id,
                            NumberTypeValue = numberType,
                            IsMessagingEnabled = mobilePhoneType != null && phoneNumberType.Id == mobilePhoneType.Id
                        };
                    }
                    else
                    {
                        phoneNumber.NumberFormatted = PhoneNumber.FormattedNumber( phoneNumber.CountryCode, phoneNumber.Number );
                    }

                    phoneNumbers.Add( phoneNumber );

                    var pnlPhoneNumContainer = new Panel { CssClass = "form-group", ID = $"pnlContainer{phoneNumber.NumberTypeValueId}_Phone" };

                    var lblPhone = new Label
                    {
                        CssClass = "control-label",
                        Text = $"{phoneNumber.NumberTypeValue.Value} Phone"
                    };

                    var pnlPhoneNumControls = new Panel { CssClass = "controls" };

                    pnlPhoneNumContainer.Controls.Add( lblPhone );
                    pnlPhoneNumContainer.Controls.Add( pnlPhoneNumControls );

                    var hfPhoneType = new HiddenField
                    {
                        ID = $"hfPhoneType{phoneNumber.NumberTypeValueId}",
                        Value = phoneNumber.NumberTypeValueId.ToString()
                    };
                    var pnbPhone = new PhoneNumberBox
                    {
                        ID = $"pnbPhone{phoneNumber.NumberTypeValueId}",
                        CountryCode = phoneNumber.CountryCode,
                        Number = phoneNumber.NumberFormatted,
                        RequiredErrorMessage = $"{phoneNumber.NumberTypeValue.Value} phone is required",
                        Required = requiredPhoneTypes.Contains( phoneNumber.NumberTypeValue.Guid ) && !isChild,
                        ValidationGroup = BlockValidationGroup
                    };
                    var cbSms = new RockCheckBox
                    {
                        ID = $"cbSms{phoneNumber.NumberTypeValueId}",
                        Text = GetAttributeValue( AttributeKey.SMSEnableLabel ),
                        Checked = phoneNumber.IsMessagingEnabled,
                        ContainerCssClass = "mb-0",
                        CssClass = "js-sms-number",
                        Visible = GetAttributeValue( AttributeKey.ShowSMSEnable ).AsBoolean() && phoneNumber.NumberTypeValueId == Rock.Web.Cache.DefinedValueCache.GetId( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ) )
                    };
                    lblPhone.AssociatedControlID = pnbPhone.ID;

                    if ( pnbPhone.Required )
                    {
                        pnlPhoneNumContainer.AddCssClass( "required" );
                    }

                    pnlPhoneNumControls.Controls.Add( hfPhoneType );
                    pnlPhoneNumControls.Controls.Add( pnbPhone );
                    pnlPhoneNumControls.Controls.Add( cbSms );

                    contactFieldsControls.Add( pnlPhoneNumContainer );
                }
            }

            rblEmailPreference.Items.Clear();
            rblEmailPreference.Items.Add( new ListItem( "All Emails", "EmailAllowed" ) );
            rblEmailPreference.Items.Add( new ListItem( "Only Personalized", "NoMassEmails" ) );
            rblEmailPreference.Items.Add( new ListItem( "Do Not Email", "DoNotEmail" ) );
            rblEmailPreference.RepeatDirection = RepeatDirection.Horizontal;
            rblEmailPreference.SelectedValue = person.EmailPreference.ConvertToString( false );

            rblCommunicationPreference.Items.Clear();
            rblCommunicationPreference.Items.Add( new ListItem( "Email", "1" ) );
            rblCommunicationPreference.Items.Add( new ListItem( "SMS", "2" ) );
            rblCommunicationPreference.RepeatDirection = RepeatDirection.Horizontal;
            rblCommunicationPreference.SetValue( person.CommunicationPreference == CommunicationType.SMS ? "2" : "1" );

            if ( isChild )
            {
                var displayEmailPreference = GetAttributeValue( AttributeKey.EmailPreference );
                rblEmailPreference.Visible = displayEmailPreference == "Show on All";

                var displayCommunicationPreference = GetAttributeValue( AttributeKey.CommunicationPreference );
                rblCommunicationPreference.Visible = displayCommunicationPreference == "Show on All";
            }

            var pnlContactBody = pnlContact.FindControl( "pnlContactBody" );
            if ( pnlContactBody == null )
            {
                pnlContactBody = pnlContact;
            }

            var pnlFieldsContact = new Panel { ID = $"pnlFieldsContact_{person.Id}", CssClass = "d-flex flex-wrap" };
            if ( familyMember )
            {
                pnlFieldsContact = pnlContactBody as Panel;
            }
            else
            {
                pnlContactBody.Controls.Add( pnlFieldsContact );
            }

            var addedContactCtrls = 0;
            foreach ( var ctrl in contactFieldsOrder )
            {
                var actualCtrls = contactFieldsControls.Where( f => f.ID.EndsWith( ctrl ) );
                foreach ( var actualCtrl in actualCtrls )
                {
                    var actualWebCtrl = actualCtrl as WebControl;
                    if ( actualCtrl != null && actualCtrl.Visible )
                    {
                        var fieldCol = new Panel { CssClass = "col-md-6" };

                        if ( ( actualCtrl != null && actualWebCtrl == null ) || ( actualWebCtrl != null && !actualWebCtrl.CssClass.Contains( "hide" ) ) )
                        {
                            var index = contactFieldsOrder.IndexOf( ctrl );
                            if ( !pnlFieldsContact.ID.StartsWith( "pnlFieldsContact" ) && index <= pnlFieldsContact.Controls.Count )
                            {
                                pnlFieldsContact.Controls.AddAt( index + addedContactCtrls, fieldCol );
                            }
                            else
                            {
                                pnlFieldsContact.Controls.Add( fieldCol );
                            }
                            fieldCol.Controls.Add( actualCtrl );
                        }
                        else
                        {
                            pnlFieldsContact.Controls.Add( actualCtrl );
                        }
                    }
                }
            }

            if ( pnlFieldsContact != null && pnlFieldsContact.Controls.Cast<Control>().Count( control => control.Visible ) < 2 )
            {
                pnlContact.Visible = false;
            }
            else
            {
                var nbCommunicationPreferenceWarning = new NotificationBox { ID = "nbCommunicationPreferenceWarning", Visible = false };
                pnlFieldsContact.Controls.Add( nbCommunicationPreferenceWarning );
            }
        }

        private bool SavePerson( RockContext rockContext, ref Guid personGuid, int? groupId, Group group, Panel pnlPanel, out Person person )
        {
            var personService = new PersonService( rockContext );

            var imgPhoto = pnlPanel.FindControl( "imgPhoto" ) as ImageEditor;
            var dvpTitle = pnlPanel.FindControl( "dvpTitle" ) as DefinedValuePicker;
            var tbFirstName = pnlPanel.FindControl( "tbFirstName" ) as RockTextBox;
            var tbNickName = pnlPanel.FindControl( "tbNickName" ) as RockTextBox;
            var tbLastName = pnlPanel.FindControl( "tbLastName" ) as RockTextBox;
            var dvpSuffix = pnlPanel.FindControl( "dvpSuffix" ) as DefinedValuePicker;
            var rpRace = pnlPanel.FindControl( "rpRace" ) as RacePicker;
            var epEthnicity = pnlPanel.FindControl( "epEthnicity" ) as EthnicityPicker;
            var bpBirthday = pnlPanel.FindControl( "bpBirthday" ) as BirthdayPicker;
            var ddlGender = pnlPanel.FindControl( "ddlGender" ) as RockDropDownList;
            var ypGraduation = pnlPanel.FindControl( "ypGraduation" ) as YearPicker;
            var ddlGrade = pnlPanel.FindControl( "ddlGrade" ) as RockDropDownList;
            var cpCampus = pnlPanel.FindControl( "cpCampus" ) as CampusPicker;
            var rblRole = pnlPanel.FindControl( "rblRole" ) as RockRadioButtonList;
            var dvpMaritalStatus = pnlPanel.FindControl( "dvpMaritalStatus" ) as DefinedValuePicker;
            var avcPersonAttributes = pnlPanel.FindControl( "avcPersonAttributes" ) as AttributeValuesContainer;
            var avcChildPersonAttributes = pnlPanel.FindControl( "avcChildPersonAttributes" ) as AttributeValuesContainer;

            var ebEmail = pnlPanel.FindControl( "ebEmail" ) as EmailBox;
            var rblEmailPreference = pnlPanel.FindControl( "rblEmailPreference" ) as RockRadioButtonList;
            var rblCommunicationPreference = pnlPanel.FindControl( "rblCommunicationPreference" ) as RockRadioButtonList;
            var nbCommunicationPreferenceWarning = pnlPanel.FindControl( "nbCommunicationPreferenceWarning" ) as NotificationBox;

            if ( personGuid == Guid.Empty )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groupMember = new GroupMember() { Person = new Person(), Group = group, GroupId = group.Id };
                if ( dvpTitle != null )
                {
                    groupMember.Person.TitleValueId = dvpTitle.SelectedValueAsId();
                }
                if ( tbFirstName != null )
                {
                    groupMember.Person.FirstName = tbFirstName.Text;
                }
                if ( tbNickName != null )
                {
                    groupMember.Person.NickName = tbNickName.Text;
                }
                if ( tbLastName != null )
                {
                    groupMember.Person.LastName = tbLastName.Text;
                }
                if ( dvpSuffix != null )
                {
                    groupMember.Person.SuffixValueId = dvpSuffix.SelectedValueAsId();
                }
                if ( ddlGender != null )
                {
                    groupMember.Person.Gender = ddlGender.SelectedValue.IsNotNullOrWhiteSpace() ? ddlGender.SelectedValueAsEnum<Gender>() : Gender.Unknown;
                }
                if ( bpBirthday != null )
                {
                    DateTime? birthdate = bpBirthday.SelectedDate;
                    if ( birthdate.HasValue )
                    {
                        // If setting a future birthdate, subtract a century until birthdate is not greater than today.
                        var today = RockDateTime.Today;
                        while ( birthdate.Value.CompareTo( today ) > 0 )
                        {
                            birthdate = birthdate.Value.AddYears( -100 );
                        }
                    }

                    groupMember.Person.SetBirthDate( birthdate );
                }
                if ( ddlGrade != null && ddlGrade.Visible )
                {
                    groupMember.Person.GradeOffset = ddlGrade.SelectedValueAsInt();
                }
                if ( rblRole != null )
                {
                    var role = group.GroupType.Roles.Where( r => r.Id == ( rblRole.SelectedValueAsInt() ?? 0 ) ).FirstOrDefault();
                    if ( role != null )
                    {
                        groupMember.GroupRole = role;
                        groupMember.GroupRoleId = role.Id;
                    }
                }

                groupMember.Person.ConnectionStatusValueId = CurrentPerson.ConnectionStatusValueId;

                var headOfHousehold = GroupServiceExtensions.HeadOfHousehold( group.Members.AsQueryable() );
                if ( headOfHousehold != null )
                {
                    DefinedValueCache dvcRecordStatus = DefinedValueCache.Get( headOfHousehold.RecordStatusValueId ?? 0 );
                    if ( dvcRecordStatus != null )
                    {
                        groupMember.Person.RecordStatusValueId = dvcRecordStatus.Id;
                    }
                }

                if ( groupMember.GroupRole != null && groupMember.GroupRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() )
                {
                    groupMember.Person.GivingGroupId = group.Id;
                }

                groupMember.Person.IsEmailActive = true;
                groupMember.Person.EmailPreference = EmailPreference.EmailAllowed;
                groupMember.Person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;

                groupMemberService.Add( groupMember );
                rockContext.SaveChanges();
                personGuid = groupMember.Person.Guid;
            }

            person = personService.Get( personGuid );
            if ( person != null )
            {
                int? orphanedPhotoId = null;
                if ( imgPhoto != null && person.PhotoId != imgPhoto.BinaryFileId )
                {
                    orphanedPhotoId = person.PhotoId;
                    person.PhotoId = imgPhoto.BinaryFileId;
                }

                if ( dvpTitle != null )
                {
                    person.TitleValueId = dvpTitle.SelectedValueAsInt();
                }
                if ( tbFirstName != null )
                {
                    person.FirstName = tbFirstName.Text;
                }
                if ( tbNickName != null )
                {
                    person.NickName = tbNickName.Text;
                }
                if ( tbLastName != null )
                {
                    person.LastName = tbLastName.Text;
                }
                if ( dvpSuffix != null )
                {
                    person.SuffixValueId = dvpSuffix.SelectedValueAsInt();
                }
                if ( rpRace != null )
                {
                    person.RaceValueId = rpRace.SelectedValueAsId();
                }
                if ( epEthnicity != null )
                {
                    person.EthnicityValueId = epEthnicity.SelectedValueAsId();
                }
                if ( bpBirthday != null )
                {
                    var birthMonth = person.BirthMonth;
                    var birthDay = person.BirthDay;
                    var birthYear = person.BirthYear;

                    var birthday = bpBirthday.SelectedDate;
                    if ( birthday.HasValue )
                    {
                        // If setting a future birthdate, subtract a century until birthdate is not greater than today.
                        var today = RockDateTime.Today;
                        while ( birthday.Value.CompareTo( today ) > 0 )
                        {
                            birthday = birthday.Value.AddYears( -100 );
                        }

                        person.BirthMonth = birthday.Value.Month;
                        person.BirthDay = birthday.Value.Day;
                        if ( birthday.Value.Year != DateTime.MinValue.Year )
                        {
                            person.BirthYear = birthday.Value.Year;
                        }
                        else
                        {
                            person.BirthYear = null;
                        }
                    }
                    else
                    {
                        person.SetBirthDate( null );
                    }
                }

                if ( ddlGrade != null && ddlGrade.Visible )
                {
                    int? graduationYear = null;
                    if ( ypGraduation.SelectedYear.HasValue )
                    {
                        graduationYear = ypGraduation.SelectedYear.Value;
                    }

                    person.GraduationYear = graduationYear;
                }

                if ( ddlGender != null )
                {
                    person.Gender = ddlGender.SelectedValue.IsNotNullOrWhiteSpace() ? ddlGender.SelectedValueAsEnum<Gender>() : Gender.Unknown;
                }

                if ( cpCampus != null && cpCampus.Visible )
                {
                    var primaryFamily = person.GetFamily( rockContext );
                    if ( primaryFamily.CampusId != cpCampus.SelectedCampusId )
                    {
                        primaryFamily.CampusId = cpCampus.SelectedCampusId;
                    }
                }

                var selectedPhoneTypes = GetAttributeValues( AttributeKey.PhoneTypes ).AsGuidList();

                if ( selectedPhoneTypes.Any() )
                {
                    var phoneNumberTypeIds = new List<int>();

                    bool smsSelected = false;

                    var pnlContactBody = pnlProfilePanels.FindControl( "pnlContactBody" );

                    foreach ( Control ctrl in pnlContactBody.ControlsOfTypeRecursive<Panel>().Where( p => p.ID != null ) )
                    {
                        var phoneTypeId = ctrl.ID.Replace( "pnlContainer", "" ).Replace( "_Phone", "" );

                        HiddenField hfPhoneType = ctrl.FindControl( $"hfPhoneType{phoneTypeId}" ) as HiddenField;
                        PhoneNumberBox pnbPhone = ctrl.FindControl( $"pnbPhone{phoneTypeId}" ) as PhoneNumberBox;
                        CheckBox cbSms = ctrl.FindControl( $"cbSms{phoneTypeId}" ) as CheckBox;

                        if ( hfPhoneType != null
                            && pnbPhone != null
                            && cbSms != null )
                        {
                            if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbPhone.Number ) ) )
                            {
                                int phoneNumberTypeId;
                                if ( int.TryParse( hfPhoneType.Value, out phoneNumberTypeId ) )
                                {
                                    var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneNumberTypeId );
                                    if ( phoneNumber == null )
                                    {
                                        phoneNumber = new PhoneNumber { NumberTypeValueId = phoneNumberTypeId };
                                        person.PhoneNumbers.Add( phoneNumber );
                                    }

                                    phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode );
                                    phoneNumber.Number = PhoneNumber.CleanNumber( pnbPhone.Number );

                                    // Only allow one number to have SMS selected
                                    if ( smsSelected )
                                    {
                                        phoneNumber.IsMessagingEnabled = false;
                                    }
                                    else
                                    {
                                        phoneNumber.IsMessagingEnabled = cbSms.Checked;
                                        smsSelected = cbSms.Checked;
                                    }

                                    phoneNumberTypeIds.Add( phoneNumberTypeId );
                                }
                            }
                        }
                    }

                    var phoneNumberService = new PhoneNumberService( rockContext );

                    // Remove any duplicate numbers
                    var hasDuplicate = person.PhoneNumbers.GroupBy( pn => pn.Number ).Where( g => g.Count() > 1 ).Any();

                    if ( hasDuplicate )
                    {
                        var listOfValidNumbers = person.PhoneNumbers
                            .OrderBy( o => o.NumberTypeValueId )
                            .GroupBy( pn => pn.Number )
                            .Select( y => y.First() )
                            .ToList();
                        var removedNumbers = person.PhoneNumbers.Except( listOfValidNumbers ).ToList();
                        phoneNumberService.DeleteRange( removedNumbers );
                        person.PhoneNumbers = listOfValidNumbers;
                    }
                }

                if ( ebEmail != null )
                {
                    person.Email = ebEmail.Text.Trim();
                }
                if ( rblEmailPreference != null )
                {
                    person.EmailPreference = rblEmailPreference.SelectedValue.ConvertToEnum<EmailPreference>();
                }

                if ( rblCommunicationPreference != null )
                {
                    var communicationPreference = rblCommunicationPreference.SelectedValueAsEnum<CommunicationType>();

                    if ( rblCommunicationPreference.Visible && selectedPhoneTypes.Any() && communicationPreference == CommunicationType.SMS && nbCommunicationPreferenceWarning != null )
                    {
                        if ( !person.PhoneNumbers.Any( a => a.IsMessagingEnabled ) )
                        {
                            nbCommunicationPreferenceWarning.Text = "A phone number with SMS enabled is required when Communication Preference is set to SMS.";
                            nbCommunicationPreferenceWarning.NotificationBoxType = NotificationBoxType.Warning;
                            nbCommunicationPreferenceWarning.Visible = true;
                            return false;
                        }
                    }

                    person.CommunicationPreference = communicationPreference;
                }

                person.LoadAttributes();

                if ( avcPersonAttributes != null && avcPersonAttributes.Visible )
                {
                    avcPersonAttributes.GetEditValues( person );
                }

                if ( avcChildPersonAttributes != null && avcChildPersonAttributes.Visible )
                {
                    avcChildPersonAttributes.GetEditValues( person );
                }

                if ( person.IsValid )
                {
                    if ( rockContext.SaveChanges() > 0 )
                    {
                        if ( orphanedPhotoId.HasValue )
                        {
                            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                            var binaryFile = binaryFileService.Get( orphanedPhotoId.Value );
                            if ( binaryFile != null )
                            {
                                // marked the old images as IsTemporary so they will get cleaned up later
                                binaryFile.IsTemporary = true;
                                rockContext.SaveChanges();
                            }
                        }

                        // if they used the ImageEditor, and cropped it, the original file is still in BinaryFile. So clean it up.
                        if ( imgPhoto.CropBinaryFileId.HasValue )
                        {
                            if ( imgPhoto.CropBinaryFileId != person.PhotoId )
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
                    }

                    person.SaveAttributeValues( rockContext );

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private Panel GeneratePanel( string pnlName, string headerText = "", string cssClass = "", string bodyCssClass = "" )
        {
            if ( headerText.IsNullOrWhiteSpace() )
            {
                headerText = GetAttributeValue( $"{pnlName}FieldsHeader" );
            }

            var pnlParent = new Panel
            {
                ID = $"pnl{pnlName}",
                CssClass = $"card card-{pnlName.ToLower()} mb-3 {cssClass}"
            };

            if ( headerText.IsNotNullOrWhiteSpace() )
            {
                var pnlHeader = new Panel();
                pnlHeader.ID = $"pnl{pnlName}Header";
                pnlHeader.CssClass = $"card-header";

                var header = new WebControl( HtmlTextWriterTag.H5 );
                header.ID = $"hdr{pnlName}";
                header.Controls.Add( new LiteralControl( headerText ) );

                pnlHeader.Controls.Add( header );
                pnlParent.Controls.Add( pnlHeader );
            }

            var pnlBody = new Panel
            {
                ID = $"pnl{pnlName}Body",
                CssClass = $"card-body card-body-{pnlName.ToLower()} {bodyCssClass}"
            };
            pnlParent.Controls.Add( pnlBody );

            var pnlFooter = new Panel();
            pnlFooter.ID = $"pnl{pnlName}Footer";
            pnlFooter.CssClass = $"card-footer";

            var displayButtonsInPanels = GetAttributeValue( AttributeKey.DisplayButtonsInAllPanels ).AsBoolean();

            if ( displayButtonsInPanels )
            {
                pnlParent.Controls.Add( pnlFooter );

                var lbSave = new LinkButton
                {
                    ID = $"lbSave{pnlName}",
                    AccessKey = "s",
                    ToolTip = "Alt+s",
                    Text = "Save",
                    CssClass = "btn btn-primary",
                    ValidationGroup = BlockValidationGroup
                };
                lbSave.Click += lbSave_Click;
                pnlProfilePanels.DefaultButton = lbSave.ID;

                pnlFooter.Controls.Add( lbSave );

                var lbCancel = new LinkButton
                {
                    ID = $"lbCancel{pnlName}",
                    AccessKey = "c",
                    ToolTip = "Alt+c",
                    Text = "Cancel",
                    CssClass = "btn btn-link",
                    CausesValidation = false,
                    ValidationGroup = BlockValidationGroup
                };
                lbCancel.Click += lbCancel_Click;

                pnlFooter.Controls.Add( lbCancel );
            }

            return pnlParent;
        }

        private Control GenerateControl( string ctrlName, Type fieldType, string labelText = "", string cssClass = "", int configId = -1 )
        {
            var ctrl = new Control();

            var displayControl = GetAttributeValue( ctrlName );

            if ( fieldType == typeof( RockTextBox ) )
            {
                var textbox = new RockTextBox
                {
                    ID = $"tb{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    Required = displayControl.Contains( "Required" ),
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = textbox;
            }
            else if ( fieldType == typeof( EmailBox ) )
            {
                var textbox = new EmailBox
                {
                    ID = $"eb{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    Required = displayControl.Contains( "Required" ),
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = textbox;
            }
            else if ( fieldType == typeof( DefinedValuePicker ) )
            {
                var dvp = new DefinedValuePicker
                {
                    ID = $"dvp{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    DefinedTypeId = configId,
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = dvp;
            }
            else if ( fieldType == typeof( ImageEditor ) )
            {
                var imageEditor = new ImageEditor
                {
                    ID = $"img{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = imageEditor;
            }
            else if ( fieldType == typeof( BirthdayPicker ) )
            {
                var birthday = new BirthdayPicker
                {
                    ID = $"bp{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = birthday;
            }
            else if ( fieldType == typeof( RockDropDownList ) )
            {
                var ddlCtrl = new RockDropDownList
                {
                    ID = $"ddl{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = ddlCtrl;
            }
            else if ( fieldType == typeof( RockRadioButtonList ) )
            {
                var control = new RockRadioButtonList
                {
                    ID = $"rbl{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = control;
            }
            else if ( fieldType == typeof( GradePicker ) )
            {
                var control = new GradePicker
                {
                    ID = $"ddl{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = control;
            }
            else if ( fieldType == typeof( YearPicker ) )
            {
                var control = new YearPicker
                {
                    ID = $"yp{ctrlName}",
                    Label = labelText,
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };

                ctrl = control;
            }
            else if ( fieldType == typeof( RacePicker ) )
            {
                var control = new RacePicker
                {
                    ID = $"rp{ctrlName}",
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };
                if ( labelText != null )
                {
                    control.Label = labelText;
                }

                ctrl = control;
            }
            else if ( fieldType == typeof( EthnicityPicker ) )
            {
                var control = new EthnicityPicker
                {
                    ID = $"ep{ctrlName}",
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };
                if ( labelText != null )
                {
                    control.Label = labelText;
                }

                ctrl = control;
            }
            else if ( fieldType == typeof( CampusPicker ) )
            {
                var control = new CampusPicker
                {
                    ID = $"cp{ctrlName}",
                    CssClass = cssClass,
                    Required = displayControl == "Required",
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide",
                    ValidationGroup = BlockValidationGroup
                };
                if ( labelText != null )
                {
                    control.Label = labelText;
                }

                ctrl = control;
            }
            else if ( fieldType == typeof( Panel ) )
            {
                var control = new Panel
                {
                    ID = $"pnl{ctrlName}",
                    CssClass = cssClass,
                    Enabled = displayControl != "Disable",
                    Visible = displayControl != "Hide"
                };

                ctrl = control;
            }
            else if ( fieldType == typeof( PlaceHolder ) )
            {
                var control = new PlaceHolder
                {
                    ID = $"ph{ctrlName}",
                    Visible = displayControl != "Hide"
                };

                ctrl = control;
            }

            return ctrl;
        }

        #endregion Methods
    }

    public class PersonFamilyMember
    {
        public Guid Guid { get; set; }

        public int PersonId { get; set; }

        public Person Person { get; set; }
    }
}