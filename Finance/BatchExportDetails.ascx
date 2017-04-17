<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BatchExportDetails.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Finance.BatchExportDetails" %>

<asp:UpdatePanel runat="server" ID="upnlBatchExportDetails">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlBatchExportDetails" CssClass="row">
            <asp:Panel runat="server" ID="pnlExportBatchButton" Visible="false" CssClass="col-md-12 pull-right">
                <p>
                    &nbsp;<asp:LinkButton runat="server" ID="lbExportBatchButton" Visible="true" CssClass="btn btn-default pull-right" OnClick="lbExportBatchButton_Click">Export Batch</asp:LinkButton><br />
                    &nbsp;
                </p>
            </asp:Panel>
            <asp:Panel runat="server" ID="pnlDateExported" Visible="false" CssClass="col-md-12">
                <div class="label label-default pull-right">
                    <asp:Literal runat="server" ID="litDateExported" Visible="true"></asp:Literal>
                </div>
                <br />
                <p class="pull-right">&nbsp;<asp:Button runat="server" ID="btnRemoveDateExported" Visible="false" CssClass="btn btn-default btn-xs" OnClick="btnRemoveDateExported_Click"></asp:Button></p>
            </asp:Panel>
            <div class="clearfix">
                <br />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
