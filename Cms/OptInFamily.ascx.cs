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

    //[MemoField( "Intro Text", "The text to instruct users how and why to use this form.", false, "", "", 1, null, 3, true )]
    [CodeEditorField( "Intro Text", "The text to instruct users how and why to use this form.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, "", "", 3 )]
    //[GroupRoleField( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY, "Family Role", "The family role that has access to edit this attribute for other family members.", true, "Adult", "", 2 )]
    [CustomCheckboxListField( "Family Roles", "The Family Roles which can edit this attribute.", "SELECT [r].[Id] AS [Value], [r].[Name] AS [Text] FROM [GroupTypeRole] [r] JOIN [GroupType] [t] ON [r].[GroupTypeId] = [t].[Id] WHERE [t].[Guid] LIKE '790E3215-3B10-442B-AF69-616C0DCB998E'", true )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Person Attribute", "The person attribute that will be set for each selected family member. If it's a datetime attribute, current datetime will be saved, otherwise \"True\" will be the value.", true, false, order: 3 )]
    [TextField( "Confirmation Text", "The text to display when information has been successfully submitted.", false, "", "", 4 )]
    [LinkedPage( "Confirmation Page", "The page to redirect the user to once the form has been submitted. Overrides the Confirmation Text setting.", false, "", "", 5 )]
    [TextField( "Not Authorized Message", "The message to display if a user that is not in one of the selected group type roles lands on this block.", false, "", "", 6 )]

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

        private bool _canEdit = false;

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

            //_canEdit = !GetAttributeValue( "ViewOnly" ).AsBoolean();

            var rockContext = new RockContext();

            // If impersonation is allowed, and a valid person key was used, set the target to that person
            if ( GetAttributeValue( "Impersonation" ).AsBooleanOrNull() ?? false )
            {
                string personKey = PageParameter( "Person" );
                if ( !string.IsNullOrWhiteSpace( personKey ) )
                {
                    //var rockContext = new RockContext();
                    _person = new PersonService( rockContext ).GetByUrlEncodedKey( personKey );
                }
            }

            if ( _person == null )
            {
                _person = CurrentPerson;
            }

            var introText = new HtmlContent();
            introText.Content = GetAttributeValue( "IntroText" );
            lIntroText.Text = introText.Content;
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
                //nbNotAuthorized.Visible = true;
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
        /// Handles the Click event of the lbRequestChanges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbRequestChanges_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "WorkflowLaunchPage" );
        }

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
            if ( ddlGroup.SelectedValueAsId().HasValue )
            {
                var group = new GroupService( rockContext ).Get( ddlGroup.SelectedValueAsId().Value );
                if ( group != null )
                {
                    var personService = new PersonService( rockContext );
                    var peopleToUpdate = new List<int>();

                    foreach ( RepeaterItem item in rptGroupMembers.Items )
                    {
                        var chk = item.FindControl( "cbSelectFamilyMember" ) as CheckBox;

                        if ( chk.Checked )
                        {
                            var pid = chk.Attributes["CommandArgument"].AsInteger();
                            peopleToUpdate.Add( pid );
                        }

                    }

                    foreach ( int pid in peopleToUpdate )
                    {
                        var person = personService.Get( pid );
                        if ( person != null )
                        {
                            //person.LoadAttributes();
                            var attributeSetting = GetAttributeValue( "PersonAttribute" );
                            var attribute = AttributeCache.Get( attributeSetting );
                            if ( attribute.FieldTypeId == 11 )
                            {
                                string originalValue = person.GetAttributeValue( attribute.Key );
                                //string newValue = attribute.FieldType.Field.GetEditValue( attributeControl, attribute.QualifierValues );
                                //Rock.Attribute.Helper.SaveAttributeValue( person, attribute, newValue, rockContext );

                                string newValue = RockDateTime.Now.ToString();
                                Rock.Attribute.Helper.SaveAttributeValue( person, attribute, newValue, rockContext );
                            }
                            else
                            {
                                Rock.Attribute.Helper.SaveAttributeValue( person, attribute, "True", rockContext );
                            }

                        }
                    }

                    //var queryString = new Dictionary<string, string>();

                    //var personParam = PageParameter( "Person" );
                    //if ( !string.IsNullOrWhiteSpace( personParam ) )
                    //{
                    //    queryString.Add( "Person", personParam );
                    //}

                    //NavigateToPage( RockPage.Guid, queryString );
                }
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

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlGroup_SelectedIndexChanged( object sender, EventArgs e )
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

            if ( _person != null )
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
                    .ToList(); //.Get( ddlGroup.SelectedValueAsId().Value );

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

            }

            hfPersonId.Value = string.Empty;
            pnlView.Visible = true;
        }


        #endregion
    }
}
