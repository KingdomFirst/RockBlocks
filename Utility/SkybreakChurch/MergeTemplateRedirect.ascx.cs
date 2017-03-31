// <copyright>
// Copyright 2013 by the Spark Development Network
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
using System.ComponentModel;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.KFS.Utility
{
    /// <summary>
    /// A  block that updates the value of a ViewState item.
    /// </summary>
    [DisplayName( "Merge Template Redirect" )]
    [Category( "KFS > Utility" )]
    [Description( "Redirects to desired Merge Template Entry page." )]

    [TextField( "Redirect Page Route", "Sample: ~/FinanceMergeTemplate/{0}", true )]
    [TextField( "Button Text", "Text to use in the redirect button. Default: Redirect", false )]

    public partial class MergeTemplateRedirect : RockBlock
    {
        int? entitySetId = null;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !String.IsNullOrWhiteSpace( GetAttributeValue( "RedirectPageRoute" ) ) )
            {
                entitySetId = this.PageParameter( "Set" ).AsIntegerOrNull();
            }

            if ( !String.IsNullOrWhiteSpace( GetAttributeValue( "ButtonText" ) ) )
            {
                btnMergeRedirect.Text = GetAttributeValue( "ButtonText" );
            }
        }

        protected void btnRedirect_Click( object sender, EventArgs e )
        {
            if ( entitySetId != null )
            {
                Response.Redirect( String.Format( GetAttributeValue( "RedirectPageRoute" ), entitySetId.ToString() ) );
            }
        }
    }
}
