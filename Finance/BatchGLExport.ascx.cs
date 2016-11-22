
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kingdomfirstsolutions.Finance
{
    [DisplayName( "Batch GL Export" )]
    [Category( "KFS > Finance" )]
    [Description( "Lists all financial batches and provides GL Export capability" )]
    [LinkedPage( "Detail Page", order: 0 )]
    [BooleanField( "Show Accounting Code", "Should the accounting code column be displayed.", false, "", 1 )]
    public partial class BatchGLExport : Rock.Web.UI.RockBlock
    {
        #region Fields

        private RockDropDownList ddlAction;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            gfBatchFilter.ApplyFilterClick += gfBatchFilter_ApplyFilterClick;
            gfBatchFilter.ClearFilterClick += gfBatchFilter_ClearFilterClick;
            gfBatchFilter.DisplayFilterValue += gfBatchFilter_DisplayFilterValue;

            gBatchList.DataKeyNames = new string[] { "Id" };
            gBatchList.Actions.ShowExcelExport = false;
            gBatchList.Actions.ShowMergeTemplate = false;
            gBatchList.GridRebind += gBatchList_GridRebind;
            gBatchList.RowDataBound += gBatchList_RowDataBound;

            dpExportDate.SelectedDate = RockDateTime.Today;
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfBatchFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchFilter_ClearFilterClick( object sender, EventArgs e )
        {
            gfBatchFilter.DeleteUserPreferences();
            BindFilter();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            nbResult.Visible = false;

            if ( !Page.IsPostBack )
            {
                SetVisibilityOption();
                BindFilter();
                BindGrid();
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
            SetVisibilityOption();
            BindGrid();
        }

        /// <summary>
        /// Handles the DisplayFilterValue event of the gfBatchFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs"/> instance containing the event data.</param>
        protected void gfBatchFilter_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Row Limit":
                    {
                        // row limit filter was removed, so hide it just in case
                        e.Value = null;
                        break;
                    }

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
                            var transactionTypeValue = DefinedValueCache.Read( transactionTypeValueId.Value );
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
                        var campus = CampusCache.Read( e.Value.AsInteger() );
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
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfBatchFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfBatchFilter.SaveUserPreference( "Date Range", drpBatchDate.DelimitedValues );
            gfBatchFilter.SaveUserPreference( "Title", tbTitle.Text );
            if ( tbAccountingCode.Visible )
            {
                gfBatchFilter.SaveUserPreference( "Accounting Code", tbAccountingCode.Text );
            }

            gfBatchFilter.SaveUserPreference( "Status", ddlStatus.SelectedValue );
            gfBatchFilter.SaveUserPreference( "Campus", campCampus.SelectedValue );
            gfBatchFilter.SaveUserPreference( "Contains Transaction Type", ddlTransactionType.SelectedValue );

            BindGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gBatchList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gBatchList_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var batchRow = e.Row.DataItem as BatchRow;

                if ( batchRow != null )
                {
                    if ( batchRow.TransactionCount > 0 )
                    {
                        e.Row.AddCssClass( "js-has-transactions" );
                    }

                }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gBatchList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gBatchList_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid( e.IsExporting );
        }

        public void btnExport_Click ( object sender, EventArgs e)
        {
            var batchesSelected = new List<int>();

            gBatchList.SelectedKeys.ToList().ForEach( b => batchesSelected.Add( b.ToString().AsInteger() ) );

            if ( batchesSelected.Any() )
            {
                string output = String.Empty;
                var rockContext = new RockContext();
                var batchService = new FinancialBatchService( rockContext );
                var batchesToUpdate = batchService.Queryable()
                    .Where( b => batchesSelected.Contains( b.Id ) )
                    .ToList();

                var batchesToUpdateDetail = batchService.Queryable()
                     .Where( b => batchesSelected.Contains( b.Id ) )
                     .Select( b => new {
                         Id = b.Id,
                         Name = b.Name,
                         AccountingSystemCode = b.AccountingSystemCode,
                         TransactionCount = b.Transactions.Count(),
                         TransactionAmount = b.Transactions.Sum( t => ( decimal? ) ( t.TransactionDetails.Sum( d => ( decimal? ) d.Amount ) ?? 0.0M ) ) ?? 0.0M,
                         ControlAmount = b.ControlAmount,
                         AccountSummaryList = b.Transactions
                             .SelectMany( t => t.TransactionDetails )
                             .OrderBy( s => s.Account.Order )
                             .ToList()
                     } )
                     .ToList();

                List<GLTransaction> batchTransactions = new List<GLTransaction>();

                foreach ( var batch in batchesToUpdate )
                {
                    batch.LoadAttributes();
                    foreach ( var transaction in batch.Transactions )
                    {
                        transaction.LoadAttributes();
                        foreach (var transactionDetail in transaction.TransactionDetails)
                        {
                            transactionDetail.Account.LoadAttributes();

                            GLTransaction transactionItem = new GLTransaction();
                            transactionItem.glCompany = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_Company" );
                            transactionItem.glFund = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_Fund" );
                            transactionItem.glBankAccount = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_BankAccount" );
                            transactionItem.glRevenueAccount = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_RevenueAccount" );
                            transactionItem.glRevenueDepartment = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_RevenueDepartment" );
                            transactionItem.projectCode = transaction.GetAttributeValue( "Project" );
                            transactionItem.total = transactionDetail.Amount;

                            transactionItem.batch = batch;
                            transactionItem.transaction = transaction;
                            transactionItem.transactionDetail = transactionDetail;

                            batchTransactions.Add( transactionItem );

                        }
                    }
                }

                var groupedTransactions = batchTransactions.GroupBy( d => new { d.glBankAccount, d.glCompany, d.glFund, d.glRevenueAccount, d.glRevenueDepartment, d.projectCode } )
                    .Select( t => new GLTransaction
                    {
                        glBankAccount = t.Key.glBankAccount,
                        glCompany = t.Key.glCompany,
                        glFund = t.Key.glFund,
                        glRevenueAccount = t.Key.glRevenueAccount,
                        glRevenueDepartment = t.Key.glRevenueDepartment,
                        projectCode = t.Key.projectCode,
                        total = t.Sum( f => ( decimal? ) f.total ) ?? 0.0M
                    } )
                    .ToList();

                List<GLExportLineItem> items = GenerateLineItems( batchTransactions, "Contributions", ddlJournalType.SelectedValue, tbAccountingPeriod.Text, dpExportDate.SelectedDate );

                StringBuilder stringBuilder = new StringBuilder();
                int num = 0;
                foreach ( GLExportLineItem lineitem in items )
                {
                    if ( num > 0 )
                    {
                        stringBuilder.Append( Environment.NewLine );
                    }
                    stringBuilder.Append( convertGLItemToStr( lineitem ) );
                    num++;
                }
                if (items.Count > 0 && nbResult.NotificationBoxType  != NotificationBoxType.Warning)
                {
                    output = stringBuilder.ToString();

                    MemoryStream ms = new MemoryStream( Encoding.ASCII.GetBytes( output ) );
                    Response.ClearContent();
                    Response.ClearHeaders();
                    Response.ContentType = "application/text";
                    Response.AddHeader( "Content-Disposition", "attachment; filename=GLTRN2000.txt" );
                    ms.WriteTo( Response.OutputStream );
                    Response.End();
                } 
            }
            else
            {
                nbResult.Text = string.Format( "There were not any batches selected." );
                nbResult.NotificationBoxType = NotificationBoxType.Warning;
                nbResult.Visible = true;
            }

        }

        private string convertGLItemToStr( GLExportLineItem item )
        {
            string[] str = new string[] { "", "".PadLeft( 5, '0' ), null, null, null, null, null, null, null, null };
            string[] strArrays = new string[] { item.CompanyNumber.ToString().PadLeft( 3, '0' ), item.FundNumber.ToString().PadLeft( 3, '0' ), item.AccountingPeriod.ToString().PadLeft( 2, '0' ), item.JournalType, null };
            int journalNumber = item.JournalNumber;
            strArrays[4] = journalNumber.ToString().PadLeft( 5, '0' );
            str[2] = string.Concat( strArrays );
            str[3] = "".PadLeft( 3, '0' );
            str[4] = item.Date.HasValue ? item.Date.Value.ToString( "MMddyy" ) : "<not available>";
            str[5] = item.Description1.Substring( 0, ( item.Description1.Length <= 30 ? item.Description1.Length : 30 ) );
            str[6] = item.Description1.Substring( 0, ( item.Description2.Length <= 30 ? item.Description2.Length : 30 ) );
            str[7] = string.Concat( item.DepartmentNumber.ToString().PadLeft( 3, '0' ), item.AccountNumber.ToString().PadLeft( 9, '0' ) );
            decimal amount = item.Amount;
            str[8] = amount.ToString( "0.00" ).Replace( ".", "" );
            str[9] = item.ProjectCode;
            StringBuilder stringBuilder = new StringBuilder();
            int num = 0;
            string[] strArrays1 = str;
            for ( int i = 0; i < ( int ) strArrays1.Length; i++ )
            {
                string str1 = strArrays1[i];
                if ( num > 0 )
                {
                    stringBuilder.AppendFormat( "\"{0}\",", str1.Replace( "\"", "" ) );
                }
                num++;
            }
            string str2 = stringBuilder.ToString();
            if ( str2 != "" )
            {
                str2 = str2.Substring( 0, str2.Length - 1 );
            }
            return str2;
        }

        private List<GLExportLineItem> GenerateLineItems( List<GLTransaction> transactionItems, string description, string journalType, string accountingPeriod, DateTime? selectedDate )
        {
            List<GLExportLineItem> returnList = new List<GLExportLineItem>();
            foreach ( var transaction in transactionItems )
            {
                //transaction.LoadAttributes();

                string projectCode = String.Empty;

                //var transactionProject = transaction.GetAttributeValue( "Project" );
                if ( !String.IsNullOrWhiteSpace( transaction.projectCode ) )
                {
                    var dt = DefinedValueCache.Read( transaction.projectCode );
                    var projects = DefinedTypeCache.Read( dt.DefinedTypeId );
                    if ( projects != null )
                    {
                        foreach ( var project in projects.DefinedValues.OrderByDescending( a => a.Value.AsInteger() ).Where( p => p.Guid.ToString().ToLower() == transaction.projectCode.ToString().ToLower() ) )
                        {
                            projectCode = project.GetAttributeValue( "Code" );
                        }
                    }
                }

                GLExportLineItem generalLedgerExportLineItem = new GLExportLineItem()
                {
                    AccountingPeriod = accountingPeriod,
                    AccountNumber = transaction.glBankAccount, //row["gl_bank_account"].ToString(),
                    Amount = ( decimal ) transaction.total,
                    CompanyNumber = transaction.glCompany,//row["gl_company"].ToString(),
                    Date = selectedDate,
                    DepartmentNumber = "",
                    Description1 = description,
                    Description2 = string.Empty,
                    FundNumber = transaction.glFund,//row["gl_fund"].ToString(),
                    JournalNumber = 0,
                    JournalType = journalType,
                    ProjectCode = projectCode//row["project_code"].ToString()
                };
                nbResult.Text += "<strong>" + transaction.transactionDetail.Account.Name + "</strong><br>";
                if ( ValidateObject( generalLedgerExportLineItem ) )
                    returnList.Add( generalLedgerExportLineItem );
                GLExportLineItem generalLedgerExportLineItem1 = new GLExportLineItem()
                {
                    AccountingPeriod = accountingPeriod,
                    AccountNumber = transaction.glRevenueAccount,//row["gl_revenue_account"].ToString(),
                    Amount = new decimal( 10, 0, 0, true, 1 ) * ( decimal ) transaction.total,
                    CompanyNumber = transaction.glCompany,//row["gl_company"].ToString(),
                    Date = selectedDate,
                    DepartmentNumber = transaction.glRevenueDepartment,//row["gl_revenue_department"].ToString(),
                    Description1 = description,
                    Description2 = string.Empty,
                    FundNumber = transaction.glFund,//row["gl_fund"].ToString(),
                    JournalNumber = 0,
                    JournalType = journalType,
                    ProjectCode = projectCode//row["project_code"].ToString()
                };
                if ( ValidateObject( generalLedgerExportLineItem1 ) )
                    returnList.Add( generalLedgerExportLineItem1 );
            }
            return returnList;
        }

        #endregion

        #region Methods

        private void SetVisibilityOption()
        {
            bool showAccountingCode = GetAttributeValue( "ShowAccountingCode" ).AsBoolean();
            tbAccountingCode.Visible = showAccountingCode;
            gBatchList.Columns[4].Visible = showAccountingCode;

            if ( showAccountingCode )
            {
                string accountingCode = gfBatchFilter.GetUserPreference( "Accounting Code" );
                tbAccountingCode.Text = !string.IsNullOrWhiteSpace( accountingCode ) ? accountingCode : string.Empty;
            }
        }

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            string titleFilter = gfBatchFilter.GetUserPreference( "Title" );
            tbTitle.Text = !string.IsNullOrWhiteSpace( titleFilter ) ? titleFilter : string.Empty;

            if ( tbAccountingCode.Visible )
            {
                string accountingCode = gfBatchFilter.GetUserPreference( "Accounting Code" );
                tbAccountingCode.Text = !string.IsNullOrWhiteSpace( accountingCode ) ? accountingCode : string.Empty;
            }

            ddlStatus.BindToEnum<BatchStatus>();
            ddlStatus.Items.Insert( 0, Rock.Constants.All.ListItem );
            string statusFilter = gfBatchFilter.GetUserPreference( "Status" );
            if ( string.IsNullOrWhiteSpace( statusFilter ) )
            {
                statusFilter = BatchStatus.Open.ConvertToInt().ToString();
            }

            ddlStatus.SetValue( statusFilter );

            var definedTypeTransactionTypes = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE.AsGuid() );
            ddlTransactionType.BindToDefinedType( definedTypeTransactionTypes, true );
            ddlTransactionType.SetValue( gfBatchFilter.GetUserPreference( "Contains Transaction Type" ) );

            var campusi = CampusCache.All();
            campCampus.Campuses = campusi;
            campCampus.Visible = campusi.Any();
            campCampus.SetValue( gfBatchFilter.GetUserPreference( "Campus" ) );

            drpBatchDate.DelimitedValues = gfBatchFilter.GetUserPreference( "Date Range" );
        }

        /// <summary>
        /// Formats the value as currency (called from markup)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string FormatValueAsCurrency( decimal value )
        {
            return value.FormatAsCurrency();
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid( bool isExporting = false )
        {

            var txnCountCol = gBatchList.ColumnsOfType<RockBoundField>().FirstOrDefault( c => c.DataField == "TransactionCount" );
            if ( txnCountCol != null )
            {
                txnCountCol.HeaderText = isExporting ? "Transaction Count" :
                    "<span class='hidden-print'>Transaction Count</span><span class='visible-print-inline'>Txns</span>";
            }

            var txnAmountCol = gBatchList.ColumnsOfType<CurrencyField>().FirstOrDefault( c => c.DataField == "TransactionAmount" );
            if ( txnAmountCol != null )
            {
                txnAmountCol.HeaderText = isExporting ? "Transaction Amount" :
                    "<span class='hidden-print'>Transaction Total</span><span class='visible-print-inline'>Txn Total</span>";
            }

            var accountsCol = gBatchList.ColumnsOfType<RockBoundField>().FirstOrDefault( c => c.HeaderText == "Accounts" );
            if ( accountsCol != null )
            {
                accountsCol.DataField = isExporting ? "AccountSummaryText" : "AccountSummaryHtml";
            }

            try
            {
                var qry = GetQuery().AsNoTracking();
                var batchRowQry = qry.Select( b => new BatchRow
                {
                    Id = b.Id,
                    BatchStartDateTime = b.BatchStartDateTime.Value,
                    Name = b.Name,
                    AccountingSystemCode = b.AccountingSystemCode,
                    TransactionCount = b.Transactions.Count(),
                    TransactionAmount = b.Transactions.Sum( t => (decimal?)( t.TransactionDetails.Sum( d => (decimal?)d.Amount ) ?? 0.0M ) ) ?? 0.0M,
                    ControlAmount = b.ControlAmount,
                    CampusName = b.Campus != null ? b.Campus.Name : "",
                    Status = b.Status,
                    UnMatchedTxns = b.Transactions.Any( t => !t.AuthorizedPersonAliasId.HasValue ),
                    BatchNote = b.Note,
                    AccountSummaryList = b.Transactions
                        .SelectMany( t => t.TransactionDetails )
                        .GroupBy( d => d.AccountId )
                        .Select( s => new BatchAccountSummary
                        {
                            AccountId = s.Key,
                            AccountOrder = s.Max( d => d.Account.Order ),
                            AccountName = s.Max( d => d.Account.Name ),
                            Amount = s.Sum( d => (decimal?)d.Amount ) ?? 0.0M
                        } )
                        .OrderBy( s => s.AccountOrder )
                        .ToList()
                } );

                gBatchList.SetLinqDataSource( batchRowQry.AsNoTracking() );
                gBatchList.EntityTypeId = EntityTypeCache.Read<Rock.Model.FinancialBatch>().Id;
                gBatchList.DataBind();

                var qryTransactionDetails = qry.SelectMany( a => a.Transactions ).SelectMany( a => a.TransactionDetails );
                var accountSummaryQry = qryTransactionDetails.GroupBy( a => a.Account ).Select( a => new
                {
                    a.Key.Name,
                    a.Key.Order,
                    TotalAmount = (decimal?)a.Sum( d => d.Amount )
                } ).OrderBy( a => a.Order );

            }
            catch ( Exception ex )
            {
                nbWarningMessage.Text = ex.Message;
            }
        }

        /// <summary>
        /// Gets the query.  Set the timeout to 90 seconds in case the user
        /// has not set any filters and they've imported N years worth of
        /// batch data into Rock.
        /// </summary>
        /// <returns></returns>
        private IOrderedQueryable<FinancialBatch> GetQuery()
        {
            var rockContext = new RockContext();
            var batchService = new FinancialBatchService( rockContext );
            rockContext.Database.CommandTimeout = 90;
            var qry = batchService.Queryable()
                .Where( b => b.BatchStartDateTime.HasValue );

            // filter by date
            string dateRangeValue = gfBatchFilter.GetUserPreference( "Date Range" );
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

            // filter by status
            var status = gfBatchFilter.GetUserPreference( "Status" ).ConvertToEnumOrNull<BatchStatus>();
            if ( status.HasValue )
            {
                qry = qry.Where( b => b.Status == status );
            }

            // filter by batches that contain transactions of the specified transaction type
            var transactionTypeValueId = gfBatchFilter.GetUserPreference( "Contains Transaction Type" ).AsIntegerOrNull();
            if ( transactionTypeValueId.HasValue )
            {
                qry = qry.Where( a => a.Transactions.Any( t => t.TransactionTypeValueId == transactionTypeValueId.Value ) );
            }

            // filter by title
            string title = gfBatchFilter.GetUserPreference( "Title" );
            if ( !string.IsNullOrEmpty( title ) )
            {
                qry = qry.Where( batch => batch.Name.StartsWith( title ) );
            }

            // filter by accounting code
            if ( tbAccountingCode.Visible )
            {
                string accountingCode = gfBatchFilter.GetUserPreference( "Accounting Code" );
                if ( !string.IsNullOrEmpty( accountingCode ) )
                {
                    qry = qry.Where( batch => batch.AccountingSystemCode.StartsWith( accountingCode ) );
                }
            }

            // filter by campus
            var campus = CampusCache.Read( gfBatchFilter.GetUserPreference( "Campus" ).AsInteger() );
            if ( campus != null )
            {
                qry = qry.Where( b => b.CampusId == campus.Id );
            }

            IOrderedQueryable<FinancialBatch> sortedQry = null;

            SortProperty sortProperty = gBatchList.SortProperty;
            if ( sortProperty != null )
            {
                switch ( sortProperty.Property )
                {
                    case "TransactionCount":
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                sortedQry = qry.OrderBy( b => b.Transactions.Count() );
                            }
                            else
                            {
                                sortedQry = qry.OrderByDescending( b => b.Transactions.Count() );
                            }

                            break;
                        }

                    case "TransactionAmount":
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                sortedQry = qry.OrderBy( b => b.Transactions.Sum( t => (decimal?)( t.TransactionDetails.Sum( d => (decimal?)d.Amount ) ?? 0.0M ) ) ?? 0.0M );
                            }
                            else
                            {
                                sortedQry = qry.OrderByDescending( b => b.Transactions.Sum( t => (decimal?)( t.TransactionDetails.Sum( d => (decimal?)d.Amount ) ?? 0.0M ) ) ?? 0.0M );
                            }

                            break;
                        }

                    default:
                        {
                            sortedQry = qry.Sort( sortProperty );
                            break;
                        }
                }
            }
            else
            {
                sortedQry = qry
                    .OrderByDescending( b => b.BatchStartDateTime )
                    .ThenBy( b => b.Name );
            }

            return sortedQry;
        }

        private bool ValidateObject( GLExportLineItem model )
        {

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext context = new ValidationContext( model, null, null );

            if ( !Validator.TryValidateObject( model, context, errors, true ) )
            {
                nbResult.Text = string.Format( "Error exporting batch: <br>" );
                nbResult.NotificationBoxType = NotificationBoxType.Warning;
                nbResult.Visible = true;

                foreach ( ValidationResult e in errors )
                {
                    nbResult.Text += e.ErrorMessage;
                    nbResult.Text += "<br>";
                }
                return false;
            }

            return true;

        }

        #endregion

        #region Helper Class

        public class BatchAccountSummary
        {
            public int AccountId { get; set; }
            public int AccountOrder { get; set; }
            public string AccountName { get; set; }
            public decimal Amount { get; set; }
            public override string ToString()
            {
                return string.Format( "{0}: {1}", AccountName, Amount.FormatAsCurrency() );
            }
        }

        public class BatchRow
        {
            public int Id { get; set; }
            public DateTime BatchStartDateTime { get; set; }
            public string Name { get; set; }
            public string AccountingSystemCode { get; set; }
            public int TransactionCount { get; set; }
            public decimal TransactionAmount { get; set; }
            public decimal ControlAmount { get; set; }
            public List<BatchAccountSummary> AccountSummaryList { get; set; }
            public string CampusName { get; set; }
            public BatchStatus Status { get; set; }
            public bool UnMatchedTxns { get; set; }
            public string BatchNote { get; set; }

            public decimal Variance
            {
                get
                {
                    return TransactionAmount - ControlAmount;
                }
            }

            public string AccountSummaryText
            {
                get
                {
                    var summary = new List<string>();
                    AccountSummaryList.ForEach( a => summary.Add( a.ToString() ) );
                    return summary.AsDelimited( Environment.NewLine );
                }
            }

            public string AccountSummaryHtml
            {
                get
                {
                    var summary = new List<string>();
                    AccountSummaryList.ForEach( a => summary.Add( a.ToString() ) );
                    return "<small>" + summary.AsDelimited( "<br/>" ) + "</small>";
                }
            }

            public string StatusText
            {
                get
                {
                    return Status.ConvertToString();
                }
            }


            public string StatusLabelClass
            {
                get
                {
                    switch ( Status )
                    {
                        case BatchStatus.Closed: return "label label-default";
                        case BatchStatus.Open: return "label label-info";
                        case BatchStatus.Pending: return "label label-warning";
                    }

                    return string.Empty;
                }
            }

            public string Notes
            {
                get
                {
                    var notes = new StringBuilder();

                    switch ( Status )
                    {
                        case BatchStatus.Open:
                            {
                                if ( UnMatchedTxns )
                                {
                                    notes.Append( "<span class='label label-warning'>Unmatched Transactions</span><br/>" );
                                }

                                break;
                            }
                    }

                    notes.Append( BatchNote );

                    return notes.ToString();
                }
            }
        }

        public class GLTransaction : IEquatable<GLTransaction>
        {
            public string glCompany { get; set; }
            public string glFund { get; set; }
            public string glBankAccount { get; set; }
            public string glRevenueDepartment { get; set; }
            public string glRevenueAccount { get; set; }
            public string projectCode { get; set; }
            public decimal total { get; set; }

            public FinancialBatch batch { get; set; }
            public FinancialTransaction transaction { get; set; }
            public FinancialTransactionDetail transactionDetail { get; set; }

            public bool Equals( GLTransaction obj )
            {
                if ( ReferenceEquals( null, obj ) )
                    return false;
                if ( ReferenceEquals( this, obj ) )
                    return true;
                if ( obj.GetType() != this.GetType() )
                    return false;
                return string.Equals( glCompany, obj.glCompany )
                    && string.Equals( glFund, obj.glFund )
                    && string.Equals( glBankAccount, obj.glBankAccount )
                    && string.Equals( glRevenueDepartment, obj.glRevenueDepartment )
                    && string.Equals( glRevenueAccount, obj.glRevenueAccount )
                    && string.Equals( projectCode, obj.projectCode );
            }

            public override int GetHashCode()
            {
                int hashglCompany = glCompany == null ? 0 : glCompany.GetHashCode();
                int hashglFund = glFund == null ? 0 : glFund.GetHashCode();
                int hashglBankAccount = glBankAccount == null ? 0 : glBankAccount.GetHashCode();
                int hashglRevenueDepartment = glRevenueDepartment == null ? 0 : glRevenueDepartment.GetHashCode();
                int hashglRevenueAccount = glRevenueAccount == null ? 0 : glRevenueAccount.GetHashCode();
                int hashprojectCode = projectCode == null ? 0 : projectCode.GetHashCode();

                return hashglCompany ^ hashglFund ^ hashglBankAccount ^ hashglRevenueDepartment ^ hashglRevenueAccount ^ hashprojectCode;
            }

        }

        public class GLExportLineItem
        {
            [StringLength( 2, ErrorMessage = "AccountingPeriod cannot have more than 2 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "AccountingPeriod must be numeric." )]
            public string AccountingPeriod { get; set; }
            [StringLength( 9, ErrorMessage = "AccountNumber cannot have more than 9 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "AccountNumber must be numeric." )]
            public string AccountNumber { get; set; }
            public decimal Amount { get; set; }
            [StringLength( 4, ErrorMessage = "CompanyNumber cannot have more than 4 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "CompanyNumber must be numeric." )]
            [Required()]
            public string CompanyNumber { get; set; }
            public DateTime? Date { get; set; }
            [StringLength( 3, ErrorMessage = "DepartmentNumber cannot have more than 3 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "DepartmentNumber must be numeric." )]
            public string DepartmentNumber { get; set; }
            public string Description1 { get; set; }
            public string Description2 { get; set; }
            [StringLength( 5, ErrorMessage = "FundNumber cannot have more than 5 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "FundNumber must be numeric." )]
            [Required()]
            public string FundNumber { get; set; }
            [Range( 0, 99999, ErrorMessage = "JournalNumber cannot have more than 5 characters in length." )]
            public int JournalNumber { get; set; }
            [StringLength( 2, ErrorMessage = "JournalType cannot have more than 2 characters in length." )]
            public string JournalType { get; set; }
            [StringLength( 50, ErrorMessage = "ProjectCode cannot have more than 50 characters in length." )]
            public string ProjectCode { get; set; }
        }

        #endregion
    }
}