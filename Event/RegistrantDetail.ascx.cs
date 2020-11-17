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
// * For use with KFS Advanced Events.
// * Added ability to edit person fields and attributes.
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Field.Types;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Event
{
    #region Block Attributes

    [DisplayName( "Advanced Registrant Detail" )]
    [Category( "KFS > Advanced Event Registration" )]
    [Description( "Displays interface for editing the registration attribute values and fees for a given registrant." )]

    #endregion

    #region Block Settings

    [LinkedPage( "Add Family Link", "Select the page where a new family can be added. If specified, a link will be shown which will open in a new window when clicked", false, "6a11a13d-05ab-4982-a4c2-67a8b1950c74,af36e4c2-78c6-4737-a983-e7a78137ddc7", "", 2 )]
    [SecurityAction( "AddFamilies", "The roles and/or users that can add new families to the system." )]

    #endregion

    /// <summary>
    /// Displays interface for editing the registration attribute values and fees for a given registrant.
    /// </summary>

    public partial class RegistrantDetail : RockBlock
    {
        #region Properties

        /// <summary>
        /// Gets or sets the TemplateState
        /// </summary>
        /// <value>
        /// The state of the template.
        /// </value>
        private RegistrationTemplate TemplateState { get; set; }

        /// <summary>
        /// Gets or sets the RegistrantSate
        /// </summary>
        /// <value>
        /// The state of the registrant.
        /// </value>
        private RegistrantInfo RegistrantState { get; set; }

        /// <summary>
        /// Gets or sets the registration instance identifier.
        /// </summary>
        /// <value>
        /// The registration instance identifier.
        /// </value>
        private int RegistrationInstanceId { get; set; }

        /// <summary>
        /// Gets or sets the registration group.
        /// </summary>
        /// <value>
        /// The registration group.
        /// </value>
        private Group RegistrationGroup { get; set; }

        /// <summary>
        /// Gets or sets the bool if the registrant has been placed in group.
        /// </summary>
        /// <value>
        /// Gets or sets the bool if the registrant has been placed in group.
        /// </value>
        private bool RegistrantPlacedInGroup { get; set; } // all other registration fields stored as FieldValue objects

        #endregion

        #region Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            var json = ViewState["Template"] as string;
            if ( !string.IsNullOrWhiteSpace( json ) )
            {
                TemplateState = JsonConvert.DeserializeObject<RegistrationTemplate>( json );
            }

            json = ViewState["Registrant"] as string;
            if ( !string.IsNullOrWhiteSpace( json ) )
            {
                RegistrantState = JsonConvert.DeserializeObject<RegistrantInfo>( json );
            }

            json = ViewState["RegistrationGroup"] as string;
            if ( !string.IsNullOrWhiteSpace( json ) )
            {
                RegistrationGroup = JsonConvert.DeserializeObject<Group>( json );
            }

            RegistrationInstanceId = ViewState["RegistrationInstanceId"] as int? ?? 0;

            BuildControls( false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlRegistrantDetail );

            bool canAddFamilies = UserCanAdministrate || IsUserAuthorized( "AddFamilies" );
            string addFamilyUrl = this.LinkedPageUrl( "AddFamilyLink" );
            rcwAddNewFamily.Visible = ( !string.IsNullOrWhiteSpace( addFamilyUrl ) && canAddFamilies );
            if ( rcwAddNewFamily.Visible )
            {
                // force the link to open a new scrollable,resizable browser window (and make it work in FF, Chrome and IE) http://stackoverflow.com/a/2315916/1755417
                hlAddNewFamily.Attributes["onclick"] = string.Format( "javascript: window.open('{0}', '_blank', 'scrollbars=1,resizable=1,toolbar=1'); return false;", addFamilyUrl );
            }
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
                LoadState();
                BuildControls( true );
            }
            else
            {
                ParseControls();
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

            ViewState["Template"] = JsonConvert.SerializeObject( TemplateState, Formatting.None, jsonSetting );
            ViewState["Registrant"] = JsonConvert.SerializeObject( RegistrantState, Formatting.None, jsonSetting );
            ViewState["RegistrationGroup"] = JsonConvert.SerializeObject( RegistrationGroup, Formatting.None, jsonSetting );
            ViewState["RegistrationInstanceId"] = RegistrationInstanceId;
            return base.SaveViewState();
        }

        #endregion

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            if ( RegistrantState != null )
            {
                RockContext rockContext = new RockContext();
                var personService = new PersonService( rockContext );
                var registrantService = new RegistrationRegistrantService( rockContext );
                var registrantFeeService = new RegistrationRegistrantFeeService( rockContext );
                var registrationTemplateFeeService = new RegistrationTemplateFeeService( rockContext );
                RegistrationRegistrant registrant = null;
                if ( RegistrantState.Id > 0 )
                {
                    registrant = registrantService.Get( RegistrantState.Id );
                }

                var previousRegistrantPersonIds = registrantService.Queryable().Where( a => a.RegistrationId == RegistrantState.RegistrationId )
                                .Where( r => r.PersonAlias != null )
                                .Select( r => r.PersonAlias.PersonId )
                                .ToList();

                bool newRegistrant = false;
                var registrantChanges = new History.HistoryChangeList();
                var personChanges = new History.HistoryChangeList();

                if ( registrant == null )
                {
                    newRegistrant = true;
                    registrant = new RegistrationRegistrant();
                    registrant.RegistrationId = RegistrantState.RegistrationId;
                    registrantService.Add( registrant );
                    registrantChanges.AddChange( History.HistoryVerb.Add, History.HistoryChangeType.Record, "Registrant" );
                }

                if ( !registrant.PersonAliasId.Equals( ppPerson.PersonAliasId ) )
                {
                    string prevPerson = ( registrant.PersonAlias != null && registrant.PersonAlias.Person != null ) ?
                        registrant.PersonAlias.Person.FullName : string.Empty;
                    string newPerson = ppPerson.PersonName;
                    newRegistrant = true;
                    History.EvaluateChange( registrantChanges, "Person", prevPerson, newPerson );
                }
                int? personId = ppPerson.PersonId.Value;
                registrant.PersonAliasId = ppPerson.PersonAliasId.Value;

                // Get the name of registrant for history
                string registrantName = "Unknown";
                var person = new Person();
                if ( ppPerson.PersonId.HasValue )
                {
                    person = personService.Get( ppPerson.PersonId.Value );
                    if ( person != null )
                    {
                        registrantName = person.FullName;
                    }
                }

                // update group membership
                if ( RegistrationGroup != null )
                {
                    rockContext.WrapTransaction( () =>
                    {
                        var groupMemberService = new GroupMemberService( rockContext );
                        var groupMember = groupMemberService.Queryable()
                            .FirstOrDefault( m =>
                                m.GroupId == RegistrationGroup.Id &&
                                m.PersonId == person.Id &&
                                m.GroupRoleId == TemplateState.GroupMemberRoleId.Value );
                        if ( groupMember == null && RegistrantPlacedInGroup )
                        {
                            // add the group member
                            groupMember = new GroupMember();
                            groupMember.GroupId = RegistrationGroup.Id;
                            groupMember.PersonId = person.Id;
                            groupMember.GroupRoleId = TemplateState.GroupMemberRoleId.Value;
                            groupMember.GroupMemberStatus = TemplateState.GroupMemberStatus;
                            groupMemberService.Add( groupMember );
                            rockContext.SaveChanges();
                            registrant.GroupMember = groupMember;
                            registrant.GroupMemberId = groupMember.Id;
                            registrantChanges.AddChange( History.HistoryVerb.Add, History.HistoryChangeType.Record, string.Format( "Registrant to {0} group", RegistrationGroup.Name ) );
                        }
                        else if ( groupMember != null && !RegistrantPlacedInGroup )
                        {
                            // remove the group member
                            groupMemberService.Delete( groupMember );
                            rockContext.SaveChanges();
                            registrant.GroupMember = null;
                            registrantChanges.AddChange( History.HistoryVerb.RemovedFromGroup, History.HistoryChangeType.Record, string.Format( "Registrant from {0} group", RegistrationGroup.Name ) );
                        }
                    } );
                }

                // set their status (wait list / registrant)
                registrant.OnWaitList = !tglWaitList.Checked;

                History.EvaluateChange( registrantChanges, "Cost", registrant.Cost, cbCost.Text.AsDecimal() );
                registrant.Cost = cbCost.Text.AsDecimal();

                History.EvaluateChange( registrantChanges, "Discount Applies", registrant.DiscountApplies, cbDiscountApplies.Checked );
                registrant.DiscountApplies = cbDiscountApplies.Checked;

                if ( !Page.IsValid )
                {
                    return;
                }

                // Remove/delete any registrant fees that are no longer in UI with quantity
                foreach ( var dbFee in registrant.Fees.ToList() )
                {
                    if ( !RegistrantState.FeeValues.Keys.Contains( dbFee.RegistrationTemplateFeeId ) ||
                        RegistrantState.FeeValues[dbFee.RegistrationTemplateFeeId] == null ||
                        !RegistrantState.FeeValues[dbFee.RegistrationTemplateFeeId]
                            .Any( f =>
                                f.Option == dbFee.Option &&
                                f.Quantity > 0 ) )
                    {
                        registrantChanges.AddChange( History.HistoryVerb.Delete, History.HistoryChangeType.Record, "Fee" ).SetOldValue( string.Format( "Removed '{0}' Fee (Quantity:{1:N0}, Cost:{2:C2}, Option:{3}", dbFee.RegistrationTemplateFee.Name, dbFee.Quantity, dbFee.Cost, dbFee.Option ) );

                        registrant.Fees.Remove( dbFee );
                        registrantFeeService.Delete( dbFee );
                    }
                }

                // Add/Update any of the fees from UI
                foreach ( var uiFee in RegistrantState.FeeValues.Where( f => f.Value != null ) )
                {
                    foreach ( var uiFeeOption in uiFee.Value )
                    {
                        var dbFee = registrant.Fees
                            .Where( f =>
                                f.RegistrationTemplateFeeId == uiFee.Key &&
                                f.Option == uiFeeOption.Option )
                            .FirstOrDefault();

                        if ( dbFee == null )
                        {
                            dbFee = new RegistrationRegistrantFee();
                            dbFee.RegistrationTemplateFeeId = uiFee.Key;
                            dbFee.Option = uiFeeOption.Option;
                            registrant.Fees.Add( dbFee );
                        }

                        var templateFee = dbFee.RegistrationTemplateFee;
                        if ( templateFee == null )
                        {
                            templateFee = registrationTemplateFeeService.Get( uiFee.Key );
                        }

                        string feeName = templateFee != null ? templateFee.Name : "Fee";
                        if ( !string.IsNullOrWhiteSpace( uiFeeOption.Option ) )
                        {
                            feeName = string.Format( "{0} ({1})", feeName, uiFeeOption.Option );
                        }

                        if ( dbFee.Id <= 0 )
                        {
                            registrantChanges.AddChange( History.HistoryVerb.Add, History.HistoryChangeType.Record, "Fee" ).SetNewValue( feeName );
                        }

                        History.EvaluateChange( registrantChanges, feeName + " Quantity", dbFee.Quantity, uiFeeOption.Quantity );
                        dbFee.Quantity = uiFeeOption.Quantity;

                        History.EvaluateChange( registrantChanges, feeName + " Cost", dbFee.Cost, uiFeeOption.Cost );
                        dbFee.Cost = uiFeeOption.Cost;
                    }
                }

                if ( TemplateState.RequiredSignatureDocumentTemplate != null )
                {
                    var documentService = new SignatureDocumentService( rockContext );
                    var binaryFileService = new BinaryFileService( rockContext );
                    SignatureDocument document = null;

                    int? signatureDocumentId = hfSignedDocumentId.Value.AsIntegerOrNull();
                    int? binaryFileId = fuSignedDocument.BinaryFileId;
                    if ( signatureDocumentId.HasValue )
                    {
                        document = documentService.Get( signatureDocumentId.Value );
                    }

                    if ( document == null && binaryFileId.HasValue )
                    {
                        var instance = new RegistrationInstanceService( rockContext ).Get( RegistrationInstanceId );

                        document = new SignatureDocument();
                        document.SignatureDocumentTemplateId = TemplateState.RequiredSignatureDocumentTemplate.Id;
                        document.AppliesToPersonAliasId = registrant.PersonAliasId.Value;
                        document.AssignedToPersonAliasId = registrant.PersonAliasId.Value;
                        document.Name = string.Format( "{0}_{1}",
                            ( instance != null ? instance.Name : TemplateState.Name ),
                            ( person != null ? person.FullName.RemoveSpecialCharacters() : string.Empty ) );
                        document.Status = SignatureDocumentStatus.Signed;
                        document.LastStatusDate = RockDateTime.Now;
                        documentService.Add( document );
                    }

                    if ( document != null )
                    {
                        int? origBinaryFileId = document.BinaryFileId;
                        document.BinaryFileId = binaryFileId;

                        if ( origBinaryFileId.HasValue && origBinaryFileId.Value != document.BinaryFileId )
                        {
                            // if a new the binaryFile was uploaded, mark the old one as Temporary so that it gets cleaned up
                            var oldBinaryFile = binaryFileService.Get( origBinaryFileId.Value );
                            if ( oldBinaryFile != null && !oldBinaryFile.IsTemporary )
                            {
                                oldBinaryFile.IsTemporary = true;
                            }
                        }

                        // ensure the IsTemporary is set to false on binaryFile associated with this document
                        if ( document.BinaryFileId.HasValue )
                        {
                            var binaryFile = binaryFileService.Get( document.BinaryFileId.Value );
                            if ( binaryFile != null && binaryFile.IsTemporary )
                            {
                                binaryFile.IsTemporary = false;
                            }
                        }
                    }
                }

                if ( !registrant.IsValid )
                {
                    // Controls will render the error messages
                    return;
                }

                var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
                var adultRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();
                var childRoleId = familyGroupType.Roles
                    .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ) )
                    .Select( r => r.Id )
                    .FirstOrDefault();
                var multipleFamilyGroupIds = new Dictionary<Guid, int>();

                // use WrapTransaction since SaveAttributeValues does it's own RockContext.SaveChanges()
                rockContext.WrapTransaction( () =>
                {
                    rockContext.SaveChanges();

                    int? campusId = null;
                    Location location = null;

                    // Set any of the template's person fields
                    foreach ( var field in TemplateState.Forms
                    .SelectMany( f => f.Fields
                        .Where( t => t.FieldSource == RegistrationFieldSource.PersonField ) ) )
                    {
                        var fieldValue = RegistrantState.FieldValues
                            .Where( f => f.Key == field.Id )
                            .Select( f => f.Value.FieldValue )
                            .FirstOrDefault();

                        if ( fieldValue != null )
                        {
                            switch ( field.PersonFieldType )
                            {
                                case RegistrationPersonFieldType.Campus:
                                    {
                                        if ( fieldValue != null )
                                        {
                                            campusId = fieldValue.ToString().AsIntegerOrNull();
                                        }
                                        break;
                                    }

                                case RegistrationPersonFieldType.Address:
                                    {
                                        location = fieldValue as Location;
                                        break;
                                    }

                                case RegistrationPersonFieldType.Birthdate:
                                    {
                                        var birthMonth = person.BirthMonth;
                                        var birthDay = person.BirthDay;
                                        var birthYear = person.BirthYear;

                                        person.SetBirthDate( fieldValue as DateTime? );

                                        History.EvaluateChange( personChanges, "Birth Month", birthMonth, person.BirthMonth );
                                        History.EvaluateChange( personChanges, "Birth Day", birthDay, person.BirthDay );
                                        History.EvaluateChange( personChanges, "Birth Year", birthYear, person.BirthYear );

                                        break;
                                    }

                                case RegistrationPersonFieldType.Grade:
                                    {
                                        var newGraduationYear = fieldValue.ToString().AsIntegerOrNull();
                                        History.EvaluateChange( personChanges, "Graduation Year", person.GraduationYear, newGraduationYear );
                                        person.GraduationYear = newGraduationYear;

                                        break;
                                    }

                                case RegistrationPersonFieldType.Gender:
                                    {
                                        var newGender = fieldValue.ToString().ConvertToEnumOrNull<Gender>() ?? Gender.Unknown;
                                        History.EvaluateChange( personChanges, "Gender", person.Gender, newGender );
                                        person.Gender = newGender;
                                        break;
                                    }

                                case RegistrationPersonFieldType.MaritalStatus:
                                    {
                                        if ( fieldValue != null )
                                        {
                                            int? newMaritalStatusId = fieldValue.ToString().AsIntegerOrNull();
                                            History.EvaluateChange( personChanges, "Marital Status", DefinedValueCache.GetName( person.MaritalStatusValueId ), DefinedValueCache.GetName( newMaritalStatusId ) );
                                            person.MaritalStatusValueId = newMaritalStatusId;
                                        }
                                        break;
                                    }

                                case RegistrationPersonFieldType.MobilePhone:
                                    {
                                        SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid(), personChanges );
                                        break;
                                    }

                                case RegistrationPersonFieldType.HomePhone:
                                    {
                                        SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid(), personChanges );
                                        break;
                                    }

                                case RegistrationPersonFieldType.WorkPhone:
                                    {
                                        SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid(), personChanges );
                                        break;
                                    }

                                case RegistrationPersonFieldType.Email:
                                    {
                                        var newEmail = fieldValue.ToString() ?? string.Empty;
                                        History.EvaluateChange( personChanges, "Email", person.Email, newEmail );
                                        person.Email = newEmail;
                                        break;
                                    }
                            }
                        }
                    }

                    var family = person.GetFamily();
                    int? singleFamilyId = null;
                    singleFamilyId = family.Id;

                    // Save the person ( and family if needed )
                    SavePerson( rockContext, person, family.Guid, campusId, location, adultRoleId, childRoleId, multipleFamilyGroupIds, ref singleFamilyId );

                    // Load the person's attributes
                    person.LoadAttributes();

                    // Set any of the template's person fields
                    foreach ( var field in TemplateState.Forms
                        .SelectMany( f => f.Fields
                        .Where( t =>
                            t.FieldSource == RegistrationFieldSource.PersonAttribute &&
                            t.AttributeId.HasValue ) ) )
                    {
                        // Find the registrant's value
                        var fieldValue = RegistrantState.FieldValues
                        .Where( f => f.Key == field.Id )
                        .Select( f => f.Value.FieldValue )
                        .FirstOrDefault();

                        if ( fieldValue != null )
                        {
                            var attribute = AttributeCache.Get( field.AttributeId.Value );
                            if ( attribute != null )
                            {
                                string originalValue = person.GetAttributeValue( attribute.Key );
                                string newValue = fieldValue.ToString();
                                person.SetAttributeValue( attribute.Key, fieldValue.ToString() );

                                // DateTime values must be stored in ISO8601 format as http://www.rockrms.com/Rock/Developer/BookContent/16/16#datetimeformatting
                                if ( attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DATE.AsGuid() ) ||
                                attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DATE_TIME.AsGuid() ) )
                                {
                                    DateTime aDateTime;
                                    if ( DateTime.TryParse( newValue, out aDateTime ) )
                                    {
                                        newValue = aDateTime.ToString( "o" );
                                    }
                                }

                                if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                                {
                                    string formattedOriginalValue = string.Empty;
                                    if ( !string.IsNullOrWhiteSpace( originalValue ) )
                                    {
                                        formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                                    }

                                    string formattedNewValue = string.Empty;
                                    if ( !string.IsNullOrWhiteSpace( newValue ) )
                                    {
                                        formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                                    }

                                    Helper.SaveAttributeValue( person, attribute, newValue, rockContext );
                                    History.EvaluateChange( personChanges, attribute.Name, formattedOriginalValue, formattedNewValue );
                                }
                            }
                        }
                    }

                    //
                    // this causes duplicate phone number types to be added
                    // leaving old phone numbers and adding new phone numbers
                    //
                    //personChanges.ForEach( c => registrantChanges.Add( c ) );

                    rockContext.SaveChanges();

                    // Set any of the templat's registrant attributes
                    registrant.LoadAttributes();
                    foreach ( var field in TemplateState.Forms
                        .SelectMany( f => f.Fields
                            .Where( t =>
                                t.FieldSource == RegistrationFieldSource.RegistrationAttribute &&
                                t.AttributeId.HasValue ) ) )
                    {
                        // Find the registrant's value
                        var fieldValue = RegistrantState.FieldValues
                        .Where( f => f.Key == field.Id )
                        .Select( f => f.Value.FieldValue )
                        .FirstOrDefault();

                        if ( fieldValue != null )
                        {
                            var attribute = AttributeCache.Get( field.AttributeId.Value );
                            if ( attribute != null )
                            {
                                string originalValue = registrant.GetAttributeValue( attribute.Key );
                                string newValue = fieldValue.ToString();
                                registrant.SetAttributeValue( attribute.Key, fieldValue.ToString() );

                                // DateTime values must be stored in ISO8601 format as http://www.rockrms.com/Rock/Developer/BookContent/16/16#datetimeformatting
                                if ( attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DATE.AsGuid() ) ||
                                attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DATE_TIME.AsGuid() ) )
                                {
                                    DateTime aDateTime;
                                    if ( DateTime.TryParse( fieldValue.ToString(), out aDateTime ) )
                                    {
                                        newValue = aDateTime.ToString( "o" );
                                    }
                                }

                                if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                                {
                                    string formattedOriginalValue = string.Empty;
                                    if ( !string.IsNullOrWhiteSpace( originalValue ) )
                                    {
                                        formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                                    }

                                    string formattedNewValue = string.Empty;
                                    if ( !string.IsNullOrWhiteSpace( newValue ) )
                                    {
                                        formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                                    }

                                    Helper.SaveAttributeValue( registrant, attribute, newValue, rockContext );
                                    History.EvaluateChange( registrantChanges, attribute.Name, formattedOriginalValue, formattedNewValue );
                                }
                            }
                        }
                    }

                    // Set any of the template's group member attributes
                    registrant.GroupMember.LoadAttributes();

                    if ( registrant.GroupMember != null )
                    {
                        foreach ( var field in TemplateState.Forms
                            .SelectMany( f => f.Fields
                                .Where( t =>
                                    t.FieldSource == RegistrationFieldSource.GroupMemberAttribute &&
                                    t.AttributeId.HasValue ) ) )
                        {
                            // Find the registrant's value
                            var fieldValue = RegistrantState.FieldValues
                                .Where( f => f.Key == field.Id )
                                .Select( f => f.Value.FieldValue )
                                .FirstOrDefault();

                            if ( fieldValue != null )
                            {
                                var attribute = AttributeCache.Get( field.AttributeId.Value );
                                if ( attribute != null )
                                {
                                    string originalValue = registrant.GroupMember.GetAttributeValue( attribute.Key );
                                    string newValue = fieldValue.ToString();
                                    registrant.GroupMember.SetAttributeValue( attribute.Key, fieldValue.ToString() );

                                    if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                                    {
                                        string formattedOriginalValue = string.Empty;
                                        if ( !string.IsNullOrWhiteSpace( originalValue ) )
                                        {
                                            formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                                        }

                                        string formattedNewValue = string.Empty;
                                        if ( !string.IsNullOrWhiteSpace( newValue ) )
                                        {
                                            formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                                        }

                                        Helper.SaveAttributeValue( registrant.GroupMember, attribute, newValue, rockContext );
                                    }
                                }
                            }
                        }
                    }
                } );

                if ( newRegistrant && TemplateState.GroupTypeId.HasValue && ppPerson.PersonId.HasValue )
                {
                    using ( var newRockContext = new RockContext() )
                    {
                        var reloadedRegistrant = new RegistrationRegistrantService( newRockContext ).Get( registrant.Id );
                        if ( reloadedRegistrant != null &&
                            reloadedRegistrant.Registration != null &&
                            reloadedRegistrant.Registration.Group != null &&
                            reloadedRegistrant.Registration.Group.GroupTypeId == TemplateState.GroupTypeId.Value )
                        {
                            int? groupRoleId = TemplateState.GroupMemberRoleId.HasValue ?
                                TemplateState.GroupMemberRoleId.Value :
                                reloadedRegistrant.Registration.Group.GroupType.DefaultGroupRoleId;
                            if ( groupRoleId.HasValue )
                            {
                                var groupMemberService = new GroupMemberService( newRockContext );
                                var groupMember = groupMemberService
                                    .Queryable().AsNoTracking()
                                    .Where( m =>
                                        m.GroupId == reloadedRegistrant.Registration.Group.Id &&
                                        m.PersonId == reloadedRegistrant.PersonId &&
                                        m.GroupRoleId == groupRoleId.Value )
                                    .FirstOrDefault();
                                if ( groupMember == null )
                                {
                                    groupMember = new GroupMember();
                                    groupMemberService.Add( groupMember );
                                    groupMember.GroupId = reloadedRegistrant.Registration.Group.Id;
                                    groupMember.PersonId = ppPerson.PersonId.Value;
                                    groupMember.GroupRoleId = groupRoleId.Value;
                                    groupMember.GroupMemberStatus = TemplateState.GroupMemberStatus;

                                    newRockContext.SaveChanges();

                                    registrantChanges.AddChange( History.HistoryVerb.Add, History.HistoryChangeType.Record, string.Format( "Registrant to {0} group", reloadedRegistrant.Registration.Group.Name ) );
                                }
                                else
                                {
                                    registrantChanges.AddChange( History.HistoryVerb.Modify, History.HistoryChangeType.Record, string.Format( "Registrant to existing person in {0} group", reloadedRegistrant.Registration.Group.Name ) );
                                }

                                // Record this to the Person's and Registrants Notes and History...
                                reloadedRegistrant.Registration.SavePersonNotesAndHistory( reloadedRegistrant.Registration.PersonAlias.Person, this.CurrentPersonAliasId, previousRegistrantPersonIds );

                                reloadedRegistrant.GroupMemberId = groupMember.Id;
                                newRockContext.SaveChanges();
                            }
                        }
                    }
                }

                HistoryService.SaveChanges(
                    rockContext,
                    typeof( Registration ),
                    Rock.SystemGuid.Category.HISTORY_EVENT_REGISTRATION.AsGuid(),
                    registrant.RegistrationId,
                    registrantChanges,
                    "Registrant: " + registrantName,
                    null, null );
            }

            NavigateToRegistration();
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToRegistration();
        }

        protected void lbWizardTemplate_Click( object sender, EventArgs e )
        {
            var qryParams = new Dictionary<string, string>();
            var pageCache = PageCache.Get( RockPage.PageId );
            if ( pageCache != null &&
                pageCache.ParentPage != null &&
                pageCache.ParentPage.ParentPage != null &&
                pageCache.ParentPage.ParentPage.ParentPage != null )
            {
                qryParams.Add( "RegistrationTemplateId", TemplateState != null ? TemplateState.Id.ToString() : "0" );
                NavigateToPage( pageCache.ParentPage.ParentPage.ParentPage.Guid, qryParams );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbWizardInstance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbWizardInstance_Click( object sender, EventArgs e )
        {
            var qryParams = new Dictionary<string, string>();
            var pageCache = PageCache.Get( RockPage.PageId );
            if ( pageCache != null &&
                pageCache.ParentPage != null &&
                pageCache.ParentPage.ParentPage != null )
            {
                qryParams.Add( "RegistrationInstanceId", RegistrationInstanceId.ToString() );
                NavigateToPage( pageCache.ParentPage.ParentPage.Guid, qryParams );
            }
        }

        protected void lbWizardRegistration_Click( object sender, EventArgs e )
        {
            NavigateToRegistration();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            RegistrantState = null;
            LoadState();
            BuildControls( true );
        }

        #endregion

        #region Methods

        private void LoadState()
        {
            int? registrantId = PageParameter( "RegistrantId" ).AsIntegerOrNull();
            int? registrationId = PageParameter( "RegistrationId" ).AsIntegerOrNull();

            if ( RegistrantState == null )
            {
                var rockContext = new RockContext();
                RegistrationRegistrant registrant = null;

                if ( registrantId.HasValue && registrantId.Value != 0 )
                {
                    registrant = new RegistrationRegistrantService( rockContext )
                        .Queryable( "Registration.RegistrationInstance.RegistrationTemplate.Forms.Fields,Registration.RegistrationInstance.RegistrationTemplate.Fees,PersonAlias.Person,Fees" ).AsNoTracking()
                        .Where( r => r.Id == registrantId.Value )
                        .FirstOrDefault();

                    if ( registrant != null &&
                        registrant.Registration != null &&
                        registrant.Registration.RegistrationInstance != null &&
                        registrant.Registration.RegistrationInstance.RegistrationTemplate != null )
                    {
                        RegistrantState = new RegistrantInfo( registrant, rockContext );
                        TemplateState = registrant.Registration.RegistrationInstance.RegistrationTemplate;
                        RegistrationInstanceId = registrant.Registration.RegistrationInstanceId;

                        if ( registrant.Registration.Group != null )
                        {
                            RegistrationGroup = registrant.Registration.Group.Clone( false );
                        }

                        lWizardTemplateName.Text = registrant.Registration.RegistrationInstance.RegistrationTemplate.Name;
                        lWizardInstanceName.Text = registrant.Registration.RegistrationInstance.Name;
                        lWizardRegistrationName.Text = registrant.Registration.ToString();
                        lWizardRegistrantName.Text = registrant.ToString();

                        tglWaitList.Checked = !registrant.OnWaitList;
                    }
                }

                if ( TemplateState == null && registrationId.HasValue && registrationId.Value != 0 )
                {
                    var registration = new RegistrationService( rockContext )
                        .Queryable( "RegistrationInstance.RegistrationTemplate.Forms.Fields,RegistrationInstance.RegistrationTemplate.Fees" ).AsNoTracking()
                        .Where( r => r.Id == registrationId.Value )
                        .FirstOrDefault();

                    if ( registration != null &&
                        registration.RegistrationInstance != null &&
                        registration.RegistrationInstance.RegistrationTemplate != null )
                    {
                        TemplateState = registration.RegistrationInstance.RegistrationTemplate;
                        RegistrationInstanceId = registration.RegistrationInstanceId;
                        if ( registration.Group != null )
                        {
                            RegistrationGroup = registration.Group.Clone( false );
                        }

                        lWizardTemplateName.Text = registration.RegistrationInstance.RegistrationTemplate.Name;
                        lWizardInstanceName.Text = registration.RegistrationInstance.Name;
                        lWizardRegistrationName.Text = registration.ToString();
                        lWizardRegistrantName.Text = "New Registrant";
                    }
                }

                if ( TemplateState != null )
                {
                    tglWaitList.Visible = TemplateState.WaitListEnabled;
                }

                if ( TemplateState != null && RegistrantState == null )
                {
                    RegistrantState = new RegistrantInfo();
                    RegistrantState.RegistrationId = registrationId ?? 0;
                    if ( TemplateState.SetCostOnInstance.HasValue && TemplateState.SetCostOnInstance.Value )
                    {
                        var instance = new RegistrationInstanceService( rockContext ).Get( RegistrationInstanceId );
                        if ( instance != null )
                        {
                            RegistrantState.Cost = instance.Cost ?? 0.0m;
                        }
                    }
                    else
                    {
                        RegistrantState.Cost = TemplateState.Cost;
                    }
                }

                if ( registrant != null && registrant.PersonAlias != null && registrant.PersonAlias.Person != null )
                {
                    ppPerson.SetValue( registrant.PersonAlias.Person );
                    if ( TemplateState != null && TemplateState.RequiredSignatureDocumentTemplate != null )
                    {
                        fuSignedDocument.Label = TemplateState.RequiredSignatureDocumentTemplate.Name;
                        if ( TemplateState.RequiredSignatureDocumentTemplate.BinaryFileType != null )
                        {
                            fuSignedDocument.BinaryFileTypeGuid = TemplateState.RequiredSignatureDocumentTemplate.BinaryFileType.Guid;
                        }

                        var signatureDocument = new SignatureDocumentService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( d =>
                                d.SignatureDocumentTemplateId == TemplateState.RequiredSignatureDocumentTemplateId.Value &&
                                d.AppliesToPersonAlias != null &&
                                d.AppliesToPersonAlias.PersonId == registrant.PersonAlias.PersonId &&
                                d.LastStatusDate.HasValue &&
                                d.Status == SignatureDocumentStatus.Signed &&
                                d.BinaryFile != null )
                            .OrderByDescending( d => d.LastStatusDate.Value )
                            .FirstOrDefault();

                        if ( signatureDocument != null )
                        {
                            hfSignedDocumentId.Value = signatureDocument.Id.ToString();
                            fuSignedDocument.BinaryFileId = signatureDocument.BinaryFileId;
                        }

                        fuSignedDocument.Visible = true;
                    }
                    else
                    {
                        fuSignedDocument.Visible = false;
                    }
                }
                else
                {
                    ppPerson.SetValue( null );
                }

                if ( RegistrantState != null )
                {
                    cbCost.Text = RegistrantState.Cost.ToString( "N2" );
                    cbDiscountApplies.Checked = RegistrantState.DiscountApplies;
                }
            }
        }

        private void NavigateToRegistration()
        {
            if ( RegistrantState != null )
            {
                var qryParams = new Dictionary<string, string>();
                qryParams.Add( "RegistrationId", RegistrantState.RegistrationId.ToString() );
                NavigateToParentPage( qryParams );
            }
        }

        #region Build Controls

        private void BuildControls( bool setValues )
        {
            if ( RegistrantState != null && TemplateState != null )
            {
                BuildFields( setValues );
                BuildFees( setValues );
            }
        }

        private void BuildFields( bool setValues )
        {
            phFields.Controls.Clear();

            if ( RegistrationGroup != null )
            {
                var ddlGroup = new RockDropDownList();
                ddlGroup.ID = "ddlGroup";
                ddlGroup.Required = false;
                ddlGroup.Label = "Target Group";
                ddlGroup.ValidationGroup = BlockValidationGroup;
                ddlGroup.Items.Add( new ListItem( "", "" ) );
                ddlGroup.Items.Add( new ListItem( RegistrationGroup.Name, RegistrationGroup.Id.ToString() ) );
                ddlGroup.SetValue( RegistrationGroup );
                phFields.Controls.Add( ddlGroup );
            }

            if ( TemplateState.Forms != null )
            {
                foreach ( var form in TemplateState.Forms.OrderBy( f => f.Order ) )
                {
                    if ( form.Fields != null )
                    {
                        foreach ( var field in form.Fields.OrderBy( f => f.Order ) )
                        {
                            {
                                object fieldValue = null;
                                if ( RegistrantState.FieldValues.ContainsKey( field.Id ) )
                                {
                                    fieldValue = RegistrantState.FieldValues[field.Id].FieldValue;
                                }

                                if ( field.FieldSource == RegistrationFieldSource.PersonField )
                                {
                                    CreatePersonField( field, setValues, fieldValue );
                                }
                                else if ( field.AttributeId.HasValue )
                                {
                                    var attribute = AttributeCache.Get( field.AttributeId.Value );
                                    string value = string.Empty;
                                    if ( setValues && fieldValue != null )
                                    {
                                        value = fieldValue.ToString();
                                    }
                                    attribute.AddControl( phFields.Controls, value, BlockValidationGroup, setValues, true, field.IsRequired, null, string.Empty );
                                }
                            }
                        }
                    }
                }
            }
        }

        private void BuildFees( bool setValues )
        {
            phFees.Controls.Clear();

            if ( TemplateState.Fees != null && TemplateState.Fees.Any() )
            {
                divFees.Visible = true;

                foreach ( var fee in TemplateState.Fees.OrderBy( f => f.Order ) )
                {
                    var feeValues = new List<FeeInfo>();
                    if ( RegistrantState.FeeValues.ContainsKey( fee.Id ) )
                    {
                        feeValues = RegistrantState.FeeValues[fee.Id];
                    }

                    if ( fee.FeeType == RegistrationFeeType.Single )
                    {
                        string label = fee.Name;
                        var cost = fee.CostValue.AsDecimalOrNull();
                        if ( cost.HasValue && cost.Value != 0.0M )
                        {
                            label = string.Format( "{0} ({1})", fee.Name, cost.Value.FormatAsCurrency() );
                        }

                        if ( fee.AllowMultiple )
                        {
                            // Single Option, Multi Quantity
                            var numUpDown = new NumberUpDown();
                            numUpDown.ID = "fee_" + fee.Id.ToString();
                            numUpDown.Label = label;
                            numUpDown.Minimum = 0;
                            phFees.Controls.Add( numUpDown );

                            if ( setValues && feeValues != null && feeValues.Any() )
                            {
                                numUpDown.Value = feeValues.First().Quantity;
                            }
                        }
                        else
                        {
                            // Single Option, Single Quantity
                            var cb = new RockCheckBox();
                            cb.ID = "fee_" + fee.Id.ToString();
                            cb.Label = label;
                            cb.SelectedIconCssClass = "fa fa-check-square-o fa-lg";
                            cb.UnSelectedIconCssClass = "fa fa-square-o fa-lg";
                            phFees.Controls.Add( cb );

                            if ( setValues && feeValues != null && feeValues.Any() )
                            {
                                cb.Checked = feeValues.First().Quantity > 0;
                            }
                        }
                    }
                    else
                    {
                        // Parse the options to get name and cost for each
                        var options = new Dictionary<string, string>();
                        string[] nameValues = fee.CostValue.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );
                        foreach ( string nameValue in nameValues )
                        {
                            string[] nameAndValue = nameValue.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );
                            if ( nameAndValue.Length == 1 )
                            {
                                options.AddOrIgnore( nameAndValue[0], nameAndValue[0] );
                            }
                            if ( nameAndValue.Length == 2 )
                            {
                                options.AddOrIgnore( nameAndValue[0], string.Format( "{0} ({1})", nameAndValue[0], nameAndValue[1].AsDecimal().FormatAsCurrency() ) );
                            }
                        }

                        if ( fee.AllowMultiple )
                        {
                            HtmlGenericControl feeAllowMultiple = new HtmlGenericControl( "div" );
                            phFees.Controls.Add( feeAllowMultiple );

                            feeAllowMultiple.AddCssClass( "feetype-allowmultiples" );

                            Label titleLabel = new Label();
                            feeAllowMultiple.Controls.Add( titleLabel );
                            titleLabel.CssClass = "control-label";
                            titleLabel.Text = fee.Name;

                            foreach ( var optionKeyVal in options )
                            {
                                var numUpDown = new NumberUpDown();
                                numUpDown.ID = string.Format( "fee_{0}_{1}", fee.Id, optionKeyVal.Key );
                                numUpDown.Label = string.Format( "{0}", optionKeyVal.Value );
                                numUpDown.Minimum = 0;
                                numUpDown.CssClass = "fee-allowmultiple";
                                feeAllowMultiple.Controls.Add( numUpDown );

                                if ( setValues && feeValues != null && feeValues.Any() )
                                {
                                    numUpDown.Value = feeValues
                                        .Where( f => f.Option == optionKeyVal.Key )
                                        .Select( f => f.Quantity )
                                        .FirstOrDefault();
                                }
                            }
                        }
                        else
                        {
                            // Multi Option, Single Quantity
                            var ddl = new RockDropDownList();
                            ddl.ID = "fee_" + fee.Id.ToString();
                            ddl.AddCssClass( "input-width-md" );
                            ddl.Label = fee.Name;
                            ddl.DataValueField = "Key";
                            ddl.DataTextField = "Value";
                            ddl.DataSource = options;
                            ddl.DataBind();
                            ddl.Items.Insert( 0, "" );
                            phFees.Controls.Add( ddl );

                            if ( setValues && feeValues != null && feeValues.Any() )
                            {
                                ddl.SetValue( feeValues
                                    .Where( f => f.Quantity > 0 )
                                    .Select( f => f.Option )
                                    .FirstOrDefault() );
                            }
                        }
                    }
                }
            }
            else
            {
                divFees.Visible = false;
            }
        }

        #endregion

        #region Parse Controls

        private void ParseControls()
        {
            if ( RegistrantState != null && TemplateState != null )
            {
                ParseFields();
                ParseFees();
            }
        }

        private void ParseFields()
        {
            if ( RegistrationGroup != null )
            {
                var control = phFields.FindControl( "ddlGroup" );
                if ( control != null )
                {
                    RegistrantPlacedInGroup = ( ( RockDropDownList ) control ).SelectedValueAsInt() > 0;
                }
            }

            if ( TemplateState.Forms != null )
            {
                foreach ( var form in TemplateState.Forms.OrderBy( f => f.Order ) )
                {
                    if ( form.Fields != null )
                    {
                        foreach ( var field in form.Fields.OrderBy( f => f.Order ) )
                        {
                            {
                                object value = null;

                                if ( field.FieldSource == RegistrationFieldSource.PersonField )
                                {
                                    switch ( field.PersonFieldType )
                                    {
                                        case RegistrationPersonFieldType.FirstName:
                                            {
                                                Control control = phFields.FindControl( "tbFirstName" );
                                                if ( control != null )
                                                {
                                                    value = ( ( RockTextBox ) control ).Text;
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.LastName:
                                            {
                                                Control control = phFields.FindControl( "tbLastName" );
                                                if ( control != null )
                                                {
                                                    value = ( ( RockTextBox ) control ).Text;
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.Campus:
                                            {
                                                Control control = phFields.FindControl( "cpHomeCampus" );
                                                if ( control != null )
                                                {
                                                    value = ( ( CampusPicker ) control ).SelectedValue.AsIntegerOrNull();
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.Address:
                                            {
                                                Control control = phFields.FindControl( "acAddress" );
                                                if ( control != null )
                                                {
                                                    var address = new AddressFieldType();
                                                    var location = new LocationService( new RockContext() ).Get( address.GetEditValue( control, null ).AsGuid() );
                                                    value = location;
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.Email:
                                            {
                                                Control control = phFields.FindControl( "tbEmail" );
                                                if ( control != null )
                                                {
                                                    value = ( ( EmailBox ) control ).Text;
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.Birthdate:
                                            {
                                                Control control = phFields.FindControl( "bpBirthday" );
                                                if ( control != null )
                                                {
                                                    value = ( ( BirthdayPicker ) control ).SelectedDate;
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.Grade:
                                            {
                                                Control control = phFields.FindControl( "gpGrade" );
                                                if ( control != null )
                                                {
                                                    value = Person.GraduationYearFromGradeOffset( ( ( GradePicker ) control ).SelectedValue.AsIntegerOrNull() );
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.Gender:
                                            {
                                                Control control = phFields.FindControl( "ddlGender" );
                                                if ( control != null )
                                                {
                                                    value = ( ( RockDropDownList ) control ).SelectedValue;
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.MaritalStatus:
                                            {
                                                Control control = phFields.FindControl( "ddlMaritalStatus" );
                                                if ( control != null )
                                                {
                                                    value = ( ( RockDropDownList ) control ).SelectedValue;
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.MobilePhone:
                                            {
                                                var phoneNumber = new PhoneNumber();
                                                var ppMobile = phFields.FindControl( "ppMobile" ) as PhoneNumberBox;
                                                if ( ppMobile != null )
                                                {
                                                    phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppMobile.CountryCode );
                                                    phoneNumber.Number = PhoneNumber.CleanNumber( ppMobile.Number );
                                                    value = phoneNumber;
                                                }
                                                break;
                                            }
                                        case RegistrationPersonFieldType.HomePhone:
                                            {
                                                var phoneNumber = new PhoneNumber();
                                                var ppHome = phFields.FindControl( "ppHome" ) as PhoneNumberBox;
                                                if ( ppHome != null )
                                                {
                                                    phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppHome.CountryCode );
                                                    phoneNumber.Number = PhoneNumber.CleanNumber( ppHome.Number );
                                                    value = phoneNumber;
                                                }
                                                break;
                                            }

                                        case RegistrationPersonFieldType.WorkPhone:
                                            {
                                                var phoneNumber = new PhoneNumber();
                                                var ppWork = phFields.FindControl( "ppWork" ) as PhoneNumberBox;
                                                if ( ppWork != null )
                                                {
                                                    phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppWork.CountryCode );
                                                    phoneNumber.Number = PhoneNumber.CleanNumber( ppWork.Number );
                                                    value = phoneNumber;
                                                }
                                                break;
                                            }
                                    }
                                }
                                else if ( field.AttributeId.HasValue )
                                {
                                    var attribute = AttributeCache.Get( field.AttributeId.Value );
                                    string fieldId = "attribute_field_" + attribute.Id.ToString();

                                    Control control = phFields.FindControl( fieldId );
                                    if ( control != null )
                                    {
                                        value = attribute.FieldType.Field.GetEditValue( control, attribute.QualifierValues );
                                    }
                                }

                                if ( value != null )
                                {
                                    RegistrantState.FieldValues.AddOrReplace( field.Id, new FieldValueObject( field, value ) );
                                }
                                else
                                {
                                    RegistrantState.FieldValues.Remove( field.Id );
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ParseFees()
        {
            if ( TemplateState.Fees != null )
            {
                foreach ( var fee in TemplateState.Fees.OrderBy( f => f.Order ) )
                {
                    List<FeeInfo> feeValues = ParseFee( fee );
                    if ( fee != null )
                    {
                        RegistrantState.FeeValues.AddOrReplace( fee.Id, feeValues );
                    }
                    else
                    {
                        // not possible since fee is null:
                        //RegistrantState.FeeValues.Remove( fee.Id );
                    }
                }
            }
        }

        private List<FeeInfo> ParseFee( RegistrationTemplateFee fee )
        {
            string fieldId = string.Format( "fee_{0}", fee.Id );

            if ( fee.FeeType == RegistrationFeeType.Single )
            {
                if ( fee.AllowMultiple )
                {
                    // Single Option, Multi Quantity
                    var numUpDown = phFees.FindControl( fieldId ) as NumberUpDown;
                    if ( numUpDown != null && numUpDown.Value > 0 )
                    {
                        return new List<FeeInfo> { new FeeInfo( string.Empty, numUpDown.Value, fee.CostValue.AsDecimal() ) };
                    }
                }
                else
                {
                    // Single Option, Single Quantity
                    var cb = phFees.FindControl( fieldId ) as RockCheckBox;
                    if ( cb != null && cb.Checked )
                    {
                        return new List<FeeInfo> { new FeeInfo( string.Empty, 1, fee.CostValue.AsDecimal() ) };
                    }
                }
            }
            else
            {
                // Parse the options to get name and cost for each
                var options = new Dictionary<string, string>();
                var optionCosts = new Dictionary<string, decimal>();

                string[] nameValues = fee.CostValue.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );
                foreach ( string nameValue in nameValues )
                {
                    string[] nameAndValue = nameValue.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );
                    if ( nameAndValue.Length == 1 )
                    {
                        options.AddOrIgnore( nameAndValue[0], nameAndValue[0] );
                        optionCosts.AddOrIgnore( nameAndValue[0], 0.0m );
                    }
                    if ( nameAndValue.Length == 2 )
                    {
                        options.AddOrIgnore( nameAndValue[0], string.Format( "{0} ({1})", nameAndValue[0], nameAndValue[1].AsDecimal().FormatAsCurrency() ) );
                        optionCosts.AddOrIgnore( nameAndValue[0], nameAndValue[1].AsDecimal() );
                    }
                }

                if ( fee.AllowMultiple )
                {
                    // Multi Option, Multi Quantity
                    var result = new List<FeeInfo>();

                    foreach ( var optionKeyVal in options )
                    {
                        string optionFieldId = string.Format( "{0}_{1}", fieldId, optionKeyVal.Key );
                        var numUpDown = phFees.FindControl( optionFieldId ) as NumberUpDown;
                        if ( numUpDown != null && numUpDown.Value > 0 )
                        {
                            result.Add( new FeeInfo( optionKeyVal.Key, numUpDown.Value, optionCosts[optionKeyVal.Key] ) );
                        }
                    }

                    if ( result.Any() )
                    {
                        return result;
                    }
                }
                else
                {
                    // Multi Option, Single Quantity
                    var ddl = phFees.FindControl( fieldId ) as RockDropDownList;
                    if ( ddl != null && ddl.SelectedValue != "" )
                    {
                        return new List<FeeInfo> { new FeeInfo( ddl.SelectedValue, 1, optionCosts[ddl.SelectedValue] ) };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Saves the person.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="location">The location.</param>
        /// <param name="adultRoleId">The adult role identifier.</param>
        /// <param name="childRoleId">The child role identifier.</param>
        /// <param name="multipleFamilyGroupIds">The multiple family group ids.</param>
        /// <param name="singleFamilyId">The single family identifier.</param>
        /// <returns></returns>
        private Person SavePerson( RockContext rockContext, Person person, Guid familyGuid, int? campusId, Location location, int adultRoleId, int childRoleId,
            Dictionary<Guid, int> multipleFamilyGroupIds, ref int? singleFamilyId )
        {
            int? familyId = null;

            if ( person.Id > 0 )
            {
                rockContext.SaveChanges();

                // Set the family guid for any other registrants that were selected to be in the same family
                var family = person.GetFamilies( rockContext ).FirstOrDefault();
                if ( family != null )
                {
                    familyId = family.Id;
                    multipleFamilyGroupIds.AddOrIgnore( familyGuid, family.Id );
                    if ( !singleFamilyId.HasValue )
                    {
                        singleFamilyId = family.Id;
                    }

                    if ( campusId.HasValue )
                    {
                        family.CampusId = campusId;
                    }
                }
            }
            else
            {
                //
                // not adding new people via this registration form
                // relies on the person record already existing
                //
                //// If we've created the family aready for this registrant, add them to it
                //if (
                //        ( RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Ask && multipleFamilyGroupIds.ContainsKey( familyGuid ) ) ||
                //        ( RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Yes && singleFamilyId.HasValue )
                //    )
                //{
                //    // Add person to existing family
                //    var age = person.Age;
                //    int familyRoleId = age.HasValue && age < 18 ? childRoleId : adultRoleId;

                //    familyId = RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Ask ?
                //        multipleFamilyGroupIds[familyGuid] :
                //        singleFamilyId.Value;
                //    PersonService.AddPersonToFamily( person, true, multipleFamilyGroupIds[familyGuid], familyRoleId, rockContext );

                //}

                //// otherwise create a new family
                //else
                {
                    // Create Person/Family
                    var familyGroup = PersonService.SaveNewPerson( person, rockContext, campusId, false );
                    if ( familyGroup != null )
                    {
                        familyId = familyGroup.Id;

                        // Store the family id for next person
                        multipleFamilyGroupIds.AddOrIgnore( familyGuid, familyGroup.Id );
                        if ( !singleFamilyId.HasValue )
                        {
                            singleFamilyId = familyGroup.Id;
                        }
                    }
                }
            }

            if ( familyId.HasValue && location != null )
            {
                var homeLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
                if ( homeLocationType != null )
                {
                    var familyGroup = new GroupService( rockContext ).Get( familyId.Value );
                    if ( familyGroup != null )
                    {
                        GroupService.AddNewGroupAddress(
                            rockContext,
                            familyGroup,
                            Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME,
                            location.Street1, location.Street2, location.City, location.State, location.PostalCode, location.Country, true );
                    }
                }
            }

            return new PersonService( rockContext ).Get( person.Id );
        }

        /// <summary>
        /// Saves the phone.
        /// </summary>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="person">The person.</param>
        /// <param name="phoneTypeGuid">The phone type unique identifier.</param>
        /// <param name="changes">The changes.</param>
        private void SavePhone( object fieldValue, Person person, Guid phoneTypeGuid, History.HistoryChangeList changes )
        {
            var phoneNumber = fieldValue as PhoneNumber;
            if ( phoneNumber != null )
            {
                string cleanNumber = PhoneNumber.CleanNumber( phoneNumber.Number );
                if ( !string.IsNullOrWhiteSpace( cleanNumber ) )
                {
                    var numberType = DefinedValueCache.Get( phoneTypeGuid );
                    if ( numberType != null )
                    {
                        var phone = person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValueId == numberType.Id );
                        string oldPhoneNumber = string.Empty;
                        if ( phone == null )
                        {
                            phone = new PhoneNumber { NumberTypeValueId = numberType.Id };
                            person.PhoneNumbers.Add( phone );
                        }
                        else
                        {
                            oldPhoneNumber = phone.NumberFormattedWithCountryCode;
                        }
                        phone.CountryCode = PhoneNumber.CleanNumber( phoneNumber.CountryCode );
                        phone.Number = cleanNumber;

                        History.EvaluateChange(
                            changes,
                            string.Format( "{0} Phone", numberType.Value ),
                            oldPhoneNumber,
                            phoneNumber.NumberFormattedWithCountryCode );
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
        private void CreatePersonField( RegistrationTemplateFormField field, bool setValue, object fieldValue )
        {
            switch ( field.PersonFieldType )
            {
                case RegistrationPersonFieldType.FirstName:
                    {
                        var tbFirstName = new RockTextBox();
                        tbFirstName.ID = "tbFirstName";
                        tbFirstName.Label = "First Name";
                        tbFirstName.Required = field.IsRequired;
                        tbFirstName.ValidationGroup = BlockValidationGroup;
                        tbFirstName.AddCssClass( "js-first-name" );
                        tbFirstName.Enabled = false;
                        phFields.Controls.Add( tbFirstName );

                        if ( setValue && fieldValue != null )
                        {
                            tbFirstName.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case RegistrationPersonFieldType.LastName:
                    {
                        var tbLastName = new RockTextBox();
                        tbLastName.ID = "tbLastName";
                        tbLastName.Label = "Last Name";
                        tbLastName.Required = field.IsRequired;
                        tbLastName.ValidationGroup = BlockValidationGroup;
                        tbLastName.Enabled = false;
                        phFields.Controls.Add( tbLastName );

                        if ( setValue && fieldValue != null )
                        {
                            tbLastName.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Campus:
                    {
                        var cpHomeCampus = new CampusPicker();
                        cpHomeCampus.ID = "cpHomeCampus";
                        cpHomeCampus.Label = "Campus";
                        cpHomeCampus.Required = field.IsRequired;
                        cpHomeCampus.ValidationGroup = BlockValidationGroup;
                        cpHomeCampus.Campuses = CampusCache.All( false );

                        phFields.Controls.Add( cpHomeCampus );

                        if ( setValue && fieldValue != null )
                        {
                            cpHomeCampus.SelectedCampusId = fieldValue.ToString().AsIntegerOrNull();
                        }
                        break;
                    }

                case RegistrationPersonFieldType.Address:
                    {
                        var acAddress = new AddressControl();
                        acAddress.ID = "acAddress";
                        acAddress.Label = "Address";
                        acAddress.UseStateAbbreviation = true;
                        acAddress.UseCountryAbbreviation = false;
                        acAddress.Required = field.IsRequired;
                        acAddress.ValidationGroup = BlockValidationGroup;

                        phFields.Controls.Add( acAddress );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue as Location;
                            acAddress.SetValues( value );
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Email:
                    {
                        var tbEmail = new EmailBox();
                        tbEmail.ID = "tbEmail";
                        tbEmail.Label = "Email";
                        tbEmail.Required = field.IsRequired;
                        tbEmail.ValidationGroup = BlockValidationGroup;
                        phFields.Controls.Add( tbEmail );

                        if ( setValue && fieldValue != null )
                        {
                            tbEmail.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Birthdate:
                    {
                        var bpBirthday = new BirthdayPicker();
                        bpBirthday.ID = "bpBirthday";
                        bpBirthday.Label = "Birthday";
                        bpBirthday.Required = field.IsRequired;
                        bpBirthday.ValidationGroup = BlockValidationGroup;
                        phFields.Controls.Add( bpBirthday );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue as DateTime?;
                            bpBirthday.SelectedDate = value;
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Grade:
                    {
                        var gpGrade = new GradePicker();
                        gpGrade.ID = "gpGrade";
                        gpGrade.Label = "Grade";
                        gpGrade.Required = field.IsRequired;
                        gpGrade.ValidationGroup = BlockValidationGroup;
                        gpGrade.UseAbbreviation = true;
                        gpGrade.UseGradeOffsetAsValue = true;
                        gpGrade.CssClass = "input-width-md";
                        phFields.Controls.Add( gpGrade );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().AsIntegerOrNull();
                            gpGrade.SetValue( Person.GradeOffsetFromGraduationYear( value ) );
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Gender:
                    {
                        var ddlGender = new RockDropDownList();
                        ddlGender.ID = "ddlGender";
                        ddlGender.Label = "Gender";
                        ddlGender.Required = field.IsRequired;
                        ddlGender.ValidationGroup = BlockValidationGroup;
                        ddlGender.BindToEnum<Gender>( false );

                        // change the 'Unknow' value to be blank instead
                        ddlGender.Items.FindByValue( "0" ).Text = string.Empty;

                        phFields.Controls.Add( ddlGender );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().ConvertToEnumOrNull<Gender>() ?? Gender.Unknown;
                            ddlGender.SetValue( value.ConvertToInt() );
                        }

                        break;
                    }

                case RegistrationPersonFieldType.MaritalStatus:
                    {
                        var ddlMaritalStatus = new RockDropDownList();
                        ddlMaritalStatus.ID = "ddlMaritalStatus";
                        ddlMaritalStatus.Label = "Marital Status";
                        ddlMaritalStatus.Required = field.IsRequired;
                        ddlMaritalStatus.ValidationGroup = BlockValidationGroup;
                        ddlMaritalStatus.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ), true );
                        phFields.Controls.Add( ddlMaritalStatus );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().AsInteger();
                            ddlMaritalStatus.SetValue( value );
                        }

                        break;
                    }

                case RegistrationPersonFieldType.MobilePhone:
                    {
                        var dv = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );
                        if ( dv != null )
                        {
                            var ppMobile = new PhoneNumberBox();
                            ppMobile.ID = "ppMobile";
                            ppMobile.Label = dv.Value;
                            ppMobile.Required = field.IsRequired;
                            ppMobile.ValidationGroup = BlockValidationGroup;
                            ppMobile.CountryCode = PhoneNumber.DefaultCountryCode();

                            phFields.Controls.Add( ppMobile );

                            if ( setValue && fieldValue != null )
                            {
                                var value = fieldValue as PhoneNumber;
                                if ( value != null )
                                {
                                    ppMobile.CountryCode = value.CountryCode;
                                    ppMobile.Number = value.ToString();
                                }
                            }
                        }

                        break;
                    }
                case RegistrationPersonFieldType.HomePhone:
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

                            phFields.Controls.Add( ppHome );

                            if ( setValue && fieldValue != null )
                            {
                                var value = fieldValue as PhoneNumber;
                                if ( value != null )
                                {
                                    ppHome.CountryCode = value.CountryCode;
                                    ppHome.Number = value.ToString();
                                }
                            }
                        }

                        break;
                    }

                case RegistrationPersonFieldType.WorkPhone:
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

                            phFields.Controls.Add( ppWork );

                            if ( setValue && fieldValue != null )
                            {
                                var value = fieldValue as PhoneNumber;
                                if ( value != null )
                                {
                                    ppWork.CountryCode = value.CountryCode;
                                    ppWork.Number = value.ToString();
                                }
                            }
                        }

                        break;
                    }
            }
        }

        #endregion

        #endregion

        protected void ppPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( RegistrantState != null && ( ppPerson.PersonId.HasValue && ppPerson.PersonId > 0 ) )
            {
                RockContext rockContext = new RockContext();
                var personService = new PersonService( rockContext );
                var registrantService = new RegistrationRegistrantService( rockContext );
                var registrantFeeService = new RegistrationRegistrantFeeService( rockContext );
                var registrationTemplateFeeService = new RegistrationTemplateFeeService( rockContext );
                RegistrationRegistrant registrant = null;
                if ( RegistrantState.Id > 0 )
                {
                    registrant = registrantService.Get( RegistrantState.Id );
                }

                var registration = new RegistrationService( rockContext ).Get( RegistrantState.RegistrationId );
                var alreadyRegistered = registrantService.Queryable().Any( r => r.PersonAliasId == ppPerson.PersonAliasId && r.Registration.RegistrationInstanceId == registration.RegistrationInstanceId );
                if ( !alreadyRegistered )
                {
                    var registrantChanges = new History.HistoryChangeList();
                    var personChanges = new History.HistoryChangeList();

                    if ( registrant == null )
                    {
                        registrant = new RegistrationRegistrant();
                        registrant.RegistrationId = RegistrantState.RegistrationId;
                        registrantService.Add( registrant );
                        registrantChanges.AddChange( History.HistoryVerb.Add, History.HistoryChangeType.Record, "Registrant" );
                    }

                    if ( !registrant.PersonAliasId.Equals( ppPerson.PersonAliasId ) )
                    {
                        string prevPerson = ( registrant.PersonAlias != null && registrant.PersonAlias.Person != null ) ?
                            registrant.PersonAlias.Person.FullName : string.Empty;
                        string newPerson = ppPerson.PersonName;
                        History.EvaluateChange( registrantChanges, "Person", prevPerson, newPerson );
                    }
                    registrant.PersonAliasId = ppPerson.PersonAliasId.Value;

                    // set cost and discounts
                    History.EvaluateChange( registrantChanges, "Cost", registrant.Cost, cbCost.Text.AsDecimal() );
                    registrant.Cost = cbCost.Text.AsDecimal();

                    History.EvaluateChange( registrantChanges, "Discount Applies", registrant.DiscountApplies, cbDiscountApplies.Checked );
                    registrant.DiscountApplies = cbDiscountApplies.Checked;

                    // Get the name of registrant for history
                    string registrantName = "Unknown";
                    var person = new Person();
                    if ( ppPerson.PersonId.HasValue )
                    {
                        person = personService.Get( ppPerson.PersonId.Value );
                        if ( person != null )
                        {
                            registrantName = person.FullName;
                        }
                    }

                    if ( !registrant.IsValid )
                    {
                        // Controls will render the error messages
                        return;
                    }

                    try
                    {
                        rockContext.SaveChanges();
                    }
                    catch ( Exception ex )
                    {
                        throw ex;
                    }

                    HistoryService.SaveChanges(
                        rockContext,
                        typeof( Registration ),
                        Rock.SystemGuid.Category.HISTORY_EVENT_REGISTRATION.AsGuid(),
                        registrant.RegistrationId,
                        registrantChanges,
                        "Registrant: " + registrantName,
                        null, null );

                    var qryParams = new Dictionary<string, string>();
                    qryParams.Add( "RegistrationId", RegistrantState.RegistrationId.ToString() );
                    qryParams.Add( "RegistrantId", registrant.Id.ToString() );
                    NavigateToCurrentPage( qryParams );
                }
                else
                {
                    // person already a registrant in instance
                    ppPerson.SelectedValue = null;
                    ppPerson.PersonName = string.Empty;
                    return;
                }
            }
        }
    }
}
