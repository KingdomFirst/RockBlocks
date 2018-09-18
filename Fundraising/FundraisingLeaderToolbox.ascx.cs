using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Fundraising
{
    [DisplayName( "Fundraising Leader Toolbox KFS" )]
    [Category( "Fundraising" )]
    [Description( "The Leader Toolbox for a fundraising opportunity" )]

    [CodeEditorField( "Summary Lava Template", "Lava template for what to display at the top of the main panel. Usually used to display title and other details about the fundraising opportunity.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 100, false,
         @"
<h1>{{ Group | Attribute:'OpportunityTitle' }}</h1>
{% assign dateRangeParts = Group | Attribute:'OpportunityDateRange','RawValue' | Split:',' %}
{% assign dateRangePartsSize = dateRangeParts | Size %}
{% if dateRangePartsSize == 2 %}
    {{ dateRangeParts[0] | Date:'MMMM dd, yyyy' }} to {{ dateRangeParts[1] | Date:'MMMM dd, yyyy' }}<br/>
{% elsif dateRangePartsSize == 1  %}      
    {{ dateRangeParts[0] | Date:'MMMM dd, yyyy' }}
{% endif %}
{{ Group | Attribute:'OpportunityLocation' }}

<br />
<br />
<p>
{{ Group | Attribute:'OpportunitySummary' }}
</p>
", order: 1 )]

    [LinkedPage( "Participant Page", "The partipant page for a participant of this fundraising opportunity", required: false, order: 2 )]
    [LinkedPage( "Main Page", "The main page for the fundraising opportunity", required: false, order: 3 )]
    [BooleanField( "Show Member Funding Goal", "Determines if the Funding Goal of the Group Member should be displayed.", order: 4 )]
    [BooleanField( "Show Member Total Funding", "Determines if the Total Funding of the Group Member should be displayed.", order: 5 )]
    [BooleanField( "Show Member Funding Remaining", "Determines if the Funding Remaining of the Group Member should be displayed.", true, order: 6 )]
    [CustomDropdownListField( "Export Group Member Attributes", "Determines which Group Members Attributes should be included in the Excel export.", "0^None,1^All,2^Display In List", defaultValue: "-1", order: 7 )]
    [BooleanField( "Bypass Attribute Security", "Determines if the field level security on each attribute should be ignored.", order: 8 )]

    public partial class FundraisingLeaderToolbox : RockBlock
    {
        private Dictionary<string, object> _groupTotals = new Dictionary<string, object>();
        private int _gmAttributeExport = 0;

        /// <summary>
        /// Gets or sets the available attributes.
        /// </summary>
        /// <value>
        /// The available attributes.
        /// </value>
        public List<AttributeCache> AvailableAttributes { get; set; }

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _gmAttributeExport = GetAttributeValue( "ExportGroupMemberAttributes" ).AsInteger();

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            gGroupMembers.Actions.ShowBulkUpdate = false;
            gGroupMembers.Actions.ShowCommunicate = true;
            gGroupMembers.Actions.ShowMergePerson = false;
            gGroupMembers.Actions.ShowMergeTemplate = false;

            gGroupMembers.GridRebind += gGroupMembers_GridRebind;
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
                int? groupId = this.PageParameter( "GroupId" ).AsIntegerOrNull();

                if ( groupId.HasValue )
                {
                    ShowView( groupId.Value );
                }
                else
                {
                    pnlView.Visible = false;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the view.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        protected void ShowView( int groupId )
        {
            pnlView.Visible = true;
            hfGroupId.Value = groupId.ToString();
            var rockContext = new RockContext();

            var group = new GroupService( rockContext ).Get( groupId );
            if ( group == null )
            {
                pnlView.Visible = false;
                return;
            }

            // only show if the current person is a Leader in the Group
            if ( !group.Members.Any( a => a.PersonId == this.CurrentPersonId && a.GroupRole.IsLeader ) )
            {
                pnlView.Visible = false;
                return;
            }

            group.LoadAttributes( rockContext );
            var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson, new CommonMergeFieldsOptions { GetLegacyGlobalMergeFields = false } );
            mergeFields.Add( "Group", group );

            // Left Top Sidebar
            var photoGuid = group.GetAttributeValue( "OpportunityPhoto" );
            imgOpportunityPhoto.ImageUrl = string.Format( "~/GetImage.ashx?Guid={0}", photoGuid );

            // Top Main
            string summaryLavaTemplate = this.GetAttributeValue( "SummaryLavaTemplate" );

            BindGroupMembersGrid();

            mergeFields.Add( "GroupTotals", _groupTotals );
            lMainTopContentHtml.Text = summaryLavaTemplate.ResolveMergeFields( mergeFields );
        }

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void BindGroupMembersGrid()
        {
            var showGoal = GetAttributeValue( "ShowMemberFundingGoal" ).AsBoolean();
            var showTotal = GetAttributeValue( "ShowMemberTotalFunding" ).AsBoolean();
            var showRemaining = GetAttributeValue( "ShowMemberFundingRemaining" ).AsBoolean();

            gGroupMembers.Columns[4].Visible = showGoal;
            gGroupMembers.Columns[5].Visible = showTotal;
            gGroupMembers.Columns[6].Visible = showRemaining;

            var rockContext = new RockContext();

            int groupId = hfGroupId.Value.AsInteger();
            var totalFundraisingGoal = 0.00M;
            var totalContribution = 0.00M;
            var totalFundingRemaining = 0.00M;
            var groupMembersQuery = new GroupMemberService( rockContext ).Queryable().Where( a => a.GroupId == groupId );
            var group = new GroupService( rockContext ).Get( groupId );
            group.LoadAttributes( rockContext );
            var defaultIndividualFundRaisingGoal = group.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull();

            groupMembersQuery = groupMembersQuery.Sort( gGroupMembers.SortProperty ?? new SortProperty { Property = "Person.LastName, Person.NickName" } );

            var entityTypeIdGroupMember = EntityTypeCache.GetId<Rock.Model.GroupMember>();
            var entityTypeIdPerson = EntityTypeCache.GetId<Rock.Model.Person>();

            var groupMemberList = groupMembersQuery.ToList().Select( a =>
            {
                var groupMember = a;
                groupMember.LoadAttributes( rockContext );

                var contributionTotal = new FinancialTransactionDetailService( rockContext ).Queryable()
                            .Where( d => d.EntityTypeId == entityTypeIdGroupMember
                                    && d.EntityId == groupMember.Id )
                            .Sum( d => (decimal?)d.Amount ) ?? 0.00M;

                var individualFundraisingGoal = groupMember.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull();
                bool disablePublicContributionRequests = groupMember.GetAttributeValue( "DisablePublicContributionRequests" ).AsBoolean();
                if ( !individualFundraisingGoal.HasValue )
                {
                    individualFundraisingGoal = group.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull();
                }

                var fundingRemaining = individualFundraisingGoal - contributionTotal;
                if ( disablePublicContributionRequests || !showRemaining )
                {
                    fundingRemaining = null;
                }
                else if ( fundingRemaining < 0 )
                {
                    fundingRemaining = 0.00M;
                }

                totalFundraisingGoal += ( individualFundraisingGoal != null ? ( decimal ) individualFundraisingGoal : 0 );
                totalContribution += contributionTotal;
                totalFundingRemaining += ( fundingRemaining != null ? ( decimal ) fundingRemaining : 0 );

                if ( !showGoal )
                {
                    individualFundraisingGoal = null;
                    totalFundraisingGoal = 0.00M;
                }

                if ( !showTotal )
                {
                    contributionTotal = 0.00M;
                    totalContribution = 0.00M;
                }

                return new
                {
                    groupMember.Id,
                    PersonId = groupMember.PersonId,
                    DateTimeAdded = groupMember.DateTimeAdded,
                    groupMember.Person.FullName,
                    groupMember.Person.Gender,
                    FundingRemaining = fundingRemaining,
                    GroupRoleName = a.GroupRole.Name,
                    FundingGoal = individualFundraisingGoal,
                    TotalFunding = contributionTotal,
                    Email = groupMember.Person.Email
                };
            } ).ToList();

            _groupTotals.Add( "TotalFundraisingGoal", totalFundraisingGoal );
            _groupTotals.Add( "TotalContribution", totalContribution );
            _groupTotals.Add( "TotalFundingRemaining", totalFundingRemaining );
            
            //
            // Attributes
            //

            BindAttributes( group );
            AddDynamicControls( group );

            // Get all the person ids in current page of query results
            var personIds = groupMemberList
                .Select( m => m.PersonId )
                .Distinct()
                .ToList();

            // Get all the group member ids and the group id in current page of query results
            var groupMemberIds = new List<int>();
            foreach ( var groupMember in groupMemberList
                .Select( m => m ) )
            {
                groupMemberIds.Add( groupMember.Id );
            }

            var groupMemberAttributesIds = new List<int>();
            foreach (var gma in AvailableAttributes.Where(a => a.EntityTypeId == entityTypeIdGroupMember ))
            {
                groupMemberAttributesIds.Add( gma.Id );
            }

            var personAttributesIds = new List<int>();
            foreach ( var pa in AvailableAttributes.Where( a => a.EntityTypeId == entityTypeIdPerson ) )
            {
                personAttributesIds.Add( pa.Id );
            }

            // If there are any attributes that were selected to be displayed, we're going
            // to try and read all attribute values in one query and then put them into a
            // custom grid ObjectList property so that the AttributeField columns don't need
            // to do the LoadAttributes and querying of values for each row/column

            // Query the attribute values for all rows and attributes
            var attributeValues = new AttributeValueService( rockContext )
                .Queryable( "Attribute" ).AsNoTracking()
                .Where( v =>
                    v.EntityId.HasValue &&
                    (
                        (
                            personAttributesIds.Contains( v.AttributeId ) &&
                            personIds.Contains( v.EntityId.Value )
                        ) ||
                        (
                            groupMemberAttributesIds.Contains( v.AttributeId ) &&
                            groupMemberIds.Contains( v.EntityId.Value )
                        )
                    )
                )
                .ToList();

            // Get the attributes to add to each row's object
            BindAttributes( group );
            var attributes = new Dictionary<string, AttributeCache>();
            AvailableAttributes.ForEach( a => attributes
                    .Add( a.Id.ToString() + a.Key, a ) );

            // Initialize the grid's object list
            gGroupMembers.ObjectList = new Dictionary<string, object>();

            // Loop through each of the current page's registrants and build an attribute
            // field object for storing attributes and the values for each of the registrants
            foreach ( var gm in groupMemberList )
            {
                // Create a row attribute object
                var attributeFieldObject = new AttributeFieldObject();

                // Add the attributes to the attribute object
                attributeFieldObject.Attributes = attributes;

                // Add any person attribute values to object
                attributeValues
                    .Where( v =>
                        personAttributesIds.Contains( v.AttributeId ) &&
                        v.EntityId.Value == gm.PersonId )
                    .ToList()
                    .ForEach( v => attributeFieldObject.AttributeValues
                        .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );

                // Add any group member attribute values to object
                attributeValues
                    .Where( v =>
                        groupMemberAttributesIds.Contains( v.AttributeId ) &&
                        v.EntityId.Value == gm.Id )
                    .ToList()
                    .ForEach( v => attributeFieldObject.AttributeValues
                        .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );

                // Add row attribute object to grid's object list
                gGroupMembers.ObjectList.Add( gm.Id.ToString(), attributeFieldObject );
            }
            
            gGroupMembers.DataSource = groupMemberList;
            gGroupMembers.DataBind();
        }

        /// <summary>
        /// Binds the attributes.
        /// </summary>
        private void BindAttributes( Group _group )
        {
            var bypassSecurity = GetAttributeValue( "BypassAttributeSecurity" ).AsBoolean();

            // Parse the attribute filters 
            AvailableAttributes = new List<AttributeCache>();
            if ( _group != null && _gmAttributeExport > 0 )
            {
                // GROUP MEMBER ATTRIBUTES
                int gmEntityTypeId = new GroupMember().TypeId;
                string groupQualifier = _group.Id.ToString();
                string groupTypeQualifier = _group.GroupTypeId.ToString();
                foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                    .Where( a =>
                        a.EntityTypeId == gmEntityTypeId &&
                        ( a.IsGridColumn || _gmAttributeExport == 1 ) &&
                        ( ( a.EntityTypeQualifierColumn.Equals( "GroupId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupQualifier ) ) ||
                         ( a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupTypeQualifier ) ) ) )
                    .OrderByDescending( a => a.EntityTypeQualifierColumn )
                    .ThenBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    if ( attributeModel.IsAuthorized( Authorization.VIEW, CurrentPerson ) || bypassSecurity )
                    {
                        AvailableAttributes.Add( AttributeCache.Get( attributeModel ) );
                    }
                }

                // PERSON ATTRIBUTES
                var AvailablePersonAttributeIds = new List<int>();
                if ( _group.Attributes == null )
                {
                    _group.LoadAttributes();
                }

                if ( _group.Attributes != null )
                {
                    var attributeFieldTypeId = FieldTypeCache.Get( Rock.SystemGuid.FieldType.ATTRIBUTE ).Id;
                    var personAttributes = _group.Attributes.Values.FirstOrDefault( a => 
                                                a.FieldTypeId == attributeFieldTypeId &&
                                                a.QualifierValues.ContainsKey( "entitytype" ) &&
                                                a.QualifierValues.Values.Any( v => v.Value.Equals( Rock.SystemGuid.EntityType.PERSON, StringComparison.CurrentCultureIgnoreCase ) )
                                                );

                    if ( personAttributes != null )
                    {
                        var personAttributeValues = _group.GetAttributeValue( personAttributes.Key );

                        foreach ( var personAttribute in personAttributeValues.Split( ',' ).AsGuidList() )
                        {
                            AvailablePersonAttributeIds.Add( AttributeCache.Get( personAttribute ).Id );
                        }
                    }
                }
                int pEntityTypeId = new Person().TypeId;
                foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                    .Where( a =>
                        a.EntityTypeId == pEntityTypeId &&
                        AvailablePersonAttributeIds.Contains( a.Id )
                        )
                    .OrderByDescending( a => a.EntityTypeQualifierColumn )
                    .OrderBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    if ( attributeModel.IsAuthorized( Authorization.VIEW, CurrentPerson ) || bypassSecurity )
                    {
                        AvailableAttributes.Add( AttributeCache.Get( attributeModel ) );
                    }
                }
            }
        }

        /// <summary>
        /// Adds the attribute columns.
        /// </summary>
        private void AddDynamicControls( Group _group )
        {
            // Remove attribute columns
            foreach ( var column in gGroupMembers.Columns.OfType<AttributeField>().ToList() )
            {
                gGroupMembers.Columns.Remove( column );
            }

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    bool columnExists = gGroupMembers.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id ) != null;
                    if ( !columnExists )
                    {
                        AttributeField boundField = new AttributeField();
                        boundField.DataField = attribute.Id.ToString() + attribute.Key;
                        boundField.AttributeId = attribute.Id;
                        boundField.HeaderText = attribute.Name;
                        boundField.Visible = false;

                        var attributeCache = Rock.Web.Cache.AttributeCache.Get( attribute.Id );
                        if ( attributeCache != null )
                        {
                            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                        }

                        gGroupMembers.Columns.Add( boundField );
                    }
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the GridRebind event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        private void gGroupMembers_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGroupMembersGrid();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowView( hfGroupId.Value.AsInteger() );
        }

        /// <summary>
        /// Handles the Click event of the btnMainPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnMainPage_Click( object sender, EventArgs e )
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add( "GroupId", hfGroupId.Value );
            NavigateToLinkedPage( "MainPage", queryParams );
        }

        /// <summary>
        /// Handles the RowSelected event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroupMembers_RowSelected( object sender, RowEventArgs e )
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add( "GroupId", hfGroupId.Value );
            queryParams.Add( "GroupMemberId", e.RowKeyId.ToString() );
            NavigateToLinkedPage( "ParticipantPage", queryParams );
        }

        #endregion
    }
}