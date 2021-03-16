<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ConnectionOpportunitySearch.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Connection.OpportunitySearch" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maWarning" runat="server" />

        <asp:Panel ID="pnlSearch" runat="server" CssClass="row" DefaultButton="btnSearch">
            <h3>Search</h3>

            <Rock:RockTextBox ID="tbSearchName" runat="server" Label="Name" />

            <Rock:RockCheckBoxList ID="cblCampus" runat="server" Label="Campuses" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />

            <asp:PlaceHolder ID="phAttributeFilters" runat="server" />

            <Rock:BootstrapButton ID="btnSearch" CssClass="btn btn-primary" runat="server" OnClick="btnSearch_Click" Text="Search" />
        </asp:Panel>

        <asp:Panel pnl="pnlSearchDropdowns" runat="server" CssClass="row">
            <asp:Panel ID="pnlAttributeOne" CssClass="row" runat="server">
                <asp:PlaceHolder ID="phAttributeOne" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlAttributeTwo" CssClass="row" runat="server">
                <asp:PlaceHolder ID="phAttributeTwo" runat="server" />
            </asp:Panel>
        </asp:Panel>

        <asp:Literal ID="lOutput" runat="server"></asp:Literal>

        <asp:Literal ID="lDebug" Visible="false" runat="server"></asp:Literal>

    </ContentTemplate>
</asp:UpdatePanel>
