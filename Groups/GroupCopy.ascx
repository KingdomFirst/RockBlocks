<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupCopy.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Groups.GroupCopy" %>

<asp:UpdatePanel runat="server" ID="upnlGroupCopy">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlGroupCopy" CssClass="row" Visible="false" >
            <asp:HiddenField ID="hfGroupId" runat="server" />
            <asp:Panel runat="server" ID="pnlCopyButton" Visible="true" CssClass="col-md-12 pull-right">
                <p>
                    &nbsp;<asp:LinkButton runat="server" ID="lbCopyButton" Visible="true" CssClass="btn btn-default btn-groupcopy pull-right" OnClick="lbCopyButton_Click">Copy Group</asp:LinkButton><br />
                    &nbsp;
                </p>
            </asp:Panel>
            <div class="clearfix">
                <br />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
