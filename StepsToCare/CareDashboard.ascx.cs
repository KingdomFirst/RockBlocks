// <copyright>
// Copyright 2021 by Kingdom First Solutions
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
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using rocks.kfs.StepsToCare.Model;

namespace RockWeb.Plugins.rocks_kfs.StepsToCare
{
    #region Block Attributes

    [DisplayName( "Care Dashboard" )]
    [Category( "KFS > Steps To Care" )]
    [Description( "Care dashboard block for KFS Steps to Care package. " )]

    #endregion Block Attributes

    #region Block Settings

    [ContextAware( typeof( Person ) )]
    [LinkedPage(
        "Detail Page",
        Description = "Page used to modify and create care needs.",
        IsRequired = true,
        Order = 1,
        Key = AttributeKey.DetailPage )]

    [LinkedPage(
        "Configuration Page",
        Description = "Page used to configure care workers and note templates.",
        IsRequired = true,
        Order = 2,
        Key = AttributeKey.ConfigurationPage )]

    [DefinedValueField(
        "Outstanding Care Needs Statuses",
        Description = "Select the status values that count towards the 'Outstanding Care Needs' total.",
        IsRequired = true,
        Order = 3,
        Key = AttributeKey.OutstandingCareNeedsStatuses,
        AllowMultiple = true,
        DefaultValue = rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_OPEN,
        DefinedTypeGuid = rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_STATUS
        )]

    [CodeEditorField(
        "Categories Template",
        Description = "Lava Template that can be used to customize what is displayed in the last status section. Includes common merge fields plus Care Need Categories.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        DefaultValue = CategoriesTemplateDefaultValue,
        Order = 4,
        Key = AttributeKey.CategoriesTemplate )]

    [CustomDropdownListField( "Display Type",
        Description = "The format to use for displaying notes.",
        ListSource = "Full,Light",
        IsRequired = true,
        DefaultValue = "Full",
        Category = "Notes Dialog",
        Order = 5,
        Key = AttributeKey.DisplayType )]

    [BooleanField( "Use Person Icon",
        DefaultBooleanValue = false,
        Order = 6,
        Category = "Notes Dialog",
        Key = AttributeKey.UsePersonIcon )]

    [BooleanField( "Show Alert Checkbox",
        DefaultBooleanValue = true,
        Category = "Notes Dialog",
        Order = 7,
        Key = AttributeKey.ShowAlertCheckbox )]

    [BooleanField( "Show Private Checkbox",
        DefaultBooleanValue = true,
        Category = "Notes Dialog",
        Order = 8,
        Key = AttributeKey.ShowPrivateCheckbox )]

    [BooleanField( "Show Security Button",
        DefaultBooleanValue = true,
        Category = "Notes Dialog",
        Order = 9,
        Key = AttributeKey.ShowSecurityButton )]

    [BooleanField( "Allow Backdated Notes",
        DefaultBooleanValue = false,
        Category = "Notes Dialog",
        Order = 10,
        Key = AttributeKey.AllowBackdatedNotes )]

    [BooleanField( "Close Dialog on Save",
        DefaultBooleanValue = true,
        Category = "Notes Dialog",
        Order = 11,
        Key = AttributeKey.CloseDialogOnSave )]

    [CodeEditorField( "Note View Lava Template",
        Description = "The Lava Template to use when rendering the view of the notes.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 100,
        IsRequired = false,
        DefaultValue = @"{% include '~~/Assets/Lava/NoteViewList.lava' %}",
        Category = "Notes Dialog",
        Order = 12,
        Key = AttributeKey.NoteViewLavaTemplate )]


    #endregion Block Settings

    public partial class CareDashboard : Rock.Web.UI.RockBlock
    {
        #region Keys

        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string DetailPage = "DetailPage";
            public const string CategoriesTemplate = "CategoriesTemplate";
            public const string OutstandingCareNeedsStatuses = "OutstandingCareNeedsStatuses";
            public const string ConfigurationPage = "ConfigurationPage";
            public const string UsePersonIcon = "UsePersonIcon";
            public const string DisplayType = "DisplayType";
            public const string ShowAlertCheckbox = "ShowAlertCheckbox";
            public const string ShowPrivateCheckbox = "ShowPrivateCheckbox";
            public const string ShowSecurityButton = "ShowSecurityButton";
            public const string AllowBackdatedNotes = "AllowBackdatedNotes";
            public const string CloseDialogOnSave = "CloseDialogOnSave";
            public const string NoteViewLavaTemplate = "NoteViewLavaTemplate";
        }

