<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LoginLogout.ascx.cs" Inherits="RockWeb.Blocks.Security.LoginLogout" %>

<asp:LinkButton ID="lbLoginLogout" runat="server" OnClick="lbLoginLogout_Click" CausesValidation="false"></asp:LinkButton>

<asp:HiddenField ID="hfActionType" runat="server" />
