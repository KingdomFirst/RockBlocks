<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MergeTemplateRedirect.ascx.cs" Inherits="RockWeb.Blocks.KFS.Utility.MergeTemplateRedirect" %>

<asp:LinkButton ID="btnMergeRedirect" runat="server" Text="Redirect" CssClass="btn btn-primary" OnClientClick="$('.js-merge-error').hide()" OnClick="btnRedirect_Click" />
