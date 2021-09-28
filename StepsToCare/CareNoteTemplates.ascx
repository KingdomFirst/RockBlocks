<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CareNoteTemplates.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.StepsToCare.CareNoteTemplates" %>
<style>
    .modal-panel-drawer {
        margin-top: -8px;
        margin-left: -12px;
        margin-right: -12px;
        margin-bottom: 12px;
        width: auto;
    }
</style>
<asp:UpdatePanel runat="server" ID="upnlCareNoteTemplates">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <asp:HiddenField ID="hfNoteTemplateId" runat="server" />
            <div class="panel-heading">
                <h1 class="panel-title pull-left"><i class="fas fa-notes-medical"></i>Care Note Templates</h1>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" DisplayType="Full" AllowSorting="false" OnRowDataBound="gList_RowDataBound" OnRowSelected="gList_Edit" OnGridReorder="gList_GridReorder" ExportSource="DataSource">
                        <Columns>
                            <Rock:ReorderField></Rock:ReorderField>
                            <Rock:RockTemplateField SortExpression="" HeaderText="Icon" ItemStyle-CssClass="text-center fa-lg" HeaderStyle-CssClass="text-center">
                                <ItemTemplate>
                                    <asp:Literal ID="lIcon" runat="server" />
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:RockBoundField DataField="Note" HeaderText="Note" />
                            <Rock:BoolField DataField="IsActive" HeaderText="Active"></Rock:BoolField>
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>

        <Rock:ModalDialog ID="mdAddNoteTemplate" runat="server" Title="Add Worker" OnSaveClick="mdAddNoteTemplate_SaveClick" OnSaveThenAddClick="mdAddNoteTemplate_SaveThenAddClick" ValidationGroup="AddNoteTemplate">
            <Content>
                <Rock:PanelDrawer ID="pdAuditDetails" runat="server" CssClass="modal-panel-drawer"></Rock:PanelDrawer>
                <asp:ValidationSummary ID="vsAddNoteTemplate" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="AddNoteTemplate" />

                <div class="row">
                    <div class="col-md-4">
                        <Rock:RockTextBox ID="tbIcon" runat="server" Label="Icon"></Rock:RockTextBox>
                    </div>
                    <div class="col-md-4">
                        <Rock:RockTextBox ID="tbNote" runat="server" Label="Note"></Rock:RockTextBox>
                    </div>
                    <div class="col-md-4">
                        <Rock:RockCheckBox ID="cbActive" runat="server" Label="Active" Checked="true" />
                    </div>
                </div>

                <Rock:DynamicPlaceholder ID="phAttributes" runat="server" />
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>