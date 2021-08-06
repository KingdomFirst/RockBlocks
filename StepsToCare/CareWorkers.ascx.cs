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
using Rock.Web.UI.Controls;
using rocks.kfs.StepsToCare.Model;

namespace RockWeb.Plugins.rocks_kfs.StepsToCare
{
    #region Block Attributes

    [DisplayName( "Care Workers" )]
    [Category( "KFS > Steps To Care" )]
    [Description( "Care workers block for KFS Steps to Care package. Used for adding and editing care workers for assignment." )]

    #endregion

    #region Block Settings

    #endregion

    public partial class CareWorkers : Rock.Web.UI.RockBlock
    {

        /// <summary>
        /// User Preference Key
        /// </summary>
        private static class UserPreferenceKey
        {
            public const string Category = "Category";
            public const string Campus = "Campus";
        }

        /// <summary>
        /// View State Keys
        /// </summary>
        private static class ViewStateKey
        {
            public const string AvailableAttributes = "AvailableAttributes";
        }

        #region Properties

        public List<AttributeCache> AvailableAttributes { get; set; }

        private bool _canAddEditDelete = false;

        #endregion Properties

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

            gList.GridRebind += gList_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlCareWorkers );
            rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;

            _canAddEditDelete = IsUserAuthorized( Authorization.EDIT );

