// <copyright>
// Copyright 2019 by Kingdom First Solutions
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

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

using rocks.kfs.FinancialEdge;

namespace RockWeb.Plugins.rocks_kfs.FinancialEdge
{
    #region Block Attributes

    [DisplayName( "Financial Edge Batches to Journal" )]
    [Category( "KFS > Financial Edge" )]
    [Description( "Block used to create a CSV file that can be imported to Financial Edge from multiple Rock Financial Batches." )]

    #endregion

    #region Block Settings

    [LinkedPage( "Detail Page", "", true, "606BDA31-A8FE-473A-B3F8-A00ECF7E06EC", order: 0 )]
    [TextField( "Button Text", "The text to use in the Export Button.", false, "Create FE CSV", "", 1 )]
    [IntegerField( "Months Back", "The number of months back that batches should be loaded. This is helpful to prevent database timeouts if there are years of historical batches.", true, 2, "", 2 )]

    #endregion

    public partial class BatchesToJournal : RockBlock, ICustomGridColumns
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gfBatchesToExportFilter.ApplyFilterClick += gfBatchesToExportFilter_ApplyFilterClick;
            gfBatchesToExportFilter.ClearFilterClick += gfBatchesToExportFilter_ClearFilterClick;
            gfBatchesToExportFilter.DisplayFilterValue += gfBatchesToExportFilter_DisplayFilterValue;

            gBatchesToExport.GridRebind += gBatchesToExport_GridRebind;

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                BindFilter();
                BindGrid();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            string titleFilter = gfBatchesToExportFilter.GetUserPreference( "Title" );
            tbTitle.Text = !string.IsNullOrWhiteSpace( titleFilter ) ? titleFilter : string.Empty;

            string batchIdFilter = gfBatchesToExportFilter.GetUserPreference( "Batch Id" );
            tbBatchId.Text = !string.IsNullOrWhiteSpace( batchIdFilter ) ? batchIdFilter : string.Empty;

            ddlStatus.BindToEnum<BatchStatus>();
            ddlStatus.Items.Insert( 0, Rock.Constants.All.ListItem );
            string statusFilter = gfBatchesToExportFilter.GetUserPreference( "Status" );
            if ( string.IsNullOrWhiteSpace( statusFilter ) )
            {
                statusFilter = BatchStatus.Open.ConvertToInt().ToString();
            }

            ddlStatus.SetValue( statusFilter );

