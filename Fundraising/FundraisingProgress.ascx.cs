// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
// <notice>
// This file contains modifications by Kingdom First Solutions
// and is a derivative work.
//
// Modification (including but not limited to):
// * Adds settings to hide various elements of the core block.
// * Adds Excel Export functionality.
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using OfficeOpenXml;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Group = Rock.Model.Group;

namespace RockWeb.Plugins.rocks_kfs.Fundraising
{
    #region Block Attributes

    [DisplayName( "Fundraising Progress" )]
    [Category( "KFS > Fundraising" )]
    [Description( "Progress for all people in a fundraising opportunity" )]

    #endregion Block Attributes

    #region Block Settings

    [BooleanField( "Show Group Title", "Should the Group Title be displayed?", true, "", 1 )]
    [BooleanField( "Show Total Amount Raised", "Should the total amount raised be displayed?", false, "", 2 )]
    [BooleanField( "Show Group Total Goals", "Should the total goals be displayed?", true, "", 3 )]
    [BooleanField( "Show Group Total Goals Amount", "Should the group total goals amount be displayed?", true, "", 4 )]
    [BooleanField( "Show Group Total Goals Progress Bar", "Should the group total goals progress bar be displayed?", true, "", 5 )]
    [BooleanField( "Show Group Member Goals", "Should group member goals be displayed?", true, "", 6 )]
    [BooleanField( "Show Group Member Goal Amounts", "Should group member goal amounts be displayed?", true, "", 7 )]
    [BooleanField( "Show Group Member Goal Progress Bars", "Should group member goal progress bars be displayed?", true, "", 8 )]
    [BooleanField( "Show Excel Export Button", "Should the Excel Export Button be displayed?", false, "", 9 )]

    #endregion Block Settings

    public partial class FundraisingProgress : RockBlock
    {
        #region Fields

        public decimal PercentComplete = 0;
        public decimal GroupIndividualFundraisingGoal;
        public decimal GroupContributionTotal;
        public string ProgressCssClass;

        #endregion Fields

        #region Base Control Methods

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
                Session["IndividualData"] = null;
                int? groupId = this.PageParameter( "GroupId" ).AsIntegerOrNull();
                int? groupMemberId = this.PageParameter( "GroupMemberId" ).AsIntegerOrNull();

                if ( groupId.HasValue || groupMemberId.HasValue )
                {
                    ShowView( groupId, groupMemberId );
                }
                else
                {
                    pnlView.Visible = false;
                }

