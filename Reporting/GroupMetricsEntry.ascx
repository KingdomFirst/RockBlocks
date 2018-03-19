<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMetricsEntry.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Reporting.GroupMetricsEntry" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
<ContentTemplate>

    <div class="panel panel-block">

        <div class="panel-heading">
            <h1 class="panel-title"><i class="fa fa-signal"></i> Metric Entry</h1>
        </div>

        <div class="panel-body">

            <asp:Panel ID="pnlMetrics" runat="server">

                <asp:ValidationSummary ID="vsDetails" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
                <Rock:NotificationBox ID="nbMetricsSaved" runat="server" Text="Metric Values Have Been Updated" NotificationBoxType="Success" Visible="false" />

                <div class="form-horizontal label-md" >
                   <Rock:GroupPicker  ID="gpSelectGroup" runat="server" OnSelectItem="bddl_SelectionChanged" Label="Select a Group" />
                   <Rock:DatePicker ID="dpMetricValueDateTime" runat="server" Label="Date" AutoPostBack="true" OnTextChanged="bddl_SelectionChanged" />
                    <asp:Repeater ID="rptrMetric" runat="server" OnItemDataBound="rptrMetric_ItemDataBound">
                        <ItemTemplate>
                            <asp:HiddenField ID="hfMetricId" runat="server" Value='<%# Eval("Id") %>' />
                            <Rock:NumberBox ID="nbMetricValue" runat="server" NumberType="Double" Label='<%# Eval( "Name") %>' Text='<%# Eval( "Value") %>' />
                        </ItemTemplate>
                    </asp:Repeater>
                </div>

                <Rock:RockTextBox ID="tbNote" runat="server" Label="Note" TextMode="MultiLine" Rows="4" />

                <div class="actions">
                    <asp:LinkButton ID="btnSave" runat="server" Text="Save" AccessKey="s" ToolTip="Alt+s" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                </div>

            </asp:Panel>

            <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-warning">
                No group was available from the querystring.
            </asp:Panel>

        </div>

    </div>

</ContentTemplate>
</asp:UpdatePanel>
