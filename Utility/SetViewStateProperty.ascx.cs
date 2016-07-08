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
using System.Web;

using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.KFS.Utility
{
    /// <summary>
    /// A  block that updates the value of a ViewState item.
    /// </summary>
    [DisplayName( "ViewState Value Override" )]
    [Category( "KFS > Utility" )]
    [Description( "Sets a ViewState value via block settings." )]

    [TextField( "ViewState Item", "The name of the ViewState item to set. Sample: MergeTemplatePageRoute", true )]
    [TextField( "ViewState Value", "The value of the ViewState item. Sample: ~/FinanceMergeTemplate/{0}", false )]

    public partial class SetViewStateProperty : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !String.IsNullOrWhiteSpace( GetAttributeValue( "ViewStateItem" ) ) )
            {
                ViewState[GetAttributeValue( "ViewStateItem" )] = GetAttributeValue( "ViewStateValue" ) ?? "";
            }
        }
    }
}
