<%@ Control Language="C#" AutoEventWireup="true" CodeFile="DisplayMessages.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.CheckIn.DisplayMessages" %>
<asp:UpdatePanel ID="upContent" runat="server">
<ContentTemplate>

    <Rock:ModalAlert ID="maWarning" runat="server" />

</ContentTemplate>
</asp:UpdatePanel>
