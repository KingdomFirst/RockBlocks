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
    [LinkedPage( "Detail Page" )]

    #endregion Block Settings

    public partial class CareDashboard : Rock.Web.UI.RockBlock
    {
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

        #endregion Private Members

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AvailableAttributes = ViewState["AvailableAttributes"] as List<AttributeCache>;

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
            ViewState["AvailableAttributes"] = AvailableAttributes;

            return base.SaveViewState();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
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

            // in case this is used as a Person Block, set the TargetPerson
            TargetPerson = ContextEntity<Person>();
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
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFilter.SaveUserPreference( "Start Date", "Start Date", drpDate.LowerValue.HasValue ? drpDate.LowerValue.Value.ToString( "o" ) : string.Empty );
            rFilter.SaveUserPreference( "End Date", "End Date", drpDate.UpperValue.HasValue ? drpDate.UpperValue.Value.ToString( "o" ) : string.Empty );
            rFilter.SaveUserPreference( "First Name", "First Name", tbFirstName.Text );
            rFilter.SaveUserPreference( "Last Name", "Last Name", tbLastName.Text );
            rFilter.SaveUserPreference( "Submitted By", "Submitted By", ddlSubmitter.SelectedItem.Value );
            rFilter.SaveUserPreference( "Category", "Category", dvpCategory.SelectedValues.AsDelimited( ";" ) );
            rFilter.SaveUserPreference( "Status", "Status", dvpStatus.SelectedItem.Value );
            rFilter.SaveUserPreference( "Campus", "Campus", cpCampus.SelectedCampusId.ToString() );

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
                case "Start Date":
                case "End Date":
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

                case "First Name":
                    return;

                case "Last Name":
                    return;

                case "Campus":
                    {
                        int? campusId = e.Value.AsIntegerOrNull();
                        if ( campusId.HasValue )
                        {
                            e.Value = CampusCache.Get( campusId.Value ).Name;
                        }
                        return;
                    }

                case "Submitted By":
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

                case "Category":
                    e.Value = ResolveValues( e.Value, dvpCategory );
                    return;

                case "Status":
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
                    Literal lName = e.Row.FindControl( "lName" ) as Literal;
                    if ( lName != null )
                    {
                        if ( careNeed.PersonAlias != null )
                        {
                            lName.Text = string.Format( "<a href=\"{0}\">{1}</a>", ResolveUrl( string.Format( "~/Person/{0}", careNeed.PersonAlias.PersonId ) ), careNeed.PersonAlias.Person.FullName ?? string.Empty );
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

            NavigateToLinkedPage( "DetailPage", qryParams );
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

        #endregion Events

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void SetFilter()
        {
            drpDate.LowerValue = rFilter.GetUserPreference( "Start Date" ).AsDateTime();
            drpDate.UpperValue = rFilter.GetUserPreference( "End Date" ).AsDateTime();

            cpCampus.Campuses = CampusCache.All();
            cpCampus.SelectedCampusId = rFilter.GetUserPreference( "Campus" ).AsInteger();

            // hide the First/Last name filter if this is being used as a Person block
            //tbFirstName.Visible = TargetPerson == null;
            //tbLastName.Visible = TargetPerson == null;

            tbFirstName.Text = rFilter.GetUserPreference( "First Name" );
            tbLastName.Text = rFilter.GetUserPreference( "Last Name" );

            var listData = new CareNeedService( new RockContext() ).Queryable( "Person" )
                .Where( cn => cn.SubmitterAliasId != null )
                .Select( cn => cn.SubmitterPersonAlias.Person )
                .Distinct()
                .ToList();
            ddlSubmitter.DataSource = listData;
            ddlSubmitter.DataTextField = "FullName";
            ddlSubmitter.DataValueField = "PrimaryAliasId";
            ddlSubmitter.DataBind();
            ddlSubmitter.Items.Insert( 0, new ListItem() );
            ddlSubmitter.SetValue( rFilter.GetUserPreference( "Submitted By" ) );

            dvpCategory.DefinedTypeId = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_CATEGORY ) ).Id;
            string categoryValue = rFilter.GetUserPreference( "Category" );
            if ( !string.IsNullOrWhiteSpace( categoryValue ) )
            {
                dvpCategory.SetValues( categoryValue.Split( ';' ).ToList() );
            }

            dvpStatus.DefinedTypeId = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_STATUS ) ).Id;
            dvpStatus.SetValue( rFilter.GetUserPreference( "Status" ) );

            // set attribute filters
            BindAttributes();
            AddDynamicControls();
        }

        private void AddDynamicControls()
        {
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
            var qry = careNeedService.Queryable( "PersonAlias,PersonAlias.Person,SubmitterPersonAlias,SubmitterPersonAlias.Person" ).AsNoTracking();

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
                qry = qry.Where( cn => cn.CategoryValueId != null && categories.Contains(  cn.CategoryValueId.Value ) );
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