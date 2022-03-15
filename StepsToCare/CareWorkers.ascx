<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CareWorkers.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.StepsToCare.CareWorkers" %>
<style>
    .modal-panel-drawer {
        margin-top: -8px;
        margin-left: -12px;
        margin-right: -12px;
        margin-bottom: 12px;
        width: auto;
    }
</style>
<asp:UpdatePanel runat="server" ID="upnlCareWorkers">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <asp:HiddenField ID="hfCareWorkerId" runat="server" />
            <div class="panel-heading">
                <h1 class="panel-title pull-left"><i class="fas fa-user-nurse"></i>Care Workers</h1>
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
                            <Rock:PersonField DataField="PersonAlias.Person" SortExpression="PersonAlias.Person.LastName, PersonAlias.Person.NickName, LastName, FirstName" HeaderText="Name"></Rock:PersonField>
                            <Rock:DefinedValueField DataField="CategoryValues" HeaderText="Category"></Rock:DefinedValueField>
                            <Rock:CampusField DataField="Campuses" HeaderText="Campus" SortExpression="Campuses" />
                            <Rock:RockBoundField DataField="AgeRangeMin" HeaderText="Age Min"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="AgeRangeMax" HeaderText="Age Max"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Gender" HeaderText="Gender"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="GeoFenceId" HeaderText="Geofence"></Rock:RockBoundField>
                            <Rock:BoolField DataField="IsActive" HeaderText="Active"></Rock:BoolField>
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
                        <Rock:DefinedValuesPickerEnhanced ID="dvpCategory" runat="server" Label="Category" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="CategoryValueId" />
                    </div>
                    <div class="col-md-4">
                        <Rock:CampusesPicker ID="cpCampus" runat="server" Label="Campus" />
                    </div>
                </div>

                <div class="row">
                    <div class="col-md-4">
                        <Rock:LocationPicker ID="lpGeofenceLocation" runat="server" Label="Geofence Location" AllowedPickerModes="Named" Help="If geofence is set it is used for auto assignment of care needs based on where the person's home is located. Only named Polygon/Geofence locations are supported to minimize where you can create the area for use in multiple entities such as Groups." />
                    </div>
                    <div class="col-md-4">
                        <Rock:NumberRangeEditor ID="nreAgeRange" runat="server" Label="Age Range" Help="If using 'Include Family' checkbox on Care Entry it will take into account age ranges for appropriate family members" />
                    </div>
                    <div class="col-md-4">
                        <Rock:RockDropDownList ID="ddlGender" runat="server" Label="Gender" Help="If using 'Include Family' checkbox on Care Entry it will take into account Gender for appropriate family members.">
                            <asp:ListItem Text="" Value="" />
                            <asp:ListItem Text="Male" Value="Male" />
                            <asp:ListItem Text="Female" Value="Female" />
                            <asp:ListItem Text="Unknown" Value="Unknown" />
                        </Rock:RockDropDownList>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-4">
                        <Rock:RockCheckBox ID="cbActive" runat="server" Label="Active" />
                    </div>
                </div>

                <Rock:DynamicPlaceholder ID="phAttributes" runat="server" />
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
