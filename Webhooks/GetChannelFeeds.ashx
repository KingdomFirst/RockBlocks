<!--
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
// * Allows multiple content channel ids to be provided instead of just one.
// </notice>
//
-->
<%@ WebHandler Language="C#" Class="RockWeb.Plugins.rocks_kfs.Webhooks.GetChannelFeeds" %>
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using DotLiquid;
using DotLiquid.Util;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace RockWeb.Plugins.rocks_kfs.Webhooks
{
    /// <summary>
    /// Handles retrieving file data from storage
    /// </summary>
    public class GetChannelFeeds : IHttpHandler
    {

        private HttpRequest request;
        private HttpResponse response;

        private int rssItemLimit = 10;

        public void ProcessRequest( HttpContext context )
        {
            Template.RegisterFilter( typeof( KFSFilters ) );

            request = context.Request;
            response = context.Response;

            string cacheKey = "Rock:GetChannelFeed:" + request.RawUrl;
            var contentCache = RockCache.Get( cacheKey );
            var mimeTypeCache = RockCache.Get( cacheKey + ":MimeType" );
            if ( mimeTypeCache != null && contentCache != null )
            {
                response.ContentType = ( string ) mimeTypeCache;
                response.Write( ( string ) contentCache );
                response.StatusCode = 200;
                return;
            }

            if ( request.HttpMethod != "GET" && request.HttpMethod != "HEAD" )
            {
                response.TrySkipIisCustomErrors = true;
                response.StatusCode = 405;
                response.Headers.Add( "Allow", "GET" );
                response.Write( "Invalid request method." );
                return;
            }

            if ( request.QueryString["ChannelId"] == null )
            {
                response.TrySkipIisCustomErrors = true;
                response.StatusCode = 400;
                response.Write( "A ChannelId is required." );
                return;
            }

            RockContext rockContext = new RockContext();

            if ( request.QueryString["ChannelId"] != null )
            {
                int templateDefinedValueId;
                DefinedValueCache dvRssTemplate;
                string rssTemplate;
                var channelIds = new List<int>();

                if ( !string.IsNullOrWhiteSpace( request.QueryString["ChannelId"] ) )
                {
                    var channelIdQueryString = request.QueryString["ChannelId"];
                    int channelId;
                    foreach ( var id in channelIdQueryString.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) )
                    {
                        if ( int.TryParse( id, out channelId ) )
                        {
                            channelIds.Add( channelId );
                        }
                    }
                }
                if ( channelIds.Count < 1 )
                {
                    response.Write( "Invalid channel id(s)." );
                    response.StatusCode = 200;
                    return;
                }

                if ( request.QueryString["TemplateId"] == null || !int.TryParse( request.QueryString["TemplateId"], out templateDefinedValueId ) )
                {
                    dvRssTemplate = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.DEFAULT_RSS_CHANNEL );
                }
                else
                {
                    dvRssTemplate = DefinedValueCache.Get( templateDefinedValueId );
                }

                rssTemplate = dvRssTemplate.GetAttributeValue( "Template" );

                if ( request.QueryString["EnableDebug"] != null )
                {
                    // when in debug mode we need to export as html and linkin styles so that the debug info will be displayed
                    string appPath = HttpContext.Current.Request.ApplicationPath;

                    response.Write( "<html>" );
                    response.Write( "<head>" );
                    response.Write( string.Format( "<link rel='stylesheet' type='text/css' href='{0}Themes/Rock/Styles/bootstrap.css'>", appPath ) );
                    response.Write( string.Format( "<link rel='stylesheet' type='text/css' href='{0}Themes/Rock/Styles/theme.css'>", appPath ) );
                    response.Write( string.Format( "<script src='{0}Scripts/jquery-1.12.4.min.js'></script>", appPath ) );
                    response.Write( string.Format( "<script src='{0}Scripts/bootstrap.min.js'></script>", appPath ) );
                    response.Write( "</head>" );
                    response.Write( "<body style='padding: 24px;'>" );
                }
                else
                {
                    if ( string.IsNullOrWhiteSpace( dvRssTemplate.GetAttributeValue( "MimeType" ) ) )
                    {
                        response.ContentType = "application/rss+xml";
                    }
                    else
                    {
                        response.ContentType = dvRssTemplate.GetAttributeValue( "MimeType" );
                    }
                }

                ContentChannelService channelService = new ContentChannelService( rockContext );

                var channels = channelService.Queryable( "ContentChannelType" ).Where( c => channelIds.Contains( c.Id ) && c.EnableRss == true );

                if ( channels.Count() > 0 )
                {
                    // load merge fields
                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                    mergeFields.Add( "Channels", channels );

                    Dictionary<string, object> requestObjects = new Dictionary<string, object>();
                    requestObjects.Add( "Scheme", request.Url.Scheme );
                    requestObjects.Add( "Host", request.Url.Host );
                    requestObjects.Add( "Authority", request.Url.Authority );
                    requestObjects.Add( "LocalPath", request.Url.LocalPath );
                    requestObjects.Add( "AbsoluteUri", request.Url.AbsoluteUri );
                    requestObjects.Add( "AbsolutePath", request.Url.AbsolutePath );
                    requestObjects.Add( "Port", request.Url.Port );
                    requestObjects.Add( "Query", request.Url.Query );
                    requestObjects.Add( "OriginalString", request.Url.OriginalString );

                    mergeFields.Add( "Request", requestObjects );

                    // check for new rss item limit
                    if ( request.QueryString["Count"] != null )
                    {
                        int.TryParse( request.QueryString["Count"], out rssItemLimit );
                    }
                    ContentChannelItemService contentService = new ContentChannelItemService( rockContext );
                    var finalContent = contentService.Queryable( "ContentChannelType" ).Where( c => c == null );
                    foreach ( var channel in channels )
                    {
                        // get channel items

                        var content = contentService.Queryable( "ContentChannelType" )
                                        .Where( c =>
                                            c.ContentChannelId == channel.Id &&
                                            ( c.Status == ContentChannelItemStatus.Approved || c.ContentChannel.RequiresApproval == false ) &&
                                            c.StartDateTime <= RockDateTime.Now );

                        if ( channel.ContentChannelType.DateRangeType == ContentChannelDateType.DateRange && request.QueryString["IgnoreExpire"] == null )
                        {
                            if ( channel.ContentChannelType.IncludeTime )
                            {
                                content = content.Where( c => c.ExpireDateTime >= RockDateTime.Now );
                            }
                            else
                            {
                                content = content.Where( c => c.ExpireDateTime > RockDateTime.Today );
                            }
                        }

                        if ( channel.ItemsManuallyOrdered )
                        {
                            content = content.OrderBy( c => c.Order );
                        }
                        else
                        {
                            content = content.OrderByDescending( c => c.StartDateTime );
                        }

                        content = content.Take( rssItemLimit );

                        foreach ( var item in content )
                        {
                            item.Content = item.Content.ResolveMergeFields( mergeFields );

                            var childrentoAdd = new List<childItemsExtended>();
                            foreach ( var child in item.ChildItems )
                            {
                                var newchild = new childItemsExtended();
                                newchild.Attributes = child.Attributes;
                                newchild.AttributeValues = child.AttributeValues;
                                newchild.ChildContentChannelItemId = child.ChildContentChannelItemId;
                                newchild.ChildContentChannelItem = child.ChildContentChannelItem;
                                newchild.ContentChannelItem = child.ContentChannelItem;
                                newchild.ContentChannelItemId = child.ContentChannelItemId;
                                newchild.CreatedByPersonAlias = child.CreatedByPersonAlias;
                                newchild.CreatedByPersonAliasId = child.CreatedByPersonAliasId;
                                newchild.CreatedDateTime = child.CreatedDateTime;
                                newchild.CustomSortValue = child.CustomSortValue;
                                newchild.ForeignGuid = child.ForeignGuid;
                                newchild.ForeignId = child.ForeignId;
                                newchild.ForeignKey = child.ForeignKey;
                                newchild.ModifiedAuditValuesAlreadyUpdated = child.ModifiedAuditValuesAlreadyUpdated;
                                newchild.ModifiedByPersonAlias = child.ModifiedByPersonAlias;
                                newchild.ModifiedByPersonAliasId = child.ModifiedByPersonAliasId;
                                newchild.ModifiedDateTime = child.ModifiedDateTime;
                                newchild.Order = child.Order;
                                newchild.Id = child.Id;
                                newchild.Guid = child.Guid;
                                newchild.StartDateTime = child.ChildContentChannelItem.StartDateTime;
                                newchild.ExpireDateTime = child.ChildContentChannelItem.ExpireDateTime;
                                childrentoAdd.Add( newchild );
                            }
                            item.ChildItems.Clear();
                            foreach ( var child in childrentoAdd )
                            {
                                item.ChildItems.Add( child );
                            }

                            // resolve any relative links
                            var globalAttributes = Rock.Web.Cache.GlobalAttributesCache.Get();
                            string publicAppRoot = globalAttributes.GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
                            item.Content = item.Content.Replace( @" src=""/", @" src=""" + publicAppRoot );
                            item.Content = item.Content.Replace( @" href=""/", @" href=""" + publicAppRoot );

                            // get item attributes and add them as elements to the feed
                            item.LoadAttributes( rockContext );
                            foreach ( var attributeValue in item.AttributeValues )
                            {
                                attributeValue.Value.Value = attributeValue.Value.Value.ResolveMergeFields( mergeFields );
                            }
                        }
                        finalContent = finalContent.Concat( content );
                    }
                    mergeFields.Add( "Items", finalContent );

                    mergeFields.Add( "RockVersion", Rock.VersionInfo.VersionInfo.GetRockProductVersionNumber() );

                    var outputContent = rssTemplate.ResolveMergeFields( mergeFields );
                    // show debug info
                    if ( request.QueryString["EnableDebug"] != null )
                    {
                        response.Write( mergeFields.lavaDebugInfo() );
                        response.Write( "<pre>" );
                        response.Write( WebUtility.HtmlEncode( rssTemplate.ResolveMergeFields( mergeFields ) ) );
                        response.Write( "</pre>" );
                        response.Write( "</body>" );
                        response.Write( "</html" );
                    }
                    else
                    {
                        response.Write( outputContent );

                        var cacheDuration = dvRssTemplate.GetAttributeValue( "CacheDuration" ).AsInteger();
                        if ( cacheDuration > 0 )
                        {
                            var expiration = RockDateTime.Now.AddMinutes( cacheDuration );
                            if ( expiration > RockDateTime.Now )
                            {
                                RockCache.AddOrUpdate( cacheKey + ":MimeType", null, response.ContentType, expiration );
                                RockCache.AddOrUpdate( cacheKey, null, outputContent, expiration );
                            };
                        }
                    }
                }
                else
                {
                    response.StatusCode = 200;
                    response.Write( "Invalid channel ids or RSS is not enabled for the channel(s)." );
                    response.StatusCode = 200;
                    return;
                }

            }
            else
            {
                response.Write( "A ChannelId is required." );
                response.StatusCode = 200;
                return;
            }

        }

        /// <summary>
        /// Sends a 403 (forbidden)
        /// </summary>
        /// <param name="context">The context.</param>
        private void SendNotAuthorized( HttpContext context )
        {
            context.Response.StatusCode = System.Net.HttpStatusCode.Forbidden.ConvertToInt();
            context.Response.StatusDescription = "Not authorized to view file";
            context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler" /> instance.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler" /> instance is reusable; otherwise, false.</returns>
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
    public class childItemsExtended : ContentChannelItemAssociation
    {
        [LavaInclude]
        public DateTime StartDateTime { get; set; }
        [LavaInclude]
        public DateTime? ExpireDateTime { get; set; }
    }

    public static class KFSFilters
    {
        public static IEnumerable KFSSort( object input, string property = null, string sortOrder = "asc" )
        {
            List<object> ary;
            if ( input is IEnumerable )
                ary = ( ( IEnumerable ) input ).Flatten().Cast<object>().ToList();
            else
                ary = new List<object>( new[] { input } );
            if ( !ary.Any() )
                return ary;

            if ( string.IsNullOrEmpty( property ) )
                ary.Sort();
            else if ( ( ary.All( o => o is IDictionary ) ) && ( ( IDictionary ) ary.First() ).Contains( property ) )
                if ( sortOrder.ToLower() == "desc" )
                    ary.Sort( ( a, b ) => Comparer.Default.Compare( ( ( IDictionary ) b )[property], ( ( IDictionary ) a )[property] ) );
                else
                    ary.Sort( ( a, b ) => Comparer.Default.Compare( ( ( IDictionary ) a )[property], ( ( IDictionary ) b )[property] ) );
            else if ( ary.All( o => o.RespondTo( property ) ) )
                if ( sortOrder.ToLower() == "desc" )
                    ary.Sort( ( a, b ) => Comparer.Default.Compare( b.Send( property ), a.Send( property ) ) );
                else
                    ary.Sort( ( a, b ) => Comparer.Default.Compare( a.Send( property ), b.Send( property ) ) );

            return ary;
        }
    }
}
