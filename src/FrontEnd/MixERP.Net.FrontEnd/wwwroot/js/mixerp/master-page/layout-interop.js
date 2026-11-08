/*
    Layout JS-interop helpers.

    MIGRATED FROM: the inline onclick handlers of MixERPMaster.Master and BackendMaster.Master
    (`onclick=" $('#MenuContainer').toggle(100); "` and `onclick=" toggleSidebar(this); "`).
    Blazor handles the click in C# (@onclick) and calls these helpers through IJSRuntime, because
    inline <script>/onclick javascript inside a .razor file is not executed.
*/
(function () {
    "use strict";

    //MixERPMaster: $('#MenuContainer').toggle(100);
    window.mixerpToggleElement = function (selector, duration) {
        $(selector).toggle(duration);
    };

    //BackendMaster: toggleSidebar(this) — defined by /js/mixerp/master-page/menu.js. The argument
    //was never used by that function, so nothing is lost by not passing the element.
    window.mixerpToggleSidebar = function () {
        if (typeof toggleSidebar === "function") {
            toggleSidebar(null);
            return;
        }

        if (window.console) {
            console.error("toggleSidebar is not loaded (/js/mixerp/master-page/menu.js); the sidebar cannot be toggled.");
        }
    };
})();
