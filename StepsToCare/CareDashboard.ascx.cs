// <copyright>
// Copyright 2024 by Kingdom First Solutions
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
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Drawing.Avatar;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using rocks.kfs.StepsToCare.Model;
using ConnectionType = Rock.Model.ConnectionType;
using TableCell = System.Web.UI.WebControls.TableCell;

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

    [IntegerField(
        "Minimum Care Touches",
        Description = "Minimum care touches in 'Minimum Care Touch Hours' before the need gets 'flagged'.",
        DefaultIntegerValue = 2,
        IsRequired = true,
        Order = 3,
        Key = AttributeKey.MinimumCareTouches )]

    [IntegerField(
        "Minimum Care Touch Hours",
        Description = "Minimum care touches in this time period before the need gets 'flagged'.",
        DefaultIntegerValue = 24,
        IsRequired = true,
        Order = 4,
        Key = AttributeKey.MinimumCareTouchHours )]

    [IntegerField(
        "Minimum Follow Up Care Touch Hours",
        Description = "Minimum hours for the follow up worker to add a care touch before the need gets 'flagged'.",
        DefaultIntegerValue = 24,
        IsRequired = true,
        Order = 4,
        Key = AttributeKey.MinimumFollowUpTouchHours )]

    [BooleanField( "New Care Need Uses Edit Permission",
        Description = "Should the Add Care Need buttons be protected by the edit permission?",
        DefaultBooleanValue = false,
        Order = 5,
        Key = AttributeKey.EnterCareNeed )]

    [DefinedValueField(
        "Outstanding Care Needs Statuses",
        Description = "Select the status values that count towards the 'Outstanding Care Needs' total.",
        IsRequired = true,
        Order = 5,
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
        Order = 6,
        Key = AttributeKey.CategoriesTemplate )]

    [BooleanField(
        "Target Person Mode Include Family Members",
        Description = "When running in Target Person mode (Page has Person specific content), should we include family member needs in the list? Default: false.",
        IsRequired = false,
        DefaultBooleanValue = false,
        Order = 7,
        Key = AttributeKey.TargetModeIncludeFamilyMembers )]

    [IntegerField( "Threshold of Days for Scheduled Needs",
        Description = "The number of days from today to display scheduled needs on the dashboard.",
        IsRequired = true,
        DefaultIntegerValue = 3,
        Order = 8,
        Key = AttributeKey.FutureThresholdDays )]

    [TextField( "Touch Needed Tooltip Template",
        Description = "Basic merge template for touch needed tooltip notes. <span class='tip tip-lava'></span>",
        DefaultValue = "{% if TouchTemplate == 'Follow Up Worker' %}Touch Needed{% else %}{{TouchCount}} out of {{MinimumCareTouches}} in {{MinimumCareTouchHours}} hours.{% endif %}",
        Order = 9,
        Key = AttributeKey.TouchNeededTooltipTemplate )]

    [CodeEditorField(
        "Quick Note Status Template",
        Description = "Lava Template that can be used to customize what is displayed in the make note dialog. Includes common merge fields, Care Need and Touch Templates loaded from Categories.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        DefaultValue = QuickNoteStatusTemplateDefaultValue,
        Order = 10,
        Key = AttributeKey.QuickNoteStatusTemplate )]

    [BooleanField(
        "Quick Note Auto Save",
        Description = "Automatically save Quick Notes when clicked from the Care Dashboard. If 'no' the 'Make Note' dialog will popup to allow you to add an additional note before saving.",
        IsRequired = false,
        DefaultBooleanValue = true,
        Order = 11,
        Key = AttributeKey.QuickNoteAutoSave )]

    [BooleanField(
        "Enable Launch Workflow",
        Description = "Enable Launch Workflow Action",
        IsRequired = false,
        DefaultBooleanValue = true,
        Order = 9,
        Category = "Actions",
        Key = AttributeKey.WorkflowEnable )]

    [LinkedPage(
        "Prayer Detail Page",
        Description = "Page used to convert needs to prayer requests. (if not set the action will not show)",
        IsRequired = false,
        Order = 10,
        Category = "Actions",
        Key = AttributeKey.PrayerDetailPage )]

    [LinkedPage(
        "Benevolence Detail Page",
        Description = "Page used to convert needs to benevolence requests. (if not set the action will not show)",
        IsRequired = false,
        Order = 11,
        Category = "Actions",
        Key = AttributeKey.BenevolenceDetailPage )]

    [LinkedPage(
        "Care Need History Page",
        Description = "Page used to view history of Care Needs (if not set the action will not show)",
        IsRequired = false,
        Order = 11,
        Category = "Actions",
        Key = AttributeKey.CareNeedHistoryPage )]

    [BenevolenceTypeField( "Benevolence Type",
        Description = "The Benevolence type used when creating benevolence requests from Steps to Care 'Actions'",
        IsRequired = true,
        DefaultValue = Rock.SystemGuid.BenevolenceType.BENEVOLENCE,
        Key = AttributeKey.BenevolenceType,
        Category = "Actions",
        Order = 12 )]

    [BooleanField(
        "Enable Add Connection Request",
        Description = "Enable Add Connection Request Action",
        IsRequired = false,
        DefaultBooleanValue = false,
        Order = 13,
        Category = "Actions",
        Key = AttributeKey.ConnectionRequestEnable )]

    [ConnectionTypesField( "Filter Connection Types",
        Description = "Filter down the connection types to include only these selected types.",
        Category = "Actions",
        IsRequired = false,
        Order = 14,
        Key = AttributeKey.IncludeConnectionTypes )]

    [BooleanField( "Complete Child Needs on Parent Completion",
        DefaultBooleanValue = true,
        Category = "Actions",
        Order = 15,
        Key = AttributeKey.CompleteChildNeeds )]

    [TextField( "Complete Action Text",
        DefaultValue = "Complete Need",
        Category = "Actions",
        Order = 16,
        Key = AttributeKey.CompleteActionText )]

    [TextField( "Snooze Action Text",
        Description = "This action will only display when 'Custom Follow Up' is enabled on a need.",
        DefaultValue = "Snooze",
        Category = "Actions",
        Order = 17,
        Key = AttributeKey.SnoozeActionText )]

    [BooleanField( "Snooze Child Needs on Parent Follow Up",
        Description = "Should Child Needs also be moved to \"Snoozed\" status when a parent need is moved to it?",
        DefaultBooleanValue = true,
        Category = "Actions",
        Order = 18,
        Key = AttributeKey.SnoozeChildNeeds )]

    [BooleanField( "Automatic Snooze Enabled",
        Description = "Should a Care Need be moved to \"Snoozed\" status when any quick note/care touch is performed on a Care Need with Custom Follow Up enabled?",
        DefaultBooleanValue = false,
        Category = "Actions",
        Order = 19,
        Key = AttributeKey.MoveNeedToSnoozed )]

    [CustomDropdownListField( "Display Type",
        Description = "The format to use for displaying notes.",
        ListSource = "Full,Light",
        IsRequired = true,
        DefaultValue = "Full",
        Category = "Notes Dialog",
        Order = 20,
        Key = AttributeKey.DisplayType )]

    [BooleanField( "Use Person Icon",
        DefaultBooleanValue = false,
        Order = 21,
        Category = "Notes Dialog",
        Key = AttributeKey.UsePersonIcon )]

    [BooleanField( "Show Alert Checkbox",
        DefaultBooleanValue = true,
        Category = "Notes Dialog",
        Order = 22,
        Key = AttributeKey.ShowAlertCheckbox )]

    [BooleanField( "Show Private Checkbox",
        DefaultBooleanValue = true,
        Category = "Notes Dialog",
        Order = 23,
        Key = AttributeKey.ShowPrivateCheckbox )]

    [BooleanField( "Show Security Button",
        DefaultBooleanValue = true,
        Category = "Notes Dialog",
        Order = 24,
        Key = AttributeKey.ShowSecurityButton )]

    [BooleanField( "Allow Backdated Notes",
        DefaultBooleanValue = false,
        Category = "Notes Dialog",
        Order = 25,
        Key = AttributeKey.AllowBackdatedNotes )]

    [BooleanField( "Close Dialog on Save",
        DefaultBooleanValue = true,
        Category = "Notes Dialog",
        Order = 26,
        Key = AttributeKey.CloseDialogOnSave )]

    [CodeEditorField( "Note View Lava Template",
        Description = "The Lava Template to use when rendering the view of the notes.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 100,
        IsRequired = false,
        DefaultValue = @"{% include '~~/Assets/Lava/NoteViewList.lava' %}",
        Category = "Notes Dialog",
        Order = 27,
        Key = AttributeKey.NoteViewLavaTemplate )]

    [SecurityAction(
        SecurityActionKey.CareWorkers,
        "The roles and/or users that have access to view 'Care Worker Only' needs." )]

    [SecurityAction(
        SecurityActionKey.ViewAll,
        "The roles and/or users that have access to view all care needs minus 'Care Worker Only' those are protected by 'Care Workers'." )]

    [SecurityAction(
        SecurityActionKey.CompleteNeeds,
        "The roles and/or users that have access to Complete Needs." )]

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
            public const string MinimumCareTouches = "MinimumCareTouches";
            public const string MinimumCareTouchHours = "MinimumCareTouchHours";
            public const string MinimumFollowUpTouchHours = "MinimumFollowUpTouchHours";
            public const string PrayerDetailPage = "PrayerDetailPage";
            public const string BenevolenceDetailPage = "BenevolenceDetailPage";
            public const string ConnectionRequestEnable = "ConnectionRequestEnable";
            public const string IncludeConnectionTypes = "IncludeConnectionTypes";
            public const string WorkflowEnable = "WorkflowEnable";
            public const string CompleteChildNeeds = "CompleteChildNeeds";
            public const string TargetModeIncludeFamilyMembers = "TargetModeIncludeFamilyMembers";
            public const string FutureThresholdDays = "FutureThresholdDays";
            public const string BenevolenceType = "BenevolenceType";
            public const string SnoozeActionText = "SnoozeActionText";
            public const string SnoozeChildNeeds = "SnoozeChildNeeds";
            public const string CompleteActionText = "CompleteActionText";
            public const string MoveNeedToSnoozed = "MoveNeedToSnoozed";
            public const string TouchNeededTooltipTemplate = "TouchNeededTooltipTemplate";
            public const string QuickNoteStatusTemplate = "QuickNoteStatusTemplate";
            public const string QuickNoteAutoSave = "QuickNoteAutoSave";
            public const string EnterCareNeed = "EnterCareNeed";
            public const string CareNeedHistoryPage = "CareNeedHistoryPage";
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
            public const string IncludeScheduledNeeds = "Include Scheduled Needs";
            public const string StartDateFollowUp = "FollowUp Start Date";
            public const string EndDateFollowUp = "FollowUp End Date";
            public const string FirstNameFollowUp = "FollowUp First Name";
            public const string LastNameFollowUp = "FollowUp Last Name";
            public const string SubmittedByFollowUp = "FollowUp Submitted By";
            public const string CategoryFollowUp = "FollowUp Category";
            public const string StatusFollowUp = "FollowUp Status";
            public const string CampusFollowUp = "FollowUp Campus";
            public const string AssignedToMeFollowUp = "FollowUp Assigned to Me";
        }

        /// <summary>
        /// Security Action Keys
        /// </summary>
        private static class SecurityActionKey
        {
            public const string ViewAll = "ViewAll";
            public const string CareWorkers = "CareWorkers";
            public const string CompleteNeeds = "CompleteNeeds";
        }

        /// <summary>
        /// View State Keys
        /// </summary>
        private static class ViewStateKey
        {
            public const string AvailableAttributes = "AvailableAttributes";
        }

        /// <summary>
        /// Page Parameter Keys
        /// </summary>
        private static class PageParameterKey
        {
            public const string QuickNote = "QuickNote";
            public const string CareNeed = "CareNeed";
            public const string NoConfirm = "NoConfirm";
        }

        #endregion Keys

        #region Attribute Default values

        private const string CategoriesTemplateDefaultValue = @"
