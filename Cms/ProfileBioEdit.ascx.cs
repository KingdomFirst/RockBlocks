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

namespace RockWeb.Plugins.rocks_kfs.Cms
{
    #region Block Attributes

    [DisplayName( "Edit Profile" )]
    [Category( "KFS > Cms" )]
    [Description( "Customized Edit Profile block to have more control over the fields and interface." )]

    #endregion Block Attributes

    #region Block Settings

    [BooleanField(
        "Allow Photo Editing",
        Description = "Should you be able to edit the person photo?",
        DefaultBooleanValue = true,
        Key = AttributeKey.AllowPhotoEditing,
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
        ListSource = ListSource.HIDE_DISABLE_REQUIRED,
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
        DefaultValue = Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME,
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
        customValues: ListSource.PersonFields,
        order: 33 )]

    [ValueListField(
        name: "Contact Fields Order",
        description: "Set the order of the contact fields.",
        key: AttributeKey.ContactFieldsOrder,
        required: false,
        customValues: ListSource.ContactFields,
        order: 34 )]

    #endregion Block Settings
    public partial class ProfileBioEdit : Rock.Web.UI.RockBlock
    {
        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string AllowPhotoEditing = "AllowPhotoEditing";
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
            public const string CampusSelector = "CampusSelector";
            public const string CampusSelectorLabel = "CampusSelectorLabel";
            public const string CampusTypes = "CampusTypes";
            public const string CampusStatuses = "CampusStatuses";
            public const string PanelOrder = "PanelOrder";
            public const string PersonFieldsOrder = "PersonFieldsOrder";
            public const string ContactFieldsOrder = "ContactFieldsOrder";
        }

        private static class PageParameterKey
        {
        }

        private static class ListSource
        {
            public const string HIDE_OPTIONAL_REQUIREDADULT_REQUIREDBOTH = "Hide,Optional,Required Adult,Required Adult and Child";
            public const string HIDE_OPTIONAL_REQUIRED = "Hide,Optional,Required";
            public const string HIDE_DISABLE_REQUIRED = "Hide,Disable,Required";
            public const string Panels = "Person,Contact,Family,FamilyMember,Address";
            public const string PersonFields = "Title,FirstName,NickName,LastName,Suffix,Birthday,Gender,Race,Ethnicity,MaritalStatus,Campus";
            public const string ContactFields = "Phone,SmsEnabled,Email,EmailPreference,CommunicationPreference";
        }

        private static class AttributeCategory
        {
            public const string PersonFields = "Person Fields";
            public const string ContactFields = "Contact Fields";
            public const string Campus = "Campus";
            public const string Attributes = "Attributes";
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

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            confirmExit.Enabled = true;

            BuildForm();
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

            Panel pnlPerson = GeneratePanel( "Person" );
            Panel pnlContact = GeneratePanel( "Contact" );
            Panel pnlFamily = GeneratePanel( "Family" );
            Panel pnlFamilyMember = GeneratePanel( "FamilyMember" );

            var addressMergeFields = new Dictionary<string, object>();
            addressMergeFields.Add( "Type", "Home" ); // make this dynamic
            Panel pnlAddress = GeneratePanel( "Address", GetAttributeValue( AttributeKey.AddressFieldsHeader ).ResolveMergeFields( addressMergeFields ) );

            var pnlControls = new List<Panel> { pnlPerson, pnlContact, pnlFamily, pnlFamilyMember, pnlAddress };

            foreach ( var panel in panelOrder )
            {
                pnlProfilePanels.Controls.Add( pnlControls.FirstOrDefault( p => p.ID == $"pnl{panel}" ) );
            }

            pnlAddress.Visible = showFamilyMember;

        }

        private Panel GeneratePanel( string pnlName, string headerText = "", string cssClass = "", string bodyCssClass = "" )
        {
            if ( headerText.IsNullOrWhiteSpace() )
            {
                headerText = GetAttributeValue( $"{pnlName}FieldsHeader" );
            }

            var pnlParent = new Panel();
            pnlParent.ID = $"pnl{pnlName}";
            pnlParent.CssClass = $"card card-{pnlName.ToLower()} mb-3 {cssClass}";

            var pnlBody = new Panel();
            pnlBody.ID = $"pnl{pnlName}Body";
            pnlBody.CssClass = $"card-body card-body-{pnlName.ToLower()} {bodyCssClass}";
            pnlParent.Controls.Add( pnlBody );

            if ( headerText.IsNotNullOrWhiteSpace() )
            {
                //var pnlHeader = new Panel();
                //pnlHeader.ID = $"pnl{pnlName}Header";
                //pnlHeader.CssClass = $"card-header";

                var header = new LiteralControl( $"<h5>{headerText}</h5>" );
                pnlBody.Controls.Add( header );
            }

            return pnlParent;
        }

        private Control GenerateControl(string ctrlName, FieldType fieldType, string labelText = "", string cssClass = "")
        {
            var ctrl = new Control();

            switch ( fieldType )
            {

                case null:
                    break;
            }
               


            return ctrl;
        }

        #endregion Methods
    }
}