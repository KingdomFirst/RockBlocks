<%@ Control Language="C#" AutoEventWireup="true" CodeFile="DefinedValueList.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Core.DefinedValueList" %>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlContent" CssClass="panel panel-block" runat="server">

            <asp:HiddenField ID="hfDefinedTypeId" runat="server" />

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-file-o"></i>
                    <asp:Literal ID="lTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">
                <asp:Panel ID="pnlList" runat="server" Visible="false">
                    <asp:Panel ID="pnlValues" runat="server">
                        <Rock:ModalAlert ID="mdGridWarningValues" runat="server" />
                        <div class="grid grid-panel">
                            <Rock:GridFilter ID="gfDefinedValues" runat="server" OnClearFilterClick="gfDefinedValues_ClearFilterClick">
                                <Rock:RockTextBox ID="tbValue" runat="server" Label="Value"></Rock:RockTextBox>
                                <Rock:RockTextBox ID="tbDescription" runat="server" Label="Description"></Rock:RockTextBox>
                                <Rock:RockCheckBox ID="cbActive" runat="server" Label="Active" Checked="true" />
                                <asp:PlaceHolder ID="phAttributeFilters" runat="server" />
                            </Rock:GridFilter>
                            <Rock:Grid ID="gDefinedValues" runat="server" AllowPaging="true" DisplayType="Full" OnRowSelected="gDefinedValues_Edit" AllowSorting="True">
                                <Columns>
                                    <Rock:RockBoundField DataField="Order" HeaderText="Order" SortExpression="Order" />
                                    <Rock:RockBoundField DataField="Value" HeaderText="Value" SortExpression="Value" />
                                    <Rock:RockBoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                                    <Rock:BoolField DataField="IsActive" HeaderText="Active" />
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </asp:Panel>
                </asp:Panel>
            </div>

            <Rock:ModalDialog ID="modalValue" runat="server" Title="Defined Value" ValidationGroup="Value">
                <Content>
                    <asp:HiddenField ID="hfDefinedValueId" runat="server" />
                    <asp:ValidationSummary ID="valSummaryValue" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="Value" />
                    <legend>
                        <asp:Literal ID="lActionTitleDefinedValue" runat="server" />
                    </legend>
                    <fieldset>
                        <Rock:DataTextBox ID="tbValueName" runat="server" SourceTypeName="Rock.Model.DefinedValue, Rock" PropertyName="Value" ValidationGroup="Value" Label="Value" />
                        <Rock:DataTextBox ID="tbValueDescription" runat="server" SourceTypeName="Rock.Model.DefinedValue, Rock" PropertyName="Description" TextMode="MultiLine" Rows="3" ValidationGroup="Value" ValidateRequestMode="Disabled" />
                        <asp:CheckBox ID="cbValueActive" runat="server" Text="Active" />
                        <div class="attributes">
                            <Rock:AttributeValuesContainer ID="avcDefinedValueAttributes" runat="server" />
                        </div>
                    </fieldset>
                </Content>
            </Rock:ModalDialog>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
