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

    [MemoField( "Intro Text", "The text to instruct users how and why to use this form.", false, "", "", 1, null, 3, true )]
    //[CodeEditorField( "Intro Text", "The text to instruct users how and why to use this form.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, "", "", 3 )]
    [GroupRoleField( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY, "Family Role", "The family role that has access to edit this attribute for other family members." , true, "Adult", "", 2 )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Person Attribute", "The person attribute that will be set for each selected family member. If it's a datetime attribute, current datetime will be saved, otherwise \"True\" will be the value.", true, true, order: 3 )]
    [TextField( "Confirmation Text", "The text to display when information has been successfully submitted.", false, "", "", 4 )]
    [LinkedPage( "Confirmation Page", "The page to redirect the user to once the form has been submitted. Overrides the Confirmation Text setting.", false, "", "", 5)]
    [TextField( "Not Authorized Message", "The message to display if a user that is not in one of the selected group type roles lands on this block.", false, "", "", 6)]

    #endregion

    public partial class OptInFamily : RockBlock
    {
        #region Properties

        private Person _person = null;

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
                if ( !Page.IsPostBack )
                {
                    BindFamilies();
                }
                else
                {
                    var rockContext = new RockContext();
                    var group = new GroupService( rockContext ).Get( ddlGroup.SelectedValueAsId().Value );
                    var person = new PersonService( rockContext ).Get( hfPersonId.ValueAsInt() );
                    if ( person != null && group != null )
                    {

                    }
                    if ( person == null && RoleType != null )
                    {
 
                    }
                }
            }
            else
            {
                pnlView.Visible = false;
                //nbNotAuthorized.Visible = true;
            }
        }

        private void BindFamilies()
        {
            ddlGroup.DataSource = _person.GetFamilies().ToList();
            ddlGroup.DataBind();
            ShowDetail();
        }

        #endregion

        #region Events

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

            foreach ( Group fam in families )
            {
                if ( families.Count > 1 )
                {
                    if ( families.First() == fam )
                    {
                        familyNames += ( "(" + fam.Name );
                    }
                    else if ( families.Last() == fam )
                    {
                        familyNames += ( ", " + fam.Name + ")" );
                    }
                    else
                    {
                        familyNames += ( ", " + fam.Name + "" );
                    }
                }
                else if (families.Count == 1)
                {
                    familyNames = ( "(" + fam.Name + ")" );
                }

            }

            familyNames = "<span style='font-size: 12px;'>" + familyNames + "</span>";

            // Person Info
            cbSelectFamilyMember.Text = ( "<span style='font-weight: bold; font-size: 16px; margin-right: 10px;'>" + person.FullName + "</span><span>" + familyNames +"</span>" );

        }

        #endregion

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {

            //var peopleToUpdate = new List<Person>();

            //foreach (Control c in pnlView.Controls )
            //{
            //    if ( (c is CheckBox ) && ((CheckBox) c).Checked){
            //        //var person = new PersonService( rockContext ).Get( c.ID );
            //        var chk = (CheckBox)c;
            //        var personId = chk.Attributes["CommandArgument"].AsInteger();
            //        var person = new Person( personId );       
            //    }
            //}


            var rockContext = new RockContext();
            if ( ddlGroup.SelectedValueAsId().HasValue )
            {
                var group = new GroupService( rockContext ).Get( ddlGroup.SelectedValueAsId().Value );
                if ( group != null )
                {
                    rockContext.WrapTransaction( () =>
                    {
                        var personService = new PersonService( rockContext );

                        var peopleToUpdate = new List<int>();

                        foreach ( Control c in pnlView.Controls )
                        {
                            if ( ( c is CheckBox ) && ( (CheckBox)c ).Checked )
                            {
                                //var person = new PersonService( rockContext ).Get( c.ID );
                                var chk = (CheckBox)c;
                                var pid = chk.Attributes["CommandArgument"].AsInteger();
                                peopleToUpdate.Add( pid );
                            }
                        }

                        //var personId = hfPersonId.Value.AsInteger();
                        //if ( personId == 0 )
                        //{
                        //    var groupMemberService = new GroupMemberService( rockContext );
                        //    var groupMember = new GroupMember() { Person = new Person(), Group = group, GroupId = group.Id };

                        //    var headOfHousehold = GroupServiceExtensions.HeadOfHousehold( group.Members.AsQueryable() );
                        //    if ( headOfHousehold != null )
                        //    {
                        //        DefinedValueCache dvcRecordStatus = DefinedValueCache.Get( headOfHousehold.RecordStatusValueId ?? 0 );
                        //        if ( dvcRecordStatus != null )
                        //        {
                        //            groupMember.Person.RecordStatusValueId = dvcRecordStatus.Id;
                        //        }
                        //    }

                        //    if ( groupMember.GroupRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() )
                        //    {
                        //        groupMember.Person.GivingGroupId = group.Id;
                        //    }

                        //    groupMember.Person.IsEmailActive = true;
                        //    groupMember.Person.EmailPreference = EmailPreference.EmailAllowed;
                        //    groupMember.Person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;

                        //    groupMemberService.Add( groupMember );
                        //    rockContext.SaveChanges();
                        //    personId = groupMember.PersonId;
                        //}

                        foreach( int pid in peopleToUpdate )
                        {
                            var person = personService.Get( pid );
                            if ( person != null )
                            {
                                //person.LoadAttributes();

                                //var attributeToChange = GetAttributeValue( "PersonAttribute" );
                                //if ( attributeToChange.GetType() =  )
                                //{
                                //    person.SetAttributeValue( attributeToChange, 
                                //}
                                //else
                                //{
                                //    person.ConnectionStatusValueId = CurrentPerson.ConnectionStatusValueId;
                                //}
                            }
                        }

                        

                    } );

                    var queryString = new Dictionary<string, string>();

                    var personParam = PageParameter( "Person" );
                    if ( !string.IsNullOrWhiteSpace( personParam ) )
                    {
                        queryString.Add( "Person", personParam );
                    }

                    NavigateToPage( RockPage.Guid, queryString );
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

 
                if ( _person.GetFamilies().Count() > 1 )
                {
                    ddlGroup.Visible = true;
                }

                    if ( ddlGroup.SelectedValueAsId().HasValue )
                    {
                        var group = new GroupService( rockContext ).Get( ddlGroup.SelectedValueAsId().Value );
                        if ( group != null )
                        {
                            // Family Name
                            //lGroupName.Text = group.Name;

                            rptGroupMembers.DataSource = group.Members.Where( gm =>
                                gm.Person.IsDeceased == false )
                                .OrderBy( m => m.GroupRole.Order )
                                .ToList();
                            rptGroupMembers.DataBind();
                        }
                    }
            }

            hfPersonId.Value = string.Empty;
            pnlView.Visible = true;
        }


        #endregion
    }
}
