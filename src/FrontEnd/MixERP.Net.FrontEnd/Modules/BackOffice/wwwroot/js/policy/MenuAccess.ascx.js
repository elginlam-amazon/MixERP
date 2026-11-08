/*
    Menu-access policy controller for Modules/BackOffice/Policy/MenuAccess.

    BLAZOR ADAPTATION, behaviour preserved. The original body of this file was an anonymous
    `$(function () { ... })` block: it ran ONCE per document, which was enough under WebForms
    because every Show/Save was a full POSTBACK that re-rendered the page and therefore re-ran
    this script. Blazor re-renders the grid WITHOUT reloading the document, so a one-shot
    block would attach its handlers to the rows of the FIRST bind only and every row of a
    re-bound grid would become unclickable.

    The block is therefore now the NAMED function initializeMenuAccessGrid(), called from the
    same $(function(){}) it always was AND re-invoked by the component from
    OnAfterRenderAsync after every render. Its contents are unchanged except for
    `.off("click")` before each `.click(...)`, which makes the repeated call idempotent
    instead of stacking a second handler on the same element. Nothing else was touched.

    updateSelection() is unchanged and remains a GLOBAL: it is what fills
    #SelectedMenusHidden from the ticked checkboxes, and the ported Save handler invokes it by
    that name immediately before reading the element - the same moment the legacy
    OnClientClick="return updateSelection();" ran it.

    ⚠ POSITIONAL GRID CONTRACT - DO NOT REORDER #MenuAccessGridView's COLUMNS.
    updateSelection() reads each ticked row's `td:nth-child(3)`, which is the MenuId column, so
    the ported component emits the six columns of DbGetMenuPolicyResult in the data source's own
    order (RowNumber, Access, MenuId, MenuCode, MenuText, Url). `grid.find("input")` likewise
    assumes the Access column renders a (disabled) checkbox, which is what the WebForms
    auto-generated boolean column produced and what the ported component's column template
    reproduces.
*/

function initializeMenuAccessGrid() {
    var grid = $("#MenuAccessGridView");
    var checkAllButton = $("#CheckAllButton");
    var uncheckAllButton = $("#UncheckAllButton");

    grid.find("input").removeAttr("disabled");

    checkAllButton.off("click").click(function () {
        grid.find("input").prop("checked", true);
    });

    uncheckAllButton.off("click").click(function () {
        grid.find("input").prop("checked", false);
    });

    grid.find("tr").off("click").click(function () {
        var el = $(this).find("input");
        el.prop("checked", !el.prop("checked"));
    });
}

$(function () {
    initializeMenuAccessGrid();
});

function updateSelection() {
    var selectedMenusHidden = $("#SelectedMenusHidden");
    var selectedElements = $("input:checked");
    var items = [];

    selectedElements.each(function () {
        var menuId = $(this).closest("tr").find("td:nth-child(3)").html();
        items.push(menuId);
    });

    selectedMenusHidden.val(items.join(","));
};