        /// <summary>
        /// User Preference Key
        /// </summary>
        private static class UserPreferenceKey
        {
            public const string StartDate = "Start Date";
            public const string EndDate = "End Date";
            public const string FirstName = "First Name";
            public const string LastName = "Last Name";
            public const string SubmittedBy = "Submitted By";
            public const string Category = "Category";
            public const string Status = "Status";
            public const string Campus = "Campus";
            public const string AssignedToMe = "Assigned to Me";
        }

        /// <summary>
        /// View State Keys
        /// </summary>
        private static class ViewStateKey
        {
            public const string AvailableAttributes = "AvailableAttributes";
        }

        #endregion Keys

        #region Attribute Default values

        private const string CategoriesTemplateDefaultValue = @"
<div class="""">
{% for category in Categories %}
    <span class=""badge p-2 mb-2"" style=""background-color: {{ category | Attribute:'Color' }}"">{{ category.Value }}</span>
{% endfor %}
</div>";

        #endregion Attribute Default values

        #region Properties

        /// <summary>
        /// Gets the target person
        /// </summary>
        /// <value>
        /// The target person.
        /// </value>
        protected Person TargetPerson { get; private set; }

        public List<AttributeCache> AvailableAttributes { get; set; }

        #endregion Properties

        #region Private Members

        /// <summary>
        /// Holds whether or not the person can add, edit, and delete.
        /// </summary>
        private bool _canAddEditDelete = false;

        private readonly string _photoFormat = "<div class=\"photo-icon photo-round photo-round-xs pull-left margin-r-sm js-person-popover\" personid=\"{0}\" data-original=\"{1}&w=50\" style=\"background-image: url( '{2}' ); background-size: cover; background-repeat: no-repeat;\"></div>";

        private List<NoteTypeCache> _careNeedNoteTypes = new List<NoteTypeCache>();

        #endregion Private Members

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AvailableAttributes = ViewState[ViewStateKey.AvailableAttributes] as List<AttributeCache>;

            AddDynamicControls();
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState[ViewStateKey.AvailableAttributes] = AvailableAttributes;

            return base.SaveViewState();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            /// add lazyload js so that person-link-popover javascript works (see CareDashboard.ascx)
            RockPage.AddScriptLink( "~/Scripts/jquery.lazyload.min.js" );

            gList.GridRebind += gList_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlCareDashboard );
            rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;

            _canAddEditDelete = IsUserAuthorized( Authorization.EDIT );

            gList.GridRebind += gList_GridRebind;
            gList.RowDataBound += gList_RowDataBound;
            gList.DataKeyNames = new string[] { "Id" };
            gList.Actions.ShowAdd = _canAddEditDelete;
            gList.Actions.AddClick += gList_AddClick;
            gList.IsDeleteEnabled = _canAddEditDelete;

            mdMakeNote.Footer.Visible = false;

            // in case this is used as a Person Block, set the TargetPerson
            TargetPerson = ContextEntity<Person>();

            _careNeedNoteTypes = NoteTypeCache.GetByEntity( EntityTypeCache.Get( typeof( CareNeed ) ).Id, "", "", true );
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
                SetFilter();
                BindGrid();
            }
            if ( !string.IsNullOrWhiteSpace( hfCareNeedId.Value ) )
            {
                var careNeed = new CareNeedService( new RockContext() ).Get( hfCareNeedId.Value.AsInteger() );

                if ( careNeed != null )
                {
                    SetupNoteTimeline( careNeed );
                }
            }
        }

        #endregion Base Control Methods

        #region Events

        /// <summary>
        /// Binds the attributes.
        /// </summary>
        private void BindAttributes()
        {
            // Parse the attribute filters
            AvailableAttributes = new List<AttributeCache>();

            int entityTypeId = new CareNeed().TypeId;
            foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                .Where( a =>
                    a.EntityTypeId == entityTypeId &&
                    a.IsGridColumn )
                .OrderByDescending( a => a.EntityTypeQualifierColumn )
                .ThenBy( a => a.Order )
                .ThenBy( a => a.Name ) )
            {
                AvailableAttributes.Add( AttributeCache.Get( attributeModel ) );
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            SetFilter();
            BindGrid();
        }
        /// <summary>
        /// Handles the Click event of the lbCareConfigure control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCareConfigure_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.ConfigurationPage );
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFilter.SaveUserPreference( UserPreferenceKey.StartDate, "Start Date", drpDate.LowerValue.HasValue ? drpDate.LowerValue.Value.ToString( "o" ) : string.Empty );
            rFilter.SaveUserPreference( UserPreferenceKey.EndDate, "End Date", drpDate.UpperValue.HasValue ? drpDate.UpperValue.Value.ToString( "o" ) : string.Empty );
            rFilter.SaveUserPreference( UserPreferenceKey.FirstName, "First Name", tbFirstName.Text );
            rFilter.SaveUserPreference( UserPreferenceKey.LastName, "Last Name", tbLastName.Text );
            rFilter.SaveUserPreference( UserPreferenceKey.SubmittedBy, "Submitted By", ddlSubmitter.SelectedItem.Value );
            rFilter.SaveUserPreference( UserPreferenceKey.Category, "Category", dvpCategory.SelectedValues.AsDelimited( ";" ) );
            rFilter.SaveUserPreference( UserPreferenceKey.Status, "Status", dvpStatus.SelectedItem.Value );
            rFilter.SaveUserPreference( UserPreferenceKey.Campus, "Campus", cpCampus.SelectedCampusId.ToString() );
            rFilter.SaveUserPreference( UserPreferenceKey.AssignedToMe, "Assigned to Me", cbAssignedToMe.Checked.ToString() );

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                    if ( filterControl != null )
                    {
                        try
                        {
                            var values = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                            rFilter.SaveUserPreference( attribute.Key, attribute.Name, attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter ).ToJson() );
                        }
                        catch
                        {
                            // intentionally ignore
                        }
                    }
                }
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the filter display for each saved user value
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void rFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case UserPreferenceKey.StartDate:
                case UserPreferenceKey.EndDate:
                    var dateTime = e.Value.AsDateTime();
                    if ( dateTime.HasValue )
                    {
                        e.Value = dateTime.Value.ToShortDateString();
                    }
                    else
                    {
                        e.Value = null;
                    }

                    return;

                case UserPreferenceKey.FirstName:
                    return;

                case UserPreferenceKey.LastName:
                    return;

                case UserPreferenceKey.Campus:
                    {
                        int? campusId = e.Value.AsIntegerOrNull();
                        if ( campusId.HasValue )
                        {
                            e.Value = CampusCache.Get( campusId.Value ).Name;
                        }
                        return;
                    }

                case UserPreferenceKey.SubmittedBy:
                    int? personAliasId = e.Value.AsIntegerOrNull();
                    if ( personAliasId.HasValue )
                    {
                        var personAlias = new PersonAliasService( new RockContext() ).Get( personAliasId.Value );
                        if ( personAlias != null )
                        {
                            e.Value = personAlias.Person.FullName;
                        }
                    }

                    return;

                case UserPreferenceKey.Category:
                    e.Value = ResolveValues( e.Value, dvpCategory );
                    return;

                case UserPreferenceKey.Status:
                    var definedValueId = e.Value.AsIntegerOrNull();
                    if ( definedValueId.HasValue )
                    {
                        var definedValue = DefinedValueCache.Get( definedValueId.Value );
                        if ( definedValue != null )
                        {
                            e.Value = definedValue.Value;
                        }
                    }

                    return;

                case UserPreferenceKey.AssignedToMe:
                    return;

                default:
                    e.Value = string.Empty;
                    return;
            }
        }

        /// <summary>
        /// Handles the RowDataBound event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.GridViewRowEventArgs"/> instance containing the event data.</param>
        public void gList_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                CareNeed careNeed = e.Row.DataItem as CareNeed;
                if ( careNeed != null )
                {
                    careNeed.Category.LoadAttributes();
                    var categoryColor = careNeed.Category.GetAttributeValue( "Color" );
                    var categoryCell = e.Row.Cells[0];
                    if ( categoryCell != null && categoryColor.IsNotNullOrWhiteSpace() )
                    {
                        categoryCell.Style[HtmlTextWriterStyle.BackgroundColor] = categoryColor;
                    }

                    Literal lName = e.Row.FindControl( "lName" ) as Literal;
                    if ( lName != null )
                    {
                        if ( careNeed.PersonAlias != null )
                        {
                            lName.Text = string.Format( "<a href=\"{0}\">{1}</a>", ResolveUrl( string.Format( "~/Person/{0}", careNeed.PersonAlias.PersonId ) ), careNeed.PersonAlias.Person.FullName ?? string.Empty );
                        }
                    }

                    Literal lCareTouches = e.Row.FindControl( "lCareTouches" ) as Literal;
                    if ( lCareTouches != null )
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var noteType = _careNeedNoteTypes.FirstOrDefault();
                            if ( noteType != null )
                            {
                                var careNeedNotes = new NoteService( rockContext )
                                    .GetByNoteTypeId( noteType.Id )
                                    .Where( n => n.EntityId == careNeed.Id );

                                lCareTouches.Text = careNeedNotes.Count().ToString();
                            }
                            else
                            {
                                lCareTouches.Text = "0";
                            }
                        }
                    }

                    Literal lAssigned = e.Row.FindControl( "lAssigned" ) as Literal;
                    if ( lAssigned != null )
                    {
                        if ( careNeed.AssignedPersons.Any() )
                        {
                            StringBuilder sbPersonHtml = new StringBuilder();
                            foreach ( var assignedPerson in careNeed.AssignedPersons )
                            {
                                var person = assignedPerson.PersonAlias.Person;
                                sbPersonHtml.AppendFormat( _photoFormat, person.Id, person.PhotoUrl, ResolveUrl( "~/Assets/Images/person-no-photo-unknown.svg" ) );
                            }
                            lAssigned.Text = sbPersonHtml.ToString();
                        }
                    }

                    HighlightLabel hlStatus = e.Row.FindControl( "hlStatus" ) as HighlightLabel;
                    if ( hlStatus != null )
                    {
                        switch ( careNeed.Status.Value )
                        {
                            case "Open":
                                hlStatus.Text = "Open";
                                hlStatus.LabelType = LabelType.Success;
                                return;

                            case "Follow Up":
                                hlStatus.Text = "Follow Up";
                                hlStatus.LabelType = LabelType.Danger;
                                return;

                            case "Closed":
                                hlStatus.Text = "Closed";
                                hlStatus.LabelType = LabelType.Default;
                                return;

                            default:
                                hlStatus.Text = careNeed.Status.Value;
                                hlStatus.LabelType = LabelType.Info;
                                return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnNoteTemplate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void btnNoteTemplate_Click( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                LinkButtonField field = sender as LinkButtonField;
                var fieldId = field.ID.Replace( "btn_", "" );
                var noteTemplate = new NoteTemplateService( rockContext ).Get( fieldId.AsInteger() );
                var noteService = new NoteService( rockContext );
                var noteType = _careNeedNoteTypes.FirstOrDefault();

                if ( noteType != null )
                {
                    var note = new Note { Id = 0 };
                    note.IsSystem = false;
                    note.IsAlert = false;
                    note.NoteTypeId = noteType.Id;
                    note.EntityId = e.RowKeyId;
                    note.Text = noteTemplate.Note;
                    note.EditedByPersonAliasId = CurrentPersonAliasId;
                    note.EditedDateTime = RockDateTime.Now;
                    note.NoteUrl = this.RockBlock()?.CurrentPageReference?.BuildUrl();
                    note.Caption = string.Empty;

                    if ( noteType.RequiresApprovals )
                    {
                        if ( note.IsAuthorized( Authorization.APPROVE, CurrentPerson ) )
                        {
                            note.ApprovalStatus = NoteApprovalStatus.Approved;
                            note.ApprovedByPersonAliasId = CurrentPersonAliasId;
                            note.ApprovedDateTime = RockDateTime.Now;
                        }
                        else
                        {
                            note.ApprovalStatus = NoteApprovalStatus.PendingApproval;
                        }
                    }
                    else
                    {
                        note.ApprovalStatus = NoteApprovalStatus.Approved;
                    }
                    note.CopyAttributesFrom( noteTemplate );

                    if ( note.IsValid )
                    {
                        noteService.Add( note );

                        rockContext.WrapTransaction( () =>
                        {
                            rockContext.SaveChanges();
                            note.SaveAttributeValues( rockContext );
                        } );

                        mdGridWarning.Show( "Note Saved: " + noteTemplate.Note, ModalAlertType.Information );
                        BindGrid();
                    }
                    else
                    {
                        mdGridWarning.Show( "Note is invalid. <br>" + note.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" ), ModalAlertType.Alert );
                    }
                }
                else
                {
                    mdGridWarning.Show( "The Care Need Note type is missing. Please setup a note type for Care Need.", ModalAlertType.Alert );

                }
            }
        }

        /// <summary>
        /// Handles the Click event of the gMakeNote control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gMakeNote_Click( object sender, RowEventArgs e )
        {
            var careNeed = new CareNeedService( new RockContext() ).Get( e.RowKeyId );
            string messages = string.Empty;
            hfCareNeedId.Value = careNeed.Id.ToString();

            mdMakeNote.Show();

            if ( careNeed != null )
            {
                SetupNoteTimeline( careNeed );
            }

        }

        /// <summary>
        /// Handles the AddClick event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gList_AddClick( object sender, EventArgs e )
        {
            var qryParams = new Dictionary<string, string>();
            qryParams.Add( "CareNeedId", 0.ToString() );
            if ( TargetPerson != null )
            {
                qryParams.Add( "PersonId", TargetPerson.Id.ToString() );
            }

            NavigateToLinkedPage( AttributeKey.DetailPage, qryParams );
        }

        /// <summary>
        /// Handles the Edit event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gList_Edit( object sender, RowEventArgs e )
        {
            var qryParams = new Dictionary<string, string>();
            qryParams.Add( "CareNeedId", e.RowKeyId.ToString() );
            if ( TargetPerson != null )
            {
                qryParams.Add( "PersonId", TargetPerson.Id.ToString() );
            }

            NavigateToLinkedPage( "DetailPage", qryParams );
        }

        /// <summary>
        /// Handles the Delete event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gList_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            CareNeedService service = new CareNeedService( rockContext );
            CareNeed careNeed = service.Get( e.RowKeyId );
            if ( careNeed != null )
            {
                string errorMessage;
                if ( !service.CanDelete( careNeed, out errorMessage ) )
                {
                    mdGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                service.Delete( careNeed );
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gPledges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the RowCreated event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gList_RowCreated( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.Header )
            {
                var notesField = gList.GetColumnByHeaderText( "Quick Notes" );
                var notesFieldIndex = gList.GetColumnIndex( notesField );
                if ( e.Row.Cells.Count > notesFieldIndex + 3 ) // ensure there are more columns to colspan with, +3 for itself, make a note and delete columns
                {
                    e.Row.Cells[notesFieldIndex].ColumnSpan = 2;
                    //now make up for the colspan from cell2
                    e.Row.Cells.RemoveAt( notesFieldIndex + 1 );
                }
            }
        }

        /// <summary>
        /// Handles the mdMakeNote Save Click event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdMakeNote_SaveClick( object sender, EventArgs e )
        {
            var closeDialogOnSave = GetAttributeValue( AttributeKey.CloseDialogOnSave ).AsBoolean();
            hfCareNeedId.Value = "";
            if ( closeDialogOnSave )
            {
                mdMakeNote.Hide();
            }
            BindGrid();
        }

        #endregion Events

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void SetFilter()
        {
            drpDate.LowerValue = rFilter.GetUserPreference( UserPreferenceKey.StartDate ).AsDateTime();
            drpDate.UpperValue = rFilter.GetUserPreference( UserPreferenceKey.EndDate ).AsDateTime();

            cpCampus.Campuses = CampusCache.All();
            cpCampus.SelectedCampusId = rFilter.GetUserPreference( UserPreferenceKey.Campus ).AsInteger();

            // hide the First/Last name filter if this is being used as a Person block
            tbFirstName.Visible = TargetPerson == null;
            tbLastName.Visible = TargetPerson == null;

            tbFirstName.Text = rFilter.GetUserPreference( UserPreferenceKey.FirstName );
            tbLastName.Text = rFilter.GetUserPreference( UserPreferenceKey.LastName );

            var listData = new CareNeedService( new RockContext() ).Queryable( "PersonAlias,PersonAlias.Person" )
                .Where( cn => cn.SubmitterAliasId != null )
                .Select( cn => cn.SubmitterPersonAlias.Person )
                .Distinct()
                .ToList();
            ddlSubmitter.DataSource = listData;
            ddlSubmitter.DataTextField = "FullName";
            ddlSubmitter.DataValueField = "PrimaryAliasId";
            ddlSubmitter.DataBind();
            ddlSubmitter.Items.Insert( 0, new ListItem() );
            ddlSubmitter.SetValue( rFilter.GetUserPreference( UserPreferenceKey.SubmittedBy ) );

            var categoryDefinedType = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_CATEGORY ) );
            dvpCategory.DefinedTypeId = categoryDefinedType.Id;
            string categoryValue = rFilter.GetUserPreference( UserPreferenceKey.Category );
            if ( !string.IsNullOrWhiteSpace( categoryValue ) )
            {
                dvpCategory.SetValues( categoryValue.Split( ';' ).ToList() );
            }

            var statusDefinedType = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_STATUS ) );
            dvpStatus.DefinedTypeId = statusDefinedType.Id;
            dvpStatus.SetValue( rFilter.GetUserPreference( UserPreferenceKey.Status ) );

            cbAssignedToMe.Checked = rFilter.GetUserPreference( UserPreferenceKey.AssignedToMe ).AsBoolean();

            var template = GetAttributeValue( AttributeKey.CategoriesTemplate );

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson, new Rock.Lava.CommonMergeFieldsOptions { GetLegacyGlobalMergeFields = false } );
            mergeFields.Add( "Categories", categoryDefinedType.DefinedValues );
            mergeFields.Add( "Statuses", statusDefinedType.DefinedValues );
            lCategories.Text = template.ResolveMergeFields( mergeFields );

            // set attribute filters
            BindAttributes();
            AddDynamicControls();
        }

        /// <summary>
        /// Adds dynamic columns and filter controls for note templates and attributes.
        /// </summary>
        private void AddDynamicControls()
        {
            using ( var rockContext = new RockContext() )
            {
                var noteTemplates = new NoteTemplateService( rockContext ).Queryable().AsNoTracking().Where( nt => nt.IsActive ).OrderBy( nt => nt.Order );
                var firstCol = true;
                foreach ( var template in noteTemplates )
                {
                    var btnId = string.Format( "btn_{0}", template.Id );
                    bool btnColumnExists = gList.Columns.OfType<LinkButtonField>().FirstOrDefault( b => b.ID == btnId ) != null;
                    if ( !btnColumnExists )
                    {
                        var btnNoteTemplate = new LinkButtonField();
                        btnNoteTemplate.ID = btnId;
                        btnNoteTemplate.Text = string.Format( "<i class='{0}'></i>", template.Icon );
                        btnNoteTemplate.ToolTip = template.Note;
                        btnNoteTemplate.Click += btnNoteTemplate_Click;
                        if ( firstCol )
                        {
                            btnNoteTemplate.HeaderText = "Quick Notes";
                            firstCol = false;
                        }
                        //btnNoteTemplate.CommandName = "QuickNote";
                        //btnNoteTemplate.CommandArgument = template.Id.ToString() + "^" + careNeed.Id.ToString();
                        btnNoteTemplate.CssClass = "btn btn-info btn-sm";
                        gList.Columns.Add( btnNoteTemplate );
                    }
                }
            }

            var makeNoteField = new LinkButtonField();
            makeNoteField.HeaderText = "Make Note";
            makeNoteField.CssClass = "btn btn-primary btn-make-note btn-sm w-auto";
            makeNoteField.Text = "Make Note";
            makeNoteField.Click += gMakeNote_Click;
            makeNoteField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
            makeNoteField.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
            gList.Columns.Add( makeNoteField );

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                    if ( control != null )
                    {
                        if ( control is IRockControl )
                        {
                            var rockControl = ( IRockControl ) control;
                            rockControl.Label = attribute.Name;
                            rockControl.Help = attribute.Description;
                            phAttributeFilters.Controls.Add( control );
                        }
                        else
                        {
                            var wrapper = new RockControlWrapper();
                            wrapper.ID = control.ID + "_wrapper";
                            wrapper.Label = attribute.Name;
                            wrapper.Controls.Add( control );
                            phAttributeFilters.Controls.Add( wrapper );
                        }

                        string savedValue = rFilter.GetUserPreference( attribute.Key );
                        if ( !string.IsNullOrWhiteSpace( savedValue ) )
                        {
                            try
                            {
                                var values = JsonConvert.DeserializeObject<List<string>>( savedValue );
                                attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, values );
                            }
                            catch
                            {
                                // intentionally ignore
                            }
                        }
                    }

                    bool columnExists = gList.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id ) != null;
                    if ( !columnExists )
                    {
                        AttributeField boundField = new AttributeField();
                        boundField.DataField = attribute.Key;
                        boundField.AttributeId = attribute.Id;
                        boundField.HeaderText = attribute.Name;

                        var attributeCache = Rock.Web.Cache.AttributeCache.Get( attribute.Id );
                        if ( attributeCache != null )
                        {
                            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                        }

                        gList.Columns.Add( boundField );
                    }
                }
            }

            // Add delete column
            var deleteField = new DeleteField();
            gList.Columns.Add( deleteField );
            deleteField.Click += gList_Delete;
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            rFilter.Visible = true;
            gList.Visible = true;
            RockContext rockContext = new RockContext();
            CareNeedService careNeedService = new CareNeedService( rockContext );
            NoteService noteService = new NoteService( rockContext );
            var qry = careNeedService.Queryable( "PersonAlias,PersonAlias.Person,SubmitterPersonAlias,SubmitterPersonAlias.Person" ).AsNoTracking();
            var noteQry = noteService.GetByNoteTypeId( _careNeedNoteTypes.Any() ? _careNeedNoteTypes.FirstOrDefault().Id : 0 ).AsNoTracking();

            var outstandingCareStatuses = GetAttributeValues( AttributeKey.OutstandingCareNeedsStatuses );
            var totalCareNeeds = qry.Count();
            var outstandingCareNeeds = qry.Count( cn => outstandingCareStatuses.Contains( cn.Status.Guid.ToString() ) );

            var currentDateTime = RockDateTime.Now;
            var last7Days = currentDateTime.AddDays( -7 );
            var careTouches = noteQry.Count( n => n.CreatedDateTime >= last7Days && n.CreatedDateTime <= currentDateTime );

            // Filter by Start Date
            DateTime? startDate = drpDate.LowerValue;
            if ( startDate != null )
            {
                qry = qry.Where( b => b.DateEntered >= startDate );
            }

            // Filter by End Date
            DateTime? endDate = drpDate.UpperValue;
            if ( endDate != null )
            {
                qry = qry.Where( b => b.DateEntered <= endDate );
            }

            // Filter by Campus
            if ( cpCampus.SelectedCampusId.HasValue )
            {
                qry = qry.Where( b => b.CampusId == cpCampus.SelectedCampusId );
            }

            if ( TargetPerson != null )
            {
                // show benevolence request for the target person and also for their family members
                var qryFamilyMembers = TargetPerson.GetFamilyMembers( true, rockContext );
                qry = qry.Where( a => a.PersonAliasId.HasValue && qryFamilyMembers.Any( b => b.PersonId == a.PersonAlias.PersonId ) );
            }
            else
            {
                // Filter by First Name
                string firstName = tbFirstName.Text;
                if ( !string.IsNullOrWhiteSpace( firstName ) )
                {
                    qry = qry.Where( b => b.PersonAlias.Person.FirstName.StartsWith( firstName ) );
                }

                // Filter by Last Name
                string lastName = tbLastName.Text;
                if ( !string.IsNullOrWhiteSpace( lastName ) )
                {
                    qry = qry.Where( b => b.PersonAlias.Person.LastName.StartsWith( lastName ) );
                }
            }

            // Filter by Submitter
            int? submitterPersonAliasId = ddlSubmitter.SelectedItem.Value.AsIntegerOrNull();
            if ( submitterPersonAliasId != null )
            {
                qry = qry.Where( b => b.SubmitterAliasId == submitterPersonAliasId );
            }

            // Filter by Status
            int? requestStatusValueId = dvpStatus.SelectedItem.Value.AsIntegerOrNull();
            if ( requestStatusValueId != null )
            {
                qry = qry.Where( b => b.StatusValueId == requestStatusValueId );
            }

            // Filter by Category
            List<int> categories = dvpCategory.SelectedValuesAsInt;
            if ( categories.Any() )
            {
                qry = qry.Where( cn => cn.CategoryValueId != null && categories.Contains( cn.CategoryValueId.Value ) );
            }

            // Filter by Assigned to Me
            if ( cbAssignedToMe.Checked )
            {
                qry = qry.Where( cn => cn.AssignedPersons.Count( ap => ap.PersonAliasId == CurrentPersonAliasId ) > 0 );
            }

            SortProperty sortProperty = gList.SortProperty;
            if ( sortProperty != null )
            {
                qry = qry.Sort( sortProperty );
            }
            else
            {
                qry = qry.OrderByDescending( a => a.DateEntered ).ThenByDescending( a => a.Id );
            }

            // Filter query by any configured attribute filters
            if ( AvailableAttributes != null && AvailableAttributes.Any() )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                    qry = attribute.FieldType.Field.ApplyAttributeQueryFilter( qry, filterControl, attribute, careNeedService, Rock.Reporting.FilterMode.SimpleFilter );
                }
            }

            var list = qry.ToList();

            gList.DataSource = list;
            gList.DataBind();

            // Hide the campus column if the campus filter is not visible.
            gList.ColumnsOfType<RockBoundField>().First( c => c.DataField == "Campus.Name" ).Visible = cpCampus.Visible;

            lTouchesCount.Text = careTouches.ToString();
            lCareNeedsCount.Text = outstandingCareNeeds.ToString();
            lTotalNeedsCount.Text = totalCareNeeds.ToString();
        }

        /// <summary>
        /// Setup NoteTimeline/Container for use in Make Note dialog.
        /// </summary>
        private void SetupNoteTimeline( CareNeed careNeed )
        {
            var noteTypes = _careNeedNoteTypes;

            NoteOptions noteOptions = new NoteOptions( notesTimeline )
            {
                EntityId = careNeed.Id,
                NoteTypes = noteTypes.ToArray(),
                DisplayType = GetAttributeValue( AttributeKey.DisplayType ) == "Light" ? NoteDisplayType.Light : NoteDisplayType.Full,
                ShowAlertCheckBox = GetAttributeValue( AttributeKey.ShowAlertCheckbox ).AsBoolean(),
                ShowPrivateCheckBox = GetAttributeValue( AttributeKey.ShowPrivateCheckbox ).AsBoolean(),
                ShowSecurityButton = GetAttributeValue( AttributeKey.ShowSecurityButton ).AsBoolean(),
                AddAlwaysVisible = true,
                ShowCreateDateInput = GetAttributeValue( AttributeKey.AllowBackdatedNotes ).AsBoolean(),
                NoteViewLavaTemplate = GetAttributeValue( AttributeKey.NoteViewLavaTemplate ),
                DisplayNoteTypeHeading = false,
                UsePersonIcon = GetAttributeValue( AttributeKey.UsePersonIcon ).AsBoolean(),
                ExpandReplies = false
            };

            notesTimeline.NoteOptions = noteOptions;
            notesTimeline.AllowAnonymousEntry = false;
            notesTimeline.SortDirection = ListSortDirection.Descending;

            var noteEditor = ( NoteEditor ) notesTimeline.Controls[0];
            noteEditor.CssClass = "note-new-kfs";
            noteEditor.SaveButtonClick += mdMakeNote_SaveClick;
        }

        /// <summary>
        /// Resolves the values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="listControl">The list control.</param>
        /// <returns></returns>
        private string ResolveValues( string values, CheckBoxList checkBoxList )
        {
            var resolvedValues = new List<string>();

            foreach ( string value in values.Split( ';' ) )
            {
                var item = checkBoxList.Items.FindByValue( value );
                if ( item != null )
                {
                    resolvedValues.Add( item.Text );
                }
            }

            return resolvedValues.AsDelimited( ", " );
        }

        #endregion Methods
    }
}