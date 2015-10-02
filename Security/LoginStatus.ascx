<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LoginStatus.ascx.cs" Inherits="RockWeb.Blocks.Security.LoginStatus" %>

<ul>
    <li>
        <a ID="aMyAccount" runat="server" class="navbar-link loginstatus" href="#">
            <%--<div id="divProfilePhoto" runat="server" class="profile-photo"></div>--%>
            <asp:Image ID="imgProvilePhoto" CssClass="profile-photo" runat="server" />
            <asp:PlaceHolder ID="phHello" runat="server">
                <asp:Literal ID="lHello" runat="server" /></asp:PlaceHolder>
        </a>
        <asp:LinkButton ID="lbLogin" runat="server" OnClick="lbLoginLogout_Click" CausesValidation="false" Text="Login"></asp:LinkButton>
    </li>
</ul>