<div class="""">
{% for category in Categories %}
    <span class=""badge rounded-0 p-2 mb-2"" style=""background-color: {{ category | Attribute:'Color' }}"">{{ category.Value }}</span>
{% endfor %}
<br><span class=""badge rounded-0 p-2 mb-2 text-color"" style=""background-color: oldlace"">Assigned to You</span>
</div>";

        private const string QuickNoteStatusTemplateDefaultValue = @"<div class=""pull-right""><span class=""label mr-2"" style=""background-color: {{ CareNeed.Category | Attribute:'Color' }}"">{{ CareNeed.Category.Value }}</span><span class=""{{ CareNeed.Status | Attribute:'CssClass' }}"">{{ CareNeed.Status.Value }}</span></div>
<h4 class=""mt-0"">{{ CareNeed.DateEntered }} - {{ CareNeed.PersonAlias.Person.FullName }}</h4>
<p>{{ CareNeed.Details }}</p>

{% assign currentNow = 'Now' | Date %}
{% assign hourDifference = CareNeed.CreatedDateTime | DateDiff:currentNow,'h' %}
{% if CareNeed.TouchCount < MinimumCareTouches and hourDifference >= MinimumCareTouchHours %}<span class=""label label-danger"">Not enough Care Touches ({{ MinimumCareTouches | Minus:CareNeed.TouchCount }} of {{ MinimumCareTouches }} in {{ MinimumCareTouchHours }} hours)</span>{% elseif CareNeed.TouchCount < MinimumCareTouches %}<span class=""label label-warning"">Minimum Care Touches ({{ MinimumCareTouches | Minus:CareNeed.TouchCount }} of {{ MinimumCareTouches }}) Needed</span>{% else %}<span class=""label label-success"">Minimum Care Touches ({{ CareNeed.TouchCount }} of {{ MinimumCareTouches }}) Met</span>{% endif %}
{% assign followUpWorkers = CareNeed.AssignedPersons | Where:'FollowUpWorker',True %}
{% assign followUpWorkerNotes = 0 %}
{% for worker in followUpWorkers %}
    {% assign followUpWorkerNotes = CareNeedNotes | Where:'CreatedByPersonAliasId',worker.PersonAliasId | Size %}
    {% if followUpWorkerNotes > 0 %}{% break %}{% endif %}
{% endfor %}
{% if followUpWorkers != empty and MinimumFollowUpCareTouchHours != 0 and hourDifference >= MinimumFollowUpCareTouchHours and followUpWorkerNotes < 1 %}<span class=""label label-danger"">Follow Up Worker Touch Needed!</span>{% elseif MinimumFollowUpCareTouchHours != 0 and hourDifference <= MinimumFollowUpCareTouchHours and followUpWorkerNotes < 1 %}<span class=""label label-warning"">Follow Up Worker Touch Needed!</span>{% else %}<span class=""label label-success"">Follow Up Worker</span>{% endif %}
{% for template in TouchTemplates %}{% assign templateNotes = '' %}
    {% assign notesByText = CareNeedNotes | Where:'Text',template.NoteTemplate.Note %}
    {% assign notesbyGuid = CareNeedNotes | Where:'ForeignGuid',template.NoteTemplate.Guid %}
    {% for note in notesByText %}{% assign templateNotes = templateNotes | AddToArray:note %}{% endfor %}
    {% for note in notesbyGuid %}{% assign templateNotes = templateNotes | AddToArray:note %}{% endfor %}
    {% if templateNotes and templateNotes != empty %}{% assign templateNotes = templateNotes | Distinct:'Id' %}{% endif %}
    {% assign labelStatus = ""label label-warning"" %}{% assign labelText = """" %}{% assign recurringTemplateNotes = '' %}
    {% assign templateNotesSize = templateNotes | Size %}
    {% if template.Recurring %}{% assign templateNotes = templateNotes | OrderBy:'CreatedDateTime desc' %}
            {% for templateNote in templateNotes %}
                {% assign noteHours = templateNote.CreatedDateTime | DateDiff:currentNow,'h' %}
                {% if noteHours <= template.MinimumCareTouchHours %}
                    {% assign recurringTemplateNotes = recurringTemplateNotes | AddToArray:templateNote %}
                {% endif %}
            {% endfor %}
            {% assign recurringTemplateNotesSize = recurringTemplateNotes | Size %}{% assign defaultCreatedDate = CareNeed.CreatedDateTime | AsString %}
            {% assign noteHoursCompare = templateNotes[0].CreatedDateTime | Default:defaultCreatedDate | DateDiff:currentNow,'h' %}
            {% capture labelText %}{{ template.NoteTemplate.Note }}: {{ template.MinimumCareTouches | Minus:recurringTemplateNotesSize }} of {{ template.MinimumCareTouches }} due by {{ templateNotes[0].CreatedDateTime | Default:defaultCreatedDate | DateAdd:template.MinimumCareTouchHours,'h' | Date:'sd'  }}{% endcapture %}
            {% if recurringTemplateNotesSize < template.MinimumCareTouches and noteHoursCompare >= template.MinimumCareTouchHours %}
                {% capture labelText %}{{ template.NoteTemplate.Note }}: {{ template.MinimumCareTouches | Minus:recurringTemplateNotesSize }} of {{ template.MinimumCareTouches }} overdue since {{ templateNotes[0].CreatedDateTime | Default:defaultCreatedDate | DateAdd:template.MinimumCareTouchHours,'h' | Date:'sd'  }}{% endcapture %}
                {% assign labelStatus = ""label label-danger label-recurring"" %}
            {% elseif recurringTemplateNotesSize >= template.MinimumCareTouches %}
                {% capture labelText %}{{ template.NoteTemplate.Note }}: {{ recurringTemplateNotesSize }} of {{ template.MinimumCareTouches }}{% endcapture %}
                 {% assign labelStatus = ""label label-success label-recurring"" %}
            {% endif %}
        {% else %}
            {% capture labelText %}{{ template.NoteTemplate.Note }}: {{ template.MinimumCareTouches | Minus:templateNotesSize }} of {{ template.MinimumCareTouches }} due by {{ CareNeed.CreatedDateTime | DateAdd:template.MinimumCareTouchHours,'h' | Date:'sd' }}{% endcapture %}
            {% if templateNotesSize < template.MinimumCareTouches and hourDifference >= template.MinimumCareTouchHours %}
                {% capture labelText %}{{ template.NoteTemplate.Note }}: {{ template.MinimumCareTouches | Minus:templateNotesSize }} of {{ template.MinimumCareTouches }} overdue since {{ CareNeed.CreatedDateTime | DateAdd:template.MinimumCareTouchHours,'h' | Date:'sd' }}{% endcapture %}
                {% assign labelStatus = ""label label-danger"" %}
            {% elseif templateNotesSize >= template.MinimumCareTouches %}
                {% capture labelText %}{{ template.NoteTemplate.Note }}: {{ templateNotesSize }} of {{ template.MinimumCareTouches }}{% endcapture %}
                {% assign labelStatus = ""label label-success"" %}
            {% endif %}
        {% endif %}
        <span class=""{{ labelStatus }}"">{{ labelText }}</span>  
{% endfor %}
<p></p>";

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
        /// Holds whether or not the person can edit requests.
        /// </summary>
        private bool _canEdit = false;

        /// <summary>
        /// Holds whether or not the person can Administrate and delete.
        /// </summary>
        private bool _canAdministrate = false;

        /// <summary>
        /// Holds whether or not the person can view needs, by default View will only give permission to view needs assigned to the user.
        /// </summary>
        private bool _canView = false;

        /// <summary>
        /// Holds whether or not the person can view all needs.
        /// </summary>
        private bool _canViewAll = false;

        /// <summary>
        /// Holds whether or not the person can view Care Worker Only needs.
        /// </summary>
        private bool _canViewCareWorker = false;

        private readonly string _photoFormat = "<div class=\"photo-icon photo-round photo-round-xs pull-left margin-r-sm js-person-popover-simple\" personid=\"{0}\" data-original=\"{1}&w=50\" style=\"background-image: url( '{2}' ); background-size: cover; background-repeat: no-repeat;\"></div>";

        private List<NoteTypeCache> _careNeedNoteTypes = new List<NoteTypeCache>();

        public Dictionary<int, List<TouchTemplate>> _touchTemplates = new Dictionary<int, List<TouchTemplate>>();

        private Dictionary<int, AvatarColors> _noteTemplateColors = new Dictionary<int, AvatarColors>();

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
        /// Gets or sets the launch workflow page route.
        /// Example: "~/LaunchWorkflows/{0}" where {0} will be formatted with the EntitySetId
        /// </summary>
        /// <value>
        /// The launch workflow page route.
        /// </value>
        public virtual string DefaultLaunchWorkflowPageRoute
        {
            get
            {
                return ViewState["DefaultLaunchWorkflowPageRoute"] as string ?? "~/LaunchWorkflows/{0}";
            }
            set
            {
                ViewState["DefaultLaunchWorkflowPageRoute"] = value;
            }
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
            rFollowUpFilter.ApplyFilterClick += rFollowUpFilter_ApplyFilterClick;
            rFollowUpFilter.PreferenceKeyPrefix = "FollowUp";

            _canEdit = IsUserAuthorized( Authorization.EDIT );
            _canAdministrate = IsUserAuthorized( Authorization.ADMINISTRATE );
            _canView = IsUserAuthorized( Authorization.VIEW );
            _canViewAll = IsUserAuthorized( SecurityActionKey.ViewAll );
            _canViewCareWorker = IsUserAuthorized( SecurityActionKey.CareWorkers );

            var enterCareNeedEditPermission = GetAttributeValue( AttributeKey.EnterCareNeed ).AsBoolean();

            gList.GridRebind += gList_GridRebind;
            gList.RowDataBound += gList_RowDataBound;
            gList.RowCreated += gList_RowCreated;
            if ( _canEdit )
            {
                gList.RowSelected += gList_Edit;
            }
            gList.DataKeyNames = new string[] { "Id" };
            gList.Actions.ShowAdd = _canEdit || !enterCareNeedEditPermission;
            gList.Actions.AddClick += gList_AddClick;
            gList.IsDeleteEnabled = _canAdministrate;

            gFollowUp.GridRebind += gList_GridRebind;
            gFollowUp.RowDataBound += gList_RowDataBound;
            gFollowUp.RowCreated += gList_RowCreated;
            if ( _canEdit )
            {
                gFollowUp.RowSelected += gList_Edit;
            }
            gFollowUp.DataKeyNames = new string[] { "Id" };
            gFollowUp.Actions.ShowAdd = false;
            gFollowUp.Actions.ShowMergeTemplate = false;
            gFollowUp.IsDeleteEnabled = _canAdministrate;

            liEnterNeed.Visible = _canEdit || !enterCareNeedEditPermission;

            mdMakeNote.Footer.Visible = false;

            lbCareConfigure.Visible = _canAdministrate;

            // in case this is used as a Person Block, set the TargetPerson, future expansion point
            TargetPerson = ContextEntity<Person>();

            _careNeedNoteTypes = NoteTypeCache.GetByEntity( EntityTypeCache.Get( typeof( CareNeed ) ).Id, "", "", true );

            hDashboard.Visible = ( TargetPerson == null );

            pnlStepsToCareStats.Visible = ( TargetPerson == null );

            pnlFollowUpGrid.Visible = ( TargetPerson == null );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            LoadTouchTemplates();

            if ( !Page.IsPostBack )
            {
                SetFilter();
                AddNotificationAttributeControl( true );

                CheckAndAddQuickNote();
            }
            BindGrid();
            if ( !string.IsNullOrWhiteSpace( hfCareNeedId.Value ) )
            {
                using ( var rockContext = new RockContext() )
                {
                    var careNeed = new CareNeedService( rockContext ).Get( hfCareNeedId.Value.AsInteger() );

                    if ( careNeed != null )
                    {
                        SetupNoteTimeline( careNeed );
                    }
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
            SetFilter( false );
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
        /// Handles the Click event of the lbNotificationType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNotificationType_Click( object sender, EventArgs e )
        {
            AddNotificationAttributeControl( true );

            mdNotificationType.Show();
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
            rFilter.SaveUserPreference( UserPreferenceKey.Status, "Status", dvpStatus.SelectedValues.AsDelimited( ";" ) );
            rFilter.SaveUserPreference( UserPreferenceKey.Campus, "Campus", cpCampus.SelectedCampusId.ToString() );
            rFilter.SaveUserPreference( UserPreferenceKey.AssignedToMe, "Assigned to Me", cbAssignedToMe.Checked.ToString() );
            rFilter.SaveUserPreference( UserPreferenceKey.IncludeScheduledNeeds, "Include Scheduled Needs", cbIncludeFutureNeeds.Checked.ToString() );

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

            BindMainGrid( null, null, null );
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rFollowUpFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFollowUpFilter.SaveUserPreference( UserPreferenceKey.StartDateFollowUp, "Start Date", drpFollowUpDate.LowerValue.HasValue ? drpFollowUpDate.LowerValue.Value.ToString( "o" ) : string.Empty );
            rFollowUpFilter.SaveUserPreference( UserPreferenceKey.EndDateFollowUp, "End Date", drpFollowUpDate.UpperValue.HasValue ? drpFollowUpDate.UpperValue.Value.ToString( "o" ) : string.Empty );
            rFollowUpFilter.SaveUserPreference( UserPreferenceKey.FirstNameFollowUp, "First Name", tbFollowUpFirstName.Text );
            rFollowUpFilter.SaveUserPreference( UserPreferenceKey.LastNameFollowUp, "Last Name", tbFollowUpLastName.Text );
            rFollowUpFilter.SaveUserPreference( UserPreferenceKey.SubmittedByFollowUp, "Submitted By", ddlFollowUpSubmitter.SelectedItem.Value );
            rFollowUpFilter.SaveUserPreference( UserPreferenceKey.CategoryFollowUp, "Category", dvpFollowUpCategory.SelectedValues.AsDelimited( ";" ) );
            rFollowUpFilter.SaveUserPreference( UserPreferenceKey.CampusFollowUp, "Campus", cpFollowUpCampus.SelectedCampusId.ToString() );
            rFollowUpFilter.SaveUserPreference( UserPreferenceKey.AssignedToMeFollowUp, "Assigned to Me", cbFollowUpAssignedToMe.Checked.ToString() );

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phFollowUpAttributeFilters.FindControl( "filter_followup_" + attribute.Id.ToString() );
                    if ( filterControl != null )
                    {
                        try
                        {
                            var values = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                            rFollowUpFilter.SaveUserPreference( "filter_followup_" + attribute.Key, attribute.Name, attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter ).ToJson() );
                        }
                        catch
                        {
                            // intentionally ignore
                        }
                    }
                }
            }

            BindFollowUpGrid( null, null, null );
        }

        protected void rFilter_ClearFilterClick( object sender, EventArgs e )
        {
            rFilter.DeleteUserPreferences();
            SetFilter( false );
            BindMainGrid( null, null, null );
        }

        protected void rFollowUpFilter_ClearFilterClick( object sender, EventArgs e )
        {
            rFollowUpFilter.DeleteUserPreferences();
            SetFilter( false );
            BindFollowUpGrid( null, null, null );
        }

        /// <summary>
        /// Handles the filter display for each saved user value
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void rFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            if ( AvailableAttributes != null )
            {
                var attribute = AvailableAttributes.FirstOrDefault( a => a.Key == e.Key );
                if ( attribute != null )
                {
                    try
                    {
                        var values = JsonConvert.DeserializeObject<List<string>>( e.Value );
                        e.Value = attribute.FieldType.Field.FormatFilterValues( attribute.QualifierValues, values );
                        return;
                    }
                    catch
                    {
                        // intentionally ignore
                    }
                }
            }

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
                    e.Value = ResolveValues( e.Value, dvpStatus );
                    return;

                case UserPreferenceKey.AssignedToMe:
                case UserPreferenceKey.IncludeScheduledNeeds:
                    if ( !e.Value.AsBoolean() )
                    {
                        e.Value = string.Empty;
                    }
                    return;
                default:
                    e.Value = string.Empty;
                    return;
            }
        }

        /// <summary>
        /// Handles the filter display for each saved user value
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void rFollowUpFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            if ( AvailableAttributes != null )
            {
                var attribute = AvailableAttributes.FirstOrDefault( a => "filter_followup_" + a.Key == e.Key );
                if ( attribute != null )
                {
                    try
                    {
                        var values = JsonConvert.DeserializeObject<List<string>>( e.Value );
                        e.Value = attribute.FieldType.Field.FormatFilterValues( attribute.QualifierValues, values );
                        return;
                    }
                    catch
                    {
                        // intentionally ignore
                    }
                }
            }

            switch ( e.Key )
            {
                case UserPreferenceKey.StartDateFollowUp:
                case UserPreferenceKey.EndDateFollowUp:
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

                case UserPreferenceKey.FirstNameFollowUp:
                case UserPreferenceKey.LastNameFollowUp:
                    return;

                case UserPreferenceKey.CampusFollowUp:
                    {
                        int? campusId = e.Value.AsIntegerOrNull();
                        if ( campusId.HasValue )
                        {
                            e.Value = CampusCache.Get( campusId.Value ).Name;
                        }
                        return;
                    }

                case UserPreferenceKey.SubmittedByFollowUp:
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

                case UserPreferenceKey.CategoryFollowUp:
                    e.Value = ResolveValues( e.Value, dvpCategory );
                    return;

                case UserPreferenceKey.AssignedToMeFollowUp:
                    if ( !e.Value.AsBoolean() )
                    {
                        e.Value = string.Empty;
                    }
                    return;
                default:
                    e.Value = string.Empty;
                    return;
            }
        }

        /// <summary>
        /// Handles the RowDataBound event of the gList control.
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
                    if ( careNeed.Category != null )
                    {
                        careNeed.Category.LoadAttributes();
                        var categoryColor = careNeed.Category.GetAttributeValue( "Color" );
                        var categoryCell = e.Row.Cells[0];
                        if ( categoryCell != null && categoryColor.IsNotNullOrWhiteSpace() )
                        {
                            categoryCell.Style[HtmlTextWriterStyle.BackgroundColor] = categoryColor;
                        }
                    }

                    List<int> assignedFollowUpWorkers = new List<int>();
                    var assignedFollowUpWorkerCareTouch = false;
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
                                if ( assignedPerson.PersonAliasId == CurrentPersonAliasId )
                                {
                                    e.Row.AddCssClass( "assigned" );
                                }
                                if ( assignedPerson.FollowUpWorker && assignedPerson.PersonAliasId.HasValue )
                                {
                                    assignedFollowUpWorkers.Add( assignedPerson.PersonAliasId.Value );
                                }
                            }
                            lAssigned.Text = sbPersonHtml.ToString();
                        }
                    }

                    var hasChildNeeds = false;
                    var hasParentNeed = false;
                    if ( careNeed.ChildNeeds != null &&
                            (
                                ( careNeed.ChildNeeds.Any( cn => !cn.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) ) && _canViewAll ) ||
                                ( careNeed.ChildNeeds.Any( cn => cn.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) ) && !_canViewAll )
                            )
                       )
                    {
                        hasChildNeeds = true;
                        e.Row.AddCssClass( "hasChildNeeds" );
                        e.Row.AddCssClass( string.Format( "parentNeed{0}", careNeed.Id ) );
                    }

                    if ( careNeed.ParentNeedId.HasValue )
                    {
                        hasParentNeed = true;
                        if ( !e.Row.HasCssClass( "assigned" ) || careNeed.ParentNeed.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) )
                        {
                            e.Row.AddCssClass( "hasParentNeed" );
                            e.Row.AddCssClass( "hide" );
                            e.Row.AddCssClass( string.Format( "hasParentNeed{0}", careNeed.ParentNeedId.Value ) );

                            if ( careNeed.Details == careNeed.ParentNeed.Details )
                            {
                                var rbfName = e.Row.Controls[1] as DataControlFieldCell;
                                var rbfDetails = e.Row.Controls[2] as DataControlFieldCell;
                                if ( rbfName != null && rbfDetails != null )
                                {
                                    rbfName.ColumnSpan = 2;
                                    rbfDetails.Text = "";
                                    rbfDetails.Visible = false;
                                }
                            }
                        }
                        else
                        {
                            e.Row.AddCssClass( "hasParentNeedAssigned" );
                        }
                    }

                    var hasTouchTemplates = false;
                    if ( _touchTemplates.Count > 0 && careNeed.CategoryValueId.HasValue && _touchTemplates.ContainsKey( careNeed.CategoryValueId.Value ) )
                    {
                        hasTouchTemplates = true;
                    }

                    var minimumCareTouches = GetAttributeValue( AttributeKey.MinimumCareTouches ).AsInteger();
                    var careTouchCount = 0;
                    var careNeedNotesList = new List<Note>();
                    Literal lCareTouches = e.Row.FindControl( "lCareTouches" ) as Literal;
                    if ( lCareTouches != null )
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var noteType = _careNeedNoteTypes.FirstOrDefault();
                            if ( noteType != null )
                            {
                                var careNeedNotes = new NoteService( rockContext )
                                    .GetByNoteTypeId( noteType.Id ).AsNoTracking()
                                    .Where( n => n.EntityId == careNeed.Id && ( n.Caption == null || !n.Caption.StartsWith( "Action" ) ) );
                                careNeedNotesList = careNeedNotes.ToList();

                                lCareTouches.Text = careNeedNotes.Count().ToString();
                                careTouchCount = careNeedNotes.Count();

                                if ( assignedFollowUpWorkers.Any() )
                                {
                                    assignedFollowUpWorkerCareTouch = careNeedNotes.Any( n => assignedFollowUpWorkers.Contains( n.CreatedByPersonAliasId.Value ) );
                                }
                            }
                            else
                            {
                                lCareTouches.Text = "0";
                            }
                        }
                    }
                    var actionsColumn = gList.ColumnsOfType<RockTemplateField>().First( c => c.HeaderText == "Actions" );
                    var followUpGrid = false;
                    if ( e.Row.ClientID.Contains( "gFollowUp" ) )
                    {
                        actionsColumn = gFollowUp.ColumnsOfType<RockTemplateField>().First( c => c.HeaderText == "Actions" );
                        followUpGrid = true;
                    }
                    if ( actionsColumn.Visible )
                    {
                        TableCell actionsCell = null;
                        if ( followUpGrid )
                        {
                            actionsCell = e.Row.Cells[gFollowUp.Columns.IndexOf( actionsColumn )];
                        }
                        else
                        {
                            actionsCell = e.Row.Cells[gList.Columns.IndexOf( actionsColumn )];
                        }
                        actionsCell.CssClass += " align-middle";

                        var ddlNav = new HtmlGenericControl( "div" );
                        ddlNav.Attributes["class"] = "btn-group";
                        actionsCell.Controls.Add( ddlNav );

                        var ddlToggle = new HtmlGenericControl( "a" );
                        ddlToggle.Attributes["class"] = "btn btn-default btn-sm dropdown-toggle";
                        ddlToggle.Attributes["data-toggle"] = "dropdown";
                        ddlToggle.Attributes["href"] = "#";
                        ddlToggle.Attributes["tabindex"] = "0";
                        ddlNav.Controls.Add( ddlToggle );

                        var ddlToggleText = new HtmlGenericControl( "span" );
                        ddlToggleText.InnerText = "Actions ";
                        ddlToggle.Controls.Add( ddlToggleText );

                        var ddlToggleCaret = new HtmlGenericControl( "b" );
                        ddlToggleCaret.AddCssClass( "caret" );
                        ddlToggle.Controls.Add( ddlToggleCaret );

                        var ddlMenu = new HtmlGenericControl( "ul" );
                        ddlMenu.Attributes["class"] = "dropdown-menu dropdown-menu-right";
                        ddlNav.Controls.Add( ddlMenu );

                        var actionItem1 = new HtmlGenericControl( "li" );
                        ddlMenu.Controls.Add( actionItem1 );

                        var canCompleteNeeds = IsUserAuthorized( SecurityActionKey.CompleteNeeds );
                        if ( canCompleteNeeds )
                        {
                            var lbCompleteNeed = new LinkButton();
                            lbCompleteNeed.Command += lbNeedAction_Click;
                            lbCompleteNeed.CommandArgument = careNeed.Id.ToString();
                            lbCompleteNeed.CommandName = "complete";
                            lbCompleteNeed.Text = GetAttributeValue( AttributeKey.CompleteActionText );
                            actionItem1.Controls.Add( lbCompleteNeed );
                        }

                        if ( careNeed.Status.Guid != rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_SNOOZED.AsGuid() && careNeed.Status.Guid != rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_CLOSED.AsGuid() )
                        {
                            var lbSnoozeNeed = new LinkButton();
                            lbSnoozeNeed.Command += lbNeedAction_Click;
                            lbSnoozeNeed.CommandArgument = careNeed.Id.ToString();
                            lbSnoozeNeed.CommandName = "snooze";
                            lbSnoozeNeed.Text = GetAttributeValue( AttributeKey.SnoozeActionText );
                            actionItem1.Controls.Add( lbSnoozeNeed );
                        }

                        if ( careNeed.Status.Guid == rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_FOLLOWUP.AsGuid() || careNeed.Status.Guid == rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_SNOOZED.AsGuid() )
                        {
                            var actionItemReopen = new HtmlGenericControl( "li" );
                            ddlMenu.Controls.Add( actionItemReopen );

                            var lbReOpenNeed = new LinkButton();
                            lbReOpenNeed.Command += lbNeedAction_Click;
                            lbReOpenNeed.CommandArgument = careNeed.Id.ToString();
                            lbReOpenNeed.CommandName = "reopen";
                            lbReOpenNeed.Text = "Re-Open";
                            actionItemReopen.Controls.Add( lbReOpenNeed );
                        }

                        var launchWorkflowEnabled = GetAttributeValue( AttributeKey.WorkflowEnable ).AsBoolean();
                        if ( launchWorkflowEnabled )
                        {
                            var actionItem2 = new HtmlGenericControl( "li" );
                            ddlMenu.Controls.Add( actionItem2 );

                            var lbLaunchWorkflow = new LinkButton();
                            lbLaunchWorkflow.Command += lbNeedAction_Click;
                            lbLaunchWorkflow.CommandArgument = careNeed.Id.ToString();
                            lbLaunchWorkflow.CommandName = "launchworkflow";
                            lbLaunchWorkflow.Text = "Launch Workflow";
                            actionItem1.Controls.Add( lbLaunchWorkflow );
                        }

                        var prayerDetailPage = LinkedPageRoute( AttributeKey.PrayerDetailPage );
                        if ( prayerDetailPage.IsNotNullOrWhiteSpace() )
                        {
                            var actionItem3 = new HtmlGenericControl( "li" );
                            ddlMenu.Controls.Add( actionItem3 );

                            var lbLaunchPrayer = new LinkButton();
                            lbLaunchPrayer.Command += lbNeedAction_Click;
                            lbLaunchPrayer.CommandArgument = careNeed.Id.ToString();
                            lbLaunchPrayer.CommandName = "prayer";
                            lbLaunchPrayer.Text = "Add Prayer Request";
                            actionItem3.Controls.Add( lbLaunchPrayer );
                        }

                        var benevolenceDetailPage = new Rock.Web.PageReference( GetAttributeValue( AttributeKey.BenevolenceDetailPage ) );
                        if ( benevolenceDetailPage != null && benevolenceDetailPage.PageId > 1 && PageCache.Get( benevolenceDetailPage.PageId ).IsAuthorized( Authorization.EDIT, CurrentPerson ) )
                        {
                            var actionItem4 = new HtmlGenericControl( "li" );
                            ddlMenu.Controls.Add( actionItem4 );

                            var lbLaunchBenevolence = new LinkButton();
                            lbLaunchBenevolence.Command += lbNeedAction_Click;
                            lbLaunchBenevolence.CommandArgument = careNeed.Id.ToString();
                            lbLaunchBenevolence.CommandName = "benevolence";
                            lbLaunchBenevolence.Text = "Add Benevolence Request";
                            actionItem4.Controls.Add( lbLaunchBenevolence );
                        }

                        var connectionRequestEnabled = GetAttributeValue( AttributeKey.ConnectionRequestEnable ).AsBoolean();
                        if ( connectionRequestEnabled )
                        {
                            var actionItem5 = new HtmlGenericControl( "li" );
                            ddlMenu.Controls.Add( actionItem5 );

                            var lbLaunchConnection = new LinkButton();
                            lbLaunchConnection.Command += lbNeedAction_Click;
                            lbLaunchConnection.CommandArgument = careNeed.Id.ToString();
                            lbLaunchConnection.CommandName = "connection";
                            lbLaunchConnection.Text = "Add Connection Request";
                            actionItem5.Controls.Add( lbLaunchConnection );
                        }

                        var careNeedHistoryPage = LinkedPageRoute( AttributeKey.CareNeedHistoryPage );
                        if ( careNeedHistoryPage.IsNotNullOrWhiteSpace() )
                        {
                            var actionItem6 = new HtmlGenericControl( "li" );
                            ddlMenu.Controls.Add( actionItem6 );

                            var lbLaunchHistory = new LinkButton();
                            lbLaunchHistory.Command += lbNeedAction_Click;
                            lbLaunchHistory.CommandArgument = careNeed.Id.ToString();
                            lbLaunchHistory.CommandName = "history";
                            lbLaunchHistory.Text = "View History";
                            actionItem6.Controls.Add( lbLaunchHistory );
                        }
                    }

                    Literal lName = e.Row.FindControl( "lName" ) as Literal;
                    if ( lName != null )
                    {
                        var minimumCareTouchHours = GetAttributeValue( AttributeKey.MinimumCareTouchHours ).AsInteger();
                        var minimumFollowUpTouchHours = GetAttributeValue( AttributeKey.MinimumFollowUpTouchHours ).AsInteger();
                        var dateDifference = RockDateTime.Now - careNeed.DateEntered.Value;
                        var careNeedFlag = ( dateDifference.TotalHours >= minimumCareTouchHours && careTouchCount < minimumCareTouches );
                        var careNeedFollowUpWorkerTouch = assignedFollowUpWorkers.Any() && minimumFollowUpTouchHours != 0 && dateDifference.TotalHours >= minimumFollowUpTouchHours && !assignedFollowUpWorkerCareTouch;
                        var careNeedFlagStr = "";
                        var careNeedFlagStrTooltip = "";
                        var childNeedStr = "";
                        var parentNeedStr = "";
                        var touchTooltipTemplate = GetAttributeValue( AttributeKey.TouchNeededTooltipTemplate );
                        var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                        mergeFields.Add( "CareNeed", careNeed );
                        mergeFields.Add( "TouchTemplate", "" );
                        mergeFields.Add( "TouchCount", careTouchCount );
                        mergeFields.Add( "TouchCompareDate", careNeed.DateEntered.Value );
                        mergeFields.Add( "MinimumCareTouches", minimumCareTouches );
                        mergeFields.Add( "MinimumCareTouchHours", minimumCareTouchHours );
                        mergeFields.Add( "MinimumFollowUpCareTouchHours", minimumFollowUpTouchHours );

                        if ( careNeedFlag )
                        {
                            mergeFields["TouchTemplate"] = "Not Enough";
                            careNeedFlagStrTooltip += $@"
                            <div class='assessment-tooltip-item'>
                                <span style='color:#E15759;' class='assessment-caretouch'>
                                    <span class='fa-stack'>
                                        <i class='fa fa-circle fa-stack-2x'></i>
                                        <i class='fa fa-handshake fa-stack-1x text-white'></i>
                                    </span>
                                </span>
                                <span class='assessment-tooltip-value'>
                                    <span class='assessment-name'>Not Enough Care Touches</span>
                                    <span class='assessment-summary'>{touchTooltipTemplate.ResolveMergeFields( mergeFields )}</span>
                                </span>
                            </div>";
                        }
                        if ( hasTouchTemplates )
                        {
                            var catTouchTemplates = _touchTemplates.GetValueOrDefault( careNeed.CategoryValueId.Value, new List<TouchTemplate>() );
                            foreach ( var template in catTouchTemplates )
                            {
                                var templateNotes = careNeedNotesList.Where( n => n.Text.Equals( template.NoteTemplate.Note ) || n.ForeignGuid.Equals( template.NoteTemplate.Guid ) );
                                var templateNotesToCheckInHours = templateNotes.Where( n => ( RockDateTime.Now - n.CreatedDateTime ).Value.TotalHours <= template.MinimumCareTouchHours );
                                var templateFlag = ( ( template.Recurring && templateNotesToCheckInHours.Count() < template.MinimumCareTouches ) || ( !template.Recurring && templateNotes.Count() < template.MinimumCareTouches ) ) && dateDifference.TotalHours >= template.MinimumCareTouchHours;

                                if ( templateFlag )
                                {
                                    mergeFields["TouchTemplate"] = template;
                                    mergeFields["TouchCount"] = ( template.Recurring ) ? templateNotesToCheckInHours.Count() : templateNotes.Count();
                                    mergeFields["TouchCompareDate"] = templateNotes.OrderByDescending( n => n.CreatedDateTime ).Select( n => n.CreatedDateTime ).FirstOrDefault();
                                    mergeFields["MinimumCareTouches"] = template.MinimumCareTouches;
                                    mergeFields["MinimumCareTouchHours"] = template.MinimumCareTouchHours;
                                    var randomColor = GetNoteTemplateColor( template.NoteTemplate );
                                    careNeedFlagStrTooltip += $@"
                                <div class='assessment-tooltip-item'>
                                    <span style='color:{randomColor.BackgroundColor};' class='assessment-caretouch'>
                                        <span class='fa-stack'>
                                            <i class='fa fa-circle fa-stack-2x'></i>
                                            <i class='{template.NoteTemplate.Icon} fa-stack-1x' style='color:{randomColor.ForegroundColor};'></i>
                                        </span>
                                    </span>
                                    <span class='assessment-tooltip-value'>
                                        <span class='assessment-name'>{template.NoteTemplate.Note} Touches Needed</span>
                                        <span class='assessment-summary'>{touchTooltipTemplate.ResolveMergeFields( mergeFields )}</span>
                                    </span>
                                </div>";
                                    continue;
                                }
                            }
                        }
                        if ( careNeedFollowUpWorkerTouch )
                        {
                            mergeFields["TouchTemplate"] = "Follow Up Worker";
                            careNeedFlagStrTooltip += $@"<div class='assessment-tooltip-item'>
                                <span style='color:#F28E2B;' class='assessment-caretouch'>
                                    <span class='fa-stack'>
                                        <i class='fa fa-circle fa-stack-2x'></i>
                                        <i class='fa fa-user fa-stack-1x text-white'></i>
                                    </span>
                                </span>
                                <span class='assessment-tooltip-value'>
                                    <span class='assessment-name'>Follow Up Worker</span>
                                    <span class='assessment-summary'>{touchTooltipTemplate.ResolveMergeFields( mergeFields )}</span>
                                </span>
                            </div>";
                        }
                        if ( careNeedFlagStrTooltip.IsNotNullOrWhiteSpace() )
                        {
                            careNeedFlagStr = $"<i class=\"fas fa-flag text-danger mx-2\" data-toggle=\"tooltip\" data-html=\"true\" data-original-title=\"{careNeedFlagStrTooltip.Replace( "\"", "&quot;" )}\"></i>";
                        }
                        if ( hasChildNeeds )
                        {
                            childNeedStr = string.Format( "<a href=\"javascript:toggleNeeds('{0}');\" class=\"mr-2\" id=\"toggleLink{0}\"><i class=\"fas fa-plus fa-lg\" data-toggle=\"tooltip\" title=\"Toggle Child Needs\" id=\"toggleIcon{0}\"></i></a>", careNeed.Id );
                        }
                        if ( hasParentNeed )
                        {
                            if ( _canViewAll || careNeed.ParentNeed.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) )
                            {
                                parentNeedStr = string.Format( "<a href=\"{0}\"><i class=\"fas fa-child mx-2\" data-toggle=\"tooltip\" title=\"Has a Parent Need\"></i></a>", ResolveUrl( string.Format( "{0}/{1}", LinkedPageRoute( AttributeKey.DetailPage ), careNeed.ParentNeedId ) ) );
                            }
                            else
                            {
                                parentNeedStr = string.Format( "<i class=\"fas fa-child mx-2\" data-toggle=\"tooltip\" title=\"Has a Parent Need: {0} - {1}\"></i>", careNeed.ParentNeed.Details.StripHtml().Truncate( 30 ), careNeed.ParentNeed.PersonAlias.Person.FullName );
                            }
                        }
                        if ( TargetPerson != null )
                        {
                            careNeedFlagStr = "";
                        }
                        if ( careNeed.PersonAlias != null )
                        {
                            lName.Text = string.Format( "{3} {4} <a href=\"{0}\" class=\"js-person-popover-stepstocare\" personid=\"{5}\">{1}</a> {2}", ResolveUrl( string.Format( "~/Person/{0}", careNeed.PersonAlias.PersonId ) ), careNeed.PersonAlias.Person.FullName ?? string.Empty, careNeedFlagStr, childNeedStr, parentNeedStr, careNeed.PersonAlias.PersonId );
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Handles the DataBound event of the lStatus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void lStatus_DataBound( object sender, RowEventArgs e )
        {
            if ( TargetPerson != null || dvpStatus.SelectedValuesAsInt.Count != 1 )
            {
                Literal lStatus = sender as Literal;
                CareNeed careNeed = e.Row.DataItem as CareNeed;
                careNeed.Status.LoadAttributes();
                lStatus.Text = string.Format( "<span class='{0}'>{1}</span>", careNeed.Status.GetAttributeValue( "CssClass" ), careNeed.Status.Value );
            }
        }

        private void lbNeedAction_Click( object sender, CommandEventArgs e )
        {
            var id = e.CommandArgument.ToString().AsInteger();
            switch ( e.CommandName )
            {
                case "complete":
                    // do something with complete here.
                    using ( var rockContext = new RockContext() )
                    {
                        var completeChildNeeds = GetAttributeValue( AttributeKey.CompleteChildNeeds ).AsBoolean();
                        var careNeedService = new CareNeedService( rockContext );
                        var careNeed = careNeedService.Get( id );
                        var completeValueId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_CLOSED ).Id;
                        careNeed.StatusValueId = completeValueId;

                        if ( completeChildNeeds && careNeed.ChildNeeds.Any() )
                        {
                            foreach ( var childneed in careNeed.ChildNeeds )
                            {
                                childneed.StatusValueId = completeValueId;
                            }
                        }
                        rockContext.SaveChanges();

                        createNote( rockContext, id, "Marked Complete" );

                        if ( completeChildNeeds && careNeed.ChildNeeds.Any() )
                        {
                            foreach ( var childneed in careNeed.ChildNeeds )
                            {
                                createNote( rockContext, childneed.Id, "Marked Complete from Parent Need" );
                            }
                        }
                    }
                    BindGrid();
                    break;
                case "reopen":
                    // do something with re-open here.
                    using ( var rockContext = new RockContext() )
                    {
                        var careNeedService = new CareNeedService( rockContext );
                        var careNeed = careNeedService.Get( id );
                        var openValueId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_OPEN ).Id;
                        careNeed.StatusValueId = openValueId;
                        careNeed.DateEntered = RockDateTime.Now;
                        rockContext.SaveChanges();

                        createNote( rockContext, id, "Re-Open Need" );
                    }
                    BindGrid();
                    break;
                case "launchworkflow":
                    using ( var rockContext = new RockContext() )
                    {
                        var entitySet = new EntitySet();
                        entitySet.EntityTypeId = EntityTypeCache.Get<CareNeed>().Id;
                        entitySet.ExpireDateTime = RockDateTime.Now.AddMinutes( 5 );
                        List<EntitySetItem> entitySetItems = new List<Rock.Model.EntitySetItem>();

                        var item = new EntitySetItem();
                        item.EntityId = id;
                        entitySet.Items.Add( item );

                        if ( entitySet.Items.Any() )
                        {
                            var service = new EntitySetService( rockContext );
                            service.Add( entitySet );
                            rockContext.SaveChanges();

                            var routeTemplate = GetRouteFromEventArgs( e ) ?? DefaultLaunchWorkflowPageRoute;
                            string url;

                            // If the user passed a format-able string like "/Launch/{0}", then fill in the entity set id
                            // accordingly. Otherwise, add the entity set id as a query param.
                            if ( routeTemplate.Contains( "{0}" ) )
                            {
                                url = string.Format( routeTemplate, entitySet.Id );
                            }
                            else
                            {
                                var uri = new Uri( Page.Request.Url, routeTemplate );
                                var uriBuilder = new UriBuilder( uri.AbsoluteUri );
                                var paramValues = HttpUtility.ParseQueryString( uriBuilder.Query );
                                paramValues.Add( "EntitySetId", entitySet.Id.ToString() );
                                uriBuilder.Query = paramValues.ToString();
                                url = uriBuilder.Uri.PathAndQuery;
                            }

                            Page.Response.Redirect( url, false );
                        }
                        createNote( rockContext, id, "Workflow Launched" );
                    }
                    break;
                case "prayer":
                    using ( var rockContext = new RockContext() )
                    {
                        PrayerRequest prayerRequest;
                        PrayerRequestService prayerRequestService = new PrayerRequestService( rockContext );
                        var careNeedService = new CareNeedService( rockContext );
                        var careNeed = careNeedService.Get( id );

                        prayerRequest = new PrayerRequest();
                        prayerRequestService.Add( prayerRequest );
                        prayerRequest.EnteredDateTime = RockDateTime.Now;

                        if ( careNeed.PersonAliasId.HasValue )
                        {
                            prayerRequest.RequestedByPersonAliasId = careNeed.PersonAliasId;
                            prayerRequest.FirstName = careNeed.PersonAlias.Person.FirstName;
                            prayerRequest.LastName = careNeed.PersonAlias.Person.LastName;
                            prayerRequest.Email = careNeed.PersonAlias.Person.Email;
                        }

                        //var expireDays = Convert.ToDouble( GetAttributeValue( "ExpireDays" ) );
                        //prayerRequest.ExpirationDate = RockDateTime.Now.AddDays( expireDays );

                        prayerRequest.CampusId = careNeed.CampusId;

                        prayerRequest.Text = careNeed.Details.Trim();

                        prayerRequest.LoadAttributes( rockContext );

                        if ( !prayerRequest.IsValid )
                        {
                            mdGridWarning.Show( "Prayer Request is invalid. <br>" + prayerRequest.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" ), ModalAlertType.Alert );
                            return;
                        }

                        rockContext.SaveChanges();
                        prayerRequest.SaveAttributeValues( rockContext );

                        var qryParams = new Dictionary<string, string>();
                        qryParams.Add( "PrayerRequestId", prayerRequest.Id.ToString() );

                        NavigateToLinkedPage( AttributeKey.PrayerDetailPage, qryParams );
                    }
                    break;
                case "benevolence":
                    using ( var rockContext = new RockContext() )
                    {
                        var careNeedService = new CareNeedService( rockContext );
                        var careNeed = careNeedService.Get( id );
                        BenevolenceRequestService benevolenceRequestService = new BenevolenceRequestService( rockContext );
                        BenevolenceResultService benevolenceResultService = new BenevolenceResultService( rockContext );
                        var requestType = new BenevolenceTypeService( rockContext ).Get( GetAttributeValue( AttributeKey.BenevolenceType ) );
                        if ( requestType == null )
                        {
                            mdGridWarning.Show( "Invalid benevolence type provided.", ModalAlertType.Alert );
                            break;
                        }
                        BenevolenceRequest benevolenceRequest = null;
                        benevolenceRequest = new BenevolenceRequest { Id = 0 };

                        benevolenceRequest.FirstName = careNeed.PersonAlias.Person.FirstName;
                        benevolenceRequest.LastName = careNeed.PersonAlias.Person.LastName;
                        benevolenceRequest.Email = careNeed.PersonAlias.Person.Email;
                        benevolenceRequest.RequestText = careNeed.Details;
                        benevolenceRequest.CampusId = careNeed.CampusId;
                        benevolenceRequest.BenevolenceTypeId = requestType.Id;

                        if ( careNeed.PersonAlias.Person.GetHomeLocation() != null )
                        {
                            benevolenceRequest.LocationId = careNeed.PersonAlias.Person.GetHomeLocation().Id;
                        }

                        benevolenceRequest.RequestedByPersonAliasId = careNeed.PersonAliasId;

                        var pendingStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.BENEVOLENCE_PENDING );
                        benevolenceRequest.RequestStatusValueId = pendingStatus.Id;

                        benevolenceRequest.RequestDateTime = RockDateTime.Now;

                        benevolenceRequest.HomePhoneNumber = careNeed.PersonAlias.Person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() )?.NumberFormatted;
                        benevolenceRequest.CellPhoneNumber = careNeed.PersonAlias.Person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() )?.NumberFormatted;
                        benevolenceRequest.WorkPhoneNumber = careNeed.PersonAlias.Person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() )?.NumberFormatted;

                        if ( benevolenceRequest.IsValid )
                        {
                            if ( benevolenceRequest.Id.Equals( 0 ) )
                            {
                                benevolenceRequestService.Add( benevolenceRequest );
                            }

                            // get attributes
                            benevolenceRequest.LoadAttributes();

                            rockContext.WrapTransaction( () =>
                            {
                                rockContext.SaveChanges();
                                benevolenceRequest.SaveAttributeValues( rockContext );
                            } );

                            var qryParams = new Dictionary<string, string>();
                            qryParams.Add( "BenevolenceRequestId", benevolenceRequest.Id.ToString() );

                            NavigateToLinkedPage( AttributeKey.BenevolenceDetailPage, qryParams );
                        }
                        else
                        {
                            mdGridWarning.Show( "Benevolence Request is invalid. <br>" + benevolenceRequest.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" ), ModalAlertType.Alert );
                        }
                    }
                    break;
                case "connection":
                    hfConnectionCareNeedId.Value = id.ToString();
                    using ( var rockContext = new RockContext() )
                    {
                        var careNeedService = new CareNeedService( rockContext );
                        var careNeed = careNeedService.Get( id );
                        tbComments.Text = careNeed.Details;
                    }
                    LoadOpportunities();
                    nbSuccess.Visible = false;
                    nbDanger.Visible = false;
                    tbComments.Visible = true;
                    pnlConnectionTypes.Visible = true;
                    mdConnectionRequest.SaveClick += mdConnectionRequest_SaveClick;
                    mdConnectionRequest.Show();
                    break;
                case "snooze":
                    using ( var rockContext = new RockContext() )
                    {
                        var careNeedService = new CareNeedService( rockContext );
                        var careNeed = careNeedService.Get( id );
                        if ( careNeed.CustomFollowUp && ( !careNeed.RenewMaxCount.HasValue || careNeed.RenewCurrentCount <= careNeed.RenewMaxCount.Value ) )
                        {
                            SnoozeNeed( id );
                            BindGrid();
                        }
                        else
                        {
                            hfSnoozeNeed_CareNeedId.Value = id.ToString();
                            mdSnoozeNeed.Show();
                        }
                    }
                    break;
                case "history":
                    var qryParamsHistory = new Dictionary<string, string>
                    {
                        { "CareNeedId", id.ToString() }
                    };
                    NavigateToLinkedPage( AttributeKey.CareNeedHistoryPage, qryParamsHistory );
                    break;
                default:
                    break;
            }
        }

        protected void mdConnectionRequest_SaveClick( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var opportunityService = new ConnectionOpportunityService( rockContext );
                var connectionRequestService = new ConnectionRequestService( rockContext );
                var careNeedService = new CareNeedService( rockContext );
                var careNeed = careNeedService.Get( hfConnectionCareNeedId.ValueAsInt() );

                if ( careNeed != null && careNeed.Id > 0 )
                {
                    var person = careNeed.PersonAlias.Person;
                    List<string> opportunityNames = new List<string>();

                    // If there is a valid person with a primary alias, continue
                    if ( person != null && person.PrimaryAliasId.HasValue )
                    {

                        foreach ( RepeaterItem typeItem in rptConnnectionTypes.Items )
                        {
                            var cblOpportunities = typeItem.FindControl( "cblOpportunities" ) as RockCheckBoxList;
                            foreach ( int connectionOpportunityId in cblOpportunities.SelectedValuesAsInt )
                            {

                                // Get the opportunity and default status
                                var opportunity = opportunityService
                                    .Queryable()
                                    .Where( o => o.Id == connectionOpportunityId )
                                    .FirstOrDefault();

                                int defaultStatusId = opportunity.ConnectionType.ConnectionStatuses
                                    .Where( s => s.IsDefault )
                                    .Select( s => s.Id )
                                    .FirstOrDefault();

                                // If opportunity is valid and has a default status
                                if ( opportunity != null && defaultStatusId > 0 )
                                {
                                    // Now we create the connection request
                                    var connectionRequest = new ConnectionRequest();
                                    connectionRequest.PersonAliasId = person.PrimaryAliasId.Value;
                                    connectionRequest.Comments = string.Format( "{0} (Added from Care Need #{1})", tbComments.Text.Trim(), careNeed.Id.ToString() );
                                    connectionRequest.ConnectionOpportunityId = opportunity.Id;
                                    connectionRequest.ConnectionState = ConnectionState.Active;
                                    connectionRequest.ConnectionStatusId = defaultStatusId;

                                    if ( person.GetCampus() != null )
                                    {
                                        var campusId = person.GetCampus().Id;
                                        connectionRequest.CampusId = campusId;
                                        connectionRequest.ConnectorPersonAliasId = opportunity.GetDefaultConnectorPersonAliasId( campusId );
                                        if (
                                            opportunity != null &&
                                            opportunity.ConnectionOpportunityCampuses != null )
                                        {
                                            var campus = opportunity.ConnectionOpportunityCampuses
                                                .Where( c => c.CampusId == campusId )
                                                .FirstOrDefault();
                                            if ( campus != null )
                                            {
                                                connectionRequest.ConnectorPersonAliasId = campus.DefaultConnectorPersonAliasId;
                                            }
                                        }
                                    }

                                    if ( !connectionRequest.IsValid )
                                    {
                                        return;
                                    }

                                    opportunityNames.Add( opportunity.Name );

                                    connectionRequestService.Add( connectionRequest );
                                }
                            }
                        }
                    }

                    if ( opportunityNames.Count > 0 )
                    {

                        rockContext.SaveChanges();

                        // Reset everything for the next person
                        tbComments.Text = string.Empty;
                        LoadOpportunities();

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine( String.Format( "{0}'s connection requests have been entered for the following opportunities:\n<ul>", person.FullName ) );
                        foreach ( var name in opportunityNames )
                        {
                            sb.AppendLine( String.Format( "<li> {0}</li>", name ) );
                        }
                        sb.AppendLine( "</ul>" );

                        mdConnectionRequest.SaveClick -= mdConnectionRequest_SaveClick;

                        tbComments.Visible = false;
                        pnlConnectionTypes.Visible = false;
                        nbSuccess.Text = sb.ToString();
                        nbSuccess.Visible = true;
                        nbDanger.Visible = false;
                    }
                    else
                    {
                        nbSuccess.Visible = false;
                        nbDanger.Visible = true;
                        nbDanger.Text = "Please select an opportunity.";
                    }
                }
                else
                {
                    nbSuccess.Visible = false;
                    nbDanger.Visible = true;
                    nbDanger.Text = "Care Need is invalid, please try again.";
                }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptConnnectionTypes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptConnnectionTypes_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var cblOpportunities = e.Item.FindControl( "cblOpportunities" ) as RockCheckBoxList;
            var lConnectionTypeName = e.Item.FindControl( "lConnectionTypeName" ) as Literal;
            var connectionType = e.Item.DataItem as ConnectionType;
            if ( cblOpportunities != null && lConnectionTypeName != null && connectionType != null )
            {
                lConnectionTypeName.Text = String.Format( "<h4 class='block-title'>{0}</h4>", connectionType.Name );

                cblOpportunities.DataSource = connectionType.ConnectionOpportunities.Where( c => c.IsActive ).OrderBy( c => c.Name );
                cblOpportunities.DataBind();
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
                var autoSaveQuickNote = GetAttributeValue( AttributeKey.QuickNoteAutoSave ).AsBoolean();

                if ( autoSaveQuickNote )
                {

                    var noteTemplate = new NoteTemplateService( rockContext ).Get( fieldId.AsInteger() );
                    var shouldSnoozeNeed = GetAttributeValue( AttributeKey.MoveNeedToSnoozed ).AsBoolean();

                    var noteCreated = createNote( rockContext, e.RowKeyId, noteTemplate.Note, true, noteTemplate, true );
                    if ( noteCreated )
                    {
                        if ( shouldSnoozeNeed )
                        {
                            var careNeed = new CareNeedService( rockContext ).Get( e.RowKeyId );
                            if ( careNeed != null && careNeed.CustomFollowUp )
                            {
                                SnoozeNeed( e.RowKeyId );
                            }
                        }
                        BindGrid();
                    }
                }
                else
                {
                    rrblQuickNotes.SelectedValue = fieldId;
                    rtbNote.Text = "";
                    pnlQuickNoteText.Visible = true;

                    notesTimeline.AddAllowed = false;
                    notesTimeline.AddAlwaysVisible = false;

                    gMakeNote_Click( null, e );
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
            using ( var rockContext = new RockContext() )
            {
                string messages = string.Empty;
                hfCareNeedId.Value = e.RowKeyId.ToString();

                if ( sender != null )
                {
                    rrblQuickNotes.SelectedValue = "-1";
                    rtbNote.Text = "";
                    pnlQuickNoteText.Visible = false;

                    notesTimeline.AddAllowed = true;
                    notesTimeline.AddAlwaysVisible = true;
                }

                mdMakeNote.Show();

                SetupNoteDialog( e.RowKeyId, rockContext );

                if ( pnlQuickNoteText.Visible )
                {
                    rtbNote.Focus();
                }

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
            if ( _canEdit )
            {
                var qryParams = new Dictionary<string, string>();
                qryParams.Add( "CareNeedId", e.RowKeyId.ToString() );
                if ( TargetPerson != null )
                {
                    qryParams.Add( "PersonId", TargetPerson.Id.ToString() );
                }

                NavigateToLinkedPage( "DetailPage", qryParams );
            }
        }

        /// <summary>
        /// Handles the Delete event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gList_Delete( object sender, RowEventArgs e )
        {
            if ( _canAdministrate )
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
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gList control.
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
                if ( notesField != null )
                {
                    var notesFieldIndex = gList.GetColumnIndex( notesField );
                    if ( e.Row.Cells.Count > notesFieldIndex + 3 ) // ensure there are more columns to colspan with, +3 for itself, make a note and delete columns
                    {
                        e.Row.Cells[notesFieldIndex].ColumnSpan = 2;
                        //now make up for the colspan from cell2
                        e.Row.Cells.RemoveAt( notesFieldIndex + 1 );
                    }
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

        protected void mdNotificationType_SaveClick( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var attribute = AttributeCache.Get( rocks.kfs.StepsToCare.SystemGuid.PersonAttribute.NOTIFICATION );

            if ( CurrentPerson != null )
            {
                Control attributeControl = phNotificationAttribute.FindControl( string.Format( "attribute_field_{0}", attribute.Id ) );
                if ( attributeControl != null )
                {
                    string newValue = attribute.FieldType.Field.GetEditValue( attributeControl, attribute.QualifierValues );
                    Rock.Attribute.Helper.SaveAttributeValue( CurrentPerson, attribute, newValue, rockContext );
                }
            }

            mdNotificationType.Hide();
        }

        protected void mdConfirmNote_SaveClick( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var noteTemplate = new NoteTemplateService( rockContext ).Get( hfQuickNote_NoteId.ValueAsInt() );

                var noteCreated = createNote( rockContext, hfQuickNote_CareNeedId.ValueAsInt(), noteTemplate.Note, true, noteTemplate, true );
                if ( noteCreated )
                {
                    if ( GetAttributeValue( AttributeKey.MoveNeedToSnoozed ).AsBoolean() )
                    {
                        var careNeed = new CareNeedService( rockContext ).Get( hfQuickNote_CareNeedId.ValueAsInt() );
                        if ( careNeed != null && careNeed.CustomFollowUp )
                        {
                            SnoozeNeed( hfQuickNote_CareNeedId.ValueAsInt() );
                        }
                    }

                    BindGrid();
                    mdConfirmNote.Hide();
                }
            }

        }

        protected void mdSnoozeNeed_SaveClick( object sender, EventArgs e )
        {
            SnoozeNeed( hfSnoozeNeed_CareNeedId.ValueAsInt(), dpSnoozeUntil.SelectedDate );
            BindGrid();
            mdSnoozeNeed.Hide();
        }

        protected void rrblQuickNotes_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( rrblQuickNotes.SelectedValue == "-1" )
            {
                notesTimeline.AddAllowed = true;
                notesTimeline.AddAlwaysVisible = true;
                pnlQuickNoteText.Visible = false;
            }
            else
            {
                notesTimeline.AddAllowed = false;
                notesTimeline.AddAlwaysVisible = false;

                pnlQuickNoteText.Visible = true;
                rtbNote.Focus();
            }
        }

        protected void rbBtnQuickNoteSave_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var noteTemplate = new NoteTemplateService( rockContext ).Get( rrblQuickNotes.SelectedValueAsInt() ?? 0 );
                var closeDialogOnSave = GetAttributeValue( AttributeKey.CloseDialogOnSave ).AsBoolean();
                var careNeedId = hfCareNeedId.ValueAsInt();

                var noteCreated = createNote( rockContext, careNeedId, $"{noteTemplate.Note}{( rtbNote.Text.IsNotNullOrWhiteSpace() ? ":" : "" )} {rtbNote.Text}", closeDialogOnSave, noteTemplate, true, cbAlert.Checked, cbPrivate.Checked );
                if ( noteCreated )
                {
                    if ( GetAttributeValue( AttributeKey.MoveNeedToSnoozed ).AsBoolean() )
                    {
                        var careNeed = new CareNeedService( rockContext ).Get( hfCareNeedId.ValueAsInt() );
                        if ( careNeed != null && careNeed.CustomFollowUp )
                        {
                            SnoozeNeed( hfCareNeedId.ValueAsInt() );
                        }
                    }

                    rtbNote.Text = "";
                    if ( closeDialogOnSave )
                    {
                        hfCareNeedId.Value = "";
                        rrblQuickNotes.SelectedValue = "-1";
                        mdMakeNote.Hide();
                    }
                    else
                    {
                        SetupNoteDialog( careNeedId, rockContext );
                    }
                    BindGrid();
                }
            }
        }

        #endregion Events

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void SetFilter( bool refreshColumns = true )
        {
            using ( var rockContext = new RockContext() )
            {
                drpDate.LowerValue = rFilter.GetUserPreference( UserPreferenceKey.StartDate ).AsDateTime();
                drpDate.UpperValue = rFilter.GetUserPreference( UserPreferenceKey.EndDate ).AsDateTime();
                drpFollowUpDate.LowerValue = rFollowUpFilter.GetUserPreference( UserPreferenceKey.StartDateFollowUp ).AsDateTime();
                drpFollowUpDate.UpperValue = rFollowUpFilter.GetUserPreference( UserPreferenceKey.EndDateFollowUp ).AsDateTime();

                cpCampus.Campuses = CampusCache.All();
                cpCampus.SelectedCampusId = rFilter.GetUserPreference( UserPreferenceKey.Campus ).AsInteger();
                cpFollowUpCampus.Campuses = CampusCache.All();
                cpFollowUpCampus.SelectedCampusId = rFollowUpFilter.GetUserPreference( UserPreferenceKey.CampusFollowUp ).AsInteger();

                // hide the First/Last name filter if this is being used as a Person block
                tbFirstName.Visible = TargetPerson == null;
                tbLastName.Visible = TargetPerson == null;
                tbFollowUpFirstName.Visible = TargetPerson == null;
                tbFollowUpLastName.Visible = TargetPerson == null;

                tbFirstName.Text = rFilter.GetUserPreference( UserPreferenceKey.FirstName );
                tbLastName.Text = rFilter.GetUserPreference( UserPreferenceKey.LastName );
                tbFollowUpFirstName.Text = rFollowUpFilter.GetUserPreference( UserPreferenceKey.FirstNameFollowUp );
                tbFollowUpLastName.Text = rFollowUpFilter.GetUserPreference( UserPreferenceKey.LastNameFollowUp );

                var listData = new CareNeedService( rockContext ).Queryable( "PersonAlias,PersonAlias.Person" )
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

                ddlFollowUpSubmitter.DataSource = listData;
                ddlFollowUpSubmitter.DataTextField = "FullName";
                ddlFollowUpSubmitter.DataValueField = "PrimaryAliasId";
                ddlFollowUpSubmitter.DataBind();
                ddlFollowUpSubmitter.Items.Insert( 0, new ListItem() );
                ddlFollowUpSubmitter.SetValue( rFollowUpFilter.GetUserPreference( UserPreferenceKey.SubmittedByFollowUp ) );

                var categoryDefinedType = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_CATEGORY ) );
                dvpCategory.DefinedTypeId = categoryDefinedType.Id;
                string categoryValue = rFilter.GetUserPreference( UserPreferenceKey.Category );
                if ( !string.IsNullOrWhiteSpace( categoryValue ) )
                {
                    dvpCategory.SetValues( categoryValue.Split( ';' ).ToList() );
                }
                else
                {
                    dvpCategory.ClearSelection();
                }
                dvpFollowUpCategory.DefinedTypeId = categoryDefinedType.Id;
                string categoryValueFollowUp = rFollowUpFilter.GetUserPreference( UserPreferenceKey.CategoryFollowUp );
                if ( !string.IsNullOrWhiteSpace( categoryValueFollowUp ) )
                {
                    dvpFollowUpCategory.SetValues( categoryValueFollowUp.Split( ';' ).ToList() );
                }
                else
                {
                    dvpFollowUpCategory.ClearSelection();
                }

                var statusDefinedType = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_STATUS ) );
                dvpStatus.DefinedTypeId = statusDefinedType.Id;
                var statusValue = rFilter.GetUserPreference( UserPreferenceKey.Status );
                if ( string.IsNullOrWhiteSpace( statusValue ) && TargetPerson == null )
                {
                    statusValue = new DefinedValueService( rockContext ).Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_OPEN.AsGuid() ).Id.ToString();
                }
                dvpStatus.SetValues( statusValue.Split( ';' ).ToList() );

                cbAssignedToMe.Checked = rFilter.GetUserPreference( UserPreferenceKey.AssignedToMe ).AsBoolean();
                cbIncludeFutureNeeds.Checked = rFilter.GetUserPreference( UserPreferenceKey.IncludeScheduledNeeds ).AsBoolean();
                var followUpAssignedToMe = rFollowUpFilter.GetUserPreference( UserPreferenceKey.AssignedToMeFollowUp );
                if ( !string.IsNullOrWhiteSpace( followUpAssignedToMe ) )
                {
                    cbFollowUpAssignedToMe.Checked = followUpAssignedToMe.AsBoolean();
                }
                else
                {
                    cbFollowUpAssignedToMe.Checked = true;
                }
                cbAssignedToMe.Visible = cbFollowUpAssignedToMe.Visible = _canViewAll;

                var template = GetAttributeValue( AttributeKey.CategoriesTemplate );

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "Categories", categoryDefinedType.DefinedValues );
                mergeFields.Add( "Statuses", statusDefinedType.DefinedValues );
                lCategories.Text = template.ResolveMergeFields( mergeFields );

                if ( TargetPerson != null )
                {
                    lCategoriesHeader.Text = template.ResolveMergeFields( mergeFields );
                }
            }

            // set attribute filters
            BindAttributes();
            AddDynamicControls( refreshColumns );
        }

        /// <summary>
        /// Adds dynamic columns and filter controls for note templates and attributes.
        /// </summary>
        private void AddDynamicControls( bool addColumns = true )
        {
            if ( AvailableAttributes != null )
            {
                phAttributeFilters.Controls.Clear();
                phFollowUpAttributeFilters.Controls.Clear();
                foreach ( var attribute in AvailableAttributes )
                {
                    var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                    var controlFollowUp = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_followup_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
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
                    if ( controlFollowUp != null )
                    {
                        if ( controlFollowUp is IRockControl )
                        {
                            var rockControl = ( IRockControl ) controlFollowUp;
                            rockControl.Label = attribute.Name;
                            rockControl.Help = attribute.Description;
                            phFollowUpAttributeFilters.Controls.Add( controlFollowUp );
                        }
                        else
                        {
                            var wrapper = new RockControlWrapper();
                            wrapper.ID = controlFollowUp.ID + "_wrapper";
                            wrapper.Label = attribute.Name;
                            wrapper.Controls.Add( controlFollowUp );
                            phFollowUpAttributeFilters.Controls.Add( wrapper );
                        }

                        string savedValue = rFilter.GetUserPreference( "filter_followup_" + attribute.Key );
                        if ( !string.IsNullOrWhiteSpace( savedValue ) )
                        {
                            try
                            {
                                var values = JsonConvert.DeserializeObject<List<string>>( savedValue );
                                attribute.FieldType.Field.SetFilterValues( controlFollowUp, attribute.QualifierValues, values );
                            }
                            catch
                            {
                                // intentionally ignore
                            }
                        }
                    }

                    bool columnExists = gList.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id ) != null;
                    if ( !columnExists && addColumns )
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

                    bool followUpColumnExists = gFollowUp.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id ) != null;
                    if ( !followUpColumnExists && addColumns )
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

                        gFollowUp.Columns.Add( boundField );
                    }
                }
            }

            if ( addColumns )
            {
                var lStatus = new RockLiteralField();
                lStatus.ID = "NeedStatus";
                lStatus.HeaderStyle.CssClass = "grid-columnstatus";
                lStatus.ItemStyle.CssClass = "grid-columnstatus";
                lStatus.FooterStyle.CssClass = "grid-columnstatus";
                lStatus.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                lStatus.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                lStatus.DataBound += lStatus_DataBound;
                lStatus.SortExpression = "StatusValueId";
                gList.Columns.Add( lStatus );

                using ( var rockContext = new RockContext() )
                {

                    var noteTemplates = new NoteTemplateService( rockContext ).Queryable().AsNoTracking().Where( nt => nt.IsActive ).OrderBy( nt => nt.Order );
                    var firstCol = true;
                    rrblQuickNotes.Items.Clear();
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
                            gFollowUp.Columns.Add( btnNoteTemplate );

                            rrblQuickNotes.Items.Add( new ListItem( $"<i class='{template.Icon}'></i> {template.Note}", template.Id.ToString() ) );
                        }
                    }
                    rrblQuickNotes.Items.Add( new ListItem( "<i class='fa fa-question'></i> Other", "-1" ) );
                    rrblQuickNotes.SelectedValue = "-1";

                    var makeNoteField = new LinkButtonField();
                    makeNoteField.HeaderText = "Make Note";
                    makeNoteField.CssClass = "btn btn-primary btn-make-note btn-sm w-auto px-2";
                    makeNoteField.Text = "Make Note";
                    makeNoteField.Click += gMakeNote_Click;
                    makeNoteField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                    makeNoteField.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                    gList.Columns.Add( makeNoteField );
                    gFollowUp.Columns.Add( makeNoteField );

                    var actionTemplateField = new RockTemplateField();
                    actionTemplateField.HeaderText = "Actions";
                    actionTemplateField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                    actionTemplateField.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                    actionTemplateField.ID = "tfActions";
                    gList.Columns.Add( actionTemplateField );
                    gFollowUp.Columns.Add( actionTemplateField );

                    if ( _canAdministrate )
                    {
                        // Add delete column
                        var deleteField = new DeleteField();
                        gList.Columns.Add( deleteField );
                        deleteField.Click += gList_Delete;

                        // Add delete column
                        var deleteFieldFollowUp = new DeleteField();
                        gFollowUp.Columns.Add( deleteFieldFollowUp );
                        deleteFieldFollowUp.Click += gList_Delete;
                    }
                }
            }

            AddNotificationAttributeControl( false );
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

            // Status is hidden at the top in TargetPerson mode, no need to run queries.
            if ( TargetPerson == null )
            {
                var noteQry = noteService.GetByNoteTypeId( _careNeedNoteTypes.Any() ? _careNeedNoteTypes.FirstOrDefault().Id : 0 ).AsNoTracking();

                var outstandingCareStatuses = GetAttributeValues( AttributeKey.OutstandingCareNeedsStatuses );
                var totalCareNeeds = qry.Count();
                var outstandingCareNeeds = qry.Count( cn => outstandingCareStatuses.Contains( cn.Status.Guid.ToString() ) );

                var currentDateTime = RockDateTime.Now;
                var last7Days = currentDateTime.AddDays( -7 );
                var careTouches = noteQry.Count( n => n.CreatedDateTime >= last7Days && n.CreatedDateTime <= currentDateTime && ( n.Caption == null || !n.Caption.StartsWith( "Action" ) ) );

                lTouchesCount.Text = careTouches.ToString();
                lCareNeedsCount.Text = outstandingCareNeeds.ToString();
                lTotalNeedsCount.Text = totalCareNeeds.ToString();
            }

            qry = PermissionFilterQuery( qry );

            BindMainGrid( rockContext, careNeedService, qry );
            BindFollowUpGrid( rockContext, careNeedService, qry );
        }

        private void LoadTouchTemplates()
        {
            using ( RockContext rockContext = new RockContext() )
            {
                if ( _touchTemplates.Count < 1 )
                {
                    var categoryDefinedType = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_CATEGORY ) );
                    foreach ( var needCategory in categoryDefinedType.DefinedValues )
                    {
                        var matrixGuid = needCategory.GetAttributeValue( "CareTouchTemplates" ).AsGuid();
                        var touchTemplates = new List<TouchTemplate>();
                        if ( matrixGuid != Guid.Empty )
                        {
                            var matrix = new AttributeMatrixService( rockContext ).Get( matrixGuid );
                            if ( matrix != null )
                            {
                                foreach ( var matrixItem in matrix.AttributeMatrixItems )
                                {
                                    matrixItem.LoadAttributes();

                                    var noteTemplateGuid = matrixItem.GetAttributeValue( "NoteTemplate" ).AsGuid();
                                    var noteTemplate = new NoteTemplateService( rockContext ).Get( noteTemplateGuid );

                                    if ( noteTemplate != null )
                                    {
                                        var touchTemplate = new TouchTemplate();
                                        touchTemplate.NoteTemplate = noteTemplate;
                                        touchTemplate.MinimumCareTouches = matrixItem.GetAttributeValue( "MinimumCareTouches" ).AsInteger();
                                        touchTemplate.MinimumCareTouchHours = matrixItem.GetAttributeValue( "MinimumCareTouchHours" ).AsInteger();
                                        touchTemplate.NotifyAll = matrixItem.GetAttributeValue( "NotifyAllAssigned" ).AsBoolean();
                                        touchTemplate.Recurring = matrixItem.GetAttributeValue( "Recurring" ).AsBoolean();
                                        touchTemplate.Order = matrixItem.Order;

                                        touchTemplates.Add( touchTemplate );
                                    }
                                }
                                _touchTemplates.AddOrReplace( needCategory.Id, touchTemplates );
                            }
                        }
                    }
                }
            }
        }

        private void BindMainGrid( RockContext rockContext, CareNeedService careNeedService, IQueryable<CareNeed> qry )
        {
            if ( rockContext == null )
            {
                rockContext = new RockContext();
            }
            if ( careNeedService == null )
            {
                careNeedService = new CareNeedService( rockContext );
            }
            if ( qry == null )
            {
                qry = careNeedService.Queryable( "PersonAlias,PersonAlias.Person,SubmitterPersonAlias,SubmitterPersonAlias.Person" ).AsNoTracking();

                qry = PermissionFilterQuery( qry );
            }

            var futureDate = DateTime.Now.AddDays( GetAttributeValue( AttributeKey.FutureThresholdDays ).AsInteger() );

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

            if ( !cbIncludeFutureNeeds.Checked )
            {
                qry = qry.Where( b => b.DateEntered <= futureDate );
            }

            // Filter by Campus
            if ( cpCampus.SelectedCampusId.HasValue )
            {
                qry = qry.Where( b => b.CampusId == cpCampus.SelectedCampusId );
            }

            if ( TargetPerson != null )
            {
                var includeFamilyMembers = GetAttributeValue( AttributeKey.TargetModeIncludeFamilyMembers ).AsBoolean();
                if ( includeFamilyMembers )
                {
                    // show care needs for the target person and also for their family members
                    var qryFamilyMembers = TargetPerson.GetFamilyMembers( true, rockContext );
                    qry = qry.Where( a => a.PersonAliasId.HasValue && qryFamilyMembers.Any( b => b.PersonId == a.PersonAlias.PersonId ) );
                }
                else
                {
                    qry = qry.Where( a => a.PersonAliasId.HasValue && TargetPerson.PrimaryAlias.PersonId == a.PersonAlias.PersonId );
                }
            }
            else
            {
                // Filter by First Name
                string firstName = tbFirstName.Text;
                if ( !string.IsNullOrWhiteSpace( firstName ) )
                {
                    qry = qry.Where( b => b.PersonAlias.Person.FirstName.StartsWith( firstName ) || b.PersonAlias.Person.NickName.StartsWith( firstName ) );
                }

                // Filter by Last Name
                string lastName = tbLastName.Text;
                if ( !string.IsNullOrWhiteSpace( lastName ) )
                {
                    qry = qry.Where( b => b.PersonAlias.Person.LastName.StartsWith( lastName ) );
                }
            }

            // Filter by Submitter
            int? submitterPersonAliasId = ddlSubmitter.SelectedItem?.Value.AsIntegerOrNull();
            if ( submitterPersonAliasId != null )
            {
                qry = qry.Where( b => b.SubmitterAliasId == submitterPersonAliasId );
            }

            // Filter by Status
            List<int> statuses = dvpStatus.SelectedValuesAsInt;
            if ( statuses.Any() )
            {
                qry = qry.Where( cn => cn.StatusValueId != null && statuses.Contains( cn.StatusValueId.Value ) );
            }
            else if ( TargetPerson == null )
            {
                var followUpStatusId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_FOLLOWUP ).Id;
                qry = qry.Where( b => b.StatusValueId != followUpStatusId );
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

            // Filter query by any configured attribute filters
            if ( AvailableAttributes != null && AvailableAttributes.Any() )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                    qry = attribute.FieldType.Field.ApplyAttributeQueryFilter( qry, filterControl, attribute, careNeedService, Rock.Reporting.FilterMode.SimpleFilter );
                }
            }

            SortProperty sortProperty = gList.SortProperty;
            if ( sortProperty != null )
            {
                if ( sortProperty.Property.Contains( "AssignedPersons" ) )
                {
                    if ( sortProperty.Direction == SortDirection.Ascending )
                    {
                        qry = qry.OrderBy( a => a.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) )
                            .ThenBy( a => a.DateEntered )
                            .ThenBy( a => a.ParentNeedId )
                            .ThenBy( a => a.Id );
                    }
                    else
                    {
                        qry = qry.OrderByDescending( a => a.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) )
                            .ThenByDescending( a => a.DateEntered )
                            .ThenBy( a => a.ParentNeedId )
                            .ThenByDescending( a => a.Id );
                    }
                }
                else
                {
                    qry = qry.Sort( sortProperty );
                }
            }
            else
            {
                qry = qry.OrderByDescending( a => a.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) )
                    .ThenByDescending( a => a.DateEntered )
                    .ThenBy( a => a.ParentNeedId )
                    .ThenByDescending( a => a.Id );
            }

            var list = qry.ToList();
            AddChildNeedsToList( list );

            gList.DataSource = list;
            gList.DataBind();

            // Hide the campus column if the campus filter is not visible.
            gList.ColumnsOfType<RockBoundField>().First( c => c.DataField == "Campus.Name" ).Visible = cpCampus.Visible;

            var statusColumn = gList.ColumnsOfType<RockLiteralField>().First( c => c.ID.StartsWith( "NeedStatus" ) );
            if ( TargetPerson != null || statuses.Count != 1 )
            {
                statusColumn.HeaderText = "Status";
                statusColumn.SortExpression = "Status";
            }
            else
            {
                statusColumn.HeaderText = "";
                statusColumn.SortExpression = "";
            }
        }

        private void BindFollowUpGrid( RockContext rockContext, CareNeedService careNeedService, IQueryable<CareNeed> qry )
        {
            if ( TargetPerson != null )
            {
                // Follow up grid is hidden on Person Tab/Target Person mode.
                return;
            }
            if ( rockContext == null )
            {
                rockContext = new RockContext();
            }
            if ( careNeedService == null )
            {
                careNeedService = new CareNeedService( rockContext );
            }
            if ( qry == null )
            {
                qry = careNeedService.Queryable( "PersonAlias,PersonAlias.Person,SubmitterPersonAlias,SubmitterPersonAlias.Person" ).AsNoTracking();

                qry = PermissionFilterQuery( qry );
            }

            // Filter by Start Date
            DateTime? startDate = drpFollowUpDate.LowerValue;
            if ( startDate != null )
            {
                qry = qry.Where( b => b.DateEntered >= startDate );
            }

            // Filter by End Date
            DateTime? endDate = drpFollowUpDate.UpperValue;
            if ( endDate != null )
            {
                qry = qry.Where( b => b.DateEntered <= endDate );
            }

            // Filter by Campus
            if ( cpFollowUpCampus.SelectedCampusId.HasValue )
            {
                qry = qry.Where( b => b.CampusId == cpFollowUpCampus.SelectedCampusId );
            }

            if ( TargetPerson != null )
            {
                // show care needs for the target person and also for their family members
                var qryFamilyMembers = TargetPerson.GetFamilyMembers( true, rockContext );
                qry = qry.Where( a => a.PersonAliasId.HasValue && qryFamilyMembers.Any( b => b.PersonId == a.PersonAlias.PersonId ) );
            }
            else
            {
                // Filter by First Name
                string firstName = tbFollowUpFirstName.Text;
                if ( !string.IsNullOrWhiteSpace( firstName ) )
                {
                    qry = qry.Where( b => b.PersonAlias.Person.FirstName.StartsWith( firstName ) || b.PersonAlias.Person.NickName.StartsWith( firstName ) );
                }

                // Filter by Last Name
                string lastName = tbFollowUpLastName.Text;
                if ( !string.IsNullOrWhiteSpace( lastName ) )
                {
                    qry = qry.Where( b => b.PersonAlias.Person.LastName.StartsWith( lastName ) );
                }
            }

            // Filter by Submitter
            int? submitterPersonAliasId = ddlFollowUpSubmitter.SelectedItem.Value.AsIntegerOrNull();
            if ( submitterPersonAliasId != null )
            {
                qry = qry.Where( b => b.SubmitterAliasId == submitterPersonAliasId );
            }

            // Filter by Status
            var requestStatusValueId = new DefinedValueService( rockContext ).Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_FOLLOWUP.AsGuid() );
            if ( requestStatusValueId != null )
            {
                qry = qry.Where( b => b.StatusValueId == requestStatusValueId.Id );
            }

            // Filter by Category
            List<int> categories = dvpFollowUpCategory.SelectedValuesAsInt;
            if ( categories.Any() )
            {
                qry = qry.Where( cn => cn.CategoryValueId != null && categories.Contains( cn.CategoryValueId.Value ) );
            }

            // Filter by Assigned to Me
            if ( cbFollowUpAssignedToMe.Checked )
            {
                qry = qry.Where( cn => cn.AssignedPersons.Count( ap => ap.PersonAliasId == CurrentPersonAliasId ) > 0 );
            }

            // Filter query by any configured attribute filters
            if ( AvailableAttributes != null && AvailableAttributes.Any() )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phFollowUpAttributeFilters.FindControl( "filter_followup_" + attribute.Id.ToString() );
                    qry = attribute.FieldType.Field.ApplyAttributeQueryFilter( qry, filterControl, attribute, careNeedService, Rock.Reporting.FilterMode.SimpleFilter );
                }
            }

            SortProperty sortProperty = gFollowUp.SortProperty;
            if ( sortProperty != null )
            {
                if ( sortProperty.Property.Contains( "AssignedPersons" ) )
                {
                    if ( sortProperty.Direction == SortDirection.Ascending )
                    {
                        qry = qry.OrderBy( a => a.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) )
                            .ThenBy( a => a.DateEntered )
                            .ThenBy( a => a.Id );
                    }
                    else
                    {
                        qry = qry.OrderByDescending( a => a.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) )
                            .ThenByDescending( a => a.DateEntered )
                            .ThenByDescending( a => a.Id );
                    }
                }
                else
                {
                    qry = qry.Sort( sortProperty );
                }
            }
            else
            {
                qry = qry.OrderByDescending( a => a.DateEntered ).ThenByDescending( a => a.Id );
            }

            var list = qry.ToList();
            AddChildNeedsToList( list );

            gFollowUp.DataSource = list;
            gFollowUp.DataBind();

            // Hide the campus column if the campus filter is not visible.
            gFollowUp.ColumnsOfType<RockBoundField>().First( c => c.DataField == "Campus.Name" ).Visible = cpFollowUpCampus.Visible;
        }

        private IQueryable<CareNeed> PermissionFilterQuery( IQueryable<CareNeed> qry )
        {
            if ( !_canViewCareWorker )
            {
                qry = qry.Where( cn => !cn.WorkersOnly );
            }

            if ( !_canViewAll )
            {
                qry = qry.Where( cn => cn.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) && ( !cn.ParentNeedId.HasValue || ( cn.ParentNeedId.HasValue && !cn.ParentNeed.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) ) ) );
            }
            else
            {
                qry = qry.Where( cn => !cn.ParentNeedId.HasValue || cn.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) );
            }

            return qry;
        }

        private void AddChildNeedsToList( List<CareNeed> list )
        {
            var _savedChildNeeds = new Dictionary<CareNeed, IEnumerable<CareNeed>>();
            foreach ( var li in list )
            {
                if ( li.ChildNeeds != null && li.ChildNeeds.Any() )
                {
                    if ( _canViewAll )
                    {
                        _savedChildNeeds.Add( li, li.ChildNeeds.Where( cn => !cn.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) ) );
                    }
                    else
                    {
                        _savedChildNeeds.Add( li, li.ChildNeeds.Where( cn => cn.AssignedPersons.Any( ap => ap.PersonAliasId == CurrentPersonAliasId ) ) );
                    }
                }
            }
            foreach ( var di in _savedChildNeeds )
            {
                var index = list.IndexOf( di.Key );
                list.InsertRange( index + 1, di.Value );
            }
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

            cbAlert.Visible = noteOptions.ShowAlertCheckBox;
            cbAlert.Checked = false;
            cbPrivate.Visible = noteOptions.ShowPrivateCheckBox;
            cbPrivate.Checked = false;

            var noteEditor = ( NoteEditor ) notesTimeline.Controls[0];
            noteEditor.CssClass = "note-new-kfs";
            noteEditor.SaveButtonClick += mdMakeNote_SaveClick;
            noteEditor.Focus();
        }

        private bool createNote( RockContext rockContext, int entityId, string noteText, bool displayDialog = false, NoteTemplate noteTemplate = null, bool countsForTouch = false, bool isAlert = false, bool isPrivate = false )
        {
            var noteService = new NoteService( rockContext );
            var noteType = _careNeedNoteTypes.FirstOrDefault();
            var retVal = false;

            if ( noteType != null )
            {
                var note = new Note
                {
                    Id = 0,
                    IsSystem = false,
                    IsAlert = isAlert,
                    IsPrivateNote = isPrivate,
                    NoteTypeId = noteType.Id,
                    EntityId = entityId,
                    Text = noteText,
                    EditedByPersonAliasId = CurrentPersonAliasId,
                    EditedDateTime = RockDateTime.Now,
                    NoteUrl = this.RockBlock()?.CurrentPageReference?.BuildUrl(),
                    Caption = !countsForTouch ? $"Action - {CurrentPerson.FullName}" : string.Empty
                };
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
                if ( noteTemplate != null )
                {
                    note.CopyAttributesFrom( noteTemplate );

                    if ( countsForTouch )
                    {
                        note.ForeignGuid = noteTemplate.Guid;
                    }
                }

                if ( note.IsValid )
                {
                    noteService.Add( note );

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        note.SaveAttributeValues( rockContext );
                    } );

                    if ( displayDialog )
                    {
                        mdGridWarning.Show( "Note Saved: " + noteText, ModalAlertType.Information );
                    }
                    retVal = true;
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
            return retVal;
        }

        /// <summary>
        /// Gets the route from event arguments.
        /// </summary>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        private string GetRouteFromEventArgs( EventArgs eventArgs )
        {
            var commandArgs = eventArgs as CommandEventArgs;

            if ( commandArgs?.CommandName != "Route" || commandArgs.CommandArgument.ToStringSafe().IsNullOrWhiteSpace() )
            {
                return null;
            }

            return commandArgs.CommandArgument.ToString();
        }

        /// <summary>
        /// Loads the opportunities.
        /// </summary>
        private void LoadOpportunities()
        {
            var typeFilter = GetAttributeValue( AttributeKey.IncludeConnectionTypes ).SplitDelimitedValues().AsGuidList();
            rptConnnectionTypes.DataSource = new ConnectionTypeService( new RockContext() ).Queryable().Where( t => !typeFilter.Any() || typeFilter.Contains( t.Guid ) ).ToList();
            rptConnnectionTypes.DataBind();
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

        private void AddNotificationAttributeControl( bool setValues )
        {
            phNotificationAttribute.Controls.Clear();

            var attribute = AttributeCache.Get( rocks.kfs.StepsToCare.SystemGuid.PersonAttribute.NOTIFICATION );
            CurrentPerson.LoadAttributes();
            string attributeValue = CurrentPerson.GetAttributeValue( attribute.Key );
            attribute.AddControl( phNotificationAttribute.Controls, attributeValue, "Notification", setValues, true, true, "Choose how you would like to receive Steps to Care notifications", "" );

            if ( !CurrentPerson.CanReceiveEmail( false ) )
            {
                nbNotificationWarning.Text = "Please ensure your email address is setup properly on your profile. ";
                nbNotificationWarning.Visible = true;
            }
            if ( CurrentPerson.PhoneNumbers.GetFirstSmsNumber().IsNullOrWhiteSpace() )
            {
                var notificationTxt = "Please ensure you have a valid phone number attached to your profile and SMS is allowed. ";
                if ( !nbNotificationWarning.Text.Contains( notificationTxt ) )
                {
                    nbNotificationWarning.Text += notificationTxt;
                }
                nbNotificationWarning.Visible = true;
            }
        }
        private void CheckAndAddQuickNote()
        {
            var quickNoteId = PageParameter( PageParameterKey.QuickNote ).AsIntegerOrNull();
            var careNeedId = PageParameter( PageParameterKey.CareNeed ).AsIntegerOrNull();
            var noConfirm = PageParameter( PageParameterKey.NoConfirm ).AsBooleanOrNull();
            var shouldSnoozeNeed = GetAttributeValue( AttributeKey.MoveNeedToSnoozed ).AsBoolean();

            if ( quickNoteId.HasValue && careNeedId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var noteTemplate = new NoteTemplateService( rockContext ).Get( quickNoteId.Value );
                    var careNeed = new CareNeedService( rockContext ).Get( careNeedId.Value );
                    if ( noteTemplate != null && careNeed != null )
                    {
                        if ( noConfirm.HasValue && noConfirm.Value )
                        {
                            var noteCreated = createNote( rockContext, careNeedId.Value, noteTemplate.Note, true, noteTemplate, true );
                            if ( noteCreated )
                            {
                                if ( shouldSnoozeNeed )
                                {
                                    if ( careNeed.CustomFollowUp )
                                    {
                                        SnoozeNeed( careNeedId.Value );
                                    }
                                }
                                BindGrid();
                            }
                        }
                        else
                        {
                            hfQuickNote_CareNeedId.SetValue( careNeedId.Value );
                            hfQuickNote_NoteId.SetValue( noteTemplate.Id );
                            lQuickNote.Text = noteTemplate.Note;
                            lCareNeedId.Text = careNeedId.ToString();
                            mdConfirmNote.Show();
                        }
                    }
                    else if ( quickNoteId.Value == -1 && careNeed != null )
                    {
                        gMakeNote_Click( null, new RowEventArgs( 0, careNeed.Id ) );
                    }
                }

            }
        }

        private void SnoozeNeed( int id, DateTime? selectedDateTime = null )
        {
            using ( var rockContext = new RockContext() )
            {
                var snoozeChildNeeds = GetAttributeValue( AttributeKey.SnoozeChildNeeds ).AsBoolean();
                var careNeedService = new CareNeedService( rockContext );
                var careNeed = careNeedService.Get( id );
                var snoozeValueId = DefinedValueCache.Get( rocks.kfs.StepsToCare.SystemGuid.DefinedValue.CARE_NEED_STATUS_SNOOZED ).Id;
                if ( careNeed.StatusValueId != snoozeValueId && ( !careNeed.RenewMaxCount.HasValue || careNeed.RenewCurrentCount <= careNeed.RenewMaxCount.Value || selectedDateTime != null ) )
                {
                    careNeed.StatusValueId = snoozeValueId;
                    careNeed.SnoozeDate = RockDateTime.Now;
                    if ( selectedDateTime != null )
                    {
                        var dayDiff = ( selectedDateTime - RockDateTime.Now ).Value.TotalDays;
                        careNeed.RenewPeriodDays = Math.Ceiling( dayDiff ).ToIntSafe();
                        careNeed.RenewMaxCount = careNeed.RenewCurrentCount;
                    }

                    if ( snoozeChildNeeds && careNeed.ChildNeeds.Any() )
                    {
                        foreach ( var childneed in careNeed.ChildNeeds )
                        {
                            childneed.StatusValueId = snoozeValueId;
                            childneed.SnoozeDate = careNeed.SnoozeDate;
                            if ( selectedDateTime != null )
                            {
                                childneed.RenewPeriodDays = careNeed.RenewPeriodDays;
                                childneed.RenewMaxCount = childneed.RenewCurrentCount;
                            }
                        }

                    }
                    rockContext.SaveChanges();

                    createNote( rockContext, id, GetAttributeValue( AttributeKey.SnoozeActionText ) );

                    if ( snoozeChildNeeds && careNeed.ChildNeeds.Any() )
                    {
                        foreach ( var childneed in careNeed.ChildNeeds )
                        {
                            createNote( rockContext, childneed.Id, $"Marked {GetAttributeValue( AttributeKey.SnoozeActionText )} from Parent Need" );
                        }
                    }
                }
            }
        }

        private void SetupNoteDialog( int careNeedId, RockContext rockContext )
        {
            var careNeed = new CareNeedService( rockContext ).Get( careNeedId );

            if ( careNeed != null )
            {
                var careNeedNotesList = new NoteService( rockContext )
                    .GetByNoteTypeId( _careNeedNoteTypes.Any() ? _careNeedNoteTypes.FirstOrDefault().Id : 0 )
                    .AsNoTracking()
                    .Where( n => n.EntityId == careNeed.Id )
                    .ToList();
                var catTouchTemplates = _touchTemplates.GetValueOrDefault( careNeed.CategoryValueId.Value, new List<TouchTemplate>() );

                var lavaTemplate = GetAttributeValue( AttributeKey.QuickNoteStatusTemplate );

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "CareNeed", careNeed );
                mergeFields.Add( "TouchTemplates", catTouchTemplates );
                mergeFields.Add( "CareNeedNotes", careNeedNotesList.Where( n => n.Caption == null || ( n.Caption != null && !n.Caption.StartsWith( "Action" ) ) ) );
                mergeFields.Add( "CareNeedNotesWithActions", careNeedNotesList );
                mergeFields.Add( "MinimumCareTouches", GetAttributeValue( AttributeKey.MinimumCareTouches ).AsInteger() );
                mergeFields.Add( "MinimumCareTouchHours", GetAttributeValue( AttributeKey.MinimumCareTouchHours ).AsInteger() );
                mergeFields.Add( "MinimumFollowUpCareTouchHours", GetAttributeValue( AttributeKey.MinimumFollowUpTouchHours ).AsInteger() );
                lQuickNoteStatus.Text = lavaTemplate.ResolveMergeFields( mergeFields );

                SetupNoteTimeline( careNeed );
            }
        }

        private AvatarColors GetNoteTemplateColor( NoteTemplate noteTemplate )
        {
            var randomColor = new AvatarColors();
            if ( _noteTemplateColors.ContainsKey( noteTemplate.Id ) )
            {
                randomColor = _noteTemplateColors[noteTemplate.Id];
            }
            else
            {
                randomColor.GenerateMissingColors();
                _noteTemplateColors.AddOrReplace( noteTemplate.Id, randomColor );
            }
            return randomColor;
        }

        #endregion Methods
    }
}