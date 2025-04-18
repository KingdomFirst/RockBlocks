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
// * To be used similarly to the Person Bio block minus person specific demographic information.
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Crm
{
    /// <summary>
    /// The main Person Profile block the main information about a person 
    /// </summary>
    [DisplayName( "Business Bio" )]
    [Category( "KFS > CRM" )]
    [Description( "Business biographic/demographic information and picture (Business detail page)." )]

    #region Block Attributes

    [BadgesField(
        "Badges",
        Key = AttributeKey.Badges,
        Description = "The label badges to display in this block.",
        IsRequired = false,
        Order = 0 )]

    [WorkflowTypeField(
        "Workflow Actions",
        Key = AttributeKey.WorkflowActions,
        Description = "The workflows to make available as actions.",
        AllowMultiple = true,
        IsRequired = false,
        Order = 1 )]

    [CodeEditorField(
        "Additional Custom Actions",
        Key = AttributeKey.AdditionalCustomActions,
        Description = BlockAttributeDescription.AdditionalCustomActions,
        EditorMode = CodeEditorMode.Html,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 200,
        IsRequired = false,
        Order = 2 )]

    [BooleanField(
        "Enable Impersonation",
        Key = AttributeKey.EnableImpersonation,
        Description = "Should the Impersonate custom action be enabled? Note: If enabled, it is only visible to users that are authorized to administrate the person.",
        DefaultBooleanValue = false,
        Order = 3 )]

    [LinkedPage(
        "Impersonation Start Page",
        Key = AttributeKey.ImpersonationStartPage,
        Description = "The page to navigate to after clicking the Impersonate action.",
        IsRequired = false,
        Order = 4 )]

    [LinkedPage(
        "Person Detail Page",
        Key = AttributeKey.PersonDetailPage,
        Description = "The page to redirect user to if a person is requested.",
        IsRequired = false,
        Order = 5 )]

    [LinkedPage(
        "Nameless Person Detail Page",
        Key = AttributeKey.NamelessPersonDetailPage,
        Description = "The page to redirect user to if the person record is a Nameless Person record type.",
        IsRequired = false,
        Order = 6 )]

    [BooleanField(
        "Display Country Code",
        Key = AttributeKey.DisplayCountryCode,
        Description = "When enabled prepends the country code to all phone numbers.",
        DefaultBooleanValue = false,
        Order = 7 )]

    [CodeEditorField(
        "Custom Content",
        Key = AttributeKey.CustomContent,
        Description = "Custom Content will be rendered after the person's demographic information <span class='tip tip-lava'></span>.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 200,
        IsRequired = false,
        Order = 9 )]

    [BooleanField(
        "Allow Following",
        Key = AttributeKey.AllowFollowing,
        Description = "Should people be able to follow a person by selecting the star on the person's photo?",
        DefaultBooleanValue = true,
        Order = 10 )]

    [BooleanField(
        "Display Tags",
        Key = AttributeKey.DisplayTags,
        Description = "Should tags be displayed?",
        DefaultBooleanValue = true,
        Order = 11 )]

    [CategoryField(
        "Tag Category",
        Key = AttributeKey.TagCategory,
        Description = "Optional category to limit the tags to. If specified all new personal tags will be added with this category.",
        AllowMultiple = false,
        EntityType = typeof( Rock.Model.Tag ),
        IsRequired = false,
        Order = 14 )]

    [AttributeCategoryField(
        "Social Media Category",
        Key = AttributeKey.SocialMediaCategory,
        Description = "The Attribute Category to display attributes from.",
        AllowMultiple = false,
        EntityType = typeof( Rock.Model.Person ),
        IsRequired = false,
        DefaultValue = Rock.SystemGuid.Category.PERSON_ATTRIBUTES_SOCIAL,
        Order = 15 )]

    [BooleanField(
        "Enable Call Origination",
        Key = AttributeKey.EnableCallOrigination,
        Description = "Should click-to-call links be added to phone numbers.",
        DefaultBooleanValue = true,
        Order = 16 )]

    [LinkedPage(
        "Communication Page",
        Key = AttributeKey.CommunicationPage,
        Description = "The communication page to use for when the person's email address is clicked. Leave this blank to use the default.",
        IsRequired = false,
        Order = 17 )]

    #endregion Block Attributes

    public partial class BusinessBio : PersonBlock
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        private static class AttributeKey
        {
            public const string Badges = "Badges";
            public const string WorkflowActions = "WorkflowActions";
            public const string AdditionalCustomActions = "Actions";
            public const string EnableImpersonation = "EnableImpersonation";
            public const string ImpersonationStartPage = "ImpersonationStartPage";
            public const string PersonDetailPage = "PersonDetailPage";
            public const string NamelessPersonDetailPage = "NamelessPersonDetailPage";
            public const string DisplayCountryCode = "DisplayCountryCode";
            public const string CustomContent = "CustomContent";
            public const string AllowFollowing = "AllowFollowing";
            public const string DisplayTags = "DisplayTags";
            public const string TagCategory = "TagCategory";
            public const string SocialMediaCategory = "SocialMediaCategory";
            public const string EnableCallOrigination = "EnableCallOrigination";
            public const string CommunicationPage = "CommunicationPage";
        }

        private static class PageParameterKey
        {
            public const string PersonId = "PersonId";
            public const string BusinessId = "BusinessId";
            public const string NamelessPersonId = "NamelessPersonId";
        }

        private static class BlockAttributeDescription
        {
            public const string AdditionalCustomActions = @"
Additional custom actions (will be displayed after the list of workflow actions). Any instance of '{0}' will be replaced with the current person's id.
Because the contents of this setting will be rendered inside a &lt;ul&gt; element, it is recommended to use an 
&lt;li&gt; element for each available action.  Example:
<pre>
    &lt;li&gt;&lt;a href='~/WorkflowEntry/4?PersonId={0}' tabindex='0'&gt;Fourth Action&lt;/a&gt;&lt;/li&gt;
</pre>";
        }

        #endregion Attribute Keys

        #region Fields

        private const string NAME_KEY = "name";
        private const string ICONCSSCLASS_KEY = "iconcssclass";
        private const string COLOR_KEY = "color";
        private const string TEXT_TEMPLATE = "texttemplate";
        private const string BASEURL = "baseurl";

        private string _editUrlFormat = "~/Business/{0}/Edit";

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddCSSLink( "~/Styles/fluidbox.css" );
            RockPage.AddScriptLink( "~/Scripts/imagesloaded.min.js" );
            RockPage.AddScriptLink( "~/Scripts/jquery.fluidbox.min.js" );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( pnlContent );

            if ( Person == null )
            {
                return;
            }

            // If this is the empty person, check for an old Alias record and if found, redirect permanent to it instead.
            if ( Person.Id == 0 )
            {
                //referring to aliasPersonId as person might be merged
                var personId = this.PageParameter( PageParameterKey.PersonId ).AsIntegerOrNull();

                if ( personId.HasValue )
                {
                    var personAlias = new PersonAliasService( new RockContext() ).GetByAliasId( personId.Value );
                    if ( personAlias != null )
                    {
                        var pageReference = RockPage.PageReference;
                        pageReference.Parameters.AddOrReplace( PageParameterKey.PersonId, personAlias.PersonId.ToString() );
                        Response.RedirectPermanent( pageReference.BuildUrl(), false );
                    }
                }
            }

            if ( Person.Id == 0 )
            {
                //referring to aliasPersonId as person might be merged
                var businessId = this.PageParameter( PageParameterKey.BusinessId ).AsIntegerOrNull();

                if ( businessId.HasValue )
                {
                    var personAlias = new PersonAliasService( new RockContext() ).GetByAliasId( businessId.Value );
                    if ( personAlias != null )
                    {
                        var pageReference = RockPage.PageReference;
                        pageReference.Parameters.AddOrReplace( PageParameterKey.BusinessId, personAlias.PersonId.ToString() );
                        Response.RedirectPermanent( pageReference.BuildUrl(), false );
                    }
                    else
                    {
                        Response.Redirect( string.Format( _editUrlFormat, Person.Id ), false );
                    }
                }
            }

            pnlFollow.Visible = GetAttributeValue( AttributeKey.AllowFollowing ).AsBoolean();

            if ( !Person.IsBusiness() )
            {
                var parms = new Dictionary<string, string>();
                parms.Add( PageParameterKey.PersonId, Person.Id.ToString() );
                NavigateToLinkedPage( AttributeKey.PersonDetailPage, parms );
            }
            else if ( Person.IsNameless() )
            {
                var parms = new Dictionary<string, string>();
                parms.Add( PageParameterKey.NamelessPersonId, Person.Id.ToString() );
                NavigateToLinkedPage( AttributeKey.NamelessPersonDetailPage, parms );
            }

            // Set the browser page title to include person's name
            RockPage.BrowserTitle = Person.FullName;

            string badgeList = GetAttributeValue( AttributeKey.Badges );
            if ( !string.IsNullOrWhiteSpace( badgeList ) )
            {
                foreach ( string badgeGuid in badgeList.SplitDelimitedValues() )
                {
                    Guid guid = badgeGuid.AsGuid();
                    if ( guid != Guid.Empty )
                    {
                        var badge = BadgeCache.Get( guid );
                        if ( badge != null )
                        {
                            blStatus.BadgeTypes.Add( badge );
                        }
                    }
                }
            }

            lbEditBusiness.Visible = IsUserAuthorized( Rock.Security.Authorization.EDIT );

            // only show if the Impersonation button if the feature is enabled, and the current user is authorized to Administrate the person
            bool enableImpersonation = this.GetAttributeValue( AttributeKey.EnableImpersonation ).AsBoolean();
            lbImpersonate.Visible = false;
            if ( enableImpersonation && Person.Id != CurrentPersonId && Person.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, this.CurrentPerson ) )
            {
                lbImpersonate.Visible = true;
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
                // dont' show if there isn't a person, or if it is a 'Nameless" person record type
                if ( Person == null || Person.Id == 0 || Person.RecordTypeValueId == DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_NAMELESS.AsGuid() ) )
                {
                    nbInvalidBusiness.Visible = true;
                    pnlContent.Visible = false;
                    return;
                }

                string quickReturnLava = "{{ Person.FullName | AddQuickReturn:'People', 10 }}";
                var quickReturnMergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                quickReturnMergeFields.Add( "Person", Person );
                quickReturnLava.ResolveMergeFields( quickReturnMergeFields );

                SetPersonName();

                // Setup Image
                string imgTag = Rock.Model.Person.GetPersonPhotoImageTag( Person, 200, 200 );
                if ( Person.PhotoId.HasValue )
                {
                    lImage.Text = string.Format( "<a href='{0}'>{1}</a>", Person.PhotoUrl, imgTag );
                }
                else
                {
                    lImage.Text = imgTag;
                }

                if ( GetAttributeValue( AttributeKey.AllowFollowing ).AsBoolean() )
                {
                    FollowingsHelper.SetFollowing( Person.PrimaryAlias, pnlFollow, this.CurrentPerson );
                }

                hlVCard.NavigateUrl = ResolveUrl( string.Format( "~/api/People/VCard/{0}", Person.Guid ) );

                var socialCategoryGuid = GetAttributeValue( AttributeKey.SocialMediaCategory ).AsGuidOrNull();
                if ( socialCategoryGuid.HasValue )
                {
                    var attributes = Person.Attributes.Where( p => p.Value.Categories.Select( c => c.Guid ).Contains( socialCategoryGuid.Value ) );
                    var result = attributes.Join( Person.AttributeValues, a => a.Key, v => v.Key, ( a, v ) => new { Attribute = a.Value, Value = v.Value, QualifierValues = a.Value.QualifierValues } );

                    rptSocial.DataSource = result
                        .Where( r =>
                            r.Value != null &&
                            r.Value.Value != string.Empty &&
                            r.QualifierValues != null &&
                            r.QualifierValues.ContainsKey( NAME_KEY ) &&
                            r.QualifierValues.ContainsKey( ICONCSSCLASS_KEY ) &&
                            r.QualifierValues.ContainsKey( COLOR_KEY ) )
                        .OrderBy( r => r.Attribute.Order )
                        .Select( r => new
                        {
                            url = r.Value.Value,
                            name = r.QualifierValues[NAME_KEY].Value,
                            icon = r.Attribute.QualifierValues[ICONCSSCLASS_KEY].Value.Contains( "fa-fw" ) ?
                                    r.Attribute.QualifierValues[ICONCSSCLASS_KEY].Value :
                                    r.Attribute.QualifierValues[ICONCSSCLASS_KEY].Value + " fa-fw",
                            color = r.Attribute.QualifierValues[COLOR_KEY].Value,
                        } )
                        .ToList();
                    rptSocial.DataBind();
                }

                var business = Person;

                SetHeadingStatusInfo( business );
                var detailsLeft = new DescriptionList();
                var detailsRight = new DescriptionList();

                if ( business.GivingGroup != null && business.GivingGroup.Campus != null )
                {
                    detailsLeft.Add( "Campus", business.GivingGroup.Campus );
                }

                if ( business.RecordStatusReasonValue != null )
                {
                    detailsLeft.Add( "Record Status Reason", business.RecordStatusReasonValue );
                }

                // Get address
                var workLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid() );
                if ( workLocationType != null )
                {
                    if ( business.GivingGroup != null ) // Giving Group is a shortcut to Family Group for business
                    {
                        var location = business.GivingGroup.GroupLocations
                            .Where( gl => gl.GroupLocationTypeValueId == workLocationType.Id )
                            .Select( gl => gl.Location )
                            .FirstOrDefault();
                        if ( location != null )
                        {
                            var addTo = detailsLeft;
                            if ( business.GivingGroup != null && business.GivingGroup.Campus != null && business.RecordStatusReasonValue != null )
                            {
                                addTo = detailsRight;
                            }
                            addTo.Add( "Address", location.GetFullStreetAddress().ConvertCrLfToHtmlBr() );
                        }
                    }
                }

                lDetailsLeft.Text = detailsLeft.Html;

                var workPhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );
                if ( workPhoneType != null )
                {
                    var phoneNumber = business.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == workPhoneType.Id );
                    if ( phoneNumber != null )
                    {
                        //detailsRight.Add( "Phone Number", phoneNumber.ToString() );
                        detailsRight.Add( "Phone Number", FormatPhoneNumber( phoneNumber.IsUnlisted, phoneNumber.CountryCode, phoneNumber.ToString(), phoneNumber.NumberTypeValueId ?? 0, phoneNumber.IsMessagingEnabled ) );
                    }
                }

                var communicationLinkedPageValue = this.GetAttributeValue( AttributeKey.CommunicationPage );
                Rock.Web.PageReference communicationPageReference;
                if ( communicationLinkedPageValue.IsNotNullOrWhiteSpace() )
                {
                    communicationPageReference = new Rock.Web.PageReference( communicationLinkedPageValue );
                }
                else
                {
                    communicationPageReference = null;
                }

                detailsRight.Add( "Email Address", business.GetEmailTag( ResolveRockUrl( "/" ), communicationPageReference ) );

                lDetailsRight.Text = detailsRight.Html;

                //if ( Person.PhoneNumbers != null )
                //{
                //    var phoneNumbers = Person.PhoneNumbers.AsEnumerable();
                //    var phoneNumberTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE ) );
                //    if ( phoneNumberTypes.DefinedValues.Any() )
                //    {
                //        var phoneNumberTypeIds = phoneNumberTypes.DefinedValues.Select( a => a.Id ).ToList();
                //        phoneNumbers = phoneNumbers.OrderBy( a => phoneNumberTypeIds.IndexOf( a.NumberTypeValueId.Value ) );
                //    }

                //    rptPhones.DataSource = phoneNumbers;
                //    rptPhones.DataBind();
                //}

                //if ( communicationLinkedPageValue.IsNotNullOrWhiteSpace() )
                //{
                //    communicationPageReference = new Rock.Web.PageReference( communicationLinkedPageValue );
                //}
                //else
                //{
                //    communicationPageReference = null;
                //}

                //lEmail.Text = Person.GetEmailTag( ResolveRockUrl( "/" ), communicationPageReference );

                if ( GetAttributeValue( AttributeKey.DisplayTags ).AsBoolean( true ) )
                {
                    taglPersonTags.Visible = true;
                    taglPersonTags.EntityTypeId = Person.TypeId;
                    taglPersonTags.EntityGuid = Person.Guid;
                    taglPersonTags.CategoryGuid = GetAttributeValue( AttributeKey.TagCategory ).AsGuidOrNull();
                    taglPersonTags.GetTagValues( CurrentPersonId );
                }
                else
                {
                    taglPersonTags.Visible = false;
                }

                CreateActionMenu();

                string customContent = GetAttributeValue( AttributeKey.CustomContent );
                if ( !string.IsNullOrWhiteSpace( customContent ) )
                {
                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
                    string resolvedContent = customContent.ResolveMergeFields( mergeFields );
                    phCustomContent.Controls.Add( new LiteralControl( resolvedContent ) );
                }
            }
        }

        protected void CreateActionMenu()
        {
            StringBuilder sbActions = new StringBuilder();

            // First list the actions manually entered as html in the block settting
            var actions = GetAttributeValue( AttributeKey.AdditionalCustomActions );
            if ( !string.IsNullOrWhiteSpace( actions ) )
            {
                string appRoot = ResolveRockUrl( "~/" );
                string themeRoot = ResolveRockUrl( "~~/" );
                actions = actions.Replace( "~~/", themeRoot ).Replace( "~/", appRoot );

                if ( actions.Contains( "{0}" ) )
                {
                    actions = string.Format( actions, Person.Id );
                }

                sbActions.Append( "<li role=\"separator\" class=\"divider\"></li>" );
                sbActions.Append( actions );
            }

            // Next list the workflow actions selected in the picker
            var workflowActions = GetAttributeValue( AttributeKey.WorkflowActions );
            if ( !string.IsNullOrWhiteSpace( workflowActions ) )
            {
                List<WorkflowType> workflowTypes = new List<WorkflowType>();

                using ( var rockContext = new RockContext() )
                {
                    var workflowTypeService = new WorkflowTypeService( rockContext );
                    foreach ( string guidValue in workflowActions.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) )
                    {
                        Guid? guid = guidValue.AsGuidOrNull();
                        if ( guid.HasValue )
                        {
                            var workflowType = workflowTypeService.Get( guid.Value );
                            if ( workflowType != null && ( workflowType.IsActive ?? true ) && workflowType.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
                            {
                                workflowTypes.Add( workflowType );
                            }
                        }
                    }
                }

                workflowTypes = workflowTypes.OrderBy( w => w.Name ).ToList();

                if ( workflowTypes.Count() > 0 )
                {
                    sbActions.Append( "<li role=\"separator\" class=\"divider\"></li>" );
                }

                foreach ( var workflowType in workflowTypes )
                {
                    string url = string.Format( "~/WorkflowEntry/{0}?PersonId={1}", workflowType.Id, Person.Id );
                    sbActions.AppendFormat(
                        "<li><a href='{0}'><i class='fa-fw {1}'></i> {2}</a></li>",
                        ResolveRockUrl( url ),
                        workflowType.IconCssClass,
                        workflowType.Name );
                    sbActions.AppendLine();
                }
            }

            lActions.Text = sbActions.ToString();
        }

        /// <summary>
        /// Sets the heading Status information.
        /// </summary>
        /// <param name="business">The business.</param>
        private void SetHeadingStatusInfo( Person business )
        {
            if ( business.RecordStatusValue != null )
            {
                hlStatus.Text = business.RecordStatusValue.Value;
                if ( business.RecordStatusValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() )
                {
                    hlStatus.LabelType = LabelType.Warning;
                }
                else if ( business.RecordStatusValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid() )
                {
                    hlStatus.LabelType = LabelType.Danger;
                }
                else
                {
                    hlStatus.LabelType = LabelType.Success;
                }
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            // reload the page if block settings where changed
            Response.Redirect( Request.RawUrl, false );
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// Sets the name of the person.
        /// </summary>
        private void SetPersonName()
        {
            // Check if this record represents a Business.
            bool isBusiness = false;

            if ( Person.RecordTypeValueId.HasValue )
            {
                int recordTypeValueIdBusiness = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;

                isBusiness = ( Person.RecordTypeValueId.Value == recordTypeValueIdBusiness );
            }

            // Get the Display Name.
            string nameText;

            if ( isBusiness )
            {
                nameText = Person.LastName;
            }
            else
            {
                nameText = string.Format( "<span class='first-word nickname'>{0}</span> <span class='lastname'>{1}</span>", Person.NickName, Person.LastName );

                // Add First Name if different from NickName.
                if ( Person.NickName != Person.FirstName )
                {
                    if ( !string.IsNullOrWhiteSpace( Person.FirstName ) )
                    {
                        nameText += string.Format( " <span class='firstname'>({0})</span>", Person.FirstName );
                    }
                }

            }

            lName.Text = nameText;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbEditBusiness control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEditBusiness_Click( object sender, EventArgs e )
        {
            if ( Person != null )
            {
                Response.Redirect( string.Format( _editUrlFormat, Person.Id ), false );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbImpersonate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbImpersonate_Click( object sender, EventArgs e )
        {
            if ( Person != null )
            {
                if ( Person.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, this.CurrentPerson ) )
                {
                    var impersonationToken = this.Person.GetImpersonationToken( RockDateTime.Now.AddMinutes( 5 ), 1, null );

                    // store the current user in Session["ImpersonatedByUser"] so that we can log back in as them from the Admin Bar
                    Session["ImpersonatedByUser"] = this.CurrentUser;

                    var qryParams = new Dictionary<string, string>();
                    qryParams.Add( "rckipid", impersonationToken );
                    if ( !string.IsNullOrEmpty( this.GetAttributeValue( AttributeKey.ImpersonationStartPage ) ) )
                    {
                        NavigateToLinkedPage( AttributeKey.ImpersonationStartPage, qryParams );
                    }
                    else
                    {
                        NavigateToCurrentPageReference( qryParams );
                    }
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Formats the phone number.
        /// </summary>
        /// <param name="unlisted">if set to <c>true</c> [unlisted].</param>
        /// <param name="countryCode">The country code.</param>
        /// <param name="number">The number.</param>
        /// <param name="phoneNumberTypeId">The phone number type identifier.</param>
        /// <returns></returns>
        protected string FormatPhoneNumber( bool unlisted, object countryCode, object number, int phoneNumberTypeId, bool smsEnabled = false )
        {
            var originationEnabled = GetAttributeValue( AttributeKey.EnableCallOrigination ).AsBoolean();

            string formattedNumber = "Unlisted";

            string cc = countryCode as string ?? string.Empty;
            string n = number as string ?? string.Empty;

            if ( !unlisted )
            {
                if ( GetAttributeValue( AttributeKey.DisplayCountryCode ).AsBoolean() )
                {
                    formattedNumber = PhoneNumber.FormattedNumber( cc, n, true );
                }
                else
                {
                    formattedNumber = PhoneNumber.FormattedNumber( cc, n );
                }
            }

            var phoneType = DefinedValueCache.Get( phoneNumberTypeId );
            if ( phoneType != null )
            {
                string phoneMarkup = formattedNumber;

                if ( originationEnabled )
                {
                    var pbxComponent = Rock.Pbx.PbxContainer.GetAllowedActiveComponentWithOriginationSupport( CurrentPerson );

                    if ( pbxComponent != null )
                    {
                        var jsScript = string.Format( "javascript: Rock.controls.pbx.originate('{0}', '{1}', '{2}','{3}','{4}');", CurrentPerson.Guid, number.ToString(), CurrentPerson.FullName, Person.FullName, formattedNumber );
                        phoneMarkup = string.Format( "<a class='originate-call js-originate-call' href=\"{0}\">{1}</a>", jsScript, formattedNumber );
                    }
                    else if ( RockPage.IsMobileRequest ) // if the page is being loaded locally then add the tel:// link
                    {
                        phoneMarkup = string.Format( "<a href=\"tel://{0}\">{1}</a>", n, formattedNumber );
                    }
                }

                if ( smsEnabled )
                {
                    formattedNumber = string.Format( "{0} <small>{1} <span class='label label-success' title='SMS Enabled' data-toggle='tooptip' data-placement='top'><i class='fa fa-sms'></i></span></small>", phoneMarkup, phoneType.Value );
                }
                else
                {
                    formattedNumber = string.Format( "{0} <small>{1}</small>", phoneMarkup, phoneType.Value );
                }

            }

            return formattedNumber;
        }

        #endregion
    }
}
