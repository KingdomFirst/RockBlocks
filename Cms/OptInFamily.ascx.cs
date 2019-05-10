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
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Cms
{

    #region Block Attributes

    [DisplayName( "Opt In Family KFS" )]
    [Category( "KFS > CMS" )]
    [Description( "Block for users to manage the opting in/out of family members from a certain matter based on the value of a supplied Person Attribute." )]

    [CodeEditorField( "Intro Text", "The text to instruct users how and why to use this form.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, "", "", 3 )]
    [CustomCheckboxListField( "Family Roles", "The Family Roles which can edit this attribute.", "SELECT [r].[Id] AS [Value], [r].[Name] AS [Text] FROM [GroupTypeRole] [r] JOIN [GroupType] [t] ON [r].[GroupTypeId] = [t].[Id] WHERE [t].[Guid] LIKE '790E3215-3B10-442B-AF69-616C0DCB998E'", true )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Person Attribute", "The person attribute that will be set for each selected family member. If it's a datetime attribute, current datetime will be saved, otherwise \"True\" will be the value.", true, false, order: 3 )]
    [TextField( "Confirmation Message", "The text to display when information has been successfully submitted.", false, "Form submitted successfully!" , "", 4 )]
    [LinkedPage( "Confirmation Page", "The page to redirect the user to once the form has been submitted. Overrides the Confirmation Text setting.", false, "", "", 5 )]
    [TextField( "Not Authorized Message", "The message to display if a user that is not in one of the selected group type roles lands on this block.", false, "Your family role has not been given authorization to fill out this form. Please contact your system administrator.", "", 6 )]

    #endregion

    public partial class OptInFamily : RockBlock
    {
        #region Properties

        private Person _person = null;
        private List<int> _familyRoles = new List<int>();

        /// <summary>
        /// Gets or sets the Role Type. Used to help in loading Attribute panel
        /// </summary>
        protected int? RoleType
        {
            get { return ViewState["RoleType"] as int? ?? null; }
            set { ViewState["RoleType"] = value; }
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
            RockPage.AddCSSLink( ResolveRockUrl( "~/Styles/fluidbox.css" ) );
            RockPage.AddScriptLink( ResolveRockUrl( "~/Scripts/imagesloaded.min.js" ) );
            RockPage.AddScriptLink( ResolveRockUrl( "~/Scripts/jquery.fluidbox.min.js" ) );

            _familyRoles = GetAttributeValue( "FamilyRoles" ).SplitDelimitedValues().AsIntegerList();

            this.BlockUpdated += Block_BlockUpdated;

            var rockContext = new RockContext();

            _person = CurrentPerson;

            var introText = new HtmlContent();
            introText.Content = GetAttributeValue( "IntroText" );
            lIntroText.Text = introText.Content;

            pnlNotAuthorizedMessage.Visible = false;

            var allowedFamilyIds = new List<int>();

            foreach ( Group family in _person.GetFamilies() )
            {
                var roleId = family.Members
                    .Where( m => m.PersonId == _person.Id )
                    .OrderBy( m => m.GroupRole.Order )
                    .FirstOrDefault()
                    .GroupRoleId;

                if ( _familyRoles.Contains( roleId ) )
                {
                    allowedFamilyIds.Add( family.Id );
                }
            }

            if( allowedFamilyIds.Count() == 0 )
            {
                pnlView.Visible = false;
                btnSave.Visible = false;
                btnCancel.Visible = false;

                Literal notAuthorizedText = new Literal();
                notAuthorizedText.Text = GetAttributeValue( "NotAuthorizedMessage" );
                pnlNotAuthorizedMessage.Controls.Add( notAuthorizedText );
                pnlNotAuthorizedMessage.Visible = true;
            }

            pnlConfirmationMessage.Visible = false;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( _person != null )
            {
                ShowDetail();
            }
            else
            {
                pnlView.Visible = false;
                btnSave.Visible = false;
                btnCancel.Visible = false;
                pnlNotAuthorizedMessage.Visible = true;
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
            ShowDetail();
        }

        #region View Events

        /// <summary>
        /// Handles the ItemCommand event of the rptGroupMembers control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rptGroupMembers_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            int personId = e.CommandArgument.ToString().AsInteger();
        }


        /// <summary>
        /// Handles the ItemDataBound event of the rptGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptGroupMembers_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );

            var groupMember = e.Item.DataItem as GroupMember;
            var person = groupMember.Person;
            var lGroupMemberImage = e.Item.FindControl( "lGroupMemberImage" ) as Literal;
            var cbSelectFamilyMember = e.Item.FindControl( "cbSelectFamilyMember" ) as CheckBox;

            // Get all families that this person belongs to
            var families = person.GetFamilies().ToList();
            var familyNames = "";

            foreach ( Group family in families )
            {
                if ( families.Count > 1 )
                {
                    if ( families.First() == family )
                    {
                        familyNames += ( "(" + family.Name );
                    }
                    else if ( families.Last() == family )
                    {
                        familyNames += ( ", " + family.Name + ")" );
                    }
                    else
                    {
                        familyNames += ( ", " + family.Name + "" );
                    }
                }
                else if ( families.Count == 1 )
                {
                    familyNames = ( "(" + family.Name + ")" );
                }

            }

            familyNames = "<span style='font-size: 12px;'>" + familyNames + "</span>";

            // Person Info
            cbSelectFamilyMember.Text = ( "<span style='font-weight: bold; font-size: 16px; margin-right: 10px;'>" + person.FullName + "</span><span>" + familyNames + "</span>" );

        }

        #endregion

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var peopleToUpdateTrue = new List<int>();
            var peopleToUpdateFalse = new List<int>();

            foreach ( RepeaterItem item in rptGroupMembers.Items )
            {
                CheckBox chk = item.FindControl( "cbSelectFamilyMember" ) as CheckBox;
                var pid = chk.Attributes["CommandArgument"].AsInteger();
                if ( chk.Checked )
                {
                    peopleToUpdateTrue.Add( pid );
                }
                else
                {
                    peopleToUpdateFalse.Add( pid );
                }

            }

            foreach ( int pid in peopleToUpdateTrue )
            {
                var person = personService.Get( pid );
                if ( person != null )
                {
                    var attributeSetting = GetAttributeValue( "PersonAttribute" );
                    var attribute = AttributeCache.Get( attributeSetting );
                    //if ( attribute.FieldTypeId == 11 )
                    if( attribute.FieldType.Guid == Rock.SystemGuid.FieldType.DATE.AsGuid() || attribute.FieldType.Guid == Rock.SystemGuid.FieldType.DATE_TIME.AsGuid() )
                    {
                        string originalValue = person.GetAttributeValue( attribute.Key );
                        string newValue = RockDateTime.Now.ToString();
                        Rock.Attribute.Helper.SaveAttributeValue( person, attribute, newValue, rockContext );
                    }
                    else
                    {
                        Rock.Attribute.Helper.SaveAttributeValue( person, attribute, "True", rockContext );
                    }

                }
            }

            foreach ( int pid in peopleToUpdateFalse )
            {
                var person = personService.Get( pid );
                if ( person != null )
                {
                    var attributeSetting = GetAttributeValue( "PersonAttribute" );
                    var attribute = AttributeCache.Get( attributeSetting );
                    if ( attribute.FieldType.Guid == Rock.SystemGuid.FieldType.DATE.AsGuid() || attribute.FieldType.Guid == Rock.SystemGuid.FieldType.DATE_TIME.AsGuid() )
                    {
                        string originalValue = person.GetAttributeValue( attribute.Key );
                        string newValue = null;
                        Rock.Attribute.Helper.SaveAttributeValue( person, attribute, newValue, rockContext );
                    }
                    else
                    {
                        Rock.Attribute.Helper.SaveAttributeValue( person, attribute, "False", rockContext );
                    }

                }
            }

            if ( GetAttributeValue( "ConfirmationPage" ) != "" )
            {
                NavigateToLinkedPage( "ConfirmationPage" );
            }
            else
            {
                Literal confirmationText = new Literal();
                confirmationText.Text = GetAttributeValue( "ConfirmationMessage" );
                pnlConfirmationMessage.Controls.Add( confirmationText );
                pnlConfirmationMessage.Visible = true;
                pnlView.Visible = false;
                btnSave.Visible = false;
                btnCancel.Visible = false;
                lIntroText.Visible = false;
                pnlConfirmationMessage.Visible = true;
            }

        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            ShowDetail();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail()
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );

            if ( !IsPostBack && _person != null )
            {
                var personId = _person.Id;

                var allowedFamilyIds = new List<int>();

                foreach ( Group family in _person.GetFamilies() )
                {
                    var roleId = family.Members
                        .Where( m => m.PersonId == _person.Id )
                        .OrderBy( m => m.GroupRole.Order )
                        .FirstOrDefault()
                        .GroupRoleId;

                    if ( _familyRoles.Contains( roleId ) )
                    {
                        allowedFamilyIds.Add( family.Id );
                    }
                }


                var groups = new GroupService( rockContext )
                    .Queryable()
                    .Where( g => allowedFamilyIds.Contains( g.Id ) )
                    .ToList();

                if ( groups.Any() )
                {
                    var peopleDictionary = new Dictionary<int, GroupMember>();
                    foreach ( var group in groups )
                    {
                        foreach ( var groupMember in group.Members )
                        {
                            if ( !peopleDictionary.ContainsKey( groupMember.PersonId ) )
                            {
                                peopleDictionary.Add( groupMember.PersonId, groupMember );
                            }
                        }

                    }
                    rptGroupMembers.DataSource = peopleDictionary.Values.Where( gm =>
                         gm.Person.IsDeceased == false )
                         .OrderBy( m => m.GroupRole.Order )
                         .ToList();
                    rptGroupMembers.DataBind();
                }

                hfPersonId.Value = string.Empty;
                pnlView.Visible = true;

            }
        }


        #endregion
    }
}
