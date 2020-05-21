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
using System.ComponentModel;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

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
    [CustomDropdownListField( "Display Type", "The format to use for displaying notes.", "Full,Light", true, "Full", "", 2 )]
    [BooleanField( "Use Person Icon", "", false, "", 3 )]
    [BooleanField( "Show Alert Checkbox", "", true, "", 4 )]
    [BooleanField( "Show Private Checkbox", "", true, "", 5 )]
    [BooleanField( "Show Security Button", "", true, "", 6 )]
    [BooleanField( "Allow Backdated Notes", "", false, "", 7 )]
    [NoteTypeField( "Note Types", "Optional list of note types to limit display to", true, "", "", "", false, "", "", 8 )]
    [BooleanField( "Display Note Type Heading", "Should each note's Note Type be displayed as a heading above each note?", false, "", 9 )]
    [BooleanField( "Expand Replies", "Should replies to automatically expanded?", false, "", 10 )]
    [CodeEditorField( "Note View Lava Template", "The Lava Template to use when rendering the view of the notes.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 100, false, @"{% include '~~/Assets/Lava/NoteViewList.lava' %}", order: 11 )]
    [MemoField( "Default Note Text", "Enter default note text such as pre-filled questions here.", false, "", "", 12 )]
    #endregion
    public partial class Notes : RockBlock
    {
        #region Private Variables

        private int? _entityTypeId = null;
        private int? _entityId = null;

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
                if ( groupMemberId.HasValue )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var groupMember = new GroupMemberService( rockContext ).Get( groupMemberId.Value );
                        gmpGroupMember.GroupId = groupMember.GroupId;
                        gmpGroupMember.SetValue( groupMemberId );
                        _entityId = groupMemberId;
                    }
                }
            }
            else
            {
                _entityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.PERSON ).Id;
                ppPerson.Visible = true;
                gmpGroupMember.Visible = false;
                if ( personId.HasValue )
                {
                    ppPerson.PersonId = personId;
                    ppPerson.SelectedValue = personId;
                    _entityId = personId;
                }
            }

            ShowNotes();
        }

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
                pnlNoteEntry.Visible = true;

                using ( var rockContext = new RockContext() )
                {
                    var noteTypes = NoteTypeCache.GetByEntity( _entityTypeId, string.Empty, string.Empty, true );

                    // If block is configured to only allow certain note types, limit notes to those types.
                    var configuredNoteTypes = GetAttributeValue( "NoteTypes" ).SplitDelimitedValues().AsGuidList();
                    if ( configuredNoteTypes.Any() )
                    {
                        noteTypes = noteTypes.Where( n => configuredNoteTypes.Contains( n.Guid ) ).ToList();
                    }

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
                        UsePersonIcon = GetAttributeValue( "UsePersonIcon" ).AsBoolean(),
                        ExpandReplies = GetAttributeValue( "ExpandReplies" ).AsBoolean()
                    };

                    noteEditor.SetNoteOptions( noteOptions );
                    noteEditor.Text = GetAttributeValue( "DefaultNoteText" );
                    noteEditor.ShowEditMode = true;
                    noteEditor.EntityId = _entityId;
                    noteEditor.CssClass = "note-new";

                    noteEditor.CreatedByPersonAlias = RockPage.CurrentPersonAlias;
                    noteEditor.SaveButtonClick += note_SaveButtonClick;
                }
            }
            else
            {
                pnlNoteEntry.Visible = false;
            }
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
            noteEditor.Text = string.Empty;
            noteEditor.IsAlert = false;
            noteEditor.IsPrivate = false;
            noteEditor.NoteId = null;

            ShowNotes();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the SelectPerson event of the Notes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void SelectPerson( object sender, EventArgs e )
        {
            ShowNotes();
        }
        #endregion
    }
}