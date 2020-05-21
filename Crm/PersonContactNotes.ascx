<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonContactNotes.ascx.cs" Inherits="RockWeb.Plugins.kfs_rocks.Crm.Notes" %>

<asp:UpdatePanel ID="upPersonContactNotes" runat="server">
    <ContentTemplate>
        <div class="row row-eq-height">
            <div class="col-md-3">
                <asp:Panel ID="pnlSelectPerson" CssClass="panel panel-block" runat="server">
                    <div class="panel-heading">
                        <h1 class="panel-title"><i class="fa fa-user"></i> Select a Person</h1>
                    </div>
                    <div class="panel-body">
                        <Rock:PersonPicker ID="ppPerson" runat="server" OnSelectPerson="SelectPerson" />
                        <Rock:GroupMemberPicker ID="gmpGroupMember" runat="server" OnSelectedIndexChanged="SelectPerson"></Rock:GroupMemberPicker>
                        <asp:Literal ID="lLavaPersonInfo" runat="server" />
                    </div>
                </asp:Panel>
            </div>
            <div class="col-md-9">
                <asp:Panel ID="pnlNoteEntry" CssClass="panel panel-block" runat="server">
                    <div class="panel-heading">
                        <h1 class="panel-title"><i class="fa fa-sticky-note"></i> Add a Note</h1>
                    </div>
                    <div class="panel-body">
                        <Rock:NoteEditor ID="noteEditor" runat="server" />
                    </div>
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
