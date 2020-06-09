// <copyright>
// Copyright 2020 by Kingdom First Solutions
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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using DotLiquid;

namespace RockWeb.Plugins.kfs_rocks.Crm
{
    #region Block Detail
    /// <summary>
    /// Person Contact note quick entry with pre-filled questions and customizable person info display.
    /// </summary>
    [DisplayName( "Person Contact Note" )]
    [Category( "KFS > CRM" )]
    [Description( "Person or Group Member contact note entry." )]
    #endregion

    #region Block Settings

    [TextField( "Note Term", "The term to use for note (i.e. 'Note', 'Comment').", false, "Note", "", 1 )]
    [CustomDropdownListField( "Display Type", "The format to use for displaying notes.", "Full,Light", true, "Full", "Notes Column", 2 )]
    [BooleanField( "Show Alert Checkbox", "", true, "Notes Column", 3 )]
    [BooleanField( "Show Private Checkbox", "", true, "Notes Column", 4 )]
    [BooleanField( "Show Security Button", "", true, "Notes Column", 5 )]
    [BooleanField( "Allow Backdated Notes", "", false, "Notes Column", 6 )]
    [NoteTypeField( "Person Note Types", "Optional list of person note types to limit display to", true, "Rock.Model.Person", "", "", false, "", "", 7 )]
    [NoteTypeField( "Group Member Note Types", "Optional list of group member note types to limit display to", true, "Rock.Model.GroupMember", "", "", false, "", "", 8 )]
    [BooleanField( "Display Note Type Heading", "Should each note's Note Type be displayed as a heading above each note?", false, "Notes Column", 9 )]
    [BooleanField( "Expand Replies", "Should replies be automatically expanded?", false, "Notes Column", 10 )]
    [CodeEditorField( "Note View Lava Template", "The Lava Template to use when rendering the view of the notes.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 100, false, @"{% include '~~/Assets/Lava/NoteViewList.lava' %}", "Notes Column", 11 )]
    [MemoField( "Default Note Text", "Enter default note text such as pre-filled questions here.", false, "", "Notes Column", 12 )]
    [CodeEditorField( "Person Info Lava Template", "The Lava Template to use when rendering the person that was selected.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 100, false, @"{% include '~/Plugins/rocks_kfs/crm/Lava/PersonInfo.lava' %}", "Person Info", 13 )]
    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for the Person Info block.", false, "", "Person Info", 14 )]
    [WorkflowTypeField( "Workflow", "Workflow to initiate after contact note is entered.", false, false, "", "", 15 )]
    [CustomDropdownListField( "Workflow Entity", "", "Person,Note,GroupMember", true, "Person", "", 16 )]
    [BooleanField( "Start new search after adding a note", "Should each time you add/save a note the page restart from the person picker?", false, "", 17, "StartNewSearch" )]
    #endregion
    public partial class Notes : RockBlock
    {
        #region Private Variables

        private int? _entityTypeId = null;
        private int? _entityId = null;
        private int? _noteId = null;
        private RockContext _rockContext = new RockContext();

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Notes_BlockUpdated;

            var groupId = PageParameter( "GroupId" ).AsIntegerOrNull();
            var personId = PageParameter( "PersonId" ).AsIntegerOrNull();
            var groupMemberId = PageParameter( "GroupMember" ).AsIntegerOrNull();

            if ( groupId.HasValue || groupMemberId.HasValue )
            {
                _entityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP_MEMBER ).Id;
                gmpGroupMember.Visible = true;
                ppPerson.Visible = false;
                gmpGroupMember.GroupId = groupId;
                gmpGroupMember.AutoPostBack = true;
                lblPersonInfoHdr.Text = "Select a Group Member";
                if ( groupMemberId.HasValue )
                {
                    var groupMember = new GroupMemberService( _rockContext ).Get( groupMemberId.Value );
                    gmpGroupMember.GroupId = groupMember.GroupId;
                    gmpGroupMember.SetValue( groupMemberId );
                    _entityId = groupMemberId;
                }
            }
            else
            {
                _entityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.PERSON ).Id;
                ppPerson.Visible = true;
                gmpGroupMember.Visible = false;
                if ( personId.HasValue )
                {
                    var person = new PersonService( _rockContext ).Get( personId.Value );
                    ppPerson.SetValue( person );
                    _entityId = personId;
                }
            }

            ShowPersonInfo();
            ShowNotes();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Notes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Notes_BlockUpdated( object sender, EventArgs e )
        {
            ShowNotes();
        }

        /// <summary>
        /// Handles the Click event of the NoteEditor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void note_SaveButtonClick( object sender, NoteEventArgs e )
        {
            //var noteEditor = ( NoteEditor ) notesTimeline.Controls[0];
            //noteEditor.Text = string.Empty;
            //noteEditor.IsAlert = false;
            //noteEditor.IsPrivate = false;
            //noteEditor.NoteId = null;
            _noteId = e.NoteId;
            LaunchWorkflow();

            if ( GetAttributeValue( "StartNewSearch" ).AsBoolean() )
            {
                Response.Redirect( Request.Path );
            }
            else
            {
                ShowNotes();
            }
        }

        /// <summary>
        /// Handles the SelectPerson event of the Notes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void SelectPerson( object sender, EventArgs e )
        {
            var groupMemberId = gmpGroupMember.SelectedValueAsId();
            var url = Request.Path;
            if ( ppPerson.SelectedValue.HasValue )
            {
                url += string.Format( "?PersonId={0}", ppPerson.SelectedValue.ToString() );
            }
            else if ( groupMemberId.HasValue )
            {
                url += string.Format( "?GroupMember={0}", groupMemberId.ToString() );
            }

            Response.Redirect( url, true );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Renders the notes.
        /// </summary>
        private void ShowNotes()
        {
            if ( gmpGroupMember.SelectedValueAsId().HasValue )
            {
                _entityId = gmpGroupMember.SelectedValueAsId();
            }
            if ( ppPerson.SelectedValue.HasValue )
            {
                _entityId = ppPerson.SelectedValue;
            }

            if ( _entityTypeId.HasValue && _entityId.HasValue )
            {
                notesTimeline.Visible = true;

                using ( var rockContext = new RockContext() )
                {
                    List<NoteTypeCache> noteTypes = getNoteTypes();

                    NoteOptions noteOptions = new NoteOptions( new NoteContainer() )
                    {
                        EntityId = _entityId,
                        NoteTypes = noteTypes.ToArray(),
                        NoteLabel = GetAttributeValue( "NoteTerm" ),
                        DisplayType = GetAttributeValue( "DisplayType" ) == "Light" ? NoteDisplayType.Light : NoteDisplayType.Full,
                        ShowAlertCheckBox = GetAttributeValue( "ShowAlertCheckbox" ).AsBoolean(),
                        ShowPrivateCheckBox = GetAttributeValue( "ShowPrivateCheckbox" ).AsBoolean(),
                        ShowSecurityButton = GetAttributeValue( "ShowSecurityButton" ).AsBoolean(),
                        AddAlwaysVisible = true,
                        ShowCreateDateInput = GetAttributeValue( "AllowBackdatedNotes" ).AsBoolean(),
                        NoteViewLavaTemplate = GetAttributeValue( "NoteViewLavaTemplate" ),
                        DisplayNoteTypeHeading = GetAttributeValue( "DisplayNoteTypeHeading" ).AsBoolean(),
                        UsePersonIcon = false,
                        ExpandReplies = GetAttributeValue( "ExpandReplies" ).AsBoolean()
                    };

                    notesTimeline.NoteOptions = noteOptions;
                    notesTimeline.Title = string.Format( "Add a {0}", GetAttributeValue( "NoteTerm" ) );
                    notesTimeline.TitleIconCssClass = "fa fa-sticky-note";
                    notesTimeline.AllowAnonymousEntry = false;
                    notesTimeline.SortDirection = ListSortDirection.Descending;

                    var noteEditor = ( NoteEditor ) notesTimeline.Controls[0];
                    noteEditor.Text = GetAttributeValue( "DefaultNoteText" );
                    noteEditor.CssClass = "note-new-kfs";
                    noteEditor.SaveButtonClick += note_SaveButtonClick;

                }
            }
            else
            {
                notesTimeline.Visible = false;
            }
        }

        private List<NoteTypeCache> getNoteTypes()
        {
            var noteTypes = NoteTypeCache.GetByEntity( _entityTypeId, string.Empty, string.Empty, true );

            // If block is configured to only allow certain note types, limit notes to those types.
            var configuredPersonNoteTypes = GetAttributeValue( "PersonNoteTypes" ).SplitDelimitedValues().AsGuidList();
            var configuredGroupMemberNoteTypes = GetAttributeValue( "GroupMemberNoteTypes" ).SplitDelimitedValues().AsGuidList();
            if ( configuredPersonNoteTypes.Any() || configuredGroupMemberNoteTypes.Any() )
            {
                noteTypes = noteTypes.Where( n => configuredPersonNoteTypes.Contains( n.Guid ) || configuredGroupMemberNoteTypes.Contains( n.Guid ) ).ToList();
            }

            return noteTypes;
        }

        private void ShowPersonInfo()
        {
            var noteTypes = getNoteTypes();
            var template = GetAttributeValue( "PersonInfoLavaTemplate" );
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "CurrentPage", this.PageCache );
            if ( _entityId.HasValue )
            {
                if ( ppPerson.Visible )
                {
                    var person = new PersonService( _rockContext ).Get( _entityId.Value );
                    mergeFields.Add( "Person", person );
                }
                else
                {
                    var groupMember = new GroupMemberService( _rockContext ).Get( _entityId.Value );
                    mergeFields.Add( "GroupMember", groupMember );
                }
                var notesList = new List<Note>();
                foreach ( var noteType in noteTypes )
                {
                    notesList.AddRange( new NoteService( _rockContext ).Get( noteType.Id, _entityId.Value ) );
                }
                mergeFields.Add( "Notes", notesList.OrderByDescending( n => n.CreatedDateTime ) );
            }
            lLavaPersonInfo.Text = template.ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );
        }

        public void LaunchWorkflow()
        {
            Guid? workflowTypeGuid = GetAttributeValue( "Workflow" ).AsGuidOrNull();
            if ( workflowTypeGuid.HasValue )
            {
                var workflowType = WorkflowTypeCache.Get( workflowTypeGuid.Value );
                if ( workflowType != null && ( workflowType.IsActive ?? true ) )
                {
                    Person person = null;
                    int? groupMemberId = null;
                    GroupMember groupMember = null;
                    if ( gmpGroupMember.SelectedValueAsId().HasValue )
                    {
                        groupMemberId = gmpGroupMember.SelectedValueAsId();
                        if ( groupMemberId.HasValue )
                        {
                            groupMember = new GroupMemberService( _rockContext ).Get( groupMemberId.Value );
                            person = groupMember.Person;
                        }
                    }
                    if ( ppPerson.SelectedValue.HasValue )
                    {
                        person = new PersonService( _rockContext ).Get( ppPerson.PersonId.Value );
                    }

                    try
                    {
                        var workflowEntity = GetAttributeValue( "WorkflowEntity" );

                        if ( workflowEntity.Equals( "Note" ) && _noteId.HasValue )
                        {
                            var noteRequest = new NoteService( _rockContext ).Get( _noteId.Value );
                            if ( noteRequest != null )
                            {
                                var workflow = Workflow.Activate( workflowType, person.FullName );
                                List<string> workflowErrors;
                                new WorkflowService( _rockContext ).Process( workflow, noteRequest, out workflowErrors );
                            }
                        }
                        else if ( workflowEntity.Equals( "GroupMember" ) && groupMemberId.HasValue && groupMember != null )
                        {
                            var workflow = Workflow.Activate( workflowType, person.FullName );
                            List<string> workflowErrors;
                            new WorkflowService( _rockContext ).Process( workflow, groupMember, out workflowErrors );
                        }
                        else
                        {
                            var workflow = Workflow.Activate( workflowType, person.FullName );
                            List<string> workflowErrors;
                            new WorkflowService( _rockContext ).Process( workflow, person, out workflowErrors );
                        }
                    }
                    catch ( Exception ex )
                    {
                        ExceptionLogService.LogException( ex, this.Context );
                    }
                }
            }
        }
        #endregion
    }
}