<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CareWorkers.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.StepsToCare.CareWorkers" %>
<style>
    .modal-panel-drawer {
        margin-top: -8px;
        margin-left: -12px;
        margin-right: -12px;
        width: auto;
    }
</style>
<asp:UpdatePanel runat="server" ID="upnlCareWorkers">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <asp:HiddenField ID="hfCareWorkerId" runat="server" />
            <div class="panel-heading">
                <h1 class="panel-title pull-left"><i class="fas fa-user-nurse"></i> Care Workers</h1>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue" OnClearFilterClick="rFilter_ClearFilterClick">
                        <Rock:DefinedValuePicker ID="dvpFilterCategory" runat="server" Label="Category" DataTextField="Value" DataValueField="Id" />
                        <Rock:CampusPicker ID="cpFilterCampus" runat="server" Label="Campus" />
                        <asp:PlaceHolder ID="phAttributeFilters" runat="server" />
                    </Rock:GridFilter>
                    <Rock:Grid ID="gList" runat="server" DisplayType="Full" AllowSorting="true" OnRowDataBound="gList_RowDataBound" OnRowSelected="gList_Edit" ExportSource="DataSource">
                        <Columns>
                            <Rock:DefinedValueField DataField="CategoryValueId" SortExpression="Category.Value" HeaderText="Category"></Rock:DefinedValueField>
                            <Rock:PersonField DataField="PersonAlias.Person" SortExpression="PersonAlias.Person.LastName, PersonAlias.Person.NickName, LastName, FirstName" HeaderText="Name"></Rock:PersonField>
                            <Rock:RockBoundField DataField="Campus.Name" HeaderText="Campus" SortExpression="Campus.Name" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>

        </asp:Panel>

        <Rock:ModalDialog ID="mdAddPerson" runat="server" Title="Add Worker" OnSaveClick="mdAddPerson_SaveClick" OnSaveThenAddClick="mdAddPerson_SaveThenAddClick" ValidationGroup="AddPerson">
            <Content>
                <Rock:PanelDrawer ID="pdAuditDetails" runat="server" CssClass="modal-panel-drawer"></Rock:PanelDrawer>
                <asp:ValidationSummary ID="vsAddPerson" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="AddPerson" />
                <Rock:NotificationBox ID="nbAddPersonExists" runat="server" NotificationBoxType="Danger" Visible="false">
                    Person already exists in the workers with this category.
                </Rock:NotificationBox>

                <div class="row">
                    <div class="col-md-4">
                        <Rock:PersonPicker ID="ppNewPerson" runat="server" Label="Person" Required="true" CssClass="js-newperson" ValidationGroup="AddPerson" OnSelectPerson="ppPerson_SelectPerson" />
                    </div>
                    <div class="col-md-4">
                        <Rock:DefinedValuePicker ID="dvpCategory" runat="server" Label="Category" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="CategoryValueId" />
                    </div>
                    <div class="col-md-4">
                        <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" />
                    </div>
                </div>

                <Rock:DynamicPlaceholder ID="phAttributes" runat="server" />

            </Content>
        </Rock:ModalDialog>

    </ContentTemplate>
</asp:UpdatePanel>