                divTotalRaised.Visible = GetAttributeValue( "ShowTotalAmountRaised" ).AsBoolean();
                divPanelHeading.Visible = GetAttributeValue( "ShowGroupTitle" ).AsBoolean();
                pnlHeader.Visible = GetAttributeValue( "ShowGroupTotalGoals" ).AsBoolean();
                pTotalAmounts.Visible = GetAttributeValue( "ShowGroupTotalGoalsAmount" ).AsBoolean();
                divTotalProgress.Visible = GetAttributeValue( "ShowGroupTotalGoalsProgressBar" ).AsBoolean();
                ulGroupMembers.Visible = GetAttributeValue( "ShowGroupMemberGoals" ).AsBoolean();
                pnlActions.Visible = GetAttributeValue( "ShowExcelExportButton" ).AsBoolean();
            }
        }

        #endregion Base Control Methods

        #region Protected Methods

        /// <summary>
        /// Shows the view.
        /// </summary>
        /// <param name = "groupId" > The group identifier.</param>
        protected void ShowView( int? groupId, int? groupMemberId )
        {
            var rockContext = new RockContext();
            Group group = null;
            GroupMember groupMember = null;
            int fundraisingOpportunityTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FUNDRAISINGOPPORTUNITY ).Id;

            pnlView.Visible = true;
            hfGroupId.Value = groupId.ToStringSafe();
            hfGroupMemberId.Value = groupMemberId.ToStringSafe();

            if ( groupId.HasValue )
            {
                group = new GroupService( rockContext ).Get( groupId.Value );
            }
            else
            {
                groupMember = new GroupMemberService( rockContext ).Get( groupMemberId ?? 0 );
                group = groupMember.Group;
            }

            if ( group == null || ( group.GroupTypeId != fundraisingOpportunityTypeId && group.GroupType.InheritedGroupTypeId != fundraisingOpportunityTypeId ) )
            {
                pnlView.Visible = false;
                return;
            }
            lTitle.Text = group.Name.FormatAsHtmlTitle();

            BindGroupMembersProgressGrid( group, groupMember, rockContext );
        }

        /// <summary>
        /// Binds the group members progress repeater.
        /// </summary>
        protected void BindGroupMembersProgressGrid( Group group, GroupMember gMember, RockContext rockContext )
        {
            IQueryable<GroupMember> groupMembersQuery;

            if ( gMember != null )
            {
                groupMembersQuery = new GroupMemberService( rockContext ).Queryable().Where( a => a.Id == gMember.Id );

                pnlHeader.Visible = false;
            }
            else
            {
                groupMembersQuery = new GroupMemberService( rockContext ).Queryable().Where( a => a.GroupId == group.Id );
            }

            group.LoadAttributes( rockContext );
            var defaultIndividualFundRaisingGoal = group.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull();

            groupMembersQuery = groupMembersQuery.Sort( new SortProperty { Property = "Person.LastName, Person.NickName" } );

            var entityTypeIdGroupMember = EntityTypeCache.GetId<Rock.Model.GroupMember>();

            var groupMemberList = groupMembersQuery.ToList().Select( a =>
            {
                var groupMember = a;
                groupMember.LoadAttributes( rockContext );

                var contributionTotal = new FinancialTransactionDetailService( rockContext ).Queryable()
                            .Where( d => d.EntityTypeId == entityTypeIdGroupMember
                                    && d.EntityId == groupMember.Id )
                            .Sum( d => ( decimal? ) d.Amount ) ?? 0;

                var individualFundraisingGoal = groupMember.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull();
                bool disablePublicContributionRequests = groupMember.GetAttributeValue( "DisablePublicContributionRequests" ).AsBoolean();
                if ( !individualFundraisingGoal.HasValue )
                {
                    individualFundraisingGoal = group.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull();
                }

                decimal percentageAchieved = 0;
                if ( individualFundraisingGoal != null )
                {
                    percentageAchieved = individualFundraisingGoal == 0 ? 100 : contributionTotal / ( 0.01M * individualFundraisingGoal.Value );
                }

                var progressBarWidth = percentageAchieved;

                if ( percentageAchieved >= 100 )
                {
                    progressBarWidth = 100;
                }

                if ( !individualFundraisingGoal.HasValue )
                {
                    individualFundraisingGoal = 0;
                }

                return new
                {
                    groupMember.Person.FullName,
                    groupMember.Person.NickName,
                    groupMember.Person.LastName,
                    GroupName = group.Name,
                    IndividualFundraisingGoal = ( individualFundraisingGoal ?? 0.00M ).ToString( "0.##" ),
                    ContributionTotal = contributionTotal.ToString( "0.##" ),
                    Percentage = percentageAchieved.ToString( "0.##" ),
                    CssClass = GetProgressCssClass( percentageAchieved ),
                    ProgressBarWidth = progressBarWidth
                };
            } ).ToList();
            Session["IndividualData"] = groupMemberList;

            this.GroupIndividualFundraisingGoal = groupMemberList.Sum( a => decimal.Parse( a.IndividualFundraisingGoal ) );
            this.GroupContributionTotal = groupMemberList.Sum( a => decimal.Parse( a.ContributionTotal ) );
            this.PercentComplete = decimal.Round( this.GroupIndividualFundraisingGoal == 0 ? 100 : this.GroupContributionTotal / ( this.GroupIndividualFundraisingGoal * 0.01M ), 2 );
            this.ProgressCssClass = GetProgressCssClass( this.PercentComplete );

            rptFundingProgress.DataSource = groupMemberList;
            rptFundingProgress.DataBind();
        }

        protected void btnExport_Click( object sender, EventArgs e )
        {
            if ( Session["IndividualData"] != null )
            {
                var individualSource = ( IEnumerable<dynamic> ) Session["IndividualData"];

                // create default settings
                string workSheetName = "Export";
                string title = "RockExport";

                ExcelPackage excel = new ExcelPackage();

                excel.Workbook.Properties.Title = title;

                // add author info
                Rock.Model.UserLogin userLogin = Rock.Model.UserLoginService.GetCurrentUser();
                if ( userLogin != null )
                {
                    excel.Workbook.Properties.Author = userLogin.Person.FullName;
                }
                else
                {
                    excel.Workbook.Properties.Author = "Rock";
                }

                // add the page that created this
                excel.Workbook.Properties.SetCustomPropertyValue( "Source", HttpContext.Current.Request.Url.OriginalString );

                ExcelWorksheet worksheet = excel.Workbook.Worksheets.Add( workSheetName );

                var headerRows = 1;
                int rowCounter = headerRows;
                int columnCounter = 1;

                worksheet.Cells[rowCounter, columnCounter].Value = "NickName";
                worksheet.Column( columnCounter ).Style.Numberformat.Format = ExcelHelper.GeneralFormat;
                columnCounter++;
                worksheet.Cells[rowCounter, columnCounter].Value = "LastName";
                worksheet.Column( columnCounter ).Style.Numberformat.Format = ExcelHelper.GeneralFormat;
                columnCounter++;
                worksheet.Cells[rowCounter, columnCounter].Value = "IndividualGoal";
                worksheet.Column( columnCounter ).Style.Numberformat.Format = ExcelHelper.CurrencyFormat;
                columnCounter++;
                worksheet.Cells[rowCounter, columnCounter].Value = "TotalRaised";
                worksheet.Column( columnCounter ).Style.Numberformat.Format = ExcelHelper.CurrencyFormat;
                columnCounter++;
                worksheet.Cells[rowCounter, columnCounter].Value = "PercentageRaised";
                worksheet.Column( columnCounter ).Style.Numberformat.Format = "0%";

                // print data
                if ( individualSource.Any() )
                {
                    foreach ( var individual in individualSource )
                    {
                        rowCounter++;
                        var columnIndex = 1;
                        ExcelHelper.SetExcelValue( worksheet.Cells[rowCounter, columnIndex], individual.NickName );
                        ExcelHelper.FinalizeColumnFormat( worksheet, columnIndex, individual.NickName );
                        columnIndex++;
                        ExcelHelper.SetExcelValue( worksheet.Cells[rowCounter, columnIndex], individual.LastName );
                        ExcelHelper.FinalizeColumnFormat( worksheet, columnIndex, individual.LastName );
                        columnIndex++;
                        ExcelHelper.SetExcelValue( worksheet.Cells[rowCounter, columnIndex], decimal.Parse( individual.IndividualFundraisingGoal ) );
                        ExcelHelper.FinalizeColumnFormat( worksheet, columnIndex, decimal.Parse( individual.IndividualFundraisingGoal ) );
                        columnIndex++;
                        ExcelHelper.SetExcelValue( worksheet.Cells[rowCounter, columnIndex], decimal.Parse( individual.ContributionTotal ) );
                        ExcelHelper.FinalizeColumnFormat( worksheet, columnIndex, decimal.Parse( individual.ContributionTotal ) );
                        columnIndex++;
                        ExcelHelper.SetExcelValue( worksheet.Cells[rowCounter, columnIndex], decimal.Parse( individual.Percentage ) / 100 );
                        ExcelHelper.FinalizeColumnFormat( worksheet, columnIndex, decimal.Parse( individual.Percentage ) );
                    }
                }
                else
                {
                    rowCounter++;

                    var columnIndex = 1;
                    ExcelHelper.SetExcelValue( worksheet.Cells[rowCounter, columnIndex], string.Empty );
                    ExcelHelper.FinalizeColumnFormat( worksheet, columnIndex, string.Empty );
                }

                var range = worksheet.Cells[headerRows, 1, rowCounter, columnCounter];
                var table = worksheet.Tables.Add( range, title );

                table.ShowHeader = true;
                table.ShowFilter = true;
                table.TableStyle = OfficeOpenXml.Table.TableStyles.None;

                // Format header range
                using ( ExcelRange r = worksheet.Cells[headerRows, 1, headerRows, columnCounter] )
                {
                    r.Style.Font.Bold = true;
                    r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                }

                // do AutoFitColumns on no more than the first 10000 rows (10000 can take 4-5 seconds, but could take several minutes if there are 100000+ rows )
                int autoFitRows = Math.Min( rowCounter, 10000 );
                var autoFitRange = worksheet.Cells[headerRows, 1, autoFitRows, columnCounter];

                autoFitRange.AutoFitColumns();

                // set some footer text
                worksheet.HeaderFooter.OddHeader.CenteredText = title;
                worksheet.HeaderFooter.OddFooter.RightAlignedText = string.Format( "Page {0} of {1}", ExcelHeaderFooter.PageNumber, ExcelHeaderFooter.NumberOfPages );

                var filename = string.Format( "FinancialProgress_{0}.xlsx", Regex.Replace( individualSource.FirstOrDefault().GroupName, "[^A-Za-z0-9_\\- ]", string.Empty, RegexOptions.CultureInvariant ) );

                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AppendHeader( "Content-Disposition", "attachment; filename=" + filename );
                Response.Charset = string.Empty;
                Response.BinaryWrite( excel.ToByteArray() );
                Response.Flush();
                Response.End();
                int? groupId = this.PageParameter( "GroupId" ).AsIntegerOrNull();
                int? groupMemberId = this.PageParameter( "GroupMemberId" ).AsIntegerOrNull();

                if ( groupId.HasValue || groupMemberId.HasValue )
                {
                    ShowView( groupId, groupMemberId );
                }
            }
        }

        #endregion Protected Methods

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowView( hfGroupId.Value.AsIntegerOrNull(), hfGroupMemberId.Value.AsIntegerOrNull() );
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptFundingProgress control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptFundingProgress_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var divMemberGoalAmount = e.Item.FindControl( "divMemberGoalAmount" );
            var pnlMemberGoalProgressBar = e.Item.FindControl( "pnlMemberGoalProgressBar" ) as Panel;

            divMemberGoalAmount.Visible = GetAttributeValue( "ShowGroupMemberGoalAmounts" ).AsBoolean();
            pnlMemberGoalProgressBar.Visible = GetAttributeValue( "ShowGroupMemberGoalProgressBars" ).AsBoolean();

            if ( divMemberGoalAmount.Visible )
            {
                pnlMemberGoalProgressBar.CssClass = "col-xs-12 col-md-8 col-md-offset-4";
            }
            else
            {
                pnlMemberGoalProgressBar.CssClass = "col-xs-12 col-md-8";
            }
        }

        #endregion Events

        #region Private Methods

        private string GetProgressCssClass( decimal percentage )
        {
            var cssClass = "warning";

            if ( percentage >= 100 )
            {
                cssClass = "success";
            }
            else if ( percentage > 40 && percentage < 100 )
            {
                cssClass = "info";
            }

            return cssClass;
        }

        #endregion Private Methods
    }
}