using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Security;

namespace RockWeb.Plugins.com_kingdomfirstsolutions.Groups
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Group Member Self Manage/Add/Remove" )]
    [Category( "KFS > Groups" )]
    [Description( "Can Add/Remove a person from a group based on inputs from the URL query string (GroupId, PersonGuid) or let them manage their group membership." )]
    [GroupField( "Parent Group", "This is the parent group whose first level descendents will be used to populate the list.", true, order: 0 )]
    [CodeEditorField( "Success Message", "Lava template to display when person saves their selections.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-success'>
    {{ Person.NickName }}, your selections have been successfully submitted.
</div>", order: 0 )]
    [CodeEditorField( "Remove Success Message", "Lava template to display when person has been removed from the group.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-success'>
    {{ Person.NickName }} has been removed from the group '{{ Group.Name }}'.
</div>", order: 1 )]
    [CodeEditorField( "Not In Group Message", "Lava template to display when person is not in the group.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-warning'>
    {{ Person.NickName }} was not in the group '{{ Group.Name }}'.
</div>", order: 2 )]
    [BooleanField( "Warn When Not In Group", "Determines if the 'Not In Group Message'should be shown if the person is not in the group. Otherwise the success message will be shown", true, order: 3 )]
    [BooleanField( "Inactivate Instead of Remove", "Inactivates the person in the group instead of removing them.", false, key: "Inactivate", order: 4 )]
    [GroupRoleField( "", "Default Group Member Role", "The default role to use if one is not passed through the query string (optional).", false, order: 5 )]
    [CodeEditorField( "Add Success Message", "Lava template to display when person has been added to the group.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-success'>
    {{ Person.NickName }} has been added to the group '{{ Group.Name }}' with the role of {{ Role.Name }}.
</div>", order: 6 )]
    [CodeEditorField( "Already In Group Message", "Lava template to display when person is already in the group with that role.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-warning'>
    {{ Person.NickName }} is already in the group '{{ Group.Name }}' with the role of {{ Role.Name }}.
</div>", order: 7 )]
    [EnumField( "Group Member Status", "The status to use when adding a person to the group.", typeof( GroupMemberStatus ), true, "Active", order: 8 )]
    [BooleanField( "Show Details", "Display detail information about the group beneath its checkbox when clicked.", order: 9 )]
    [BooleanField( "Expand Description", "Flag to determine whether to expand the description panel by default.", order: 10 )]
    [BooleanField( "None of the above", "Include a none of the above option when creating choices.", order: 11 )]

    [BooleanField( "Enable Debug", "Shows the Lava variables available for this block", order: 13 )]
    public partial class GroupMemberSelfManageAddRemove : Rock.Web.UI.RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        private string _action = string.Empty;
        protected Person _person = null;
        protected CheckBox cb;
        protected Literal lt;
        protected GroupMember gm;

        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RockContext rockContext = new RockContext();

            // get person
            Person person = null;

            if ( !string.IsNullOrWhiteSpace( Request["PersonGuid"] ) )
            {
                person = new PersonService( rockContext ).Get( Request["PersonGuid"].AsGuid() );
            }
            if ( person == null && CurrentPerson != null )
            {
                person = CurrentPerson;
            }

            if ( person == null )
            {
                lAlerts.Text += "A person could not be found for the identifier provided.";
                return;
            }
            _person = person;

            bool ExpandDescriptionSetting = GetAttributeValue( "ExpandDescription" ).AsBoolean();
            if ( !ExpandDescriptionSetting )
            {
                LiteralControl expandDescriptions = new LiteralControl( "<script src='/Plugins/com_kingdomfirstsolutions/Groups/js/expandDescription.js'></script>" );
                this.Page.Header.Controls.Add( expandDescriptions );
            }


            if ( !Page.IsPostBack )
            {

                Group group = null;
                Guid personGuid = Guid.Empty;
                GroupTypeRole groupMemberRole = null;

                // get group from url
                if ( Request["GroupId"] != null || Request["GroupGuid"] != null )
                {
                    if ( Request["GroupId"] != null )
                    {
                        int groupId = 0;
                        if ( Int32.TryParse( Request["GroupId"], out groupId ) )
                        {
                            group = new GroupService( rockContext ).Queryable().Where( g => g.Id == groupId ).FirstOrDefault();
                        }
                    }
                    else
                    {
                        Guid groupGuid = Request["GroupGuid"].AsGuid();
                        group = new GroupService( rockContext ).Queryable().Where( g => g.Guid == groupGuid ).FirstOrDefault();
                    }
                }
                else
                {
                    Guid groupGuid = Guid.Empty;
                    if ( Guid.TryParse( GetAttributeValue( "ParentGroup" ), out groupGuid ) )
                    {
                        group = new GroupService( rockContext ).Queryable().Where( g => g.Guid == groupGuid ).FirstOrDefault();
                    }
                }

                if ( group == null )
                {
                    lAlerts.Text = "Could not determine the group to add/remove from.";
                    return;
                }

                    _action = Request.QueryString["action"] != string.Empty && Request.QueryString["action"] != null ? Request.QueryString["action"] : string.Empty;


                // get status
                var groupMemberStatus = this.GetAttributeValue( "GroupMemberStatus" ).ConvertToEnum<GroupMemberStatus>( GroupMemberStatus.Active );

                // load merge fields
                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "Group", group );
                mergeFields.Add( "Person", person );
                mergeFields.Add( "CurrentPerson", CurrentPerson );
                mergeFields.Add( "GroupMemberStatus", groupMemberStatus.ToString() );

                var groupMemberService = new GroupMemberService( rockContext );

                if ( _action == "unsubscribe" )
                {
                    var groupMemberList = groupMemberService.Queryable()
                                            .Where( m => m.GroupId == group.Id && m.PersonId == person.Id )
                                            .ToList();

                    if ( groupMemberList.Count > 0 )
                    {
                        foreach ( var groupMember in groupMemberList )
                        {
                            if ( GetAttributeValue( "Inactivate" ).AsBoolean() )
                            {
                                groupMember.GroupMemberStatus = GroupMemberStatus.Inactive;
                            }
                            else
                            {
                                groupMemberService.Delete( groupMember );
                            }

                            rockContext.SaveChanges();
                        }

                        lContent.Text = GetAttributeValue( "RemoveSuccessMessage" ).ResolveMergeFields( mergeFields );
                    }
                    else
                    {
                        if ( GetAttributeValue( "WarnWhenNotInGroup" ).AsBoolean() )
                        {
                            lContent.Text = GetAttributeValue( "NotInGroupMessage" ).ResolveMergeFields( mergeFields );
                        }
                        else
                        {
                            lContent.Text = GetAttributeValue( "RemoveSuccessMessage" ).ResolveMergeFields( mergeFields );
                        }
                    }
                }
                else if ( _action == "subscribe" )
                {
                    // get group role id from url
                    if ( Request["GroupMemberRoleId"] != null )
                    {
                        int groupMemberRoleId = 0;
                        if ( Int32.TryParse( Request["GroupMemberRoleId"], out groupMemberRoleId ) )
                        {
                            groupMemberRole = new GroupTypeRoleService( rockContext ).Get( groupMemberRoleId );
                        }
                    }
                    else
                    {
                        Guid groupMemberRoleGuid = Guid.Empty;
                        if ( Guid.TryParse( GetAttributeValue( "DefaultGroupMemberRole" ), out groupMemberRoleGuid ) )
                        {
                            groupMemberRole = new GroupTypeRoleService( rockContext ).Get( groupMemberRoleGuid );
                        }
                    }

                    if ( groupMemberRole == null )
                    {
                        lAlerts.Text += "Could not determine the group role to use for the add.";
                        return;
                    }

                    mergeFields.Add( "Role", groupMemberRole );

                    var groupMemberList = groupMemberService.Queryable()
                                            .Where( m => m.PersonId == person.Id && m.GroupRoleId == groupMemberRole.Id )
                                            .ToList();

                    // ensure that the person is not already in the group
                    if ( groupMemberList.Count() != 0 )
                    {
                        foreach ( var groupMemberExisting in groupMemberList )
                        {
                            if ( GetAttributeValue( "Inactivate" ).AsBoolean() )
                            {
                                groupMemberExisting.GroupMemberStatus = groupMemberStatus;
                            }
                            else
                            {
                                string templateInGroup = GetAttributeValue( "AlreadyInGroupMessage" );
                                lContent.Text = templateInGroup.ResolveMergeFields( mergeFields );
                                divAlert.Visible = false;
                                return;
                            }
                        }

                    }
                    else
                    {
                        // add person to group
                        GroupMember groupMember = new GroupMember();
                        groupMember.GroupId = group.Id;
                        groupMember.PersonId = person.Id;
                        groupMember.GroupRoleId = groupMemberRole.Id;
                        groupMember.GroupMemberStatus = groupMemberStatus;
                        group.Members.Add( groupMember );
                    }
                    try
                    {
                        rockContext.SaveChanges();
                    }
                    catch ( Exception ex )
                    {
                        divAlert.Visible = true;
                        lAlerts.Text = String.Format( "An error occurred adding {0} to the group {1}. Message: {2}.", person.FullName, group.Name, ex.Message );
                    }

                    string templateSuccess = GetAttributeValue( "AddSuccessMessage" );
                    lContent.Text = templateSuccess.ResolveMergeFields( mergeFields );

                }
                // hide alert
                divAlert.Visible = false;

                // show debug info?
                bool enableDebug = GetAttributeValue( "EnableDebug" ).AsBoolean();
                if ( enableDebug && IsUserAuthorized( Authorization.EDIT ) )
                {
                    lDebug.Visible = true;
                    lDebug.Text = mergeFields.lavaDebugInfo();
                }

            }
            loadProfiles();
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }
        protected void btnSave_Click( object sender, EventArgs e )
        {
            //
            // Walk each of the controls found and determine if we need to
            // take any action for the value of that control.
            //
            var checkboxes = new List<Control>();
            Type[] types = new Type[] { typeof( CheckBox ), typeof( RadioButton ) };
            KFSFindControlsRecursive( phGroups, types, ref checkboxes );
            foreach ( Control c in checkboxes )
            {
                SaveSelections( c );
            }

            var mergeFields = new Dictionary<string, object>();
            mergeFields.Add( "Person", _person );
            mergeFields.Add( "CurrentPerson", CurrentPerson );

            string templateSuccess = GetAttributeValue( "SuccessMessage" );
            lContent.Text = templateSuccess.ResolveMergeFields( mergeFields );
        }

        #endregion

        #region Methods
        protected void SaveSelections( Control c )
        {
            CheckBox cbox;
            int i, groupID;
            GroupTypeRole groupMemberRole = null;
            Guid personGuid = Guid.Empty;

            //
            // If the control is not a checkbox or radio button then
            // ignore it.
            //
            if ( c.GetType() != typeof( CheckBox ) && c.GetType() != typeof( RadioButton ) )
                return;
            //
            // Pretend the control is a checkbox, if it is a radio button
            // it will cast fine since the radio button inherits from a
            // check box.
            //
            cbox = ( CheckBox ) c;
            if ( cbox.ID.Contains( "_none" ) )
            {
                return;
            }
            groupID = 0;
            if ( !Int32.TryParse( cbox.ID, out groupID ) )
            {
                int stFrom = cbox.ID.IndexOf( "profile_" ) + "profile_".Length;
                int stTo = cbox.ID.LastIndexOf( "_field" );
                groupID = Int32.Parse( cbox.ID.Substring( stFrom, stTo - stFrom ) );
            }
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var groupMemberService = new GroupMemberService( rockContext );
            var groupMemberStatus = this.GetAttributeValue( "GroupMemberStatus" ).ConvertToEnum<GroupMemberStatus>( GroupMemberStatus.Active );

            var qry = groupService
                .Queryable()
                .Where( g => g.Id == groupID );

            var group = qry.FirstOrDefault();
            if ( group != null )
            {
                // get person
                Person person = null;

                if ( !string.IsNullOrWhiteSpace( Request["PersonGuid"] ) )
                {
                    person = new PersonService( rockContext ).Get( Request["PersonGuid"].AsGuid() );
                }
                if ( person == null && CurrentPerson != null )
                {
                    person = CurrentPerson;
                }

                if ( person == null )
                {
                    lAlerts.Text += "A person could not be found for the identifier provided.";
                }
                _person = person;

                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "Group", group );
                mergeFields.Add( "Person", person );
                mergeFields.Add( "CurrentPerson", CurrentPerson );
                mergeFields.Add( "GroupMemberStatus", groupMemberStatus.ToString() );

                if ( cbox.Checked == true )
                {

                    // get group role id from url
                    Guid groupMemberRoleGuid = Guid.Empty;
                    if ( Guid.TryParse( GetAttributeValue( "DefaultGroupMemberRole" ), out groupMemberRoleGuid ) )
                    {
                        groupMemberRole = new GroupTypeRoleService( rockContext ).Get( groupMemberRoleGuid );
                    }
                    if ( groupMemberRole == null && group != null && group.GroupType.DefaultGroupRoleId.HasValue )
                    {
                        groupMemberRole = group.GroupType.DefaultGroupRole;
                    }

                    if ( groupMemberRole == null )
                    {
                        lAlerts.Text += "Could not determine the group role to use for the add.";
                    }
                    mergeFields.Add( "Role", groupMemberRole );

                    var groupMemberList = groupMemberService.Queryable()
                                            .Where( m => m.PersonId == person.Id && m.GroupId == group.Id && m.GroupRoleId == groupMemberRole.Id )
                                            .ToList();

                    // ensure that the person is not already in the group
                    if ( groupMemberList.Count() != 0 )
                    {
                        foreach ( var groupMemberExisting in groupMemberList )
                        {
                            if ( GetAttributeValue( "Inactivate" ).AsBoolean() )
                            {
                                groupMemberExisting.GroupMemberStatus = groupMemberStatus;
                            }
                            else
                            {
                                string templateInGroup = GetAttributeValue( "AlreadyInGroupMessage" );
                                lContent.Text += templateInGroup.ResolveMergeFields( mergeFields );
                                divAlert.Visible = false;
                            }
                        }

                    }
                    else
                    {
                        // add person to group
                        GroupMember groupMember = new GroupMember();
                        groupMember.GroupId = group.Id;
                        groupMember.PersonId = person.Id;
                        groupMember.GroupRoleId = groupMemberRole.Id;
                        groupMember.GroupMemberStatus = groupMemberStatus;
                        group.Members.Add( groupMember );
                    }
                    try
                    {
                        rockContext.SaveChanges();
                    }
                    catch ( Exception ex )
                    {
                        divAlert.Visible = true;
                        lAlerts.Text += String.Format( "An error occurred adding {0} to the group {1}. Message: {2}.", person.FullName, group.Name, ex.Message );
                    }

                    string templateSuccess = GetAttributeValue( "AddSuccessMessage" );
                    lContent.Text = templateSuccess.ResolveMergeFields( mergeFields );
                }
                if ( !cbox.Checked )
                {
                    var groupMemberList = groupMemberService.Queryable()
                                            .Where( m => m.GroupId == group.Id && m.PersonId == person.Id )
                                            .ToList();

                    if ( groupMemberList.Count > 0 )
                    {
                        foreach ( var groupMember in groupMemberList )
                        {
                            if ( GetAttributeValue( "Inactivate" ).AsBoolean() )
                            {
                                groupMember.GroupMemberStatus = GroupMemberStatus.Inactive;
                            }
                            else
                            {
                                groupMemberService.Delete( groupMember );
                            }

                            rockContext.SaveChanges();
                        }

                        lContent.Text = GetAttributeValue( "RemoveSuccessMessage" ).ResolveMergeFields( mergeFields );
                    }
                    else
                    {
                        if ( GetAttributeValue( "WarnWhenNotInGroup" ).AsBoolean() )
                        {
                            lContent.Text = GetAttributeValue( "NotInGroupMessage" ).ResolveMergeFields( mergeFields );
                        }
                        else
                        {
                            lContent.Text = GetAttributeValue( "RemoveSuccessMessage" ).ResolveMergeFields( mergeFields );
                        }
                    }
                }
            }
        }

        protected void loadProfiles()
        {
            // Get query of groups of the selected group type
            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );
            Guid groupGuid = Guid.Empty;
            Guid.TryParse( GetAttributeValue( "ParentGroup" ), out groupGuid );

            var groupQry = groupService
                .Queryable()
                .Where( g => g.IsActive && g.ParentGroup.Guid == groupGuid && g.IsPublic );

            List<Group> groups = null;

            groups = groupQry.OrderBy( g => g.Name ).ToList();

            foreach ( var g in groups )
            {

                //
                // Create a div to add each profile check box into.
                // This is gives us a place to add additional content when checked.
                //
                Panel pnl = new Panel();
                pnl.ID = "pnl_" + g.Id;
                //
                // Find the member in the profile if they are already there.
                //

                var groupMemberService = new GroupMemberService( rockContext );

                var groupMemberList = groupMemberService.Queryable()
                                            .Where( m => m.GroupId == g.Id && m.PersonId == _person.Id )
                                            .ToList();

                if ( groupMemberList.Count > 0 )
                {
                    foreach ( var groupMember in groupMemberList )
                    {
                        gm = groupMember;
                    }
                }

                cb = new CheckBox();

                //
                // Fill in all the control information and add it to the list.
                //
                cb.ID = g.Id.ToString();
                cb.Attributes.Add( "name", "cb_profile_" + g.Id.ToString() );
                cb.Text = g.Name;
                cb.CssClass = "sjt_profile";
                var groupMemberStatus = this.GetAttributeValue( "GroupMemberStatus" ).ConvertToEnum<GroupMemberStatus>( GroupMemberStatus.Active );
                if ( gm != null && gm.GroupMemberStatus == groupMemberStatus )
                {
                    cb.Checked = true;
                    cb.CssClass += " sjt_disabled";
                }
                pnl.Controls.Add( cb );
                phGroups.Controls.Add( pnl );

                //
                // Add a div to contain detail information
                //
                bool ShowDetailsSetting = GetAttributeValue( "ShowDetails" ).AsBoolean();
                bool ExpandDescriptionSetting = GetAttributeValue( "ExpandDescription" ).AsBoolean();

                if ( ShowDetailsSetting != false )
                {
                    Panel pnlDet = new Panel();
                    pnlDet.ID = "pnlDet_" + cb.ID;
                    Label lbl = new Label();
                    lbl.ID = "lblDesc_" + cb.ID;
                    lbl.Text = "<b>Description: </b>" + g.Description;
                    lbl.CssClass = "profileDescriptionLabel";
                    pnlDet.Controls.Add( lbl );
                    if ( !ExpandDescriptionSetting )
                        pnlDet.CssClass = "profileDetailsContainer";
                    else
                        pnlDet.CssClass = "profileDetailsContainerExpand";
                    pnl.Controls.Add( pnlDet );

                    if ( !ExpandDescriptionSetting )
                        pnlDet.Style.Add( "display", "none" );
                }
            }
            //
            // Add in a "none of the above" option if requested. This does
            // not do anything on submit, but resolves any "I changed my mind
            // how do I get out" questions.
            //
            bool NoneAboveSetting = GetAttributeValue( "Noneoftheabove" ).AsBoolean();
            if ( NoneAboveSetting != false )
            {
                Panel pnl = new Panel();
                pnl.ID = "pnl_" + 0 + "_none";
                //
                // Create either the checkbox or the radio button depending on
                // the module setting.
                //
                cb = new CheckBox();
                //
                // Setup the information about the none of the above control.
                //
                cb.ID = "0_none";
                cb.Text = "None of the above";
                cb.CssClass = "sjt_profile";
                pnl.Controls.Add( cb );
                phGroups.Controls.Add( pnl );
                //
                // Add in a newline character.
                //
                lt = new Literal();
                lt.Text = "<br />";
                phGroups.Controls.Add( lt );
            }
        }

        // helper functional methods (like BindGrid(), etc.)

        protected void KFSFindControlsRecursive( Control root, Type[] types, ref List<Control> list )
        {
            if ( root.Controls.Count != 0 )
            {
                foreach ( Control c in root.Controls )
                {
                    if ( types.Contains( c.GetType() ) )
                        list.Add( c );
                    else if ( c.HasControls() )
                        KFSFindControlsRecursive( c, types, ref list );
                }
            }
        }

        /// <summary>
        /// Shows the error.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="text">The text.</param>
        private void ShowError( string title, string text )
        {
            nbError.Title = title;
            nbError.Text = text;
            nbError.Visible = true;
        }

        /// <summary>
        /// Shows the success.
        /// </summary>
        /// <param name="text">The text.</param>
        private void ShowSuccess( string text )
        {
            pnlInputInfo.Visible = false;
            pnlSuccess.Visible = true;
            nbSuccess.Text = text;
        }

        #endregion
    }
}