            var definedTypeTransactionTypes = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE.AsGuid() );
            dvpTransactionType.DefinedTypeId = definedTypeTransactionTypes.Id;
            dvpTransactionType.SetValue( gfBatchesToExportFilter.GetUserPreference( "Contains Transaction Type" ) );

            var campusi = CampusCache.All();
            campCampus.Campuses = campusi;
            campCampus.Visible = campusi.Any();
            campCampus.SetValue( gfBatchesToExportFilter.GetUserPreference( "Campus" ) );

            drpBatchDate.DelimitedValues = gfBatchesToExportFilter.GetUserPreference( "Date Range" );

            var definedTypeSourceTypes = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE.AsGuid() );
            dvpSourceType.DefinedTypeId = definedTypeSourceTypes.Id;
            dvpSourceType.SetValue( gfBatchesToExportFilter.GetUserPreference( "Contains Source Type" ) );
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            btnExportToFinancialEdge.Text = GetAttributeValue( "ButtonText" );
            var monthsBack = GetAttributeValue( "MonthsBack" ).AsInteger() * -1;
            var firstBatchDate = RockDateTime.Now.AddMonths( monthsBack );

            var batchIdList = new List<int>();
            var filteredBatchIdList = new List<int>();
            var batchesToExport = new List<BatchData>();

            using ( var rockContextBatches = new RockContext() )
            {
                var batchService = new FinancialBatchService( rockContextBatches );
                var qry = batchService
                    .Queryable().AsNoTracking()
                    .Where( b => b.BatchStartDateTime.HasValue )
                    .Where( b => b.BatchStartDateTime >= firstBatchDate )
                    .Where( b => b.ControlAmount == b.Transactions.Sum( t => t.TransactionDetails.Sum( d => d.Amount ) ) );

                string dateRangeValue = gfBatchesToExportFilter.GetUserPreference( "Date Range" );
                if ( !string.IsNullOrWhiteSpace( dateRangeValue ) )
                {
                    var drp = new DateRangePicker();
                    drp.DelimitedValues = dateRangeValue;
                    if ( drp.LowerValue.HasValue )
                    {
                        qry = qry.Where( b => b.BatchStartDateTime >= drp.LowerValue.Value );
                    }

                    if ( drp.UpperValue.HasValue )
                    {
                        var endOfDay = drp.UpperValue.Value.AddDays( 1 );
                        qry = qry.Where( b => b.BatchStartDateTime < endOfDay );
                    }
                }

                var status = gfBatchesToExportFilter.GetUserPreference( "Status" ).ConvertToEnumOrNull<BatchStatus>();
                if ( status.HasValue )
                {
                    qry = qry.Where( b => b.Status == status );
                }

                // filter by batches that contain transactions of the specified transaction type
                var transactionTypeValueId = gfBatchesToExportFilter.GetUserPreference( "Contains Transaction Type" ).AsIntegerOrNull();
                if ( transactionTypeValueId.HasValue )
                {
                    qry = qry.Where( a => a.Transactions.Any( t => t.TransactionTypeValueId == transactionTypeValueId.Value ) );
                }

                // filter by title
                string title = gfBatchesToExportFilter.GetUserPreference( "Title" );
                if ( !string.IsNullOrWhiteSpace( title ) )
                {
                    qry = qry.Where( batch => batch.Name.Contains( title ) );
                }

                // filter by batch id
                var batchId = gfBatchesToExportFilter.GetUserPreference( "Batch Id" ).AsIntegerOrNull();
                if ( batchId.HasValue )
                {
                    qry = qry.Where( batch => batch.Id == batchId.Value );
                }

                // filter by campus
                var campus = CampusCache.Get( gfBatchesToExportFilter.GetUserPreference( "Campus" ).AsInteger() );
                if ( campus != null )
                {
                    qry = qry.Where( b => b.CampusId == campus.Id );
                }

                // filter by batches that contain transactions of the specified source type
                var sourceTypeValueId = gfBatchesToExportFilter.GetUserPreference( "Contains Source Type" ).AsIntegerOrNull();
                if ( sourceTypeValueId.HasValue )
                {
                    qry = qry.Where( a => a.Transactions.Any( t => t.SourceTypeValueId == sourceTypeValueId.Value ) );
                }

                SortProperty sortProperty = gBatchesToExport.SortProperty;
                if ( sortProperty != null )
                {
                    qry = qry.Sort( sortProperty );
                }
                else
                {
                    qry = qry
                        .OrderByDescending( b => b.BatchStartDateTime )
                        .ThenBy( b => b.Name );
                }

                batchIdList = qry.Select( b => b.Id ).ToList();
            }

            using ( var rockContextAttributeValues = new RockContext() )
            {
                var dateExportedAttributeId = AttributeCache.Get( "16EFE0B4-E607-4960-BC92-8D66854E827A".AsGuid() ).Id;  //rocks.kfs.FinancialEdge.DateExported

                foreach ( var batchId in batchIdList )
                {
                    var attributeValue = new AttributeValueService( rockContextAttributeValues ).GetByAttributeIdAndEntityId( dateExportedAttributeId, batchId );
                    if ( attributeValue == null || attributeValue.ValueAsDateTime == null )
                    {
                        filteredBatchIdList.Add( batchId );
                    }
                }
            }

            using ( var rockContextBatchData = new RockContext() )
            {
                foreach ( var batchId in filteredBatchIdList )
                {
                    var batch = new FinancialBatchService( rockContextBatchData ).Get( batchId );

                    batchesToExport.Add( new BatchData
                    {
                        Id = batch.Id,
                        BatchStartDateTime = batch.BatchStartDateTime,
                        Name = batch.Name,
                        Status = batch.Status.ToString(),
                        Note = batch.Note,
                        Transactions = batch.Transactions.Count,
                        Total = batch.GetTotalTransactionAmount( rockContextBatchData )
                    } );
                }
            }

            gBatchesToExport.DataSource = batchesToExport;
            gBatchesToExport.ObjectList = ( ( List<BatchData> ) gBatchesToExport.DataSource ).ToDictionary( b => b.Id.ToString(), v => v as object );
            gBatchesToExport.EntityTypeId = EntityTypeCache.Get<FinancialBatch>().Id;

            gBatchesToExport.DataBind();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the GridRebind event of the gBatchesToExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        private void gBatchesToExport_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Gfs the batches to export filter display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void gfBatchesToExportFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Date Range":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }

                case "Status":
                    {
                        var status = e.Value.ConvertToEnumOrNull<BatchStatus>();
                        if ( status.HasValue )
                        {
                            e.Value = status.ConvertToString();
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }

                case "Contains Transaction Type":
                    {
                        var transactionTypeValueId = e.Value.AsIntegerOrNull();
                        if ( transactionTypeValueId.HasValue )
                        {
                            var transactionTypeValue = DefinedValueCache.Get( transactionTypeValueId.Value );
                            e.Value = transactionTypeValue != null ? transactionTypeValue.ToString() : string.Empty;
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }

                case "Campus":
                    {
                        var campus = CampusCache.Get( e.Value.AsInteger() );
                        if ( campus != null )
                        {
                            e.Value = campus.Name;
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }
                case "Contains Source Type":
                    {
                        var sourceTypeValueId = e.Value.AsIntegerOrNull();
                        if ( sourceTypeValueId.HasValue )
                        {
                            e.Value = DefinedValueCache.GetValue( sourceTypeValueId.Value );
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfBatchesToExportFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchesToExportFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfBatchesToExportFilter.SaveUserPreference( "Status", ddlStatus.SelectedValue );
            gfBatchesToExportFilter.SaveUserPreference( "Date Range", drpBatchDate.DelimitedValues );
            gfBatchesToExportFilter.SaveUserPreference( "Title", tbTitle.Text );
            gfBatchesToExportFilter.SaveUserPreference( "Campus", campCampus.SelectedValue );
            gfBatchesToExportFilter.SaveUserPreference( "Contains Transaction Type", dvpTransactionType.SelectedValue );
            gfBatchesToExportFilter.SaveUserPreference( "Contains Source Type", dvpSourceType.SelectedValue );
            gfBatchesToExportFilter.SaveUserPreference( "Batch Id", tbBatchId.Text );

            BindGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfBatchesToExportFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchesToExportFilter_ClearFilterClick( object sender, EventArgs e )
        {
            gfBatchesToExportFilter.DeleteUserPreferences();
            gfBatchesToExportFilter.SaveUserPreference( "Status", BatchStatus.Open.ConvertToInt().ToString() );
            BindFilter();
            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the gBatchesToExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gBatchesToExport_Click( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "batchId", e.RowKeyId );
        }

        /// <summary>
        /// Handles the Click event of the btnExportToFinancialEdge control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnExportToFinancialEdge_Click( object sender, EventArgs e )
        {
            var selectedBatches = new List<int>();

            gBatchesToExport.SelectedKeys.ToList().ForEach( b => selectedBatches.Add( b.ToString().AsInteger() ) );

            if ( selectedBatches.Any() )
            {
                var feJournal = new FEJournal();
                var items = new List<JournalEntryLine>();

                var rockContext = new RockContext();
                var batchService = new FinancialBatchService( rockContext );
                var batchesToUpdate = new List<FinancialBatch>();

                var exportedBatches = batchService.Queryable()
                    .WhereAttributeValue( rockContext, a => a.Attribute.Key == "rocks.kfs.FinancialEdge.DateExported" && ( a.Value != null && a.Value != "" ) )
                    .Select( b => b.Id )
                    .ToList();

                batchesToUpdate = batchService.Queryable()
                    .Where( b =>
                        selectedBatches.Contains( b.Id ) &&
                        !exportedBatches.Contains( b.Id ) )
                    .ToList();

                foreach ( var batch in batchesToUpdate )
                {
                    var changes = new History.HistoryChangeList();
                    History.EvaluateChange( changes, "Status", batch.Status, BatchStatus.Closed );

                    string errorMessage;

                    if ( batch.IsAutomated && batch.Status == BatchStatus.Pending )
                    {
                        errorMessage = string.Format( "{0} is an automated batch and the status can not be modified when the status is pending. The system will automatically set this batch to OPEN when all transactions have been downloaded.", batch.Name );
                        maWarningDialog.Show( errorMessage, ModalAlertType.Warning );
                        return;
                    }

                    batch.Status = BatchStatus.Closed;

                    if ( !batch.IsValid )
                    {
                        string message = string.Format( "Unable to update status for the selected batches.<br/><br/>{0}", batch.ValidationResults.AsDelimited( "<br/>" ) );
                        maWarningDialog.Show( message, ModalAlertType.Warning );
                        return;
                    }

                    batch.LoadAttributes();

                    var newDate = string.Empty;

                    var oldDate = batch.GetAttributeValue( "rocks.kfs.FinancialEdge.DateExported" );
                    newDate = RockDateTime.Now.ToString();
                    History.EvaluateChange( changes, "Date Exported", oldDate, newDate.ToString() );

                    items.AddRange( feJournal.GetGlEntries( rockContext, batch, tbJournalType.Text ) );

                    HistoryService.SaveChanges(
                        rockContext,
                        typeof( FinancialBatch ),
                        Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                        batch.Id,
                        changes,
                        false );

                    batch.SetAttributeValue( "rocks.kfs.FinancialEdge.DateExported", newDate );
                    batch.SaveAttributeValue( "rocks.kfs.FinancialEdge.DateExported", rockContext );
                }

                rockContext.SaveChanges();

                feJournal.SetFinancialEdgeSessions( items, RockDateTime.Now.ToString( "yyyyMMdd_HHmmss" ) );

                NavigateToPage( this.RockPage.Guid, new Dictionary<string, string>() );
            }
            else
            {
                nbError.Text = string.Format( "There were not any batches selected." );
                nbError.NotificationBoxType = NotificationBoxType.Warning;
                nbError.Visible = true;
            }
        }

        #endregion

        #region Classes

        public class BatchData
        {
            public int Id { get; set; }
            public DateTime? BatchStartDateTime { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public string Note { get; set; }
            public int Transactions { get; set; }
            public decimal Total { get; set; }
        }

        #endregion
    }
}
