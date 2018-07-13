<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonAttributeFromAnotherEntity.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Utility.PersonAttributeFromAnotherEntity" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" runat="server" CssClass="panel panel-block" Visible="false">
            <div class="panel-heading"><asp:Literal ID="ltTitle" runat="server"></asp:Literal></div>
            <div class="panel-body">
                <asp:Panel ID="pnlAttributeValue" runat="server">
                    <asp:PlaceHolder ID="phEditAttributes" runat="server"></asp:PlaceHolder>

                    <fieldset id="fsAttributes" runat="server" class="attribute-values"></fieldset>

                    <asp:LinkButton ID="btnSave" runat="server" CssClass="btn btn-primary" Text="Save" OnClick="btnSave_Click"></asp:LinkButton>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
