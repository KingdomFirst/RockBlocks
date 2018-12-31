<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesFolderTree.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Reporting.ReportingServicesFolderTree" %>
<%--<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="false">--%>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfSelectedItem" runat="server" />
        <asp:HiddenField ID="hfSelectionType" runat="server" />
        <asp:HiddenField ID="hfExpandedItems" runat="server" />
        <Rock:NotificationBox ID="nbRockError" runat="server" NotificationBoxType="Danger" Visible="false" />
        <asp:Panel ID="pnlFolders" runat="server">
            <asp:Panel ID="pnlHeader" runat="server" CssClass="panel-heading">
                <h1 class="panel-title"><i class="fa fa-folder-open-o"></i>
                    <asp:Literal ID="lTitle" runat="server" /></h1>
            </asp:Panel>
            <asp:Panel ID="pnlTree" runat="server">
                <div id="folders" style="display: none;">
                    <asp:Literal ID="lFolders" runat="server" ViewStateMode="Disabled" />
                </div>
            </asp:Panel>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
