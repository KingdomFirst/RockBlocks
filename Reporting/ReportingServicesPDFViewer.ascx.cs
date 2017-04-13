using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;

namespace com.kfs.Reporting.SQLReportingServices
{
    [TextField("")]
    public partial class ReportingServicesPDFViewer : RockBlock
    {
        #region Page Event
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }
        #endregion

    }
}