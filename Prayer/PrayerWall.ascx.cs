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
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;

using DotLiquid;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Transactions;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Prayer
{
    #region Block Attributes

    [DisplayName( "Prayer Wall" )]
    [Category( "KFS > Prayer" )]
    [Description( "Block that uses lava to display prayer requests." )]

    #endregion

    #region Block Settings

    [IntegerField( "Page Size", "Number of prayer requests to return per page.", false, 10, order: 0 )]
    [CategoryField( "Category Filter", "The category (or parent category) to limit the listed prayer requests.", true, "Rock.Model.PrayerRequest", required: false, order: 1 )]
    [CustomDropdownListField( "Sort Order", "", "0^Date Entered Descending,1^Date Entered Ascending", false, "0", order: 2 )]

    [CodeEditorField( "Lava Template", "Lava template for prayer wall display.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"
<div class='panel panel-block'>
    <div class='panel-body'>
        <ul class='list-group'>
            {% assign numReq = PrayerRequests | Size -%}
            {% if numReq > 0 -%}
            {% for request in PrayerRequests -%}
            {% capture heading -%}{{ request.FirstName }} {{ request.LastName }}{% endcapture -%}
            <li class='list-group-item'>
                <div class='container-fluid'>
                    <div class='col-sm-12 col-md-6'>
                        <h4 class='list-group-item-heading'>{{ heading | Trim }}</h4>
                    </div>
                    <div class='col-xs-6 col-md-3'>{% if request.PrayerCount -%}<em class='pull-right'>Prayed for {{ request.PrayerCount }} {{ 'time' | PluralizeForQuantity:request.PrayerCount }}.</em>{% endif -%}</div>
                    <div class='col-xs-6 col-md-3'><a class='btn btn-default text-uppercase' href='#' onclick="" {{ request.Id | Postback:'PrayerCountIncrement' }}"">I Prayed</a></div>
                    <div class='col-md-offset-1 col-sm-10'>
                        <p class='list-group-item-text margin-t-sm margin-b-sm'>{{ request.Text }}<br>
                    </div>
                    <div class='col-sm-12'><small><strong>Received: {{ request.EnteredDateTime | Date:'MMMM d, yyyy' }}</strong></small></div>
                    {% if request.Category -%}<div class='col-sm-12'><small><strong>Category: {{ request.Category.Name }}</strong></small></div>{% endif -%}
                    {% if request.Campus -%}<div class='col-sm-12'><small><strong>Campus: {{ request.Campus.Name }}</strong></small></div>{% endif -%}
                </div>
            </li>
            {% endfor -%}
            {% else -%}
            <div class='alert alert-info'>Sorry, no active prayer requests found.</div>
            {% endif -%}
        </ul>
        {% if Pagination.TotalPages > 1 -%}
        <div class='margin-t-xl text-center'>
            {% assign nextPageString = Pagination.NextPage | ToString -%}
            {% assign prevPageString = Pagination.PreviousPage | ToString -%}
            <nav role='navigation'>
                <ul class='pagination margin-b-sm'>
                    {% if Pagination.PreviousPage == -1 -%}
                    <li class='disabled'>
                        <a href='#'>
                            <span aria-hidden='true'><i class='fa fa-step-backward'></i></span>
                        </a>
                    </li>
                    {% if Pagination.TotalPages > 2 -%}
                    <li class='disabled'>
                        <a href='#'>
                            <span aria-hidden='true'><i class='fa fa-backward'></i></span>
                        </a>
                    </li>
                    {% endif -%}
                    {% else -%}
                    <li>
                        <a href='{{ Pagination.UrlTemplate | Replace:'PageNum', '1' }}'>
                            <span aria-hidden='true'><i class='fa fa-step-backward'></i></span>
                        </a>
                    </li>
                    {% if Pagination.TotalPages > 2 -%}
                    <li>
                        <a href='{{ Pagination.UrlTemplate | Replace:'PageNum', prevPageString }}'>
                            <span aria-hidden='true'><i class='fa fa-backward'></i></span>
                        </a>
                    </li>
                    {% endif -%}
                    {% endif -%}

                    {% assign pageOffset = Pagination.CurrentPage | Minus:3 %}
                    {% if pageOffset < 1 -%}
                    {% assign pageOffset = 0 %}
                    {% endif -%}
                    {% assign toEnd = Pagination.TotalPages | Minus:Pagination.CurrentPage %}
                    {% if toEnd < 2 -%}
                    {% assign pageOffset = Pagination.TotalPages | Minus:5 %}
                    {% endif -%}
                    {% for page in Pagination.Pages limit:5 offset:pageOffset -%}
                    {% if page.PageNumber == Pagination.CurrentPage -%}
                    <li class='active' aria-current='page'>
                        <span>{{ page.PageNumber}}</span>
                    </li>
                    {% else -%}
                    <li>
                        <a href='{{ page.PageUrl }}'>
                            <span>{{ page.PageNumber}}</span>
                        </a>
                    </li>
                    {% endif -%}
                    {% endfor -%}

                    {% if Pagination.NextPage == -1 -%}
                    {% if Pagination.TotalPages > 2 -%}
                    <li class='disabled'>
                        <a href='#'>
                            <span aria-hidden='true'><i class='fa fa-forward'></i></span>
                        </a>
                    </li>
                    {% endif -%}
                    <li class='disabled'>
                        <a href='#'>
                            <span aria-hidden='true'><i class='fa fa-step-forward'></i></span>
                        </a>
                    </li>
                    {% else -%}
                    {% if Pagination.TotalPages > 2 -%}
                    <li>
                        <a href='{{ Pagination.UrlTemplate | Replace:'PageNum', nextPageString }}'>
                            <span aria-hidden='true'><i class='fa fa-forward'></i></span>
                        </a>
                    </li>
                    {% endif -%}
                    <li>
                        <a href='{{ Pagination.UrlTemplate | Replace:'PageNum', Pagination.TotalPages }}'>
                            <span aria-hidden='true'><i class='fa fa-step-forward'></i></span>
                        </a>
                    </li>
                    {% endif -%}
                </ul>
            </nav>
            <span class='margin-l-lg margin-r-lg'>Page {{ Pagination.CurrentPage }} of {{ Pagination.TotalPages }}</span>
        </div>
        {% endif -%}
    </div>
</div>
", category: "Lava", order: 3 )]
    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for this block.", false, category: "Lava", order: 4 )]

    [WorkflowTypeField( "Prayer Count Workflow", "An optional workflow to start when the prayer count is incremented. The PrayerRequest will be set as the workflow 'Entity' when processing is started.", false, false, "", "Workflow", 5 )]
    [BooleanField( "Process Workflow Immediately", "Determines whether the workflow is processed instantly or queued to begin sometime within the next 60 seconds.", category: "Workflow", order: 6 )]

    #endregion

    public partial class PrayerWall : RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RouteAction();
            ShowWall();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the wall.
        /// </summary>
        private void ShowWall()
        {
            var pageRef = CurrentPageReference;
            pageRef.Parameters.AddOrReplace( "Page", "PageNum" );

            var prayerRequests = new List<PrayerRequest>();

            var qry = new PrayerRequestService( new RockContext() )
                .Queryable()
                .AsNoTracking()
                .Where( r => r.ExpirationDate >= RockDateTime.Now &&
                    r.IsApproved == true &&
                    r.IsPublic == true );

            var categoryGuids = ( GetAttributeValue( "CategoryFilter" ) ?? string.Empty ).SplitDelimitedValues().AsGuidList();
            if ( categoryGuids.Any() )
            {
                qry = qry.Where( a => a.CategoryId.HasValue && ( categoryGuids.Contains( a.Category.Guid ) || ( a.Category.ParentCategoryId.HasValue && categoryGuids.Contains( a.Category.ParentCategory.Guid ) ) ) );
            }

            var campusEntity = RockPage.GetCurrentContext( EntityTypeCache.Get( typeof( Campus ) ) );
            if ( campusEntity != null )
            {
                var campusId = campusEntity.Id;
                qry = qry.Where( r => r.CampusId.HasValue && r.CampusId == campusId );
            }

            var sortOrder = GetAttributeValue( "SortOrder" ).AsInteger();
            switch ( sortOrder )
            {
                case 0:
                    qry = qry.OrderByDescending( a => a.EnteredDateTime );
                    break;

                case 1:
                    qry = qry.OrderBy( a => a.EnteredDateTime );
                    break;
            }

            prayerRequests = qry.ToList();

            var pagination = new Pagination();
            pagination.ItemCount = prayerRequests.Count();
            pagination.PageSize = GetAttributeValue( "PageSize" ).AsInteger();
            pagination.CurrentPage = PageParameter( "Page" ).AsIntegerOrNull() ?? 1;
            pagination.UrlTemplate = pageRef.BuildUrl();

            var currentPrayerRequests = pagination.GetCurrentPageItems( prayerRequests );

            var commonMergeFields = LavaHelper.GetCommonMergeFields( RockPage );

            var mergeFields = new Dictionary<string, object>( commonMergeFields );
            mergeFields.Add( "Pagination", pagination );
            mergeFields.Add( "PrayerRequests", currentPrayerRequests );

            Template template = null;
            ILavaTemplate lavaTemplate = null;
            var error = string.Empty;
            try
            {
                if ( LavaService.RockLiquidIsEnabled )
                {
                    template = Template.Parse( GetAttributeValue( "LavaTemplate" ) );

                    LavaHelper.VerifyParseTemplateForCurrentEngine( GetAttributeValue( "LavaTemplate" ) );
                }
                else
                {
                    var parseResult = LavaService.ParseTemplate( GetAttributeValue( "LavaTemplate" ) );

                    lavaTemplate = parseResult.Template;
                }
            }
            catch ( Exception ex )
            {
                error = string.Format( "Lava error: {0}", ex.Message );
            }
            finally
            {
                if ( error.IsNotNullOrWhiteSpace() )
                {
                    nbError.Text = error;
                    nbError.Visible = true;
                }

                if ( template != null || lavaTemplate != null )
                {
                    if ( LavaService.RockLiquidIsEnabled )
                    {
                        template.Registers["EnabledCommands"] = GetAttributeValue( "EnabledLavaCommands" );
                        lContent.Text = template.Render( Hash.FromDictionary( mergeFields ) );
                    }
                    else
                    {
                        var lavaContext = LavaService.NewRenderContext( mergeFields, GetAttributeValue( "EnabledLavaCommands" ).SplitDelimitedValues() );
                        var result = LavaService.RenderTemplate( lavaTemplate, lavaContext );

                        lContent.Text = result.Text;
                    }
                }
            }
        }

        /// <summary>
        /// Increments the prayer count.
        /// </summary>
        /// <param name="prayerRequestId">The prayer request identifier.</param>
        private void IncrementPrayerCount( int prayerRequestId )
        {
            using ( var rockContext = new RockContext() )
            {
                var request = new PrayerRequestService( rockContext ).Get( prayerRequestId );
                var count = request.PrayerCount ?? 0;
                request.PrayerCount = ( count + 1 );
                rockContext.SaveChanges();

                Guid? workflowTypeGuid = GetAttributeValue( "PrayerCountWorkflow" ).AsGuidOrNull();
                if ( workflowTypeGuid.HasValue )
                {
                    var workflowType = WorkflowTypeCache.Get( workflowTypeGuid.Value );
                    if ( workflowType != null && ( workflowType.IsActive ?? true ) )
                    {
                        if ( GetAttributeValue( "ProcessWorkflowImmediately" ).AsBoolean() )
                        {
                            try
                            {
                                var workflow = Workflow.Activate( workflowType, request.Name );
                                List<string> workflowErrors;
                                new WorkflowService( rockContext ).Process( workflow, request, out workflowErrors );
                            }
                            catch ( Exception ex )
                            {
                                ExceptionLogService.LogException( ex, this.Context );
                            }
                        }
                        else
                        {
                            var workflowDetails = new List<LaunchWorkflowDetails>();
                            workflowDetails.Add( new LaunchWorkflowDetails( request ) );
                            var transaction = new Rock.Transactions.LaunchWorkflowsTransaction( workflowTypeGuid.Value, workflowDetails );
                            // NOTE: In v8, the initiator will always be null while using delayed start.
                            //transaction.InitiatorPersonAliasId = CurrentPersonAliasId; // available in v9
                            Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Routes the action.
        /// </summary>
        private void RouteAction()
        {
            int requestId = 0;
            var sm = ScriptManager.GetCurrent( Page );

            if ( Request.Form["__EVENTARGUMENT"] != null )
            {

                string[] eventArgs = Request.Form["__EVENTARGUMENT"].Split( '^' );

                if ( eventArgs.Length == 2 )
                {
                    string action = eventArgs[0];
                    string parameters = eventArgs[1];

                    int argument = 0;
                    int.TryParse( parameters, out argument );

                    switch ( action )
                    {
                        case "PrayerCountIncrement":
                            requestId = int.Parse( parameters );
                            IncrementPrayerCount( requestId );
                            break;
                    }
                }
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="DotLiquid.Drop" />
        public class Pagination : DotLiquid.Drop
        {

            /// <summary>
            /// Gets or sets the item count.
            /// </summary>
            /// <value>
            /// The item count.
            /// </value>
            public int ItemCount { get; set; }

            /// <summary>
            /// Gets or sets the size of the page.
            /// </summary>
            /// <value>
            /// The size of the page.
            /// </value>
            public int PageSize { get; set; }

            /// <summary>
            /// Gets or sets the current page.
            /// </summary>
            /// <value>
            /// The current page.
            /// </value>
            public int CurrentPage { get; set; }

            /// <summary>
            /// Gets the previous page.
            /// </summary>
            /// <value>
            /// The previous page.
            /// </value>
            public int PreviousPage
            {
                get
                {
                    CurrentPage = CurrentPage > TotalPages ? TotalPages : CurrentPage;
                    return ( CurrentPage > 1 ) ? CurrentPage - 1 : -1;
                }
            }

            /// <summary>
            /// Gets the next page.
            /// </summary>
            /// <value>
            /// The next page.
            /// </value>
            public int NextPage
            {
                get
                {
                    CurrentPage = CurrentPage > TotalPages ? TotalPages : CurrentPage;
                    return ( CurrentPage < TotalPages ) ? CurrentPage + 1 : -1;
                }
            }

            /// <summary>
            /// Gets the total pages.
            /// </summary>
            /// <value>
            /// The total pages.
            /// </value>
            public int TotalPages
            {
                get
                {
                    if ( PageSize == 0 )
                    {
                        return 1;
                    }
                    else
                    {
                        return Convert.ToInt32( Math.Abs( ItemCount / PageSize ) ) +
                            ( ( ItemCount % PageSize ) > 0 ? 1 : 0 );
                    }
                }
            }

            public string UrlTemplate { get; set; }

            /// <summary>
            /// Gets or sets the pages.
            /// </summary>
            /// <value>
            /// The pages.
            /// </value>
            public List<PaginationPage> Pages
            {
                get
                {
                    var pages = new List<PaginationPage>();

                    for ( int i = 1; i <= TotalPages; i++ )
                    {
                        pages.Add( new PaginationPage( UrlTemplate, i ) );
                    }

                    return pages;
                }
            }

            /// <summary>
            /// Gets the current page items.
            /// </summary>
            /// <param name="allItems">All items.</param>
            /// <returns></returns>
            public List<PrayerRequest> GetCurrentPageItems( List<PrayerRequest> allItems )
            {
                if ( PageSize > 0 )
                {
                    CurrentPage = CurrentPage > TotalPages ? TotalPages : CurrentPage;
                    return allItems.Skip( ( CurrentPage - 1 ) * PageSize ).Take( PageSize ).ToList();
                }

                return allItems;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="DotLiquid.Drop" />
        public class PaginationPage : DotLiquid.Drop
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PaginationPage"/> class.
            /// </summary>
            /// <param name="urlTemplate">The URL template.</param>
            /// <param name="pageNumber">The page number.</param>
            public PaginationPage( string urlTemplate, int pageNumber )
            {
                UrlTemplate = urlTemplate;
                PageNumber = pageNumber;
            }

            private string UrlTemplate { get; set; }

            /// <summary>
            /// Gets the page number.
            /// </summary>
            /// <value>
            /// The page number.
            /// </value>
            public int PageNumber { get; private set; }

            /// <summary>
            /// Gets the page URL.
            /// </summary>
            /// <value>
            /// The page URL.
            /// </value>
            public string PageUrl
            {
                get
                {
                    if ( !string.IsNullOrWhiteSpace( UrlTemplate ) && UrlTemplate.Contains( "PageNum" ) )
                    {
                        return string.Format( UrlTemplate.Replace( "PageNum", "{0}" ), PageNumber );
                    }
                    else
                    {
                        return PageNumber.ToString();
                    }
                }
            }
        }

        #endregion
    }
}