            gList.GridRebind += gList_GridRebind;
            //gList.RowDataBound += gList_RowDataBound;
            gList.DataKeyNames = new string[] { "Id" };
            gList.Actions.ShowAdd = _canAddEditDelete;
            gList.Actions.AddClick += gList_AddClick;
            gList.IsDeleteEnabled = _canAddEditDelete;
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
            else
            {
                var rockContext = new RockContext();
                CareWorker item = new CareWorkerService( rockContext ).Get( hfCareWorkerId.ValueAsInt() );
                if ( item == null )
                {
                    item = new CareWorker();
                }
                item.LoadAttributes();

                phAttributes.Controls.Clear();
                Helper.AddEditControls( item, phAttributes, false, BlockValidationGroup, 2 );
            }
        }

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

        #endregion

        #region Events

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
        /// Handles the SaveClick event of the mdAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdAddPerson_SaveClick( object sender, EventArgs e )
        {
            if ( !AddWorker( hfCareWorkerId.Value.AsInteger(), ppNewPerson.SelectedValue.Value, dvpCategory.SelectedDefinedValueId ) )
            {
                nbAddPersonExists.Visible = true;
                return;
            }

            mdAddPerson.Hide();
            BindGrid();
        }

        /// <summary>
        /// Handles the SaveThenAddClick event of the mdAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdAddPerson_SaveThenAddClick( object sender, EventArgs e )
        {
            if ( !AddWorker( hfCareWorkerId.Value.AsInteger(), ppNewPerson.SelectedValue.Value, dvpCategory.SelectedDefinedValueId ) )
            {
                nbAddPersonExists.Visible = true;
                return;
            }

            BindGrid();
            ppNewPerson.SetValue( null );
            ShowDetail( 0 );
            nbAddPersonExists.Visible = false;
        }

        /// <summary>
        /// Handles the SelectPerson event of the ppPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( ppNewPerson.PersonId != null )
            {
                Person person = new PersonService( new RockContext() ).Get( ppNewPerson.PersonId.Value );
                if ( person != null )
                {
                    int? workerId = PageParameter( "CareWorkerId" ).AsIntegerOrNull();

                    if ( !cpCampus.SelectedCampusId.HasValue && ( e != null || ( workerId.HasValue && workerId == 0 ) ) )
                    {
                        var personCampus = person.GetCampus();
                        cpCampus.SelectedCampusId = personCampus != null ? personCampus.Id : ( int? ) null;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFilter.SaveUserPreference( UserPreferenceKey.Category, "Category", dvpCategory.SelectedDefinedValueId.ToString() );
            rFilter.SaveUserPreference( UserPreferenceKey.Campus, "Campus", cpCampus.SelectedCampusId.ToString() );

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
                case UserPreferenceKey.Campus:
                    {
                        int? campusId = e.Value.AsIntegerOrNull();
                        if ( campusId.HasValue )
                        {
                            e.Value = CampusCache.Get( campusId.Value ).Name;
                        }
                        return;
                    }

                case UserPreferenceKey.Category:
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
        /// Handles the RowDataBound event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.GridViewRowEventArgs"/> instance containing the event data.</param>
        public void gList_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
        }

        /// <summary>
        /// Handles the AddClick event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gList_AddClick( object sender, EventArgs e )
        {
            ShowDetail( 0 );
            nbAddPersonExists.Visible = false;
            mdAddPerson.Show();
        }

        /// <summary>
        /// Handles the Edit event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gList_Edit( object sender, RowEventArgs e )
        {
            ShowDetail( e.RowKeyId );
            nbAddPersonExists.Visible = false;
            mdAddPerson.Show();
        }

        /// <summary>
        /// Handles the Delete event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gList_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            CareWorkerService service = new CareWorkerService( rockContext );
            CareWorker careWorker = service.Get( e.RowKeyId );
            if ( careWorker != null )
            {
                service.Delete( careWorker );
                rockContext.SaveChanges();
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

        #endregion

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void SetFilter()
        {
            cpCampus.Campuses = CampusCache.All();
            cpCampus.SelectedCampusId = rFilter.GetUserPreference( UserPreferenceKey.Campus ).AsInteger();

            dvpFilterCategory.DefinedTypeId = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_CATEGORY ) ).Id;
            string categoryValue = rFilter.GetUserPreference( UserPreferenceKey.Category );
            if ( !string.IsNullOrWhiteSpace( categoryValue ) )
            {
                dvpFilterCategory.SetValue( rFilter.GetUserPreference( UserPreferenceKey.Category ) );
            }

            // set attribute filters
            BindAttributes();
            AddDynamicControls();
        }

        private void BindGrid()
        {
            rFilter.Visible = true;
            gList.Visible = true;
            RockContext rockContext = new RockContext();
            CareWorkerService careWorkerService = new CareWorkerService( rockContext );
            var qry = careWorkerService.Queryable( "PersonAlias,PersonAlias.Person" ).AsNoTracking();

            // Filter by Campus
            if ( cpCampus.SelectedCampusId.HasValue )
            {
                qry = qry.Where( b => b.CampusId == cpCampus.SelectedCampusId );
            }

            // Filter by Category
            var categoryValueId = dvpFilterCategory.SelectedDefinedValueId;
            if ( categoryValueId != null )
            {
                qry = qry.Where( b => b.CategoryValueId == categoryValueId );
            }

            SortProperty sortProperty = gList.SortProperty;
            if ( sortProperty != null )
            {
                qry = qry.Sort( sortProperty );
            }
            else
            {
                qry = qry.OrderBy( cw => cw.PersonAlias.Person.LastName ).ThenBy( cw => cw.PersonAlias.Person.FirstName ).ThenByDescending( cw => cw.Id );
            }

            // Filter query by any configured attribute filters
            if ( AvailableAttributes != null && AvailableAttributes.Any() )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                    qry = attribute.FieldType.Field.ApplyAttributeQueryFilter( qry, filterControl, attribute, careWorkerService, Rock.Reporting.FilterMode.SimpleFilter );
                }
            }

            var list = qry.ToList();

            gList.DataSource = list;
            gList.DataBind();
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="careWorkerId">The care worker identifier</param>
        public void ShowDetail( int careWorkerId )
        {
            CareWorker careWorker = null;
            var rockContext = new RockContext();
            CareWorkerService careWorkerService = new CareWorkerService( rockContext );
            if ( !careWorkerId.Equals( 0 ) )
            {
                careWorker = careWorkerService.Get( careWorkerId );
                pdAuditDetails.SetEntity( careWorker, ResolveRockUrl( "~" ) );
            }

            if ( careWorker == null )
            {
                careWorker = new CareWorker { Id = 0 };
                pdAuditDetails.Visible = false;
            }

            if ( careWorker.Campus != null )
            {
                cpCampus.SelectedCampusId = careWorker.CampusId;
            }
            else
            {
                cpCampus.SelectedIndex = 0;
            }

            if ( careWorker.PersonAlias != null )
            {
                ppNewPerson.SetValue( careWorker.PersonAlias.Person );
            }
            else
            {
                ppNewPerson.SetValue( null );
            }

            dvpCategory.DefinedTypeId = DefinedTypeCache.Get( new Guid( rocks.kfs.StepsToCare.SystemGuid.DefinedType.CARE_NEED_CATEGORY ) ).Id;

            if ( careWorker.CategoryValueId != null )
            {
                dvpCategory.SetValue( careWorker.CategoryValueId );
            }
            else
            {
                dvpCategory.SelectedIndex = 0;
            }

            careWorker.LoadAttributes();
            Helper.AddEditControls( careWorker, phAttributes, true, BlockValidationGroup, 2 );

            ppPerson_SelectPerson( null, null );

            hfCareWorkerId.Value = careWorker.Id.ToString();
        }

        /// <summary>
        /// Adds the New Worker
        /// </summary>
        private bool AddWorker( int careWorkerId, int personId, int? categoryId )
        {
            if ( Page.IsValid )
            {
                RockContext rockContext = new RockContext();
                CareWorkerService careWorkerService = new CareWorkerService( rockContext );

                CareWorker careWorker = null;

                if ( !careWorkerId.Equals( 0 ) )
                {
                    careWorker = careWorkerService.Get( careWorkerId );
                }
                else
                {
                    careWorker = careWorkerService.Get( categoryId, personId );
                    if ( careWorker != null )
                    {
                        return false;
                    }
                }

                if ( careWorker == null )
                {
                    careWorker = new CareWorker { Id = 0 };
                }

                careWorker.CampusId = cpCampus.SelectedCampusId;

                careWorker.PersonAliasId = ppNewPerson.PersonAliasId;

                careWorker.CategoryValueId = dvpCategory.SelectedValue.AsIntegerOrNull();

                if ( careWorker.IsValid )
                {
                    if ( careWorker.Id.Equals( 0 ) )
                    {
                        careWorkerService.Add( careWorker );
                    }

                    // get attributes
                    careWorker.LoadAttributes();
                    Helper.GetEditValues( phAttributes, careWorker );

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        careWorker.SaveAttributeValues( rockContext );
                    } );

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
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
        #endregion
    }
}
