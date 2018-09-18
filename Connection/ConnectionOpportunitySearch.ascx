<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ConnectionOpportunitySearch.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Connection.OpportunitySearch" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maWarning" runat="server" />
        
        <asp:Panel ID="pnlSearch" CssClass="row" runat="server">
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