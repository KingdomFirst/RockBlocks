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
    [GroupField( "Default Group", "The default group to use if one is not passed through the query string (optional).", false, order: 0 )]
    [CodeEditorField( "Remove Success Message", "Lava template to display when person has been removed from the group.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-success'>
    {{ Person.NickName }} has been removed from the group '{{ Group.Name }}'.
</div>", order: 1 )]
    [CodeEditorField( "Not In Group Message", "Lava template to display when person is not in the group.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-warning'>
    {{ Person.NickName }} was not in the group '{{ Group.Name }}'.
</div>", order: 2 )]
    [BooleanField( "Warn When Not In Group", "Determines if the 'Not In Group Message'should be shown if the person is not in the group. Otherwise the success message will be shown", true, order: 3 )]
    [BooleanField( "Inactivate Instead of Remove", "Inactivates the person in the group instead of removing them.", false, key: "Inactivate", order: 4 )]
    [GroupRoleField( "", "Default Group Member Role", "The default role to use if one is not passed through the query string (optional).", false )]
    [CodeEditorField( "Add Success Message", "Lava template to display when person has been added to the group.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-success'>
    {{ Person.NickName }} has been added to the group '{{ Group.Name }}' with the role of {{ Role.Name }}.
</div>" )]
    [CodeEditorField( "Already In Group Message", "Lava template to display when person is already in the group with that role.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"<div class='alert alert-warning'>
    {{ Person.NickName }} is already in the group '{{ Group.Name }}' with the role of {{ Role.Name }}.
</div>" )]
    [EnumField( "Group Member Status", "The status to use when adding a person to the group.", typeof( GroupMemberStatus ), true, "Active" )]
    [BooleanField( "Enable Debug", "Shows the Lava variables available for this block" )]
    public partial class GroupMemberSelfManageAddRemove : Rock.Web.UI.RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        private string _action = string.Empty;

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

            if ( !Page.IsPostBack )
            {
                RockContext rockContext = new RockContext();

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
                    if ( Guid.TryParse( GetAttributeValue( "DefaultGroup" ), out groupGuid ) )
                    {
                        group = new GroupService( rockContext ).Queryable().Where( g => g.Guid == groupGuid ).FirstOrDefault();
                        ;
                    }
                }

                if ( group == null )
                {
                    lAlerts.Text = "Could not determine the group to add/remove from.";
                    return;
                }

                // get person
                Person person = null;

                if ( !string.IsNullOrWhiteSpace( Request["PersonGuid"] ) )
                {
                    person = new PersonService( rockContext ).Get( Request["PersonGuid"].AsGuid() );
                }

                if ( person == null )
                {
                    lAlerts.Text += "A person could not be found for the identifier provided.";
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

        #endregion

        #region Methods

        // helper functional methods (like BindGrid(), etc.)

        #endregion
    }
}
